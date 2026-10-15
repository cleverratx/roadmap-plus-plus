import { Link, useSearchParams } from 'react-router'
import { RegisterForm } from '@/features/auth'
import { paths } from '@/shared/config'
import { FormCard } from '@/shared/ui'

export function RegisterPage() {
  const [searchParams] = useSearchParams()

  return (
    <FormCard
      title="Регистрация"
      footer={
        <>
          Уже есть аккаунт? <Link to={`${paths.login}?${searchParams.toString()}`}>Войти</Link>
        </>
      }
    >
      <RegisterForm />
    </FormCard>
  )
}
