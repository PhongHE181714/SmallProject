# Giải thích cơ chế State trong React, Redux Toolkit và RTK Query (kèm ví dụ trong SmallProject)

Tài liệu này giải thích chi tiết cơ chế hoạt động của **State trong React**, **Redux / Redux Toolkit (RTK)** và **RTK Query**, đồng thời đối chiếu trực tiếp với mã nguồn thực tế trong thư mục `FE/src` của dự án **SmallProject**.

---

## Mục lục

1. [Bức tranh tổng thể về State trong SmallProject](#1-bức-tranh-tổng-thể-về-state-trong-smallproject)
2. [Cơ chế State trong React (`useState` & `useEffect`)](#2-cơ-chế-state-trong-react-usestate--useeffect)
3. [Cơ chế State trong Redux & Redux Toolkit](#3-cơ-chế-state-trong-redux--redux-toolkit)
4. [RTK Query chi tiết: Kiến trúc, Cache, Lifecycle & Tag Invalidation](#4-rtk-query-chi-tiết-kiến-trúc-cache-lifecycle--tag-invalidation)
5. [Cách SmallProject kết hợp 3 lớp State trong thực tế](#5-cách-smallproject-kết-hợp-3-lớp-state-trong-thực-tế)
6. [Bảng so sánh & Quy tắc lựa chọn nhanh](#6-bảng-so-sánh--quy-tắc-lựa-chọn-nhanh)

---

## 1. Bức tranh tổng thể về State trong SmallProject

Trong ứng dụng React hiện đại, không phải dữ liệu nào cũng đưa vào Redux, và cũng không phải dữ liệu nào cũng giữ bằng `useState`. Trong `FE/`, dữ liệu thay đổi theo thời gian (state) được chia làm **3 nhóm rõ ràng**:

1. **Local / UI State (React `useState`)**: Dữ liệu chỉ thuộc về một component hoặc hộp thoại cụ thể (giá trị input, lỗi validate từng ô, trạng thái mở/đóng dialog, bộ lọc đang gõ, dòng bảng đang mở rộng).
2. **Client Global State (Redux Slice + `createAsyncThunk`)**: Trạng thái chung do client tự định nghĩa và quản lý trong Redux store (ví dụ: `apiHealthSlice` kiểm tra kết nối DB).
3. **Server State / API Cache (RTK Query)**: Dữ liệu đọc từ Backend API cần cache, tự động đồng bộ, chia sẻ giữa nhiều component và làm mới theo tag (`ActiveProducts`, `SaleOuts`).

```text
+-------------------------------------------------------------------------+
|                           REACT COMPONENT TREE                          |
|                                                                         |
|  1. Local UI State (useState)                                           |
|     - App.tsx: activePage                                               |
|     - ProductMasterView / SaleOutView: query, searchField, dialog open  |
|     - CreateSaleOutDialog: header, items, headerErrors, isSubmitting    |
|                                                                         |
|  2. Redux Store (src/app/store.ts)                                      |
|     +-- state.apiHealth (Redux Slice thông thường)                      |
|     |     Quản lý bởi apiHealthSlice.ts + checkDatabaseConnection thunk |
|     |                                                                   |
|     +-- state.baseApi (RTK Query Slice + baseApi.middleware)            |
|           +-- queries: cache getActiveProducts, getSaleOuts(params)     |
|           +-- provided / subscriptions: quản lý Tag & Lifecycle         |
+-------------------------------------------------------------------------+
```

---

## 2. Cơ chế State trong React (`useState` & `useEffect`)

### 2.1. State trong React hoạt động như thế nào?

Một React component bản chất là một **hàm JavaScript** nhận `props` và trả về JSX.
- Biến cục bộ bình thường (`let count = 0`) sẽ **bị reset về giá trị ban đầu** mỗi lần component chạy lại (re-render).
- **`useState`** lưu giá trị ở bộ nhớ nội bộ của React gắn với vị trí component đó trên cây UI (Fiber tree), tồn tại xuyên suốt các lần re-render cho tới khi component bị gỡ bỏ (unmount).

Chu trình khi cập nhật state trong React:

```text
Người dùng thao tác (gõ input, bấm nút, submit)
        │
        ▼
Gọi hàm setter: setHeader(...) / setQuery(...)
        │
        ▼
React KHÔNG sửa state ngay lập tức ở dòng code tiếp theo,
mà đưa bản cập nhật vào hàng đợi & lên lịch Re-render
        │
        ▼
React gọi lại hàm Component với giá trị state MỚI
        │
        ▼
So sánh Virtual DOM cũ và mới (Reconciliation) → chỉ cập nhật phần DOM thật thay đổi
```

### 2.2. Các nguyên tắc cốt lõi khi dùng React State

#### a) Lazy Initialization (Khởi tạo lười)
Nếu giá trị ban đầu cần tính toán hoặc đọc từ `window.location` (URL), truyền **một hàm** vào `useState(() => ...)` để React chỉ chạy hàm đó **đúng 1 lần lúc mount**, không chạy lại ở các lần re-render sau.

**Ví dụ trong `FE/src/App.tsx`**:
```tsx
const [activePage, setActivePage] = useState<AppPage>(() =>
  getPageFromPath(window.location.pathname),
)
```

**Ví dụ trong `FE/src/features/saleOut/components/SaleOutView.tsx`** (đọc `page`, `pageSize`, `searchField`, `searchValue` từ URL query string lúc mới vào trang):
```tsx
const [query, setQuery] = useState<SaleOutQuery>(() =>
  readSearchablePageState<SaleOutSearchField>(
    { page: 1, pageSize: DEFAULT_PAGE_SIZE, searchField: 'CustomerPoNo', searchValue: '' },
    SALE_OUT_SEARCH_FIELDS.map((field) => field.value),
    MAX_PAGE_SIZE,
  ),
)
```

#### b) Tính bất biến (Immutability) và Functional Update (`prev => ...`)
React so sánh state cũ và state mới bằng tham chiếu (`Object.is(prevState, nextState)`). Nếu bạn sửa trực tiếp thuộc tính bên trong object hoặc `.push()` vào mảng cũ, tham chiếu vẫn giống nhau → **React có thể bỏ qua không re-render**.

Do đó luôn phải tạo **object / array / Set mới**:

**Ví dụ cập nhật object & array bất biến trong `FE/src/features/saleOut/components/CreateSaleOutDialog.tsx`**:
```tsx
// Cập nhật 1 field trong object header -> tạo object mới bằng spread (...)
const handleHeaderChange = (field: keyof HeaderForm, value: string) => {
  setHeader((current) => ({ ...current, [field]: value }))
  setHeaderErrors((current) => ({ ...current, [field]: undefined }))
  setSubmissionError('')
}

// Cập nhật 1 dòng trong mảng items -> tạo mảng mới bằng .map(), tạo item mới bằng spread
const handleItemChange = (key: string, field: SaleOutItemEditableField, value: string) => {
  setItems((current) =>
    current.map((item) => (item.key === key ? { ...item, [field]: value } : item)),
  )
  setItemErrors((current) => ({
    ...current,
    [key]: { ...current[key], [field]: undefined },
  }))
  setSubmissionError('')
}
```

Và khi dùng `Set<string>` để lưu các đơn Sale Out đang mở rộng dòng chi tiết trong `SaleOutView.tsx`, mỗi lần thay đổi cũng phải tạo `new Set(...)` mới chứ không `.add()` trực tiếp vào `Set` cũ.

Ngoài ra, dạng hàm `setQuery((currentQuery) => ...)` (**functional update**) đảm bảo luôn đọc đúng giá trị mới nhất kể cả khi nhiều `setState` được gom nhóm (batching) trong cùng 1 sự kiện.

#### c) Tách biệt "input đang gõ" và "query đang tìm"
Một pattern rất tốt trong `ProductMasterView.tsx` và `SaleOutView.tsx`:
- `searchField`, `searchValue` là state tạm gắn với ô nhập liệu (thay đổi liên tục mỗi phím gõ).
- `query` là state đại diện cho điều kiện lọc thực sự đã xác nhận.
- Chỉ khi người dùng submit form tìm kiếm (`handleSearch`), component mới cập nhật `query`:

```tsx
const handleSearch = (event: FormEvent<HTMLFormElement>) => {
  event.preventDefault()
  setQuery((currentQuery) => ({
    ...currentQuery,
    page: 1,
    searchField,
    searchValue: searchValue.trim(),
  }))
}
```
Nhờ tách như vậy, ứng dụng không gửi request API mỗi khi người dùng gõ 1 ký tự.

#### d) `useEffect` để đồng bộ State với hệ thống bên ngoài
`useEffect` không dùng để "tính toán giá trị suy diễn" (derived state), mà dùng để đồng bộ React state với **thế giới bên ngoài** (URL trình duyệt, sự kiện `window`, timer, hoặc `fetch` API).

Các ví dụ tiêu biểu trong dự án:
1. **Đồng bộ `query` lên URL** (`SaleOutView.tsx` & `ProductMasterView.tsx`):
   ```tsx
   useEffect(() => {
     writeSearchablePageState(query, {
       page: 1,
       pageSize: DEFAULT_PAGE_SIZE,
       searchField: 'CustomerPoNo',
       searchValue: '',
     })
   }, [query])
   ```
2. **Reset form khi mở dialog** (`CreateProductDialog.tsx` & `CreateSaleOutDialog.tsx`):
   ```tsx
   useEffect(() => {
     if (open) {
       setForm(INITIAL_FORM_STATE)
       setErrors({})
       setIsSubmitting(false)
     }
   }, [open])
   ```
3. **Gọi API thủ công kèm hủy request cũ bằng `AbortController`** (`ProductMasterView.tsx`):
   ```tsx
   useEffect(() => {
     const controller = new AbortController()
     setIsLoading(true)
     setErrorMessage('')

     void fetchProducts(query, controller.signal)
       .then((data) => {
         if (!controller.signal.aborted) {
           setResult(data)
         }
       })
       .catch((error: unknown) => {
         if (!controller.signal.aborted) {
           setResult(null)
           setErrorMessage(getProductErrorMessage(error, query.searchField, t))
         }
       })
       .finally(() => {
         if (!controller.signal.aborted) {
           setIsLoading(false)
         }
       })

     // Cleanup: hủy request cũ nếu query đổi nhanh hoặc component unmount
     return () => controller.abort()
   }, [query, retryCount, t])
   ```

> **Nhận xét quan trọng**: Đoạn code `ProductMasterView.tsx` ở trên cho thấy **chi phí của việc quản lý API bằng `useState` + `useEffect`**: bạn phải tự tạo 4 state (`result`, `isLoading`, `errorMessage`, `retryCount`), tự xử lý `AbortController`, không có cache giữa các trang/component, và nếu component unmount rồi mount lại thì mất sạch dữ liệu phải gọi lại từ đầu. Đây chính là lý do **Redux** và **RTK Query** xuất hiện.

---

## 3. Cơ chế State trong Redux & Redux Toolkit

### 3.1. Redux giải quyết bài toán gì?

Khi nhiều component ở các nhánh khác nhau cần dùng chung state (hoặc cần lưu state ngoài vòng đời mount/unmount của 1 component), việc truyền `props` từ trên xuống (prop drilling) trở nên rườm rà.

Redux đưa toàn bộ state dùng chung vào **một kho duy nhất (`Store`)** nằm ngoài cây component, vận hành theo **luồng dữ liệu một chiều (Unidirectional Data Flow)**:

```text
  +-------------------+       dispatch(action)       +-------------------------+
  |  React Component  | ---------------------------> |       Redux Store       |
  |  (UI / Event)     |                              |  (Middleware -> Reducer)|
  +-------------------+                              +-------------------------+
            ▲                                                    │
            │       useAppSelector() nhận state mới              │
            └────────────────────────────────────────────────────┘
```

### 3.2. Các khái niệm cốt lõi trong Redux Toolkit (RTK)

1. **Store (`configureStore`)**: Nơi giữ toàn bộ state tree của ứng dụng, chạy reducer để tạo state mới và thông báo cho các component đang lắng nghe.
2. **Action**: Một object thuần mô tả sự kiện vừa xảy ra, dạng `{ type: 'apiHealth/checkDatabaseConnection/pending', payload?: ... }`.
3. **Reducer & Immer**:
   - Reducer là hàm thuần `(currentState, action) => nextState`.
   - Trong Redux thuần ngày xưa, bạn phải viết `{ ...state, status: 'loading' }` rất dài dòng.
   - **Redux Toolkit tích hợp sẵn thư viện Immer**, cho phép viết code trông như đang mutate (`state.status = 'loading'`), nhưng bên dưới Immer tự động ghi nhận thay đổi trên một bản nháp (*Draft*) và xuất ra một **state mới bất biến hoàn toàn**.
4. **Thunk (`createAsyncThunk`)**:
   - Redux reducer phải là hàm đồng bộ và thuần túy (không được gọi `fetch` trực tiếp bên trong reducer).
   - Middleware `redux-thunk` (được `configureStore` bật sẵn) cho phép `dispatch` một hàm bất đồng bộ.
   - `createAsyncThunk` tự động sinh ra **3 action lifecycle**:
     - `<typePrefix>/pending`: khi bắt đầu gọi async
     - `<typePrefix>/fulfilled`: khi Promise trả về thành công (`action.payload` là kết quả)
     - `<typePrefix>/rejected`: khi Promise ném lỗi (`action.error` chứa thông tin lỗi)
5. **Selector (`useAppSelector`)**:
   - Component đăng ký đọc đúng phần state mình cần (ví dụ `state => state.apiHealth`).
   - Khi store cập nhật, React-Redux chỉ re-render những component có giá trị selector trả về **thay đổi**.

### 3.3. Ví dụ thực tế trong SmallProject (`apiHealth`)

#### Bước 1: Định nghĩa Slice & AsyncThunk (`FE/src/features/apiHealth/model/apiHealthSlice.ts`)

```ts
import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { get } from '../../../shared/api/httpClient'

type DatabasePingResponse = {
  connected: boolean
  database: string
}

type ApiHealthState = {
  status: 'idle' | 'loading' | 'succeeded' | 'failed'
  database: string
  error: string
}

const INITIAL_STATE: ApiHealthState = {
  status: 'idle',
  database: '',
  error: '',
}

// 1. Tạo AsyncThunk để gọi API kiểm tra DB
export const checkDatabaseConnection = createAsyncThunk(
  'apiHealth/checkDatabaseConnection',
  async () => get<DatabasePingResponse>('/api/database/ping'),
)

// 2. Tạo Slice quản lý state của tính năng apiHealth
const apiHealthSlice = createSlice({
  name: 'apiHealth',
  initialState: INITIAL_STATE,
  reducers: {}, // Không có action đồng bộ riêng
  extraReducers: (builder) => {
    builder
      .addCase(checkDatabaseConnection.pending, (state) => {
        state.status = 'loading'
        state.error = ''
      })
      .addCase(checkDatabaseConnection.fulfilled, (state, action) => {
        state.status = action.payload.connected ? 'succeeded' : 'failed'
        state.database = action.payload.database
        state.error = ''
      })
      .addCase(checkDatabaseConnection.rejected, (state, action) => {
        state.status = 'failed'
        state.error = action.error.message ?? ''
      })
  },
})

export default apiHealthSlice.reducer
```

#### Bước 2: Cấu hình Store (`FE/src/app/store.ts`) và Typed Hooks (`FE/src/app/hooks.ts`)

```ts
// FE/src/app/store.ts
import { configureStore } from '@reduxjs/toolkit'
import { setupListeners } from '@reduxjs/toolkit/query'
import apiHealthReducer from '../features/apiHealth/model/apiHealthSlice'
import '../features/productMaster/api/productQueries'
import '../features/saleOut/api/saleOutQueries'
import { baseApi } from '../shared/api/baseApi'

export const store = configureStore({
  reducer: {
    apiHealth: apiHealthReducer,
    [baseApi.reducerPath]: baseApi.reducer,
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(baseApi.middleware),
})

setupListeners(store.dispatch)

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
```

```ts
// FE/src/app/hooks.ts
import { useDispatch, useSelector } from 'react-redux'
import type { TypedUseSelectorHook } from 'react-redux'
import type { AppDispatch, RootState } from './store'

export const useAppDispatch = useDispatch.withTypes<AppDispatch>()
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector
```

> **Tại sao cần `useAppDispatch` và `useAppSelector`?**
> `useDispatch` và `useSelector` gốc của `react-redux` không biết kiểu `RootState` và `AppDispatch` của dự án. Tạo typed hooks trong `src/app/hooks.ts` giúp TypeScript tự gợi ý `state.apiHealth`, `state.baseApi` và hiểu được `dispatch(checkDatabaseConnection())` trả về một Promise có thể `.unwrap()` hoặc `void`.

#### Bước 3: Cung cấp Store cho toàn bộ React App (`FE/src/app/AppProviders.tsx` & `FE/src/main.tsx`)

```tsx
// FE/src/app/AppProviders.tsx
import type { PropsWithChildren } from 'react'
import { Provider } from 'react-redux'
import { store } from './store'

export default function AppProviders({ children }: PropsWithChildren) {
  return <Provider store={store}>{children}</Provider>
}
```

`<AppProviders>` bọc ngoài `<App />` trong `FE/src/main.tsx` để mọi component bên trong đều truy cập được `store` qua React Context.

#### Bước 4: Component đọc và gửi Action (`FE/src/features/apiHealth/components/ApiHealthStatus.tsx`)

```tsx
export default function ApiHealthStatus() {
  const { t } = useTranslation()
  const dispatch = useAppDispatch()
  const { status, database } = useAppSelector((state) => state.apiHealth)

  useEffect(() => {
    void dispatch(checkDatabaseConnection())
  }, [dispatch])

  const isLoading = status === 'idle' || status === 'loading'
  const isConnected = status === 'succeeded'
  // ...
```

**Diễn biến từng bước khi `ApiHealthStatus` chạy**:
1. Lúc mới mount: `state.apiHealth` là `{ status: 'idle', database: '', error: '' }` → `isLoading = true` → hiển thị spinner.
2. `useEffect` chạy → gọi `dispatch(checkDatabaseConnection())`.
3. `redux-thunk` chặn thunk này, dispatch ngay action `apiHealth/checkDatabaseConnection/pending` → `extraReducers` đổi `status` thành `'loading'`.
4. Hàm async gọi `GET /api/database/ping` qua `httpClient.ts`.
5. Nếu thành công: dispatch `.../fulfilled` với `action.payload = { connected: true, database: '...' }` → `status` thành `'succeeded'`, `database` được lưu → `useAppSelector` nhận state mới → component re-render hiển thị icon xanh và tên database.
6. Nếu thất bại: dispatch `.../rejected` → `status` thành `'failed'` → component re-render hiển thị nút **Retry** gọi lại `dispatch(checkDatabaseConnection())`.

---

## 4. RTK Query chi tiết: Kiến trúc, Cache, Lifecycle & Tag Invalidation

### 4.1. RTK Query là gì?

**RTK Query** là lớp **Data Fetching & Caching** được đóng gói sẵn bên trong `@reduxjs/toolkit` (`@reduxjs/toolkit/query/react`).

Nếu `createSlice` + `createAsyncThunk` giúp quản lý state client, thì **RTK Query** tự động hóa toàn bộ phần giao tiếp với server:
- Tự quản lý loading / fetching / error / data.
- Tự cache dữ liệu theo từng endpoint + tham số đầu vào.
- Chống gọi trùng request (**request deduplication**): nếu 2 component cùng mount và gọi `useGetActiveProductsQuery()` cùng lúc, RTK Query chỉ bắn **1 HTTP request** duy nhất và chia sẻ kết quả cho cả 2.
- Tự động làm mới dữ liệu (refetch) khi có **Tag Invalidation**, khi người dùng quay lại tab (`refetchOnFocus`), hoặc khi có mạng trở lại (`refetchOnReconnect`).
- Tự động dọn cache không còn ai sử dụng sau một khoảng thời gian (`keepUnusedDataFor`, mặc định **60 giây**).

### 4.2. Cơ chế hoạt động bên trong Redux Store

RTK Query **không phải là một thư viện state tách rời ngoài Redux** — nó chạy hoàn toàn trên chính Redux Store của bạn thông qua 2 thứ được đăng ký trong `FE/src/app/store.ts`:

```ts
reducer: {
  apiHealth: apiHealthReducer,
  [baseApi.reducerPath]: baseApi.reducer, // Slice chứa cache của RTK Query
},
middleware: (getDefaultMiddleware) =>
  getDefaultMiddleware().concat(baseApi.middleware), // Bộ não xử lý cache/refetch/GC
```

Khi bạn mở Redux DevTools, nhánh `state.baseApi` sẽ có cấu trúc nội bộ như sau:

```text
state.baseApi
├── queries: {
│     'getActiveProducts(undefined)': {
│       status: 'fulfilled',
│       data: [...],
│       fulfilledTimeStamp: ...
│     },
│     'getSaleOuts({"page":1,"pageSize":10,"searchField":"CustomerPoNo","searchValue":""})': {
│       status: 'fulfilled',
│       data: { items: [...], totalItems: ..., totalPages: ... }
│     }
│   }
├── mutations: { ... }
├── provided: {
│     tags: {
│       ActiveProducts: { __internal_without_id: ['getActiveProducts(undefined)'] },
│       SaleOuts: { __internal_without_id: ['getSaleOuts(...)'] }
│     }
│   }
├── subscriptions: {
│     'getActiveProducts(undefined)': { 'subId_1': { pollingInterval: 0, ... } }
│   }
└── config: { focused: true, online: true, refetchOnFocus: false, ... }
```

**Vai trò của `baseApi.middleware`**:
1. **Theo dõi subscription (Reference Counting)**: Mỗi component gọi `useXxxQuery(...)` sẽ tạo 1 `subscription` cho cache key tương ứng. Khi component unmount (hoặc `skip: true`), subscription bị gỡ.
2. **Dọn rác cache (Garbage Collection)**: Khi số lượng subscription của 1 cache key giảm về `0`, middleware bắt đầu đếm ngược `keepUnusedDataFor` (mặc định `60s`). Nếu trong 60s không có component nào mount lại để dùng key đó, cache entry sẽ bị xóa khỏi `state.baseApi.queries`.
3. **Deduplication**: Nếu `queries[cacheKey]` đang ở trạng thái `pending` hoặc đã `fulfilled` (còn hiệu lực), component thứ 2 gọi cùng hook với cùng tham số sẽ dùng lại luôn request/cache đó mà không gọi API nữa.
4. **Tag Invalidation**: Khi nhận action `invalidateTags(['ActiveProducts'])`, middleware tra bảng `provided.tags.ActiveProducts` để tìm tất cả `queryCacheKey` liên quan:
   - Nếu `queryCacheKey` đang có component subscribe (`subscriptions > 0`) → **refetch ngay lập tức**.
   - Nếu `queryCacheKey` đang không có component nào subscribe (nhưng chưa bị xóa khỏi cache) → đánh dấu cache đó là không còn hợp lệ (invalidate / xóa khỏi cache chờ lần tới mở lại sẽ fetch mới).

### 4.3. Chi tiết từng thành phần RTK Query trong SmallProject

#### Thành phần 1: Khởi tạo `baseApi` dùng chung (`FE/src/shared/api/baseApi.ts`)

```ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import { API_BASE_URL } from './httpClient'

export const baseApi = createApi({
  reducerPath: 'baseApi',
  baseQuery: fetchBaseQuery({ baseUrl: API_BASE_URL }),
  tagTypes: ['ActiveProducts', 'SaleOuts'],
  endpoints: () => ({}),
})
```

Điểm cần chú ý:
- Import từ `@reduxjs/toolkit/query/react` (có `/react`) để RTK Query tự sinh ra các React hooks (`use...Query`).
- `reducerPath: 'baseApi'`: tên nhánh trong `RootState` (`state.baseApi`).
- `fetchBaseQuery({ baseUrl: API_BASE_URL })`: wrapper gọn nhẹ quanh `fetch`, dùng chung `API_BASE_URL` từ `httpClient.ts` (`http://localhost:5069` qua API Gateway).
- `tagTypes: ['ActiveProducts', 'SaleOuts']`: khai báo danh sách các loại nhãn (tag) hợp lệ cho toàn bộ `baseApi`.
- `endpoints: () => ({})`: để trống ở `shared/api`! Dự án áp dụng **Code Splitting / Feature Injection** — mỗi feature tự khai báo endpoint của mình bằng `baseApi.injectEndpoints(...)`.

#### Thành phần 2: Kích hoạt Listener toàn cục (`FE/src/app/store.ts`)

```ts
import '../features/productMaster/api/productQueries'
import '../features/saleOut/api/saleOutQueries'
// ...
setupListeners(store.dispatch)
```

Hai chi tiết rất quan trọng ở `store.ts`:
1. **Import side-effect** `productQueries` và `saleOutQueries` ngay trong `store.ts`: đảm bảo các endpoint đã được inject vào `baseApi` ngay khi khởi tạo app (đặc biệt hữu ích để `baseApi.util.invalidateTags` hoạt động nhất quán ngay từ đầu).
2. **`setupListeners(store.dispatch)`**: đăng ký các listener của trình duyệt (`visibilitychange`, `focus`, `online`, `offline`). Nếu **không gọi `setupListeners`**, các tùy chọn như `refetchOnFocus: true` hoặc `refetchOnReconnect: true` sẽ **không hoạt động**.

#### Thành phần 3: Endpoint chuẩn với `query` (`FE/src/features/saleOut/api/saleOutQueries.ts`)

```ts
import { baseApi } from '../../../shared/api/baseApi'
import type { SaleOutSearchParams } from './saleOutApi'
import type { SaleOutSearchResponse } from '../model/saleOutTypes'

export const saleOutQueriesApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getSaleOuts: builder.query<SaleOutSearchResponse, SaleOutSearchParams>({
      query: (params) => {
        const queryParams: Record<string, string | number> = {
          page: params.page,
          pageSize: params.pageSize,
        }

        if (params.searchValue.trim()) {
          queryParams.searchField = params.searchField
          queryParams.searchValue = params.searchValue.trim()
        }

        return { url: '/api/sale-outs', params: queryParams }
      },
      providesTags: ['SaleOuts'],
    }),
  }),
})

export const { useGetSaleOutsQuery } = saleOutQueriesApi
```

Cách hoạt động:
- Kiểu generic `builder.query<SaleOutSearchResponse, SaleOutSearchParams>`: tham số 1 là kiểu dữ liệu trả về (`data`), tham số 2 là kiểu tham số đầu vào (`params`).
- RTK Query **serialize** `params` thành chuỗi để làm **cache key**. Ví dụ:
  - Trang 1 (`page: 1, pageSize: 10`) là một cache key riêng.
  - Trang 2 (`page: 2, pageSize: 10`) là một cache key khác.
  - Khi người dùng bấm sang trang 2 rồi bấm quay lại trang 1 trong vòng 60s, dữ liệu trang 1 được lấy ngay từ cache!
- `providesTags: ['SaleOuts']`: gắn nhãn `'SaleOuts'` cho mọi cache entry do `getSaleOuts` tạo ra. Khi có thao tác làm thay đổi đơn Sale Out và `invalidateTags(['SaleOuts'])` được dispatch, toàn bộ các trang/bộ lọc đã cache của `getSaleOuts` đều được làm mới.

#### Thành phần 4: Endpoint nâng cao với `queryFn` và `fetchWithBQ` (`FE/src/features/productMaster/api/productQueries.ts`)

Backend `/api/products` có phân trang (`pageSize` tối đa `200`), trong khi hộp thoại tạo/sửa Sale Out cần **toàn bộ danh sách sản phẩm đang hoạt động (`isActive === true`)** để đưa vào `<Autocomplete>`. Nếu chỉ dùng `query: (...) => ...` thì chỉ gọi được 1 HTTP request đơn lẻ.

Vì vậy, SmallProject dùng **`queryFn`** (cho phép viết logic async tùy ý và gộp nhiều request thành 1 cache entry duy nhất):

```ts
import { baseApi } from '../../../shared/api/baseApi'
import type { ProductListItem, ProductSearchResponse } from '../model/productTypes'

const PRODUCT_PAGE_SIZE = 200

export const productQueriesApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getActiveProducts: builder.query<ProductListItem[], void>({
      async queryFn(_arg, _api, _extraOptions, fetchWithBQ) {
        const firstPageResult = await fetchWithBQ({
          url: '/api/products',
          params: { page: 1, pageSize: PRODUCT_PAGE_SIZE },
        })

        if (firstPageResult.error) {
          return { error: firstPageResult.error }
        }

        const firstPage = firstPageResult.data as ProductSearchResponse
        const remainingPages = await Promise.all(
          Array.from({ length: Math.max(firstPage.totalPages - 1, 0) }, (_, index) =>
            fetchWithBQ({
              url: '/api/products',
              params: { page: index + 2, pageSize: PRODUCT_PAGE_SIZE },
            }),
          ),
        )

        const failedPage = remainingPages.find((page) => page.error)
        if (failedPage?.error) {
          return { error: failedPage.error }
        }

        const pages = [firstPage, ...remainingPages.map((page) => page.data as ProductSearchResponse)]
        return {
          data: pages.flatMap((page) => page.items).filter((product) => product.isActive),
        }
      },
      providesTags: ['ActiveProducts'],
    }),
  }),
})

export const { useGetActiveProductsQuery } = productQueriesApi
```

Giải thích chi tiết `queryFn`:
- Tham số thứ 4 `fetchWithBQ` chính là hàm `fetchBaseQuery` đã cấu hình sẵn `API_BASE_URL` ở `baseApi.ts`.
- Không ném exception (`throw`), mà trả về `{ data: ... }` hoặc `{ error: ... }` theo chuẩn của RTK Query.
- Kết quả cuối cùng sau khi gộp các trang và `.filter(product => product.isActive)` mới được lưu vào cache của `getActiveProducts(undefined)`, gắn tag `'ActiveProducts'`.

#### Thành phần 5: Dùng Query Hook trong Component (`SaleOutView.tsx`, `CreateSaleOutDialog.tsx`, `EditSaleOutDialog.tsx`)

##### A. Trong màn hình danh sách (`FE/src/features/saleOut/components/SaleOutView.tsx`)

```tsx
const {
  currentData: result,
  isLoading: isInitialLoading,
  isFetching,
  error: queryError,
  refetch,
} = useGetSaleOutsQuery(query)

const isLoading = isInitialLoading || isFetching
const errorMessage = queryError ? getSaleOutErrorMessage(queryError, t) : ''
```

**Phân biệt cực kỳ quan trọng trong RTK Query**:
- **`isLoading` vs `isFetching`**:
  - `isLoading` (`isInitialLoading`): chỉ là `true` ở **lần tải đầu tiên khi chưa có dữ liệu nào** cho hook này.
  - `isFetching`: là `true` ở **mọi lần đang có request chạy** (bao gồm cả lúc chuyển trang, đổi tìm kiếm, hoặc refetch ngầm sau khi invalidate tag).
  - Vì `SaleOutView` muốn hiển thị trạng thái đang tải mỗi khi chuyển trang hoặc refetch, code kết hợp `isInitialLoading || isFetching`.
- **`data` vs `currentData`**:
  - Khi đổi `query` từ trang 1 sang trang 2: trong lúc trang 2 đang tải, `data` **vẫn giữ dữ liệu của trang 1**, còn `currentData` là `undefined` cho tới khi trang 2 tải xong.
  - `SaleOutView` lấy `currentData: result` để tránh hiển thị nhầm dữ liệu trang cũ trong lúc trang mới đang tải.
- **`refetch`**: hàm cho phép component chủ động gọi lại API (ví dụ khi bấm nút Thử lại lúc mất kết nối).

##### B. Trong hộp thoại Tạo / Sửa Sale Out (`CreateSaleOutDialog.tsx` & `EditSaleOutDialog.tsx`)

```tsx
// CreateSaleOutDialog.tsx
const {
  data: products = [],
  isLoading: isLoadingProducts,
  isError: hasProductLoadError,
} = useGetActiveProductsQuery(undefined, {
  skip: !open,
  refetchOnFocus: true,
})
```
(Trong `EditSaleOutDialog.tsx` cũng tương tự với `skip: !saleOut, refetchOnFocus: true`).

Cơ chế diễn ra từng bước với cấu hình này:
1. **Khi `SaleOutView` mới mở, dialog chưa mở (`open === false`)**:
   - `skip: !open` là `true` → RTK Query **không gọi API** `/api/products`, tiết kiệm băng thông.
2. **Khi người dùng bấm mở dialog lần 1 (`open === true`)**:
   - `skip` thành `false` → hook bắt đầu subscribe `getActiveProducts(undefined)` → gọi API, cache kết quả với tag `'ActiveProducts'`.
3. **Khi đóng dialog (`open === false`), rồi mở lại ngay (trong 60s) hoặc mở `EditSaleOutDialog`**:
   - Nếu tag `'ActiveProducts'` **chưa bị invalidate**, dữ liệu có sẵn trong Redux store được trả về **ngay lập tức** (`isLoadingProducts = false`), không gọi lại API.
4. **Khi dialog đang mở và người dùng chuyển sang tab trình duyệt khác (ví dụ tab Product Master) rồi quay lại tab này**:
   - Nhờ `setupListeners(store.dispatch)` ở `store.ts` + `refetchOnFocus: true` ở hook, RTK Query tự động refetch lại danh sách sản phẩm hoạt động mới nhất!

#### Thành phần 6: Làm mới Cache bằng `baseApi.util.invalidateTags`

Trong SmallProject, các thao tác **ghi** (Create, Update, Delete, Import) được gọi qua hàm async thuần trong `productsApi.ts` và `saleOutApi.ts` (dùng `httpClient.ts`), sau khi thành công thì component chủ động báo cho RTK Query biết tag nào đã cũ:

**1. Ở `FE/src/features/productMaster/components/ProductMasterView.tsx`** (sau khi Tạo, Sửa, Xóa hoặc Import sản phẩm thành công):
```tsx
dispatch(baseApi.util.invalidateTags(['ActiveProducts']))
```
- Nếu bên Sale Out có cache `getActiveProducts`, tag `'ActiveProducts'` bị đánh dấu cũ → lần tới mở `CreateSaleOutDialog` hoặc `EditSaleOutDialog` (hoặc nếu đang mở) sẽ tự động tải lại danh sách sản phẩm mới nhất!

**2. Ở `FE/src/features/saleOut/components/SaleOutView.tsx`** (sau khi Tạo, Sửa, Xóa hoặc Import đơn Sale Out thành công):
```tsx
setQuery((currentQuery) => ({ ...currentQuery, page: 1 }))
dispatch(baseApi.util.invalidateTags(['SaleOuts']))
```
- Vì `SaleOutView` đang mount và đang subscribe `useGetSaleOutsQuery(query)`, ngay khi `invalidateTags(['SaleOuts'])` được dispatch, RTK Query **tự động refetch** lại danh sách đơn hàng mà component không cần gọi `fetchSaleOuts` thủ công.

> **Mở rộng kiến thức (`builder.mutation`)**: Trong dự án hiện tại, các thao tác ghi dùng hàm rời (`createSaleOut`, `updateSaleOutOrder`, `deleteSaleOutOrder`) rồi gọi `dispatch(baseApi.util.invalidateTags(...))` ở `View`. Nếu về sau chuyển các thao tác ghi sang `builder.mutation` của RTK Query, bạn có thể khai báo `invalidatesTags: ['SaleOuts']` trực tiếp ngay trên mutation endpoint để RTK Query tự động invalidate tag khi mutation thành công mà không cần `dispatch` thủ công trong component.

---

## 5. Cách SmallProject kết hợp 3 lớp State trong thực tế

Hãy xem một luồng nghiệp vụ hoàn chỉnh trong **SmallProject**: *"Người dùng xóa một sản phẩm ở Product Master, rồi sang trang Sale Out mở hộp thoại tạo đơn"*.

```text
1. [ProductMasterView]
   - Các ô lọc (searchField, searchValue), trạng thái dialog (isCreateOpen, editingProduct)
     nằm trong React useState cục bộ.
   - Bấm Xóa sản phẩm → gọi deleteProduct(product.id) (productsApi.ts).
   - Xóa thành công → gọi:
       dispatch(baseApi.util.invalidateTags(['ActiveProducts']))
     → Cache của getActiveProducts trong state.baseApi bị đánh dấu stale / xóa.

2. [App.tsx]
   - Bấm chuyển sang màn Sale Out → setActivePage('saleOut') (React useState) + pushState URL.

3. [SaleOutView]
   - Đọc URL vào state `query` (useState).
   - Gọi `useGetSaleOutsQuery(query)` → RTK Query kiểm tra state.baseApi.queries:
     chưa có cache cho `query` này thì fetch `/api/sale-outs` và lưu cache với tag 'SaleOuts'.

4. [CreateSaleOutDialog]
   - Bấm nút Thêm → `isCreateDialogOpen` (useState) thành `true`.
   - `useGetActiveProductsQuery(undefined, { skip: !open, refetchOnFocus: true })` kích hoạt:
     vì tag 'ActiveProducts' đã bị invalidate ở bước 1, RTK Query tự tải lại danh sách sản phẩm mới
     (sản phẩm vừa xóa không còn xuất hiện trong dropdown).
   - Người dùng gõ Customer PO No, thêm dòng sản phẩm: toàn bộ dữ liệu nháp (header, items, errors)
     nằm gọn trong React useState của dialog; đóng dialog là tự hủy/reset.
   - Tạo đơn thành công → `onSuccess()` gọi `dispatch(baseApi.util.invalidateTags(['SaleOuts']))`
     → `SaleOutView` tự refetch bảng đơn hàng.
```

> **Lưu ý về thực trạng codebase**: Hiện tại trong `ProductMasterView.tsx`, bảng danh sách sản phẩm chính vẫn đang dùng `useState` + `useEffect` + `fetchProducts` + `retryCount` (và chỉ dùng RTK Query cho `getActiveProducts` chia sẻ sang Sale Out). Khi xem 2 file `ProductMasterView.tsx` và `SaleOutView.tsx` cạnh nhau, bạn sẽ thấy rõ sự khác biệt giữa cách quản lý API thủ công và cách dùng RTK Query.

---

## 6. Bảng so sánh & Quy tắc lựa chọn nhanh

### 6.1. So sánh 3 cơ chế State

| Tiêu chí | React `useState` | Redux Slice (`createSlice` + `createAsyncThunk`) | RTK Query (`createApi` / `injectEndpoints`) |
|---|---|---|---|
| **Mục đích chính** | State UI cục bộ, ngắn hạn | State client toàn cục / logic client phức tạp | Dữ liệu từ Server API (Server State) |
| **Nơi lưu trữ** | Trong component instance (mất khi unmount) | Redux Store (`store.getState().<slice>`) | Redux Store (`store.getState().baseApi`) |
| **Chia sẻ nhiều màn hình** | Khó (phải nâng state lên cha / truyền props) | Rất dễ (`useAppSelector`) | Rất dễ (cùng gọi hook query, tự dùng chung cache) |
| **Cache & Deduplicate API** | Không có (tự code bằng `useEffect`) | Không có sẵn (phải tự viết logic kiểm tra) | Có sẵn tự động theo `endpoint(args)` |
| **Làm mới dữ liệu cũ** | Tự quản lý biến `retryCount` / gọi lại hàm | Tự dispatch thunk lại | Tự động qua `providesTags` / `invalidateTags` / `refetchOnFocus` |
| **Ví dụ trong SmallProject** | `App.tsx`, `CreateProductDialog.tsx`, `CreateSaleOutDialog.tsx`, form search trong `SaleOutView.tsx` | `apiHealthSlice.ts` + `ApiHealthStatus.tsx` | `baseApi.ts`, `productQueries.ts`, `saleOutQueries.ts`, `SaleOutView.tsx` |

### 6.2. Quy tắc chọn nhanh khi thêm tính năng mới (theo `FE/ARCHITECTURE.md`)

1. **Dữ liệu chỉ phục vụ giao diện trong 1 component / dialog** (ô nhập liệu, lỗi validate, đóng/mở modal, dòng đang expand, trang/bộ lọc hiện tại)?
   → Dùng **React `useState`** ngay trong component đó. Không đưa input form vào Redux.
2. **Dữ liệu lấy từ API backend, cần cache hoặc dùng ở nhiều nơi / tự làm mới sau thao tác ghi**?
   → Dùng **RTK Query**: khai báo endpoint trong `src/features/<feature>/api/*Queries.ts` bằng `baseApi.injectEndpoints`, khai báo `tagTypes` ở `src/shared/api/baseApi.ts` nếu cần tag mới, và import file queries vào `src/app/store.ts`.
3. **Dữ liệu client toàn cục không phải danh sách/tài nguyên API cache chuẩn** (ví dụ trạng thái kết nối hệ thống, cấu hình phiên làm việc dùng xuyên nhiều trang)?
   → Dùng **Redux Slice** trong `src/features/<feature>/model/*Slice.ts` và đăng ký reducer vào `src/app/store.ts`.
