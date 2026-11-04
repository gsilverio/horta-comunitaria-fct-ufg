export interface VisualGrowthValues {
  visualGrowthDays: number | null
  visualHarvestDays: number | null
}
export interface VisualGrowthCatalog extends VisualGrowthValues {
  id: string
  version: number
}
