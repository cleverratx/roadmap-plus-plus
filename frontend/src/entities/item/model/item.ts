import { keepPreviousData, useQuery, type QueryClient } from '@tanstack/react-query'
import { api, unwrap, type components } from '@/shared/api'

export type Item = components['schemas']['ItemResponse']

export type ItemDetails = components['schemas']['ItemDetails']

export type ItemKind = components['schemas']['ItemKind']

export type DateRange = {
  from: string
  to: string
}

/** Общий префикс всех запросов узлов: после правки сбрасываем его целиком, чтобы обновились и таймлайн, и панели. */
export const allItemsQueryKey = ['items'] as const

export function itemsQueryKey(range: DateRange) {
  return ['items', 'range', range.from, range.to] as const
}

export function itemQueryKey(itemId: string) {
  return ['items', itemId] as const
}

export function useItems(range: DateRange) {
  return useQuery({
    queryKey: itemsQueryKey(range),
    queryFn: ({ signal }) => unwrap(api.GET('/api/items', { params: { query: range }, signal })),
    placeholderData: keepPreviousData,
  })
}

export function useItem(itemId: string) {
  return useQuery({
    queryKey: itemQueryKey(itemId),
    queryFn: ({ signal }) => unwrap(api.GET('/api/items/{itemId}', { params: { path: { itemId } }, signal })),
  })
}

/**
 * Кладёт свежий узел, который вернул сервер после правки, в кэш и просит перечитать остальные запросы узлов:
 * таймлайн, панели родителя и детей. Сам узел повторно не запрашивается, он уже свежий.
 */
export function rememberItem(queryClient: QueryClient, details: ItemDetails) {
  queryClient.setQueryData(itemQueryKey(details.item.id), details)
  void queryClient.invalidateQueries({ queryKey: allItemsQueryKey, predicate: (query) => query.queryKey[1] !== details.item.id })
}
