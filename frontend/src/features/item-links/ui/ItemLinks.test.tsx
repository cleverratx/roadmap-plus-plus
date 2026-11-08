import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { itemQueryKey, useItem, type ItemDetails } from '@/entities/item'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { ItemLinks } from './ItemLinks'

const hackathon = { id: 'event', workspaceId: 'skb', parentId: null, kind: 'event' as const, title: 'Хакатон', startDate: '2026-11-14', endDate: '2026-11-16', position: 0 }
const repo = { id: 'repo', url: 'https://github.com/skb/hackathon', title: '', kind: 'repo' as const, position: 0 }

function LinksOfCachedItem() {
  const details = useItem('event')
  return details.data ? <ItemLinks itemId="event" links={details.data.links} /> : null
}

function renderLinks(links = [repo]) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity }, mutations: { retry: false } } })
  const details: ItemDetails = { item: hackathon, descriptionMd: '', path: [], children: [], links }
  client.setQueryData(itemQueryKey('event'), details)
  render(
    <QueryClientProvider client={client}>
      <LinksOfCachedItem />
    </QueryClientProvider>,
  )
}

function section() {
  return screen.getByRole('region', { name: 'Ссылки' })
}

describe('ItemLinks', () => {
  it('shows each link with its kind and opens it in a new tab', () => {
    mockApi({})
    renderLinks()

    const link = within(section()).getByRole('link', { name: 'github.com/skb/hackathon' })
    expect(link).toHaveAttribute('href', repo.url)
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    expect(within(section()).getByText('Репозиторий')).toBeInTheDocument()
  })

  it('adds a link and shows it right away', async () => {
    const post = { id: 'post', url: 'https://t.me/skb_hack/42', title: 'Положение', kind: 'tgPost', position: 1 }
    const fetchMock = mockApi({ 'POST /api/items/event/links': () => json(post, 201) })
    renderLinks()

    await userEvent.click(within(section()).getByRole('button', { name: '+ Ссылка' }))
    await userEvent.type(screen.getByLabelText('Адрес'), 't.me/skb_hack/42')
    await userEvent.type(screen.getByLabelText('Название, если нужно своё'), 'Положение')
    await userEvent.click(screen.getByRole('button', { name: 'Добавить' }))

    expect(await within(section()).findByRole('link', { name: 'Положение' })).toHaveAttribute('href', post.url)
    expect(within(section()).getByText('Пост в Telegram')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Новая ссылка' })).not.toBeInTheDocument()
    const request = fetchMock.mock.calls[0][0] as Request
    expect(await request.json()).toEqual({ url: 't.me/skb_hack/42', title: 'Положение' })
  })

  it('shows why the server did not take the address', async () => {
    mockApi({
      'POST /api/items/event/links': () =>
        problem(400, { code: 'planning.invalid_request', errors: { url: ['Это не похоже на адрес сайта.'] } }),
    })
    renderLinks([])

    await userEvent.click(screen.getByRole('button', { name: '+ Ссылка' }))
    await userEvent.type(screen.getByLabelText('Адрес'), 'просто текст')
    await userEvent.click(screen.getByRole('button', { name: 'Добавить' }))

    expect(await screen.findByLabelText('Адрес')).toHaveAccessibleDescription('Это не похоже на адрес сайта.')
  })

  it('edits and deletes a link', async () => {
    const renamed = { ...repo, title: 'Репозиторий команды' }
    mockApi({
      'PUT /api/items/event/links/repo': () => json(renamed),
      'DELETE /api/items/event/links/repo': () => new Response(null, { status: 204 }),
    })
    renderLinks()

    await userEvent.click(screen.getByRole('button', { name: 'Изменить ссылку "github.com/skb/hackathon"' }))
    await userEvent.type(screen.getByLabelText('Название, если нужно своё'), 'Репозиторий команды')
    await userEvent.click(screen.getByRole('button', { name: 'Сохранить' }))
    expect(await screen.findByRole('link', { name: 'Репозиторий команды' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Удалить ссылку "Репозиторий команды"' }))
    await vi.waitFor(() => expect(screen.queryByRole('link')).not.toBeInTheDocument())
    expect(screen.getByText(/всё, что нужно держать под рукой/)).toBeInTheDocument()
  })
})
