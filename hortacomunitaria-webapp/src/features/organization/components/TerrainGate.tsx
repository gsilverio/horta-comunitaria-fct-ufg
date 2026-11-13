import type { ReactNode } from 'react'
import { Lock, Map as MapIcon } from 'lucide-react'
import { linkTo } from '../../../routes/routes'
import { useAuth } from '../../auth/hooks/useAuth'
import { terrainBlocks, useTerrainRequirement } from '../hooks/useTerrainRequirement'
import { OrganizationTrail } from './OrganizationLayout'

/**
 * Setores e canteiros dependem do terreno: numa horta nova (sem terreno salvo e sem setores) a página mostra o
 * caminho para o mapa em vez do cadastro. Hortas com setores anteriores ao mapa continuam acessíveis, com aviso.
 * Falha na consulta não bloqueia: a interface só orienta; o servidor continua validando os cadastros.
 */
export function TerrainGate({ garden, page, children }: { garden: string; page: 'Setores' | 'Canteiros'; children: ReactNode }) {
  const terrain = useTerrainRequirement(garden)
  const canManage = useAuth().isAdministrator
  if (terrain.loading) return <p role="status">Conferindo o terreno da horta…</p>
  if (terrainBlocks(terrain.data)) return <>
    <OrganizationTrail current={page} />
    <section className="terrain-gate" aria-labelledby="terrain-gate-title">
      <span className="terrain-gate-icon" aria-hidden="true"><MapIcon /></span>
      <h1 id="terrain-gate-title" tabIndex={-1}>Desenhe o terreno primeiro</h1>
      <p>Setores e canteiros ficam dentro do terreno da horta. Delimite o contorno no mapa e salve a configuração; depois {page === 'Setores' ? 'os setores' : 'os canteiros'} ficam disponíveis aqui.</p>
      <ol className="terrain-gate-steps" aria-label="Ordem da construção">
        <li aria-current="step"><span>1</span>Terreno</li>
        <li><Lock aria-hidden="true" />Setores</li>
        <li><Lock aria-hidden="true" />Canteiros</li>
      </ol>
      {canManage
        ? <a className="action-link" href={linkTo(garden, 'horta/mapa')}>Desenhar o terreno no mapa</a>
        : <p className="notice m-0">Somente o perfil Adm desenha o terreno. Peça a um Adm para configurar o mapa da horta.</p>}
    </section>
  </>
  return <>
    {terrain.data && !terrain.data.hasTerrain && <p className="notice" role="status">Esta horta ainda não tem terreno desenhado. {canManage ? <a href={linkTo(garden, 'horta/mapa')}>Desenhar no mapa</a> : 'Peça a um Adm para desenhá-lo no mapa.'}</p>}
    {children}
  </>
}
