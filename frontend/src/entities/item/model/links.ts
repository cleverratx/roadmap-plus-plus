import type { components } from '@/shared/api'

export type Link = components['schemas']['LinkResponse']

export type LinkKind = components['schemas']['LinkKind']

export const linkKindLabels: Record<LinkKind, string> = {
  googleSheet: 'Google Таблица',
  googleDoc: 'Google Документ',
  googleSlides: 'Google Презентация',
  googleDrive: 'Google Диск',
  tgPost: 'Пост в Telegram',
  telegram: 'Telegram',
  repo: 'Репозиторий',
  figma: 'Figma',
  notion: 'Notion',
  miro: 'Miro',
  other: 'Ссылка',
}

/** Что написать на ссылке: своё название, а если его нет, то адрес без схемы и "www", вроде "github.com/skb/hackathon". */
export function linkTitle(link: Pick<Link, 'url' | 'title'>): string {
  if (link.title) {
    return link.title
  }

  try {
    const url = new URL(link.url)
    const host = url.hostname.replace(/^www\./, '')
    const path = url.pathname.replace(/\/+$/, '')
    return `${host}${path}`
  } catch {
    return link.url
  }
}
