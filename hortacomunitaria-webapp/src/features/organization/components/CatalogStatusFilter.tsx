export function CatalogStatusFilter({ value, change }: { value: string; change: (value: string) => void }) {
  return <label className="flex-row items-center gap-2 text-xs text-horta-muted">Mostrar<select className="w-auto" value={value} onChange={event => change(event.target.value)}><option value="active">Ativos</option><option value="inactive">Inativos</option><option value="all">Todos</option></select></label>
}
