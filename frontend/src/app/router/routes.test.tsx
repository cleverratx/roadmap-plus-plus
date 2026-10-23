import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { createQueryClient } from '../providers/query-client'
import { routes } from './routes'

const me = { id: '0192f0c4-0000-7000-8000-000000000001', email: 'yaroslav@example.com', displayName: 'Ярослав' }

function renderApp(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })

  render(
    <QueryClientProvider client={createQueryClient()}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )

  return router
}

function signedIn(health = new Response('Healthy')) {
  return mockApi({
    'GET /api/me': () => json(me),
    'GET /api/health': () => health.clone(),
    'GET /api/workspaces': () => json([]),
    'POST /api/auth/logout': () => new Response(null, { status: 204 }),
  })
}

function signedOut() {
  return mockApi({
    'GET /api/me': () => problem(401),
    'GET /api/health': () => new Response('Healthy'),
    'POST /api/auth/login': () => json(me),
  })
}

describe('routes', () => {
  it('opens the roadmap for a signed in user', async () => {
    signedIn()
    renderApp('/')

    expect(await screen.findByRole('heading', { level: 1, name: 'Роадмап' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Роадмап' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByText('Ярослав')).toBeInTheDocument()
  })

  it('navigates between sections through the header', async () => {
    signedIn()
    renderApp('/')

    await userEvent.click(await screen.findByRole('link', { name: 'Сегодня' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Сегодня / завтра' })).toBeInTheDocument()
  })

  it('shows not found for an unknown path', async () => {
    signedIn()
    renderApp('/no-such-page')

    expect(await screen.findByRole('heading', { level: 1, name: 'Страница не найдена' })).toBeInTheDocument()
  })

  it('shows whether the API works', async () => {
    signedIn(new Response('Unhealthy', { status: 503 }))
    renderApp('/')

    expect(await screen.findByText('API недоступен')).toBeInTheDocument()
  })

  it('sends a guest to the login page and back after signing in', async () => {
    signedOut()
    const router = renderApp('/today')

    expect(await screen.findByRole('heading', { level: 1, name: 'Вход' })).toBeInTheDocument()
    expect(router.state.location.search).toBe('?returnTo=%2Ftoday')

    await userEvent.type(screen.getByLabelText('Email'), me.email)
    await userEvent.type(screen.getByLabelText('Пароль'), 'correct horse')
    await userEvent.click(screen.getByRole('button', { name: 'Войти' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Сегодня / завтра' })).toBeInTheDocument()
  })

  it('returns to the login page after logout', async () => {
    signedIn()
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Выйти' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Вход' })).toBeInTheDocument()
  })

  it('does not show the login page to a signed in user', async () => {
    signedIn()
    renderApp('/login')

    expect(await screen.findByRole('heading', { level: 1, name: 'Роадмап' })).toBeInTheDocument()
  })
})
