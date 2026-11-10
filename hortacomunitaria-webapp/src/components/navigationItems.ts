import type { RecordRoute } from '../types/records'

export const navigationItems = [
  {
    "id": "visao-geral",
    "label": "Visão geral",
    "items": [
      {
        "id": "painel",
        "label": "Dashboard"
      },
      {
        "id": "water-consumption",
        "label": "Água"
      },
      {
        "id": "soil-moisture",
        "label": "Umidade"
      },
      {
        "id": "seedling-establishment",
        "label": "Pegamento"
      },
      {
        "id": "productivity",
        "label": "Produtividade"
      },
      {
        "id": "harvest-losses",
        "label": "Perdas"
      },
      {
        "id": "drying-yield",
        "label": "Secagem"
      },
      {
        "id": "work-hours",
        "label": "Horas"
      },
      {
        "id": "operational-occurrences",
        "label": "Ocorrências"
      },
      {
        "id": "social-educational-participation",
        "label": "Participação"
      },
      {
        "id": "basic-costs",
        "label": "Custos"
      }
    ]
  },
  {
    "id": "horta",
    "label": "Horta",
    "items": [
      {
        "id": "interativa",
        "label": "Configurar horta"
      },
      {
        "id": "hortas",
        "label": "Hortas"
      },
      {
        "id": "setores",
        "label": "Setores"
      },
      {
        "id": "canteiros",
        "label": "Canteiros"
      }
    ]
  },
  {
    "id": "irrigacao",
    "label": "Irrigação",
    "items": [
      {
        "id": "registros",
        "label": "Registros"
      }
    ]
  },
  {
    "id": "ambiente",
    "label": "Ambiente",
    "items": [
      {
        "id": "variaveis",
        "label": "Variáveis"
      },
      {
        "id": "pontos",
        "label": "Pontos"
      },
      {
        "id": "medicoes",
        "label": "Medições"
      }
    ]
  },
  {
    "id": "cultivo",
    "label": "Cultivo",
    "items": [
      {
        "id": "especies",
        "label": "Espécies"
      },
      {
        "id": "cultivares",
        "label": "Cultivares"
      },
      {
        "id": "origens",
        "label": "Origem do material"
      },
      {
        "id": "ciclos",
        "label": "Ciclos"
      },
      {
        "id": "plantios",
        "label": "Plantios e reposições"
      },
      {
        "id": "avaliacoes",
        "label": "Avaliações"
      },
      {
        "id": "observacoes",
        "label": "Observações"
      }
    ]
  },
  {
    "id": "producao",
    "label": "Produção",
    "items": [
      {
        "id": "colheitas",
        "label": "Colheitas"
      },
      {
        "id": "secagens",
        "label": "Secagens"
      }
    ]
  },
  {
    "id": "fotos",
    "label": "Fotos",
    "items": [
      {
        "id": "galeria",
        "label": "Galeria"
      }
    ]
  },
  {
    "id": "atividades",
    "label": "Atividades",
    "items": [
      {
        "id": "registros",
        "label": "Atividades"
      },
      {
        "id": "participantes",
        "label": "Participantes"
      },
      {
        "id": "insumos",
        "label": "Insumos"
      },
      {
        "id": "instituicoes",
        "label": "Instituições"
      }
    ]
  },
  {
    "id": "ocorrencias",
    "label": "Ocorrências",
    "items": [
      {
        "id": "registros",
        "label": "Ocorrências"
      }
    ]
  },
  {
    "id": "custos",
    "label": "Custos",
    "items": [
      {
        "id": "registros",
        "label": "Lançamentos"
      }
    ]
  }
]

/** Páginas de gestão de uma horta. A listagem de hortas fica no próprio item Horta. */
const gardenPages = [
  { id: 'setores', label: 'Setores' },
  { id: 'canteiros', label: 'Canteiros' },
  { id: 'interativa', label: 'Mapa da horta' },
  { id: 'hortas', label: 'Dados da horta' },
]

export function modulePages(module: string) {
  if (module === 'horta') return gardenPages
  const items = navigationItems.find(item => item.id === module)?.items ?? []
  return items.length > 1 ? items : []
}

/** Localização atual: módulo e página, para o header e leitores de tela. */
export function currentLocation(route: RecordRoute) {
  const area = navigationItems.find(item => item.id === route.module)?.label
  const isCatalog = route.module === 'horta' && route.resource === 'hortas' && !route.id
  const page = isCatalog ? 'Todas as hortas' : route.module === 'horta' && route.resource === 'hortas' && route.id === 'novo' ? 'Nova horta' : modulePages(route.module).find(item => item.id === route.resource)?.label
  return { area, page }
}
