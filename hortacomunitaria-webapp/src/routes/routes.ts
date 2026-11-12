/**
 * Fonte única das rotas do frontend (caminhos da History API; o servidor devolve o index.html para qualquer caminho).
 *
 *   /hortas                                   lista de hortas
 *   /hortas/nova                              cadastro de horta
 *   /hortas/{horta}                           visão geral da horta
 *   /hortas/{horta}/{página}[/{id}][/{ação}]  páginas da horta (ex.: cultivo/ciclos/{id}/editar)
 *   /catalogo/{página}[/{id}][/{ação}]        catálogos globais (espécies e cultivares)
 *   /usuarios[/{id}][/editar]                 contas de acesso (somente Adm); `novo` ocupa o lugar do id
 *   /conta/senha                              troca da própria senha
 *
 * Páginas sem registros (ex.: horta/dados) aceitam a ação logo após o nome: /hortas/{horta}/horta/dados/editar.
 * Filtros e contexto de preenchimento ficam na query; `voltar` guarda o endereço para retorno.
 */

export type Area = 'visao-geral' | 'horta' | 'irrigacao' | 'ambiente' | 'cultivo' | 'producao' | 'fotos' | 'atividades' | 'ocorrencias' | 'custos'

interface PageMeta { area: Area; label: string; /** Nome de um registro da página (singular); ausente em páginas sem registros. */ item?: string; records: boolean }

const page = (area: Area, label: string, item?: string): PageMeta => ({ area, label, item, records: item !== undefined })

export const gardenPages = {
  'visao-geral': page('visao-geral', 'Panorâmica'),
  'visao-geral/agua': page('visao-geral', 'Água'),
  'visao-geral/umidade': page('visao-geral', 'Umidade'),
  'visao-geral/pegamento': page('visao-geral', 'Pegamento'),
  'visao-geral/produtividade': page('visao-geral', 'Produtividade'),
  'visao-geral/perdas': page('visao-geral', 'Perdas'),
  'visao-geral/secagem': page('visao-geral', 'Secagem'),
  'visao-geral/horas': page('visao-geral', 'Horas'),
  'visao-geral/ocorrencias': page('visao-geral', 'Ocorrências'),
  'visao-geral/participacao': page('visao-geral', 'Participação'),
  'visao-geral/custos': page('visao-geral', 'Custos'),
  'horta/mapa': page('horta', 'Mapa da horta'),
  'horta/setores': page('horta', 'Setores', 'Setor'),
  'horta/canteiros': page('horta', 'Canteiros', 'Canteiro'),
  'horta/dados': page('horta', 'Dados da horta'),
  'irrigacao': page('irrigacao', 'Irrigações', 'Irrigação'),
  'ambiente/variaveis': page('ambiente', 'Variáveis', 'Variável ambiental'),
  'ambiente/pontos': page('ambiente', 'Pontos', 'Ponto de coleta'),
  'ambiente/medicoes': page('ambiente', 'Medições', 'Medição'),
  'cultivo/origens': page('cultivo', 'Origem do material', 'Origem do material'),
  'cultivo/ciclos': page('cultivo', 'Ciclos', 'Ciclo de cultivo'),
  'cultivo/plantios': page('cultivo', 'Plantios e reposições', 'Plantio'),
  'cultivo/avaliacoes': page('cultivo', 'Avaliações', 'Avaliação'),
  'cultivo/observacoes': page('cultivo', 'Observações', 'Observação'),
  'producao/colheitas': page('producao', 'Colheitas', 'Colheita'),
  'producao/secagens': page('producao', 'Secagens', 'Secagem'),
  'fotos': page('fotos', 'Galeria'),
  'atividades/registros': page('atividades', 'Atividades', 'Atividade'),
  'atividades/participantes': page('atividades', 'Participantes', 'Participante'),
  'atividades/insumos': page('atividades', 'Insumos', 'Insumo'),
  'atividades/instituicoes': page('atividades', 'Instituições', 'Instituição'),
  'ocorrencias': page('ocorrencias', 'Ocorrências', 'Ocorrência'),
  'custos': page('custos', 'Lançamentos', 'Lançamento'),
} as const satisfies Record<string, PageMeta>

export const catalogPages = {
  'catalogo/especies': page('cultivo', 'Espécies', 'Espécie'),
  'catalogo/cultivares': page('cultivo', 'Cultivares', 'Cultivar'),
} as const satisfies Record<string, PageMeta>

export type GardenPage = keyof typeof gardenPages
export type CatalogPage = keyof typeof catalogPages
/** `hortas` é a lista/cadastro de hortas; não pertence a uma horta. */
export type PageKey = GardenPage | CatalogPage | 'hortas'

/** Ações que mudam o modo de um registro ou página. `novo` ocupa o lugar do id. */
export const routeActions = ['editar', 'configurar', 'cultivar', 'plantar', 'inativar', 'fases'] as const
export type RouteAction = typeof routeActions[number]

