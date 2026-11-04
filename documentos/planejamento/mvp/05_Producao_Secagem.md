# Fluxo 05 — Colheita, perdas e secagem

**Requisitos:** RF09/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [produção e secagem](../../arquitetura/Modelo_Cultivo_Producao.md), [dados dos indicadores](../../arquitetura/Dados_Indicadores.md), arquitetura integral. **Dependência:** ciclos do fluxo 04.

## Resultado e percurso

Registrar colheita → informar massa aproveitável quando medida → consultar produção/perdas → destinar massa a lote de secagem → registrar massa seca conhecida → consultar rendimento, preservando origens e correções.

## Dados e regras

- Harvest: cycleId, harvestedOn, freshMassKg >= 0, usableMassKg? entre 0 e freshMassKg, lossReason?, responsibleParticipantId?. Massas em kg, sem conversão implícita de gramas.
- DryingLot: code único por horta, method?, startedOn, endedOn?, status InProgress/Completed, totalDryMassKg?, responsibleParticipantId?, items. Fim não precede início, massa seca conhecida não negativa.
- DryingItem: harvestId, allocatedMassKg > 0; um item por lote/colheita, sem duplicados. Válido por meio do lote pai. Não introduzir pesagem atribuída/rateio por origem no MVP: o RF09 exige origem e massa destinada, não motor de distribuição de massa seca.
- Colheita e lote são da horta; destinação não precede a colheita. Colheita ligada a ciclo válido. Uma colheita pode alimentar vários lotes.
- Hipótese conservadora do protótipo: só destinar quando usableMassKg é conhecido; soma das destinações em lotes válidos <= usableMassKg. Se desconhecido, manter coleta pendente e solicitar pesagem, sem presumir que toda massa fresca está disponível.
- Destinações/correções/invalidações de lote e correções da colheita bloqueiam as mesmas linhas Harvest, em ordem de ID, em transação curta. Recalcular saldo incluindo estado proposto antes de confirmar. Redução da colheita abaixo do já destinado e invalidação com lote dependente válido dão 409 record.has_dependents.

## Contratos e slices

Prefixo `/api/v1/gardens/{gardenId}`. POST/GET `/harvests`; GET/PUT `/harvests/{id}`, invalidação/histórico. POST/GET `/drying-lots`; GET/PUT `/drying-lots/{id}`, invalidação/histórico. Correção do lote envia representação completa com items e versão/motivo, sem endpoint de exclusão de item sem história. Finalização: POST `/drying-lots/{id}/completion` com endedOn, totalDryMassKg, expectedVersion, reason; se peso final não foi medido, manter pendência/InProgress, sem inventar zero.

Filtros de colheita: from/to por harvestedOn, cycleId?, speciesId?, validade. Lote: from/to por startedOn, status?, validade. Detalhe mostra massa destinada por origem, saldo conhecido da colheita e estado do lote.

Slices Cultivation: RegisterHarvest/ListHarvests/GetHarvest/CorrectHarvest/InvalidateHarvest/ListHarvestHistory; CreateDryingLot/ListDryingLots/GetDryingLot/CorrectDryingLot/CompleteDryingLot/InvalidateDryingLot/ListDryingLotHistory. Indicators: GetProductivity/GetHarvestLosses/GetDryingYield.

## Indicadores propostos

| Rota após o prefixo | Definição e cobertura |
| --- | --- |
| `/indicators/productivity` | from/to pela colheita, cycleId?/speciesId?. Massa fresca conhecida por espécie/ciclo. Para cada ciclo, soma fresca/área ocupada do ciclo; área usada uma vez, mesmo com várias colheitas. Não calcular produtividade global usando soma de áreas físicas reutilizadas |
| `/indicators/harvest-losses` | Mesmo período/filtros. Perda kg = fresca − aproveitável quando ambas conhecidas; % = soma perdas/soma fresca dos eventos elegíveis, com denominador > 0. Eventos sem aproveitável geram cobertura parcial, não perda zero |
| `/indicators/drying-yield` | from/to pelo início do lote, lotId?. Lotes Completed válidos com total seco e massa destinada > 0: total seco/soma destinada × 100. Incompletos identificados. Consulta por espécie só oferece percentual quando todas as origens são dessa espécie; lote misto sem atribuição não tem massa seca por espécie |

Cada consulta retorna Page de origens/resultados e resumo/cobertura aplicáveis conforme contrato comum. Não somar massa fresca junto com seca como um total de produção. Se filtrado por espécie, lote misto aparece como origem não atribuível, sem inventar proporção.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 05.1 — Colheita | Registro/gestão/histórico, formulário e listagem | Massa fresca/aproveitável distintas e desconhecido preservado |
| 05.2 — Produção e perdas | Dois indicadores e UI com origem | 3 + 2 kg em ciclo de 5 m² = 1 kg/m²; 10/8 kg = 2 kg e 20% |
| 05.3 — Destinação e secagem | Lotes/itens, saldo transacional, gestão/histórico e UI | Destinações concorrentes respeitam saldo; lote mantém todas as origens |
| 05.4 — Rendimento | Indicador de lote, filtros e UI | 5 kg destinados e 1 kg seco = 20%; espécie em lote misto indisponível |

## Cenários de aceite

1. Duas colheitas do mesmo ciclo não duplicam a área; ausência de colheita é NoData. Aproveitável null torna perda indisponível/parcial.
2. Colheita com 4 kg aproveitáveis: duas destinações concorrentes de 3 kg não confirmam 6 kg. Uma retorna conflito de saldo, sem lote/item/histórico parcial.
3. Lote com origens 3 + 2 kg e massa seca 1 kg dá 20%; não atribui 0,6/0,4 kg automaticamente.
4. Corrigir/inutilizar lote atualiza saldo e rendimento; reduzir massa da colheita abaixo do saldo utilizado é rejeitado. Repetições não duplicam destinação.
5. Destinação de outra horta, massa negativa e datas incoerentes são rejeitadas. Aplicar verificações comuns e conferir percurso completo na UI.

**Validar com a PO:** base de produtividade/perda, necessidade de pesagem aproveitável antes de destinar, data de competência e conclusão da secagem. **Evidências:** pendentes.
