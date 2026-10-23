import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { invitePath } from '@/shared/config'
import { Button, Dialog, TextField, type ButtonVariant } from '@/shared/ui'
import { parseInvitationToken } from '../model/parse-invitation'
import styles from './Invitation.module.css'

type JoinByLinkDialogProps = {
  triggerVariant?: ButtonVariant
}

export function JoinByLinkDialog({ triggerVariant = 'secondary' }: JoinByLinkDialogProps) {
  const navigate = useNavigate()
  const [link, setLink] = useState('')
  const [error, setError] = useState<string>()

  function submit(event: FormEvent) {
    event.preventDefault()
    const token = parseInvitationToken(link)
    if (!token) {
      setError('Это не похоже на ссылку-приглашение. Скопируйте её целиком из сообщения.')
      return
    }

    navigate(invitePath(token))
  }

  return (
    <Dialog
      title="Вступить по приглашению"
      description="Вставьте ссылку, которую прислал глава команды."
      trigger={<Button variant={triggerVariant}>У меня есть приглашение</Button>}
    >
      <form className={styles.form} onSubmit={submit} noValidate>
        <TextField
          label="Ссылка-приглашение"
          value={link}
          onChange={(event) => {
            setLink(event.target.value)
            setError(undefined)
          }}
          error={error}
        />
        <Button type="submit" variant="primary">
          Продолжить
        </Button>
      </form>
    </Dialog>
  )
}
