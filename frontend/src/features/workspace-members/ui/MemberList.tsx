import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { WorkspaceDot, workspaceQueryKey, workspacesQueryKey, type WorkspaceDetails, type WorkspaceMember } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { paths } from '@/shared/config'
import { Button } from '@/shared/ui'
import styles from './MemberList.module.css'

type MemberListProps = {
  workspace: WorkspaceDetails
  meId: string
}

export function MemberList({ workspace, meId }: MemberListProps) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const path = (userId: string) => ({ params: { path: { workspaceId: workspace.id, userId } } })

  const setHead = useMutation({
    mutationFn: (member: WorkspaceMember) =>
      unwrap(api.PATCH('/api/workspaces/{workspaceId}/members/{userId}', { ...path(member.userId), body: { isHead: !member.isHead } })),
    onSuccess: (details) => {
      queryClient.setQueryData(workspaceQueryKey(workspace.id), details)
      void queryClient.invalidateQueries({ queryKey: workspacesQueryKey, exact: true })
    },
  })

  const remove = useMutation({
    mutationFn: (member: WorkspaceMember) => unwrap(api.DELETE('/api/workspaces/{workspaceId}/members/{userId}', path(member.userId))),
    onSuccess: (_, member) => {
      void queryClient.invalidateQueries({ queryKey: workspacesQueryKey, exact: true })
      if (member.userId === meId) {
        queryClient.removeQueries({ queryKey: workspaceQueryKey(workspace.id) })
        navigate(paths.settings, { replace: true })
        return
      }

      void queryClient.invalidateQueries({ queryKey: workspaceQueryKey(workspace.id) })
    },
  })

  const error = setHead.error ?? remove.error
  const busy = setHead.isPending || remove.isPending

  return (
    <section className={styles.section} aria-labelledby="members-title">
      <h2 id="members-title" className={styles.title}>
        Участники
      </h2>
      <ul className={styles.list}>
        {workspace.members.map((member) => {
          const isMe = member.userId === meId

          return (
            <li key={member.userId} className={styles.member}>
              <WorkspaceDot color={member.color} />
              <div className={styles.who}>
                <span className={styles.name}>
                  {member.displayName}
                  {isMe && <span className={styles.muted}> (вы)</span>}
                </span>
                <span className={styles.muted}>{member.email}</span>
              </div>
              {member.isHead && <span className={styles.badge}>глава</span>}
              <div className={styles.actions}>
                {workspace.isHead && (
                  <Button variant="ghost" onClick={() => setHead.mutate(member)} disabled={busy}>
                    {member.isHead ? 'Снять главу' : 'Сделать главой'}
                  </Button>
                )}
                {isMe ? (
                  <Button variant="ghost" onClick={() => remove.mutate(member)} disabled={busy}>
                    Выйти из команды
                  </Button>
                ) : (
                  workspace.isHead && (
                    <Button variant="ghost" onClick={() => remove.mutate(member)} disabled={busy}>
                      Исключить
                    </Button>
                  )
                )}
              </div>
            </li>
          )
        })}
      </ul>
      {error && (
        <p className={styles.error} role="alert">
          {toFormErrors(error).message}
        </p>
      )}
    </section>
  )
}
