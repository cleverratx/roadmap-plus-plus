import { useQuery } from '@tanstack/react-query'
import { api, unwrap, type components } from '@/shared/api'

export type WorkspaceSummary = components['schemas']['WorkspaceSummary']

export type WorkspaceDetails = components['schemas']['WorkspaceDetails']

export type WorkspaceMember = components['schemas']['MemberResponse']

export type WorkspaceRole = components['schemas']['RoleResponse']

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

export function useWorkspace(workspaceId: string, enabled = true) {
  return useQuery({
    queryKey: workspaceQueryKey(workspaceId),
    queryFn: ({ signal }) => unwrap(api.GET('/api/workspaces/{workspaceId}', { params: { path: { workspaceId } }, signal })),
    enabled,
  })
}

/** Можно ли создавать узлы верхнего уровня: при включённых ролях это делают только главы (ADR-0003). */
export function canCreateRoot(workspace: Pick<WorkspaceSummary, 'isHead' | 'rolesEnabled'>): boolean {
  return workspace.isHead || !workspace.rolesEnabled
}
