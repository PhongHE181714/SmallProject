import { useEffect, useState, type FormEvent } from 'react'
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
import { useGetActiveProductsQuery } from '../../productMaster/api/productQueries'
import { fetchSaleOutOrder, updateSaleOutOrder } from '../api/saleOutApi'
import type { SaleOutListItem, SaleOutOrderListItem, UpdateSaleOutOrderRequest } from '../model/saleOutTypes'
import SaleOutItemsEditor, { type SaleOutItemDraft, type SaleOutItemDraftErrors, type SaleOutItemEditableField } from './SaleOutItemsEditor'

interface EditSaleOutDialogProps {
  saleOut: SaleOutOrderListItem | null
  onClose: () => void
  onSuccess: () => void
}

export default function EditSaleOutDialog({ saleOut, onClose, onSuccess }: EditSaleOutDialogProps) {
  const { t } = useTranslation()
  const {
    data: products = [],
    isLoading: isLoadingProducts,
    isError: hasProductLoadError,
  } = useGetActiveProductsQuery(undefined, {
    skip: !saleOut,
    refetchOnFocus: true,
  })
  const [items, setItems] = useState<SaleOutItemDraft[]>([])
  const [itemErrors, setItemErrors] = useState<Record<string, SaleOutItemDraftErrors>>({})
  const [orderItems, setOrderItems] = useState<SaleOutListItem[]>([])
  const [isLoadingOrder, setIsLoadingOrder] = useState(false)
  const [loadError, setLoadError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submissionError, setSubmissionError] = useState('')
  const isLoading = isLoadingOrder || isLoadingProducts
  const displayedLoadError = loadError || (hasProductLoadError ? t('saleOut.editDialog.loadError') : '')

  useEffect(() => {
    if (!saleOut) return

    const controller = new AbortController()
    setItems([])
    setItemErrors({})
    setOrderItems([])
    setLoadError('')
    setSubmissionError('')
    setIsLoadingOrder(true)

    const orderPromise = saleOut.saleOutNo
      ? fetchSaleOutOrder(saleOut.saleOutNo, controller.signal)
      : Promise.resolve(saleOut.products)
    void orderPromise
      .then((loadedOrder) => {
        if (controller.signal.aborted) return
        setOrderItems(loadedOrder)
        setItems(loadedOrder.map((item) => ({
          key: item.id,
          id: item.id,
          productId: item.productId ?? '',
          quantity: item.quantity == null ? '' : String(item.quantity),
          quantityPerBox: item.quantityPerBox == null ? '' : String(item.quantityPerBox),
          price: item.price == null ? '' : String(item.price),
        })))
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setLoadError(error instanceof Error ? error.message : t('saleOut.editDialog.loadError'))
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoadingOrder(false)
      })

    return () => controller.abort()
  }, [saleOut, t])

  const handleItemChange = (key: string, field: SaleOutItemEditableField, value: string) => {
    setItems((current) => current.map((item) => item.key === key ? { ...item, [field]: value } : item))
    setItemErrors((current) => ({ ...current, [key]: { ...current[key], [field]: undefined } }))
    setSubmissionError('')
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!saleOut?.saleOutNo || isSubmitting || items.length === 0) return

    const nextErrors: Record<string, SaleOutItemDraftErrors> = {}
    const selectedIds = items.map((item) => item.productId).filter(Boolean)
    const hasDuplicateProducts = new Set(selectedIds).size !== selectedIds.length
    for (const item of items) {
      const errors: SaleOutItemDraftErrors = {}
      const quantity = Number(item.quantity)
      const quantityPerBox = Number(item.quantityPerBox)
      const price = Number(item.price)
      if (!products.some((product) => product.id === item.productId)) errors.productId = t('saleOut.createDialog.validation.productRequired')
      if (!/^\d+$/.test(item.quantity) || !Number.isSafeInteger(quantity) || quantity < 1) errors.quantity = t('saleOut.editDialog.validation.quantityInvalid')
      if (!/^\d+$/.test(item.quantityPerBox) || !Number.isSafeInteger(quantityPerBox) || quantityPerBox < 1) errors.quantityPerBox = t('saleOut.editDialog.validation.quantityPerBoxInvalid')
      if (!/^\d+$/.test(item.price) || !Number.isSafeInteger(price) || price < 0) errors.price = t('saleOut.editDialog.validation.priceInvalid')
      if (hasDuplicateProducts && item.productId) errors.productId = t('saleOut.createDialog.validation.duplicateProduct')
      if (Object.keys(errors).length) nextErrors[item.key] = errors
    }
    setItemErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const payload: UpdateSaleOutOrderRequest = {
      items: items.map((item) => ({
        ...(item.id ? { id: item.id } : {}),
        productId: item.productId,
        quantity: Number(item.quantity),
        quantityPerBox: Number(item.quantityPerBox),
        price: Number(item.price),
      })),
    }

    setIsSubmitting(true)
    try {
      await updateSaleOutOrder(saleOut.saleOutNo, payload)
      onSuccess()
      onClose()
      void Swal.fire({
        icon: 'success',
        title: t('saleOut.editDialog.successTitle'),
        text: t('saleOut.editDialog.successMessage'),
        timer: 2000,
        showConfirmButton: false,
      })
    } catch (error: unknown) {
      setSubmissionError(error instanceof ApiRequestError && error.detail
        ? error.detail
        : error instanceof Error ? error.message : t('saleOut.editDialog.unexpectedError'))
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    if (!isSubmitting) onClose()
  }

  return (
    <Dialog open={Boolean(saleOut)} onClose={handleClose} maxWidth="xl" fullWidth slotProps={{ paper: { className: 'product-dialog sale-out-order-dialog' } }}>
      <DialogTitle className="product-dialog__title">
        <Box>
          <Typography component="h2" className="product-dialog__title-text">{t('saleOut.editDialog.title')}</Typography>
          <Typography className="product-dialog__subtitle-text">{t('saleOut.editDialog.subtitle')}</Typography>
        </Box>
        <IconButton size="small" onClick={handleClose} disabled={isSubmitting} className="product-dialog__close-btn" aria-label={t('saleOut.editDialog.actions.close')}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <form onSubmit={handleSubmit} noValidate>
        <DialogContent className="product-dialog__content">
          {submissionError && <Alert severity="error" sx={{ mb: 2 }}>{submissionError}</Alert>}
          {displayedLoadError && <Alert severity="error" sx={{ mb: 2 }}>{displayedLoadError}</Alert>}
          <div className="sale-out-order-header sale-out-order-header--readonly">
            <ReadOnlyHeader label={t('saleOut.fields.saleOutNo')} value={saleOut?.saleOutNo} />
            <ReadOnlyHeader label={t('saleOut.fields.customerPoNo')} value={orderItems[0]?.customerPoNo ?? saleOut?.customerPoNo} />
            <ReadOnlyHeader label={t('saleOut.fields.customerName')} value={orderItems[0]?.customerName ?? saleOut?.customerName} />
            <ReadOnlyHeader label={t('saleOut.fields.orderDate')} value={formatDate(orderItems[0]?.orderDate ?? saleOut?.orderDate)} />
          </div>
          {isLoading
            ? <div className="sale-out-items-editor__loading"><CircularProgress size={22} />{t('saleOut.loadingRows')}</div>
            : <SaleOutItemsEditor
              items={items}
              products={products}
              errors={itemErrors}
              disabled={isSubmitting}
              loadingProducts={false}
              onChange={handleItemChange}
              onAdd={() => setItems((current) => [...current, createDraftItem()])}
              onRemove={(key) => {
                setItems((current) => current.length > 1 ? current.filter((item) => item.key !== key) : current)
                setItemErrors((current) => Object.fromEntries(Object.entries(current).filter(([itemKey]) => itemKey !== key)))
              }}
            />}
        </DialogContent>
        <DialogActions className="product-dialog__actions">
          <Button type="button" variant="outlined" onClick={handleClose} disabled={isSubmitting} className="product-button product-button--quiet">{t('saleOut.editDialog.actions.close')}</Button>
          <Button type="submit" variant="contained" disabled={isSubmitting || isLoading || Boolean(displayedLoadError) || items.length === 0 || !saleOut?.saleOutNo} className="product-button product-button--primary" startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}>
            {isSubmitting ? t('saleOut.editDialog.actions.saving') : t('saleOut.editDialog.actions.save')}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}

function ReadOnlyHeader({ label, value }: { label: string; value?: string | null }) {
  return <div className="product-dialog__field"><label className="product-dialog__label">{label}</label><TextField fullWidth size="small" value={value ?? ''} disabled /></div>
}

function createDraftItem(): SaleOutItemDraft {
  return { key: `${Date.now()}-${Math.random()}`, productId: '', quantity: '', quantityPerBox: '', price: '' }
}

function formatDate(value?: string | null): string {
  if (!value) return ''
  const parts = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  return parts ? `${parts[3]}/${parts[2]}/${parts[1]}` : value
}

