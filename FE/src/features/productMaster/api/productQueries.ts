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
