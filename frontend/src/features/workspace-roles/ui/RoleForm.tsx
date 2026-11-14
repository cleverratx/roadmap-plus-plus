import { useState, type FormEvent } from 'react'
import type { WorkspaceRole } from '@/entities/workspace'
import { toFormErrors } from '@/shared/api'
import { teamColors } from '@/shared/config'
import { Button, ColorSwatches, TextField } from '@/shared/ui'
import styles from './Roles.module.css'

export type RoleDraft = {
  name: string
  color: string
}

type RoleFormProps = {
  role?: WorkspaceRole
  defaultColor: string
  pending: boolean
  error: unknown
  onSubmit: (draft: RoleDraft) => void
  onCancel?: () => void
}

export function RoleForm({ role, defaultColor, pending, error, onSubmit, onCancel }: RoleFormProps) {
  const [name, setName] = useState(role?.name ?? '')
  const [color, setColor] = useState(role?.color ?? defaultColor)
  const errors = toFormErrors(error)
  const colors = teamColors.includes(color as (typeof teamColors)[number]) ? teamColors : [color, ...teamColors]

  function submit(event: FormEvent) {
    event.preventDefault()
    onSubmit({ name, color })
  }

  return (
    <form className={styles.form} onSubmit={submit} noValidate aria-label={role ? `Правка роли "${role.name}"` : 'Новая роль'}>
      <TextField
        label="Название роли"
        value={name}
        onChange={(event) => setName(event.target.value)}
        error={errors.fields.name}
        placeholder="Например, Жюри"
      />
      <ColorSwatches label="Цвет роли" colors={colors} value={color} onChange={setColor} />
      {errors.message && (
        <p className={styles.error} role="alert">
          {errors.message}
        </p>
      )}
      <div className={styles.formActions}>
        <Button type="submit" variant="primary" disabled={pending}>
          {role ? 'Сохранить' : 'Добавить роль'}
        </Button>
        {onCancel && <Button onClick={onCancel}>Отмена</Button>}
      </div>
    </form>
  )
}
