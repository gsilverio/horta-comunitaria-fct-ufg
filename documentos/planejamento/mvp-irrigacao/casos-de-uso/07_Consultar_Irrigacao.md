# Caso 07 — Consultar detalhe da irrigação

**Etapa:** [2](../Etapa_02_Registro_Consumo.md). **Estado:** não iniciado. **Referência:** RF07/RF13.

Dependência: entidades do evento, seus vínculos e autor técnico. Ler [contrato](../Contrato_Fluxo.md).

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/irrigations/{id}` → 200 detalhe ou 404 resource.not_found.

1. Resolver irrigação pelo par GardenId/Id, sem excluir Invalid da busca.
2. Projetar todos os campos do formulário, validade, versão, RecordedAt/UpdatedAt e RecordedBy `{id, displayName, isDemonstration:true}` para o autor deste protótipo.
3. Projetar canteiros atendidos com seus IDs/códigos/nomes e assignmentId histórico. Preservar os IDs da seleção para futuras correções. Não resolver setor atual como substituto do registrado.
4. Retornar DTO próprio. operationId/CreationPayload e detalhes técnicos da persistência não integram o DTO público; volume e campos desconhecidos continuam null.

## Alterações previstas

- `Features/Irrigation/GetIrrigation/GetIrrigationQueryHandler.cs` e `GetIrrigationEndpoints.cs`.
- Compartilhar projeção do detalhe entre respostas de comandos e consultas somente se reduzir duplicação concreta; não chamar o handler de consulta de dentro do comando.
- Location de RegisterIrrigation aponta para esta rota, com o mesmo ID/escopo.

## Aceite

- Detalhe de 120 L e quatro canteiros reproduz campos salvos, instantes UTC e versão 1.
- Evento invalidado continua consultável. ID de outra horta é 404, mesmo existindo no banco.
- Troca de setor depois do evento não altera seu contexto. Não ocorre N+1 por canteiro.
- Campo de autor enviado indevidamente pelo cliente nunca altera RecordedBy retornado pelo servidor.

**Limite:** histórico é outro caso de uso; não carregar todas as alterações junto com cada detalhe.
