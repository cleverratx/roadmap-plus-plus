import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import type { ItemDetails } from '@/entities/item'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button } from '@/shared/ui'
import { checkItemFields, refreshItems, toItemBody, type ItemFieldsValue } from '../model/fields'
import { ItemFields } from './ItemFields'
import styles from './ItemForm.module.css'

type EditItemFormProps = {
  details: ItemDetails
  onDone: () => void
}

export function EditItemForm({ details, onDone }: EditItemFormProps) {
  const queryClient = useQueryClient()
  const { item } = details
  const [value, setValue] = useState<ItemFieldsValue>({
    title: item.title,
    startDate: item.startDate ?? '',
    endDate: item.endDate ?? '',
    descriptionMd: details.descriptionMd,
  })
  const [checks, setChecks] = useState<Record<string, string>>({})

  const update = useMutation({
    mutationFn: () => unwrap(api.PUT('/api/items/{itemId}', { params: { path: { itemId: item.id } }, body: toItemBody(value) })),
    onSuccess: (updated) => {
      refreshItems(queryClient, updated)
      onDone()
    },
  })
  const errors = toFormErrors(update.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    const found = checkItemFields(value, item.parentId === null)
    setChecks(found)
    if (Object.keys(found).length === 0) {
      update.mutate()
    }
  }

  return (
    <form className={styles.form} onSubmit={submit} noValidate aria-label="Правка узла">
      <ItemFields value={value} onChange={setValue} errors={{ ...errors.fields, ...checks }} />
      {errors.message && (
        <p className={styles.error} role="alert">
          {errors.message}
        </p>
      )}
      <div className={styles.actions}>
        <Button type="submit" variant="primary" disabled={update.isPending}>
          Сохранить
        </Button>
        <Button onClick={onDone}>Отмена</Button>
      </div>
    </form>
  )
}
