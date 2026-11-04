import { cultivationCycleApi } from './CultivationCycleApi'
import { plantingApi } from './PlantingApi'
import { speciesApi } from './SpeciesApi'

export const bedCultivationApi = {
  async list(garden: string, bedId: string, page: number, signal: AbortSignal) {
    const data = await cultivationCycleApi.list(garden, { bedId, page, pageSize: 5, from: '0001-01-01', to: '9998-12-31', validity: 'Valid' }, signal)
    const items = await Promise.all(data.items.map(async cycle => {
      const [species, plantings] = await Promise.all([
        speciesApi.get(cycle.speciesId, signal),
        plantingApi.list(garden, { cycleId: cycle.id, page: 1, pageSize: 3, from: '0001-01-01', to: '9998-12-31', validity: 'Valid' }, signal),
      ])
      return { ...cycle, speciesName: species.commonName, plantings }
    }))
    return { ...data, items }
  },
  async plantingContext(garden: string, bedId: string, cycleId: string, signal: AbortSignal) {
    const cycle = await cultivationCycleApi.get(garden, cycleId, signal)
    if (cycle.bedId !== bedId) throw new Error('Este cultivo pertence a outro canteiro. Volte e selecione um cultivo deste canteiro.')
    const species = await speciesApi.get(cycle.speciesId, signal)
    return { cycle, speciesName: species.commonName }
  },
}
