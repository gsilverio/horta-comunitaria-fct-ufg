import type { ReactNode } from 'react'
import { Hand, Maximize, MousePointer2, MoreHorizontal, Redo2, SlidersHorizontal, Undo2, ZoomIn, ZoomOut } from 'lucide-react'
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '../../../components/ui/dropdown-menu'

export interface ViewOptions { grid: boolean; snap: boolean; rotationSnap: boolean; alignment: boolean }

interface Props {
  tool: 'select' | 'pan' | 'other'
  selectTool: (tool: 'select' | 'pan') => void
  zoom: number
  zoomIn: () => void
  zoomOut: () => void
  fit: () => void
  canUndo: boolean
  canRedo: boolean
  undo: () => void
  redo: () => void
  canReset: boolean
  reset: () => void
  options: ViewOptions
  changeOption: (option: keyof ViewOptions, value: boolean) => void
  disabled?: boolean
}

function IconButton({ label, shortcut, pressed, disabled, onClick, children }: { label: string; shortcut?: string; pressed?: boolean; disabled?: boolean; onClick: () => void; children: ReactNode }) {
  return <button type="button" title={shortcut ? `${label} (${shortcut})` : label} aria-label={label} aria-pressed={pressed} aria-keyshortcuts={shortcut} disabled={disabled} onClick={onClick}
    className={`interactive-icon-button${pressed ? ' active' : ''}`}>{children}</button>
}

const optionLabels: { id: keyof ViewOptions; label: string; hint: string }[] = [
  { id: 'grid', label: 'Mostrar grade', hint: 'Linhas a cada unidade de desenho' },
  { id: 'snap', label: 'Encaixar na grade', hint: 'Posições em passos de 0,5 u' },
  { id: 'rotationSnap', label: 'Travar rotação em 90°', hint: 'Alinha perto de 0° e 90°' },
  { id: 'alignment', label: 'Guias de alinhamento', hint: 'Mostra guias ao mover' },
]

/** Barra do mapa: modo do cursor, histórico e câmera à esquerda; preferências de desenho num menu. */
export function CanvasToolbar(props: Props) {
  const { tool, selectTool, zoom, zoomIn, zoomOut, fit, canUndo, canRedo, undo, redo, canReset, reset, options, changeOption, disabled } = props
  return <div className="interactive-canvas-toolbar" role="toolbar" aria-label="Ferramentas do mapa" inert={disabled}>
    <div className="interactive-toolbar-group" role="group" aria-label="Cursor">
      <IconButton label="Selecionar e mover" shortcut="V" pressed={tool === 'select'} onClick={() => selectTool('select')}><MousePointer2 aria-hidden="true" /></IconButton>
      <IconButton label="Mover a vista" shortcut="H" pressed={tool === 'pan'} onClick={() => selectTool('pan')}><Hand aria-hidden="true" /></IconButton>
    </div>
    <div className="interactive-toolbar-group" role="group" aria-label="Histórico">
      <IconButton label="Desfazer" shortcut="Ctrl+Z" disabled={!canUndo} onClick={undo}><Undo2 aria-hidden="true" /></IconButton>
      <IconButton label="Refazer" shortcut="Ctrl+Shift+Z" disabled={!canRedo} onClick={redo}><Redo2 aria-hidden="true" /></IconButton>
    </div>
    <div className="interactive-toolbar-group ml-auto" role="group" aria-label="Zoom">
      <IconButton label="Afastar" shortcut="-" onClick={zoomOut}><ZoomOut aria-hidden="true" /></IconButton>
      <button type="button" className="interactive-zoom-value" title="Ajustar à tela (F)" aria-label={`Zoom ${Math.round(zoom * 100)}%. Ajustar à tela`} onClick={fit}><span aria-live="polite">{Math.round(zoom * 100)}%</span></button>
      <IconButton label="Aproximar" shortcut="+" onClick={zoomIn}><ZoomIn aria-hidden="true" /></IconButton>
      <IconButton label="Ajustar à tela" shortcut="F" onClick={fit}><Maximize aria-hidden="true" /></IconButton>
    </div>
    <div className="interactive-toolbar-group" role="group" aria-label="Preferências">
      <DropdownMenu>
        <DropdownMenuTrigger asChild><button type="button" className="interactive-icon-button" title="Preferências de desenho" aria-label="Preferências de desenho"><SlidersHorizontal aria-hidden="true" /></button></DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-64">
          <DropdownMenuLabel>Desenho</DropdownMenuLabel>
          {optionLabels.map(option => <DropdownMenuCheckboxItem key={option.id} checked={options[option.id]} onCheckedChange={value => changeOption(option.id, value === true)} onSelect={event => event.preventDefault()}>
            <span className="flex flex-col"><span>{option.label}</span><span className="text-[11px] text-horta-muted">{option.hint}</span></span>
          </DropdownMenuCheckboxItem>)}
        </DropdownMenuContent>
      </DropdownMenu>
      <DropdownMenu>
        <DropdownMenuTrigger asChild><button type="button" className="interactive-icon-button" title="Mais ações" aria-label="Mais ações"><MoreHorizontal aria-hidden="true" /></button></DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-64">
          <DropdownMenuItem disabled={!canReset} onSelect={reset}>
            <span className="flex flex-col"><span>Descartar alterações desta sessão</span><span className="text-[11px] text-horta-muted">Volta ao mapa como estava ao abrir</span></span>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuLabel className="text-[11px] font-normal text-horta-muted">Atalhos: V selecionar · H mover vista · Espaço + arraste move a vista · Delete remove · Esc cancela a ferramenta</DropdownMenuLabel>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  </div>
}
