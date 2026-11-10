const paths: Record<string, string> = {
  'visao-geral': 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
  horta: 'M3 9l9-6 9 6v12H3z M8 21v-8h8v8 M3 9h18',
  irrigacao: 'M12 3C9 7 5 11 5 15a7 7 0 0014 0c0-4-4-8-7-12z M8 15a4 4 0 004 4',
  ambiente: 'M9 14V5a3 3 0 016 0v9a5 5 0 11-6 0z M12 8v10 M18 6h3 M18 10h3',
  cultivo: 'M12 21V11 M12 15C5 15 3 10 4 5c5 0 8 4 8 10z M12 11c0-5 4-8 8-8 1 5-2 8-8 8z',
  producao: 'M3 8l9-5 9 5-9 5z M3 8v10l9 4 9-4V8 M12 13v9 M7 5l10 6',
  atividades: 'M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2 M9 11a4 4 0 100-8 4 4 0 000 8 M17 4a4 4 0 010 7 M22 21v-2a4 4 0 00-3-4',
  ocorrencias: 'M12 3L2 21h20z M12 9v5 M12 17v1',
  fotos: 'M4 7h3l2-3h6l2 3h3v13H4z M12 17a4 4 0 100-8 4 4 0 000 8',
  custos: 'M12 2v20 M17 6H9a3 3 0 000 6h6a3 3 0 010 6H6',
  chevron: 'M9 5l7 7-7 7', menu: 'M4 6h16 M4 12h16 M4 18h16', close: 'M6 6l12 12 M18 6L6 18',
  plus: 'M12 5v14 M5 12h14', filter: 'M3 5h18 M6 12h12 M9 19h6',
  arrow: 'M5 12h14 M13 6l6 6-6 6', records: 'M5 3h14v18H5z M9 7h6 M9 12h6 M9 17h4',
}
export function Icon({ name, className = '' }: { name: string; className?: string }) {
  return <svg className={`ui-icon ${className}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name] ?? paths.records} /></svg>
}
