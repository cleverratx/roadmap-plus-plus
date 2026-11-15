import { useMutation, useQueryClient } from '@tanstack/react-query'
import { accessSummaryQueryKey } from '@/entities/item'
import { workspaceQueryKey, workspacesQueryKey, type WorkspaceDetails } from '@/entities/workspace'

/**
 * Каждая правка ролей отвечает свежей командой целиком, поэтому её достаточно положить в кэш страницы команды.
 * Перечитать нужно только то, что от ролей зависит: список команд (там признак включённых ролей) и сводку
 * по событиям, которые не открыты ни одной роли.
 */
export function useWorkspaceChange<TVariables = void>(
  workspaceId: string,
  change: (variables: TVariables) => Promise<WorkspaceDetails>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: change,
    onSuccess: (details) => {
      queryClient.setQueryData(workspaceQueryKey(workspaceId), details)
      void queryClient.invalidateQueries({ queryKey: workspacesQueryKey, exact: true })
      void queryClient.invalidateQueries({ queryKey: accessSummaryQueryKey(workspaceId) })
    },
  })
}
