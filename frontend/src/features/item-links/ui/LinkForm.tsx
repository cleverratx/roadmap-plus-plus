import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import type { Link } from '@/entities/item'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button, TextField } from '@/shared/ui'
import { updateLinks } from '../model/cache'
import styles from './ItemLinks.module.css'

type LinkFormProps = {
  itemId: string
  link?: Link
  onDone: () => void
}

export function LinkForm({ itemId, link, onDone }: LinkFormProps) {
  const queryClient = useQueryClient()
  const [url, setUrl] = useState(link?.url ?? '')
  const [title, setTitle] = useState(link?.title ?? '')

  const save = useMutation({
    mutationFn: () => {
      const body = { url, title: title || null }
      return link
        ? unwrap(api.PUT('/api/items/{itemId}/links/{linkId}', { params: { path: { itemId, linkId: link.id } }, body }))
        : unwrap(api.POST('/api/items/{itemId}/links', { params: { path: { itemId } }, body }))
    },
    onSuccess: (saved) => {
      updateLinks(queryClient, itemId, (links) =>
        link ? links.map((existing) => (existing.id === saved.id ? saved : existing)) : [...links, saved],
      )
      onDone()
    },
  })
  const errors = toFormErrors(save.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    save.mutate()
  }

  return (
    <form className={styles.form} onSubmit={submit} noValidate aria-label={link ? 'Правка ссылки' : 'Новая ссылка'}>
      <TextField
        label="Адрес"
        type="url"
        inputMode="url"
        placeholder="https://"
        value={url}
        onChange={(event) => setUrl(event.target.value)}
        error={errors.fields.url}
        autoFocus
      />
      <TextField
        label="Название, если нужно своё"
        value={title}
        onChange={(event) => setTitle(event.target.value)}
        error={errors.fields.title}
      />
      {errors.message && (
        <p className={styles.error} role="alert">
          {errors.message}
        </p>
      )}
      <div className={styles.formActions}>
        <Button type="submit" variant="primary" disabled={save.isPending}>
          {link ? 'Сохранить' : 'Добавить'}
        </Button>
        <Button onClick={onDone}>Отмена</Button>
      </div>
    </form>
  )
}
