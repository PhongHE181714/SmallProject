import { useEffect, useState, type ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'
import CloseIcon from '@mui/icons-material/Close'
import InsertDriveFileOutlinedIcon from '@mui/icons-material/InsertDriveFileOutlined'
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
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import * as XLSX from 'xlsx'
import Swal from 'sweetalert2'
import { ApiRequestError } from '../../../shared/api/httpClient'
import { importSaleOuts } from '../api/saleOutApi'

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024
const MAX_DATA_ROWS = 10_000
const MAX_PREVIEW_ROWS = 20

type ImportValidationError = {
  field?: string
  message?: string
  rowNumber?: number
}

interface ImportSaleOutDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

export default function ImportSaleOutDialog({ open, onClose, onSuccess }: ImportSaleOutDialogProps) {
  const { t } = useTranslation()
  const [file, setFile] = useState<File | null>(null)
  const [previewRows, setPreviewRows] = useState<string[][]>([])
  const [isPreviewOpen, setIsPreviewOpen] = useState(false)
  const [isParsing, setIsParsing] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [rowErrors, setRowErrors] = useState<Record<number, string[]>>({})

  useEffect(() => {
    if (!open) return
    setFile(null)
    setPreviewRows([])
    setIsPreviewOpen(false)
    setIsParsing(false)
    setIsSubmitting(false)
    setErrorMessage('')
    setRowErrors({})
  }, [open])

  const handleClose = () => {
    if (!isSubmitting) onClose()
  }

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0]
    event.target.value = ''
    if (!selectedFile) return

    setFile(null)
    setPreviewRows([])
    setIsPreviewOpen(false)
    setErrorMessage('')
    setRowErrors({})

    if (selectedFile.size > MAX_FILE_SIZE_BYTES) {
      setErrorMessage(t('saleOut.importDialog.fileTooLarge'))
      return
    }

    const fileExtension = selectedFile.name.split('.').pop()?.toLowerCase()
    if (fileExtension !== 'xlsx' && fileExtension !== 'csv') {
      setErrorMessage(t('saleOut.importDialog.invalidFileType'))
      return
    }

    setIsParsing(true)
    try {
      const rows = fileExtension === 'csv'
        ? parseCsvRows(await selectedFile.text())
        : await parseExcelRows(selectedFile)

      if (rows.length > MAX_DATA_ROWS + 1) {
        setErrorMessage(t('saleOut.importDialog.tooManyRows', { max: MAX_DATA_ROWS }))
        return
      }

      setFile(selectedFile)
      setPreviewRows(rows)
    } catch {
      setErrorMessage(t('saleOut.importDialog.cannotPreview'))
    } finally {
      setIsParsing(false)
    }
  }

  const handleImport = async () => {
    if (!file || isSubmitting) return
    setIsSubmitting(true)
    setErrorMessage('')
    setRowErrors({})

    try {
      const response = await importSaleOuts(file)
      onClose()
      onSuccess()
      void Swal.fire({
        icon: 'success',
        title: t('saleOut.importDialog.successTitle'),
        text: t('saleOut.importDialog.successMessage', { count: response.importedCount }),
        timer: 2200,
        showConfirmButton: false,
      })
    } catch (error: unknown) {
      const importErrors = getImportErrorDetails(
        error,
        (key, options) => String(t(key as never, options as never)),
      )
      setErrorMessage(importErrors.message)
      setRowErrors(importErrors.rowErrors)
      if (Object.keys(importErrors.rowErrors).length > 0) {
        setIsPreviewOpen(true)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const columns = previewRows[0] ?? []
  const dataRows = previewRows
    .slice(1)
    .map((row, index) => ({ row, rowNumber: index + 2 }))
    .filter(({ row }) => row.some((value) => value.trim().length > 0))
  const hasRowErrors = Object.keys(rowErrors).length > 0
  const visibleRows = hasRowErrors
    ? dataRows.filter(({ rowNumber }) => Boolean(rowErrors[rowNumber]))
    : dataRows.slice(0, MAX_PREVIEW_ROWS)

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth={isPreviewOpen ? 'md' : 'sm'}
      fullWidth
      slotProps={{ paper: { className: 'product-dialog product-import-dialog' } }}
    >
      <DialogTitle className="product-dialog__title">
        <Box>
          <Typography component="h2" className="product-dialog__title-text">
            {isPreviewOpen ? t('saleOut.importDialog.previewTitle') : t('saleOut.importDialog.title')}
          </Typography>
          <Typography className="product-dialog__subtitle-text">
            {isPreviewOpen ? t('saleOut.importDialog.previewSubtitle') : t('saleOut.importDialog.subtitle')}
          </Typography>
        </Box>
        <IconButton size="small" onClick={handleClose} disabled={isSubmitting} className="product-dialog__close-btn" aria-label={t('saleOut.importDialog.close')}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>

      <DialogContent className="product-dialog__content">
        {hasRowErrors ? (
          <Alert severity="error" className="product-import__error" sx={{ mb: 2, alignItems: 'flex-start', '& .MuiAlert-message': { width: '100%' } }}>
            <Typography sx={{ fontWeight: 700 }}>{t('saleOut.importDialog.rowErrorsTitle')}</Typography>
            <Typography variant="body2" sx={{ mt: 0.5 }}>{t('saleOut.importDialog.rowErrorsDescription')}</Typography>
            <Box component="ul" sx={{ mb: 0, mt: 1, pl: 2.5 }}>
              {Object.entries(rowErrors).map(([rowNumber, messages]) => (
                <li key={rowNumber}>
                  <Typography variant="body2" sx={{ overflowWrap: 'anywhere' }}>
                    {t('saleOut.importDialog.rowLabel', { row: rowNumber })}: {messages.join(' · ')}
                  </Typography>
                </li>
              ))}
            </Box>
          </Alert>
        ) : errorMessage ? (
          <Alert severity="error" className="product-import__error" sx={{ mb: 2, whiteSpace: 'pre-line' }}>
            {errorMessage}
          </Alert>
        ) : null}

        {!isPreviewOpen ? (
          <div className="product-import__selection">
            <Typography className="product-dialog__label">{t('saleOut.importDialog.fileLabel')}</Typography>
            <Button
              component="label"
              variant="outlined"
              disabled={isParsing || isSubmitting}
              className="product-button product-button--quiet product-import__choose-button"
            >
              {isParsing ? t('saleOut.importDialog.readingFile') : t('saleOut.importDialog.chooseFile')}
              <input hidden type="file" accept=".xlsx,.csv" onChange={(event) => void handleFileChange(event)} />
            </Button>
            {file && (
              <button type="button" className="product-import__selected-file" onClick={() => setIsPreviewOpen(true)} disabled={isParsing || isSubmitting}>
                <InsertDriveFileOutlinedIcon fontSize="small" />
                <span>{file.name}</span>
                <small>{t('saleOut.importDialog.clickToPreview')}</small>
              </button>
            )}
          </div>
        ) : (
          <div className="product-import__preview">
            <div className="product-import__preview-summary">
              <Typography className="product-import__filename">{file?.name}</Typography>
              <Typography className="product-import__row-count">{t('saleOut.importDialog.rowCount', { count: dataRows.length })}</Typography>
            </div>
            <TableContainer className="product-import__table-container">
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    {hasRowErrors && <TableCell>{t('saleOut.importDialog.rowNumberColumn')}</TableCell>}
                    {columns.map((column, index) => <TableCell key={`${column}-${index}`}>{column || `#${index + 1}`}</TableCell>)}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {visibleRows.map(({ row, rowNumber }) => {
                    return (
                      <TableRow key={rowNumber} sx={hasRowErrors ? { backgroundColor: 'rgba(211, 47, 47, 0.06)' } : undefined}>
                        {hasRowErrors && <TableCell>{rowNumber}</TableCell>}
                        {columns.map((_, columnIndex) => <TableCell key={columnIndex}>{row[columnIndex] || '—'}</TableCell>)}
                      </TableRow>
                    )
                  })}
                  {visibleRows.length === 0 && <TableRow><TableCell colSpan={Math.max(columns.length + Number(hasRowErrors), 1)}>{t('saleOut.importDialog.noRows')}</TableCell></TableRow>}
                </TableBody>
              </Table>
            </TableContainer>
            {!hasRowErrors && dataRows.length > MAX_PREVIEW_ROWS && (
              <Typography className="product-import__preview-note">
                {t('saleOut.importDialog.previewLimit', { shown: MAX_PREVIEW_ROWS, total: dataRows.length })}
              </Typography>
            )}
          </div>
        )}

      </DialogContent>

      <DialogActions className="product-dialog__actions">
        {isPreviewOpen ? (
          <>
            <Button type="button" variant="outlined" className="product-button product-button--quiet" disabled={isSubmitting} onClick={() => { setIsPreviewOpen(false); setErrorMessage(''); setRowErrors({}) }}>
              {t('saleOut.importDialog.back')}
            </Button>
            <Button type="button" variant="contained" className="product-button product-button--primary" disabled={!file || isSubmitting} onClick={() => void handleImport()} startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}>
              {isSubmitting ? t('saleOut.importDialog.uploading') : t('saleOut.importDialog.confirmUpload')}
            </Button>
          </>
        ) : (
          <>
            <Button type="button" variant="outlined" className="product-button product-button--quiet" onClick={handleClose} disabled={isSubmitting}>{t('saleOut.importDialog.close')}</Button>
            <Button type="button" variant="contained" className="product-button product-button--primary" disabled={!file || isParsing || isSubmitting} onClick={() => void handleImport()} startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}>
              {isSubmitting ? t('saleOut.importDialog.uploading') : t('saleOut.importDialog.uploadNow')}
            </Button>
          </>
        )}
      </DialogActions>
    </Dialog>
  )
}

