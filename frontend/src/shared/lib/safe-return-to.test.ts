import { describe, expect, it } from 'vitest'
import { safeReturnTo } from './safe-return-to'

describe('safeReturnTo', () => {
  it('keeps a path inside the app', () => {
    expect(safeReturnTo('/today?x=1')).toBe('/today?x=1')
  })

  it('falls back to the root for anything that could leave the app', () => {
    expect(safeReturnTo(null)).toBe('/')
    expect(safeReturnTo('https://evil.example')).toBe('/')
    expect(safeReturnTo('//evil.example')).toBe('/')
    expect(safeReturnTo('/\\evil.example')).toBe('/')
  })
})
