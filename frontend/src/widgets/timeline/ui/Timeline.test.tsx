import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Timeline } from './Timeline'

const now = new Date(2026, 10, 4, 15, 30)

function renderTimeline() {
  render(<Timeline now={now} />)
  return screen.getByRole('img', { name: /Таймлайн/ })
}

describe('Timeline', () => {
  it('opens on a quarter with weeks under months and a today line', () => {
    const svg = renderTimeline()

    expect(within(svg).getByText('Ноябрь 2026')).toBeInTheDocument()
    expect(within(svg).getByText('2 ноя')).toBeInTheDocument()
    expect(within(svg).getByText('сегодня')).toBeInTheDocument()
  })

  it('switches to days for a week and to months for a year', async () => {
    const svg = renderTimeline()

    await userEvent.click(screen.getByRole('button', { name: 'Неделя' }))
    expect(within(svg).getByText('ср 4')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Год' }))
    expect(within(svg).getByText('ноя')).toBeInTheDocument()
    expect(within(svg).getByText('2026')).toBeInTheDocument()
  })

  it('zooms with buttons and returns to today', async () => {
    renderTimeline()
    const range = () => screen.getByRole('toolbar').querySelector('[aria-live]')!.textContent

    await userEvent.click(screen.getByRole('button', { name: 'Неделя' }))
    const week = range()
    await userEvent.click(screen.getByRole('button', { name: 'Отдалить' }))
    const wider = range()
    await userEvent.click(screen.getByRole('button', { name: 'Сегодня' }))

    expect(wider).not.toBe(week)
    expect(range()).toContain('ноя 2026')
  })
})
