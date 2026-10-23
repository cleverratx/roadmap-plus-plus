export function parseInvitationToken(input: string): string | null {
  const value = input.trim()
  if (!value) {
    return null
  }

  const fromLink = value.match(/\/invite\/([^/?#\s]+)/)
  const token = fromLink ? decodeURIComponent(fromLink[1]) : value

  return /^[A-Za-z0-9_-]+$/.test(token) ? token : null
}
