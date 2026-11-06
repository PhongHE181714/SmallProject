import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@mui/material'
import ProductMasterPage from './pages/ProductMasterPage'
import SaleOutPage from './pages/SaleOutPage'

type AppPage = 'productMaster' | 'saleOut'

export default function App() {
  const { t } = useTranslation()
  const [activePage, setActivePage] = useState<AppPage>(() => getPageFromPath(window.location.pathname))

  useEffect(() => {
    const syncPageFromPath = () => {
      const page = getPageFromPath(window.location.pathname)
      const pagePath = getPathFromPage(page)
      if (normalizePath(window.location.pathname) !== pagePath) {
        window.history.replaceState({}, '', `${pagePath}${window.location.search}`)
      }
      setActivePage(page)
    }

    syncPageFromPath()
    window.addEventListener('popstate', syncPageFromPath)
    return () => window.removeEventListener('popstate', syncPageFromPath)
  }, [])

  const navigateToPage = (page: AppPage) => {
    const pagePath = getPathFromPage(page)
    if (normalizePath(window.location.pathname) !== pagePath) {
      window.history.pushState({}, '', pagePath)
    }
    setActivePage(page)
  }

  return (
    <>
      <nav className="app-navigation" aria-label={t('appNavigation.label')}>
        <Button
          className={`app-navigation__button${activePage === 'productMaster' ? ' is-active' : ''}`}
          aria-current={activePage === 'productMaster' ? 'page' : undefined}
          onClick={() => navigateToPage('productMaster')}
        >
          {t('appNavigation.productMaster')}
        </Button>
        <Button
          className={`app-navigation__button${activePage === 'saleOut' ? ' is-active' : ''}`}
          aria-current={activePage === 'saleOut' ? 'page' : undefined}
          onClick={() => navigateToPage('saleOut')}
        >
          {t('appNavigation.saleOut')}
        </Button>
      </nav>
      {activePage === 'productMaster' && <ProductMasterPage />}
      {activePage === 'saleOut' && <SaleOutPage />}
    </>
  )
}

function getPageFromPath(pathname: string): AppPage {
  switch (normalizePath(pathname)) {
    case '/sale-out':
      return 'saleOut'
    default:
      return 'productMaster'
  }
}

function getPathFromPage(page: AppPage) {
  switch (page) {
    case 'saleOut':
      return '/sale-out'
    default:
      return '/products'
  }
}

function normalizePath(pathname: string) {
  const normalizedPath = pathname.replace(/\/+$/, '').toLowerCase()
  return normalizedPath || '/'
}
