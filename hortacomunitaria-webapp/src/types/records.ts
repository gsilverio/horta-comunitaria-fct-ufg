import type { ReactNode } from 'react'
export interface CatalogState { id: string; isActive: boolean; version: number; updatedAt: string | null }
export interface FactState { id: string; gardenId: string; operationId: string; validity: 'Valid' | 'Invalid'; version: number; recordedByUserId: string; recordedAt: string; updatedAt: string | null }
export interface AuditEntry { id: string; operation: string; reason: string | null; userId: string; occurredAt: string; before: string | null; after: string }
export interface Choice { id: string; label: string; isActive?: boolean; disabled?: boolean }
export interface DetailItem { label: string; value: ReactNode }
export interface RecordRoute { garden: string; gardenSpecified: boolean; module: string; resource: string; id: string; search: URLSearchParams }
