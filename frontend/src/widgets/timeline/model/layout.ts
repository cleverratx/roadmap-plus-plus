import type { ScaleTime } from 'd3-scale'
import type { Item, ItemKind } from '@/entities/item'
import { endOfInclusiveDay, parseDate } from './dates'
import { pixelsPerDay } from './time-units'

/** Узел раскрывается в своих детей, когда на экране он шире этого. */
export const EXPAND_WIDTH = 120

/**
 * С какого масштаба (пикселей на день) показывать группы и задачи, чей родитель не пришёл с сервера:
 * обычно это узлы под группой без дат, вроде "Организация и репозиторий". Примерно с месячного вида.
 */
export const DETAIL_DAY_WIDTH = 20

const bandOrder: readonly ItemKind[] = ['macro', 'event', 'group', 'task']

export const blockHeight: Record<ItemKind, number> = { macro: 22, event: 28, group: 22, task: 20 }

const ROW_GAP = 4

const BAND_GAP = 12

export type Block = {
  item: Item
  x: number
  width: number
  y: number
  height: number
  color: string
  expanded: boolean
}

export type TimelineLayout = {
  blocks: Block[]
  height: number
}

type Placed = {
  item: Item
  start: Date
  end: Date
  x: number
  width: number
}

/**
 * Раскладывает узлы по полосам: сверху макрособытия, под ними события, ниже группы и задачи. В полосе пересекающиеся
 * узлы разносятся по строкам; строки считаются по датам, а не по пикселям, поэтому при зуме блоки не прыгают между ними.
 * Дети узла видны, только пока сам узел достаточно широк: при приближении блоки "раскрываются" в то, из чего состоят.
 */
export function layoutItems(
  items: readonly Item[],
  scale: ScaleTime<number, number>,
  colorOf: (workspaceId: string) => string,
  hiddenWorkspaceIds: ReadonlySet<string> = new Set(),
): TimelineLayout {
  const [left, right] = scale.range()
  const dayWidth = pixelsPerDay(scale)
  const placed = new Map<string, Placed>()

  for (const item of items) {
    if (!item.startDate || !item.endDate || hiddenWorkspaceIds.has(item.workspaceId)) {
      continue
    }

    const start = parseDate(item.startDate)
    const end = endOfInclusiveDay(item.endDate)
    const x = scale(start)
    placed.set(item.id, { item, start, end, x, width: scale(end) - x })
  }

  const shown = new Map<string, boolean>()
  const isShown = (node: Placed): boolean => {
    const cached = shown.get(node.item.id)
    if (cached !== undefined) {
      return cached
    }

    const parent = node.item.parentId ? placed.get(node.item.parentId) : undefined
    const result = !node.item.parentId
      ? true
      : parent
        ? isShown(parent) && parent.width >= EXPAND_WIDTH
        : node.item.kind === 'macro' || node.item.kind === 'event' || dayWidth >= DETAIL_DAY_WIDTH

    shown.set(node.item.id, result)
    return result
  }

  const blocks: Block[] = []
  let y = 0

  for (const kind of bandOrder) {
    const band = [...placed.values()]
      .filter((node) => node.item.kind === kind && isShown(node))
      .sort((a, b) => a.start.getTime() - b.start.getTime() || b.end.getTime() - a.end.getTime())

    if (band.length === 0) {
      continue
    }

    const rowEnds: number[] = []
    for (const node of band) {
      let row = rowEnds.findIndex((end) => end <= node.start.getTime())
      if (row === -1) {
        row = rowEnds.length
        rowEnds.push(0)
      }
      rowEnds[row] = node.end.getTime()

      if (node.x + node.width >= left && node.x <= right) {
        blocks.push({
          item: node.item,
          x: node.x,
          width: node.width,
          y: y + row * (blockHeight[kind] + ROW_GAP),
          height: blockHeight[kind],
          color: colorOf(node.item.workspaceId),
          expanded: node.width >= EXPAND_WIDTH,
        })
      }
    }

    y += rowEnds.length * (blockHeight[kind] + ROW_GAP) + BAND_GAP
  }

  return { blocks, height: y }
}
