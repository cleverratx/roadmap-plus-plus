import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router'
import { WorkspaceDot, workspacesQueryKey } from '@/entities/workspace'
import { api, HttpError, toFormErrors, unwrap } from '@/shared/api'
import { paths, teamPath } from '@/shared/config'
import { plural } from '@/shared/lib'
import { Button } from '@/shared/ui'
import styles from './Invitation.module.css'

export function AcceptInvitation({ token }: { token: string }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const preview = useQuery({
    queryKey: ['invitation', token],
    queryFn: ({ signal }) => unwrap(api.GET('/api/invitations/{token}', { params: { path: { token } }, signal })),
  })

  const accept = useMutation({
    mutationFn: () => unwrap(api.POST('/api/invitations/{token}/accept', { params: { path: { token } } })),
    onSuccess: (team) => {
      void queryClient.invalidateQueries({ queryKey: workspacesQueryKey })
      navigate(teamPath(team.id), { replace: true })
    },
  })

  if (preview.isPending) {
    return <p>Проверяем приглашение...</p>
  }

  if (preview.isError) {
    const message =
      preview.error instanceof HttpError && preview.error.status === 404
        ? 'Приглашение не найдено или больше не действует. Попросите у главы команды новую ссылку.'
        : toFormErrors(preview.error).message

    return (
      <div className={styles.card}>
        <p role="alert">{message}</p>
        <Link to={paths.roadmap}>Вернуться к роадмапу</Link>
      </div>
    )
  }

  const team = preview.data

  return (
    <div className={styles.card}>
      <p className={styles.team}>
        <WorkspaceDot color={team.color} />
        <strong>{team.workspaceName}</strong>
      </p>
      <p className={styles.muted}>
        {team.memberCount} {plural(team.memberCount, ['участник', 'участника', 'участников'])}
      </p>
      {team.alreadyMember ? (
        <Button variant="primary" onClick={() => navigate(teamPath(team.workspaceId))}>
          Вы уже в команде, открыть
        </Button>
      ) : (
        <Button variant="primary" onClick={() => accept.mutate()} disabled={accept.isPending}>
          Вступить в команду
        </Button>
      )}
      {accept.error && <p role="alert">{toFormErrors(accept.error).message}</p>}
    </div>
  )
}
