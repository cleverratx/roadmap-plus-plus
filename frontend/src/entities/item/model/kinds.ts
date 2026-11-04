import type { ItemKind } from './item'

export const itemKindLabels: Record<ItemKind, string> = {
  macro: 'Макрособытие',
  event: 'Событие',
  group: 'Группа',
  task: 'Задача',
}

export const rootKinds: readonly ItemKind[] = ['macro', 'event']

const childKinds: Record<ItemKind, readonly ItemKind[]> = {
  macro: ['macro', 'event'],
  event: ['group', 'task'],
  group: ['group', 'task'],
  task: [],
}

export function childKindsOf(parent: ItemKind | null): readonly ItemKind[] {
  return parent ? childKinds[parent] : rootKinds
}
