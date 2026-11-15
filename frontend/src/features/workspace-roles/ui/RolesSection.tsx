import { useId, useState } from 'react'
import { useAccessSummary } from '@/entities/item'
import { RoleBadge, type WorkspaceDetails, type WorkspaceRole } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { teamColors } from '@/shared/config'
import { plural } from '@/shared/lib'
import { Button, Checkbox, Dialog, DialogClose } from '@/shared/ui'
import { useWorkspaceChange } from '../model/roles'
import { RoleForm, type RoleDraft } from './RoleForm'
import styles from './Roles.module.css'

const ENABLED_HINT = 'Обычный участник будет видеть только события, открытые его ролям. Главы по-прежнему видят всё.'

/** Раздел "Роли" на странице команды: флажок "Включить роли" и сами роли. Менять может только глава, смотреть могут все. */
export function RolesSection({ workspace }: { workspace: WorkspaceDetails }) {
  const titleId = useId()
  const path = { params: { path: { workspaceId: workspace.id } } }
  const [adding, setAdding] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<WorkspaceRole | null>(null)

  const toggle = useWorkspaceChange(workspace.id, (enabled: boolean) =>
    unwrap(api.PUT('/api/workspaces/{workspaceId}/roles-enabled', { ...path, body: { enabled } })),
  )
  const add = useWorkspaceChange(workspace.id, (draft: RoleDraft) =>
    unwrap(api.POST('/api/workspaces/{workspaceId}/roles', { ...path, body: draft })),
  )
  const update = useWorkspaceChange(workspace.id, ({ roleId, ...draft }: RoleDraft & { roleId: string }) =>
    unwrap(api.PATCH('/api/workspaces/{workspaceId}/roles/{roleId}', { params: { path: { workspaceId: workspace.id, roleId } }, body: draft })),
  )
  const remove = useWorkspaceChange(workspace.id, (roleId: string) =>
    unwrap(api.DELETE('/api/workspaces/{workspaceId}/roles/{roleId}', { params: { path: { workspaceId: workspace.id, roleId } } })),
  )

  const nextColor = teamColors[workspace.roles.length % teamColors.length]
  const summary = useAccessSummary(workspace.id, workspace.isHead && (workspace.rolesEnabled || workspace.roles.length > 0))
  const unshared = summary.data?.unsharedEvents ?? 0

  return (
    <section className={styles.section} aria-labelledby={titleId}>
      <h2 id={titleId} className={styles.title}>
        Роли
      </h2>

      {workspace.isHead ? (
        <Checkbox
          label="Включить роли"
          hint={workspace.rolesEnabled ? ENABLED_HINT : `Сейчас все участники видят все события команды. ${ENABLED_HINT}`}
          checked={workspace.rolesEnabled}
          disabled={toggle.isPending}
          onChange={(event) => toggle.mutate(event.target.checked)}
        />
      ) : (
        <p className={styles.muted}>
          {workspace.rolesEnabled
            ? 'Роли включены: вы видите события, открытые вашим ролям.'
            : 'Роли выключены: все участники видят все события команды.'}
        </p>
      )}
      {summary.data && unshared > 0 && (
        <div className={styles.warning} role="status">
          <p className={styles.warningText}>
            {unshared} {plural(unshared, ['событие не открыто', 'события не открыты', 'событий не открыты'])} ни одной роли.{' '}
            {workspace.rolesEnabled ? 'Сейчас их видят только главы.' : 'Когда вы включите роли, их будут видеть только главы.'}
          </p>
          <p className={styles.muted}>
            Например: {summary.data.examples.map((item) => `"${item.title}"`).join(', ')}
            {unshared > summary.data.examples.length && ' и другие'}. Открыть событие ролям можно в его панели, кнопкой "Изменить доступ".
          </p>
        </div>
      )}

      {toggle.isError && (
        <p className={styles.error} role="alert">
          {toFormErrors(toggle.error).message}
        </p>
      )}

      {workspace.roles.length === 0 ? (
        <p className={styles.muted}>Ролей пока нет.</p>
      ) : (
        <ul className={styles.list}>
          {workspace.roles.map((role) => (
            <li key={role.id} className={styles.role}>
              {editingId === role.id ? (
                <RoleForm
                  role={role}
                  defaultColor={role.color}
                  pending={update.isPending}
                  error={update.error}
                  onSubmit={(draft) => update.mutate({ roleId: role.id, ...draft }, { onSuccess: () => setEditingId(null) })}
                  onCancel={() => {
                    update.reset()
                    setEditingId(null)
                  }}
                />
              ) : (
                <>
                  <RoleBadge role={role} />
                  <span className={styles.muted}>
                    {role.memberCount} {plural(role.memberCount, ['участник', 'участника', 'участников'])}
                  </span>
                  {workspace.isHead && (
                    <div className={styles.actions}>
                      <Button variant="ghost" aria-label={`Изменить роль "${role.name}"`} onClick={() => setEditingId(role.id)}>
                        Изменить
                      </Button>
                      <Button
                        variant="ghost"
                        className={styles.danger}
                        aria-label={`Удалить роль "${role.name}"`}
                        onClick={() => setDeleting(role)}
                      >
                        Удалить
                      </Button>
                    </div>
                  )}
                </>
              )}
            </li>
          ))}
        </ul>
      )}

      {workspace.isHead &&
        (adding ? (
          <RoleForm
            defaultColor={nextColor}
            pending={add.isPending}
            error={add.error}
            onSubmit={(draft) => add.mutate(draft, { onSuccess: () => setAdding(false) })}
            onCancel={() => {
              add.reset()
              setAdding(false)
            }}
          />
        ) : (
          <div>
            <Button onClick={() => setAdding(true)}>+ Роль</Button>
          </div>
        ))}

      {deleting && (
        <Dialog
          title={`Удалить роль "${deleting.name}"?`}
          description="Роль снимется со всех участников, а события, открытые только ей, увидят только главы."
          open
          onOpenChange={(open) => !open && setDeleting(null)}
          footer={
            <>
              <DialogClose asChild>
                <Button>Отмена</Button>
              </DialogClose>
              <Button
                variant="danger"
                disabled={remove.isPending}
                onClick={() => remove.mutate(deleting.id, { onSuccess: () => setDeleting(null) })}
              >
                Удалить
              </Button>
            </>
          }
        >
          {remove.isError && (
            <p className={styles.error} role="alert">
              {toFormErrors(remove.error).message}
            </p>
          )}
        </Dialog>
      )}
    </section>
  )
}
