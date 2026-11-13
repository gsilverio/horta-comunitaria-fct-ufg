import { linkTo } from '../../../routes/routes'
import { goTo } from '../../../routes/navigation'
import { useMemo } from 'react'
import type { ReactNode } from 'react'
import { getCoreRowModel, useReactTable } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import { DataGrid } from '../../../components/data-grid/data-grid'
import { DataGridTable } from '../../../components/data-grid/data-grid-table'
import { DataGridColumnHeader } from '../../../components/data-grid/data-grid-column-header'
import { DataGridPagination } from '../../../components/data-grid/data-grid-pagination'
import { Failure } from '../../../components/Feedback'
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
  /** Ação principal da barra (ex.: Nova horta), à esquerda, como nas listagens Metronic. */
  action?: ReactNode
  filter: ReactNode
}

function StatusPill({ active }: { active: boolean }) {
  return <span className={`garden-status-pill ${active ? 'is-active' : 'is-inactive'}`}><span className="garden-status-dot" aria-hidden="true" />{active ? 'Ativa' : 'Inativa'}</span>
}

export function GardenDataGrid({ data, loading, error, page, pageSize, changePage, changePageSize, retry, action, filter }: GardenDataGridProps) {
  "use no memo";
  // Listagem enxuta: nome e situação. A linha inteira abre a horta; o nome continua sendo link para o teclado.
  const columns = useMemo<ColumnDef<Garden>[]>(() => [
    { accessorKey: 'name', size: 520, minSize: 200, meta: { headerTitle: 'Horta' },
      header: ({ column }) => <DataGridColumnHeader title="Horta" column={column} />,
      cell: ({ row }) => <div className="flex min-w-0 flex-col items-center py-0.5 text-center">
        <a className="truncate font-semibold text-horta-forest hover:underline" href={linkTo(row.original.id, 'visao-geral')} onClick={event => event.stopPropagation()}>{row.original.name}</a>
        <span className="truncate text-xs text-horta-muted">{row.original.location || 'Localização não informada'}</span>
      </div> },
    { accessorKey: 'isActive', size: 140, minSize: 110, meta: { headerTitle: 'Situação' },
      header: ({ column }) => <DataGridColumnHeader title="Situação" column={column} />,
      cell: ({ row }) => <StatusPill active={row.original.isActive} /> },
  ], [])
  const rows = !loading && !error ? data?.items ?? [] : []
  const table = useReactTable({
    data: rows, columns, getRowId: row => row.id, getCoreRowModel: getCoreRowModel(),
    manualPagination: true, rowCount: data?.totalCount ?? 0, autoResetPageIndex: false,
    // A API não ordena nem busca neste catálogo: ordenar a página atual sugeriria um resultado que não abrange todas as hortas.
    enableSorting: false,
    state: { pagination: { pageIndex: page - 1, pageSize } },
    onPaginationChange: update => {
      const next = typeof update === 'function' ? update({ pageIndex: page - 1, pageSize }) : update
      if (next.pageSize !== pageSize) changePageSize(next.pageSize)
      else changePage(next.pageIndex + 1)
    },
  })
  const total = data?.totalCount ?? 0
  return <section className="garden-data-grid panel flush overflow-hidden text-horta-ink" aria-label="Listagem de hortas" aria-busy={loading}>
    <DataGrid table={table} recordCount={total} isLoading={loading} loadingMode="skeleton"
      emptyMessage={error ? 'Não foi possível carregar as hortas.' : 'Nenhuma horta nesta seleção. Cadastre uma horta ou altere o filtro de situação.'}
      onRowClick={garden => goTo(garden.id, 'visao-geral')}
      tableClassNames={{ headerRow: 'garden-grid-head', bodyRow: 'garden-grid-row' }}
      tableLayout={{ dense: true, headerBackground: true, headerBorder: true, rowBorder: true, cellBorder: true, stripped: true, headerSticky: true, width: 'fixed' }}>
      <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
        <div className="flex items-center gap-3">{action}</div>
        <div className="flex flex-wrap items-center gap-3">{filter}</div>
      </div>
      {error && <div className="p-3"><Failure error={error} retry={retry} /></div>}
      <div className="max-h-[62dvh] min-w-0 overflow-auto" role="region" aria-label="Tabela de hortas" tabIndex={0}><DataGridTable /></div>
      <fieldset disabled={loading || !!error} className="min-w-0 border-t border-horta-line px-4 py-2"><DataGridPagination sizes={[10, 25, 50, 100]} info={total ? '{from}–{to} de {count} hortas' : 'Nenhuma horta encontrada'} /></fieldset>
    </DataGrid>
  </section>
}
