export const PRODUCT_SEARCH_FIELDS = [
  { value: 'ProductCode', labelKey: 'productMaster.fields.productCode' },
  { value: 'ProductName', labelKey: 'productMaster.fields.productName' },
  { value: 'Unit', labelKey: 'productMaster.fields.unit' },
  { value: 'Specification', labelKey: 'productMaster.fields.specification' },
  { value: 'QuantityPerBox', labelKey: 'productMaster.fields.quantityPerBox' },
  { value: 'ProductWeight', labelKey: 'productMaster.fields.productWeight' },
] as const

export type ProductSearchField = (typeof PRODUCT_SEARCH_FIELDS)[number]['value']

export type ProductListItem = {
  id: string
  productCode: string | null
  productName: string | null
  unit: string | null
  specification: string | null
  quantityPerBox: number | null
  productWeight: number | null
  isActive: boolean
}

export type ProductSearchResponse = {
  items: ProductListItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface CreateProductRequest {
  productCode: string
  productName: string
  unit: string
  specification: string
  quantityPerBox: number
  productWeight: number
}

export interface UpdateProductRequest {
  productCode: string
  productName: string
  unit: string
  specification: string
  quantityPerBox: number
  productWeight: number
  isActive: boolean
}

export interface ProductCreateResponse {
  id: string
  productCode: string | null
  productName: string | null
  unit: string | null
  specification: string | null
  quantityPerBox: number | null
  productWeight: number | null
  isActive: boolean
}

export interface ProductImportResponse {
  importedCount: number
}
