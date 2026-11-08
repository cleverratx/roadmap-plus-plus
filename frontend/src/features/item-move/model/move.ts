import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { itemQueryKey, rememberItem } from '@/entities/item'
import { api, unwrap } from '@/shared/api'

export type MoveRequest = {
  itemId: string
  parentId: string | null
  position?: number
}

/**
 * Переносит узел или меняет его место среди соседей. `position` считается среди новых соседей без самого узла:
 * чтобы поднять третий узел на одно место, передают того же родителя и позицию 1.
 */
export function useMoveItem() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ itemId, parentId, position }: MoveRequest) =>
      unwrap(api.POST('/api/items/{itemId}/move', { params: { path: { itemId } }, body: { parentId, position: position ?? null } })),
    onSuccess: (details) => rememberItem(queryClient, details),
  })
}

export function useMoveTargets(itemId: string) {
  return useQuery({
    queryKey: [...itemQueryKey(itemId), 'move-targets'],
    queryFn: ({ signal }) => unwrap(api.GET('/api/items/{itemId}/move-targets', { params: { path: { itemId } }, signal })),
  })
}
