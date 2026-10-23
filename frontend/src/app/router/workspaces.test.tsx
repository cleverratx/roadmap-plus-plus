import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { json, mockApi } from '@/shared/lib/test/mock-api'
import { createQueryClient } from '../providers/query-client'
import { routes } from './routes'

const me = { id: '11111111-0000-7000-8000-000000000001', email: 'yaroslav@example.com', displayName: 'Ярослав' }
const anastasia = { id: '22222222-0000-7000-8000-000000000002', email: 'nastya@example.com', displayName: 'Анастасия' }
const teamId = '33333333-0000-7000-8000-000000000003'

function member(user: typeof me, isHead: boolean) {
  return { userId: user.id, displayName: user.displayName, email: user.email, color: '#3b5bdb', isHead, joinedAt: '2026-10-20T12:00:00Z' }
}

const team = { id: teamId, name: 'СКБ', color: '#3b5bdb', isHead: true, members: [member(me, true), member(anastasia, false)] }

function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  render(
    <QueryClientProvider client={createQueryClient()}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  return router
}

const common = {
  'GET /api/me': () => json(me),
  'GET /api/health': () => new Response('Healthy'),
}

describe('teams', () => {
  it('offers to create or join a team when there is none', async () => {
    mockApi({ ...common, 'GET /api/workspaces': () => json([]) })
    renderApp('/')

    expect(await screen.findByRole('heading', { name: 'У вас пока нет команды' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Создать команду' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'У меня есть приглашение' })).toBeInTheDocument()
  })

  it('creates a team and opens its page', async () => {
    const fetchMock = mockApi({
      ...common,
      'GET /api/workspaces': () => json([]),
      'POST /api/workspaces': () => json({ ...team, members: [member(me, true)] }, 201),
      [`GET /api/workspaces/${teamId}/invitation`]: () => json({ active: null }),
    })
    const router = renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Создать команду' }))
    await userEvent.type(screen.getByLabelText('Название'), 'СКБ')
    await userEvent.click(screen.getByRole('radio', { name: '#2b8a3e' }))
    await userEvent.click(screen.getByRole('button', { name: 'Создать' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'СКБ' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe(`/settings/teams/${teamId}`)
    const post = fetchMock.mock.calls.map(([request]) => request as Request).find((r) => r.method === 'POST')!
    expect(await post.json()).toEqual({ name: 'СКБ', color: '#2b8a3e' })
  })

  it('turns a pasted invitation link into the invitation page', async () => {
    mockApi({
      ...common,
      'GET /api/workspaces': () => json([]),
      'GET /api/invitations/abc_DEF-123': () =>
        json({ workspaceId: teamId, workspaceName: 'СКБ', color: '#3b5bdb', memberCount: 21, alreadyMember: false }),
    })
    const router = renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'У меня есть приглашение' }))
    await userEvent.type(screen.getByLabelText('Ссылка-приглашение'), 'https://roadmap.example/invite/abc_DEF-123')
    await userEvent.click(screen.getByRole('button', { name: 'Продолжить' }))

    expect(await screen.findByText('21 участник')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/invite/abc_DEF-123')
  })

  it('joins a team by invitation', async () => {
    mockApi({
      ...common,
      'GET /api/invitations/token123': () =>
        json({ workspaceId: teamId, workspaceName: 'СКБ', color: '#3b5bdb', memberCount: 2, alreadyMember: false }),
      'POST /api/invitations/token123/accept': () => json({ id: teamId, name: 'СКБ', color: '#3b5bdb', isHead: false, memberCount: 3 }),
      [`GET /api/workspaces/${teamId}`]: () => json({ ...team, isHead: false }),
      'GET /api/workspaces': () => json([]),
    })
    const router = renderApp('/invite/token123')

    expect(await screen.findByText('2 участника')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Вступить в команду' }))

    expect(await screen.findByText('Вы участник этой команды')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe(`/settings/teams/${teamId}`)
    expect(screen.queryByRole('button', { name: 'Исключить' })).not.toBeInTheDocument()
  })

  it('explains an expired invitation', async () => {
    mockApi({
      ...common,
      'GET /api/invitations/old': () =>
        new Response(JSON.stringify({ status: 404, code: 'identity.invitation_not_found' }), {
          status: 404,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
    })
    renderApp('/invite/old')

    expect(await screen.findByRole('alert')).toHaveTextContent('Попросите у главы команды новую ссылку')
  })

  it('lets the head appoint another head', async () => {
    const fetchMock = mockApi({
      ...common,
      [`GET /api/workspaces/${teamId}`]: () => json(team),
      [`GET /api/workspaces/${teamId}/invitation`]: () => json({ active: { token: 'tok', expiresAt: '2026-10-27T12:00:00Z' } }),
      [`PATCH /api/workspaces/${teamId}/members/${anastasia.id}`]: () =>
        json({ ...team, members: [member(me, true), member(anastasia, true)] }),
      'GET /api/workspaces': () => json([]),
    })
    renderApp(`/settings/teams/${teamId}`)

    const row = (await screen.findByText('Анастасия')).closest('li')!
    await userEvent.click(within(row).getByRole('button', { name: 'Сделать главой' }))

    expect(await within(row).findByRole('button', { name: 'Снять главу' })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'Ссылка-приглашение' })).toHaveValue(`${window.location.origin}/invite/tok`)
    const patch = fetchMock.mock.calls.map(([request]) => request as Request).find((r) => r.method === 'PATCH')!
    expect(await patch.json()).toEqual({ isHead: true })
  })
})
