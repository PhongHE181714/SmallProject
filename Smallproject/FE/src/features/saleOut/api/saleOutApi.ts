import { ApiRequestError, API_BASE_URL, del, get, getBlob, post, postForm, put } from '../../../shared/api/httpClient'
import type {
  CreateSaleOutRequest,
  SaleOutListItem,
  SaleOutCreateResponse,
  SaleOutSearchField,
  SaleOutSearchResponse,
  UpdateSaleOutOrderRequest,
  SaleOutImportResponse,
  SaleOutPrintOptionsResponse,
} from '../model/saleOutTypes'

export type SaleOutSearchParams = {
  page: number
  pageSize: number
  searchField: SaleOutSearchField
  searchValue: string
}

export function fetchSaleOuts(params: SaleOutSearchParams, signal: AbortSignal) {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  })

  if (params.searchValue.trim()) {
    query.set('searchField', params.searchField)
    query.set('searchValue', params.searchValue.trim())
  }

  return get<SaleOutSearchResponse>(`/api/sale-outs?${query.toString()}`, signal)
}

export function fetchSaleOutPrintOptions(searchValue: string, signal: AbortSignal) {
  const query = new URLSearchParams({ page: '1', pageSize: '100' })
  if (searchValue.trim()) query.set('searchValue', searchValue.trim())
  return get<SaleOutPrintOptionsResponse>(`/api/sale-outs/print-options?${query.toString()}`, signal)
}

export function downloadSaleOutPrintPdf(saleOutNo: string) {
  return getBlob(`/api/sale-outs/print/${encodeURIComponent(saleOutNo)}/pdf`)
}

export function createSaleOut(payload: CreateSaleOutRequest) {
  return post<SaleOutCreateResponse, CreateSaleOutRequest>('/api/sale-outs', payload)
}

export function fetchSaleOutOrder(saleOutNo: string, signal: AbortSignal) {
  return get<SaleOutListItem[]>(`/api/sale-outs/order/${encodeURIComponent(saleOutNo)}`, signal)
}

export function updateSaleOutOrder(saleOutNo: string, payload: UpdateSaleOutOrderRequest) {
  return put<void, UpdateSaleOutOrderRequest>(`/api/sale-outs/order/${encodeURIComponent(saleOutNo)}`, payload)
}

export function deleteSaleOutOrder(saleOutNo: string) {
  return del<void>(`/api/sale-outs/order/${encodeURIComponent(saleOutNo)}`)
}

export function importSaleOuts(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return postForm<SaleOutImportResponse>('/api/sale-outs/import', formData)
}

export async function downloadSaleOutTemplate(): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}/api/sale-outs/template`)
  if (!response.ok) {
    throw new ApiRequestError(response.status)
  }

  return response.blob()
}
