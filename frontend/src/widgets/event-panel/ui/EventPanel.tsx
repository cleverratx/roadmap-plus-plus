import { useId, useState } from 'react'
import {
  childKindsOf,
  formatItemDates,
  isOutside,
  itemKindLabels,
  useItem,
  type Item,
  type ItemDetails,
  type ItemKind,
} from '@/entities/item'
import { WorkspaceDot, type WorkspaceSummary } from '@/entities/workspace'
import { EditItemForm } from '@/features/item-edit'
import { ItemLinks } from '@/features/item-links'
import { useMoveItem } from '@/features/item-move'
import { toFormErrors } from '@/shared/api'
import { Button, SidePanel } from '@/shared/ui'
import styles from './EventPanel.module.css'

type EventPanelProps = {
  itemId: string
  workspaces: readonly WorkspaceSummary[]
  onOpen: (itemId: string) => void
  onClose: () => void
  onAddChild: (parent: Item, kind: ItemKind) => void
  onDelete: (item: Item) => void
  onMove: (item: Item) => void
}

export function EventPanel({ itemId, workspaces, onOpen, onClose, onAddChild, onDelete, onMove }: EventPanelProps) {
  const details = useItem(itemId)

  if (details.isPending) {
    return (
      <SidePanel title="Загрузка..." onClose={onClose}>
        <p className={styles.muted}>Загружаем узел...</p>
      </SidePanel>
    )
  }

  if (details.isError) {
    return (
      <SidePanel title="Узел недоступен" onClose={onClose}>
        <p role="alert">{toFormErrors(details.error).message}</p>
      </SidePanel>
    )
  }

  return (
    <ItemView
      details={details.data}
      workspace={workspaces.find((w) => w.id === details.data.item.workspaceId)}
      onOpen={onOpen}
      onClose={onClose}
      onAddChild={onAddChild}
      onDelete={onDelete}
      onMove={onMove}
    />
  )
}

type ItemViewProps = Omit<EventPanelProps, 'itemId' | 'workspaces'> & {
  details: ItemDetails
  workspace?: WorkspaceSummary
}

function ItemView({ details, workspace, onOpen, onClose, onAddChild, onDelete, onMove }: ItemViewProps) {
  const [editing, setEditing] = useState(false)
  const reorder = useMoveItem()
  const childrenId = useId()
  const { item, path, children, descriptionMd, links } = details
  const parent = path.at(-1)
  const childKinds = childKindsOf(item.kind)

  return (
    <SidePanel title={item.title} onClose={onClose}>
      <div className={styles.content}>
        {path.length > 0 && (
          <nav aria-label="Где лежит узел">
            <ol className={styles.path}>
              {path.map((ancestor) => (
                <li key={ancestor.id}>
                  <button type="button" className={styles.link} onClick={() => onOpen(ancestor.id)}>
                    {ancestor.title}
                  </button>
                </li>
              ))}
            </ol>
          </nav>
        )}

        <dl className={styles.facts}>
          <dt>Вид</dt>
          <dd>{itemKindLabels[item.kind]}</dd>
          {workspace && (
            <>
              <dt>Команда</dt>
              <dd className={styles.team}>
                <WorkspaceDot color={workspace.color} />
                {workspace.name}
              </dd>
            </>
          )}
          <dt>Даты</dt>
          <dd>{formatItemDates(item)}</dd>
        </dl>

        {parent && isOutside(item, parent) && (
          <p className={styles.warning}>
            Даты выходят за рамки "{parent.title}" ({formatItemDates(parent)}). Так можно, но проверьте, что это не опечатка.
          </p>
        )}

        {editing ? (
          <EditItemForm details={details} onDone={() => setEditing(false)} />
        ) : (
          <>
            {descriptionMd ? (
              <div className={styles.description}>{descriptionMd}</div>
            ) : (
              <p className={styles.muted}>Описания пока нет.</p>
            )}
            <div className={styles.actions}>
              <Button onClick={() => setEditing(true)}>Изменить</Button>
              <Button onClick={() => onMove(item)}>Переместить</Button>
              <Button variant="ghost" className={styles.danger} onClick={() => onDelete(item)}>
                Удалить
              </Button>
            </div>
          </>
        )}

        <ItemLinks itemId={item.id} links={links} />

        {childKinds.length > 0 && (
          <section className={styles.children} aria-labelledby={childrenId}>
            <div className={styles.childrenHeader}>
              <h3 id={childrenId} className={styles.heading}>
                Внутри
              </h3>
              {childKinds.map((kind) => (
                <Button key={kind} variant="ghost" onClick={() => onAddChild(item, kind)}>
                  + {itemKindLabels[kind]}
                </Button>
              ))}
            </div>
            {children.length === 0 ? (
              <p className={styles.muted}>Пока ничего.</p>
            ) : (
              <ul className={styles.list}>
                {children.map((child, index) => (
                  <li key={child.id} className={styles.childRow}>
                    <button type="button" className={styles.child} onClick={() => onOpen(child.id)}>
                      <span className={styles.childTitle}>{child.title}</span>
                      <span className={styles.muted}>
                        {itemKindLabels[child.kind]}, {formatItemDates(child)}
                        {isOutside(child, item) && <span className={styles.outside}>, вне дат</span>}
                      </span>
                    </button>
                    <div className={styles.order}>
                      <Button
                        variant="ghost"
                        aria-label={`Поднять "${child.title}"`}
                        disabled={index === 0 || reorder.isPending}
                        onClick={() => reorder.mutate({ itemId: child.id, parentId: item.id, position: index - 1 })}
                      >
                        Выше
                      </Button>
                      <Button
                        variant="ghost"
                        aria-label={`Опустить "${child.title}"`}
                        disabled={index === children.length - 1 || reorder.isPending}
                        onClick={() => reorder.mutate({ itemId: child.id, parentId: item.id, position: index + 1 })}
                      >
                        Ниже
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
            {reorder.isError && (
              <p className={styles.error} role="alert">
                {toFormErrors(reorder.error).message}
              </p>
            )}
          </section>
        )}
      </div>
    </SidePanel>
  )
}
