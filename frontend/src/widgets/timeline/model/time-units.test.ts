import { scaleTime } from 'd3-scale'
import { describe, expect, it } from 'vitest'
import { axisTicks, chooseLevels, formatTick, pixelsPerDay } from './time-units'

function scaleOver(from: Date, to: Date, width = 1000) {
  return scaleTime().domain([from, to]).range([0, width])
}

describe('chooseLevels', () => {
  it('goes from days to years as the view widens', () => {
    expect(chooseLevels(1000 / 7)).toEqual({ minor: 'day', major: 'month' })
    expect(chooseLevels(1000 / 31)).toEqual({ minor: 'week', major: 'month' })
    expect(chooseLevels(1000 / 365)).toEqual({ minor: 'month', major: 'year' })
    expect(chooseLevels(1000 / (3 * 365))).toEqual({ minor: 'year', major: null })
  })
})

describe('axisTicks', () => {
  it('labels days and the months above them', () => {
    const scale = scaleOver(new Date(2026, 8, 28), new Date(2026, 9, 3))

    expect(axisTicks(scale, 'day', 'minor').map((t) => t.label)).toEqual(['пн 28', 'вт 29', 'ср 30', 'чт 1', 'пт 2'])
    expect(axisTicks(scale, 'month', 'major').map((t) => t.label)).toEqual(['Сентябрь 2026', 'Октябрь 2026'])
  })

  it('starts weeks on Monday', () => {
    const scale = scaleOver(new Date(2026, 10, 4), new Date(2026, 10, 20))

    expect(axisTicks(scale, 'week', 'minor').map((t) => t.label)).toEqual(['2 ноя', '9 ноя', '16 ноя'])
  })

  it('places ticks on the scale and gives each its width', () => {
    const scale = scaleOver(new Date(2026, 0, 1), new Date(2026, 0, 11))
    const [first, second] = axisTicks(scale, 'day', 'minor')

    expect(first.x).toBeCloseTo(0)
    expect(first.width).toBeCloseTo(100)
    expect(second.x).toBeCloseTo(100)
    expect(pixelsPerDay(scale)).toBeCloseTo(100)
  })
})

describe('formatTick', () => {
  it('uses short month names under years', () => {
    expect(formatTick(new Date(2026, 4, 1), 'month', 'minor')).toBe('май')
    expect(formatTick(new Date(2026, 0, 1), 'year', 'major')).toBe('2026')
  })
})
