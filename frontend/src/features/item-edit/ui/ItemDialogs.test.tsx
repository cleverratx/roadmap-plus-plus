import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { CreateItemDialog } from './CreateItemDialog'
import { DeleteItemDialog } from './DeleteItemDialog'

const study = { id: 'study', name: 'Учёба', color: '#2b8a3e', isHead: true, memberCount: 1 }
const skb = { id: 'skb', name: 'СКБ', color: '#3b5bdb', isHead: false, memberCount: 3 }
const hackathon = { id: 'event', workspaceId: 'skb', parentId: null, kind: 'event' as const, title: 'Хакатон', startDate: '2026-11-03', endDate: '2026-11-05', position: 0 }

function renderWithQuery(children: ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  render(<QueryClientProvider client={client}>{children}</QueryClientProvider>)
  return client
}

async function sentBody(fetchMock: ReturnType<typeof mockApi>, method: string) {
  const request = fetchMock.mock.calls.map(([call]) => call as Request).find((call) => call.method === method)!
  return request.json()
}

describe('CreateItemDialog', () => {
  it('creates a root event on the clicked day in the chosen team', async () => {
    const created = { item: { ...hackathon, id: 'new', title: 'Лекция', workspaceId: 'study' }, descriptionMd: '', path: [], children: [], links: [] }
    const fetchMock = mockApi({ 'POST /api/items': () => json(created, 201) })
    const onCreated = vi.fn()
    renderWithQuery(
      <CreateItemDialog
        draft={{ parent: null, kind: 'event', startDate: '2026-11-10', endDate: '2026-11-10', workspaceId: 'skb' }}
        workspaces={[study, skb]}
        onClose={vi.fn()}
        onCreated={onCreated}
      />,
    )

    expect(screen.getByRole('dialog', { name: 'Новое событие' })).toBeInTheDocument()
    await userEvent.click(screen.getByRole('radio', { name: 'Учёба' }))
    await userEvent.type(screen.getByLabelText('Название'), 'Лекция')
    await userEvent.click(screen.getByRole('button', { name: 'Создать' }))

    await vi.waitFor(() => expect(onCreated).toHaveBeenCalledWith(created))
    expect(await sentBody(fetchMock, 'POST')).toEqual({
      workspaceId: 'study',
      parentId: null,
      kind: 'event',
      title: 'Лекция',
      startDate: '2026-11-10',
      endDate: '2026-11-10',
      descriptionMd: '',
    })
  })

  it('puts a child inside its parent and lets it go without dates', async () => {
    const fetchMock = mockApi({
      'POST /api/items': () => json({ item: { ...hackathon, id: 'repo' }, descriptionMd: '', path: [hackathon], children: [], links: [] }, 201),
    })
    renderWithQuery(
      <CreateItemDialog
        draft={{ parent: hackathon, kind: 'task', startDate: hackathon.startDate, endDate: hackathon.endDate }}
        workspaces={[study, skb]}
        onClose={vi.fn()}
        onCreated={vi.fn()}
      />,
    )

    expect(screen.getByText('Внутри "Хакатон"')).toBeInTheDocument()
    expect(screen.queryByRole('radiogroup', { name: 'Команда' })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('radio', { name: 'Группа' }))
    expect(screen.getByRole('dialog', { name: 'Новая группа' })).toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('Название'), 'Организация и репозиторий')
    await userEvent.clear(screen.getByLabelText('Начало'))
    await userEvent.clear(screen.getByLabelText('Конец'))
    await userEvent.click(screen.getByRole('button', { name: 'Создать' }))

    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalled())
    expect(await sentBody(fetchMock, 'POST')).toMatchObject({ parentId: 'event', workspaceId: null, kind: 'group', startDate: null, endDate: null })
  })

  it('keeps a root item from losing its dates and shows server errors next to fields', async () => {
    const fetchMock = mockApi({
      'POST /api/items': () =>
        problem(400, { code: 'planning.invalid_request', detail: 'Проверьте поля формы.', errors: { title: ['Назовите узел.'] } }),
    })
    renderWithQuery(
      <CreateItemDialog draft={{ parent: null, kind: 'macro', startDate: null, endDate: null }} workspaces={[study]} onClose={vi.fn()} onCreated={vi.fn()} />,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Создать' }))
    expect(screen.getByLabelText('Начало')).toHaveAccessibleDescription(/не будет видно на таймлайне/)
    expect(fetchMock).not.toHaveBeenCalled()

    await userEvent.type(screen.getByLabelText('Начало'), '2026-09-01')
    expect(screen.getByLabelText('Конец')).toHaveValue('2026-09-01')
    await userEvent.click(screen.getByRole('button', { name: 'Создать' }))

    expect(await screen.findByText('Назовите узел.')).toBeInTheDocument()
  })
})

describe('DeleteItemDialog', () => {
  it('deletes the item after confirmation', async () => {
    const fetchMock = mockApi({ 'DELETE /api/items/event': () => new Response(null, { status: 204 }) })
    const onDeleted = vi.fn()
    renderWithQuery(<DeleteItemDialog item={hackathon} onClose={vi.fn()} onDeleted={onDeleted} />)

    expect(screen.getByRole('dialog', { name: 'Удалить "Хакатон"?' })).toHaveAccessibleDescription(/удалится всё, что внутри/)
    await userEvent.click(screen.getByRole('button', { name: 'Удалить' }))

    await vi.waitFor(() => expect(onDeleted).toHaveBeenCalledWith(hackathon))
    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('closes without deleting on cancel', async () => {
    const fetchMock = mockApi({})
    const onClose = vi.fn()
    renderWithQuery(<DeleteItemDialog item={hackathon} onClose={onClose} onDeleted={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Отмена' }))

    expect(onClose).toHaveBeenCalled()
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
