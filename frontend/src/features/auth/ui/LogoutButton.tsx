import { Button } from '@/shared/ui'
import { useLogout } from '../model/mutations'

export function LogoutButton() {
  const logout = useLogout()

  return (
    <Button variant="ghost" onClick={() => logout.mutate()} disabled={logout.isPending}>
      Выйти
    </Button>
  )
}
