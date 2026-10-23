import { Link } from 'react-router'
import { useWorkspaces, WorkspaceDot } from '@/entities/workspace'
import { JoinByLinkDialog } from '@/features/invitation-accept'
import { CreateWorkspaceDialog } from '@/features/workspace-create'
import { toFormErrors } from '@/shared/api'
import { teamPath } from '@/shared/config'
import { plural } from '@/shared/lib'
import { Page } from '@/shared/ui'
import styles from './SettingsPage.module.css'

export function SettingsPage() {
  const workspaces = useWorkspaces()

  return (
    <Page title="Настройки">
      <section className={styles.section} aria-labelledby="teams-title">
        <div className={styles.header}>
          <h2 id="teams-title" className={styles.title}>
            Команды
          </h2>
          <div className={styles.actions}>
            <CreateWorkspaceDialog triggerVariant="secondary" />
            <JoinByLinkDialog triggerVariant="ghost" />
          </div>
        </div>

        {workspaces.isError && <p role="alert">{toFormErrors(workspaces.error).message}</p>}
        {workspaces.data?.length === 0 && <p className={styles.muted}>Вы пока не состоите ни в одной команде.</p>}

        {workspaces.data && workspaces.data.length > 0 && (
          <ul className={styles.list}>
            {workspaces.data.map((workspace) => (
              <li key={workspace.id}>
                <Link className={styles.team} to={teamPath(workspace.id)}>
                  <WorkspaceDot color={workspace.color} />
                  <span className={styles.name}>{workspace.name}</span>
                  {workspace.isHead && <span className={styles.badge}>вы глава</span>}
                  <span className={styles.muted}>
                    {workspace.memberCount} {plural(workspace.memberCount, ['участник', 'участника', 'участников'])}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </Page>
  )
}
