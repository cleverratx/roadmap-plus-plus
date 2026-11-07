import { describe, expect, it } from 'vitest'
import { formatItemDates, isOutside } from './dates'

function dated(startDate: string | null, endDate: string | null) {
  return { startDate, endDate }
}

describe('formatItemDates', () => {
  it('writes dates the way people say them', () => {
    expect(formatItemDates(dated('2026-11-03', '2026-11-03'))).toBe('3 ноября 2026')
    expect(formatItemDates(dated('2026-11-03', '2026-11-05'))).toBe('3 - 5 ноября 2026')
    expect(formatItemDates(dated('2026-09-28', '2026-10-02'))).toBe('28 сентября - 2 октября 2026')
    expect(formatItemDates(dated('2026-12-28', '2027-01-03'))).toBe('28 декабря 2026 - 3 января 2027')
    expect(formatItemDates(dated(null, null))).toBe('без дат')
  })
})

describe('isOutside', () => {
  const hackathon = dated('2026-11-03', '2026-11-05')

  it('spots a child that starts earlier or ends later than its parent', () => {
    expect(isOutside(dated('2026-11-02', '2026-11-03'), hackathon)).toBe(true)
    expect(isOutside(dated('2026-11-05', '2026-11-06'), hackathon)).toBe(true)
    expect(isOutside(dated('2026-11-03', '2026-11-05'), hackathon)).toBe(false)
  })

  it('has nothing to compare when either side has no dates', () => {
    expect(isOutside(dated(null, null), hackathon)).toBe(false)
    expect(isOutside(dated('2026-12-01', '2026-12-02'), dated(null, null))).toBe(false)
  })
})
