import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useId, useState } from 'react'
import { linkKindLabels, linkTitle, type Link } from '@/entities/item'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button } from '@/shared/ui'
import { updateLinks } from '../model/cache'
import { LinkForm } from './LinkForm'
import styles from './ItemLinks.module.css'

type ItemLinksProps = {
  itemId: string
  links: readonly Link[]
}

/** Блок "Ссылки" в панели узла: таблица задач, пост организаторов, репозиторий, макет. */
export function ItemLinks({ itemId, links }: ItemLinksProps) {
  const headingId = useId()
  const [adding, setAdding] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  return (
    <section className={styles.links} aria-labelledby={headingId}>
      <div className={styles.header}>
        <h3 id={headingId} className={styles.heading}>
          Ссылки
        </h3>
        {!adding && (
          <Button variant="ghost" onClick={() => setAdding(true)}>
            + Ссылка
          </Button>
        )}
      </div>

      {links.length === 0 && !adding && (
        <p className={styles.muted}>Таблица задач, пост организаторов, репозиторий: всё, что нужно держать под рукой.</p>
      )}

      {links.length > 0 && (
        <ul className={styles.list}>
          {links.map((link) => (
            <li key={link.id}>
              {editingId === link.id ? (
                <LinkForm itemId={itemId} link={link} onDone={() => setEditingId(null)} />
              ) : (
                <LinkRow itemId={itemId} link={link} onEdit={() => setEditingId(link.id)} />
              )}
            </li>
          ))}
        </ul>
      )}

      {adding && <LinkForm itemId={itemId} onDone={() => setAdding(false)} />}
    </section>
  )
}

type LinkRowProps = {
  itemId: string
  link: Link
  onEdit: () => void
}

function LinkRow({ itemId, link, onEdit }: LinkRowProps) {
  const queryClient = useQueryClient()
  const title = linkTitle(link)

  const remove = useMutation({
    mutationFn: () => unwrap(api.DELETE('/api/items/{itemId}/links/{linkId}', { params: { path: { itemId, linkId: link.id } } })),
    onSuccess: () => updateLinks(queryClient, itemId, (links) => links.filter((existing) => existing.id !== link.id)),
  })

  return (
    <div className={styles.row}>
      <div className={styles.main}>
        <a className={styles.anchor} href={link.url} target="_blank" rel="noopener noreferrer">
          {title}
        </a>
        <span className={styles.kind} data-kind={link.kind}>
          {linkKindLabels[link.kind]}
        </span>
        {remove.isError && (
          <span className={styles.error} role="alert">
            {toFormErrors(remove.error).message}
          </span>
        )}
      </div>
      <div className={styles.rowActions}>
        <Button variant="ghost" aria-label={`Изменить ссылку "${title}"`} onClick={onEdit}>
          Изменить
        </Button>
        <Button
          variant="ghost"
          className={styles.danger}
          aria-label={`Удалить ссылку "${title}"`}
          onClick={() => remove.mutate()}
          disabled={remove.isPending}
        >
          Удалить
        </Button>
      </div>
    </div>
  )
}
