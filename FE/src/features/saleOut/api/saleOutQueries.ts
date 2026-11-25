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
