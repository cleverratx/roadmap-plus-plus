import { scaleTime, type ScaleTime } from 'd3-scale'
import { timeDay } from 'd3-time'
import { zoomIdentity, type ZoomTransform } from 'd3-zoom'

/** Сколько дней видно без зума: квартал вокруг сегодняшнего дня. */
export const BASE_SPAN_DAYS = 90

/** Ближе всего можно подойти к трём дням: дальше, до часов и минут, зум откроет этап 3. */
export const MIN_SPAN_DAYS = 3

export const MAX_SPAN_DAYS = 3 * 365

/** Пределы зума для d3-zoom: во сколько раз можно отдалить и приблизить базовый квартал. */
export const scaleExtent: [number, number] = [BASE_SPAN_DAYS / MAX_SPAN_DAYS, BASE_SPAN_DAYS / MIN_SPAN_DAYS]

export const presets = [
  { label: 'Год', days: 365 },
  { label: 'Квартал', days: 92 },
  { label: 'Месяц', days: 31 },
  { label: 'Неделя', days: 7 },
] as const

/**
 * Базовая шкала без зума. d3-zoom хранит только сдвиг и множитель, а видимая шкала получается из базовой через
 * `transform.rescaleX(base)`, поэтому базовую строим один раз на ширину и сегодняшний день.
 */
export function baseScale(today: Date, width: number): ScaleTime<number, number> {
  const center = timeDay.floor(today)
  return scaleTime()
    .domain([timeDay.offset(center, -BASE_SPAN_DAYS / 2), timeDay.offset(center, BASE_SPAN_DAYS / 2)])
    .range([0, width])
}

/** Преобразование, при котором `center` стоит посередине, а по ширине помещается `spanDays` дней. */
export function transformFor(base: ScaleTime<number, number>, center: Date, spanDays: number, width: number): ZoomTransform {
  const k = Math.min(Math.max(BASE_SPAN_DAYS / spanDays, scaleExtent[0]), scaleExtent[1])
  return zoomIdentity.translate(width / 2 - k * base(center), 0).scale(k)
}

export function visibleSpanDays(scale: ScaleTime<number, number>): number {
  const [start, end] = scale.domain()
  return (end.getTime() - start.getTime()) / 86_400_000
}
