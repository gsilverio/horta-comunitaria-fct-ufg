import { createContext, useContext } from 'react'
export interface GardenWorkspace {
  id: string
  name: string
  isActive: boolean
  institutionalAreaM2: number | null
  operationalAreaM2: number | null
  experimentalAreaM2: number | null
}

export const GardenState = createContext<GardenWorkspace>({
  id: '',
  name: '',
  isActive: true,
  institutionalAreaM2: null,
  operationalAreaM2: null,
  experimentalAreaM2: null,
})
export function useGardenState() { return useContext(GardenState) }
