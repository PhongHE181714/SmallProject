import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import CloseIcon from '@mui/icons-material/Close'
import {
  Alert,
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
import { downloadSaleOutRevenueReport } from '../api/saleOutApi'

interface SaleOutRevenueReportDialogProps {
  open: boolean
  onClose: () => void
}

export default function SaleOutRevenueReportDialog({ open, onClose }: SaleOutRevenueReportDialogProps) {
  const { t } = useTranslation()
  const [timeBegin, setTimeBegin] = useState('')
  const [timeEnd, setTimeEnd] = useState('')
  const [isDownloading, setIsDownloading] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [noRecordsMessage, setNoRecordsMessage] = useState('')

  useEffect(() => {
    if (!open) return
    setTimeBegin('')
    setTimeEnd('')
    setErrorMessage('')
    setNoRecordsMessage('')
  }, [open])

  const isDateRangeInvalid = Boolean(timeBegin && timeEnd && timeBegin > timeEnd)

  const handleDownload = async () => {
    if (!timeBegin || !timeEnd || isDateRangeInvalid || isDownloading) return

    setIsDownloading(true)
    setErrorMessage('')
    try {
      const report = await downloadSaleOutRevenueReport(timeBegin, timeEnd)
      const fileUrl = URL.createObjectURL(report)
      const link = document.createElement('a')
      link.href = fileUrl
      link.download = `bao-cao-doanh-thu-${timeBegin.replaceAll('-', '')}-${timeEnd.replaceAll('-', '')}.xlsx`
      link.click()
      window.setTimeout(() => URL.revokeObjectURL(fileUrl), 0)
      onClose()
    } catch (error: unknown) {
      if (error instanceof ApiRequestError && error.status === 404) {
        setNoRecordsMessage(t('saleOut.revenueReportDialog.noRecordsMessage'))
        return
      }

      setErrorMessage(getRevenueReportErrorMessage(error, t))
    } finally {
      setIsDownloading(false)
    }
  }

  const handleClose = () => {
    if (!isDownloading) onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth slotProps={{ paper: { className: 'product-dialog sale-out-revenue-report-dialog' } }}>
      <DialogTitle className="product-dialog__title">
        <div>
          <Typography component="h2" className="product-dialog__title-text">{t('saleOut.revenueReportDialog.title')}</Typography>
          <Typography className="product-dialog__subtitle-text">{t('saleOut.revenueReportDialog.subtitle')}</Typography>
        </div>
        <IconButton size="small" onClick={handleClose} disabled={isDownloading} className="product-dialog__close-btn" aria-label={t('saleOut.revenueReportDialog.close')}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent className="product-dialog__content">
        {errorMessage && <Alert severity="error" sx={{ mb: 2 }}>{errorMessage}</Alert>}
        <div className="sale-out-revenue-report-dialog__dates">
          <TextField
            type="date"
            required
            fullWidth
            size="small"
            label={t('saleOut.revenueReportDialog.startDate')}
            value={timeBegin}
            disabled={isDownloading}
            slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: timeEnd || undefined } }}
            onChange={(event) => {
              setTimeBegin(event.target.value)
              setErrorMessage('')
              setNoRecordsMessage('')
            }}
          />
          <TextField
            type="date"
            required
            fullWidth
            size="small"
            label={t('saleOut.revenueReportDialog.endDate')}
            value={timeEnd}
            disabled={isDownloading}
            slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: timeBegin || undefined } }}
            onChange={(event) => {
              setTimeEnd(event.target.value)
              setErrorMessage('')
              setNoRecordsMessage('')
            }}
          />
        </div>
        {isDateRangeInvalid && <Alert severity="warning" sx={{ mt: 2 }}>{t('saleOut.revenueReportDialog.invalidDateRange')}</Alert>}
        {noRecordsMessage && <Alert severity="info" sx={{ mt: 2 }}>{noRecordsMessage}</Alert>}
      </DialogContent>
      <DialogActions className="product-dialog__actions">
        <Button type="button" variant="outlined" onClick={handleClose} disabled={isDownloading} className="product-button product-button--quiet">
          {t('saleOut.revenueReportDialog.close')}
        </Button>
        <Button
          type="button"
          variant="contained"
          onClick={() => void handleDownload()}
          disabled={!timeBegin || !timeEnd || isDateRangeInvalid || isDownloading}
          className="product-button product-button--primary"
          startIcon={isDownloading ? <CircularProgress size={16} color="inherit" /> : null}
        >
          {isDownloading ? t('saleOut.revenueReportDialog.exporting') : t('saleOut.revenueReportDialog.export')}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function getRevenueReportErrorMessage(error: unknown, translate: (key: string) => string) {
  if (error instanceof ApiRequestError && error.status >= 500) {
    return translate('saleOut.revenueReportDialog.serverError')
  }

  return error instanceof TypeError
    ? translate('saleOut.revenueReportDialog.connectionError')
    : translate('saleOut.revenueReportDialog.unexpectedError')
}
