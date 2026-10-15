import { Navigate, Outlet, useSearchParams } from 'react-router'
import { useMe } from '@/entities/session'
import { safeReturnTo } from '@/shared/lib'

export function GuestOnly() {
  const me = useMe()
  const [searchParams] = useSearchParams()

  if (me.data) {
    return <Navigate to={safeReturnTo(searchParams.get('returnTo'))} replace />
  }

  return <Outlet />
}
