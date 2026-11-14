import type { WorkspaceRole } from '../model/workspaces'
import styles from './RoleBadge.module.css'

export function RoleBadge({ role }: { role: Pick<WorkspaceRole, 'name' | 'color'> }) {
  return (
    <span className={styles.badge} style={{ borderColor: role.color }}>
      <span className={styles.dot} style={{ background: role.color }} aria-hidden="true" />
      {role.name}
    </span>
  )
}