async function parseExcelRows(file: File): Promise<string[][]> {
  const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array' })
  const firstSheetName = workbook.SheetNames[0]
  if (!firstSheetName) throw new Error('emptyWorkbook')
  const rows = XLSX.utils.sheet_to_json<unknown[]>(workbook.Sheets[firstSheetName], {
    header: 1,
    raw: false,
    defval: '',
    blankrows: true,
  })
  return rows.map((row) => row.map((value) => String(value ?? '')))
}

function parseCsvRows(csvText: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let field = ''
  let isInsideQuotes = false
  const normalizedText = csvText.replace(/^\uFEFF/, '')

  for (let index = 0; index < normalizedText.length; index += 1) {
    const character = normalizedText[index]
    const nextCharacter = normalizedText[index + 1]
    if (isInsideQuotes) {
      if (character === '"' && nextCharacter === '"') { field += '"'; index += 1 }
      else if (character === '"') isInsideQuotes = false
      else field += character
      continue
    }
    if (character === '"' && field.length === 0) isInsideQuotes = true
    else if (character === ',') { row.push(field); field = '' }
    else if (character === '\n' || character === '\r') {
      if (character === '\r' && nextCharacter === '\n') index += 1
      row.push(field); rows.push(row); row = []; field = ''
    } else field += character
  }

  if (isInsideQuotes) throw new Error('invalidCsv')
  if (field.length > 0 || row.length > 0) { row.push(field); rows.push(row) }
  return rows
}

