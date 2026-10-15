import { vi } from 'vitest'

type Handler = (request: Request) => Response | Promise<Response>

export function mockApi(handlers: Record<string, Handler>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const request = input instanceof Request ? input : new Request(new URL(String(input), globalThis.location.origin), init)
    const handler = handlers[`${request.method} ${new URL(request.url).pathname}`]
    return handler ? handler(request) : new Response('Not Found', { status: 404 })
  })

  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export function json(body: unknown, status = 200): Response {
  return Response.json(body, { status })
}

export function problem(status: number, body: Record<string, unknown> = {}): Response {
  return new Response(JSON.stringify({ status, ...body }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}
