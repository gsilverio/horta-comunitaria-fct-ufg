import { test, expect } from '@playwright/test'

test('editor persiste terreno, setor e canteiro no PostgreSQL e exibe mapa confirmado', async ({ page, request, browser }) => {
  const api = `${process.env.HORTA_E2E_API_URL}/api/v1`
  const created = await request.post(`${api}/gardens`, { data: { name: 'Horta mapa persistido' } })
  expect(created.status()).toBe(201)
  const garden = await created.json() as { id: string }
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto(`/#/hortas/${garden.id}/horta/interativa`)
  await page.getByRole('button', { name: 'Retangular', exact: true }).click()
  await page.getByRole('button', { name: 'Concluir terreno', exact: true }).click()
  await page.getByRole('button', { name: 'Setor com contorno do terreno', exact: true }).click()
  await page.getByRole('button', { name: 'Concluir setor', exact: true }).click()
  await page.getByRole('button', { name: '+ Retângulo', exact: true }).click()
  await page.getByRole('button', { name: 'Salvar configuração', exact: true }).click()
  await expect(page.getByText('Configuração salva · versão 1', { exact: true })).toBeVisible()
  const response = await request.get(`${api}/gardens/${garden.id}/layout`)
  expect(response.status()).toBe(200)
  const saved = await response.json() as { version: number; layout: { elements: { role: string; entityId?: string }[] } }
  expect(saved.version).toBe(1)
  expect(saved.layout.elements.filter(item => item.role === 'bed')).toHaveLength(1)
  expect(saved.layout.elements.filter(item => item.role === 'sector')).toHaveLength(1)
  expect(saved.layout.elements.every(item => item.entityId)).toBe(true)
  const context = await browser.newContext()
  const other = await context.newPage()
  await other.goto(`http://127.0.0.1:5183/#/hortas/${garden.id}/horta/interativa`)
  await expect(other.getByText('Configuração salva · versão 1', { exact: true })).toBeVisible()
  await other.getByRole('link', { name: 'Ver visão panorâmica', exact: true }).click()
  await expect(other.getByRole('heading', { name: 'Visão panorâmica da horta', exact: true })).toBeVisible()
  await expect(other.getByRole('checkbox', { name: 'Mostrar informações no mapa' })).not.toBeChecked()
  await other.getByRole('checkbox', { name: 'Mostrar informações no mapa' }).check()
  await expect(other.locator('.garden-scene-label-name').filter({ hasText: 'Retângulo 2' })).toBeVisible()
  await context.close()
})
