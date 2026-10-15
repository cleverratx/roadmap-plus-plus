import { useState, type FormEvent } from 'react'
import type { Me } from '@/entities/session'
import { Button, TextField } from '@/shared/ui'
import { toFormErrors } from '../model/form-errors'
import { useLogin } from '../model/mutations'
import styles from './AuthForm.module.css'

type LoginFormProps = {
  onSuccess?: (me: Me) => void
}

export function LoginForm({ onSuccess }: LoginFormProps) {
  const login = useLogin()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const errors = toFormErrors(login.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    login.mutate({ email, password }, { onSuccess })
  }

  return (
    <form className={styles.form} onSubmit={submit} noValidate>
      <TextField
        label="Email"
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        error={errors.fields.email}
      />
      <TextField
        label="Пароль"
        type="password"
        autoComplete="current-password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        error={errors.fields.password}
      />
      {errors.message && (
        <p className={styles.error} role="alert">
          {errors.message}
        </p>
      )}
      <Button type="submit" variant="primary" className={styles.submit} disabled={login.isPending}>
        Войти
      </Button>
    </form>
  )
}
