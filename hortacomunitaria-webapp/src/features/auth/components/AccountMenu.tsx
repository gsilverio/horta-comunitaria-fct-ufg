import { ChevronDown, KeyRound, LogOut, Users } from 'lucide-react'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '../../../components/ui/dropdown-menu'
import { changePasswordHref, usersHref } from '../../../routes/routes'
import { useAuth } from '../hooks/useAuth'
import { profileLabels } from '../types'

const initials = (name: string) => name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]?.toUpperCase()).join('')

function Avatar({ name, large }: { name: string; large?: boolean }) {
  return <span className={`account-avatar ${large ? 'size-10 text-sm' : 'size-8 text-xs'}`} aria-hidden="true">{initials(name)}</span>
}

/** Identificação da pessoa autenticada, com acesso a usuários (Adm), troca de senha e saída. */
export function AccountMenu() {
  const { user, isAdministrator, signOut } = useAuth()
  return <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <button type="button" className="account-trigger" aria-label={`Conta de ${user.displayName}, perfil ${profileLabels[user.profile]}`}>
        <Avatar name={user.displayName} />
        <span className="hidden min-w-0 text-left sm:block">
          <span className="block max-w-44 truncate text-[13px] leading-4 font-semibold text-horta-ink">{user.displayName}</span>
          <span className="block text-[11px] leading-4 text-horta-muted">{profileLabels[user.profile]}</span>
        </span>
        <ChevronDown aria-hidden="true" className="size-4 shrink-0 text-horta-muted" />
      </button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" sideOffset={6} className="w-64 p-1.5">
      <DropdownMenuLabel className="flex items-center gap-3 px-2 py-2 font-normal">
        <Avatar name={user.displayName} large />
        <span className="min-w-0">
          <span className="block truncate text-sm font-semibold text-horta-ink">{user.displayName}</span>
          <span className="block truncate text-xs text-horta-muted">{user.email}</span>
          <span className="account-profile">{profileLabels[user.profile]}</span>
        </span>
      </DropdownMenuLabel>
      <DropdownMenuSeparator />
      {isAdministrator && <DropdownMenuItem asChild><a href={usersHref()} className="account-item"><Users aria-hidden="true" />Usuários</a></DropdownMenuItem>}
      <DropdownMenuItem asChild><a href={changePasswordHref} className="account-item"><KeyRound aria-hidden="true" />Trocar senha</a></DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem className="account-item text-horta-danger" onSelect={() => void signOut()}><LogOut aria-hidden="true" />Sair</DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
}
