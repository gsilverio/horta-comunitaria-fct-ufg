import { readRoute, replaceQuery } from '../routes/navigation'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from './ui/tabs'

export interface RecordTab { id: string; label: string; content: ReactNode }

/** Abas de um mesmo registro. A aba fica no endereço e os painéis permanecem montados para preservar rascunhos. */
export function RecordTabs({ tabs, label, initial }: { tabs: RecordTab[]; label: string; initial?: string }) {
  const [active, setActive] = useState(() => {
    const requested = readRoute().search.get('tab') ?? initial
    return tabs.some(tab => tab.id === requested) ? requested as string : tabs[0].id
  })
  function select(id: string) {
    setActive(id)
    replaceQuery(query => query.set('tab', id))
  }
  return <Tabs value={tabs.some(tab => tab.id === active) ? active : tabs[0].id} onValueChange={select}>
    <TabsList variant="line" aria-label={label} className="record-tabs-list">
      {tabs.map(tab => <TabsTrigger key={tab.id} value={tab.id} className="record-tab">{tab.label}</TabsTrigger>)}
    </TabsList>
    {tabs.map(tab => <TabsContent key={tab.id} value={tab.id} forceMount tabIndex={-1} className="record-tab-panel">{tab.content}</TabsContent>)}
  </Tabs>
}
