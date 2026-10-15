import { queryOptions, useQuery } from '@tanstack/react-query'
import { api, HttpError, unwrap, type components } from '@/shared/api'

export type Me = components['schemas']['MeResponse']

export const meQueryKey = ['me'] as const

export async function fetchMe(signal?: AbortSignal): Promise<Me | null> {
  try {
    return await unwrap(api.GET('/api/me', { signal }))
  } catch (error) {
    if (error instanceof HttpError && error.status === 401) {
      return null
    }

    throw error
  }
}

export const meQuery = queryOptions({
  queryKey: meQueryKey,
  queryFn: ({ signal }) => fetchMe(signal),
  staleTime: Infinity,
})

export function useMe() {
  return useQuery(meQuery)
}
