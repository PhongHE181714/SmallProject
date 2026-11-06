import { useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import CloseIcon from '@mui/icons-material/Close'
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  MenuItem,
  Select,
  TextField,
  Typography,
} from '@mui/material'
import Swal from 'sweetalert2'
import { ApiRequestError } from '../../../shared/api/httpClient'
import { updateProduct } from '../api/productsApi'
import type { ProductListItem, UpdateProductRequest } from '../model/productTypes'

const MAX_CODE_LENGTH = 50
const MAX_NAME_LENGTH = 200
const MAX_UNIT_LENGTH = 50
const MAX_SPEC_LENGTH = 200
const MAX_DECIMAL_SCALE = 3

export interface UpdateProductDialogProps {
  open: boolean
  product: ProductListItem | null
  onClose: () => void
  onSuccess: () => void
}

interface FormState {
  productCode: string
  productName: string
  unit: string
  specification: string
  quantityPerBox: string
  productWeight: string
  isActive: boolean
}

interface FormErrors {
  productCode?: string
  productName?: string
  unit?: string
  specification?: string
  quantityPerBox?: string
  productWeight?: string
}

const INITIAL_FORM_STATE: FormState = {
  productCode: '',
  productName: '',
  unit: '',
  specification: '',
  quantityPerBox: '',
  productWeight: '',
  isActive: true,
}

