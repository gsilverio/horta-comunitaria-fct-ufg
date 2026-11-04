import { useState } from 'react'
import { EditorFrame } from '../../../components/EditorFrame'
import { Field, Facts } from '../../../components/Fields'
import { useEditor } from '../../../hooks/useEditor'
import { FormFailure } from '../../irrigation/formValues'
import type { VisualGrowthCatalog, VisualGrowthValues } from '../types/VisualGrowth'

export function VisualGrowthForm<T extends VisualGrowthCatalog>({ original, correct, reload, saved, cancel }: {
  original: T
  correct: (values: VisualGrowthValues, expectedVersion: number, reason: string) => Promise<T>
  reload: () => Promise<T>
  saved: (record: T) => void
  cancel: () => void
}) {
  const [growth, setGrowth] = useState(original.visualGrowthDays?.toString() ?? '')
  const [harvest, setHarvest] = useState(original.visualHarvestDays?.toString() ?? '')
  const editor = useEditor<T, VisualGrowthValues>(original, () => { throw new Error('Selecione um cadastro existente.') }, correct, reload, saved, false)
  function values(): VisualGrowthValues {
    if (!growth.trim() && !harvest.trim()) return { visualGrowthDays: null, visualHarvestDays: null }
    const first = Number(growth), second = Number(harvest)
    if (!/^\d+$/.test(growth) || !/^\d+$/.test(harvest) || !Number.isSafeInteger(first) || !Number.isSafeInteger(second) || first <= 0 || second <= first || second > 2147483647)
      throw new FormFailure({ visualGrowthDays: ['Informe dois números inteiros: crescimento maior que zero e colheita posterior.'] })
    return { visualGrowthDays: first, visualHarvestDays: second }
  }
  return <EditorFrame title="Configurar fases visuais" requireActiveGarden={false}
    description="Dias contados a partir do plantio. São previsões ilustrativas, não confirmação de maturidade. A configuração da cultivar prevalece sobre a espécie; ambos vazios removem esta configuração."
    submitLabel="Salvar prazos visuais" busy={editor.busy} uncertain={false} error={editor.error}
    reason={editor.reason} changeReason={editor.setReason} submit={() => void editor.submit(values)} cancel={cancel}
    conflict={() => void editor.reloadConflict()} acceptReview={editor.acceptReview}
    review={editor.review && <Facts items={[{ label: 'Versão atual', value: editor.review.version }, { label: 'Dias para crescimento', value: editor.review.visualGrowthDays }, { label: 'Dias para colheita prevista', value: editor.review.visualHarvestDays }]} />}>
    <div className="grid gap-4 sm:grid-cols-2">
      <Field name="visualGrowthDays" label="Dias para crescimento intermediário" type="integer" value={growth} change={setGrowth} error={editor.error} />
      <Field name="visualHarvestDays" label="Dias para colheita prevista" type="integer" value={harvest} change={setHarvest} error={editor.error} />
    </div>
  </EditorFrame>
}
