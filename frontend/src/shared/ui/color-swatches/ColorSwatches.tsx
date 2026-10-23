import { cx } from '@/shared/lib'
import styles from './ColorSwatches.module.css'

type ColorSwatchesProps = {
  label: string
  colors: readonly string[]
  value: string
  onChange: (color: string) => void
}

export function ColorSwatches({ label, colors, value, onChange }: ColorSwatchesProps) {
  return (
    <fieldset className={styles.group}>
      <legend className={styles.legend}>{label}</legend>
      <div className={styles.swatches} role="radiogroup" aria-label={label}>
        {colors.map((color) => (
          <button
            key={color}
            type="button"
            role="radio"
            aria-checked={color === value}
            aria-label={color}
            className={cx(styles.swatch, color === value && styles.selected)}
            style={{ background: color }}
            onClick={() => onChange(color)}
          />
        ))}
      </div>
    </fieldset>
  )
}
