import { useMutation, useQueryClient } from '@tanstack/react-query'
import { meQueryKey } from '@/entities/session'
import { api, unwrap, type components } from '@/shared/api'

export type LoginRequest = components['schemas']['LoginRequest']

export type RegisterRequest = components['schemas']['RegisterRequest']

export function useLogin() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: LoginRequest) => unwrap(api.POST('/api/auth/login', { body })),
    onSuccess: (me) => queryClient.setQueryData(meQueryKey, me),
  })
}

export function useRegister() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: RegisterRequest) => unwrap(api.POST('/api/auth/register', { body })),
    onSuccess: (me) => queryClient.setQueryData(meQueryKey, me),
  })
}

export function useLogout() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/auth/logout')),
    onSuccess: () => {
      queryClient.setQueryData(meQueryKey, null)
      queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== meQueryKey[0] })
    },
  })
}
