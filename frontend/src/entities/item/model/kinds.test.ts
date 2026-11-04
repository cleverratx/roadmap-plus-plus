import { describe, expect, it } from 'vitest'
import type { ItemKind } from './item'
import { childKindsOf, itemKindLabels } from './kinds'

describe('item kinds', () => {
  it('mirror the nesting table of the backend', () => {
    const table: Array<[ItemKind | null, ItemKind[]]> = [
      [null, ['macro', 'event']],
      ['macro', ['macro', 'event']],
      ['event', ['group', 'task']],
      ['group', ['group', 'task']],
      ['task', []],
    ]

    for (const [parent, children] of table) {
      expect(childKindsOf(parent)).toEqual(children)
    }
  })

  it('have a Russian label for every kind', () => {
    expect(Object.values(itemKindLabels)).toEqual(['Макрособытие', 'Событие', 'Группа', 'Задача'])
  })
})
