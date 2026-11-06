import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutlineOutlined'
import { Button, CircularProgress, Paper, Typography } from '@mui/material'
import { useAppDispatch, useAppSelector } from '../../../app/hooks'
import { checkDatabaseConnection } from '../model/apiHealthSlice'

export default function ApiHealthStatus() {
  const { t } = useTranslation()
  const dispatch = useAppDispatch()
  const { status, database } = useAppSelector((state) => state.apiHealth)

  useEffect(() => {
    void dispatch(checkDatabaseConnection())
  }, [dispatch])

  const isLoading = status === 'idle' || status === 'loading'
  const isConnected = status === 'succeeded'

  return (
    <main className="product-page">
      <div className="product-page__content">
        <header className="product-page__header">
          <div>
            <div className="product-page__eyebrow">{t('apiHealth.eyebrow')}</div>
            <Typography component="h1" className="product-page__title">
              {t('apiHealth.title')}
            </Typography>
            <Typography className="product-page__subtitle">
              {t('apiHealth.intro')}
            </Typography>
          </div>
        </header>

        <Paper elevation={0} className="product-list" sx={{ p: 3, maxWidth: 680 }}>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            {t('apiHealth.endpoint')}
          </Typography>
          <Typography component="code" sx={{ display: 'block', mb: 3 }}>
            GET /api/database/ping
          </Typography>

          {isLoading ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <CircularProgress size={20} />
              <Typography>{t('apiHealth.checking')}</Typography>
            </div>
          ) : (
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }} role="status">
              {isConnected
                ? <CheckCircleOutlineIcon color="success" />
                : <ErrorOutlineIcon color="error" />}
              <Typography>
                {isConnected ? t('apiHealth.connected') : t('apiHealth.disconnected')}
              </Typography>
            </div>
          )}

          {isConnected && (
            <Typography sx={{ mt: 2 }}>
              {t('apiHealth.response')}: {database}
            </Typography>
          )}

          {!isLoading && !isConnected && (
            <Button
              type="button"
              variant="outlined"
              className="product-button product-button--quiet"
              sx={{ mt: 2 }}
              onClick={() => void dispatch(checkDatabaseConnection())}
            >
              {t('saleOut.retry')}
            </Button>
          )}
        </Paper>
      </div>
    </main>
  )
}
