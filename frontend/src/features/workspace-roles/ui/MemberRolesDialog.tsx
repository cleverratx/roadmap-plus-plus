import { useState } from 'react'
import { RoleBadge, type WorkspaceDetails, type WorkspaceMember } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button, Checkbox, Dialog } from '@/shared/ui'
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

  function toggle(roleId: string, checked: boolean) {
    setSelected((current) => {
      const next = new Set(current)
      if (checked) {
        next.add(roleId)
      } else {
        next.delete(roleId)
      }
      return next
    })
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
        <div className={styles.checks} role="group" aria-label="Роли участника">
          {workspace.roles.map((role) => (
            <Checkbox
              key={role.id}
              label={<RoleBadge role={role} />}
              aria-label={role.name}
              checked={selected.has(role.id)}
              onChange={(event) => toggle(role.id, event.target.checked)}
            />
          ))}
        </div>
      )}
      {save.isError && (
        <p className={styles.error} role="alert">
          {toFormErrors(save.error).message}
        </p>
      )}
    </Dialog>
  )
}
