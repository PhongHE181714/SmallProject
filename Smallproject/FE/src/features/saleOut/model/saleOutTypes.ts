export const SALE_OUT_SEARCH_FIELDS = [
  { value: 'CustomerPoNo', labelKey: 'saleOut.fields.customerPoNo' },
  { value: 'SaleOutNo', labelKey: 'saleOut.fields.saleOutNo' },
  { value: 'OrderDate', labelKey: 'saleOut.fields.orderDate' },
  { value: 'CustomerName', labelKey: 'saleOut.fields.customerName' },
  { value: 'ProductCode', labelKey: 'saleOut.fields.productCode' },
  { value: 'ProductName', labelKey: 'saleOut.fields.productName' },
] as const

export type SaleOutSearchField = (typeof SALE_OUT_SEARCH_FIELDS)[number]['value']

export type SaleOutListItem = {
  id: string
  saleOutNo: string | null
  customerPoNo: string | null
  orderDate: string | null
  customerName: string | null
  productId: string | null
  productCode: string | null
  productName: string | null
  unit: string | null
  quantity: number | null
  price: number | null
  amount: number | null
  quantityPerBox: number | null
  boxQuantity: number | null
  isActive: boolean
}

export type SaleOutSearchResponse = {
  items: SaleOutOrderListItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type SaleOutPrintOption = {
  saleOutNo: string | null
  customerPoNo: string | null
  orderDate: string | null
  customerName: string | null
  productCount: number
  totalAmount: number
}

export type SaleOutPrintOptionsResponse = {
  items: SaleOutPrintOption[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type SaleOutOrderListItem = {
  saleOutNo: string | null
  customerPoNo: string | null
  orderDate: string | null
  customerName: string | null
  productCount: number
  totalAmount: number
  products: SaleOutListItem[]
}

export type CreateSaleOutRequest = {
  customerPoNo: string
  orderDate: string
  customerName: string
  items: Array<{
    productId: string
    quantity: number
    quantityPerBox: number
    price: number
  }>
}

export type SaleOutCreateResponse = {
  saleOutNo: string
  customerPoNo: string
  orderDate: string
  customerName: string
  itemCount: number
}

export type UpdateSaleOutOrderRequest = {
  items: Array<{
    id?: string
    productId: string
    quantity: number
    quantityPerBox: number
    price: number
  }>
}

export type SaleOutImportResponse = {
  importedCount: number
}
