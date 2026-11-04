# Caso 11 — Consultar histórico da irrigação

**Etapa:** [3](../Etapa_03_Correcao_Historico.md). **Estado:** concluído tecnicamente; [evidências](../Etapa_03_Correcao_Historico.md#evidências-da-execução), PO pendente. **Referência:** RF13.

Dependência: inclusão e alterações auditadas. Ler [contrato](../Contrato_Fluxo.md).

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/irrigations/{id}/history?page=1&pageSize=20` → Page de `{id, operation, reason, occurredAt, actor, before, after}`.

1. Validar paginação e existência do fato no escopo, mesmo Invalid.
2. Filtrar AuditEntry por horta, tipo Irrigation e chave exata do evento; ordenar OccurredAt/Id decrescente.
3. Projetar snapshots e autor técnico identificável como demonstração. Created tem before null e não exige motivo. Alterações contêm os campos/vínculos necessários à reconstrução.
4. Retornar histórico paginado; não permitir sua edição e não somá-lo aos fatos do dashboard.

## Alterações e aceite

- Slice `Features/Irrigation/ListIrrigationHistory`, QueryHandler/Endpoints e registrador.
- Sequência criação/correção/invalidação mostra três operações corretas, inclusive 120 → 100 e vínculos anteriores.
- Nenhuma alteração de outra horta/evento aparece; ID fora do contexto é 404. Ordenação desempata instantes iguais.
- Histórico de evento Invalid continua disponível. Payload interno de idempotência não é exposto.

**Limite:** não criar endpoint genérico para consultar todas as auditorias ou editar snapshots.
