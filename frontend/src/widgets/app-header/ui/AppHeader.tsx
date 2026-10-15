import { NavLink } from 'react-router'
import { useMe } from '@/entities/session'
import { LogoutButton } from '@/features/auth'
import { paths } from '@/shared/config'
import { cx } from '@/shared/lib'
import { ApiStatus } from './ApiStatus'
import styles from './AppHeader.module.css'

const links = [
  { to: paths.roadmap, label: 'Роадмап' },
  { to: paths.today, label: 'Сегодня' },
  { to: paths.knowledge, label: 'База знаний' },
  { to: paths.settings, label: 'Настройки' },
]

export function AppHeader() {
  const me = useMe()

  return (
    <header className={styles.header}>
      <span className={styles.brand}>Roadmap++</span>
      <nav className={styles.nav} aria-label="Разделы">
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            end={link.to === paths.roadmap}
            className={({ isActive }) => cx(styles.link, isActive && styles.active)}
          >
            {link.label}
          </NavLink>
        ))}
      </nav>
      <ApiStatus />
      {me.data && (
        <div className={styles.user}>
          <span className={styles.userName}>{me.data.displayName}</span>
          <LogoutButton />
        </div>
      )}
    </header>
  )
}
