import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { itemQueryKey, useItem, type ItemDetails } from '@/entities/item'
import type { WorkspaceDetails } from '@/entities/workspace'
import { json, mockApi } from '@/shared/lib/test/mock-api'
import { ItemAccess } from './ItemAccess'

const jury = { id: 'jury', name: 'Жюри', color: '#7048e8', memberCount: 1 }
const mentors = { id: 'mentors', name: 'Менторы', color: '#1098ad', memberCount: 0 }
const day = { id: 'day', workspaceId: 'skb', parentId: 'event', kind: 'group' as const, title: 'День 1', startDate: null, endDate: null, position: 0, isFrame: false }

const details: ItemDetails = { item: day, descriptionMd: '', path: [], children: [], links: [], roleIds: ['mentors'], inheritedRoleIds: ['jury'] }

function team(overrides: Partial<WorkspaceDetails> = {}): WorkspaceDetails {
  return { id: 'skb', name: 'СКБ', color: '#3b5bdb', isHead: true, rolesEnabled: true, roles: [jury, mentors], members: [], ...overrides }
}

function Access() {
  const item = useItem('day')
  return item.data ? <ItemAccess details={item.data} /> : null
}

function renderAccess(workspace: WorkspaceDetails, item: ItemDetails = details) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity }, mutations: { retry: false } } })
  client.setQueryData(itemQueryKey('day'), item)
  const fetchMock = mockApi({ 'GET /api/workspaces/skb': () => json(workspace) })
  render(
    <QueryClientProvider client={client}>
      <Access />
    </QueryClientProvider>,
  )
  return fetchMock
}

describe('ItemAccess', () => {
  it('shows own roles and roles that come from ancestors', async () => {
    renderAccess(team())

    const access = await screen.findByRole('region', { name: 'Доступ' })
    expect(within(access).getByText('Открыт ролям:').parentElement).toHaveTextContent('Менторы')
    expect(within(access).getByText('Через предков:').parentElement).toHaveTextContent('Жюри')
  })

  it('warns when nobody but heads can see the item', async () => {
    renderAccess(team(), { ...details, roleIds: [], inheritedRoleIds: [] })

    expect(await screen.findByText(/видят только главы/)).toBeInTheDocument()
  })

  it('explains that roles are off', async () => {
    renderAccess(team({ rolesEnabled: false }))

    expect(await screen.findByText(/Роли в команде выключены/)).toBeInTheDocument()
  })

  it('stays hidden while the team has no roles at all', async () => {
    const fetchMock = renderAccess(team({ rolesEnabled: false, roles: [] }))

    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalled())
    expect(screen.queryByRole('region', { name: 'Доступ' })).not.toBeInTheDocument()
  })

  it('lets the head change which roles see the item', async () => {
    const fetchMock = renderAccess(team())
    fetchMock.mockImplementation(async (input: RequestInfo | URL) => {
      const request = input as Request
      if (request.method === 'PUT') {
        return json({ ...details, roleIds: ['jury', 'mentors'] })
      }
      return json(team())
    })

    await userEvent.click(await screen.findByRole('button', { name: 'Изменить доступ' }))
    const dialog = screen.getByRole('dialog', { name: 'Доступ к "День 1"' })
    expect(within(dialog).getByRole('checkbox', { name: 'Менторы' })).toBeChecked()
    await userEvent.click(within(dialog).getByRole('checkbox', { name: 'Жюри' }))
    await userEvent.click(within(dialog).getByRole('button', { name: 'Сохранить' }))

    await vi.waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    const put = fetchMock.mock.calls.map(([call]) => call as Request).find((call) => call.method === 'PUT')!
    expect(new URL(put.url).pathname).toBe('/api/items/day/roles')
    expect((await put.json()).roleIds.toSorted()).toEqual(['jury', 'mentors'])
  })

  it('does not offer changes to a regular member', async () => {
    renderAccess(team({ isHead: false }))

    await screen.findByRole('region', { name: 'Доступ' })
    expect(screen.queryByRole('button', { name: 'Изменить доступ' })).not.toBeInTheDocument()
  })
})
