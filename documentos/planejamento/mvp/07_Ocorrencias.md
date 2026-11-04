# Fluxo 07 — Ocorrências e providências

**Requisitos:** RF11/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [modelo de ocorrências](../../arquitetura/Modelo_Atividades_Ocorrencias.md), arquitetura integral. **Dependência:** atividade do fluxo 06 para intervenções; irrigação/ciclo opcionais como contexto.

## Resultado e percurso

Abrir ocorrência → acompanhar situação → vincular uma ou várias atividades de providência → registrar resolução → consultar ocorrências do período sem contar cada intervenção como outra falha.

## Dados e regras

Occurrence: openedAt, category texto, description, status Open/InProgress/Resolved, resolvedAt?, resolution? texto, responsibleParticipantId?, um contexto opcional entre bedId/sectorId/cycleId/irrigationId. Todos os vínculos pertencem à horta. Resolução exige instante não anterior à abertura e descrição da providência conclusiva; não inventar SLA ou aprovação.

Activity recebe occurrenceId? adicional ao seu contexto específico. Se ambos conhecidos, devem ser compatíveis: mesma horta; um ciclo permite derivar seu canteiro; uma irrigação permite derivar setor. Não inferir e abrir outra ocorrência a cada nova atividade.

Não fazer deduplicação por data/local/categoria: duas falhas podem ser legítimas. operationId protege retry; equipe escolhe ocorrência existente para nova intervenção. Histórico preserva mudanças de situação/responsável/resolução. Invalidação com atividades válidas vinculadas exige revisão explícita, conforme contrato comum.

## Contratos e slices

Prefixo `/api/v1/gardens/{gardenId}`: POST/GET `/occurrences`, GET/PUT `/{id}`, invalidação/histórico. Abertura cria Open; PUT atualiza contexto/dados com motivo. POST `/{id}/status` com status Open/InProgress, expectedVersion/reason; POST `/{id}/resolution` com resolvedAt/resolution/expectedVersion/reason. Reabrir Resolved é pendência e não tem endpoint neste MVP.

Atividade vinculada é criada pelo slice RegisterActivity existente com occurrenceId; incluir filtro occurrenceId na listagem e link no detalhe da ocorrência. Não copiar atividade para uma entidade de intervenção distinta.

Slices Occurrences: OpenOccurrence/ListOccurrences/GetOccurrence/CorrectOccurrence/ChangeOccurrenceStatus/ResolveOccurrence/InvalidateOccurrence/ListOccurrenceHistory. Indicators: GetOperationalOccurrences, GET `.../indicators/operational-occurrences?from=...&to=...&category=...&status=...`.

Indicador do protótipo: contar ocorrências válidas **abertas no período**, agrupadas pela situação atual, com Page de origens e contagem distinta. Não confundir com estoque de todas as ocorrências ainda abertas, nem reconstruir situação histórica no último dia do filtro. Rotular “Ocorrências abertas no período, por situação atual”. Mudança de situação não muda data de competência.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 07.1 — Abertura/gestão | Entidade, slices, histórico e UI de ocorrência | Uma falha possui identidade própria e estado consultável |
| 07.2 — Providências | Vínculo de atividade, validações de contexto, resolução e UI | Inspeção/reparo ficam associados à mesma ocorrência |
| 07.3 — Indicador | Contagem por período/situação e dashboard | Uma ocorrência com duas atividades continua uma ocorrência |

## Cenários de aceite

1. Uma falha com inspeção e reparo = uma ocorrência e duas atividades, com suas horas contabilizadas no fluxo 06.
2. Resolução mantém abertura original; dado anterior/novo e motivo ficam no histórico. Resolução anterior à abertura é rejeitada.
3. Atividade de outra horta ou contexto incompatível é rejeitada. Invalidar pai com atividade válida dá conflito revisável.
4. Nenhuma ocorrência no período é NoData; duas aberturas com operação diferente são duas ocorrências, mesmo no mesmo dia/local.
5. Conflito de versão, repetição, correção de data e invalidação refletem na contagem corretamente. Aplicar verificações comuns.

**Validar com a PO:** categorias, situações e interpretação da data/situação do indicador. **Evidências:** pendentes.
