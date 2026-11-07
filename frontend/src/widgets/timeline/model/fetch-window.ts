import { timeDay, timeMonth } from 'd3-time'
import type { DateRange } from '@/entities/item'
import { toIsoDate } from './dates'

/** Больше этого API за раз не отдаёт (`ItemService.MaxRangeDays` на бэкенде). */
export const MAX_FETCH_DAYS = 3 * 366

/**
 * Какой диапазон запрашивать для видимого куска таймлайна. Берём с запасом в половину видимого с каждой стороны
 * и округляем до целых месяцев: пока человек двигает таймлайн внутри этого окна, запрос не меняется,
 * а сдвиг к соседнему месяцу уже лежит в кэше. Если окно с запасом не влезает в лимит API, просим ровно видимое.
 */
export function fetchWindow(visibleStart: Date, visibleEnd: Date): DateRange {
  const spanDays = Math.ceil((visibleEnd.getTime() - visibleStart.getTime()) / 86_400_000)
  const padding = Math.ceil(spanDays / 2)
  const from = timeMonth.floor(timeDay.offset(visibleStart, -padding))
  const to = timeDay.offset(timeMonth.ceil(timeDay.offset(visibleEnd, padding)), -1)

  if (timeDay.count(from, to) <= MAX_FETCH_DAYS) {
    return { from: toIsoDate(from), to: toIsoDate(to) }
  }

  return { from: toIsoDate(timeDay.floor(visibleStart)), to: toIsoDate(timeDay.floor(visibleEnd)) }
}
