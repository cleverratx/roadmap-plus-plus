import type { QueryClient } from '@tanstack/react-query'
import { itemQueryKey, type ItemDetails, type Link } from '@/entities/item'

/**
 * Ссылки видны только в панели узла, поэтому после правки достаточно поправить его кэш на месте,
 * без нового запроса и без перерисовки таймлайна.
 */
export function updateLinks(queryClient: QueryClient, itemId: string, change: (links: Link[]) => Link[]) {
  queryClient.setQueryData<ItemDetails>(itemQueryKey(itemId), (details) => details && { ...details, links: change(details.links) })
}
