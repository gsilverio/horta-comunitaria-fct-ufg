import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { test as base, expect } from '@playwright/test'

/** Adm fictício criado por `--bootstrap-admin` no banco descartável do E2E. */
export const e2eAdministrator = { email: 'adm-e2e@horta.test', displayName: 'Adm fictício E2E' }

/** Cookie de renovação do Adm gravado pelo setup após a entrada pela tela; fora da árvore do projeto. */
export const administratorStorageState = join(tmpdir(), 'horta-e2e-auth', 'administrator.json')

/** Senha definida pelo setup a cada execução (gerada aleatoriamente, nunca versionada). */
export function administratorPassword() {
  const password = process.env.HORTA_E2E_ADMIN_PASSWORD
  if (!password) throw new Error('Senha do Adm do E2E ausente; o setup global não concluiu o login.')
  return password
}

/** Obtém um access token novo pela API real; o token dura minutos, por isso é emitido por teste. */
export async function administratorAccessToken() {
  const response = await fetch(`${process.env.HORTA_E2E_API_URL}/api/v1/auth/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: e2eAdministrator.email, password: administratorPassword() }),
  })
  if (!response.ok) throw new Error(`Login do Adm do E2E recusado (${response.status}).`)
  return (await response.json() as { accessToken: string }).accessToken
}

/** `request` autenticado como Adm para preparar e conferir dados diretamente na API. */
export const test = base.extend({
  request: async ({ playwright }, provide) => {
    const context = await playwright.request.newContext({ extraHTTPHeaders: { Authorization: `Bearer ${await administratorAccessToken()}` } })
    await provide(context)
    await context.dispose()
  },
})

export { expect }
