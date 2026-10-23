import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { invitePath } from '@/shared/config'
import { Button } from '@/shared/ui'
import styles from './InviteLink.module.css'

export function InviteLink({ workspaceId }: { workspaceId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ['workspaces', workspaceId, 'invitation'] as const
  const [copied, setCopied] = useState(false)
  const params = { params: { path: { workspaceId } } }

  const invitation = useQuery({
    queryKey,
    queryFn: ({ signal }) => unwrap(api.GET('/api/workspaces/{workspaceId}/invitation', { ...params, signal })),
  })

  const create = useMutation({
    mutationFn: () => unwrap(api.POST('/api/workspaces/{workspaceId}/invitation', params)),
    onSuccess: (active) => {
      queryClient.setQueryData(queryKey, { active })
      setCopied(false)
    },
  })

  const revoke = useMutation({
    mutationFn: () => unwrap(api.DELETE('/api/workspaces/{workspaceId}/invitation', params)),
    onSuccess: () => queryClient.setQueryData(queryKey, { active: null }),
  })

  const active = invitation.data?.active
  const link = active ? `${window.location.origin}${invitePath(active.token)}` : null
  const error = create.error ?? revoke.error ?? invitation.error

  async function copy() {
    if (!link) {
      return
    }

    try {
      await navigator.clipboard.writeText(link)
      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  return (
    <section className={styles.section} aria-labelledby="invite-title">
      <h2 id="invite-title" className={styles.title}>
        Ссылка-приглашение
      </h2>
      <p className={styles.hint}>
        По ссылке может вступить любой, у кого она есть. Она действует 7 дней, а новая ссылка сразу отменяет старую.
      </p>

      {invitation.isPending ? (
        <p className={styles.hint}>Загрузка...</p>
      ) : link ? (
        <>
          <div className={styles.linkRow}>
            <input className={styles.link} value={link} readOnly aria-label="Ссылка-приглашение" onFocus={(e) => e.target.select()} />
            <Button onClick={copy}>{copied ? 'Скопировано' : 'Скопировать'}</Button>
          </div>
          <p className={styles.hint}>Действует до {new Date(active!.expiresAt).toLocaleDateString('ru-RU')}</p>
          <div className={styles.actions}>
            <Button variant="ghost" onClick={() => create.mutate()} disabled={create.isPending}>
              Сделать новую ссылку
            </Button>
            <Button variant="ghost" onClick={() => revoke.mutate()} disabled={revoke.isPending}>
              Отключить ссылку
            </Button>
          </div>
        </>
      ) : (
        <Button variant="primary" onClick={() => create.mutate()} disabled={create.isPending}>
          Создать ссылку
        </Button>
      )}

      {error && (
        <p className={styles.error} role="alert">
          {toFormErrors(error).message}
        </p>
      )}
    </section>
  )
}
