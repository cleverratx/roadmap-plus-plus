import { useQuery } from '@tanstack/react-query'
import { api, unwrap, type components } from '@/shared/api'

export type WorkspaceSummary = components['schemas']['WorkspaceSummary']

export type WorkspaceDetails = components['schemas']['WorkspaceDetails']

export type WorkspaceMember = components['schemas']['MemberResponse']

export const workspacesQueryKey = ['workspaces'] as const

export function workspaceQueryKey(workspaceId: string) {
  return ['workspaces', workspaceId] as const
}

export function useWorkspaces() {
  return useQuery({
    queryKey: workspacesQueryKey,
    queryFn: ({ signal }) => unwrap(api.GET('/api/workspaces', { signal })),
  })
}

export function useWorkspace(workspaceId: string) {
  return useQuery({
    queryKey: workspaceQueryKey(workspaceId),
    queryFn: ({ signal }) => unwrap(api.GET('/api/workspaces/{workspaceId}', { params: { path: { workspaceId } }, signal })),
  })
}
