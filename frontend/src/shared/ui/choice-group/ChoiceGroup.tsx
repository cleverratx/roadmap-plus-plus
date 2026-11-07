import type { ReactNode } from 'react'
import { cx } from '@/shared/lib'
import styles from './ChoiceGroup.module.css'

export type Choice<T extends string> = {
  value: T
  label: ReactNode
}

type ChoiceGroupProps<T extends string> = {
  label: string
  choices: readonly Choice<T>[]
  value: T
  onChange: (value: T) => void
}

/** Выбор одного варианта из нескольких кнопками в ряд, вместо выпадающего списка, когда вариантов мало. */
export function ChoiceGroup<T extends string>({ label, choices, value, onChange }: ChoiceGroupProps<T>) {
  return (
    <fieldset className={styles.group}>
      <legend className={styles.legend}>{label}</legend>
      <div className={styles.choices} role="radiogroup" aria-label={label}>
        {choices.map((choice) => (
          <button
            key={choice.value}
            type="button"
            role="radio"
            aria-checked={choice.value === value}
            className={cx(styles.choice, choice.value === value && styles.selected)}
            onClick={() => onChange(choice.value)}
          >
            {choice.label}
          </button>
        ))}
      </div>
    </fieldset>
  )
}
