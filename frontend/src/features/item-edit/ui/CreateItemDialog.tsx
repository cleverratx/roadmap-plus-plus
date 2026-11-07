import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { childKindsOf, itemKindLabels, newItemTitles, type Item, type ItemDetails, type ItemKind } from '@/entities/item'
import { WorkspaceDot, type WorkspaceSummary } from '@/entities/workspace'
import { api, toFormErrors, unwrap } from '@/shared/api'
import { Button, ChoiceGroup, Dialog } from '@/shared/ui'
import { checkItemFields, refreshItems, toItemBody, type ItemFieldsValue } from '../model/fields'
import { ItemFields } from './ItemFields'
import styles from './ItemForm.module.css'

/** С чего начинается новый узел: где он будет и что подставить в форму. */
export type ItemDraft = {
  parent: Item | null
  kind: ItemKind
  startDate: string | null
  endDate: string | null
  workspaceId?: string
}

type CreateItemDialogProps = {
  draft: ItemDraft
  workspaces: readonly WorkspaceSummary[]
  onClose: () => void
  onCreated: (details: ItemDetails) => void
}

export function CreateItemDialog({ draft, workspaces, onClose, onCreated }: CreateItemDialogProps) {
  const queryClient = useQueryClient()
  const { parent } = draft
  const kinds = childKindsOf(parent?.kind ?? null)
  const [kind, setKind] = useState(draft.kind)
  const [workspaceId, setWorkspaceId] = useState(draft.workspaceId ?? workspaces[0]?.id ?? '')
  const [value, setValue] = useState<ItemFieldsValue>({
    title: '',
    startDate: draft.startDate ?? '',
    endDate: draft.endDate ?? '',
    descriptionMd: '',
  })
  const [checks, setChecks] = useState<Record<string, string>>({})

  const create = useMutation({
    mutationFn: () =>
      unwrap(
        api.POST('/api/items', {
          body: { workspaceId: parent ? null : workspaceId, parentId: parent?.id ?? null, kind, ...toItemBody(value) },
        }),
      ),
    onSuccess: (details) => {
      refreshItems(queryClient, details)
      onCreated(details)
    },
  })
  const errors = toFormErrors(create.error)

  function submit(event: FormEvent) {
    event.preventDefault()
    const found = checkItemFields(value, parent === null)
    setChecks(found)
    if (Object.keys(found).length === 0) {
      create.mutate()
    }
  }

  return (
    <Dialog
      title={newItemTitles[kind]}
      description={parent ? `Внутри "${parent.title}"` : 'На верхнем уровне таймлайна'}
      open
      onOpenChange={(open) => !open && onClose()}
    >
      <form className={styles.form} onSubmit={submit} noValidate>
        {kinds.length > 1 && (
          <ChoiceGroup
            label="Что создаём"
            choices={kinds.map((option) => ({ value: option, label: itemKindLabels[option] }))}
            value={kind}
            onChange={setKind}
          />
        )}
        {!parent && workspaces.length > 1 && (
          <ChoiceGroup
            label="Команда"
            choices={workspaces.map((workspace) => ({
              value: workspace.id,
              label: (
                <>
                  <WorkspaceDot color={workspace.color} />
                  {workspace.name}
                </>
              ),
            }))}
            value={workspaceId}
            onChange={setWorkspaceId}
          />
        )}
        <ItemFields value={value} onChange={setValue} errors={{ ...errors.fields, ...checks }} />
        {errors.message && (
          <p className={styles.error} role="alert">
            {errors.message}
          </p>
        )}
        <Button type="submit" variant="primary" disabled={create.isPending}>
          Создать
        </Button>
      </form>
    </Dialog>
  )
}
