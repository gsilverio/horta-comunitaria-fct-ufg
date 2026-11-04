# Caso 10 — Invalidar irrigação

**Etapa:** [3](../Etapa_03_Correcao_Historico.md). **Estado:** concluído tecnicamente; [evidências](../Etapa_03_Correcao_Historico.md#evidências-da-execução), PO pendente. **Referência:** RF13/RF14.

Dependência: fato com versão e histórico, leitura e consumo. Ler [contrato](../Contrato_Fluxo.md).

## Contrato e sequência

`POST /api/v1/gardens/{gardenId}/irrigations/{id}/invalidation`, body `{expectedVersion, reason}` → 200 detalhe atualizado.

1. Validar motivo/versão e carregar por GardenId/Id; ausente/outro contexto é 404.
2. Versão diferente é 409 version_conflict. Já invalidado com versão atual é 409 record_invalid.
3. Método de domínio marca Invalid, incrementa Version e atualiza instante. Não alterar dados coletados ou remover associações.
4. Criar AuditEntry Invalidated com motivo e snapshots. Confirmar alteração e histórico atomicamente; concorrência retorna 409.
5. Detalhe/histórico continuam acessíveis; próximo consumo e listagem default excluem o fato.

## Alterações e aceite

- Slice `Features/Irrigation/InvalidateIrrigation`, CommandHandler/Endpoints; método no agregado e delegação de registro.
- Único evento de 100 L invalidado deixa consumo NoData, preservando valor no banco e histórico.
- Outra horta, motivo vazio, versão antiga e repetição não removem nem duplicam histórico.
- Conflito simultâneo correção/invalidação da mesma versão confirma uma operação; rollback da auditoria conserva validade anterior.

**Limite:** invalidar é diferente de Interrupted; não criar exclusão física, reativação ou cascata sobre cadastros.
