import { spawn, execFileSync } from 'node:child_process'
import { randomBytes } from 'node:crypto'
import { mkdirSync, createWriteStream, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join, resolve } from 'node:path'
import { chromium } from '@playwright/test'
import pg from 'pg'
import { administratorStorageState, e2eAdministrator } from './fixtures'

/** Contexto de teste exclusivamente no banco descartável, sem dados inseridos pela aplicação. */
export const secondGarden = '01a11c0e-0d50-77e9-83f0-25404edc4cc5'
export const secondSector = '01a11c0e-0d51-77e9-83f0-25404edc4cc5'
export function uuidV7() {
  const bytes = randomBytes(16)
  bytes.writeUIntBE(Date.now(), 0, 6)
  bytes[6] = (bytes[6] & 15) | 112; bytes[8] = (bytes[8] & 63) | 128
  const hex = bytes.toString('hex')
  return `${hex.slice(0,8)}-${hex.slice(8,12)}-${hex.slice(12,16)}-${hex.slice(16,20)}-${hex.slice(20)}`
}
export default async function setup() {
  const connection = process.env.HORTA_E2E_POSTGRES_URL
  if (!connection) throw new Error('Defina HORTA_E2E_POSTGRES_URL para PostgreSQL 17 local dedicado a testes, com permissão de criar bancos.')
  const url = new URL(connection)
  if (!['localhost', '127.0.0.1', '[::1]'].includes(url.hostname)) throw new Error('O E2E demonstrativo usa somente PostgreSQL em loopback.')
  const admin = new pg.Client({ connectionString: connection })
  await admin.connect()
  const version = await admin.query<{ server_version_num: string }>('SHOW server_version_num')
  if (Number(version.rows[0].server_version_num) < 170000 || Number(version.rows[0].server_version_num) >= 180000) { await admin.end(); throw new Error('Os testes exigem PostgreSQL 17.') }
  const database = `horta_e2e_${uuidV7().replaceAll('-', '')}`
  await admin.query(`CREATE DATABASE "${database}"`)
  const dbUrl = new URL(connection); dbUrl.pathname = `/${database}`
  process.env.HORTA_E2E_DATABASE_URL = dbUrl.toString()
  process.env.HORTA_E2E_API_URL = 'http://127.0.0.1:5279'
  const repo = resolve(import.meta.dirname, '../..')
  mkdirSync(resolve(import.meta.dirname, '../test-results'), { recursive: true })
  const log = createWriteStream(resolve(import.meta.dirname, '../test-results/backend.log'))
  const quote = (value: string) => `'${value.replaceAll("'", "''")}'`
  const npgsql = `Host=${quote(url.hostname)};Port=${url.port || '5432'};Database=${database};Username=${quote(decodeURIComponent(url.username))};Password=${quote(decodeURIComponent(url.password))}`
  // Chaves do Data Protection descartáveis desta execução, fora do repositório.
  const keysPath = join(tmpdir(), 'horta-e2e-keys', database)
  const apiEnv = { ...process.env, DOTNET_ENVIRONMENT: 'Development', Persistence__ConnectionString: npgsql,
    Authentication__KeysPath: keysPath,
    // Cada teste obtém um token pelo login; o limite padrão (10/min por IP) é para uso real.
    Authentication__RateLimitPerMinute: '1000' }
  let api: ReturnType<typeof spawn> | undefined
  let frontend: ReturnType<typeof spawn> | undefined
  async function stop(child: ReturnType<typeof spawn> | undefined) {
    if (!child || child.exitCode !== null || !child.pid) return
    const exited = new Promise<void>(done => child.once('exit', () => done()))
    if (process.platform === 'win32') execFileSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore', windowsHide: true })
    else child.kill('SIGTERM')
    await exited
  }
  async function cleanup() {
    await stop(frontend); await stop(api)
    log.end()
    await admin.query('SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1 AND pid <> pg_backend_pid()', [database])
    if (!/^horta_e2e_[0-9a-f]{32}$/.test(database)) throw new Error('Nome de banco de teste inválido')
    await admin.query(`DROP DATABASE "${database}"`); await admin.end()
    rmSync(dirname(administratorStorageState), { recursive: true, force: true })
    rmSync(keysPath, { recursive: true, force: true })
  }
  const apiDll = resolve(repo, 'HortaComunitaria.Api/bin/Release/net10.0/HortaComunitaria.Api.dll')

  /**
   * Primeiro Adm pelo comando administrativo explícito, como em produção, e troca da senha inicial pela API.
   * As senhas são aleatórias por execução e só existem no banco descartável.
   */
  async function createAdministrator() {
    const initial = `Inicial-${randomBytes(12).toString('hex')}`
    const password = `Sessao-${randomBytes(12).toString('hex')}`
    try {
      execFileSync('dotnet', [apiDll, '--bootstrap-admin'], { cwd: resolve(repo, 'HortaComunitaria.Api'), stdio: 'pipe', windowsHide: true,
        env: { ...apiEnv, Bootstrap__Email: e2eAdministrator.email, Bootstrap__DisplayName: e2eAdministrator.displayName, Bootstrap__Password: initial } })
    } catch { throw new Error('O comando --bootstrap-admin recusou a criação do Adm do E2E.') }
    const auth = `${process.env.HORTA_E2E_API_URL}/api/v1/auth`
    const login = await fetch(`${auth}/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: e2eAdministrator.email, password: initial }) })
    if (!login.ok) throw new Error(`Login inicial do Adm do E2E recusado (${login.status}).`)
    const { accessToken } = await login.json() as { accessToken: string }
    const changed = await fetch(`${auth}/change-password`, { method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` }, body: JSON.stringify({ currentPassword: initial, newPassword: password }) })
    if (!changed.ok) throw new Error(`Troca da senha inicial do Adm do E2E recusada (${changed.status}).`)
    process.env.HORTA_E2E_ADMIN_PASSWORD = password
  }

  /** Entra pela tela real e guarda o cookie de renovação; cada teste abre a aplicação com a sessão restaurada. */
  async function saveAdministratorSession() {
    const browser = await chromium.launch()
    try {
      const context = await browser.newContext({ baseURL: 'http://127.0.0.1:5183' })
      const page = await context.newPage()
      await page.goto('/')
      await page.getByLabel('E-mail').fill(e2eAdministrator.email)
      await page.getByLabel('Senha').fill(process.env.HORTA_E2E_ADMIN_PASSWORD ?? '')
      await Promise.all([
        page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/me' && response.ok()),
        page.getByRole('button', { name: 'Entrar', exact: true }).click(),
      ])
      mkdirSync(dirname(administratorStorageState), { recursive: true })
      const state = await context.storageState({ path: administratorStorageState })
      if (!state.cookies.some(cookie => cookie.name === '__Secure-horta.refresh'))
        throw new Error('O navegador não guardou o cookie de renovação; a sessão não seria restaurada entre os testes.')
    } finally { await browser.close() }
  }

  try {
    execFileSync('dotnet', ['build', 'HortaComunitaria.Api/HortaComunitaria.Api.csproj', '-c', 'Release', '--no-restore', '-m:1'], { cwd: repo, stdio: 'pipe', windowsHide: true })
    api = spawn('dotnet', ['run', '--project', 'HortaComunitaria.Api', '-c', 'Release', '--no-build', '--no-restore', '--no-launch-profile'], {
      cwd: repo, windowsHide: true, env: { ...apiEnv, ASPNETCORE_URLS: process.env.HORTA_E2E_API_URL, Persistence__ApplyMigrationsOnStartup: 'true' },
    })
    api.stdout?.pipe(log, { end: false }); api.stderr?.pipe(log, { end: false })
    let ready = false
    for (let attempt = 0; attempt < 300 && !ready; attempt++) {
      if (api.exitCode !== null) throw new Error('A API não iniciou. Confira test-results/backend.log.')
      try { ready = (await fetch(`${process.env.HORTA_E2E_API_URL}/health/ready`)).ok } catch { /* aguardar readiness */ }
      if (!ready) await new Promise(done => setTimeout(done, 100))
    }
    if (!ready) throw new Error('Readiness da API não respondeu no prazo.')
    const db = new pg.Client({ connectionString: dbUrl.toString() }); await db.connect()
    try {
      if (!/^horta_e2e_[0-9a-f]{32}$/.test(database)) throw new Error('O contexto exige banco descartável horta_e2e_.')
      const empty = await db.query<{ count: string }>('SELECT count(*) FROM gardens')
      if (empty.rows[0].count !== '0') throw new Error('A aplicação não deve inserir dados automaticamente.')
      const garden = '01a11c0e-0d43-77e9-83f0-25404edc4cc5'
      const sector = '01a11c0e-0d4a-71e0-9b3e-6ff7bb54245b'
      await db.query('INSERT INTO gardens ("Id", "Code", "Name", "IsActive") VALUES ($1,$2,$3,true)', [garden, 'DEMO', 'Horta fictícia de demonstração'])
      await db.query('INSERT INTO irrigation_sectors ("Id", "GardenId", "Code", "Name", "IsActive") VALUES ($1,$2,$3,$4,true), ($5,$2,$6,$7,true)', [sector, garden, 'S1', 'Setor demonstrativo 1', '01a11c0e-0d4a-7872-adc6-357d4ab33105', 'S2', 'Setor demonstrativo 2'])
      const beds = ['01a11c0e-0d4b-7411-b9f3-f5a646477e57', '01a11c0e-0d4b-7360-ac23-889dd3effc2e', '01a11c0e-0d4b-7883-a1fd-c98a47cb335c', '01a11c0e-0d4b-70ff-806c-238d20497b3e']
      for (const [index, bed] of beds.entries()) {
        await db.query('INSERT INTO beds ("Id", "GardenId", "Code", "Name", "IsActive") VALUES ($1,$2,$3,$4,true)', [bed, garden, `B${index + 1}`, `Canteiro demonstrativo ${index + 1}`])
        await db.query('INSERT INTO bed_sector_assignments ("Id", "GardenId", "BedId", "IrrigationSectorId", "ValidFrom", "ValidUntil") VALUES ($1,$2,$3,$4,$5,NULL)', [uuidV7(), garden, bed, sector, '2026-10-01'])
      }
      await db.query('INSERT INTO participants ("Id", "GardenId", "DisplayName", "IsActive") VALUES ($1,$2,$3,true)', ['01a11c0e-0d4c-7c58-884b-78bf1729acbf', garden, 'Participante fictício'])
      await db.query('INSERT INTO gardens ("Id", "Code", "Name", "IsActive") VALUES ($1, $2, $3, true)', [secondGarden, 'ZZ_DEMO', 'Outra horta demonstrativa'])
      await db.query('INSERT INTO irrigation_sectors ("Id", "GardenId", "Code", "Name", "IsActive") VALUES ($1, $2, $3, $4, true)', [secondSector, secondGarden, 'R1', 'Setor da outra horta'])
      for (let index = 1; index <= 101; index++) {
        const bed = uuidV7()
        await db.query('INSERT INTO beds ("Id", "GardenId", "Code", "Name", "IsActive") VALUES ($1,$2,$3,$4,true)', [bed, secondGarden, `C${String(index).padStart(3, '0')}`, `Canteiro demonstrativo ${index}`])
        await db.query('INSERT INTO bed_sector_assignments ("Id", "GardenId", "BedId", "IrrigationSectorId", "ValidFrom", "ValidUntil") VALUES ($1,$2,$3,$4,$5,NULL)', [uuidV7(), secondGarden, bed, secondSector, '2026-10-01'])
      }
      for (let index = 3; index <= 6; index++) await db.query('INSERT INTO irrigation_sectors ("Id", "GardenId", "Code", "Name", "IsActive") VALUES ($1,$2,$3,$4,true)', [uuidV7(), '01a11c0e-0d43-77e9-83f0-25404edc4cc5', `S${index}`, `Setor demonstrativo ${index}`])
    } finally { await db.end() }
    await createAdministrator()
    if (process.env.HORTA_E2E_EMBRAPA === 'true') execFileSync('dotnet', [apiDll, '--seed-embrapa'], { cwd: resolve(repo, 'HortaComunitaria.Api'), stdio: 'pipe', windowsHide: true, env: apiEnv })
    const response = await fetch(`${process.env.HORTA_E2E_API_URL}/openapi/v1.json`)
    if (!response.ok) throw new Error('OpenAPI não disponível')
    const document = await response.json() as { paths: Record<string, Record<string, unknown>> }
    if (!document.paths['/api/v1/gardens/{gardenId}/irrigations/{id}']?.put) throw new Error('OpenAPI real incompatível com correção')
    // Filhos diretos, sem shell intermediário; mantém stdin vivo até o teardown.
    frontend = spawn(process.execPath, ['node_modules/vite/bin/vite.js', 'preview', '--port', '5183', '--strictPort'], {
      cwd: resolve(import.meta.dirname, '..'), windowsHide: true,
      env: { ...process.env, API_PROXY_TARGET: process.env.HORTA_E2E_API_URL, VITE_API_BASE_URL: '/api/v1' },
    })
    frontend.stdout?.pipe(log, { end: false }); frontend.stderr?.pipe(log, { end: false })
    frontend.once('exit', (code, signal) => log.write(`Preview encerrado: code=${code}, signal=${signal}\n`))
    let frontReady = false
    for (let attempt = 0; attempt < 100 && !frontReady; attempt++) {
      if (frontend.exitCode !== null) throw new Error('O preview não iniciou. Confira test-results/backend.log.')
      try { frontReady = (await fetch('http://127.0.0.1:5183')).ok } catch { /* aguardar listen */ }
      if (!frontReady) await new Promise(done => setTimeout(done, 100))
    }
    if (!frontReady) throw new Error('Preview não respondeu no prazo.')
    await saveAdministratorSession()
    return cleanup
  } catch (error) { await cleanup(); throw error }
}
