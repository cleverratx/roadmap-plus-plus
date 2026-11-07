import type { QueryClient } from '@tanstack/react-query'
import { allItemsQueryKey, itemQueryKey, type ItemDetails } from '@/entities/item'

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

/** Кладёт свежий узел в кэш и просит перечитать остальные запросы узлов: таймлайн, панели родителя и детей. */
export function refreshItems(queryClient: QueryClient, details: ItemDetails) {
  queryClient.setQueryData(itemQueryKey(details.item.id), details)
  void queryClient.invalidateQueries({ queryKey: allItemsQueryKey, predicate: (query) => query.queryKey[1] !== details.item.id })
}
