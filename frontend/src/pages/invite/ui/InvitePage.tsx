import { useParams } from 'react-router'
import { AcceptInvitation } from '@/features/invitation-accept'
import { Page } from '@/shared/ui'

export function InvitePage() {
  const { token = '' } = useParams()

  return (
    <Page title="Приглашение в команду">
      <AcceptInvitation token={token} />
    </Page>
  )
}
