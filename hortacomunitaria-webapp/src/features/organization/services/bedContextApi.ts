import { http } from '../../../services/http'
import type { Page } from '../../../types/api'
import type { BedAssignment, BedConfiguration } from '../types/Bed'
const path = (garden: string, bed: string) => `/gardens/${encodeURIComponent(garden)}/beds/${encodeURIComponent(bed)}`
export const bedContextApi = {
  configurations: async (garden: string, bed: string, page: number, signal: AbortSignal) => (await http.get<Page<BedConfiguration>>(`${path(garden, bed)}/configurations`, { signal, params: { page, pageSize: 5 } })).data,
  assignments: async (garden: string, bed: string, page: number, signal: AbortSignal) => (await http.get<Page<BedAssignment>>(`${path(garden, bed)}/sector-assignments`, { signal, params: { page, pageSize: 5 } })).data,
  configure: async (garden: string, bed: string, values: { cultivableAreaM2: number; lengthMeters: number | null; widthMeters: number | null; validFrom: string; expectedBedVersion: number; reason: string | null }) => (await http.post<{ context: BedConfiguration; bedVersion: number }>(`${path(garden, bed)}/configurations`, values)).data,
  assign: async (garden: string, bed: string, values: { sectorId: string; validFrom: string; expectedBedVersion: number; reason: string | null }) => (await http.post<{ context: BedAssignment; bedVersion: number }>(`${path(garden, bed)}/sector-assignments`, values)).data,
  on: async (garden: string, bed: string, on: string, signal: AbortSignal) => (await http.get<import('../types/Bed').Bed>(path(garden, bed), { signal, params: { on } })).data,
}
