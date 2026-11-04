import { test, expect } from '@playwright/test'
import type { Page, APIRequestContext } from '@playwright/test'
import pg from 'pg'
import { secondGarden, secondSector } from './setup'
import { utcInstant } from '../src/features/irrigation/formValues'

const garden = '01a11c0e-0d43-77e9-83f0-25404edc4cc5'
const sector = '01a11c0e-0d4a-71e0-9b3e-6ff7bb54245b'
const path = () => `${process.env.HORTA_E2E_API_URL}/api/v1/gardens/${garden}`
test.beforeEach(async () => {
  const connection = process.env.HORTA_E2E_DATABASE_URL
  if (!connection || !new URL(connection).pathname.startsWith('/horta_e2e_')) throw new Error('Banco descartável obrigatório')
  const db = new pg.Client({ connectionString: connection }); await db.connect()
  // Novos módulos referenciam irrigações: DELETE respeita FKs e limpa somente os fatos desta suíte.
  try { await db.query('BEGIN; DELETE FROM audit_entries; DELETE FROM irrigation_beds; DELETE FROM irrigations; COMMIT') } finally { await db.end() }
})
async function home(page: Page) {
  await page.goto(`/#/hortas/${garden}/irrigacao/registros`)
  await expect(page.getByRole('heading', { name: 'Irrigação', exact: true })).toBeVisible()
  await page.getByLabel('De', { exact: true }).fill('2026-10-08')
  await page.getByLabel('Até', { exact: true }).fill('2026-10-08')
  await page.getByRole('button', { name: 'Aplicar filtros' }).click()
  await expect(page.getByRole('heading', { name: 'Consumo conhecido de água' })).toBeVisible()
}
async function newForm(page: Page, volume: string | null = '120') {
  await page.getByRole('button', { name: '+ Nova irrigação' }).click()
  await page.getByRole('combobox', { name: 'Setor *', exact: true }).selectOption(sector)
  await page.getByLabel('Início *', { exact: true }).fill('2026-10-08T08:00')
  await page.getByLabel('Fim', { exact: true }).fill('2026-10-08T08:30')
  await expect(page.getByRole('checkbox')).toHaveCount(4)
  for (const checkbox of await page.getByRole('checkbox').all()) await checkbox.check()
  await page.getByLabel('Duração ativa (min)').fill('30')
  if (volume !== null) {
    await page.getByRole('textbox', { name: /^Volume \(L\)/ }).fill(volume)
    await page.getByLabel(/Método de obtenção/).fill('Recipiente graduado')
  }
}
async function createApi(request: APIRequestContext, volume: number | null = 120, precision = '') {
  const response = await request.post(`${path()}/irrigations`, { data: { operationId: crypto.randomUUID(), sectorId: sector, bedIds: [],
    startedAt: `2026-10-08T08:00:00${precision}-03:00`, endedAt: `2026-10-08T08:30:00${precision}-03:00`, activeDurationMinutes: 30,
    volumeLiters: volume, volumeSource: volume === null ? null : 'Measured', volumeMethod: volume === null ? null : 'Recipiente graduado',
    flowRateLitersPerMinute: null, status: 'Completed', responsibleParticipantId: null, notes: null } })
  expect(response.status()).toBe(201)
  return response.json() as Promise<{ id: string; version: number }>
}

