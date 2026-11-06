import { useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft'
import ChevronRightIcon from '@mui/icons-material/ChevronRight'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import SearchIcon from '@mui/icons-material/Search'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import {
  Box,
  Button,
  CircularProgress,
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
import Swal from 'sweetalert2'
import { ApiRequestError } from '../../../shared/api/httpClient'
import { readSearchablePageState, writeSearchablePageState } from '../../../shared/routing/searchablePageState'
import { deleteProduct, downloadProductTemplate, fetchProducts } from '../api/productsApi'
import {
  PRODUCT_SEARCH_FIELDS,
  type ProductListItem,
  type ProductSearchField,
  type ProductSearchResponse,
} from '../model/productTypes'
import CreateProductDialog from './CreateProductDialog'
import ImportProductDialog from './ImportProductDialog'
import UpdateProductDialog from './UpdateProductDialog'

const DEFAULT_PAGE_SIZE = 10
const MAX_PAGE_SIZE = 200
const PAGE_SIZE_OPTIONS = [5, 10, 20]

type ProductQuery = {
  page: number
  pageSize: number
  searchField: ProductSearchField
  searchValue: string
}

export default function ProductMasterView() {
  const { t, i18n } = useTranslation()
  const [query, setQuery] = useState<ProductQuery>(() => readSearchablePageState<ProductSearchField>(
    { page: 1, pageSize: DEFAULT_PAGE_SIZE, searchField: 'ProductName', searchValue: '' },
    PRODUCT_SEARCH_FIELDS.map((field) => field.value),
    MAX_PAGE_SIZE,
  ))
  const [searchField, setSearchField] = useState<ProductSearchField>(query.searchField)
  const [searchValue, setSearchValue] = useState(query.searchValue)
  const [pageSizeChoice, setPageSizeChoice] = useState(() =>
    PAGE_SIZE_OPTIONS.includes(query.pageSize) ? String(query.pageSize) : 'custom',
  )
  const [customPageSize, setCustomPageSize] = useState(() =>
    PAGE_SIZE_OPTIONS.includes(query.pageSize) ? '' : String(query.pageSize),
  )
  const [result, setResult] = useState<ProductSearchResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [retryCount, setRetryCount] = useState(0)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isImportOpen, setIsImportOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<ProductListItem | null>(null)
  const [isEditOpen, setIsEditOpen] = useState(false)

  useEffect(() => {
    writeSearchablePageState(query, {
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE,
      searchField: 'ProductName',
      searchValue: '',
    })
  }, [query])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setErrorMessage('')

    void fetchProducts(query, controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) {
          setResult(data)
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setResult(null)
          setErrorMessage(getProductErrorMessage(error, query.searchField, t))
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
        title: t('productMaster.invalidPageSizeTitle'),
        text: t('productMaster.invalidPageSizeMessage', { max: MAX_PAGE_SIZE }),
        confirmButtonText: t('productMaster.ok'),
      })
      return
    }

    setQuery((currentQuery) => ({ ...currentQuery, page: 1, pageSize: value }))
  }

  const handleDeleteProduct = async (product: ProductListItem) => {
    if (!product.id) {
      return
    }

    const confirmResult = await Swal.fire({
      icon: 'warning',
      title: t('productMaster.deleteDialog.confirmTitle'),
      text: t('productMaster.deleteDialog.confirmText', {
        name: product.productName ?? product.productCode ?? '',
      }),
      showCancelButton: true,
      confirmButtonText: t('productMaster.deleteDialog.yes'),
      cancelButtonText: t('productMaster.deleteDialog.no'),
      confirmButtonColor: '#b45f55',
      cancelButtonColor: '#68756c',
    })

    if (!confirmResult.isConfirmed) {
      // Chọn "Không": trở lại màn Master Sản phẩm
      return
    }

    try {
      await deleteProduct(product.id)

      void Swal.fire({
        icon: 'success',
        title: t('productMaster.deleteDialog.successTitle'),
        text: t('productMaster.deleteDialog.successMessage'),
        timer: 2000,
        showConfirmButton: false,
      })

      setRetryCount((count) => count + 1)
    } catch (error: unknown) {
      if (error instanceof ApiRequestError) {
        if (error.status === 409 || error.status === 400) {
          void Swal.fire({
            icon: 'warning',
            title: t('productMaster.deleteDialog.blockedTitle'),
            text:  t('productMaster.deleteDialog.blockedMessage'),
            confirmButtonText: t('productMaster.ok'),
          })
          return
        }

        if (error.status === 404) {
          void Swal.fire({
            icon: 'error',
            title: t('productMaster.deleteDialog.errorTitle'),
            text: error.detail || t('productMaster.updateDialog.notFound'),
            confirmButtonText: t('productMaster.ok'),
          })
          return
        }
      }

      void Swal.fire({
        icon: 'error',
        title: t('productMaster.deleteDialog.errorTitle'),
        text: error instanceof Error ? error.message : t('productMaster.unexpectedError'),
        confirmButtonText: t('productMaster.ok'),
      })
    }
  }

  const downloadTemplate = async () => {
    try {
      const blob = await downloadProductTemplate()
      const fileUrl = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = fileUrl
      link.download = 'master-product-template.xlsx'
      link.click()
      window.setTimeout(() => URL.revokeObjectURL(fileUrl), 0)
    } catch {
      void Swal.fire({
        icon: 'error',
        title: t('productMaster.downloadTemplateError'),
        confirmButtonText: t('productMaster.ok'),
      })
    }
  }

  const changePage = (page: number) => {
    setQuery((currentQuery) => ({ ...currentQuery, page }))
  }

  const rows = result?.items ?? []
  const totalPages = result?.totalPages ?? 0
  const currentPage = rows.length > 0 ? query.page : 0

  return (
    <main className="product-page">
      <div className="product-page__content">
        <header className="product-page__header">
          <div>
            <div className="product-page__eyebrow">{t('productMaster.eyebrow')}</div>
            <Typography component="h1" className="product-page__title">
              {t('productMaster.title')}
            </Typography>
            <Typography className="product-page__subtitle">
              {t('productMaster.subtitle')}
            </Typography>
          </div>
          <div className="product-page__header-tools">
            <div className="product-language" role="group" aria-label={t('productMaster.language')}>
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
                ? t('productMaster.productCount', { count: result.totalItems.toLocaleString(i18n.language) })
                : errorMessage
                  ? t('productMaster.dataUnavailable')
                  : t('productMaster.loadingData')}
            </div>
          </div>
        </header>

        <Paper component="section" elevation={0} className="product-toolbar">
          <form className="product-search" onSubmit={handleSearch}>
            <label className="product-search__label" htmlFor="product-search-field">
              {t('productMaster.searchBy')}
            </label>
            <FormControl size="small" className="product-search__select">
              <Select
                id="product-search-field"
                value={searchField}
                onChange={(event) => setSearchField(event.target.value as ProductSearchField)}
                inputProps={{ 'aria-label': t('productMaster.searchField') }}
              >
                {PRODUCT_SEARCH_FIELDS.map((field) => (
                  <MenuItem key={field.value} value={field.value}>{t(field.labelKey)}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              size="small"
              value={searchValue}
              onChange={(event) => setSearchValue(event.target.value)}
              placeholder={t('productMaster.searchPlaceholder')}
              className="product-search__input"
              slotProps={{ htmlInput: { 'aria-label': t('productMaster.searchValue') } }}
            />
            <Button
              type="submit"
              variant="contained"
              startIcon={<SearchIcon />}
              className="product-button product-button--search"
            >
              {t('productMaster.search')}
            </Button>
          </form>

          <div className="product-toolbar__actions">
            <Button
              variant="contained"
              startIcon={<AddOutlinedIcon />}
              className="product-button product-button--primary"
              onClick={() => setIsCreateOpen(true)}
            >
              {t('productMaster.add')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<DownloadOutlinedIcon />}
              className="product-button product-button--quiet"
              onClick={() => void downloadTemplate()}
            >
              {t('productMaster.downloadTemplate')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<UploadFileOutlinedIcon />}
              className="product-button product-button--quiet"
              onClick={() => setIsImportOpen(true)}
            >
              {t('productMaster.upload')}
            </Button>
          </div>
        </Paper>

        <Paper elevation={0} className="product-list">
          <div className="product-list__toolbar">
            <Box className="product-list__size-controls">
              <Typography className="product-list__label">{t('productMaster.show')}</Typography>
              <FormControl size="small" className="product-list__page-size">
                <Select
                  value={pageSizeChoice}
                  onChange={(event) => handlePageSizeChange(event.target.value)}
                  inputProps={{ 'aria-label': t('productMaster.pageSize') }}
                >
                  {PAGE_SIZE_OPTIONS.map((option) => (
                    <MenuItem key={option} value={String(option)}>{option}</MenuItem>
                  ))}
                  <MenuItem value="custom">{t('productMaster.custom')}</MenuItem>
                </Select>
              </FormControl>
              <Typography className="product-list__label">{t('productMaster.rowsPerPage')}</Typography>
              {pageSizeChoice === 'custom' && (
                <>
                  <TextField
                    size="small"
                    type="number"
                    value={customPageSize}
                    onChange={(event) => setCustomPageSize(event.target.value)}
                    slotProps={{ htmlInput: { min: 1, max: MAX_PAGE_SIZE, 'aria-label': t('productMaster.customPageSize') } }}
                    className="product-list__custom-size"
                  />
                  <Button size="small" onClick={applyCustomPageSize} className="product-list__apply-size">
                    {t('productMaster.apply')}
                  </Button>
                </>
              )}
            </Box>
            <Typography className="product-list__total">
              {result ? t('productMaster.totalResults', { count: result.totalItems.toLocaleString(i18n.language) }) : ' '}
            </Typography>
          </div>

          <TableContainer className="product-table-container product-master-table-container">
            <Table size="medium" aria-label={t('productMaster.title')}>
              <colgroup>
                <col className="product-master-column--number" />
                <col className="product-master-column--actions" />
                <col className="product-master-column--product-code" />
                <col className="product-master-column--product-name" />
                <col className="product-master-column--unit" />
                <col className="product-master-column--specification" />
                <col className="product-master-column--quantity-per-box" />
                <col className="product-master-column--product-weight" />
                <col className="product-master-column--status" />
              </colgroup>
              <TableHead>
                <TableRow>
                  <TableCell className="product-table__number">{t('productMaster.columns.number')}</TableCell>
                  <TableCell className="product-table__actions">{t('productMaster.columns.actions')}</TableCell>
                  <TableCell>{t('productMaster.fields.productCode')}</TableCell>
                  <TableCell>{t('productMaster.fields.productName')}</TableCell>
                  <TableCell>{t('productMaster.fields.unit')}</TableCell>
                  <TableCell>{t('productMaster.fields.specification')}</TableCell>
                  <TableCell align="center">{t('productMaster.fields.quantityPerBox')}</TableCell>
                  <TableCell align="center">{t('productMaster.fields.productWeight')}</TableCell>
                  <TableCell align="center">{t('productMaster.fields.status')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading && (
                  <TableRow>
                    <TableCell colSpan={9} className="product-table__state">
                      <CircularProgress size={22} />
                      <span>{t('productMaster.loadingProducts')}</span>
                    </TableCell>
                  </TableRow>
                )}
                {!isLoading && errorMessage && (
                  <TableRow>
                    <TableCell colSpan={9} className="product-table__error">
                      <div className="product-table__error-content" role="alert">
                        <span className="product-table__error-icon" aria-hidden="true">!</span>
                        <strong>{t('productMaster.loadError')}</strong>
                        <span>{errorMessage}</span>
                        <Button
                          size="small"
                          variant="outlined"
                          className="product-table__retry"
                          onClick={() => setRetryCount((count) => count + 1)}
                        >
                          {t('productMaster.retry')}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                )}
                {!isLoading && !errorMessage && rows.map((product, index) => (
                  <TableRow key={`${product.productCode ?? 'product'}-${index}`} hover>
                    <TableCell className="product-table__number">
                      {(query.page - 1) * query.pageSize + index + 1}
                    </TableCell>
                    <TableCell className="product-table__actions">
                      <Tooltip title={t('productMaster.edit')}>
                        <IconButton
                          size="small"
                          className="product-action product-action--edit"
                          aria-label={`${t('productMaster.edit')} ${product.productName ?? t('productMaster.product')}`}
                          onClick={() => {
                            setEditingProduct(product)
                            setIsEditOpen(true)
                          }}
                        >
                          <EditOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={t('productMaster.delete')}>
                        <IconButton
                          size="small"
                          className="product-action product-action--delete"
                          aria-label={`${t('productMaster.delete')} ${product.productName ?? t('productMaster.product')}`}
                          onClick={() => void handleDeleteProduct(product)}
                        >
                          <DeleteOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                    <TableCell className="product-table__code">{product.productCode || '—'}</TableCell>
                    <TableCell className="product-table__name">{product.productName || '—'}</TableCell>
                    <TableCell>{product.unit || '—'}</TableCell>
                    <TableCell>{product.specification || '—'}</TableCell>
                    <TableCell align="center">{formatDecimal(product.quantityPerBox, i18n.language)}</TableCell>
                    <TableCell align="center">{formatDecimal(product.productWeight, i18n.language)}</TableCell>
                    <TableCell align="center">
                      <span className={`product-status ${product.isActive ? 'product-status--active' : 'product-status--inactive'}`}>
                        {product.isActive ? t('productMaster.active') : t('productMaster.inactive')}
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
                {!isLoading && !errorMessage && rows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} className="product-table__empty">
                      <div className="product-table__empty-mark">⌕</div>
                      <strong>{t('productMaster.emptyTitle')}</strong>
                      <span>{t('productMaster.emptyMessage')}</span>
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
                onClick={() => changePage(query.page - 1)}
              >
                {t('productMaster.previous')}
              </Button>
              <span className="product-pagination__page">
                {t('productMaster.pageOf', { page: currentPage, pages: totalPages })}
              </span>
              <Button
                endIcon={<ChevronRightIcon />}
                className="product-pagination__button"
                disabled={isLoading || totalPages === 0 || query.page >= totalPages}
                onClick={() => changePage(query.page + 1)}
              >
                {t('productMaster.next')}
              </Button>
            </div>
          </div>
        </Paper>
      </div>

      <ImportProductDialog
        open={isImportOpen}
        onClose={() => setIsImportOpen(false)}
        onSuccess={() => {
          setQuery((currentQuery) => ({ ...currentQuery, page: 1 }))
          setRetryCount((count) => count + 1)
        }}
      />

      <CreateProductDialog
        open={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onSuccess={() => {
          setQuery((currentQuery) => ({ ...currentQuery, page: 1 }))
          setRetryCount((count) => count + 1)
        }}
      />

      <UpdateProductDialog
        open={isEditOpen}
        product={editingProduct}
        onClose={() => {
          setIsEditOpen(false)
          setEditingProduct(null)
        }}
        onSuccess={() => {
          setRetryCount((count) => count + 1)
        }}
      />
    </main>
  )
}

function formatDecimal(value: number | null, language: string) {
  if (value === null) {
    return '—'
  }

  const locale = language.startsWith('vi') ? 'vi-VN' : 'en-US'
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 3 }).format(value)
}

function getProductErrorMessage(
  error: unknown,
  searchField: ProductSearchField,
  translate: (key: string) => string,
) {
  if (error instanceof ApiRequestError) {
    if (error.status === 400) {
      if (searchField === 'QuantityPerBox') {
        return translate('productMaster.invalidQuantitySearch')
      }

      if (searchField === 'ProductWeight') {
        return translate('productMaster.invalidWeightSearch')
      }

      return translate('productMaster.invalidSearch')
    }

    if (error.status === 404) {
      return translate('productMaster.apiNotFound')
    }

    if (error.status >= 500) {
      return translate('productMaster.serverError')
    }
  }

  if (error instanceof TypeError) {
    return translate('productMaster.connectionError')
  }

  return translate('productMaster.unexpectedError')
}
