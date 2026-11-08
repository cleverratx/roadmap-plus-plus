import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ComponentProps } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { EventPanel } from './EventPanel'

const team = { id: 'team', name: 'СКБ', color: '#3b5bdb', isHead: true, memberCount: 2 }
const macro = { id: 'macro', workspaceId: 'team', parentId: null, kind: 'macro', title: 'Проектная деятельность', startDate: '2026-09-01', endDate: '2026-12-31', position: 0 }
const hackathon = { id: 'event', workspaceId: 'team', parentId: 'macro', kind: 'event', title: 'Хакатон', startDate: '2026-12-30', endDate: '2027-01-02', position: 0 }
const day = { id: 'day', workspaceId: 'team', parentId: 'event', kind: 'group', title: 'День 1', startDate: '2026-12-30', endDate: '2026-12-30', position: 0 }
const repo = { id: 'repo', workspaceId: 'team', parentId: 'event', kind: 'group', title: 'Организация и репозиторий', startDate: null, endDate: null, position: 1 }

const sheet = { id: 'sheet', url: 'https://docs.google.com/spreadsheets/d/1AbC', title: '', kind: 'googleSheet', position: 0 }

const details = { item: hackathon, descriptionMd: 'Командный хакатон СКБ', path: [macro], children: [day, repo], links: [sheet] }

function renderPanel(handlers: Partial<ComponentProps<typeof EventPanel>> = {}) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <EventPanel
        itemId="event"
        workspaces={[team]}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        onAddChild={vi.fn()}
        onDelete={vi.fn()}
        onMove={vi.fn()}
        {...handlers}
      />
    </QueryClientProvider>,
  )
}

describe('EventPanel', () => {
  it('shows where the item lives, its dates, description and what is inside, including undated parts', async () => {
    mockApi({ 'GET /api/items/event': () => json(details) })
    const onOpen = vi.fn()
    renderPanel({ onOpen })

    expect(await screen.findByRole('heading', { name: 'Хакатон' })).toBeInTheDocument()
    expect(screen.getByText('30 декабря 2026 - 2 января 2027')).toBeInTheDocument()
    expect(screen.getByText('СКБ')).toBeInTheDocument()
    expect(screen.getByText('Командный хакатон СКБ')).toBeInTheDocument()
    expect(screen.getByText(/Даты выходят за рамки "Проектная деятельность"/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'docs.google.com/spreadsheets/d/1AbC' })).toHaveAttribute('href', sheet.url)

    const inside = screen.getByRole('region', { name: 'Внутри' })
    expect(within(inside).getByRole('button', { name: /^Организация и репозиторий/ })).toHaveTextContent('Группа, без дат')

    await userEvent.click(within(inside).getByRole('button', { name: /^День 1/ }))
    expect(onOpen).toHaveBeenCalledWith('day')

    await userEvent.click(screen.getByRole('button', { name: 'Проектная деятельность' }))
    expect(onOpen).toHaveBeenCalledWith('macro')
  })

  it('asks the page to add a child of the chosen kind, to move or to delete the item', async () => {
    mockApi({ 'GET /api/items/event': () => json(details) })
    const onAddChild = vi.fn()
    const onDelete = vi.fn()
    const onMove = vi.fn()
    renderPanel({ onAddChild, onDelete, onMove })

    await userEvent.click(await screen.findByRole('button', { name: '+ Задача' }))
    expect(onAddChild).toHaveBeenCalledWith(hackathon, 'task')

    await userEvent.click(screen.getByRole('button', { name: 'Переместить' }))
    expect(onMove).toHaveBeenCalledWith(hackathon)

    await userEvent.click(screen.getByRole('button', { name: 'Удалить' }))
    expect(onDelete).toHaveBeenCalledWith(hackathon)
  })

  it('reorders what is inside', async () => {
    let current = details
    const fetchMock = mockApi({
      'GET /api/items/event': () => json(current),
      'POST /api/items/day/move': () => {
        current = { ...details, children: [repo, day] }
        return json({ item: day, descriptionMd: '', path: [macro, hackathon], children: [], links: [] })
      },
    })
    renderPanel()

    expect(await screen.findByRole('button', { name: 'Поднять "День 1"' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Опустить "Организация и репозиторий"' })).toBeDisabled()
    await userEvent.click(screen.getByRole('button', { name: 'Опустить "День 1"' }))

    const inside = screen.getByRole('region', { name: 'Внутри' })
    await vi.waitFor(() =>
      expect(within(inside).getAllByRole('listitem').map((row) => row.textContent)).toEqual([
        expect.stringContaining('Организация и репозиторий'),
        expect.stringContaining('День 1'),
      ]),
    )
    const move = fetchMock.mock.calls.map(([request]) => request as Request).find((request) => request.method === 'POST')!
    expect(await move.json()).toEqual({ parentId: 'event', position: 1 })
  })

  it('edits the item in place', async () => {
    const renamed = { ...details, item: { ...hackathon, title: 'Хакатон СКБ' } }
    const fetchMock = mockApi({
      'GET /api/items/event': () => json(details),
      'PUT /api/items/event': () => json(renamed),
    })
    renderPanel()

    await userEvent.click(await screen.findByRole('button', { name: 'Изменить' }))
    const title = screen.getByLabelText('Название')
    await userEvent.clear(title)
    await userEvent.type(title, 'Хакатон СКБ')
    await userEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    expect(await screen.findByRole('heading', { name: 'Хакатон СКБ' })).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Правка узла' })).not.toBeInTheDocument()
    const put = fetchMock.mock.calls.map(([request]) => request as Request).find((request) => request.method === 'PUT')!
    expect(await put.json()).toEqual({
      title: 'Хакатон СКБ',
      startDate: '2026-12-30',
      endDate: '2027-01-02',
      descriptionMd: 'Командный хакатон СКБ',
    })
  })

  it('says so when the item is gone', async () => {
    mockApi({ 'GET /api/items/event': () => problem(404, { code: 'planning.item_not_found', detail: 'Узел не найден.' }) })
    renderPanel()

    expect(await screen.findByRole('alert')).toHaveTextContent('Узел не найден.')
  })
})