test('percurso real 120 → 100 → Invalid/NoData, com quatro canteiros e histórico', async ({ page, request }) => {
  await home(page); await newForm(page)
  const createResponse = page.waitForResponse(response => response.request().method() === 'POST' && response.url().endsWith('/irrigations'))
  await page.getByRole('button', { name: 'Salvar irrigação' }).click()
  const createdResponse = await createResponse
  expect(createdResponse.status()).toBe(201)
  const created = await createdResponse.json() as { id: string; startedAt: string; beds: unknown[]; version: number }
  expect(created.startedAt).toBe('2026-10-08T11:00:00+00:00')
  expect(created.beds).toHaveLength(4)
  await expect(page.getByRole('heading', { name: 'Detalhe da irrigação' })).toBeVisible()
  await expect(page.getByRole('region', { name: 'Detalhe da irrigação' }).getByText('120 L', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Voltar aos registros' }).click()
  await expect(page.getByLabel('Volume conhecido', { exact: true })).toHaveText('120L')
  await page.reload(); await home(page)
  await expect(page.getByLabel('Volume conhecido', { exact: true })).toHaveText('120L')
  await page.screenshot({ path: 'test-results/desktop-dashboard.png', fullPage: true })
  await page.getByRole('navigation', { name: 'Paginação dos setores no consumo' }).getByRole('button', { name: 'Próxima' }).click()
  await expect(page.getByLabel('Volume conhecido', { exact: true })).toHaveText('120L')
  await page.getByRole('button', { name: /Abrir registro/ }).click()
  await page.getByRole('button', { name: 'Corrigir registro' }).click()
  await page.getByRole('textbox', { name: /^Volume \(L\)/ }).fill('100')
  await page.getByLabel('Motivo da correção *').fill('Conferência da medição')
  await page.getByRole('button', { name: 'Salvar correção' }).click()
  await expect(page.getByRole('region', { name: 'Detalhe da irrigação' }).getByText('100 L', { exact: true })).toBeVisible()
  await expect(page.getByText('Conferência da medição', { exact: false })).toBeVisible()
  const history = await request.get(`${path()}/irrigations/${created.id}/history`)
  const audit = await history.json() as { items: { before: { volumeLiters: number } | null; after: { volumeLiters: number }; actor: { displayName: string } }[] }
  expect(audit.items[0].before?.volumeLiters).toBe(120); expect(audit.items[0].after.volumeLiters).toBe(100)
  expect(audit.items[0].actor.displayName).toBe('Operador de demonstração')
  await page.getByRole('button', { name: 'Invalidar registro' }).click()
  await page.getByLabel('Motivo da invalidação *').fill('Registro lançado por engano')
  await page.getByRole('button', { name: 'Confirmar invalidação' }).click()
  await expect(page.getByText('Irrigação invalidada. O histórico foi preservado.')).toBeVisible()
  await page.getByRole('button', { name: 'Voltar aos registros' }).click()
  await expect(page.getByLabel('Volume conhecido', { exact: true })).toHaveText('—L')
  await expect(page.getByText('Sem dados', { exact: true }).first()).toBeVisible()
  await page.getByRole('combobox', { name: 'Registros', exact: true }).selectOption('Invalid')
  await page.getByRole('button', { name: 'Aplicar filtros' }).click()
  await expect(page.getByRole('button', { name: /Abrir registro/ })).toHaveCount(1)
  await page.reload()
  const persisted = await request.get(`${path()}/irrigations/${created.id}`)
  expect(await persisted.json()).toMatchObject({ version: 3, validity: 'Invalid', volumeLiters: 100 })
})

test('volume desconhecido, zero observado, estimativa e erro por campo', async ({ page }) => {
  await home(page); await newForm(page, null)
  await page.getByRole('button', { name: 'Salvar irrigação' }).click()
  await page.getByRole('button', { name: 'Voltar aos registros' }).click()
  await expect(page.getByText('Volume indisponível', { exact: true }).first()).toBeVisible()
  await newForm(page, '0')
  await page.getByRole('textbox', { name: /^Volume \(L\)/ }).fill('0,0001')
  await page.getByRole('button', { name: 'Salvar irrigação' }).click()
  await expect(page.getByRole('textbox', { name: /^Volume \(L\)/ })).toHaveAttribute('aria-invalid', 'true')
  await page.getByRole('textbox', { name: /^Volume \(L\)/ }).fill('0')
  await page.getByRole('combobox', { name: 'Origem do volume', exact: true }).selectOption('Estimated')
  await page.getByRole('button', { name: 'Salvar irrigação' }).click()
  await expect(page.getByRole('region', { name: 'Detalhe da irrigação' }).getByText('0 L', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Voltar aos registros' }).click()
  await expect(page.getByText('Informação parcial', { exact: true }).first()).toBeVisible()
  await expect(page.getByLabel('Volume conhecido', { exact: true })).toHaveText('0L')
  await expect(page.getByText('Inclui volumes estimados.', { exact: true })).toBeVisible()
})

test('perda da resposta após persistir: retry preserva chave/conteúdo e não duplica', async ({ page, request }) => {
  await home(page); await newForm(page)
  const attempts: unknown[] = []
  let first = true
  await page.route('**/api/v1/gardens/*/irrigations', async route => {
    if (route.request().method() !== 'POST') { await route.continue(); return }
    attempts.push(route.request().postDataJSON())
    if (first) { first = false; const response = await route.fetch(); expect(response.status()).toBe(201); await route.abort('failed') }
    else await route.continue()
  })
  await page.getByRole('button', { name: 'Salvar irrigação' }).click()
  await expect(page.getByRole('button', { name: 'Confirmar tentativa anterior' })).toBeVisible()
  await expect(page.getByRole('textbox', { name: /^Volume \(L\)/ })).toBeDisabled()
  await page.getByRole('button', { name: 'Confirmar tentativa anterior' }).click()
  await expect(page.getByRole('heading', { name: 'Detalhe da irrigação' })).toBeVisible()
  expect(attempts).toHaveLength(2); expect(attempts[0]).toEqual(attempts[1])
  const list = await request.get(`${path()}/irrigations?from=2026-10-08&to=2026-10-08`)
  expect(await list.json()).toMatchObject({ totalCount: 1 })
})

test('409 real preserva rascunho e exige revisão explícita da nova versão', async ({ page, request }) => {
  const created = await createApi(request)
  await home(page); await page.getByRole('button', { name: /Abrir registro/ }).click()
  await page.getByRole('button', { name: 'Corrigir registro' }).click()
  await page.getByRole('textbox', { name: /^Volume \(L\)/ }).fill('90')
  await page.getByLabel('Motivo da correção *').fill('Rascunho preservado')
  const invalidated = await request.post(`${path()}/irrigations/${created.id}/invalidation`, { data: { expectedVersion: 1, reason: 'Alteração concorrente' } })
  expect(invalidated.status()).toBe(200)
  await page.getByRole('button', { name: 'Salvar correção' }).click()
  await expect(page.getByRole('button', { name: 'Recarregar registro para revisão' })).toBeVisible()
  await expect(page.getByRole('textbox', { name: /^Volume \(L\)/ })).toHaveValue('90')
  await expect(page.getByLabel('Motivo da correção *')).toHaveValue('Rascunho preservado')
  await expect(page.getByRole('button', { name: 'Salvar correção' })).toBeDisabled()
  await page.getByRole('button', { name: 'Recarregar registro para revisão' }).click()
  await expect(page.getByText('Este registro foi invalidado e não pode mais ser corrigido.')).toBeVisible()
  const detail = await request.get(`${path()}/irrigations/${created.id}`)
  expect(await detail.json()).toMatchObject({ version: 2, volumeLiters: 120, validity: 'Invalid' })
})

test('catálogo com 101 canteiros e mudança de setor/horta', async ({ page }) => {
  await home(page); await newForm(page)
  await page.getByRole('combobox', { name: 'Setor *', exact: true }).selectOption('01a11c0e-0d4a-7872-adc6-357d4ab33105')
  await expect(page.getByRole('checkbox')).toHaveCount(0)
  page.once('dialog', dialog => dialog.accept())
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click()
  await page.getByRole('combobox', { name: 'Horta', exact: true }).selectOption(secondGarden)
  await page.getByRole('button', { name: '+ Nova irrigação' }).click()
  await expect(page.getByRole('combobox', { name: 'Setor *', exact: true })).toHaveValue('')
  await expect(page.getByRole('combobox', { name: 'Responsável pelo trabalho', exact: true })).toHaveValue('')
  await page.getByLabel('Início *', { exact: true }).fill('2026-10-08T08:00')
  await page.getByRole('combobox', { name: 'Setor *', exact: true }).selectOption(secondSector)
  await expect(page.getByRole('checkbox')).toHaveCount(101)
  await expect(page.getByRole('checkbox').last()).not.toBeChecked()
})

test('celular e teclado: formulário sem rolagem horizontal', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await home(page)
  await expect(page.getByRole('button', { name: '+ Nova irrigação' })).toBeVisible()
  await page.getByRole('button', { name: '+ Nova irrigação' }).click()
  await expect(page.getByRole('heading', { name: 'Nova irrigação' })).toBeFocused()
  await page.keyboard.press('Tab')
  await expect(page.getByRole('combobox', { name: 'Setor *', exact: true })).toBeFocused()
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click()
  await newForm(page)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.screenshot({ path: 'test-results/mobile-form.png', fullPage: true })
})

test('correção idêntica preserva microssegundos e não cria nova versão', async ({ page, request }) => {
  const created = await createApi(request, 120, '.123456')
  await home(page); await page.getByRole('button', { name: /Abrir registro/ }).click()
  await page.getByRole('button', { name: 'Corrigir registro' }).click()
  await expect(page.getByLabel('Início *', { exact: true })).not.toHaveValue('')
  await page.getByLabel('Motivo da correção *').fill('Conferência sem mudança')
  await page.getByRole('button', { name: 'Salvar correção' }).click()
  await expect(page.getByRole('heading', { name: 'Detalhe da irrigação' })).toBeVisible()
  const response = await request.get(`${path()}/irrigations/${created.id}`)
  expect(await response.json()).toMatchObject({ version: 1, startedAt: '2026-10-08T11:00:00.123456+00:00' })
  const history = await request.get(`${path()}/irrigations/${created.id}/history`)
  expect(await history.json()).toMatchObject({ totalCount: 1 })
})

test('conflito de correções permite salvar rascunho somente após revisão da versão atual', async ({ page, request }) => {
  const created = await createApi(request)
  await home(page); await page.getByRole('button', { name: /Abrir registro/ }).click()
  await page.getByRole('button', { name: 'Corrigir registro' }).click()
  await page.getByRole('textbox', { name: /^Volume \(L\)/ }).fill('90')
  await page.getByLabel('Motivo da correção *').fill('Medição revisada')
  const concurrent = await request.put(`${path()}/irrigations/${created.id}`, { data: {
    sectorId: sector, bedIds: [], startedAt: '2026-10-08T08:00:00-03:00', endedAt: '2026-10-08T08:30:00-03:00',
    activeDurationMinutes: 30, volumeLiters: 100, volumeSource: 'Measured', volumeMethod: 'Recipiente graduado',
    flowRateLitersPerMinute: null, status: 'Completed', responsibleParticipantId: null, notes: null, expectedVersion: 1, reason: 'Correção concorrente' } })
  expect(concurrent.status()).toBe(200)
  await page.getByRole('button', { name: 'Salvar correção' }).click()
  await page.getByRole('button', { name: 'Recarregar registro para revisão' }).click()
  await expect(page.getByText('Estado atual no servidor: versão 2')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Salvar correção' })).toBeDisabled()
  await expect(page.getByRole('textbox', { name: /^Volume \(L\)/ })).toHaveValue('90')
  await page.getByRole('button', { name: 'Revisei o estado; usar esta versão' }).click()
  await page.getByRole('button', { name: 'Salvar correção' }).click()
  await expect(page.getByRole('heading', { name: 'Detalhe da irrigação' })).toBeVisible()
  const result = await request.get(`${path()}/irrigations/${created.id}`)
  expect(await result.json()).toMatchObject({ volumeLiters: 90, version: 3 })
})

test('erro na consulta oferece recuperação sem fabricar dados', async ({ page }) => {
  let unavailable = true
  await page.route('**/api/v1/gardens/*/irrigations?*', async route => {
    if (unavailable) await route.abort('failed'); else await route.continue()
  })
  await page.goto(`/#/hortas/${garden}/irrigacao/registros`)
  await expect(page.getByRole('alert')).toBeVisible()
  await expect(page.getByText('Não foi possível acessar a API.', { exact: false })).toBeVisible()
  unavailable = false
  await page.getByRole('button', { name: 'Tentar novamente', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Registros de irrigação' })).toBeVisible()
})

test('gravação confirmada permanece visível se a leitura seguinte falhar', async ({ page, request }) => {
  await home(page); await newForm(page)
  let failDetail = true
  let writes = 0
  await page.route('**/api/v1/gardens/*/irrigations**', async route => {
    if (route.request().method() === 'POST') writes++
    if (failDetail && route.request().method() === 'GET' && /\/irrigations\/[^/?]+$/.test(new URL(route.request().url()).pathname)) await route.abort('failed')
    else await route.continue()
  })
  await page.getByRole('button', { name: 'Salvar irrigação' }).click()
  await expect(page.getByText('Irrigação salva. Os dados abaixo são confirmados pela API.')).toBeVisible()
  await expect(page.getByRole('region', { name: 'Detalhe da irrigação' }).getByRole('alert')).toBeVisible()
  failDetail = false
  await page.getByRole('button', { name: 'Tentar novamente', exact: true }).click()
  await expect(page.getByRole('region', { name: 'Detalhe da irrigação' }).getByText('120 L', { exact: true })).toBeVisible()
  expect(writes).toBe(1)
  const list = await request.get(`${path()}/irrigations?from=2026-10-08&to=2026-10-08`)
  expect(await list.json()).toMatchObject({ totalCount: 1 })
})

test('resposta antiga de canteiros não restaura seleção de outro setor', async ({ page }) => {
  await home(page)
  await page.getByRole('button', { name: '+ Nova irrigação' }).click()
  await page.getByLabel('Início *', { exact: true }).fill('2026-10-08T08:00')
  const received = Promise.withResolvers<void>(), gate = Promise.withResolvers<void>(), returned = Promise.withResolvers<void>()
  await page.route('**/api/v1/gardens/*/beds?*', async route => {
    if (new URL(route.request().url()).searchParams.get('sectorId') !== sector) { await route.continue(); return }
    const response = await route.fetch()
    received.resolve(); await gate.promise
    await route.fulfill({ response }); returned.resolve()
  })
  const selector = page.getByRole('combobox', { name: 'Setor *', exact: true })
  await selector.selectOption(sector); await received.promise
  await selector.selectOption('01a11c0e-0d4a-7872-adc6-357d4ab33105')
  await expect(page.getByText(/Nenhum canteiro associado a este setor nesta data/)).toBeVisible()
  gate.resolve(); await returned.promise
  await expect(page.getByRole('checkbox')).toHaveCount(0)
  await expect(selector).toHaveValue('01a11c0e-0d4a-7872-adc6-357d4ab33105')
})

test('fuso histórico resolve offset e rejeita horário inexistente/ambíguo', () => {
  expect(utcInstant('2026-10-08T08:00')).toBe('2026-10-08T11:00:00Z')
  expect(utcInstant('2018-01-08T08:00')).toBe('2018-01-08T10:00:00Z')
  expect(() => utcInstant('2018-11-04T00:30')).toThrow()
  expect(() => utcInstant('2019-02-16T23:30')).toThrow()
})