export default function UpdateProductDialog({
  open,
  product,
  onClose,
  onSuccess,
}: UpdateProductDialogProps) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(INITIAL_FORM_STATE)
  const [errors, setErrors] = useState<FormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (open && product) {
      setForm({
        productCode: product.productCode ?? '',
        productName: product.productName ?? '',
        unit: product.unit ?? '',
        specification: product.specification ?? '',
        quantityPerBox: product.quantityPerBox !== null && product.quantityPerBox !== undefined
          ? String(product.quantityPerBox)
          : '',
        productWeight: product.productWeight !== null && product.productWeight !== undefined
          ? String(product.productWeight)
          : '',
        isActive: product.isActive ?? true,
      })
      setErrors({})
      setIsSubmitting(false)
    }
  }, [open, product])

  const handleFieldChange = <K extends keyof FormState>(field: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [field]: value }))
    if (errors[field as keyof FormErrors]) {
      setErrors((current) => ({ ...current, [field]: undefined }))
    }
  }

  const validateForm = (): { isValid: boolean; payload?: UpdateProductRequest } => {
    const newErrors: FormErrors = {}
    const trimmedCode = form.productCode.trim()
    const trimmedName = form.productName.trim()
    const trimmedUnit = form.unit.trim()
    const trimmedSpec = form.specification.trim()
    const rawQty = form.quantityPerBox.trim()
    const rawWeight = form.productWeight.trim()

    // 1. Mã sản phẩm
    if (!trimmedCode) {
      newErrors.productCode = t('productMaster.createDialog.validation.productCodeRequired')
    } else if (trimmedCode.length > MAX_CODE_LENGTH) {
      newErrors.productCode = t('productMaster.createDialog.validation.productCodeTooLong')
    }

    // 2. Tên sản phẩm
    if (!trimmedName) {
      newErrors.productName = t('productMaster.createDialog.validation.productNameRequired')
    } else if (trimmedName.length > MAX_NAME_LENGTH) {
      newErrors.productName = t('productMaster.createDialog.validation.productNameTooLong')
    }

    // 3. Đơn vị
    if (!trimmedUnit) {
      newErrors.unit = t('productMaster.createDialog.validation.unitRequired')
    } else if (trimmedUnit.length > MAX_UNIT_LENGTH) {
      newErrors.unit = t('productMaster.createDialog.validation.unitTooLong')
    }

    // 4. Quy cách
    if (!trimmedSpec) {
      newErrors.specification = t('productMaster.createDialog.validation.specificationRequired')
    } else if (trimmedSpec.length > MAX_SPEC_LENGTH) {
      newErrors.specification = t('productMaster.createDialog.validation.specificationTooLong')
    }

    // 5. Số lượng/Thùng (> 0)
    let parsedQuantity = 0
    if (!rawQty) {
      newErrors.quantityPerBox = t('productMaster.createDialog.validation.quantityPerBoxRequired')
    } else {
      const quantityNumber = Number(rawQty)
      if (!/^\d+$/.test(rawQty) || !Number.isInteger(quantityNumber) || quantityNumber <= 0) {
        newErrors.quantityPerBox = t('productMaster.createDialog.validation.quantityPerBoxInvalid')
      } else {
        parsedQuantity = quantityNumber
      }
    }

    // 6. Trọng lượng (> 0)
    let parsedWeight = 0
    if (!rawWeight) {
      newErrors.productWeight = t('productMaster.createDialog.validation.productWeightRequired')
    } else {
      if (rawWeight.includes(',') && rawWeight.includes('.')) {
        newErrors.productWeight = t('productMaster.createDialog.validation.productWeightInvalid')
      } else {
        const normalizedWeight = rawWeight.replace(',', '.')
        const weightNumber = Number(normalizedWeight)
        if (Number.isNaN(weightNumber) || weightNumber <= 0) {
          newErrors.productWeight = t('productMaster.createDialog.validation.productWeightInvalid')
        } else {
          const decimalPart = normalizedWeight.includes('.') ? normalizedWeight.split('.')[1] : ''
          if (decimalPart && decimalPart.length > MAX_DECIMAL_SCALE) {
            newErrors.productWeight = t('productMaster.createDialog.validation.productWeightInvalid')
          } else {
            parsedWeight = weightNumber
          }
        }
      }
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors)
      return { isValid: false }
    }

    return {
      isValid: true,
      payload: {
        productCode: trimmedCode,
        productName: trimmedName,
        unit: trimmedUnit,
        specification: trimmedSpec,
        quantityPerBox: parsedQuantity,
        productWeight: parsedWeight,
        isActive: form.isActive,
      },
    }
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (isSubmitting) {
      return
    }

    if (!product?.id) {
      void Swal.fire({
        icon: 'error',
        title: t('productMaster.updateDialog.errorTitle'),
        text: t('productMaster.updateDialog.notFound'),
        confirmButtonText: t('productMaster.ok'),
      })
      return
    }

    const { isValid, payload } = validateForm()
    if (!isValid || !payload) {
      return
    }

    setIsSubmitting(true)
    try {
      await updateProduct(product.id, payload)

      onSuccess()
      onClose()

      void Swal.fire({
        icon: 'success',
        title: t('productMaster.updateDialog.successTitle'),
        text: t('productMaster.updateDialog.successMessage'),
        timer: 2000,
        showConfirmButton: false,
      })
    } catch (error: unknown) {
      if (error instanceof ApiRequestError) {
        if (error.status === 409) {
          const duplicateCodeMessage = t('productMaster.createDialog.validation.duplicateCode', {
            code: payload.productCode,
          })
          const isCodeDuplicate = !error.detail || error.detail.includes('Mã sản phẩm')

          if (isCodeDuplicate) {
            setErrors((current) => ({
              ...current,
              productCode: duplicateCodeMessage,
            }))
          } else {
            setErrors((current) => ({
              ...current,
              productName: error.detail,
            }))
          }

          void Swal.fire({
            icon: 'warning',
            title: isCodeDuplicate ? duplicateCodeMessage : (error.detail || duplicateCodeMessage),
            confirmButtonText: t('productMaster.ok'),
          })
          return
        }

        if (error.status === 404) {
          void Swal.fire({
            icon: 'error',
            title: t('productMaster.updateDialog.errorTitle'),
            text: error.detail || t('productMaster.updateDialog.notFound'),
            confirmButtonText: t('productMaster.ok'),
          })
          return
        }

        if (error.status === 400 && error.detail) {
          void Swal.fire({
            icon: 'warning',
            title: t('productMaster.updateDialog.errorTitle'),
            text: error.detail,
            confirmButtonText: t('productMaster.ok'),
          })
          return
        }
      }

      void Swal.fire({
        icon: 'error',
        title: t('productMaster.updateDialog.errorTitle'),
        text: error instanceof Error ? error.message : t('productMaster.unexpectedError'),
        confirmButtonText: t('productMaster.ok'),
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    if (!isSubmitting) {
      onClose()
    }
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="sm"
      fullWidth
      slotProps={{
        paper: {
          className: 'product-dialog',
        },
      }}
    >
      <DialogTitle className="product-dialog__title">
        <Box>
          <Typography component="h2" className="product-dialog__title-text">
            {t('productMaster.updateDialog.title')}
          </Typography>
          <Typography className="product-dialog__subtitle-text">
            {t('productMaster.updateDialog.subtitle')}
          </Typography>
        </Box>
        <IconButton
          size="small"
          onClick={handleClose}
          disabled={isSubmitting}
          className="product-dialog__close-btn"
          aria-label={t('productMaster.close')}
        >
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>

      <form onSubmit={handleSubmit} noValidate>
        <DialogContent className="product-dialog__content">
          <div className="product-dialog__grid">
            {/* Row 1: Mã sản phẩm (Disabled) | Tên sản phẩm * */}
            <div className="product-dialog__field">
              <label htmlFor="update-product-code" className="product-dialog__label">
                {t('productMaster.fields.productCode')}
              </label>
              <TextField
                id="update-product-code"
                fullWidth
                size="small"
                value={form.productCode}
                disabled
                className="product-dialog__input"
              />
            </div>

            <div className="product-dialog__field">
              <label htmlFor="update-product-name" className="product-dialog__label">
                {t('productMaster.fields.productName')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="update-product-name"
                fullWidth
                size="small"
                value={form.productName}
                onChange={(event) => handleFieldChange('productName', event.target.value)}
                error={Boolean(errors.productName)}
                helperText={errors.productName}
                placeholder={t('productMaster.createDialog.placeholders.productName')}
                disabled={isSubmitting}
                className="product-dialog__input"
                autoFocus
              />
            </div>

            {/* Row 2: Đơn vị * | Quy cách * */}
            <div className="product-dialog__field">
              <label htmlFor="update-product-unit" className="product-dialog__label">
                {t('productMaster.fields.unit')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="update-product-unit"
                fullWidth
                size="small"
                value={form.unit}
                onChange={(event) => handleFieldChange('unit', event.target.value)}
                error={Boolean(errors.unit)}
                helperText={errors.unit}
                placeholder={t('productMaster.createDialog.placeholders.unit')}
                disabled={isSubmitting}
                className="product-dialog__input"
              />
            </div>

            <div className="product-dialog__field">
              <label htmlFor="update-product-spec" className="product-dialog__label">
                {t('productMaster.fields.specification')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="update-product-spec"
                fullWidth
                size="small"
                value={form.specification}
                onChange={(event) => handleFieldChange('specification', event.target.value)}
                error={Boolean(errors.specification)}
                helperText={errors.specification}
                placeholder={t('productMaster.createDialog.placeholders.specification')}
                disabled={isSubmitting}
                className="product-dialog__input"
              />
            </div>

            {/* Row 3: Số lượng/Thùng * | Trọng lượng * */}
            <div className="product-dialog__field">
              <label htmlFor="update-product-qty" className="product-dialog__label">
                {t('productMaster.fields.quantityPerBox')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="update-product-qty"
                fullWidth
                size="small"
                type="number"
                slotProps={{ htmlInput: { min: 1, step: 1 } }}
                value={form.quantityPerBox}
                onChange={(event) => handleFieldChange('quantityPerBox', event.target.value)}
                error={Boolean(errors.quantityPerBox)}
                helperText={errors.quantityPerBox}
                placeholder={t('productMaster.createDialog.placeholders.quantityPerBox')}
                disabled={isSubmitting}
                className="product-dialog__input"
              />
            </div>

            <div className="product-dialog__field">
              <label htmlFor="update-product-weight" className="product-dialog__label">
                {t('productMaster.fields.productWeight')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="update-product-weight"
                fullWidth
                size="small"
                value={form.productWeight}
                onChange={(event) => handleFieldChange('productWeight', event.target.value)}
                error={Boolean(errors.productWeight)}
                helperText={errors.productWeight}
                placeholder={t('productMaster.createDialog.placeholders.productWeight')}
                disabled={isSubmitting}
                className="product-dialog__input"
              />
            </div>

            {/* Row 4: Trạng thái (IsActive) */}
            <div className="product-dialog__field product-dialog__field--full">
              <label htmlFor="update-product-status" className="product-dialog__label">
                {t('productMaster.fields.status')} <span className="product-dialog__required">*</span>
              </label>
              <FormControl size="small" fullWidth className="product-dialog__select">
                <Select
                  id="update-product-status"
                  value={form.isActive ? 'active' : 'inactive'}
                  onChange={(event) => handleFieldChange('isActive', event.target.value === 'active')}
                  disabled={isSubmitting}
                >
                  <MenuItem value="active">
                    <span className="product-status product-status--active">
                      {t('productMaster.active')}
                    </span>
                  </MenuItem>
                  <MenuItem value="inactive">
                    <span className="product-status product-status--inactive">
                      {t('productMaster.inactive')}
                    </span>
                  </MenuItem>
                </Select>
              </FormControl>
            </div>
          </div>
        </DialogContent>

        <DialogActions className="product-dialog__actions">
          <Button
            type="button"
            variant="outlined"
            onClick={handleClose}
            disabled={isSubmitting}
            className="product-button product-button--quiet"
          >
            {t('productMaster.updateDialog.actions.close')}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isSubmitting}
            className="product-button product-button--primary"
            startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}
          >
            {isSubmitting
              ? t('productMaster.updateDialog.actions.saving')
              : t('productMaster.updateDialog.actions.save')}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
