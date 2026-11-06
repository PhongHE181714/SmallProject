import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import CloseIcon from '@mui/icons-material/Close'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  TextField,
  Typography,
} from '@mui/material'
import Swal from 'sweetalert2'
import { ApiRequestError } from '../../../shared/api/httpClient'
import { fetchProducts } from '../../productMaster/api/productsApi'
import type { ProductListItem, ProductSearchResponse } from '../../productMaster/model/productTypes'
import { createSaleOut } from '../api/saleOutApi'
import type { CreateSaleOutRequest } from '../model/saleOutTypes'
import SaleOutItemsEditor, { type SaleOutItemDraft, type SaleOutItemDraftErrors, type SaleOutItemEditableField } from './SaleOutItemsEditor'

const PRODUCT_PAGE_SIZE = 200
const CUSTOMER_PO_NO_MAX_LENGTH = 50
const CUSTOMER_NAME_MAX_LENGTH = 200

interface CreateSaleOutDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

type HeaderForm = { customerPoNo: string; orderDate: string; customerName: string }
type HeaderErrors = Partial<Record<keyof HeaderForm, string>>

const INITIAL_HEADER: HeaderForm = { customerPoNo: '', orderDate: '', customerName: '' }

export default function CreateSaleOutDialog({ open, onClose, onSuccess }: CreateSaleOutDialogProps) {
  const { t } = useTranslation()
  const [header, setHeader] = useState<HeaderForm>(INITIAL_HEADER)
  const [headerErrors, setHeaderErrors] = useState<HeaderErrors>({})
  const [items, setItems] = useState<SaleOutItemDraft[]>([createDraftItem()])
  const [itemErrors, setItemErrors] = useState<Record<string, SaleOutItemDraftErrors>>({})
  const [products, setProducts] = useState<ProductListItem[]>([])
  const [isLoadingProducts, setIsLoadingProducts] = useState(false)
  const [productLoadError, setProductLoadError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submissionError, setSubmissionError] = useState('')
  const [isSubmissionConflict, setIsSubmissionConflict] = useState(false)

  useEffect(() => {
    if (!open) return

    setHeader(INITIAL_HEADER)
    setHeaderErrors({})
    setItems([createDraftItem()])
    setItemErrors({})
    setIsSubmitting(false)
    setSubmissionError('')
    setIsSubmissionConflict(false)
    setProducts([])
    setProductLoadError('')

    const controller = new AbortController()
    setIsLoadingProducts(true)
    void fetchActiveProducts(controller.signal)
      .then(setProducts)
      .catch(() => {
        if (!controller.signal.aborted) setProductLoadError(t('saleOut.createDialog.productLoadError'))
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoadingProducts(false)
      })

    return () => controller.abort()
  }, [open, t])

  const handleHeaderChange = (field: keyof HeaderForm, value: string) => {
    setHeader((current) => ({ ...current, [field]: value }))
    setHeaderErrors((current) => ({ ...current, [field]: undefined }))
    setSubmissionError('')
  }

  const handleItemChange = (key: string, field: SaleOutItemEditableField, value: string) => {
    setItems((current) => current.map((item) => item.key === key ? { ...item, [field]: value } : item))
    setItemErrors((current) => ({ ...current, [key]: { ...current[key], [field]: undefined } }))
    setSubmissionError('')
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (isSubmitting) return

    const nextHeaderErrors: HeaderErrors = {}
    const customerPoNo = header.customerPoNo.trim()
    const customerName = header.customerName.trim()
    if (!customerPoNo) nextHeaderErrors.customerPoNo = t('saleOut.createDialog.validation.customerPoNoRequired')
    else if (customerPoNo.length > CUSTOMER_PO_NO_MAX_LENGTH) nextHeaderErrors.customerPoNo = t('saleOut.createDialog.validation.customerPoNoTooLong')
    if (!header.orderDate) nextHeaderErrors.orderDate = t('saleOut.createDialog.validation.orderDateRequired')
    if (!customerName) nextHeaderErrors.customerName = t('saleOut.createDialog.validation.customerNameRequired')
    else if (customerName.length > CUSTOMER_NAME_MAX_LENGTH) nextHeaderErrors.customerName = t('saleOut.createDialog.validation.customerNameTooLong')

    const nextItemErrors: Record<string, SaleOutItemDraftErrors> = {}
    const selectedIds = items.map((item) => item.productId).filter(Boolean)
    const hasDuplicateProducts = new Set(selectedIds).size !== selectedIds.length
    for (const item of items) {
      const errors: SaleOutItemDraftErrors = {}
      const quantity = Number(item.quantity)
      const quantityPerBox = Number(item.quantityPerBox)
      const price = Number(item.price)
      if (!products.some((product) => product.id === item.productId)) errors.productId = t('saleOut.createDialog.validation.productRequired')
      if (!/^\d+$/.test(item.quantity) || !Number.isSafeInteger(quantity) || quantity < 1) errors.quantity = t('saleOut.createDialog.validation.quantityInvalid')
      if (!/^\d+$/.test(item.quantityPerBox) || !Number.isSafeInteger(quantityPerBox) || quantityPerBox < 1) errors.quantityPerBox = t('saleOut.createDialog.validation.quantityPerBoxInvalid')
      if (!/^\d+$/.test(item.price) || !Number.isSafeInteger(price) || price < 0) errors.price = t('saleOut.createDialog.validation.priceInvalid')
      if (hasDuplicateProducts && item.productId) errors.productId = t('saleOut.createDialog.validation.duplicateProduct')
      if (Object.keys(errors).length) nextItemErrors[item.key] = errors
    }

    setHeaderErrors(nextHeaderErrors)
    setItemErrors(nextItemErrors)
    if (Object.keys(nextHeaderErrors).length || Object.keys(nextItemErrors).length) return

    const payload: CreateSaleOutRequest = {
      customerPoNo,
      orderDate: header.orderDate,
      customerName,
      items: items.map((item) => ({
        productId: item.productId,
        quantity: Number(item.quantity),
        quantityPerBox: Number(item.quantityPerBox),
        price: Number(item.price),
      })),
    }

    setIsSubmitting(true)
    try {
      const createdOrder = await createSaleOut(payload)
      onSuccess()
      onClose()
      void Swal.fire({
        icon: 'success',
        title: t('saleOut.createDialog.successTitle'),
        text: t('saleOut.createDialog.successMessage', { saleOutNo: createdOrder.saleOutNo }),
        timer: 2000,
        showConfirmButton: false,
      })
    } catch (error: unknown) {
      const message = error instanceof ApiRequestError && error.detail
        ? error.detail
        : error instanceof Error ? error.message : t('saleOut.createDialog.unexpectedError')
      setSubmissionError(message)
      setIsSubmissionConflict(error instanceof ApiRequestError && error.status === 409)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    if (!isSubmitting) onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="xl" fullWidth slotProps={{ paper: { className: 'product-dialog sale-out-order-dialog' } }}>
      <DialogTitle className="product-dialog__title">
        <Box>
          <Typography component="h2" className="product-dialog__title-text">{t('saleOut.createDialog.title')}</Typography>
          <Typography className="product-dialog__subtitle-text">{t('saleOut.createDialog.subtitle')}</Typography>
        </Box>
        <IconButton size="small" onClick={handleClose} disabled={isSubmitting} className="product-dialog__close-btn" aria-label={t('saleOut.createDialog.actions.close')}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <form onSubmit={handleSubmit} noValidate>
        <DialogContent className="product-dialog__content">
          {submissionError && <Alert severity={isSubmissionConflict ? 'warning' : 'error'} sx={{ mb: 2 }}>{submissionError}</Alert>}
          {productLoadError && <Alert severity="error" sx={{ mb: 2 }}>{productLoadError}</Alert>}
          <div className="sale-out-order-header">
            <OrderHeaderField label={t('saleOut.fields.saleOutNo')}><TextField fullWidth size="small" value={t('saleOut.createDialog.saleOutNoAutomatic')} disabled /></OrderHeaderField>
            <OrderHeaderField label={t('saleOut.fields.customerPoNo')} required>
              <TextField fullWidth size="small" value={header.customerPoNo} onChange={(event) => handleHeaderChange('customerPoNo', event.target.value)} error={Boolean(headerErrors.customerPoNo)} helperText={headerErrors.customerPoNo} disabled={isSubmitting} slotProps={{ htmlInput: { maxLength: CUSTOMER_PO_NO_MAX_LENGTH } }} />
            </OrderHeaderField>
            <OrderHeaderField label={t('saleOut.fields.customerName')} required>
              <TextField fullWidth size="small" value={header.customerName} onChange={(event) => handleHeaderChange('customerName', event.target.value)} error={Boolean(headerErrors.customerName)} helperText={headerErrors.customerName} disabled={isSubmitting} slotProps={{ htmlInput: { maxLength: CUSTOMER_NAME_MAX_LENGTH } }} />
            </OrderHeaderField>
            <OrderHeaderField label={t('saleOut.fields.orderDate')} required>
              <TextField fullWidth size="small" type="date" value={header.orderDate} onChange={(event) => handleHeaderChange('orderDate', event.target.value)} error={Boolean(headerErrors.orderDate)} helperText={headerErrors.orderDate} disabled={isSubmitting} />
            </OrderHeaderField>
          </div>
          <SaleOutItemsEditor
            items={items}
            products={products}
            errors={itemErrors}
            disabled={isSubmitting}
            loadingProducts={isLoadingProducts}
            onChange={handleItemChange}
            onAdd={() => setItems((current) => [...current, createDraftItem()])}
            onRemove={(key) => {
              setItems((current) => current.length > 1 ? current.filter((item) => item.key !== key) : current)
              setItemErrors((current) => Object.fromEntries(Object.entries(current).filter(([itemKey]) => itemKey !== key)))
            }}
          />
        </DialogContent>
        <DialogActions className="product-dialog__actions">
          <Button type="button" variant="outlined" onClick={handleClose} disabled={isSubmitting} className="product-button product-button--quiet">{t('saleOut.createDialog.actions.close')}</Button>
          <Button type="submit" variant="contained" disabled={isSubmitting || isLoadingProducts || products.length === 0 || Boolean(productLoadError)} className="product-button product-button--primary" startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}>
            {isSubmitting ? t('saleOut.createDialog.actions.saving') : t('saleOut.createDialog.actions.save')}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}

function OrderHeaderField({ label, required, children }: { label: string; required?: boolean; children: ReactNode }) {
  return <div className="product-dialog__field"><label className="product-dialog__label">{label}{required && <span className="product-dialog__required">*</span>}</label>{children}</div>
}

function createDraftItem(): SaleOutItemDraft {
  return { key: `${Date.now()}-${Math.random()}`, productId: '', quantity: '', quantityPerBox: '', price: '' }
}

async function fetchActiveProducts(signal: AbortSignal): Promise<ProductListItem[]> {
  const firstPage = await fetchProductsPage(1, signal)
  const remainingPages = await Promise.all(Array.from({ length: Math.max(firstPage.totalPages - 1, 0) }, (_, index) => fetchProductsPage(index + 2, signal)))
  return [firstPage, ...remainingPages].flatMap((page) => page.items).filter((product) => product.isActive)
}

function fetchProductsPage(page: number, signal: AbortSignal): Promise<ProductSearchResponse> {
  return fetchProducts({ page, pageSize: PRODUCT_PAGE_SIZE, searchField: 'ProductCode', searchValue: '' }, signal)
}
