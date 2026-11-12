import { createContext, useContext } from 'react'

/** Retorno explícito da página (parâmetro `voltar`), exibido na mesma barra da trilha. */
export interface BackLinkTarget { href: string; label: string }

export const BackLinkState = createContext<BackLinkTarget | undefined>(undefined)
export function useBackLink() { return useContext(BackLinkState) }
