# Caso 06 — Listar irrigações

**Etapa:** [2](../Etapa_02_Registro_Consumo.md). **Estado:** não iniciado. **Referência:** RF13.

Dependência: registros persistidos. Ler [contrato](../Contrato_Fluxo.md), seções 3 e 4.

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/irrigations?from=2026-10-08&to=2026-10-08&sectorId={id}&validity=Valid&page=1&pageSize=20`.

sectorId/validity opcionais; período obrigatório. Resposta 200 Page de resumos definidos no contrato.

1. Validar período, enum textual e paginação. Resolver horta e setor filtrado no seu contexto.
2. Converter datas locais para os limites UTC e filtrar pelo início do evento. Default de validade é Valid.
3. Ordenar StartedAt/Id decrescente, contar, paginar e projetar campos. Usar setor registrado no evento, mesmo quando um canteiro mudou de setor posteriormente.
4. Ausência de eventos retorna items vazio; não produzir uma irrigação com volume 0.

## Alterações previstas

- `Features/Irrigation/ListIrrigations/ListIrrigationsQueryHandler.cs` e `ListIrrigationsEndpoints.cs`.
- Reutilizar construção do intervalo/filtros em `Features/Irrigation/Shared` somente se necessária aos demais casos; projeção de consumo continua no slice de indicador, sem chamar este handler.
- Rever índices para GardenId/StartedAt e filtros reais, sem criar índices especulativos para futuros domínios.

## Aceite

- Borda de meia-noite local e filtro de setor corretos; período invertido é 400.
- Evento Invalid fica fora do default e aparece com validity=Invalid. Enum numérico/indefinido é 400.
- Duas páginas de eventos com mesmo StartedAt têm desempate estável, sem repetição.
- Resumo apresenta volume null/0 corretamente e versão atual. Outra horta nunca aparece.

**Limite:** não incluir fotos, exportação, filtros por espécie/ciclo/canteiro ou dados completos do histórico nessa listagem.