export type RouteScope = 'home' | 'gardens' | 'garden' | 'catalog' | 'users' | 'account' | 'not-found'

export interface AppRoute {
  scope: RouteScope
  /** Horta do endereço. Em catálogos, o App preenche com a horta de contexto. */
  garden: string
  page: PageKey | ''
  id: string
  action: RouteAction | ''
  search: URLSearchParams
}

/** Navegação lateral: grupos e áreas na ordem exibida. */
export const navigationGroups: { label: string; areas: { id: Area; label: string; pages: PageKey[] }[] }[] = [
  { label: 'Acompanhamento', areas: [
    { id: 'visao-geral', label: 'Visão geral', pages: ['visao-geral', 'visao-geral/agua', 'visao-geral/umidade', 'visao-geral/pegamento', 'visao-geral/produtividade', 'visao-geral/perdas', 'visao-geral/secagem', 'visao-geral/horas', 'visao-geral/ocorrencias', 'visao-geral/participacao', 'visao-geral/custos'] },
  ] },
  { label: 'Organização', areas: [
    { id: 'horta', label: 'Horta', pages: ['horta/setores', 'horta/canteiros', 'horta/mapa', 'horta/dados'] },
    { id: 'atividades', label: 'Atividades', pages: ['atividades/registros', 'atividades/participantes', 'atividades/insumos', 'atividades/instituicoes'] },
    { id: 'ocorrencias', label: 'Ocorrências', pages: ['ocorrencias'] },
    { id: 'custos', label: 'Custos', pages: ['custos'] },
  ] },
  { label: 'No campo', areas: [
    { id: 'irrigacao', label: 'Irrigação', pages: ['irrigacao'] },
    { id: 'ambiente', label: 'Ambiente', pages: ['ambiente/medicoes', 'ambiente/variaveis', 'ambiente/pontos'] },
    { id: 'cultivo', label: 'Cultivo', pages: ['catalogo/especies', 'catalogo/cultivares', 'cultivo/origens', 'cultivo/ciclos', 'cultivo/plantios', 'cultivo/avaliacoes', 'cultivo/observacoes'] },
    { id: 'producao', label: 'Produção', pages: ['producao/colheitas', 'producao/secagens'] },
    { id: 'fotos', label: 'Fotos', pages: ['fotos'] },
  ] },
]

const areas = navigationGroups.flatMap(group => group.areas)
/** Página aberta ao entrar em uma área (/hortas/{horta}/{área}). */
const areaDefaults: Record<Area, PageKey> = {
  'visao-geral': 'visao-geral', horta: 'horta/setores', irrigacao: 'irrigacao', ambiente: 'ambiente/medicoes', cultivo: 'cultivo/ciclos',
  producao: 'producao/colheitas', fotos: 'fotos', atividades: 'atividades/registros', ocorrencias: 'ocorrencias', custos: 'custos',
}

export function pageMeta(key: PageKey | ''): PageMeta | undefined {
  if (!key || key === 'hortas') return undefined
  return key in gardenPages ? gardenPages[key as GardenPage] : catalogPages[key as CatalogPage]
}
export function areaOf(key: PageKey | '') { const area = pageMeta(key)?.area; return area ? areas.find(item => item.id === area) : undefined }
export function areaHomePage(area: Area) { return areaDefaults[area] }
export const isCatalogPage = (key: string): key is CatalogPage => key in catalogPages
const isGardenPage = (key: string): key is GardenPage => key in gardenPages
const isAction = (value: string): value is RouteAction => (routeActions as readonly string[]).includes(value)

