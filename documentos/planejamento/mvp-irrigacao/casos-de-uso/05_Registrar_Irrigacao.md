# Caso 05 — Registrar irrigação manual

**Etapa:** [2](../Etapa_02_Registro_Consumo.md). **Estado:** não iniciado. **Referência:** RF07 e rastreabilidade.

Dependência: etapa 1 e mapeamentos do fato/histórico. Ler [contrato](../Contrato_Fluxo.md), seções 1, 2, 4 e 5. O detalhe da criação deve estar disponível ao concluir a etapa 2.

## Contrato e sequência

`POST /api/v1/gardens/{gardenId}/irrigations`, body = formulário do contrato + `operationId` UUID obrigatório.

1. Validar formato, enums, precisão e campos; normalizar payload para comparação sem perder null/zero. Rejeitar Guid.Empty e IDs inválidos.
2. Consultar a operationId nessa horta. Se já existir, comparar com CreationPayload: igual retorna 200/Location e detalhe atual; diferente retorna 409 operation_conflict. Repetição não revalida vínculos como se fosse um evento novo.
3. Para criação nova, resolver horta/setor ativos, responsável opcional e cada vínculo na data local do início. IDs fora da horta são 404; canteiro sem vínculo compatível é 400 invalid_bed_assignment. Resolver em lote, sem N+1.
4. Factory de Irrigation aplica invariantes locais; handler coordena as relações. Gerar ID UUID v7, versão 1, validade Valid, autor técnico e RecordedAt via TimeProvider.
5. Gravar fato, filhos e AuditEntry Created atomicamente. Proteger duplicação com índice único, inclusive sob concorrência; tratar somente a violação da chave de operação para recarregar/comparar.
6. Retornar 201, Location do detalhe e representação salva; não retornar entidade EF.

## Alterações previstas

- `Domain/Irrigation` e configurações/migration conforme etapa 2. Auditoria fica no domínio de histórico e seus mapeamentos; sem referência a HTTP/JSON no domínio.
- `Features/Irrigation/RegisterIrrigation/RegisterIrrigationCommandHandler.cs` e `RegisterIrrigationEndpoints.cs`; registrador Irrigation.
- Comparação do payload inicial é compartilhada apenas dentro do domínio de features quando houver reutilização real. Nenhum motor genérico de idempotência ou outbox.

## Aceite

- 120 L e quatro canteiros persistem uma vez; null é preservado; autoria não vem do cliente.
- Mesma chave em duas requisições concorrentes resulta em um fato/Created; conteúdo diferente conflita. Nova chave permite outro evento no mesmo horário.
- Todos os casos inválidos da etapa 2 deixam zero registros parciais; falha de auditoria reverte a gravação.
- Troca histórica de setor não muda o vínculo salvo de um evento anterior.

**Limite:** não estimar volume automaticamente, abrir evento em andamento ou criar ocorrência de falha.
