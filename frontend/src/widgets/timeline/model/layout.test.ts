import { scaleTime } from 'd3-scale'
import { describe, expect, it } from 'vitest'
import type { Item, ItemKind } from '@/entities/item'
import { blockHeight, layoutItems } from './layout'

const team = 'team-1'
const other = 'team-2'

function item(id: string, kind: ItemKind, start: string | null, end: string | null, parentId: string | null = null, workspaceId = team): Item {
  return { id, workspaceId, parentId, kind, title: id, startDate: start, endDate: end, position: 0 }
}

function over(from: Date, to: Date) {
  return scaleTime().domain([from, to]).range([0, 1000])
}

const quarter = over(new Date(2026, 9, 1), new Date(2026, 11, 30))
const week = over(new Date(2026, 10, 2), new Date(2026, 10, 9))
const color = (id: string) => (id === team ? '#3b5bdb' : '#2b8a3e')

describe('layoutItems', () => {
  it('puts macro events above events and gives blocks their team color', () => {
    const { blocks } = layoutItems(
      [item('Хакатон', 'event', '2026-11-03', '2026-11-05'), item('Учёба', 'macro', '2026-09-01', '2026-12-31')],
      quarter,
      color,
    )

    const [macro, event] = blocks
    expect(macro.item.id).toBe('Учёба')
    expect(event.item.id).toBe('Хакатон')
    expect(event.y).toBeGreaterThan(macro.y)
    expect(event.color).toBe('#3b5bdb')
  })

  it('spreads overlapping events over rows and reuses free rows', () => {
    const { blocks } = layoutItems(
      [
        item('a', 'event', '2026-11-01', '2026-11-10'),
        item('b', 'event', '2026-11-05', '2026-11-06'),
        item('c', 'event', '2026-11-11', '2026-11-12'),
      ],
      quarter,
      color,
    )
    const y = Object.fromEntries(blocks.map((b) => [b.item.id, b.y]))

    expect(y.b).toBe(y.a + blockHeight.event + 4)
    expect(y.c).toBe(y.a)
  })

  it('opens children only when the parent is wide enough', () => {
    const tree = [
      item('Хакатон', 'event', '2026-11-03', '2026-11-05'),
      item('День 1', 'group', '2026-11-03', '2026-11-03', 'Хакатон'),
      item('Репозиторий', 'task', '2026-11-03', '2026-11-03', 'День 1'),
    ]

    const far = layoutItems(tree, quarter, color).blocks.map((b) => b.item.id)
    const near = layoutItems(tree, week, color).blocks.map((b) => b.item.id)

    expect(far).toEqual(['Хакатон'])
    expect(near).toEqual(['Хакатон', 'День 1', 'Репозиторий'])
  })

  it('shows tasks under an undated group only up close', () => {
    const tasks = [item('Без группы', 'task', '2026-11-04', '2026-11-04', 'группа-без-дат')]

    expect(layoutItems(tasks, quarter, color).blocks).toHaveLength(0)
    expect(layoutItems(tasks, week, color).blocks).toHaveLength(1)
  })

  it('leaves out undated items, hidden teams and items off screen', () => {
    const { blocks } = layoutItems(
      [
        item('Без дат', 'event', null, null),
        item('Чужое', 'event', '2026-11-03', '2026-11-05', null, other),
        item('Прошлое', 'event', '2025-01-01', '2025-01-02'),
        item('Видно', 'event', '2026-11-03', '2026-11-05'),
      ],
      quarter,
      color,
      new Set([other]),
    )

    expect(blocks.map((b) => b.item.id)).toEqual(['Видно'])
  })
})
