import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import CloseIcon from '@mui/icons-material/Close'
import {
  Alert,
  Autocomplete,
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
import { ApiRequestError } from '../../../shared/api/httpClient'
import { downloadSaleOutPrintPdf, fetchSaleOutPrintOptions } from '../api/saleOutApi'
import type { SaleOutPrintOption } from '../model/saleOutTypes'

interface PrintSaleOutDialogProps {
  open: boolean
  onClose: () => void
}

export default function PrintSaleOutDialog({ open, onClose }: PrintSaleOutDialogProps) {
  const { t, i18n } = useTranslation()
  const [searchValue, setSearchValue] = useState('')
  const [selectedOption, setSelectedOption] = useState<SaleOutPrintOption | null>(null)
  const [options, setOptions] = useState<SaleOutPrintOption[]>([])
  const [isLoadingOptions, setIsLoadingOptions] = useState(false)
  const [isDownloading, setIsDownloading] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    if (!open) return

    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      setIsLoadingOptions(true)
      setErrorMessage('')
      void fetchSaleOutPrintOptions(searchValue, controller.signal)
        .then((response) => {
          if (!controller.signal.aborted) setOptions(response.items)
        })
        .catch((error: unknown) => {
          if (controller.signal.aborted) return
          setOptions([])
          setErrorMessage(getPrintErrorMessage(error, t))
        })
        .finally(() => {
          if (!controller.signal.aborted) setIsLoadingOptions(false)
        })
    }, 250)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [open, searchValue, t])

  useEffect(() => {
    if (!open) return
    setSearchValue('')
    setSelectedOption(null)
    setOptions([])
    setErrorMessage('')
  }, [open])

  const handleDownload = async () => {
    const saleOutNo = selectedOption?.saleOutNo
    if (!saleOutNo || isDownloading) return

    setIsDownloading(true)
    setErrorMessage('')
    try {
      const pdf = await downloadSaleOutPrintPdf(saleOutNo)
      const fileUrl = URL.createObjectURL(pdf)
      const link = document.createElement('a')
      link.href = fileUrl
      link.download = `phieu-xuat-hang-${saleOutNo}.pdf`
      link.click()
      window.setTimeout(() => URL.revokeObjectURL(fileUrl), 0)
      onClose()
    } catch (error: unknown) {
      setErrorMessage(getPrintErrorMessage(error, t))
    } finally {
      setIsDownloading(false)
    }
  }

  const handleClose = () => {
    if (!isDownloading) onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth slotProps={{ paper: { className: 'product-dialog sale-out-print-dialog' } }}>
      <DialogTitle className="product-dialog__title">
        <Box>
          <Typography component="h2" className="product-dialog__title-text">{t('saleOut.printDialog.title')}</Typography>
          <Typography className="product-dialog__subtitle-text">{t('saleOut.printDialog.subtitle')}</Typography>
        </Box>
        <IconButton size="small" onClick={handleClose} disabled={isDownloading} className="product-dialog__close-btn" aria-label={t('saleOut.printDialog.close')}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent className="product-dialog__content">
        {errorMessage && <Alert severity="error" sx={{ mb: 2 }}>{errorMessage}</Alert>}
        <Autocomplete
          options={options}
          value={selectedOption}
          inputValue={searchValue}
          loading={isLoadingOptions}
          disabled={isDownloading}
          filterOptions={(availableOptions) => availableOptions}
          getOptionLabel={(option) => option.saleOutNo ?? ''}
          isOptionEqualToValue={(option, value) => option.saleOutNo === value.saleOutNo}
          noOptionsText={isLoadingOptions ? t('saleOut.printDialog.loading') : t('saleOut.printDialog.noOptions')}
          loadingText={t('saleOut.printDialog.loading')}
          onChange={(_, value) => setSelectedOption(value)}
          onInputChange={(_, value, reason) => {
            if (reason === 'input' || reason === 'clear' || reason === 'reset') {
              setSearchValue(value)
            }
            if (reason === 'input' || reason === 'clear') {
              setSelectedOption(null)
            }
          }}
          renderOption={(props, option) => (
            <li {...props} key={option.saleOutNo ?? 'sale-out-option'}>
              <Box>
                <Typography variant="body2" sx={{ fontWeight: 700 }}>{option.saleOutNo}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {[option.customerName, option.customerPoNo, formatPrintDate(option.orderDate, i18n.language), t('saleOut.printDialog.productCount', { count: option.productCount })]
                    .filter(Boolean)
                    .join(' · ')}
                </Typography>
              </Box>
            </li>
          )}
          renderInput={(params) => (
            <TextField
              {...params}
              autoFocus
              label={t('saleOut.printDialog.saleOutNoLabel')}
              placeholder={t('saleOut.printDialog.searchPlaceholder')}
              size="small"
            />
          )}
        />
        {selectedOption && (
          <Typography className="sale-out-print-dialog__selection" variant="body2">
            {selectedOption.customerName} · {selectedOption.productCount} {t('saleOut.printDialog.products')} · {formatCurrency(selectedOption.totalAmount, i18n.language)}
          </Typography>
        )}
      </DialogContent>
      <DialogActions className="product-dialog__actions">
        <Button type="button" variant="outlined" onClick={handleClose} disabled={isDownloading} className="product-button product-button--quiet">
          {t('saleOut.printDialog.close')}
        </Button>
        <Button
          type="button"
          variant="contained"
          onClick={() => void handleDownload()}
          disabled={!selectedOption?.saleOutNo || isDownloading}
          className="product-button product-button--primary"
          startIcon={isDownloading ? <CircularProgress size={16} color="inherit" /> : null}
        >
          {isDownloading ? t('saleOut.printDialog.exporting') : t('saleOut.printDialog.exportPdf')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function formatPrintDate(value: string | null, language: string) {
  if (!value) return ''
  const parts = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  const date = parts ? new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3])) : new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return new Intl.DateTimeFormat(language.startsWith('vi') ? 'vi-VN' : 'en-US').format(date)
}

function formatCurrency(value: number, language: string) {
  return new Intl.NumberFormat(language.startsWith('vi') ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function getPrintErrorMessage(error: unknown, translate: (key: string) => string) {
  if (error instanceof ApiRequestError) {
    if (error.status === 404) return translate('saleOut.printDialog.notFound')
    if (error.status >= 500) return translate('saleOut.printDialog.serverError')
  }

  return error instanceof TypeError
    ? translate('saleOut.printDialog.connectionError')
    : error instanceof Error ? error.message : translate('saleOut.printDialog.unexpectedError')
}
