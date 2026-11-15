import { Checkbox } from '@/shared/ui'
import type { WorkspaceRole } from '../model/workspaces'
import { RoleBadge } from './RoleBadge'
import styles from './RolePicker.module.css'

type RolePickerProps = {
  label: string
  roles: readonly WorkspaceRole[]
  selected: ReadonlySet<string>
  onChange: (selected: ReadonlySet<string>) => void
}

/** Выбор нескольких ролей галочками: кому выдать роль, каким ролям открыть узел. */
export function RolePicker({ label, roles, selected, onChange }: RolePickerProps) {
  function toggle(roleId: string, checked: boolean) {
    const next = new Set(selected)
    if (checked) {
      next.add(roleId)
    } else {
      next.delete(roleId)
    }
    onChange(next)
  }

  return (
    <div className={styles.picker} role="group" aria-label={label}>
      {roles.map((role) => (
        <Checkbox
          key={role.id}
          label={<RoleBadge role={role} />}
          aria-label={role.name}
          checked={selected.has(role.id)}
          onChange={(event) => toggle(role.id, event.target.checked)}
        />
      ))}
    </div>
  )
}
