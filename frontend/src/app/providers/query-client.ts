import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { meQueryKey } from '@/entities/session'
import { HttpError } from '@/shared/api'

export function shouldRetry(failureCount: number, error: Error): boolean {
  const isClientError = error instanceof HttpError && error.status < 500
  return !isClientError && failureCount < 2
}

export function createQueryClient(): QueryClient {
  const client: QueryClient = new QueryClient({
    queryCache: new QueryCache({ onError: forgetSessionOnUnauthorized }),
    mutationCache: new MutationCache({ onError: forgetSessionOnUnauthorized }),
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: shouldRetry,
      },
    },
  })

  function forgetSessionOnUnauthorized(error: Error) {
    if (error instanceof HttpError && error.status === 401) {
      client.setQueryData(meQueryKey, null)
    }
  }

  return client
}
