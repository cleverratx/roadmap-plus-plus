import { Link, useParams } from 'react-router'
import { useMe } from '@/entities/session'
import { useWorkspace, WorkspaceDot } from '@/entities/workspace'
import { InviteLink } from '@/features/workspace-invite'
import { MemberList } from '@/features/workspace-members'
import { WorkspaceEditForm } from '@/features/workspace-edit'
import { HttpError, toFormErrors } from '@/shared/api'
import { paths } from '@/shared/config'
import { Page } from '@/shared/ui'
import styles from './TeamPage.module.css'

export function TeamPage() {
  const { workspaceId = '' } = useParams()
  const me = useMe()
  const workspace = useWorkspace(workspaceId)

  if (workspace.isPending || !me.data) {
    return <Page title="Команда" />
  }

  if (workspace.isError) {
    const notFound = workspace.error instanceof HttpError && workspace.error.status === 404
    return (
      <Page title={notFound ? 'Команда не найдена' : 'Команда'}>
        <p role="alert">{notFound ? 'Такой команды нет или вы в ней не состоите.' : toFormErrors(workspace.error).message}</p>
        <Link to={paths.settings}>Все команды</Link>
      </Page>
    )
  }

  const team = workspace.data

  return (
    <Page title={team.name}>
      <p className={styles.meta}>
        <WorkspaceDot color={team.color} />
        {team.isHead ? 'Вы глава этой команды' : 'Вы участник этой команды'}
        <Link to={paths.settings}>Все команды</Link>
      </p>
      {team.isHead && (
        <>
          <WorkspaceEditForm key={`${team.name}${team.color}`} workspace={team} />
          <InviteLink workspaceId={team.id} />
        </>
      )}
      <MemberList workspace={team} meId={me.data.id} />
    </Page>
  )
}
