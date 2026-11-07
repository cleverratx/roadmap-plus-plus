import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { json, mockApi } from '@/shared/lib/test/mock-api'
import { Timeline } from './Timeline'

const now = new Date(2026, 10, 4, 15, 30)
const study = { id: 'study', color: '#2b8a3e' }
const hackathons = { id: 'hackathons', color: '#3b5bdb' }

const items = [
  { id: 'macro', workspaceId: 'hackathons', parentId: null, kind: 'macro', title: 'Проектная деятельность', startDate: '2026-09-01', endDate: '2026-12-31', position: 0 },
  { id: 'event', workspaceId: 'hackathons', parentId: 'macro', kind: 'event', title: 'Хакатон', startDate: '2026-11-03', endDate: '2026-11-05', position: 0 },
  { id: 'day', workspaceId: 'hackathons', parentId: 'event', kind: 'group', title: 'День 1', startDate: '2026-11-03', endDate: '2026-11-03', position: 0 },
  { id: 'lecture', workspaceId: 'study', parentId: null, kind: 'event', title: 'Лекция', startDate: '2026-11-10', endDate: '2026-11-10', position: 0 },
]

function renderTimeline(hidden: ReadonlySet<string> = new Set()) {
  const fetchMock = mockApi({ 'GET /api/items': () => json(items) })
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <Timeline workspaces={[study, hackathons]} hiddenWorkspaceIds={hidden} now={now} />
    </QueryClientProvider>,
  )
  return { svg: screen.getByRole('img', { name: /Таймлайн/ }), fetchMock }
}

describe('Timeline', () => {
  it('opens on a quarter with weeks under months and a today line', () => {
    const { svg } = renderTimeline()

    expect(within(svg).getByText('Ноябрь 2026')).toBeInTheDocument()
    expect(within(svg).getByText('2 ноя')).toBeInTheDocument()
    expect(within(svg).getByText('сегодня')).toBeInTheDocument()
  })

  it('draws items of the visible range and asks the API for a padded window', async () => {
    const { svg, fetchMock } = renderTimeline()

    expect(await within(svg).findByText('Проектная деятельность')).toBeInTheDocument()
    expect(within(svg).queryByText('День 1')).not.toBeInTheDocument()
    const url = new URL((fetchMock.mock.calls[0][0] as Request).url)
    expect([url.searchParams.get('from'), url.searchParams.get('to')]).toEqual(['2026-08-01', '2027-02-28'])
  })

  it('opens an event into its days when zoomed to a week', async () => {
    const { svg } = renderTimeline()
    await within(svg).findByText('Проектная деятельность')

    await userEvent.click(screen.getByRole('button', { name: 'Неделя' }))

    expect(within(svg).getByText('ср 4')).toBeInTheDocument()
    expect(await within(svg).findByText('День 1')).toBeInTheDocument()
  })

  it('hides teams that are switched off', async () => {
    const { svg } = renderTimeline(new Set(['study']))

    expect(await within(svg).findByText('Проектная деятельность')).toBeInTheDocument()
    expect(within(svg).queryByText('Лекция')).not.toBeInTheDocument()
  })

  it('switches to months for a year and returns to today', async () => {
    const { svg } = renderTimeline()
    const range = () => screen.getByRole('toolbar').querySelector('[aria-live]')!.textContent

    await userEvent.click(screen.getByRole('button', { name: 'Год' }))
    expect(within(svg).getByText('ноя')).toBeInTheDocument()
    const year = range()

    await userEvent.click(screen.getByRole('button', { name: 'Отдалить' }))
    expect(range()).not.toBe(year)

    await userEvent.click(screen.getByRole('button', { name: 'Сегодня' }))
    expect(range()).toContain('2026')
  })
})
