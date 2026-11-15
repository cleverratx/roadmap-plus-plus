import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useId, useState } from 'react'
import { rememberItem, type ItemDetails } from '@/entities/item'
import { RoleBadge, RolePicker, useWorkspace, type WorkspaceDetails, type WorkspaceRole } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button, Dialog } from '@/shared/ui'
import styles from './ItemAccess.module.css'

/**
 * Блок "Доступ" в панели узла: каким ролям открыт сам узел и какие роли видят его через предков.
 * Пока в команде нет ролей и они выключены, блок не нужен и не показывается.
 */
export function ItemAccess({ details }: { details: ItemDetails }) {
  const headingId = useId()
  const workspace = useWorkspace(details.item.workspaceId)

  if (!workspace.data || (workspace.data.roles.length === 0 && !workspace.data.rolesEnabled)) {
    return null
  }

  const team = workspace.data
  const known = (ids: readonly string[]) => team.roles.filter((role) => ids.includes(role.id))
  const direct = known(details.roleIds)
  const inherited = known(details.inheritedRoleIds.filter((id) => !details.roleIds.includes(id)))
  const nobody = team.rolesEnabled && direct.length === 0 && inherited.length === 0

  return (
    <section className={styles.access} aria-labelledby={headingId}>
      <div className={styles.header}>
        <h3 id={headingId} className={styles.heading}>
          Доступ
        </h3>
        {team.isHead && <ItemRolesDialog details={details} team={team} />}
      </div>

      {!team.rolesEnabled && <p className={styles.muted}>Роли в команде выключены, поэтому сейчас узел видят все участники.</p>}

      {direct.length > 0 && <RoleRow label="Открыт ролям" roles={direct} />}
      {inherited.length > 0 && <RoleRow label="Через предков" roles={inherited} />}

      {nobody && <p className={styles.warning}>Узел не открыт ни одной роли, поэтому его видят только главы.</p>}
    </section>
  )
}

function RoleRow({ label, roles }: { label: string; roles: WorkspaceRole[] }) {
  return (
    <div className={styles.row}>
      <span className={styles.muted}>{label}:</span>
      {roles.map((role) => (
        <RoleBadge key={role.id} role={role} />
      ))}
    </div>
  )
}

function ItemRolesDialog({ details, team }: { details: ItemDetails; team: WorkspaceDetails }) {
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set(details.roleIds))

  const save = useMutation({
    mutationFn: (roleIds: string[]) =>
      unwrap(api.PUT('/api/items/{itemId}/roles', { params: { path: { itemId: details.item.id } }, body: { roleIds } })),
    onSuccess: (updated) => {
      rememberItem(queryClient, updated)
      setOpen(false)
    },
  })

  function changeOpen(next: boolean) {
    setOpen(next)
    if (next) {
      setSelected(new Set(details.roleIds))
      save.reset()
    }
  }

  return (
    <Dialog
      title={`Доступ к "${details.item.title}"`}
      description="Узел и всё, что внутри, увидят участники с любой из отмеченных ролей. Главы видят всё всегда."
      open={open}
      onOpenChange={changeOpen}
      trigger={<Button variant="ghost">Изменить доступ</Button>}
      footer={
        <Button variant="primary" disabled={save.isPending || team.roles.length === 0} onClick={() => save.mutate([...selected])}>
          Сохранить
        </Button>
      }
    >
      {team.roles.length === 0 ? (
        <p className={styles.muted}>В команде пока нет ролей. Их заводит глава на странице команды.</p>
      ) : (
        <RolePicker label="Роли, которым открыт узел" roles={team.roles} selected={selected} onChange={setSelected} />
      )}
      {save.isError && (
        <p className={styles.error} role="alert">
          {toFormErrors(save.error).message}
        </p>
      )}
    </Dialog>
  )
}
