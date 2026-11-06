import { Fragment, useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft'
import ChevronRightIcon from '@mui/icons-material/ChevronRight'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import LocalPrintshopOutlinedIcon from '@mui/icons-material/LocalPrintshopOutlined'
import SearchIcon from '@mui/icons-material/Search'
import SummarizeOutlinedIcon from '@mui/icons-material/SummarizeOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown'
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp'
import Swal from 'sweetalert2'
import { readSearchablePageState, writeSearchablePageState } from '../../../shared/routing/searchablePageState'
import {
  Box,
  Button,
  CircularProgress,
  Collapse,
  FormControl,
  IconButton,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { ApiRequestError } from '../../../shared/api/httpClient'
import { deleteSaleOutOrder, downloadSaleOutTemplate, fetchSaleOuts } from '../api/saleOutApi'
import { SALE_OUT_SEARCH_FIELDS, type SaleOutOrderListItem, type SaleOutSearchField, type SaleOutSearchResponse } from '../model/saleOutTypes'
import CreateSaleOutDialog from './CreateSaleOutDialog'
import EditSaleOutDialog from './EditSaleOutDialog'
import ImportSaleOutDialog from './ImportSaleOutDialog'
import PrintSaleOutDialog from './PrintSaleOutDialog'

const DEFAULT_PAGE_SIZE = 50
const MAX_PAGE_SIZE = 200
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100]
const TABLE_COLUMN_COUNT = 8

type SaleOutQuery = {
  page: number
  pageSize: number
  searchField: SaleOutSearchField
  searchValue: string
}

export default function SaleOutView() {
  const { t, i18n } = useTranslation()
  const [query, setQuery] = useState<SaleOutQuery>(() => readSearchablePageState<SaleOutSearchField>(
    { page: 1, pageSize: DEFAULT_PAGE_SIZE, searchField: 'CustomerPoNo', searchValue: '' },
    SALE_OUT_SEARCH_FIELDS.map((field) => field.value),
    MAX_PAGE_SIZE,
  ))
  const [searchField, setSearchField] = useState<SaleOutSearchField>(query.searchField)
  const [searchValue, setSearchValue] = useState(query.searchValue)
  const [pageSizeChoice, setPageSizeChoice] = useState(() =>
    PAGE_SIZE_OPTIONS.includes(query.pageSize) ? String(query.pageSize) : 'custom',
  )
  const [customPageSize, setCustomPageSize] = useState(() =>
    PAGE_SIZE_OPTIONS.includes(query.pageSize) ? '' : String(query.pageSize),
  )
  const [result, setResult] = useState<SaleOutSearchResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [retryCount, setRetryCount] = useState(0)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [isImportDialogOpen, setIsImportDialogOpen] = useState(false)
  const [isPrintDialogOpen, setIsPrintDialogOpen] = useState(false)
  const [saleOutToEdit, setSaleOutToEdit] = useState<SaleOutOrderListItem | null>(null)
  const [expandedOrders, setExpandedOrders] = useState<Set<string>>(new Set())

  useEffect(() => {
    writeSearchablePageState(query, {
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE,
      searchField: 'CustomerPoNo',
      searchValue: '',
    })
  }, [query])

  useEffect(() => {
    setExpandedOrders(new Set())
  }, [query])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setErrorMessage('')

    void fetchSaleOuts(query, controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) {
          setResult(data)
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setResult(null)
          setErrorMessage(getSaleOutErrorMessage(error, t))
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      })

    return () => controller.abort()
  }, [query, retryCount, t])

  const handleSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setQuery((currentQuery) => ({
      ...currentQuery,
      page: 1,
      searchField,
      searchValue: searchValue.trim(),
    }))
  }

  const handlePageSizeChange = (value: string) => {
    setPageSizeChoice(value)

    if (value === 'custom') {
      setCustomPageSize(String(query.pageSize))
      return
    }

    setQuery((currentQuery) => ({
      ...currentQuery,
      page: 1,
      pageSize: Number(value),
    }))
  }

  const applyCustomPageSize = () => {
    const value = Number(customPageSize)
    if (!Number.isInteger(value) || value < 1 || value > MAX_PAGE_SIZE) {
      void Swal.fire({
        icon: 'warning',
        title: t('saleOut.invalidPageSizeTitle'),
        text: t('saleOut.invalidPageSizeMessage', { max: MAX_PAGE_SIZE }),
        confirmButtonText: t('saleOut.ok'),
      })
      return
    }

    setPageSizeChoice('custom')
    setQuery((currentQuery) => ({ ...currentQuery, page: 1, pageSize: value }))
  }

  const handleDelete = async (saleOut: SaleOutOrderListItem) => {
    if (!saleOut.saleOutNo) return
    const confirmation = await Swal.fire({
      icon: 'warning',
      title: t('saleOut.deleteDialog.confirmTitle'),
      text: t('saleOut.deleteDialog.confirmMessage', {
        saleOutNo: saleOut.saleOutNo,
        productCount: saleOut.productCount,
      }),
      showCancelButton: true,
      confirmButtonText: t('saleOut.deleteDialog.confirmButton'),
      cancelButtonText: t('saleOut.deleteDialog.cancelButton'),
      confirmButtonColor: '#c0392b',
      reverseButtons: true,
    })

    if (!confirmation.isConfirmed) return

    try {
      await deleteSaleOutOrder(saleOut.saleOutNo)
      void Swal.fire({
        icon: 'success',
        title: t('saleOut.deleteDialog.successTitle'),
        text: t('saleOut.deleteDialog.successMessage'),
        timer: 1800,
        showConfirmButton: false,
      })
      setQuery((currentQuery) => ({ ...currentQuery, page: 1 }))
      setRetryCount((count) => count + 1)
    } catch (error: unknown) {
      const message = error instanceof ApiRequestError && error.detail
        ? error.detail
        : error instanceof Error
          ? error.message
          : t('saleOut.deleteDialog.unexpectedError')

      void Swal.fire({
        icon: 'error',
        title: t('saleOut.deleteDialog.errorTitle'),
        text: message,
        confirmButtonText: t('saleOut.ok'),
      })
    }
  }

  const handleDownloadTemplate = async () => {
    try {
      const blob = await downloadSaleOutTemplate()
      const fileUrl = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = fileUrl
      link.download = 'sale-out-template.xlsx'
      link.click()
      window.setTimeout(() => URL.revokeObjectURL(fileUrl), 0)
    } catch {
      void Swal.fire({
        icon: 'error',
        title: t('saleOut.downloadTemplateError'),
        confirmButtonText: t('saleOut.ok'),
      })
    }
  }

  const rows = result?.items ?? []
  const totalPages = result?.totalPages ?? 0
  const currentPage = rows.length > 0 ? query.page : 0

  return (
    <main className="product-page">
      <div className="product-page__content">
        <header className="product-page__header">
          <div>
            <div className="product-page__eyebrow">{t('saleOut.eyebrow')}</div>
            <Typography component="h1" className="product-page__title">
              {t('saleOut.title')}
            </Typography>
            <Typography className="product-page__subtitle">
              {t('saleOut.subtitle')}
            </Typography>
          </div>
          <div className="product-page__header-tools">
            <div className="product-language" role="group" aria-label={t('saleOut.language')}>
              <Button
                size="small"
                aria-pressed={i18n.resolvedLanguage === 'vi'}
                className={`product-language__button${i18n.resolvedLanguage === 'vi' ? ' is-active' : ''}`}
                onClick={() => void i18n.changeLanguage('vi')}
              >
                VI
              </Button>
              <Button
                size="small"
                aria-pressed={i18n.resolvedLanguage === 'en'}
                className={`product-language__button${i18n.resolvedLanguage === 'en' ? ' is-active' : ''}`}
                onClick={() => void i18n.changeLanguage('en')}
              >
                EN
              </Button>
            </div>
            <div className="product-page__count">
              <span className="product-page__count-dot" />
              {result
                ? t('saleOut.recordCount', { count: result.totalItems.toLocaleString(i18n.language) })
                : errorMessage
                  ? t('saleOut.dataUnavailable')
                  : t('saleOut.loadingData')}
            </div>
          </div>
        </header>

        <Paper component="section" elevation={0} className="product-toolbar">
          <form className="product-search" onSubmit={handleSearch}>
            <label className="product-search__label" htmlFor="sale-out-search-field">
              {t('saleOut.searchBy')}
            </label>
            <FormControl size="small" className="product-search__select">
              <Select
                id="sale-out-search-field"
                value={searchField}
                onChange={(event) => setSearchField(event.target.value as SaleOutSearchField)}
                inputProps={{ 'aria-label': t('saleOut.searchField') }}
              >
                {SALE_OUT_SEARCH_FIELDS.map((field) => (
                  <MenuItem key={field.value} value={field.value}>{t(field.labelKey)}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              size="small"
              value={searchValue}
              onChange={(event) => setSearchValue(event.target.value)}
              placeholder={searchField === 'OrderDate'
                ? t('saleOut.searchDatePlaceholder')
                : t('saleOut.searchPlaceholder')}
              className="product-search__input"
              slotProps={{ htmlInput: { 'aria-label': t('saleOut.searchValue') } }}
            />
            <Button
              type="submit"
              variant="contained"
              startIcon={<SearchIcon />}
              className="product-button product-button--search"
            >
              {t('saleOut.search')}
            </Button>
          </form>
          <div className="product-toolbar__actions sale-out-toolbar__actions">
            <Button
              type="button"
              variant="contained"
              startIcon={<AddOutlinedIcon />}
              className="product-button product-button--primary"
              onClick={() => setIsCreateDialogOpen(true)}
            >
              {t('saleOut.actions.add')}
            </Button>
            <Button
              type="button"
              variant="outlined"
              startIcon={<DownloadOutlinedIcon />}
              className="product-button product-button--quiet"
              onClick={() => void handleDownloadTemplate()}
            >
              {t('saleOut.actions.downloadTemplate')}
            </Button>
            <Button
              type="button"
              variant="outlined"
              startIcon={<UploadFileOutlinedIcon />}
              className="product-button product-button--quiet"
              onClick={() => setIsImportDialogOpen(true)}
            >
              {t('saleOut.actions.upload')}
            </Button>
            <Button
              type="button"
              variant="outlined"
              startIcon={<LocalPrintshopOutlinedIcon />}
              className="product-button product-button--quiet"
              onClick={() => setIsPrintDialogOpen(true)}
            >
              {t('saleOut.actions.print')}
            </Button>
            <Button
              type="button"
              variant="outlined"
              startIcon={<SummarizeOutlinedIcon />}
              className="product-button product-button--quiet"
            >
              {t('saleOut.actions.revenueReport')}
            </Button>
          </div>
        </Paper>

        <Paper elevation={0} className="product-list">
          <div className="product-list__toolbar">
            <Box className="product-list__size-controls">
              <Typography className="product-list__label">{t('saleOut.show')}</Typography>
              <FormControl size="small" className="product-list__page-size">
                <Select
                  value={pageSizeChoice}
                  onChange={(event) => handlePageSizeChange(event.target.value)}
                  inputProps={{ 'aria-label': t('saleOut.pageSize') }}
                >
                  {PAGE_SIZE_OPTIONS.map((option) => (
                    <MenuItem key={option} value={String(option)}>{option}</MenuItem>
                  ))}
                  <MenuItem value="custom">{t('saleOut.custom')}</MenuItem>
                </Select>
              </FormControl>
              <Typography className="product-list__label">{t('saleOut.rowsPerPage')}</Typography>
              {pageSizeChoice === 'custom' && (
                <>
                  <TextField
                    size="small"
                    type="number"
                    value={customPageSize}
                    onChange={(event) => setCustomPageSize(event.target.value)}
                    slotProps={{ htmlInput: { min: 1, max: MAX_PAGE_SIZE, 'aria-label': t('saleOut.customPageSize') } }}
                    className="product-list__custom-size"
                  />
                  <Button size="small" onClick={applyCustomPageSize} className="product-list__apply-size">
                    {t('saleOut.apply')}
                  </Button>
                </>
              )}
            </Box>
            <Typography className="product-list__total">
              {result ? t('saleOut.totalResults', { count: result.totalItems.toLocaleString(i18n.language) }) : ' '}
            </Typography>
          </div>

          <TableContainer className="product-table-container sale-out-table-container">
            <Table size="medium" aria-label={t('saleOut.title')}>
              <colgroup>
                <col className="sale-out-column--number" />
                <col className="sale-out-column--actions" />
                <col className="sale-out-column--sale-out-no" />
                <col className="sale-out-column--customer-po" />
                <col className="sale-out-column--order-date" />
                <col className="sale-out-column--customer" />
                <col className="sale-out-column--product-count" />
                <col className="sale-out-column--amount" />
              </colgroup>
              <TableHead>
                <TableRow>
                  <TableCell align='center' className="product-table__number">{t('saleOut.columns.number')}</TableCell>
                  <TableCell align='center' className="product-table__actions">{t('saleOut.columns.actions')}</TableCell>
                  <TableCell align='center'>{t('saleOut.fields.saleOutNo')}</TableCell>
                  <TableCell align='center'>{t('saleOut.fields.customerPoNo')}</TableCell>
                  <TableCell align='center'>{t('saleOut.fields.orderDate')}</TableCell>
                  <TableCell align='center'>{t('saleOut.fields.customerName')}</TableCell>
                  <TableCell align='center'>{t('saleOut.columns.productCount')}</TableCell>
                  <TableCell align='center'>{t('saleOut.columns.totalAmount')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading && (
                  <TableRow>
                    <TableCell colSpan={TABLE_COLUMN_COUNT} className="product-table__state">
                      <CircularProgress size={22} />
                      <span>{t('saleOut.loadingRows')}</span>
                    </TableCell>
                  </TableRow>
                )}
                {!isLoading && errorMessage && (
                  <TableRow>
                    <TableCell colSpan={TABLE_COLUMN_COUNT} className="product-table__error">
                      <div className="product-table__error-content" role="alert">
                        <span className="product-table__error-icon" aria-hidden="true">!</span>
                        <strong>{t('saleOut.loadError')}</strong>
                        <span>{errorMessage}</span>
                        <Button
                          size="small"
                          variant="outlined"
                          className="product-table__retry"
                          onClick={() => setRetryCount((count) => count + 1)}
                        >
                          {t('saleOut.retry')}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                )}
                {!isLoading && !errorMessage && rows.map((order, index) => {
                  const orderKey = order.saleOutNo ?? order.customerPoNo ?? `order-${index}`
                  const isExpanded = expandedOrders.has(orderKey)
                  return (
                    <Fragment key={orderKey}>
                      <TableRow hover>
                        <TableCell className="product-table__number">
                          <IconButton
                            size="small"
                            aria-label={t(isExpanded ? 'saleOut.columns.collapseOrder' : 'saleOut.columns.expandOrder')}
                            aria-expanded={isExpanded}
                            onClick={() => setExpandedOrders((current) => {
                              const next = new Set(current)
                              if (next.has(orderKey)) next.delete(orderKey)
                              else next.add(orderKey)
                              return next
                            })}
                          >
                            {isExpanded ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
                          </IconButton>
                          {(query.page - 1) * query.pageSize + index + 1}
                        </TableCell>
                        <TableCell className="product-table__actions sale-out-table__actions">
                          <Tooltip title={t('saleOut.actions.edit')}>
                            <IconButton type="button" size="small" className="product-action product-action--edit" aria-label={t('saleOut.actions.edit')} onClick={() => setSaleOutToEdit(order)}>
                              <EditOutlinedIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title={t('saleOut.actions.delete')}>
                            <IconButton type="button" size="small" className="product-action product-action--delete" aria-label={t('saleOut.actions.delete')} onClick={() => void handleDelete(order)}>
                              <DeleteOutlinedIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                        <TableCell align='center' className="product-table__code">{order.saleOutNo || '—'}</TableCell>
                        <TableCell align='center' className="product-table__code">{order.customerPoNo || '—'}</TableCell>
                        <TableCell align='center'>{formatDate(order.orderDate, i18n.language)}</TableCell>
                        <TableCell align='center' className="product-table__name">{order.customerName || '—'}</TableCell>
                        <TableCell align='center'>{formatNumber(order.productCount, i18n.language)}</TableCell>
                        <TableCell align='center' className="sale-out-order-total">{formatCurrency(order.totalAmount, i18n.language)}</TableCell>
                      </TableRow>
                      <TableRow className="sale-out-order-details-row">
                        <TableCell colSpan={TABLE_COLUMN_COUNT} sx={{ p: 0, borderBottom: 0 }}>
                          <Collapse in={isExpanded} timeout="auto" unmountOnExit>
                            <Box className="sale-out-order-details">
                              <Table size="small" aria-label={t('saleOut.columns.productDetails')}>
                                <TableHead>
                                  <TableRow>
                                    <TableCell>{t('saleOut.fields.productCode')}</TableCell>
                                    <TableCell>{t('saleOut.fields.productName')}</TableCell>
                                    <TableCell align="center">{t('saleOut.fields.unit')}</TableCell>
                                    <TableCell align="right">{t('saleOut.fields.quantity')}</TableCell>
                                    <TableCell align="right">{t('saleOut.fields.quantityPerBox')}</TableCell>
                                    <TableCell align="right">{t('saleOut.fields.boxQuantity')}</TableCell>
                                    <TableCell align="right">{t('saleOut.fields.unitPrice')}</TableCell>
                                    <TableCell align="right">{t('saleOut.fields.amount')}</TableCell>
                                  </TableRow>
                                </TableHead>
                                <TableBody>
                                  {order.products.map((product) => (
                                    <TableRow key={product.id}>
                                      <TableCell className="product-table__code">{product.productCode || '—'}</TableCell>
                                      <TableCell className="product-table__name">{product.productName || '—'}</TableCell>
                                      <TableCell align="center">{product.unit || '—'}</TableCell>
                                      <TableCell align="right">{formatNumber(product.quantity, i18n.language)}</TableCell>
                                      <TableCell align="right">{formatNumber(product.quantityPerBox, i18n.language)}</TableCell>
                                      <TableCell align="right">{formatNumber(product.boxQuantity, i18n.language)}</TableCell>
                                      <TableCell align="right">{formatCurrency(product.price, i18n.language)}</TableCell>
                                      <TableCell align="right">{formatCurrency(product.amount, i18n.language)}</TableCell>
                                    </TableRow>
                                  ))}
                                </TableBody>
                              </Table>
                            </Box>
                          </Collapse>
                        </TableCell>
                      </TableRow>
                    </Fragment>
                  )
                })}
                {!isLoading && !errorMessage && rows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={TABLE_COLUMN_COUNT} className="product-table__empty">
                      <div className="product-table__empty-mark">⌕</div>
                      <strong>{t('saleOut.emptyTitle')}</strong>
                      <span>{t('saleOut.emptyMessage')}</span>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>

          <div className="product-pagination">
            <Typography className="product-pagination__summary">
              
            </Typography>
            <div className="product-pagination__controls">
              <Button
                startIcon={<ChevronLeftIcon />}
                className="product-pagination__button"
                disabled={isLoading || query.page <= 1 || totalPages === 0}
                onClick={() => setQuery((currentQuery) => ({ ...currentQuery, page: currentQuery.page - 1 }))}
              >
                {t('saleOut.previous')}
              </Button>
              <span className="product-pagination__page">
                {t('saleOut.pageOf', { page: currentPage, pages: totalPages })}
              </span>
              <Button
                endIcon={<ChevronRightIcon />}
                className="product-pagination__button"
                disabled={isLoading || totalPages === 0 || query.page >= totalPages}
                onClick={() => setQuery((currentQuery) => ({ ...currentQuery, page: currentQuery.page + 1 }))}
              >
                {t('saleOut.next')}
              </Button>
            </div>
          </div>
        </Paper>

        <CreateSaleOutDialog
          open={isCreateDialogOpen}
          onClose={() => setIsCreateDialogOpen(false)}
          onSuccess={() => {
            setQuery((currentQuery) => ({ ...currentQuery, page: 1 }))
            setRetryCount((count) => count + 1)
          }}
        />
        <EditSaleOutDialog
          saleOut={saleOutToEdit}
          onClose={() => setSaleOutToEdit(null)}
          onSuccess={() => setRetryCount((count) => count + 1)}
        />
        <ImportSaleOutDialog
          open={isImportDialogOpen}
          onClose={() => setIsImportDialogOpen(false)}
          onSuccess={() => {
            setQuery((currentQuery) => ({ ...currentQuery, page: 1 }))
            setRetryCount((count) => count + 1)
          }}
        />
        <PrintSaleOutDialog
          open={isPrintDialogOpen}
          onClose={() => setIsPrintDialogOpen(false)}
        />
      </div>
    </main>
  )
}

function formatNumber(value: number | null, language: string) {
  if (value === null) {
    return '—'
  }

  return new Intl.NumberFormat(getLocale(language), { maximumFractionDigits: 3 }).format(value)
}

function formatCurrency(value: number | null, language: string) {
  if (value === null) {
    return '—'
  }

  return new Intl.NumberFormat(getLocale(language), {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatDate(value: string | null, language: string) {
  if (!value) {
    return '—'
  }

  const dateParts = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  const date = dateParts
    ? new Date(Number(dateParts[1]), Number(dateParts[2]) - 1, Number(dateParts[3]))
    : new Date(value)
  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return new Intl.DateTimeFormat(getLocale(language)).format(date)
}

function getLocale(language: string) {
  return language.startsWith('vi') ? 'vi-VN' : 'en-US'
}

function getSaleOutErrorMessage(error: unknown, translate: (key: string) => string) {
  if (error instanceof ApiRequestError) {
    if (error.status === 400) {
      return translate('saleOut.invalidSearch')
    }

    if (error.status === 404) {
      return translate('saleOut.apiNotFound')
    }

    if (error.status >= 500) {
      return translate('saleOut.serverError')
    }
  }

  return error instanceof TypeError
    ? translate('saleOut.connectionError')
    : translate('saleOut.unexpectedError')
}
