import type { ScaleTime } from 'd3-scale'
import { timeDay, timeMonday, timeMonth, timeYear, type CountableTimeInterval } from 'd3-time'

export type TimeUnit = 'day' | 'week' | 'month' | 'year'

export type AxisLevels = {
  minor: TimeUnit
  major: TimeUnit | null
}

export type Tick = {
  key: string
  date: Date
  x: number
  width: number
  label: string
}

const intervals: Record<TimeUnit, CountableTimeInterval> = {
  day: timeDay,
  week: timeMonday,
  month: timeMonth,
  year: timeYear,
}

const unitDays: Record<TimeUnit, number> = { day: 1, week: 7, month: 30.44, year: 365.25 }

const minLabelWidth: Record<TimeUnit, number> = { day: 40, week: 52, month: 44, year: 48 }

const finestFirst: readonly TimeUnit[] = ['day', 'week', 'month', 'year']

const monthNames = [
  'январь',
  'февраль',
  'март',
  'апрель',
  'май',
  'июнь',
  'июль',
  'август',
  'сентябрь',
  'октябрь',
  'ноябрь',
  'декабрь',
]

export const monthShort = ['янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек']

const weekdayShort = ['вс', 'пн', 'вт', 'ср', 'чт', 'пт', 'сб']

export function pixelsPerDay(scale: ScaleTime<number, number>): number {
  const [start, end] = scale.domain()
  const [left, right] = scale.range()
  return (right - left) / ((end.getTime() - start.getTime()) / 86_400_000)
}

/**
 * Самые мелкие подписи, которые помещаются по ширине, и строка покрупнее над ними:
 * дни или недели под месяцами, месяцы под годами, а на самом дальнем зуме одни годы.
 */
export function chooseLevels(dayWidth: number): AxisLevels {
  const minor = finestFirst.find((unit) => unitDays[unit] * dayWidth >= minLabelWidth[unit]) ?? 'year'
  const major = minor === 'year' ? null : minor === 'month' ? 'year' : 'month'
  return { minor, major }
}

export function axisTicks(scale: ScaleTime<number, number>, unit: TimeUnit, role: 'minor' | 'major'): Tick[] {
  const [start, end] = scale.domain()
  const interval = intervals[unit]

  return interval.range(interval.floor(start), end).map((date) => {
    const x = scale(date)
    return {
      key: `${unit}-${date.getTime()}`,
      date,
      x,
      width: scale(interval.offset(date, 1)) - x,
      label: formatTick(date, unit, role),
    }
  })
}

export function formatTick(date: Date, unit: TimeUnit, role: 'minor' | 'major'): string {
  const day = date.getDate()
  const month = date.getMonth()
  const year = date.getFullYear()

  switch (unit) {
    case 'year':
      return String(year)
    case 'month':
      return role === 'major' ? `${capitalize(monthNames[month])} ${year}` : monthShort[month]
    case 'week':
      return `${day} ${monthShort[month]}`
    case 'day':
      return `${weekdayShort[date.getDay()]} ${day}`
  }
}

export function formatDate(date: Date): string {
  return `${date.getDate()} ${monthShort[date.getMonth()]} ${date.getFullYear()}`
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1)
}
