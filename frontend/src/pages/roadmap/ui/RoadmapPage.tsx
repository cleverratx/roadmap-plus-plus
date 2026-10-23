import { Link } from 'react-router'
import { useWorkspaces, WorkspaceDot } from '@/entities/workspace'
import { JoinByLinkDialog } from '@/features/invitation-accept'
import { CreateWorkspaceDialog } from '@/features/workspace-create'
import { toFormErrors } from '@/shared/api'
import { teamPath } from '@/shared/config'
import { Page } from '@/shared/ui'
import styles from './RoadmapPage.module.css'

export function RoadmapPage() {
  const workspaces = useWorkspaces()

  if (workspaces.isPending) {
    return <Page title="Роадмап" />
  }

  if (workspaces.isError) {
    return (
      <Page title="Роадмап">
        <p role="alert">{toFormErrors(workspaces.error).message}</p>
      </Page>
    )
  }

  if (workspaces.data.length === 0) {
    return (
      <Page title="Роадмап">
        <div className={styles.empty}>
          <h2 className={styles.emptyTitle}>У вас пока нет команды</h2>
          <p className={styles.muted}>
            Роадмапы в Roadmap++ всегда принадлежат команде. Создайте свою, даже если вы в ней один, или вступите в
            существующую по ссылке от главы.
          </p>
          <div className={styles.actions}>
            <CreateWorkspaceDialog />
            <JoinByLinkDialog />
          </div>
        </div>
      </Page>
    )
  }

  return (
    <Page title="Роадмап">
      <p className={styles.muted}>Здесь будет таймлайн всех событий из ваших команд: от года до дня, с панелью события по клику.</p>
      <ul className={styles.teams} aria-label="Ваши команды">
        {workspaces.data.map((workspace) => (
          <li key={workspace.id}>
            <Link className={styles.team} to={teamPath(workspace.id)}>
              <WorkspaceDot color={workspace.color} />
              {workspace.name}
            </Link>
          </li>
        ))}
      </ul>
    </Page>
  )
}
