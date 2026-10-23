import styles from './WorkspaceDot.module.css'

export function WorkspaceDot({ color }: { color: string }) {
  return <span className={styles.dot} style={{ background: color }} aria-hidden="true" />
}
