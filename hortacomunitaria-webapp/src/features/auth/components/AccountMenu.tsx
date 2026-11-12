import { ChevronDown } from 'lucide-react'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '../../../components/ui/dropdown-menu'
import { changePasswordHref, usersHref } from '../../../routes/routes'
import { useAuth } from '../hooks/useAuth'
import { profileLabels } from '../types'

const initials = (name: string) => name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]?.toUpperCase()).join('')

/** Identificação da pessoa autenticada, com acesso a usuários (Adm), troca de senha e saída. */
export function AccountMenu() {
  const { user, isAdministrator, signOut } = useAuth()
  return <DropdownMenu>
    <DropdownMenuTrigger asChild>
      <button type="button" className="secondary flex min-h-11 shrink-0 items-center gap-2.5 border-0 bg-transparent px-1.5" aria-label={`Conta de ${user.displayName}, perfil ${profileLabels[user.profile]}`}>
        <span className="hidden min-w-0 text-right sm:block">
          <span className="block max-w-48 truncate text-[13px] leading-4 font-semibold text-horta-ink">{user.displayName}</span>
          <span className="block text-xs leading-4 text-horta-muted">{profileLabels[user.profile]}</span>
        </span>
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full border border-horta-line bg-horta-soft text-xs font-semibold text-horta-forest" aria-hidden="true">{initials(user.displayName)}</span>
        <ChevronDown aria-hidden="true" className="size-4 text-horta-muted" />
      </button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" className="w-60">
      <DropdownMenuLabel className="font-normal">
        <span className="block truncate font-semibold text-horta-ink">{user.displayName}</span>
        <span className="block truncate text-xs text-horta-muted">{user.email} · {profileLabels[user.profile]}</span>
      </DropdownMenuLabel>
      <DropdownMenuSeparator />
      {isAdministrator && <DropdownMenuItem asChild><a href={usersHref()}>Usuários</a></DropdownMenuItem>}
      <DropdownMenuItem asChild><a href={changePasswordHref}>Trocar senha</a></DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem onSelect={() => void signOut()}>Sair</DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
}
