/** Classificação estética; não é recomendação de espaçamento ou indicador agronômico. */
export function visualDensity(quantity: number | null, occupiedAreaM2: number): 'Baixa' | 'Média' | 'Alta' | undefined {
  if (quantity === null || !Number.isFinite(quantity) || quantity <= 0 || !Number.isFinite(occupiedAreaM2) || occupiedAreaM2 <= 0) return undefined
  const ratio = quantity / occupiedAreaM2
  return ratio <= 4 ? 'Baixa' : ratio <= 12 ? 'Média' : 'Alta'
}
