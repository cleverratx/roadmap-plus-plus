import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { MoveItemDialog } from './MoveItemDialog'

const study = { id: 'study', workspaceId: 'team', parentId: null, kind: 'macro' as const, title: 'Учёба', startDate: '2026-09-01', endDate: '2027-06-30', position: 0 }
const projects = { id: 'projects', workspaceId: 'team', parentId: null, kind: 'macro' as const, title: 'Проектная деятельность', startDate: '2026-09-01', endDate: '2026-12-31', position: 1 }
const semester = { id: 'semester', workspaceId: 'team', parentId: 'study', kind: 'macro' as const, title: 'Осенний семестр', startDate: '2026-09-01', endDate: '2026-12-31', position: 0 }
const hackathon = { id: 'event', workspaceId: 'team', parentId: 'study', kind: 'event' as const, title: 'Хакатон', startDate: '2026-11-14', endDate: '2026-11-16', position: 1 }

const targets = {
  canBeRoot: true,
  currentParentId: 'study',
  parents: [
    { item: study, path: [] },
    { item: semester, path: ['Учёба'] },
    { item: projects, path: [] },
  ],
}

function renderDialog(onMoved = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MoveItemDialog item={hackathon} onClose={vi.fn()} onMoved={onMoved} />
    </QueryClientProvider>,
  )
  return onMoved
}

describe('MoveItemDialog', () => {
  it('lists allowed places with their path and marks the current one', async () => {
    mockApi({ 'GET /api/items/event/move-targets': () => json(targets) })
    renderDialog()

    expect(await screen.findByRole('radio', { name: /Верхний уровень команды/ })).toBeEnabled()
    expect(screen.getByRole('radio', { name: /^Учёба, сейчас здесь/ })).toBeDisabled()
    expect(screen.getByRole('radio', { name: /Осенний семестр/ })).toHaveAccessibleName('Осенний семестр, Макрособытие / Учёба')
    expect(screen.getByRole('button', { name: 'Переместить' })).toBeDisabled()
  })

  it('narrows the list by search and moves the item to the chosen parent', async () => {
    const moved = { item: { ...hackathon, parentId: 'projects' }, descriptionMd: '', path: [projects], children: [], links: [] }
    const fetchMock = mockApi({
      'GET /api/items/event/move-targets': () => json(targets),
      'POST /api/items/event/move': () => json(moved),
    })
    const onMoved = renderDialog()

    await screen.findByRole('radio', { name: /Проектная деятельность/ })
    await userEvent.type(screen.getByLabelText('Найти'), 'проект')
    expect(screen.getAllByRole('radio')).toHaveLength(1)
    await userEvent.click(screen.getByRole('radio', { name: /Проектная деятельность/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Переместить' }))

    await vi.waitFor(() => expect(onMoved).toHaveBeenCalledWith(moved))
    const request = fetchMock.mock.calls.map(([call]) => call as Request).find((call) => call.method === 'POST')!
    expect(await request.json()).toEqual({ parentId: 'projects', position: null })
  })

  it('shows why the server refused', async () => {
    mockApi({
      'GET /api/items/event/move-targets': () => json(targets),
      'POST /api/items/event/move': () =>
        problem(400, { code: 'planning.move_cycle', detail: 'Нельзя переместить узел внутрь него самого или того, что лежит в нём.' }),
    })
    renderDialog()

    await userEvent.click(await screen.findByRole('radio', { name: /Верхний уровень/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Переместить' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Нельзя переместить узел внутрь него самого')
  })
})
