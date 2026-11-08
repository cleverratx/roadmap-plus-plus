export type ItemFieldsValue = {
  title: string
  startDate: string
  endDate: string
  descriptionMd: string
}

export function toItemBody(value: ItemFieldsValue) {
  return {
    title: value.title,
    startDate: value.startDate || null,
    endDate: value.endDate || null,
    descriptionMd: value.descriptionMd,
  }
}

/**
 * Проверки, которых нет на сервере, потому что они про интерфейс, а не про данные. Узел верхнего уровня без дат
 * сервер примет, но показать его будет негде: на таймлайн он не попадёт, а родителя с панелью у него нет.
 */
export function checkItemFields(value: ItemFieldsValue, isRoot: boolean): Record<string, string> {
  if (isRoot && !value.startDate && !value.endDate) {
    return { startDate: 'Без дат узел верхнего уровня не будет видно на таймлайне, укажите их.' }
  }

  return {}
}

