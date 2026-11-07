import { timeDay } from 'd3-time'

/** Дата узла из API (`2026-09-28`) как полночь по местному времени: даты узлов календарные, без часового пояса. */
export function parseDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}

export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** Конец узла на шкале: даты включительные, поэтому блок тянется до начала следующего дня. */
export function endOfInclusiveDay(value: string): Date {
  return timeDay.offset(parseDate(value), 1)
}
