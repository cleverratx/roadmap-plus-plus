import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { workspaceQueryKey, workspacesQueryKey, type WorkspaceDetails } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { teamColors } from '@/shared/config'
import { Button, ColorSwatches, TextField } from '@/shared/ui'
import styles from './WorkspaceEditForm.module.css'

export function WorkspaceEditForm({ workspace }: { workspace: WorkspaceDetails }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(workspace.name)
  const [color, setColor] = useState(workspace.color)

  const update = useMutation({
    mutationFn: () =>
      unwrap(api.PATCH('/api/workspaces/{workspaceId}', { params: { path: { workspaceId: workspace.id } }, body: { name, color } })),
    onSuccess: (details) => {
      queryClient.setQueryData(workspaceQueryKey(workspace.id), details)
      void queryClient.invalidateQueries({ queryKey: workspacesQueryKey, exact: true })
    },
  })
  const errors = toFormErrors(update.error)
  const colors = teamColors.includes(color as (typeof teamColors)[number]) ? teamColors : [color, ...teamColors]
  const changed = name !== workspace.name || color !== workspace.color

  function submit(event: FormEvent) {
    event.preventDefault()
    update.mutate()
  }

  return (
    <form className={styles.form} onSubmit={submit} noValidate>
      <TextField label="Название команды" value={name} onChange={(event) => setName(event.target.value)} error={errors.fields.name} />
      <ColorSwatches label="Цвет на таймлайне" colors={colors} value={color} onChange={setColor} />
      {errors.message && (
        <p className={styles.error} role="alert">
          {errors.message}
        </p>
      )}
      <div>
        <Button type="submit" variant="primary" disabled={!changed || update.isPending}>
          Сохранить
        </Button>
      </div>
    </form>
  )
}
