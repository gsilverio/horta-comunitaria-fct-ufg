import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getCoreRowModel, useReactTable } from '@tanstack/react-table'
import type { ColumnDef, VisibilityState } from '@tanstack/react-table'
import { EllipsisVertical, RefreshCw, Settings2 } from 'lucide-react'
import { DataGrid } from '../../../components/data-grid/data-grid'
import { DataGridTable } from '../../../components/data-grid/data-grid-table'
import { DataGridColumnVisibility } from '../../../components/data-grid/data-grid-column-visibility'
import { DataGridPagination } from '../../../components/data-grid/data-grid-pagination'
import { Button } from '../../../components/ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '../../../components/ui/dropdown-menu'
import { Failure } from '../../../components/Feedback'
import { navigate, routeHref } from '../../../hooks/useRoute'
import type { Garden } from '../types/Garden'
import type { ApiFailure, Page } from '../../../types/api'

interface GardenDataGridProps {
  data?: Page<Garden>
  loading: boolean
  error?: ApiFailure
  page: number
  pageSize: number
  changePage: (page: number) => void
  changePageSize: (size: number) => void
  retry: () => void
  filter: ReactNode
  destination?: { id: string; label: string; resource: string }
}

export function GardenDataGrid({ data, loading, error, page, pageSize, changePage, changePageSize, retry, filter, destination }: GardenDataGridProps) {
  "use no memo";
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const columns = useMemo<ColumnDef<Garden>[]>(() => [
    { accessorKey: 'name', header: 'Horta', meta: { headerTitle: 'Horta' }, enableHiding: false, size: 260,
      cell: ({ row }) => <a className="font-semibold text-horta-forest hover:underline" href={routeHref(row.original.id, 'horta', 'hortas', row.original.id)}>{row.original.name}</a> },
    { accessorKey: 'location', header: 'Localização', meta: { headerTitle: 'Localização', headerClassName: 'hidden sm:table-cell', cellClassName: 'hidden sm:table-cell' }, size: 260,
      cell: ({ row }) => row.original.location || <span className="text-horta-muted">Não informada</span> },
    { accessorKey: 'isActive', header: 'Situação', meta: { headerTitle: 'Situação' }, size: 120,
      cell: ({ row }) => <span className={row.original.isActive ? 'badge' : 'badge invalid'}>{row.original.isActive ? 'Ativa' : 'Inativa'}</span> },
    { id: 'actions', header: 'Ações', meta: { headerTitle: 'Ações' }, enableHiding: false, size: 90,
      cell: ({ row }) => <div onClick={event => event.stopPropagation()}><GardenActions garden={row.original} destination={destination} /></div> },
  ], [destination])
  const rows = !loading && !error ? data?.items ?? [] : []
  const table = useReactTable({
    data: rows, columns, getRowId: row => row.id, getCoreRowModel: getCoreRowModel(),
    manualPagination: true, rowCount: data?.totalCount ?? 0, autoResetPageIndex: false,
    enableSorting: false,
    state: { pagination: { pageIndex: page - 1, pageSize }, columnVisibility },
    onColumnVisibilityChange: setColumnVisibility,
    onPaginationChange: update => {
      const next = typeof update === 'function' ? update({ pageIndex: page - 1, pageSize }) : update
      if (next.pageSize !== pageSize) changePageSize(next.pageSize)
      else changePage(next.pageIndex + 1)
    },
  })
  return <section className="garden-data-grid panel flush text-horta-ink" aria-label="Listagem de hortas" aria-busy={loading}>
    <DataGrid table={table} recordCount={data?.totalCount ?? 0} isLoading={loading} loadingMode="skeleton" emptyMessage={error ? 'Não foi possível carregar as hortas.' : 'Nenhuma horta nesta seleção. Cadastre uma horta ou altere o filtro de situação.'} onRowClick={garden => destination ? navigate(garden.id, destination.id, destination.resource) : navigate(garden.id, 'horta', 'hortas', garden.id)} tableClassNames={{ headerRow: '[&>th]:border-horta-line', bodyRow: '[&>td]:border-horta-line' }} tableLayout={{ headerBackground: true, headerBorder: true, rowBorder: true, width: 'auto' }}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-horta-line px-1 py-2">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" type="button" className="size-11" disabled={loading} aria-label="Atualizar listagem de hortas" onClick={retry}><RefreshCw className={loading ? 'animate-spin' : ''} /></Button>
          <span className="text-[13px] text-horta-muted" role="status">{loading ? 'Carregando hortas…' : error ? 'Consulta indisponível' : `${data?.totalCount ?? 0} horta(s)`}</span>
        </div>
        <div className="flex flex-wrap items-center gap-3">{filter}<DataGridColumnVisibility table={table} trigger={<Button variant="outline" className="h-11"><Settings2 />Colunas</Button>} onReset={() => table.resetColumnVisibility()} /></div>
      </div>
      {error && <div className="p-3"><Failure error={error} retry={retry} /></div>}
      <div className="max-h-[68dvh] overflow-auto" role="region" aria-label="Tabela de hortas" tabIndex={0}><DataGridTable /></div>
      <fieldset disabled={loading || !!error} className="min-w-0 border-t border-horta-line px-1 py-2"><DataGridPagination sizes={[10, 25, 50, 100]} info={data?.totalCount ? '{from}–{to} de {count} hortas' : 'Nenhuma horta encontrada'} /></fieldset>
    </DataGrid>
  </section>
}

function GardenActions({ garden, destination }: { garden: Garden; destination?: GardenDataGridProps['destination'] }) {
  const detail = routeHref(garden.id, 'horta', 'hortas', garden.id)
  return <DropdownMenu><DropdownMenuTrigger asChild><Button variant="ghost" size="icon" className="size-11" aria-label={`Ações da horta ${garden.name}`}><EllipsisVertical /></Button></DropdownMenuTrigger>
    <DropdownMenuContent align="end" className="w-52">
      {destination && <DropdownMenuItem asChild><a href={routeHref(garden.id, destination.id, destination.resource)}>Abrir {destination.label}</a></DropdownMenuItem>}
      <DropdownMenuItem asChild><a href={detail}>Dados e histórico</a></DropdownMenuItem>
      {garden.isActive && <DropdownMenuItem asChild><a href={routeHref(garden.id, 'horta', 'hortas', garden.id, new URLSearchParams({ edit: '1' }))}>Editar horta</a></DropdownMenuItem>}
      <DropdownMenuItem asChild><a href={routeHref(garden.id, 'horta', 'interativa')}>Configurar desenho</a></DropdownMenuItem>
      <DropdownMenuItem asChild><a href={routeHref(garden.id, 'horta', 'setores')}>Ver setores</a></DropdownMenuItem>
      {garden.isActive && <><DropdownMenuSeparator /><DropdownMenuItem asChild className="text-horta-danger"><a href={routeHref(garden.id, 'horta', 'hortas', garden.id, new URLSearchParams({ inactivate: '1' }))}>Inativar horta</a></DropdownMenuItem></>}
    </DropdownMenuContent>
  </DropdownMenu>
}
