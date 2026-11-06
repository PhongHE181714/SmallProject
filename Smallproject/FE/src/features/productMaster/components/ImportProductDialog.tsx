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
import { importProducts } from '../api/productsApi'

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024
const MAX_DATA_ROWS = 10_000
const MAX_PREVIEW_ROWS = 20

type ImportValidationError = {
  field?: string
  message?: string
  rowNumber?: number
}

export interface ImportProductDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

export default function ImportProductDialog({
  open,
  onClose,
  onSuccess,
}: ImportProductDialogProps) {
  const { t } = useTranslation()
  const [file, setFile] = useState<File | null>(null)
  const [previewRows, setPreviewRows] = useState<string[][]>([])
  const [isPreviewOpen, setIsPreviewOpen] = useState(false)
  const [isParsing, setIsParsing] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    if (open) {
      setFile(null)
      setPreviewRows([])
      setIsPreviewOpen(false)
      setIsParsing(false)
      setIsSubmitting(false)
      setErrorMessage('')
    }
  }, [open])

  const handleClose = () => {
    if (!isSubmitting) {
      onClose()
    }
  }

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0]
    event.target.value = ''

    if (!selectedFile) {
      return
    }

    setFile(null)
    setPreviewRows([])
    setIsPreviewOpen(false)
    setErrorMessage('')

    if (selectedFile.size > MAX_FILE_SIZE_BYTES) {
      setErrorMessage(t('productMaster.importDialog.fileTooLarge'))
      return
    }

    const fileExtension = selectedFile.name.split('.').pop()?.toLowerCase()
    if (fileExtension !== 'xlsx' && fileExtension !== 'csv') {
      setErrorMessage(t('productMaster.importDialog.invalidFileType'))
      return
    }

    setIsParsing(true)
    try {
      const rows = fileExtension === 'csv'
        ? parseCsvRows(await selectedFile.text())
        : await parseExcelRows(selectedFile)

      if (rows.length > MAX_DATA_ROWS + 1) {
        setErrorMessage(t('productMaster.importDialog.tooManyRows', { max: MAX_DATA_ROWS }))
        return
      }

      setFile(selectedFile)
      setPreviewRows(rows)
    } catch {
      setErrorMessage(t('productMaster.importDialog.cannotPreview'))
    } finally {
      setIsParsing(false)
    }
  }

  const handleImport = async () => {
    if (!file) {
      return
    }

    setIsSubmitting(true)
    setErrorMessage('')
    try {
      const response = await importProducts(file)
      onClose()
      onSuccess()
      void Swal.fire({
        icon: 'success',
        title: t('productMaster.importDialog.successTitle'),
        text: t('productMaster.importDialog.successMessage', {
          count: response.importedCount,
        }),
        timer: 2200,
        showConfirmButton: false,
      })
    } catch (error: unknown) {
      setErrorMessage(getImportErrorMessage(
        error,
        (key, options) => String(t(key as never, options as never)),
      ))
    } finally {
      setIsSubmitting(false)
    }
  }

  const columns = previewRows[0] ?? []
  const dataRows = previewRows
    .slice(1)
    .filter((row) => row.some((value) => value.trim().length > 0))
  const visibleRows = dataRows.slice(0, MAX_PREVIEW_ROWS)

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth={isPreviewOpen ? 'md' : 'sm'}
      fullWidth
      slotProps={{
        paper: {
          className: 'product-dialog product-import-dialog',
        },
      }}
    >
      <DialogTitle className="product-dialog__title">
        <Box>
          <Typography component="h2" className="product-dialog__title-text">
            {isPreviewOpen
              ? t('productMaster.importDialog.previewTitle')
              : t('productMaster.importDialog.title')}
          </Typography>
          <Typography className="product-dialog__subtitle-text">
            {isPreviewOpen
              ? t('productMaster.importDialog.previewSubtitle')
              : t('productMaster.importDialog.subtitle')}
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

      <DialogContent className="product-dialog__content">
        {!isPreviewOpen ? (
          <div className="product-import__selection">
            <Typography className="product-dialog__label">
              {t('productMaster.importDialog.fileLabel')}
            </Typography>
            <Button
              component="label"
              variant="outlined"
              disabled={isParsing || isSubmitting}
              className="product-button product-button--quiet product-import__choose-button"
            >
              {isParsing
                ? t('productMaster.importDialog.readingFile')
                : t('productMaster.importDialog.chooseFile')}
              <input
                hidden
                type="file"
                accept=".xlsx,.csv"
                onChange={(event) => void handleFileChange(event)}
              />
            </Button>

            {file && (
              <button
                type="button"
                className="product-import__selected-file"
                onClick={() => setIsPreviewOpen(true)}
                disabled={isParsing || isSubmitting}
              >
                <InsertDriveFileOutlinedIcon fontSize="small" />
                <span>{file.name}</span>
                <small>{t('productMaster.importDialog.clickToPreview')}</small>
              </button>
            )}

            <Typography className="product-import__limits">
              {t('productMaster.importDialog.fileLimits')}
            </Typography>
          </div>
        ) : (
          <div className="product-import__preview">
            <div className="product-import__preview-summary">
              <Typography className="product-import__filename">{file?.name}</Typography>
              <Typography className="product-import__row-count">
                {t('productMaster.importDialog.rowCount', { count: dataRows.length })}
              </Typography>
            </div>
            <TableContainer className="product-import__table-container">
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    {columns.map((column, index) => (
                      <TableCell key={`${column}-${index}`}>{column || `#${index + 1}`}</TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {visibleRows.map((row, rowIndex) => (
                    <TableRow key={rowIndex}>
                      {columns.map((_, columnIndex) => (
                        <TableCell key={columnIndex}>{row[columnIndex] || '—'}</TableCell>
                      ))}
                    </TableRow>
                  ))}
                  {visibleRows.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={Math.max(columns.length, 1)}>
                        {t('productMaster.importDialog.noRows')}
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
            {dataRows.length > MAX_PREVIEW_ROWS && (
              <Typography className="product-import__preview-note">
                {t('productMaster.importDialog.previewLimit', {
                  shown: MAX_PREVIEW_ROWS,
                  total: dataRows.length,
                })}
              </Typography>
            )}
          </div>
        )}

        {errorMessage && (
          <Alert severity="error" className="product-import__error">
            {errorMessage}
          </Alert>
        )}
      </DialogContent>

      <DialogActions className="product-dialog__actions">
        {isPreviewOpen ? (
          <>
            <Button
              type="button"
              variant="outlined"
              className="product-button product-button--quiet"
              disabled={isSubmitting}
              onClick={() => {
                setIsPreviewOpen(false)
                setErrorMessage('')
              }}
            >
              {t('productMaster.importDialog.back')}
            </Button>
            <Button
              type="button"
              variant="contained"
              className="product-button product-button--primary"
              disabled={!file || isSubmitting}
              onClick={() => void handleImport()}
              startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}
            >
              {isSubmitting
                ? t('productMaster.importDialog.uploading')
                : t('productMaster.importDialog.confirmUpload')}
            </Button>
          </>
        ) : (
          <>
            <Button
              type="button"
              variant="outlined"
              className="product-button product-button--quiet"
              onClick={handleClose}
              disabled={isSubmitting}
            >
              {t('productMaster.importDialog.close')}
            </Button>
            <Button
              type="button"
              variant="contained"
              className="product-button product-button--primary"
              disabled={!file || isParsing || isSubmitting}
              onClick={() => void handleImport()}
              startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : null}
            >
              {isSubmitting
                ? t('productMaster.importDialog.uploading')
                : t('productMaster.importDialog.uploadNow')}
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
  if (!firstSheetName) {
    throw new Error('emptyWorkbook')
  }

  const worksheet = workbook.Sheets[firstSheetName]
  const rows = XLSX.utils.sheet_to_json<unknown[]>(worksheet, {
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
      if (character === '"' && nextCharacter === '"') {
        field += '"'
        index += 1
      } else if (character === '"') {
        isInsideQuotes = false
      } else {
        field += character
      }
      continue
    }

    if (character === '"' && field.length === 0) {
      isInsideQuotes = true
    } else if (character === ',') {
      row.push(field)
      field = ''
    } else if (character === '\n' || character === '\r') {
      if (character === '\r' && nextCharacter === '\n') {
        index += 1
      }
      row.push(field)
      rows.push(row)
      row = []
      field = ''
    } else {
      field += character
    }
  }

  if (isInsideQuotes) {
    throw new Error('invalidCsv')
  }

  if (field.length > 0 || row.length > 0) {
    row.push(field)
    rows.push(row)
  }

  return rows
}

function getImportErrorMessage(
  error: unknown,
  translate: (key: string, options?: Record<string, unknown>) => string,
) {
  if (!(error instanceof ApiRequestError)) {
    return error instanceof Error ? error.message : translate('productMaster.importDialog.unexpectedError')
  }

  if (error.status === 413) {
    return translate('productMaster.importDialog.fileTooLarge')
  }

  const errors = getValidationErrors(error.data)
  const duplicateCodeErrors = errors.filter((item) =>
    item.field?.toLowerCase() === 'productcode'
    && /trùng|tồn tại|duplicate|already exists/i.test(item.message ?? ''),
  )
  const duplicateNameErrors = errors.filter((item) =>
    item.field?.toLowerCase() === 'productname'
    && /trùng|tồn tại|duplicate|already exists/i.test(item.message ?? ''),
  )
  if (duplicateCodeErrors.length > 0 || duplicateNameErrors.length > 0 || error.status === 409) {
    const productCodes = duplicateCodeErrors.flatMap((item) => {
      const matches = [...(item.message ?? '').matchAll(/Mã sản phẩm ['“]([^'”]+)['”]/gi)]
      const rowLabel = item.rowNumber
        ? translate('productMaster.importDialog.rowLabel', { row: item.rowNumber })
        : ''
      return matches.map((match) => rowLabel ? `${match[1]} (${rowLabel})` : match[1])
    })
    const codes = [...new Set(productCodes)]
    const duplicateMessages = [translate('productMaster.importDialog.duplicateBlocked')]
    if (codes.length > 0) {
      duplicateMessages.push(translate('productMaster.importDialog.duplicateCodes', { codes: codes.join(', ') }))
    }
    if (
      codes.length === 0
      && (duplicateCodeErrors.length > 0 || (error.status === 409 && duplicateNameErrors.length === 0))
    ) {
      duplicateMessages.push(translate('productMaster.importDialog.duplicateCodesGeneric'))
    }
    return duplicateMessages.join('\n')
  }

  const details = errors
    .map((item) => item.message
      ? item.rowNumber
        ? `${translate('productMaster.importDialog.rowLabel', { row: item.rowNumber })}: ${item.message}`
        : item.message
      : '')
    .filter(Boolean)

  return [error.detail, ...details]
    .filter((message, index, messages): message is string => Boolean(message) && messages.indexOf(message) === index)
    .join('\n') || translate('productMaster.importDialog.unexpectedError')
}

function getValidationErrors(data: unknown): ImportValidationError[] {
  if (!data || typeof data !== 'object' || !('errors' in data)) {
    return []
  }

  const rawErrors = (data as { errors?: unknown }).errors
  if (!Array.isArray(rawErrors)) {
    return []
  }

  return rawErrors.filter((item): item is ImportValidationError =>
    typeof item === 'object'
    && item !== null
    && ('field' in item || 'message' in item || 'rowNumber' in item),
  )
}