function getImportErrorDetails(
  error: unknown,
  translate: (key: string, options?: Record<string, unknown>) => string,
): { message: string; rowErrors: Record<number, string[]> } {
  if (!(error instanceof ApiRequestError)) {
    return {
      message: error instanceof Error ? error.message : translate('saleOut.importDialog.unexpectedError'),
      rowErrors: {},
    }
  }
  if (error.status === 413) {
    return { message: translate('saleOut.importDialog.fileTooLarge'), rowErrors: {} }
  }

  const validationErrors = getValidationErrors(error.data)
  const rowErrors: Record<number, string[]> = {}
  const fileErrors: string[] = []
  for (const item of validationErrors) {
    if (!item.message) continue
    if (item.rowNumber) {
      const message = item.field ? `${item.field}: ${item.message}` : item.message
      rowErrors[item.rowNumber] ??= []
      if (!rowErrors[item.rowNumber].includes(message)) rowErrors[item.rowNumber].push(message)
    } else {
      fileErrors.push(item.message)
    }
  }

  if (Object.keys(rowErrors).length > 0) {
    return { message: '', rowErrors }
  }

  const details = [error.detail, ...fileErrors]
    .filter((message, index, messages): message is string => Boolean(message) && messages.indexOf(message) === index)
  return {
    message: details.join('\n') || translate('saleOut.importDialog.unexpectedError'),
    rowErrors,
  }
}

function getValidationErrors(data: unknown): ImportValidationError[] {
  if (!data || typeof data !== 'object' || !('errors' in data)) return []
  const rawErrors = (data as { errors?: unknown }).errors
  if (!Array.isArray(rawErrors)) return []
  return rawErrors.filter((item): item is ImportValidationError =>
    typeof item === 'object'
    && item !== null
    && ('field' in item || 'message' in item || 'rowNumber' in item),
  )
}
