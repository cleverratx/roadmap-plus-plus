export const paths = {
  roadmap: '/',
  today: '/today',
  knowledge: '/knowledge',
  settings: '/settings',
  teams: '/settings/teams',
  invite: '/invite',
  login: '/login',
  register: '/register',
} as const

export function teamPath(workspaceId: string): string {
  return `${paths.teams}/${workspaceId}`
}

export function invitePath(token: string): string {
  return `${paths.invite}/${encodeURIComponent(token)}`
}
