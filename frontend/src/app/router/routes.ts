import type { RouteObject } from 'react-router'
import { AgendaPage } from '@/pages/agenda'
import { InvitePage } from '@/pages/invite'
import { KnowledgePage } from '@/pages/knowledge'
import { LoginPage } from '@/pages/login'
import { NotFoundPage } from '@/pages/not-found'
import { RegisterPage } from '@/pages/register'
import { RoadmapPage } from '@/pages/roadmap'
import { SettingsPage } from '@/pages/settings'
import { TeamPage } from '@/pages/team'
import { paths } from '@/shared/config'
import { AppLayout } from '../layout/AppLayout'
import { GuestOnly } from './GuestOnly'
import { RequireAuth } from './RequireAuth'
import { RouteError } from './RouteError'

export const routes: RouteObject[] = [
  {
    ErrorBoundary: RouteError,
    children: [
      {
        Component: GuestOnly,
        children: [
          { path: paths.login, Component: LoginPage },
          { path: paths.register, Component: RegisterPage },
        ],
      },
      {
        Component: RequireAuth,
        children: [
          {
            path: paths.roadmap,
            Component: AppLayout,
            children: [
              { index: true, Component: RoadmapPage },
              { path: paths.today, Component: AgendaPage },
              { path: paths.knowledge, Component: KnowledgePage },
              { path: paths.settings, Component: SettingsPage },
              { path: `${paths.teams}/:workspaceId`, Component: TeamPage },
              { path: `${paths.invite}/:token`, Component: InvitePage },
              { path: '*', Component: NotFoundPage },
            ],
          },
        ],
      },
    ],
  },
]
