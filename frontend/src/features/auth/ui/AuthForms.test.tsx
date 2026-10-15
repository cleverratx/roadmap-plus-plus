import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { json, mockApi, problem } from '@/shared/lib/test/mock-api'
import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'

const me = { id: '0192f0c4-0000-7000-8000-000000000001', email: 'yaroslav@example.com', displayName: 'Ярослав' }

function renderWithQuery(children: ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  render(<QueryClientProvider client={client}>{children}</QueryClientProvider>)
  return client
}

describe('LoginForm', () => {
  it('sends credentials and reports the signed in user', async () => {
    const fetchMock = mockApi({ 'POST /api/auth/login': () => json(me) })
    const onSuccess = vi.fn()
    const client = renderWithQuery(<LoginForm onSuccess={onSuccess} />)

    await userEvent.type(screen.getByLabelText('Email'), me.email)
    await userEvent.type(screen.getByLabelText('Пароль'), 'correct horse')
    await userEvent.click(screen.getByRole('button', { name: 'Войти' }))

    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalled())
    expect(onSuccess.mock.calls[0][0]).toEqual(me)
    const request = fetchMock.mock.calls[0][0] as Request
    expect(await request.json()).toEqual({ email: me.email, password: 'correct horse' })
    expect(client.getQueryData(['me'])).toEqual(me)
  })

  it('shows the message for wrong credentials', async () => {
    mockApi({
      'POST /api/auth/login': () =>
        problem(400, { code: 'identity.invalid_credentials', detail: 'Неверный email или пароль.' }),
    })
    renderWithQuery(<LoginForm onSuccess={vi.fn()} />)

    await userEvent.type(screen.getByLabelText('Email'), me.email)
    await userEvent.type(screen.getByLabelText('Пароль'), 'wrong password')
    await userEvent.click(screen.getByRole('button', { name: 'Войти' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Неверный email или пароль.')
  })

  it('explains a network failure', async () => {
    mockApi({ 'POST /api/auth/login': () => Promise.reject(new TypeError('Failed to fetch')) })
    renderWithQuery(<LoginForm onSuccess={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Войти' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось связаться с сервером')
  })
})

describe('RegisterForm', () => {
  it('puts field errors next to the fields', async () => {
    mockApi({
      'POST /api/auth/register': () =>
        problem(400, {
          code: 'identity.invalid_request',
          detail: 'Проверьте поля формы.',
          errors: { password: ['Пароль должен быть не короче 8 символов.'], displayName: ['Укажите, как вас называть.'] },
        }),
    })
    renderWithQuery(<RegisterForm onSuccess={vi.fn()} />)

    await userEvent.type(screen.getByLabelText('Email'), me.email)
    await userEvent.type(screen.getByLabelText('Пароль, не короче 8 символов'), 'short')
    await userEvent.click(screen.getByRole('button', { name: 'Зарегистрироваться' }))

    expect(await screen.findByLabelText('Пароль, не короче 8 символов')).toHaveAccessibleDescription(
      'Пароль должен быть не короче 8 символов.',
    )
    expect(screen.getByLabelText('Как вас называть')).toHaveAccessibleDescription('Укажите, как вас называть.')
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
