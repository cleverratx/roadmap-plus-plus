import type { ReactNode } from 'react'
import styles from './FormCard.module.css'

type FormCardProps = {
  title: string
  footer?: ReactNode
  children: ReactNode
}

export function FormCard({ title, footer, children }: FormCardProps) {
  return (
    <main className={styles.screen}>
      <section className={styles.card}>
        <p className={styles.brand}>Roadmap++</p>
        <h1 className={styles.title}>{title}</h1>
        {children}
        {footer && <div className={styles.footer}>{footer}</div>}
      </section>
    </main>
  )
}
