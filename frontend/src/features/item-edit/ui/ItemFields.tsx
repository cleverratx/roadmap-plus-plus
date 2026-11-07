import { TextArea, TextField } from '@/shared/ui'
import type { ItemFieldsValue } from '../model/fields'
import styles from './ItemForm.module.css'

type ItemFieldsProps = {
  value: ItemFieldsValue
  onChange: (value: ItemFieldsValue) => void
  errors: Record<string, string>
}

export function ItemFields({ value, onChange, errors }: ItemFieldsProps) {
  function changeStart(startDate: string) {
    const endDate = startDate && (!value.endDate || value.endDate < startDate) ? startDate : value.endDate
    onChange({ ...value, startDate, endDate })
  }

  return (
    <>
      <TextField
        label="Название"
        value={value.title}
        onChange={(event) => onChange({ ...value, title: event.target.value })}
        error={errors.title}
        autoFocus
      />
      <div className={styles.dates}>
        <TextField
          label="Начало"
          type="date"
          value={value.startDate}
          onChange={(event) => changeStart(event.target.value)}
          error={errors.startDate}
        />
        <TextField
          label="Конец"
          type="date"
          value={value.endDate}
          min={value.startDate || undefined}
          onChange={(event) => onChange({ ...value, endDate: event.target.value })}
          error={errors.endDate}
        />
      </div>
      <TextArea
        label="Описание в Markdown"
        value={value.descriptionMd}
        rows={4}
        onChange={(event) => onChange({ ...value, descriptionMd: event.target.value })}
        error={errors.descriptionMd}
      />
    </>
  )
}
