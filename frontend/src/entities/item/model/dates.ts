type Dated = {
  startDate: string | null
  endDate: string | null
}

const months = [
  'января',
  'февраля',
  'марта',
  'апреля',
  'мая',
  'июня',
  'июля',
  'августа',
  'сентября',
  'октября',
  'ноября',
  'декабря',
]

/** Даты узла по-человечески: "3 - 5 ноября 2026", "28 сентября - 2 октября 2026" или "без дат". */
export function formatItemDates({ startDate, endDate }: Dated): string {
  if (!startDate || !endDate) {
    return 'без дат'
  }

  const [startYear, startMonth, startDay] = parts(startDate)
  const [endYear, endMonth, endDay] = parts(endDate)
  const end = `${endDay} ${months[endMonth - 1]} ${endYear}`

  if (startDate === endDate) {
    return end
  }

  if (startYear !== endYear) {
    return `${startDay} ${months[startMonth - 1]} ${startYear} - ${end}`
  }

  return startMonth === endMonth ? `${startDay} - ${end}` : `${startDay} ${months[startMonth - 1]} - ${end}`
}

/** Выходят ли даты узла за даты родителя. Это не ошибка, а повод предупредить в панели. */
export function isOutside(child: Dated, parent: Dated): boolean {
  if (!child.startDate || !child.endDate || !parent.startDate || !parent.endDate) {
    return false
  }

  return child.startDate < parent.startDate || child.endDate > parent.endDate
}

function parts(isoDate: string): [number, number, number] {
  const [year, month, day] = isoDate.split('-').map(Number)
  return [year, month, day]
}
