import { useState, type FormEvent } from 'react'
import type { Me } from '@/entities/session'
import { toFormErrors } from '@/shared/api'
import { Button, TextField } from '@/shared/ui'
import { useRegister } from '../model/mutations'
import styles from './AuthForm.module.css'

type RegisterFormProps = {
  onSuccess?: (me: Me) => void
}

export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const register = useRegister()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const errors = toFormErrors(register.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    register.mutate({ displayName, email, password }, { onSuccess })
  }

  return (
    <form className={styles.form} onSubmit={submit} noValidate>
      <TextField
        label="Как вас называть"
        autoComplete="name"
        value={displayName}
        onChange={(event) => setDisplayName(event.target.value)}
        error={errors.fields.displayName}
      />
      <TextField
        label="Email"
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        error={errors.fields.email}
      />
      <TextField
        label="Пароль, не короче 8 символов"
        type="password"
        autoComplete="new-password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        error={errors.fields.password}
      />
      {errors.message && (
        <p className={styles.error} role="alert">
          {errors.message}
        </p>
      )}
      <Button type="submit" variant="primary" className={styles.submit} disabled={register.isPending}>
        Зарегистрироваться
      </Button>
    </form>
  )
}
