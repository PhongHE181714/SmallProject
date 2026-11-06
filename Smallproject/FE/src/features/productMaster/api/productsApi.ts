import { ApiRequestError, del, get, post, postForm, put } from '../../../shared/api/httpClient'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string ?? ''
import type {
  CreateProductRequest,
  ProductCreateResponse,
  ProductImportResponse,
  ProductSearchField,
  ProductSearchResponse,
  UpdateProductRequest,
} from '../model/productTypes'

export type ProductSearchParams = {
  page: number
  pageSize: number
  searchField: ProductSearchField
  searchValue: string
}

export function fetchProducts(params: ProductSearchParams, signal: AbortSignal) {
  const query = new URLSearchParams({
    page: String(params.page),
    pageSize: String(params.pageSize),
  })

  if (params.searchValue.trim()) {
    query.set('searchField', params.searchField)
    query.set('searchValue', params.searchValue.trim())
  }

  return get<ProductSearchResponse>(`/api/products?${query.toString()}`, signal)
}

export function createProduct(payload: CreateProductRequest, signal?: AbortSignal) {
  return post<ProductCreateResponse, CreateProductRequest>('/api/products', payload, signal)
}

export function updateProduct(
  id: string,
  payload: UpdateProductRequest,
  signal?: AbortSignal,
) {
  return put<ProductCreateResponse, UpdateProductRequest>(`/api/products/${id}`, payload, signal)
}

export function deleteProduct(id: string, signal?: AbortSignal) {
  return del(`/api/products/${id}`, signal)
}

export function importProducts(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return postForm<ProductImportResponse>('/api/products/import', formData)
}

export async function downloadProductTemplate(): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}/api/products/template`)
  if (!response.ok) {
    throw new ApiRequestError(response.status)
  }
  return response.blob()
}
