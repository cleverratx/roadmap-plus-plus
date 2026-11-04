import { describe, expect, it } from 'vitest'
import { baseScale, MAX_SPAN_DAYS, MIN_SPAN_DAYS, transformFor, visibleSpanDays } from './zoom'

const now = new Date(2026, 10, 4, 15, 30)
const width = 1000

describe('zoom', () => {
  it('starts with a quarter around today', () => {
    const base = baseScale(now, width)
    const [start, end] = base.domain()

    expect(visibleSpanDays(base)).toBe(90)
    expect(start < now && now < end).toBe(true)
  })

  it('shows the requested span around the requested day', () => {
    const base = baseScale(now, width)
    const center = new Date(2026, 11, 1)

    const view = transformFor(base, center, 7, width).rescaleX(base)

    expect(visibleSpanDays(view)).toBeCloseTo(7)
    expect(view(center)).toBeCloseTo(width / 2)
  })

  it('keeps the span between three days and three years', () => {
    const base = baseScale(now, width)

    expect(visibleSpanDays(transformFor(base, now, 1, width).rescaleX(base))).toBeCloseTo(MIN_SPAN_DAYS)
    expect(visibleSpanDays(transformFor(base, now, 10 * 365, width).rescaleX(base))).toBeCloseTo(MAX_SPAN_DAYS)
  })
})
