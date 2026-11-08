import { describe, expect, it } from 'vitest'
import { linkTitle } from './links'

describe('linkTitle', () => {
  it('prefers the title people gave the link', () => {
    expect(linkTitle({ url: 'https://docs.google.com/spreadsheets/d/1AbC', title: 'Задачи' })).toBe('Задачи')
  })

  it('falls back to a short address', () => {
    expect(linkTitle({ url: 'https://www.github.com/skb/hackathon/', title: '' })).toBe('github.com/skb/hackathon')
    expect(linkTitle({ url: 'https://t.me/skb_hack/42?single', title: '' })).toBe('t.me/skb_hack/42')
    expect(linkTitle({ url: 'https://example.com/', title: '' })).toBe('example.com')
  })
})
