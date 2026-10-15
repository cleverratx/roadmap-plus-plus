import { Link, useSearchParams } from 'react-router'
import { LoginForm } from '@/features/auth'
import { paths } from '@/shared/config'
import { FormCard } from '@/shared/ui'

export function LoginPage() {
  const [searchParams] = useSearchParams()

  return (
    <FormCard
      title="Вход"
      footer={
        <>
          Ещё нет аккаунта? <Link to={`${paths.register}?${searchParams.toString()}`}>Зарегистрироваться</Link>
        </>
      }
    >
      <LoginForm />
    </FormCard>
  )
}
