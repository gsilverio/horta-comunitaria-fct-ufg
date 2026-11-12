import { useState } from 'react'
import { PageHeader } from '../../../components/PageHeader'
import { ChangePasswordForm } from '../components/ChangePasswordForm'
import { useAuth } from '../hooks/useAuth'

/** Troca voluntária da própria senha; as demais sessões da conta são encerradas pelo servidor. */
export function ChangePasswordPage() {
  const { passwordChanged } = useAuth()
  const [done, setDone] = useState(false)
  return <section className="panel max-w-xl">
    <PageHeader title="Trocar senha" description="Ao trocar a senha, as sessões abertas em outros dispositivos são encerradas." />
    {done
      ? <p className="feedback success" role="status">Senha alterada. Use a nova senha no próximo acesso.</p>
      : <ChangePasswordForm done={async () => { await passwordChanged(); setDone(true) }} cancel={() => history.back()} />}
  </section>
}
