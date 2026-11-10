import { useState } from 'react'
import { EditorFrame } from '../../../components/EditorFrame'
import { Field, SelectField } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import { useLookups } from '../../../hooks/useLookups'
import { requiredNumber } from '../../../services/values'
import { today } from '../../irrigation/formValues'
import { useBedSetup } from '../hooks/useBedSetup'
import type { Bed } from '../types/Bed'

export function BedSetupForm({ garden, bed, sectorId, saved, cancel }: { garden: string; bed: Bed; sectorId: string; saved: (record: Bed) => void; cancel: () => void }) {
  const [area, setArea] = useState(bed.configuration ? String(bed.configuration.cultivableAreaM2) : '')
  const [sector, setSector] = useState(bed.sectorAssignment?.sectorId ?? sectorId)
  const [validFrom, setValidFrom] = useState(today())
  const options = useLookups(garden, ['irrigationSector'])
  const command = useBedSetup(garden, bed, saved)
  return <><ol className="organization-path" aria-label="Etapas do cadastro"><li>1. Identificação salva</li><li aria-current="step" className="font-semibold text-horta-forest">2. Área e setor</li></ol>
    <EditorFrame title={`Concluir cadastro de ${bed.name}`} description="Informe a área e confirme a qual setor o canteiro pertence." submitLabel={command.pending ? 'Retomar conclusão' : 'Concluir cadastro'} busy={command.busy} uncertain={false} dependenciesReady={!options.loading && !options.error && !command.conflict} error={command.error} submit={() => void command.submit(() => ({ area: requiredNumber(area, 'cultivableAreaM2'), sectorId: sector, validFrom }))} cancel={cancel}>
      {command.pending && <p role="status" className="notice">Parte do cadastro pode já estar salva. Retome a conclusão para conferir e salvar somente o que falta.</p>}
      {command.conflict && <p className="notice">Use Cancelar para voltar ao canteiro e revisar o estado atual antes de continuar.</p>}
      {options.error && <Failure error={options.error} retry={options.retry} />}
      {options.loading && <p role="status">Carregando setores…</p>}
      <fieldset disabled={command.pending || command.conflict} className="grid gap-4 sm:grid-cols-2">
        <Field name="cultivableAreaM2" label="Área cultivável (m²)" required type="decimal" value={area} change={setArea} error={command.error} hint="Informe a área disponível para cultivo, por exemplo 5,5." />
        <SelectField name="sectorId" label="Setor" required value={sector} change={setSector} options={options.data?.irrigationSector ?? []} error={command.error} />
        <Field name="validFrom" label="Válido a partir de" required type="date" value={validFrom} change={setValidFrom} error={command.error} hint="Data a partir da qual esta área e este setor passam a valer." />
      </fieldset>
    </EditorFrame>
  </>
}
