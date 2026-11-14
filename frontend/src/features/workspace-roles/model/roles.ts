import { useMutation, useQueryClient } from '@tanstack/react-query'
import { workspaceQueryKey, type WorkspaceDetails } from '@/entities/workspace'

/**
 * Каждая правка ролей отвечает свежей командой целиком, поэтому её достаточно положить в кэш страницы команды:
 * второй запрос за ней не нужен.
 */
export function useWorkspaceChange<TVariables = void>(
  workspaceId: string,
  change: (variables: TVariables) => Promise<WorkspaceDetails>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: change,
    onSuccess: (details) => queryClient.setQueryData(workspaceQueryKey(workspaceId), details),
  })
}
