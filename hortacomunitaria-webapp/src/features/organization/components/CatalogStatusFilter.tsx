export function CatalogStatusFilter({ value, change }: { value: string; change: (value: string) => void }) {
  return <label className="flex-row items-center gap-3">Mostrar<select className="min-h-10 w-auto py-2" value={value} onChange={event => change(event.target.value)}><option value="active">Ativos</option><option value="inactive">Inativos</option><option value="all">Todos</option></select></label>
}
