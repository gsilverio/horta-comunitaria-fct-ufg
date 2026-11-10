/** Lado maior após a redução; suficiente para leitura de campo e muito mais leve que a foto original do celular. */
const maxSide = 2048
const reduceAbove = 1.5 * 1024 * 1024

/**
 * Reduz fotos grandes antes do envio (JPEG 85%), preservando a orientação. Se o navegador não conseguir decodificar,
 * envia o arquivo original e a API decide pelo tipo/tamanho. Não altera imagens pequenas.
 */
export async function prepareImage(file: File): Promise<{ blob: Blob; fileName: string; reduced: boolean }> {
  if (file.size <= reduceAbove || typeof createImageBitmap !== 'function') return { blob: file, fileName: file.name, reduced: false }
  try {
    const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
    const scale = Math.min(1, maxSide / Math.max(bitmap.width, bitmap.height))
    const canvas = document.createElement('canvas')
    canvas.width = Math.round(bitmap.width * scale)
    canvas.height = Math.round(bitmap.height * scale)
    const context = canvas.getContext('2d')
    if (!context) { bitmap.close(); return { blob: file, fileName: file.name, reduced: false } }
    context.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    bitmap.close()
    const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/jpeg', 0.85))
    if (!blob || blob.size >= file.size) return { blob: file, fileName: file.name, reduced: false }
    return { blob, fileName: file.name.replace(/\.[^.]+$/, '') + '.jpg', reduced: true }
  } catch {
    return { blob: file, fileName: file.name, reduced: false }
  }
}
