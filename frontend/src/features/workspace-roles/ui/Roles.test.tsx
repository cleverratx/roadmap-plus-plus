import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { useWorkspace, workspaceQueryKey, type WorkspaceDetails } from '@/entities/workspace'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { MemberRolesDialog } from './MemberRolesDialog'
import { RolesSection } from './RolesSection'

const jury = { id: 'jury', name: 'Жюри', color: '#7048e8', memberCount: 1 }
const mentors = { id: 'mentors', name: 'Менторы', color: '#1098ad', memberCount: 0 }
const nastya = { userId: 'nastya', displayName: 'Анастасия', email: 'nastya@example.com', color: '#2b8a3e', isHead: false, joinedAt: '2026-11-01T10:00:00Z', roleIds: ['jury'] }

const team: WorkspaceDetails = {
  id: 'skb',
  name: 'СКБ',
  color: '#3b5bdb',
  isHead: true,
  rolesEnabled: false,
  roles: [jury, mentors],
  members: [nastya],
}

function Section() {
  const workspace = useWorkspace('skb')
  return workspace.data ? (
    <>
      <RolesSection workspace={workspace.data} />
      <MemberRolesDialog workspace={workspace.data} member={workspace.data.members[0]} />
    </>
  ) : null
}

function renderRoles(workspace: WorkspaceDetails = team) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity }, mutations: { retry: false } } })
  client.setQueryData(workspaceQueryKey('skb'), workspace)
  render(
    <QueryClientProvider client={client}>
      <Section />
    </QueryClientProvider>,
  )
}

function lastBody(fetchMock: ReturnType<typeof mockApi>) {
  const requests = fetchMock.mock.calls.map(([call]) => call as Request)
  return requests.findLast((request) => request.method !== 'GET')!.json()
}

describe('RolesSection', () => {
  it('lets the head switch roles on', async () => {
    const fetchMock = mockApi({ 'PUT /api/workspaces/skb/roles-enabled': () => json({ ...team, rolesEnabled: true }) })
    renderRoles()

    const flag = screen.getByRole('checkbox', { name: 'Включить роли' })
    expect(flag).not.toBeChecked()
    expect(flag).toHaveAccessibleDescription(/Сейчас все участники видят все события команды/)
    await userEvent.click(flag)

    await vi.waitFor(() => expect(screen.getByRole('checkbox', { name: 'Включить роли' })).toBeChecked())
    expect(await lastBody(fetchMock)).toEqual({ enabled: true })
  })

  it('adds a role with the next free color', async () => {
    const designers = { id: 'design', name: 'Дизайн', color: '#e8590c', memberCount: 0 }
    const fetchMock = mockApi({ 'POST /api/workspaces/skb/roles': () => json({ ...team, roles: [jury, mentors, designers] }) })
    renderRoles()

    await userEvent.click(screen.getByRole('button', { name: '+ Роль' }))
    await userEvent.type(screen.getByLabelText('Название роли'), 'Дизайн')
    await userEvent.click(screen.getByRole('button', { name: 'Добавить роль' }))

    expect(await screen.findByText('Дизайн')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Новая роль' })).not.toBeInTheDocument()
    expect(await lastBody(fetchMock)).toEqual({ name: 'Дизайн', color: '#e8590c' })
  })

  it('shows a taken name next to the field', async () => {
    mockApi({
      'POST /api/workspaces/skb/roles': () =>
        problem(400, { code: 'identity.role_name_taken', errors: { name: ['Роль с таким названием в команде уже есть.'] } }),
    })
    renderRoles()

    await userEvent.click(screen.getByRole('button', { name: '+ Роль' }))
    await userEvent.type(screen.getByLabelText('Название роли'), 'жюри')
    await userEvent.click(screen.getByRole('button', { name: 'Добавить роль' }))

    expect(await screen.findByLabelText('Название роли')).toHaveAccessibleDescription('Роль с таким названием в команде уже есть.')
  })

  it('renames and deletes a role after confirmation', async () => {
    const renamed = { ...jury, name: 'Судьи' }
    mockApi({
      'PATCH /api/workspaces/skb/roles/jury': () => json({ ...team, roles: [renamed, mentors] }),
      'DELETE /api/workspaces/skb/roles/jury': () => json({ ...team, roles: [mentors], members: [{ ...nastya, roleIds: [] }] }),
    })
    renderRoles()

    await userEvent.click(screen.getByRole('button', { name: 'Изменить роль "Жюри"' }))
    const name = screen.getByLabelText('Название роли')
    await userEvent.clear(name)
    await userEvent.type(name, 'Судьи')
    await userEvent.click(screen.getByRole('button', { name: 'Сохранить' }))
    expect(await screen.findByRole('button', { name: 'Удалить роль "Судьи"' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Удалить роль "Судьи"' }))
    const dialog = screen.getByRole('dialog', { name: 'Удалить роль "Судьи"?' })
    await userEvent.click(within(dialog).getByRole('button', { name: 'Удалить' }))

    await vi.waitFor(() => expect(screen.queryByText('Судьи')).not.toBeInTheDocument())
  })

  it('warns the head about events no role can see', async () => {
    mockApi({
      'GET /api/items/access-summary': () =>
        json({ unsharedEvents: 7, examples: [{ id: 'a', title: 'Хакатон' }, { id: 'b', title: 'Конференция' }] }),
    })
    renderRoles({ ...team, rolesEnabled: true })

    const warning = await screen.findByRole('status')
    expect(warning).toHaveTextContent('7 событий не открыты ни одной роли. Сейчас их видят только главы.')
    expect(warning).toHaveTextContent('"Хакатон", "Конференция" и другие')
  })

  it('only shows roles to a regular member', () => {
    mockApi({})
    renderRoles({ ...team, isHead: false, rolesEnabled: true })

    expect(screen.queryByRole('checkbox', { name: 'Включить роли' })).not.toBeInTheDocument()
    expect(screen.getByText(/Роли включены/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '+ Роль' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Изменить роль/ })).not.toBeInTheDocument()
  })
})

describe('MemberRolesDialog', () => {
  it('gives a member exactly the checked roles', async () => {
    const fetchMock = mockApi({
      'PUT /api/workspaces/skb/members/nastya/roles': () => json({ ...team, members: [{ ...nastya, roleIds: ['mentors'] }] }),
    })
    renderRoles()

    await userEvent.click(screen.getByRole('button', { name: 'Роли участника Анастасия' }))
    const dialog = screen.getByRole('dialog', { name: 'Роли: Анастасия' })
    expect(within(dialog).getByRole('checkbox', { name: 'Жюри' })).toBeChecked()
    await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Жюри' }))
    await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Менторы' }))
    await userEvent.click(within(dialog).getByRole('button', { name: 'Сохранить' }))

    await vi.waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(await lastBody(fetchMock)).toEqual({ roleIds: ['mentors'] })
  })
})
