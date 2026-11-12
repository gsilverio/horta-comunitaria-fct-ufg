import { test, expect } from './fixtures'
import type { APIRequestContext } from '@playwright/test'
import { Temporal } from '@js-temporal/polyfill'
import { today } from '../src/features/irrigation/formValues'
import { cropDensity, cropSymbolCount, bedCropSymbols, cropAsset } from '../src/features/interactive-garden/cropVisuals'
import type { GardenCrop } from '../src/features/interactive-garden/cropVisuals'
import manifest from '../src/features/interactive-garden/crop-assets.json' with { type: 'json' }
import { uuidV7 } from './setup'

const api = () => `${process.env.HORTA_E2E_API_URL}/api/v1`
async function post(request: APIRequestContext, path: string, data: unknown): Promise<{ id: string }> {
  const response = await request.post(api() + path, { data })
  expect(response.status(), await response.text()).toBe(201)
  return response.json()
}

test('faixas visuais preservam ausência, limites, variedade e orçamento sem depender do zoom', () => {
  const crop: GardenCrop = { cycleId: 'cycle', bedId: 'bed', speciesId: 'species', speciesName: 'Alface', speciesCatalogKey: 'embrapa-2023:alface', scientificName: 'Lactuca sativa', speciesImageUrl: null, cultivarName: 'BRS Lélia', cultivarCatalogKey: 'embrapa-2023:page-16', occupiedAreaM2: 4, initialQuantity: null, hasReplacements: false, hasMixedPlantingDates: false, firstPlantedOn: today(), lastHarvestedOn: null, visualGrowthDays: null, visualHarvestDays: null, stage: 'Planted' }
  expect(cropSymbolCount(crop)).toBe(0)
  expect(cropDensity(crop)).toBeUndefined()
  expect(cropSymbolCount({ ...crop, initialQuantity: 1 })).toBe(1)
  expect(cropDensity({ ...crop, initialQuantity: 16 })).toBe('Baixa')
  expect(cropDensity({ ...crop, initialQuantity: 17 })).toBe('Média')
  expect(cropDensity({ ...crop, initialQuantity: 48 })).toBe('Média')
  expect(cropDensity({ ...crop, initialQuantity: 49 })).toBe('Alta')
  expect(cropSymbolCount({ ...crop, initialQuantity: 16 })).toBe(2)
  expect(cropSymbolCount({ ...crop, initialQuantity: 48 })).toBe(4)
  expect(cropSymbolCount({ ...crop, initialQuantity: 49 })).toBe(6)
  expect(bedCropSymbols(Array.from({ length: 100 }, (_, i) => ({ ...crop, cycleId: String(i), initialQuantity: 100 })))).toHaveLength(12)
  expect(cropAsset(crop).url).toContain('alface-page-16-seedling.svg')
  expect(cropAsset({ ...crop, stage: 'Growing' }).url).toContain('growing.svg')
  expect(cropAsset({ ...crop, stage: 'HarvestExpected' }).url).toContain('harvest.svg')
  expect(cropAsset({ ...crop, speciesCatalogKey: null, scientificName: null, cultivarCatalogKey: null }).generic).toBe(true)
})

