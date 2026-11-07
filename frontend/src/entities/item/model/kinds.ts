import type { ItemKind } from './item'

export const itemKindLabels: Record<ItemKind, string> = {
  macro: 'Макрособытие',
  event: 'Событие',
  group: 'Группа',
  task: 'Задача',
}

/** Вид узла в винительном падеже, для пунктов вроде "Добавить группу". */
export const itemKindAccusative: Record<ItemKind, string> = {
  macro: 'макрособытие',
  event: 'событие',
  group: 'группу',
  task: 'задачу',
}

export const newItemTitles: Record<ItemKind, string> = {
  macro: 'Новое макрособытие',
  event: 'Новое событие',
  group: 'Новая группа',
  task: 'Новая задача',
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
