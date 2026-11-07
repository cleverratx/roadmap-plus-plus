import { useState } from 'react'
import { useSearchParams } from 'react-router'
import type { Item, ItemKind } from '@/entities/item'
import { useWorkspaces, WorkspaceDot } from '@/entities/workspace'
import { JoinByLinkDialog } from '@/features/invitation-accept'
import { CreateItemDialog, DeleteItemDialog, type ItemDraft } from '@/features/item-edit'
import { CreateWorkspaceDialog } from '@/features/workspace-create'
import { toFormErrors } from '@/shared/api'
import { cx } from '@/shared/lib'
import { Page } from '@/shared/ui'
import { EventPanel } from '@/widgets/event-panel'
import { Timeline } from '@/widgets/timeline'
import styles from './RoadmapPage.module.css'

/** Открытый узел живёт в адресе, чтобы ссылкой на него можно было поделиться, а "назад" в браузере работал. */
const ITEM_PARAM = 'item'

export function RoadmapPage() {
  const workspaces = useWorkspaces()
  const [hidden, setHidden] = useState<ReadonlySet<string>>(new Set())
  const [params, setParams] = useSearchParams()
  const [draft, setDraft] = useState<ItemDraft | null>(null)
  const [deleting, setDeleting] = useState<Item | null>(null)
  const selectedId = params.get(ITEM_PARAM)

  function toggle(workspaceId: string) {
    setHidden((current) => {
      const next = new Set(current)
      if (!next.delete(workspaceId)) {
        next.add(workspaceId)
      }
      return next
    })
  }

  function open(itemId: string | null) {
    setParams((current) => {
      const next = new URLSearchParams(current)
      if (itemId) {
        next.set(ITEM_PARAM, itemId)
      } else {
        next.delete(ITEM_PARAM)
      }
      return next
    })
  }

  function addChild(parent: Item, kind: ItemKind) {
    setDraft({ parent, kind, startDate: parent.startDate, endDate: parent.endDate })
  }

  function createAt(kind: ItemKind, date: string) {
    const visible = workspaces.data?.find((workspace) => !hidden.has(workspace.id))
    setDraft({ parent: null, kind, startDate: date, endDate: date, workspaceId: visible?.id })
  }

  function forget(item: Item) {
    if (item.id === selectedId) {
      open(item.parentId)
    }
  }

  if (workspaces.isPending) {
    return <Page title="Роадмап" />
  }

  if (workspaces.isError) {
    return (
      <Page title="Роадмап">
        <p role="alert">{toFormErrors(workspaces.error).message}</p>
      </Page>
    )
  }

  if (workspaces.data.length === 0) {
    return (
      <Page title="Роадмап">
        <div className={styles.empty}>
          <h2 className={styles.emptyTitle}>У вас пока нет команды</h2>
          <p className={styles.muted}>
            Роадмапы в Roadmap++ всегда принадлежат команде. Создайте свою, даже если вы в ней один, или вступите в
            существующую по ссылке от главы.
          </p>
          <div className={styles.actions}>
            <CreateWorkspaceDialog />
            <JoinByLinkDialog />
          </div>
        </div>
      </Page>
    )
  }

  return (
    <div className={styles.layout}>
      <div className={styles.content}>
        <Page title="Роадмап">
          <ul className={styles.teams} aria-label="Показывать команды">
            {workspaces.data.map((workspace) => (
              <li key={workspace.id}>
                <button
                  type="button"
                  className={cx(styles.team, hidden.has(workspace.id) && styles.off)}
                  aria-pressed={!hidden.has(workspace.id)}
                  onClick={() => toggle(workspace.id)}
                >
                  <WorkspaceDot color={workspace.color} />
                  {workspace.name}
                </button>
              </li>
            ))}
          </ul>
          <Timeline
            workspaces={workspaces.data}
            hiddenWorkspaceIds={hidden}
            selectedItemId={selectedId}
            onOpenItem={(item) => open(item.id)}
            onAddChild={addChild}
            onDeleteItem={setDeleting}
            onCreateAt={createAt}
          />
        </Page>
      </div>

      {selectedId && (
        <EventPanel
          key={selectedId}
          itemId={selectedId}
          workspaces={workspaces.data}
          onOpen={open}
          onClose={() => open(null)}
          onAddChild={addChild}
          onDelete={setDeleting}
        />
      )}

      {draft && (
        <CreateItemDialog
          draft={draft}
          workspaces={workspaces.data}
          onClose={() => setDraft(null)}
          onCreated={(details) => {
            setDraft(null)
            open(details.item.id)
          }}
        />
      )}

      {deleting && (
        <DeleteItemDialog
          item={deleting}
          onClose={() => setDeleting(null)}
          onDeleted={(item) => {
            setDeleting(null)
            forget(item)
          }}
        />
      )}
    </div>
  )
}
