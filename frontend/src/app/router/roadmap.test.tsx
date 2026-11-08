import { QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { json, mockApi } from '@/shared/lib/test/mock-api'
import { createQueryClient } from '../providers/query-client'
import { routes } from './routes'

const me = { id: '11111111-0000-7000-8000-000000000001', email: 'yaroslav@example.com', displayName: 'Ярослав' }
const team = { id: 'skb', name: 'СКБ', color: '#3b5bdb', isHead: true, memberCount: 1 }
const today = new Date()
const iso = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
const hackathon = { id: 'event', workspaceId: 'skb', parentId: null, kind: 'event', title: 'Хакатон', startDate: iso(today), endDate: iso(today), position: 0 }
const day = { id: 'day', workspaceId: 'skb', parentId: 'event', kind: 'group', title: 'День 1', startDate: iso(today), endDate: iso(today), position: 0 }

const common = {
  'GET /api/me': () => json(me),
  'GET /api/health': () => new Response('Healthy'),
  'GET /api/workspaces': () => json([team]),
}

function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  render(
    <QueryClientProvider client={createQueryClient()}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  return router
}

describe('roadmap', () => {
  it('opens a block in the side panel and keeps it in the address', async () => {
    mockApi({
      ...common,
      'GET /api/items': () => json([hackathon]),
      'GET /api/items/event': () => json({ item: hackathon, descriptionMd: '', path: [], children: [day], links: [] }),
      'GET /api/items/day': () => json({ item: day, descriptionMd: '', path: [hackathon], children: [], links: [] }),
    })
    const router = renderApp('/')

    fireEvent.click(await screen.findByRole('button', { name: /Событие "Хакатон"/ }))

    const panel = await screen.findByRole('complementary', { name: 'Хакатон' })
    expect(router.state.location.search).toBe('?item=event')

    await userEvent.click(within(panel).getByRole('button', { name: /День 1/ }))
    expect(await screen.findByRole('complementary', { name: 'День 1' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Закрыть панель' }))
    expect(screen.queryByRole('complementary')).not.toBeInTheDocument()
    expect(router.state.location.search).toBe('')
  })

  it('creates an event from the right click menu and opens it', async () => {
    const created = { ...hackathon, id: 'new', title: 'Лекция' }
    mockApi({
      ...common,
      'GET /api/items': () => json([]),
      'POST /api/items': () => json({ item: created, descriptionMd: '', path: [], children: [], links: [] }, 201),
      'GET /api/items/new': () => json({ item: created, descriptionMd: '', path: [], children: [], links: [] }),
    })
    const router = renderApp('/')

    fireEvent.contextMenu(await screen.findByRole('group', { name: /Таймлайн/ }), { clientX: 490 })
    await userEvent.click(screen.getByRole('menuitem', { name: 'Новое событие здесь' }))
    await userEvent.type(screen.getByLabelText('Название'), 'Лекция')
    await userEvent.click(screen.getByRole('button', { name: 'Создать' }))

    expect(await screen.findByRole('complementary', { name: 'Лекция' })).toBeInTheDocument()
    expect(router.state.location.search).toBe('?item=new')
  })

  it('closes the panel of a deleted root item', async () => {
    mockApi({
      ...common,
      'GET /api/items': () => json([hackathon]),
      'GET /api/items/event': () => json({ item: hackathon, descriptionMd: '', path: [], children: [], links: [] }),
      'DELETE /api/items/event': () => new Response(null, { status: 204 }),
    })
    const router = renderApp('/?item=event')

    const panel = await screen.findByRole('complementary', { name: 'Хакатон' })
    await userEvent.click(within(panel).getByRole('button', { name: 'Удалить' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Удалить' }))

    await vi.waitFor(() => expect(screen.queryByRole('complementary')).not.toBeInTheDocument())
    expect(router.state.location.search).toBe('')
  })
})
