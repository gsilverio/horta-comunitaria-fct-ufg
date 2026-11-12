import { useCallback, useState } from 'react'
import { Failure, Pagination } from '../../../components/Feedback'
import { Field, Group, SelectField, Toggle } from '../../../components/Fields'
import { PageHeader } from '../../../components/PageHeader'
import { Icon } from '../../../components/Icon'
import { useRemote } from '../../../hooks/useRemote'
import { apiFailure } from '../../../services/http'
import { go, replaceQuery } from '../../../routes/navigation'
import { usersHref } from '../../../routes/routes'
import type { AppRoute } from '../../../routes/routes'
import type { ApiFailure } from '../../../types/api'
import { useAuth } from '../../auth/hooks/useAuth'
import { profileLabels } from '../../auth/types'
import type { UserProfile } from '../../auth/types'
import { userApi } from '../services/userApi'
import type { User } from '../services/userApi'

const profileOptions = (Object.keys(profileLabels) as UserProfile[]).map(id => ({ id, label: profileLabels[id] }))
const isProfile = (value: string): value is UserProfile => value in profileLabels

/** Contas de acesso: lista, criação com senha inicial e alteração de nome, perfil e situação. Exclusivo do Adm. */
export function UsersPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
  const { isAdministrator } = useAuth()
  if (!isAdministrator) return <section className="panel"><h1 tabIndex={-1}>Ação restrita ao perfil Adm</h1><p>A gestão de usuários é feita pela administração da horta.</p></section>
  if (route.id === 'novo') return <CreateUser saved={user => { saved(`Usuário ${user.displayName} criado. Entregue a senha inicial; a troca será exigida no primeiro acesso.`); go(usersHref()) }} />
  if (route.id) return <EditUser id={route.id} saved={user => { saved(`Usuário ${user.displayName} atualizado.`); go(usersHref()) }} />
  return <UserList route={route} />
}

function UserList({ route }: { route: AppRoute }) {
  const page = Number(route.search.get('page')) || 1
  const status = route.search.get('isActive') ?? 'true'
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => userApi.list({ page, pageSize: 20, isActive: status === '' ? undefined : status === 'true' }, signal), [page, status])
  const result = useRemote(`${page}/${status}/${revision}`, load)
  return <>
    <PageHeader title="Usuários" description="Contas de acesso à aplicação. Operador registra e corrige fatos de campo; Adm também mantém estrutura, catálogos e usuários."
      actions={<button className="flex items-center justify-center gap-2" onClick={() => go(usersHref('novo'))}><Icon name="plus" />Novo usuário</button>} />
    <div className="mb-3 max-w-56">
      <SelectField name="isActive" label="Situação" value={status} change={value => replaceQuery(query => { query.set('isActive', value); query.delete('page') })}
        options={[{ id: 'true', label: 'Ativos' }, { id: 'false', label: 'Inativos' }]} />
    </div>
    <section className="panel flush" aria-busy={result.loading}>
      <div className="results-heading"><h2>Contas</h2><span className="text-xs text-horta-muted">{result.data ? `${result.data.totalCount} no recorte` : '—'}</span></div>
      {result.loading && <p role="status" className="panel-body plant-status">Carregando usuários…</p>}
      {result.error && <div className="panel-body"><Failure error={result.error} retry={() => setRevision(value => value + 1)} /></div>}
      {result.data && (result.data.totalCount === 0
        ? <div className="empty"><Icon name="records" /><h3>Nenhum usuário neste recorte</h3></div>
        : <ul className="record-results">{result.data.items.map(user => <li key={user.id}>
          <a className="flex min-h-11 flex-wrap items-center justify-between gap-2" href={usersHref(user.id)}>
            <span className="min-w-0"><strong className="block truncate">{user.displayName}</strong><span className="block truncate text-sm text-horta-muted">{user.email}</span></span>
            <span className="flex flex-wrap gap-1.5">
              <span className="badge">{profileLabels[user.profile]}</span>
              {!user.isActive && <span className="badge invalid">Inativo</span>}
              {user.mustChangePassword && <span className="badge">Senha inicial pendente</span>}
              {user.isLockedOut && <span className="badge invalid">Bloqueado temporariamente</span>}
            </span>
          </a>
        </li>)}</ul>)}
      {result.data && <Pagination data={result.data} change={next => replaceQuery(query => query.set('page', String(next)))} label="Páginas de usuários" />}
    </section>
  </>
}

