import { useState } from 'react'
import { RolePicker, type WorkspaceDetails, type WorkspaceMember } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button, Dialog } from '@/shared/ui'
import { useWorkspaceChange } from '../model/roles'
import styles from './Roles.module.css'

type MemberRolesDialogProps = {
  workspace: WorkspaceDetails
  member: WorkspaceMember
}

/** Кнопка "Роли" у участника: глава отмечает, какие роли у него есть, и сохраняет весь набор разом. */
export function MemberRolesDialog({ workspace, member }: MemberRolesDialogProps) {
  const [open, setOpen] = useState(false)
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set(member.roleIds))

  const save = useWorkspaceChange(workspace.id, (roleIds: string[]) =>
    unwrap(
      api.PUT('/api/workspaces/{workspaceId}/members/{userId}/roles', {
        params: { path: { workspaceId: workspace.id, userId: member.userId } },
        body: { roleIds },
      }),
    ),
  )

  function changeOpen(next: boolean) {
    setOpen(next)
    if (next) {
      setSelected(new Set(member.roleIds))
      save.reset()
    }
  }

  return (
    <Dialog
      title={`Роли: ${member.displayName}`}
      open={open}
      onOpenChange={changeOpen}
      trigger={
        <Button variant="ghost" aria-label={`Роли участника ${member.displayName}`}>
          Роли
        </Button>
      }
      footer={
        <Button
          variant="primary"
          disabled={save.isPending || workspace.roles.length === 0}
          onClick={() => save.mutate([...selected], { onSuccess: () => setOpen(false) })}
        >
          Сохранить
        </Button>
      }
    >
      {workspace.roles.length === 0 ? (
        <p className={styles.muted}>В команде пока нет ролей. Добавьте их в разделе "Роли" ниже.</p>
      ) : (
        <RolePicker label="Роли участника" roles={workspace.roles} selected={selected} onChange={setSelected} />
      )}
      {save.isError && (
        <p className={styles.error} role="alert">
          {toFormErrors(save.error).message}
        </p>
      )}
    </Dialog>
  )
}
