import { useId, type ComponentProps } from 'react'
import { cx } from '@/shared/lib'
import styles from './TextArea.module.css'

type TextAreaProps = ComponentProps<'textarea'> & {
  label: string
  error?: string
}

export function TextArea({ label, error, id, className, ...props }: TextAreaProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  const errorId = `${inputId}-error`

  return (
    <div className={cx(styles.field, className)}>
      <label htmlFor={inputId} className={styles.label}>
        {label}
      </label>
      <textarea
        id={inputId}
        className={styles.input}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        {...props}
      />
      {error && (
        <p id={errorId} className={styles.error}>
          {error}
        </p>
      )}
    </div>
  )
}