function CreateUser({ saved }: { saved: (user: User) => void }) {
  const [draft, setDraft] = useState({ email: '', displayName: '', profile: '', initialPassword: '' })
  const [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>()
  const change = (key: keyof typeof draft) => (value: string) => setDraft(current => ({ ...current, [key]: value }))
  async function submit(event: React.FormEvent) {
    event.preventDefault()
    if (busy) return
    if (!isProfile(draft.profile)) { setError({ message: 'Revise os campos indicados.', fields: { profile: ['Selecione o perfil.'] } }); return }
    setBusy(true); setError(undefined)
    try { saved(await userApi.create({ email: draft.email.trim(), displayName: draft.displayName.trim(), profile: draft.profile, initialPassword: draft.initialPassword })) }
    catch (failure) { setError(apiFailure(failure)); setBusy(false) }
  }
  return <form onSubmit={submit} className="panel">
    <PageHeader title="Novo usuário" headingLevel={1} description="A pessoa entra com a senha inicial e precisa trocá-la no primeiro acesso. Campos com * são obrigatórios." />
    <Group title="Identificação">
      <Field name="displayName" label="Nome" required value={draft.displayName} change={change('displayName')} error={error} maxLength={120} />
      <Field name="email" label="E-mail" type="email" required value={draft.email} change={change('email')} error={error} maxLength={256} />
    </Group>
    <Group title="Acesso">
      <SelectField name="profile" label="Perfil" required value={draft.profile} change={change('profile')} options={profileOptions} error={error} />
      <Field name="initialPassword" label="Senha inicial" type="password" required value={draft.initialPassword} change={change('initialPassword')} error={error} maxLength={128}
        hint="Pelo menos 12 caracteres, sem senhas comuns nem o nome ou o e-mail. Entregue-a por um canal reservado." />
    </Group>
    {error && Object.keys(error.fields).length === 0 && <Failure error={error} />}
    <div className="mt-4 flex flex-wrap gap-2">
      <button type="submit" disabled={busy}>{busy ? 'Salvando…' : 'Criar usuário'}</button>
      <button type="button" className="secondary" onClick={() => go(usersHref())} disabled={busy}>Cancelar</button>
    </div>
  </form>
}

function EditUser({ id, saved }: { id: string; saved: (user: User) => void }) {
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => userApi.get(id, signal), [id])
  const result = useRemote(`${id}/${revision}`, load)
  if (result.loading) return <p role="status">Carregando usuário…</p>
  if (result.error || !result.data) return <><Failure error={result.error ?? { message: 'Usuário não encontrado.', fields: {} }} retry={() => setRevision(value => value + 1)} /><a className="action-link secondary" href={usersHref()}>Voltar aos usuários</a></>
  return <UserAccessForm key={`${result.data.id}/${result.data.version}`} user={result.data} saved={saved} reload={() => setRevision(value => value + 1)} />
}

function UserAccessForm({ user, saved, reload }: { user: User; saved: (user: User) => void; reload: () => void }) {
  const { user: current } = useAuth()
  const self = current.id === user.id
  const [draft, setDraft] = useState<{ displayName: string; profile: string; isActive: boolean; reason: string }>({ displayName: user.displayName, profile: user.profile, isActive: user.isActive, reason: '' })
  const [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>()
  async function submit(event: React.FormEvent) {
    event.preventDefault()
    if (busy || !isProfile(draft.profile)) return
    setBusy(true); setError(undefined)
    try { saved(await userApi.update(user.id, { displayName: draft.displayName.trim(), profile: draft.profile, isActive: draft.isActive, expectedVersion: user.version, reason: draft.reason.trim() })) }
    catch (failure) { setError(apiFailure(failure)); setBusy(false) }
  }
  return <form onSubmit={submit} className="panel">
    <PageHeader title={user.displayName} headingLevel={1} status={<span className={user.isActive ? 'badge' : 'badge invalid'}>{user.isActive ? 'Ativo' : 'Inativo'}</span>}
      description={`${user.email} · Perfil ${profileLabels[user.profile]}${user.mustChangePassword ? ' · Senha inicial pendente' : ''}`} />
    <Group title="Acesso" hint="Mudar perfil ou situação encerra as sessões abertas da conta.">
      <Field name="displayName" label="Nome" required value={draft.displayName} change={value => setDraft(d => ({ ...d, displayName: value }))} error={error} maxLength={120} />
      <SelectField name="profile" label="Perfil" required value={draft.profile} change={value => setDraft(d => ({ ...d, profile: value }))} options={profileOptions} error={error}
        hint={self ? 'Você não pode retirar o perfil Adm da própria conta.' : undefined} />
      {!self && <Toggle label="Conta ativa" checked={draft.isActive} change={value => setDraft(d => ({ ...d, isActive: value }))} />}
      <Field name="reason" label="Motivo da alteração" type="textarea" required value={draft.reason} change={value => setDraft(d => ({ ...d, reason: value }))} error={error} maxLength={500} />
    </Group>
    <p className="hint">A redefinição de senha pela administração ainda não está disponível.</p>
    {error && Object.keys(error.fields).length === 0 && <Failure error={error} />}
    <div className="mt-4 flex flex-wrap gap-2">
      <button type="submit" disabled={busy || !draft.reason.trim()}>{busy ? 'Salvando…' : 'Salvar alterações'}</button>
      {error?.status === 409 && <button type="button" className="secondary" onClick={reload}>Recarregar dados atuais</button>}
      <button type="button" className="secondary" onClick={() => go(usersHref())} disabled={busy}>Cancelar</button>
    </div>
  </form>
}
