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
