import { useState, type FormEvent } from 'react'
import { itemKindLabels, type Item, type ItemDetails } from '@/entities/item'
import { toFormErrors } from '@/shared/api'
import { cx } from '@/shared/lib'
import { Button, Dialog, TextField } from '@/shared/ui'
import { useMoveItem, useMoveTargets } from '../model/move'
import styles from './MoveItemDialog.module.css'

const ROOT = 'root'

type MoveItemDialogProps = {
  item: Item
  onClose: () => void
  onMoved: (details: ItemDetails) => void
}

type Option = {
  value: string
  title: string
  hint: string
}

export function MoveItemDialog({ item, onClose, onMoved }: MoveItemDialogProps) {
  const targets = useMoveTargets(item.id)
  const move = useMoveItem()
  const [query, setQuery] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const current = targets.data ? (targets.data.currentParentId ?? ROOT) : null

  const options: Option[] = targets.data
    ? [
        ...(targets.data.canBeRoot ? [{ value: ROOT, title: 'Верхний уровень команды', hint: 'Без родителя, прямо на таймлайне' }] : []),
        ...targets.data.parents.map(({ item: parent, path }) => ({
          value: parent.id,
          title: parent.title,
          hint: [itemKindLabels[parent.kind], ...path].join(' / '),
        })),
      ]
    : []
  const needle = query.trim().toLowerCase()
  const visible = needle ? options.filter((option) => `${option.title} ${option.hint}`.toLowerCase().includes(needle)) : options
  const { message } = toFormErrors(move.error ?? targets.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    if (!selected) {
      return
    }

    move.mutate({ itemId: item.id, parentId: selected === ROOT ? null : selected }, { onSuccess: (details) => onMoved(details) })
  }

  return (
    <Dialog
      title={`Переместить "${item.title}"`}
      description="Узел переедет вместе со всем, что внутри, и встанет последним."
      open
      onOpenChange={(open) => !open && onClose()}
    >
      <form className={styles.form} onSubmit={submit} noValidate>
        <TextField label="Найти" type="search" value={query} onChange={(event) => setQuery(event.target.value)} autoFocus />
        {targets.isPending && <p className={styles.muted}>Ищем, куда можно переместить...</p>}
        {targets.data && visible.length === 0 && (
          <p className={styles.muted}>{options.length === 0 ? 'Переместить этот узел некуда.' : 'Ничего не нашлось.'}</p>
        )}
        {visible.length > 0 && (
          <div className={styles.options} role="radiogroup" aria-label="Куда переместить">
            {visible.map((option) => {
              const isCurrent = option.value === current
              return (
                <button
                  key={option.value}
                  type="button"
                  role="radio"
                  aria-checked={option.value === selected}
                  aria-label={[option.title, isCurrent && 'сейчас здесь', option.hint].filter(Boolean).join(', ')}
                  disabled={isCurrent}
                  className={cx(styles.option, option.value === selected && styles.selected)}
                  onClick={() => setSelected(option.value)}
                >
                  <span className={styles.title}>
                    {option.title}
                    {isCurrent && <span className={styles.muted}> (сейчас здесь)</span>}
                  </span>
                  <span className={styles.muted}>{option.hint}</span>
                </button>
              )
            })}
          </div>
        )}
        {message && (
          <p className={styles.error} role="alert">
            {message}
          </p>
        )}
        <Button type="submit" variant="primary" disabled={!selected || move.isPending}>
          Переместить
        </Button>
      </form>
    </Dialog>
  )
}
