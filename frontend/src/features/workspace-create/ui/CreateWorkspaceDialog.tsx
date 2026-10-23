import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { workspaceQueryKey, workspacesQueryKey } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { teamColors, teamPath } from '@/shared/config'
import { Button, ColorSwatches, Dialog, TextField, type ButtonVariant } from '@/shared/ui'
import styles from './CreateWorkspaceDialog.module.css'

type CreateWorkspaceDialogProps = {
  triggerVariant?: ButtonVariant
}

export function CreateWorkspaceDialog({ triggerVariant = 'primary' }: CreateWorkspaceDialogProps) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')
  const [color, setColor] = useState<string>(teamColors[0])

  const create = useMutation({
    mutationFn: () => unwrap(api.POST('/api/workspaces', { body: { name, color } })),
    onSuccess: (details) => {
      queryClient.setQueryData(workspaceQueryKey(details.id), details)
      void queryClient.invalidateQueries({ queryKey: workspacesQueryKey, exact: true })
      setOpen(false)
      navigate(teamPath(details.id))
    },
  })
  const errors = toFormErrors(create.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    create.mutate()
  }

  function changeOpen(next: boolean) {
    setOpen(next)
    if (!next) {
      create.reset()
    }
  }

  return (
    <Dialog
      title="Новая команда"
      description="Вы станете её главой и сможете позвать остальных по ссылке."
      open={open}
      onOpenChange={changeOpen}
      trigger={<Button variant={triggerVariant}>Создать команду</Button>}
    >
      <form className={styles.form} onSubmit={submit} noValidate>
        <TextField label="Название" value={name} onChange={(event) => setName(event.target.value)} error={errors.fields.name} />
        <ColorSwatches label="Цвет на таймлайне" colors={teamColors} value={color} onChange={setColor} />
        {errors.message && (
          <p className={styles.error} role="alert">
            {errors.message}
          </p>
        )}
        <Button type="submit" variant="primary" disabled={create.isPending}>
          Создать
        </Button>
      </form>
    </Dialog>
  )
}
