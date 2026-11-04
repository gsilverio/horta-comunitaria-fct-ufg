# Fluxo 09 — Custos previstos e realizados

**Requisitos:** RF16/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [modelo de custos](../../arquitetura/Modelo_Custos.md), arquitetura integral. **Dependência:** horta; ciclo/canteiro só quando o lançamento os identifica.

## Resultado e percurso

Registrar lançamento previsto ou realizado → consultar por período/categoria/contexto → comparar naturezas → corrigir/inutilizar com motivo e histórico.

## Dados e regras

FinancialEntry: entryDate, description obrigatória, category texto obrigatório, amountBrl decimal >= 0 (duas casas), nature Planned/Actual, bedId? ou cycleId? (no máximo um). Sem contexto é custo geral da horta. Ciclo deriva canteiro e espécie; vínculo de outra horta é rejeitado.

Valor desconhecido não é lançamento zero: não confirmar enquanto não houver valor, mantendo apenas o rascunho na interface. Realizado pode existir sem previsão. Não exigir pareamento por pagamento, gerar despesa a partir de insumo ou aplicar rateio de custos gerais.

## Contratos e slices

Prefixo `/api/v1/gardens/{gardenId}`: POST/GET `/financial-entries`, GET/PUT `/{id}`, invalidação/histórico. Lista filtra from/to por entryDate, category?, nature?, bedId? ou cycleId? e validade.

Slices Costs: RegisterFinancialEntry/ListFinancialEntries/GetFinancialEntry/CorrectFinancialEntry/InvalidateFinancialEntry/ListFinancialEntryHistory. Indicators: GetBasicCosts, GET `.../indicators/basic-costs?from=...&to=...&category=...&bedId=...&cycleId=...`.

Resumo retorna plannedBrl, actualBrl, differenceBrl = actual − planned, quantidades por natureza e Page de origens. Ausência em uma natureza é null/NoData para aquela natureza; para a diferença aritmética do **recorte registrado** pode usar soma vazia 0, mas rotular “comparação sem registros de previsão/realização”, sem afirmar custo observado zero. Com nenhum lançamento, diferença null/NoData. Zero informado é valor conhecido.

Agrupar categoria com o mesmo critério normalizado do registro (trim), sem fundir grafias diferentes por inferência. Filtrar contexto exatamente como informado; não distribuir custo geral em todos os ciclos. Recorte por espécie só usa lançamentos explicitamente vinculados ao ciclo e identifica os custos gerais excluídos.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 09.1 — Lançamento | Entidade/migration/slices, auditoria, formulário e listagem | Naturezas separadas, campos obrigatórios e contexto opcional |
| 09.2 — Gestão | Detalhe/correção/invalidação/histórico e UI | Alteração rastreável sem pareamento financeiro artificial |
| 09.3 — Totais | Indicador/filtros e dashboard com origens | Comparação explícita de previsto/realizado e ausência por natureza |

## Cenários de aceite

1. R$ 90 previstos e R$ 100 realizados = duas naturezas e diferença +R$ 10; nunca R$ 190 realizados.
2. Só R$ 100 realizados é permitido; previsão aparece sem registros, não como zero observado. Nenhum lançamento gera NoData; lançamento explícito 0 permanece conhecido.
3. Valor negativo/mais de duas casas, contexto duplo e recurso de outra horta são rejeitados.
4. Correção 100 → 95 modifica realizado/diferença e conserva 100 no histórico; invalidação remove origem elegível.
5. Paginação de origens não altera totais. Aplicar verificações comuns e conferir versão/rollback/repetição.

**Validar com a PO:** categorias e significado da data. Não implementar contabilidade, receitas, estoque, pagamento, faturamento ou custo rateado por espécie. **Evidências:** pendentes.
