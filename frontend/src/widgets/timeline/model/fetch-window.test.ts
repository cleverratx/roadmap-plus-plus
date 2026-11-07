import { describe, expect, it } from 'vitest'
import { fetchWindow, MAX_FETCH_DAYS } from './fetch-window'

describe('fetchWindow', () => {
  it('pads the visible range by half on each side and snaps to whole months', () => {
    expect(fetchWindow(new Date(2026, 10, 1), new Date(2026, 10, 31))).toEqual({ from: '2026-10-01', to: '2026-12-31' })
  })

  it('stays the same while the view moves inside the window', () => {
    const before = fetchWindow(new Date(2026, 10, 10), new Date(2026, 10, 17))
    const after = fetchWindow(new Date(2026, 10, 12), new Date(2026, 10, 19))

    expect(before).toEqual({ from: '2026-11-01', to: '2026-11-30' })
    expect(after).toEqual(before)
  })

  it('falls back to the visible range when the padded one is too wide for the API', () => {
    const range = fetchWindow(new Date(2025, 0, 1), new Date(2027, 11, 31))
    const days = (new Date(range.to).getTime() - new Date(range.from).getTime()) / 86_400_000

    expect(range).toEqual({ from: '2025-01-01', to: '2027-12-31' })
    expect(days).toBeLessThanOrEqual(MAX_FETCH_DAYS)
  })
})
