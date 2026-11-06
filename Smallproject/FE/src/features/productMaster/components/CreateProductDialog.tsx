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
  IconButton,
  TextField,
  Typography,
} from '@mui/material'
import Swal from 'sweetalert2'
import { ApiRequestError } from '../../../shared/api/httpClient'
import { createProduct } from '../api/productsApi'
import type { CreateProductRequest } from '../model/productTypes'

const MAX_CODE_LENGTH = 50
const MAX_NAME_LENGTH = 200
const MAX_UNIT_LENGTH = 50
const MAX_SPEC_LENGTH = 200
const MAX_DECIMAL_SCALE = 3

export interface CreateProductDialogProps {
  open: boolean
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
}

export default function CreateProductDialog({
  open,
  onClose,
  onSuccess,
}: CreateProductDialogProps) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(INITIAL_FORM_STATE)
  const [errors, setErrors] = useState<FormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setForm(INITIAL_FORM_STATE)
      setErrors({})
      setIsSubmitting(false)
    }
  }, [open])

  const handleFieldChange = (field: keyof FormState, value: string) => {
    setForm((current) => ({ ...current, [field]: value }))
    if (errors[field]) {
      setErrors((current) => ({ ...current, [field]: undefined }))
    }
  }

  const validateForm = (): { isValid: boolean; payload?: CreateProductRequest } => {
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

    // 5. Số lượng/Thùng
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

    // 6. Trọng lượng
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
      },
    }
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (isSubmitting) {
      return
    }

    const { isValid, payload } = validateForm()
    if (!isValid || !payload) {
      return
    }

    setIsSubmitting(true)
    try {
      await createProduct(payload)

      onSuccess()
      onClose()

      void Swal.fire({
        icon: 'success',
        title: t('productMaster.createDialog.successTitle'),
        text: t('productMaster.createDialog.successMessage'),
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

        if (error.status === 400 && error.detail) {
          void Swal.fire({
            icon: 'warning',
            title: t('productMaster.createDialog.errorTitle'),
            text: error.detail,
            confirmButtonText: t('productMaster.ok'),
          })
          return
        }
      }

      void Swal.fire({
        icon: 'error',
        title: t('productMaster.createDialog.errorTitle'),
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
            {t('productMaster.createDialog.title')}
          </Typography>
          <Typography className="product-dialog__subtitle-text">
            {t('productMaster.createDialog.subtitle')}
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
            {/* Row 1: Mã sản phẩm * | Tên sản phẩm * */}
            <div className="product-dialog__field">
              <label htmlFor="create-product-code" className="product-dialog__label">
                {t('productMaster.fields.productCode')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="create-product-code"
                fullWidth
                size="small"
                value={form.productCode}
                onChange={(event) => handleFieldChange('productCode', event.target.value)}
                error={Boolean(errors.productCode)}
                helperText={errors.productCode}
                placeholder={t('productMaster.createDialog.placeholders.productCode')}
                disabled={isSubmitting}
                className="product-dialog__input"
                autoFocus
              />
            </div>

            <div className="product-dialog__field">
              <label htmlFor="create-product-name" className="product-dialog__label">
                {t('productMaster.fields.productName')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="create-product-name"
                fullWidth
                size="small"
                value={form.productName}
                onChange={(event) => handleFieldChange('productName', event.target.value)}
                error={Boolean(errors.productName)}
                helperText={errors.productName}
                placeholder={t('productMaster.createDialog.placeholders.productName')}
                disabled={isSubmitting}
                className="product-dialog__input"
              />
            </div>

            {/* Row 2: Đơn vị * | Quy cách * */}
            <div className="product-dialog__field">
              <label htmlFor="create-product-unit" className="product-dialog__label">
                {t('productMaster.fields.unit')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="create-product-unit"
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
              <label htmlFor="create-product-spec" className="product-dialog__label">
                {t('productMaster.fields.specification')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="create-product-spec"
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
              <label htmlFor="create-product-qty" className="product-dialog__label">
                {t('productMaster.fields.quantityPerBox')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="create-product-qty"
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
              <label htmlFor="create-product-weight" className="product-dialog__label">
                {t('productMaster.fields.productWeight')} <span className="product-dialog__required">*</span>
              </label>
              <TextField
                id="create-product-weight"
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
            {t('productMaster.createDialog.actions.close')}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isSubmitting}
            className="product-button product-button--primary"
            startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}
          >
            {isSubmitting
              ? t('productMaster.createDialog.actions.saving')
              : t('productMaster.createDialog.actions.save')}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