/** Interpreta um endereço interno (`/caminho?query`). Endereços fora do formato resultam em `not-found`. */
export function parseLocation(href: string): AppRoute {
  const [path, query = ''] = href.split('#')[0].split('?')
  const search = new URLSearchParams(query)
  const blank: AppRoute = { scope: 'home', garden: '', page: '', id: '', action: '', search }
  let segments: string[]
  try { segments = path.split('/').filter(Boolean).map(decodeURIComponent) } catch { return { ...blank, scope: 'not-found' } }
  if (segments.length === 0) return blank
  const notFound: AppRoute = { ...blank, scope: 'not-found' }
  const tail = (rest: string[], records: boolean) => {
    const [id = '', action = ''] = records ? rest : ['', ...rest]
    if (rest.length > (records ? 2 : 1) || (action && !isAction(action)) || (!records && id)) return undefined
    return { id, action: action as RouteAction | '' }
  }
  if (segments[0] === 'catalogo') {
    const key = `catalogo/${segments[1] ?? ''}`
    if (!isCatalogPage(key)) return notFound
    const rest = tail(segments.slice(2), true)
    return rest ? { ...blank, scope: 'catalog', page: key, ...rest } : notFound
  }
  if (segments[0] === 'usuarios') {
    const [id = '', action = '', ...extra] = segments.slice(1)
    return extra.length || (action && action !== 'editar') || (action && id === 'novo') ? notFound : { ...blank, scope: 'users', id, action: action as RouteAction | '' }
  }
  if (segments[0] === 'conta') return segments.length === 2 && segments[1] === 'senha' ? { ...blank, scope: 'account' } : notFound
  if (segments[0] !== 'hortas') return notFound
  if (segments.length === 1) return { ...blank, scope: 'gardens', page: 'hortas' }
  if (segments[1] === 'nova') return segments.length === 2 ? { ...blank, scope: 'gardens', page: 'hortas', id: 'nova' } : notFound
  const garden = segments[1], rest = segments.slice(2)
  if (rest.length === 0) return { ...blank, scope: 'garden', garden, page: 'visao-geral' }
  const two = `${rest[0]}/${rest[1] ?? ''}`
  let key: GardenPage | undefined, remaining: string[]
  if (rest.length > 1 && isGardenPage(two)) { key = two; remaining = rest.slice(2) }
  else if (isGardenPage(rest[0])) { key = rest[0]; remaining = rest.slice(1) }
  else if (rest.length === 1 && rest[0] in areaDefaults) { const home = areaDefaults[rest[0] as Area]; key = isGardenPage(home) ? home : undefined; remaining = [] }
  else return notFound
  if (!key) return notFound
  const parsed = tail(remaining, gardenPages[key].records)
  return parsed ? { ...blank, scope: 'garden', garden, page: key, ...parsed } : notFound
}

const encode = (value: string) => encodeURIComponent(value)
const withQuery = (path: string, query?: URLSearchParams) => `/${path}${query?.size ? `?${query}` : ''}`

/**
 * Endereço de uma página. `garden` é ignorado em catálogos e na lista de hortas.
 * Em páginas sem registros, `id` deve ficar vazio e a ação vem em `action`.
 */
export function linkTo(garden: string, key: PageKey, id = '', query?: URLSearchParams, action: RouteAction | '' = ''): string {
  const tail = [id, action].filter(Boolean).map(encode)
  if (key === 'hortas') return withQuery(['hortas', ...tail].join('/'), query)
  if (isCatalogPage(key)) return withQuery([key, ...tail].join('/'), query)
  if (!garden) return withQuery('hortas', query)
  return withQuery(['hortas', encode(garden), key, ...tail].join('/'), query)
}

/** Mesmo destino da rota atual, trocando registro, query ou ação. */
export function samePage(route: AppRoute, id = '', query?: URLSearchParams, action: RouteAction | '' = '') {
  return route.page ? linkTo(route.garden, route.page, id, query, action) : '/'
}

/** Lê `voltar` somente quando aponta para um endereço interno da aplicação. */
export function backTarget(route: AppRoute) {
  const value = route.search.get('voltar')
  // Apenas caminhos internos: "//host" seria outro site.
  return value && value.startsWith('/') && !value.startsWith('//') && parseLocation(value).scope !== 'not-found' ? value : undefined
}

/** Query com `voltar` apontando para o endereço atual, para links que devem permitir retorno. */
export function withReturn(query: Record<string, string> | URLSearchParams = {}, from = location.pathname + location.search) {
  const next = new URLSearchParams(query)
  next.set('voltar', from)
  return next
}

/** Rótulo curto de um endereço interno, usado no link de retorno. */
export function describeHref(href: string) {
  const target = parseLocation(href)
  if (target.scope === 'gardens') return 'Hortas'
  if (target.scope === 'users') return target.id ? 'Usuário' : 'Usuários'
  if (target.scope === 'account') return 'Trocar senha'
  const meta = pageMeta(target.page)
  return (target.id && meta?.item) || meta?.label || 'página anterior'
}

/** Endereços fora das hortas: contas de acesso e troca da própria senha. */
export const usersHref = (id = '', action: '' | 'editar' = '') => `/${['usuarios', id, action].filter(Boolean).map(encode).join('/')}`
export const changePasswordHref = '/conta/senha'

/**
 * Páginas de estrutura e catálogos: criar, editar, configurar e inativar são exclusivos do perfil Adm.
 * `horta/canteiros` com a ação `cultivar` registra plantio e continua disponível ao Operador.
 */
const managedPages: ReadonlySet<PageKey> = new Set<PageKey>(['hortas', 'horta/setores', 'horta/canteiros', 'horta/dados', 'ambiente/variaveis',
  'ambiente/pontos', 'atividades/participantes', 'atividades/insumos', 'atividades/instituicoes', 'catalogo/especies', 'catalogo/cultivares'])
export const isManagedPage = (key: PageKey | '') => !!key && managedPages.has(key)
/** Rota que altera estrutura ou catálogo (formulário de criação/edição/configuração). */
export const changesManagedRecord = (route: AppRoute) =>
  isManagedPage(route.page) && (route.id === 'novo' || route.id === 'nova' || (route.action !== '' && route.action !== 'cultivar'))
