import { useMutation, useQueryClient } from '@tanstack/react-query'
import { allItemsQueryKey, itemQueryKey, type Item } from '@/entities/item'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button, Dialog, DialogClose } from '@/shared/ui'
import styles from './ItemForm.module.css'

type DeleteItemDialogProps = {
  item: Item
  onClose: () => void
  onDeleted: (item: Item) => void
}

export function DeleteItemDialog({ item, onClose, onDeleted }: DeleteItemDialogProps) {
  const queryClient = useQueryClient()

  const remove = useMutation({
    mutationFn: () => unwrap(api.DELETE('/api/items/{itemId}', { params: { path: { itemId: item.id } } })),
    onSuccess: () => {
      onDeleted(item)
      queryClient.removeQueries({ queryKey: itemQueryKey(item.id), exact: true })
      void queryClient.invalidateQueries({ queryKey: allItemsQueryKey })
    },
  })
  const { message } = toFormErrors(remove.error)

  return (
    <Dialog
      title={`Удалить "${item.title}"?`}
      description="Вместе с ним удалится всё, что внутри. Вернуть не получится."
      open
      onOpenChange={(open) => !open && onClose()}
      footer={
        <>
          <DialogClose asChild>
            <Button>Отмена</Button>
          </DialogClose>
          <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
            Удалить
          </Button>
        </>
      }
    >
      {message && (
        <p className={styles.error} role="alert">
          {message}
        </p>
      )}
    </Dialog>
  )
}
