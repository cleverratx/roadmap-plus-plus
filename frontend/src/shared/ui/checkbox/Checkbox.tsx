import { useId, type ComponentProps, type ReactNode } from 'react'
import { cx } from '@/shared/lib'
import styles from './Checkbox.module.css'

type CheckboxProps = Omit<ComponentProps<'input'>, 'type'> & {
  label: ReactNode
  hint?: string
}

export function Checkbox({ label, hint, id, className, ...props }: CheckboxProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  const hintId = `${inputId}-hint`

  return (
    <div className={cx(styles.field, className)}>
      <input id={inputId} type="checkbox" className={styles.input} aria-describedby={hint ? hintId : undefined} {...props} />
      <div className={styles.text}>
        <label htmlFor={inputId} className={styles.label}>
          {label}
        </label>
        {hint && (
          <p id={hintId} className={styles.hint}>
            {hint}
          </p>
        )}
      </div>
    </div>
  )
}