test('panorama usa plantios reais, densidades distintas e prazos configurados pela interface', async ({ page, request }) => {
  test.setTimeout(90000)
  const on = today(), plantedOn = Temporal.PlainDate.from(on).subtract({ days: 10 }).toString()
  const garden = await post(request, '/gardens', { name: 'Horta dos sprites', operationalAreaM2: 144 })
  const prefix = `/gardens/${garden.id}`
  const sector = await post(request, `${prefix}/sectors`, { name: 'Setor de cultivo' })
  const species = await post(request, '/species', { commonName: 'Alface visual', scientificName: 'Lactuca sativa' })
  const cycles: string[] = []
  for (const [index, quantity] of [8, 24, 80, 0].entries()) {
    const bed = await post(request, `${prefix}/beds`, { name: `Canteiro visual ${index + 1}` })
    const configured = await request.post(api() + `${prefix}/beds/${bed.id}/configurations`, { data: { validFrom: plantedOn, cultivableAreaM2: 4, widthMeters: 2, lengthMeters: 2, expectedBedVersion: 1 } })
    expect(configured.status()).toBe(201)
    expect((await request.post(api() + `${prefix}/beds/${bed.id}/sector-assignments`, { data: { sectorId: sector.id, validFrom: plantedOn, expectedBedVersion: 2 } })).status()).toBe(201)
    const cycle = await post(request, prefix + '/cultivation-cycles', { operationId: uuidV7(), bedId: bed.id, speciesId: species.id, startedOn: plantedOn, occupiedAreaM2: 4 })
    cycles.push(cycle.id)
    if (quantity) await post(request, prefix + '/plantings', { operationId: uuidV7(), cycleId: cycle.id, plantedOn, kind: 'Initial', plantedQuantity: quantity })
  }
  await page.goto(`/catalogo/especies/${species.id}/fases`)
  await page.getByLabel('Dias para crescimento intermediário').fill('5')
  await page.getByLabel('Dias para colheita prevista').fill('20')
  await page.getByLabel('Motivo da alteração').fill('Prazos fictícios usados apenas neste teste')
  await page.getByRole('button', { name: 'Salvar prazos visuais', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Alface visual', exact: true })).toBeVisible()
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto(`/hortas/${garden.id}/visao-geral`)
  const scene = page.getByRole('application', { name: 'Mapa panorâmico interativo da horta' })
  await expect(scene.locator('[data-crop-stage="Growing"]')).toHaveCount(12)
  await expect(scene.locator('[data-density="Baixa"]')).toHaveCount(2)
  await expect(scene.locator('[data-density="Média"]')).toHaveCount(4)
  await expect(scene.locator('[data-density="Alta"]')).toHaveCount(6)
  const bed = scene.getByRole('button', { name: 'Canteiro visual 3, retângulo', exact: true })
  await bed.focus(); await page.keyboard.press('Enter')
  const inspector = page.getByRole('complementary', { name: 'Dados de Canteiro visual 3' })
  await expect(inspector).toContainText('80 plantas iniciais')
  await expect(inspector).toContainText('Crescimento intermediário · fase estimada')
  await page.locator('.garden-scene').screenshot({ path: 'test-results/crop-panorama-desktop.png' })
  await page.setViewportSize({ width: 390, height: 844 })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
  const sceneBounds = await page.locator('.garden-scene').boundingBox()
  expect(sceneBounds?.width).toBeLessThanOrEqual(390)
  await page.getByRole('button', { name: 'Fechar detalhes do elemento', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Aproximar câmera', exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Aproximar câmera', exact: true }).click()
  await expect(page.locator('.garden-scene-controls')).toContainText('122%')
  await page.locator('.garden-scene').screenshot({ path: 'test-results/crop-panorama-mobile.png' })
  await post(request, prefix + '/harvests', { operationId: uuidV7(), cycleId: cycles[0], harvestedOn: on, freshMassKg: 1 })
  await page.reload()
  await expect(scene.locator('[data-crop-stage="HarvestRecorded"]')).toHaveCount(3)
  await expect(scene.locator('[data-crop-stage="Growing"]')).toHaveCount(24)
})

test('sprites de três fases carregam e possuem cobertura para as 113 cultivares', async ({ page }) => {
  expect(Object.keys(manifest.species)).toHaveLength(23)
  expect(Object.keys(manifest.cultivars)).toHaveLength(113)
  await page.goto('/')
  await page.setViewportSize({ width: 1320, height: 1400 })
  await page.setContent(`<html lang="pt-BR"><body style="margin:24px;background:#e7ecd9;font:14px sans-serif"><h1>Catálogo de sprites · plantio / crescimento / colheita</h1><div style="display:grid;grid-template-columns:repeat(5,1fr);gap:14px">${Object.entries(manifest.assets).map(([key, phases]) => `<article style="background:#fcf9eb;padding:10px;border-radius:8px"><strong>${key}</strong><div style="display:flex">${Object.values(phases).map(url => `<img alt="${key}" src="http://127.0.0.1:5183${url}" style="width:64px;height:80px;image-rendering:pixelated">`).join('')}</div></article>`).join('')}</div></body></html>`)
  await expect.poll(() => page.locator('img').evaluateAll(images => images.every(image => image instanceof HTMLImageElement && image.complete && image.naturalWidth > 0))).toBe(true)
  await page.screenshot({ path: 'test-results/crop-sprite-catalog.png', fullPage: true })
})
