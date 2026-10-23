import { describe, expect, it } from 'vitest'
import { parseInvitationToken } from './parse-invitation'

describe('parseInvitationToken', () => {
  it('takes the token from a full link or accepts the bare token', () => {
    expect(parseInvitationToken('https://roadmap.example/invite/abc_DEF-123')).toBe('abc_DEF-123')
    expect(parseInvitationToken('  /invite/abc?utm=tg ')).toBe('abc')
    expect(parseInvitationToken('abc_DEF-123')).toBe('abc_DEF-123')
  })

  it('rejects anything that is not a token', () => {
    expect(parseInvitationToken('')).toBeNull()
    expect(parseInvitationToken('привет')).toBeNull()
    expect(parseInvitationToken('https://evil.example/')).toBeNull()
  })
})
