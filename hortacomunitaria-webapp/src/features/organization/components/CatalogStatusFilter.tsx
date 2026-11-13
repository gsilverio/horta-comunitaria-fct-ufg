const choices = [{ id: 'active', label: 'Ativos' }, { id: 'inactive', label: 'Inativos' }, { id: 'all', label: 'Todos' }]

/** Filtro de situação em botões segmentados: as três opções ficam visíveis, sem abrir lista. */
export function CatalogStatusFilter({ value, change }: { value: string; change: (value: string) => void }) {
  return <div className="segmented" role="group" aria-label="Mostrar">
    {choices.map(choice => <button key={choice.id} type="button" aria-pressed={value === choice.id} onClick={() => { if (value !== choice.id) change(choice.id) }}>{choice.label}</button>)}
  </div>
}
