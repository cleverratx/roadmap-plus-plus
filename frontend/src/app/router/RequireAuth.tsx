import { Navigate, Outlet, useLocation } from 'react-router'
import { useMe } from '@/entities/session'
import { paths } from '@/shared/config'
import { Page } from '@/shared/ui'

export function RequireAuth() {
  const me = useMe()
  const location = useLocation()

  if (me.isPending) {
    return <Page title="Загрузка..." />
  }

  if (me.isError) {
    throw me.error
  }

  if (!me.data) {
    const returnTo = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`${paths.login}?returnTo=${returnTo}`} replace />
  }

  return <Outlet />
}
