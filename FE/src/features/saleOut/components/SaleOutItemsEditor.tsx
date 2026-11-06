import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import Autocomplete from '@mui/material/Autocomplete'
import {
  Button,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
} from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { ProductListItem } from '../../productMaster/model/productTypes'

export type SaleOutItemDraft = {
  key: string
  id?: string
  productId: string
  quantity: string
  quantityPerBox: string
  price: string
}

export type SaleOutItemDraftErrors = Partial<Record<'productId' | 'quantity' | 'quantityPerBox' | 'price', string>>
export type SaleOutItemEditableField = keyof SaleOutItemDraftErrors

interface SaleOutItemsEditorProps {
  items: SaleOutItemDraft[]
  products: ProductListItem[]
  errors: Record<string, SaleOutItemDraftErrors>
  disabled: boolean
  loadingProducts: boolean
  onChange: (key: string, field: SaleOutItemEditableField, value: string) => void
  onAdd: () => void
  onRemove: (key: string) => void
}

export default function SaleOutItemsEditor({
  items,
  products,
  errors,
  disabled,
  loadingProducts,
  onChange,
  onAdd,
  onRemove,
}: SaleOutItemsEditorProps) {
  const { t } = useTranslation()

  return (
    <div className="sale-out-items-editor">
      <Button
        type="button"
        size="small"
        variant="contained"
        startIcon={<AddOutlinedIcon />}
        onClick={onAdd}
        disabled={disabled}
        className="product-button product-button--primary"
      >
        {t('saleOut.createDialog.addProduct')}
      </Button>
      <Table size="small" className="sale-out-items-editor__table">
        <TableHead>
          <TableRow>
            <TableCell align="center">#</TableCell>
            <TableCell align="center">{t('saleOut.createDialog.removeProduct')}</TableCell>
            <TableCell>{t('saleOut.createDialog.product')}</TableCell>
            <TableCell>{t('saleOut.fields.unit')}</TableCell>
            <TableCell>{t('saleOut.fields.quantityPerBox')}</TableCell>
            <TableCell>{t('saleOut.fields.unitPrice')}</TableCell>
            <TableCell>{t('saleOut.fields.quantity')}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item, index) => {
            const selectedProduct = products.find((product) => product.id === item.productId) ?? null
            const rowErrors = errors[item.key] ?? {}
            const productOptions = products.filter((product) => (
              product.id === item.productId
              || !items.some((other) => other.key !== item.key && other.productId === product.id)
            ))

            return (
              <TableRow key={item.key}>
                <TableCell align="center">{index + 1}</TableCell>
                <TableCell align="center">
                  <IconButton
                    type="button"
                    size="small"
                    className="product-action product-action--delete"
                    aria-label={t('saleOut.createDialog.removeProduct')}
                    onClick={() => onRemove(item.key)}
                    disabled={disabled || items.length === 1}
                  >
                    <DeleteOutlinedIcon fontSize="small" />
                  </IconButton>
                </TableCell>
                <TableCell>
                  <Autocomplete
                    size="small"
                    options={productOptions}
                    value={selectedProduct}
                    loading={loadingProducts}
                    disabled={disabled || loadingProducts}
                    getOptionLabel={formatProductOption}
                    isOptionEqualToValue={(option, value) => option.id === value.id}
                    onChange={(_, product) => {
                      onChange(item.key, 'productId', product?.id ?? '')
                      onChange(item.key, 'quantityPerBox', product?.quantityPerBox == null ? '' : String(product.quantityPerBox))
                    }}
                    renderInput={(params) => (
                      <TextField
                        {...params}
                        error={Boolean(rowErrors.productId)}
                        helperText={rowErrors.productId}
                        placeholder={t('saleOut.createDialog.selectProduct')}
                      />
                    )}
                  />
                </TableCell>
                <TableCell>
                  <TextField size="small" value={selectedProduct?.unit ?? ''} disabled fullWidth />
                </TableCell>
                <TableCell>
                  <TextField
                    size="small"
                    type="number"
                    value={item.quantityPerBox}
                    onChange={(event) => onChange(item.key, 'quantityPerBox', event.target.value)}
                    error={Boolean(rowErrors.quantityPerBox)}
                    helperText={rowErrors.quantityPerBox}
                    disabled={disabled || !selectedProduct}
                    slotProps={{ htmlInput: { min: 1, step: 1 } }}
                  />
                </TableCell>
                <TableCell>
                  <TextField
                    size="small"
                    type="number"
                    value={item.price}
                    onChange={(event) => onChange(item.key, 'price', event.target.value)}
                    error={Boolean(rowErrors.price)}
                    helperText={rowErrors.price}
                    disabled={disabled}
                    slotProps={{ htmlInput: { min: 0, step: 1 } }}
                  />
                </TableCell>
                <TableCell>
                  <TextField
                    size="small"
                    type="number"
                    value={item.quantity}
                    onChange={(event) => onChange(item.key, 'quantity', event.target.value)}
                    error={Boolean(rowErrors.quantity)}
                    helperText={rowErrors.quantity}
                    disabled={disabled}
                    slotProps={{ htmlInput: { min: 1, step: 1 } }}
                  />
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}

function formatProductOption(product: ProductListItem): string {
  const productCode = product.productCode?.trim() ?? ''
  const productName = product.productName?.trim() ?? ''
  return productName ? `${productCode} (${productName})` : productCode
}
