import { useState } from 'react'
import { useWorkspaces, WorkspaceDot } from '@/entities/workspace'
import { JoinByLinkDialog } from '@/features/invitation-accept'
import { CreateWorkspaceDialog } from '@/features/workspace-create'
import { toFormErrors } from '@/shared/api'
import { cx } from '@/shared/lib'
import { Page } from '@/shared/ui'
import { Timeline } from '@/widgets/timeline'
import styles from './RoadmapPage.module.css'

export function RoadmapPage() {
  const workspaces = useWorkspaces()
  const [hidden, setHidden] = useState<ReadonlySet<string>>(new Set())

  function toggle(workspaceId: string) {
    setHidden((current) => {
      const next = new Set(current)
      if (!next.delete(workspaceId)) {
        next.add(workspaceId)
      }
      return next
    })
  }

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
      <ul className={styles.teams} aria-label="Показывать команды">
        {workspaces.data.map((workspace) => (
          <li key={workspace.id}>
            <button
              type="button"
              className={cx(styles.team, hidden.has(workspace.id) && styles.off)}
              aria-pressed={!hidden.has(workspace.id)}
              onClick={() => toggle(workspace.id)}
            >
              <WorkspaceDot color={workspace.color} />
              {workspace.name}
            </button>
          </li>
        ))}
      </ul>
      <Timeline workspaces={workspaces.data} hiddenWorkspaceIds={hidden} />
    </Page>
  )
}
