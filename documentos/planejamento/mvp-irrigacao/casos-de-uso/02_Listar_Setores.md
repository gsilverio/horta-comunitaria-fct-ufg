# Caso 02 — Listar setores de irrigação

**Etapa:** [1](../Etapa_01_Contexto.md). **Estado:** concluído tecnicamente em 08/10/2026; evidências no plano da etapa. **Referência:** RF02.

Dependência: persistência de Garden e IrrigationSector. Ler [contrato](../Contrato_Fluxo.md) e arquitetura conforme AGENTS.md.

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/sectors?page=1&pageSize=20` → 200 `Page<{id, code, name}>`.

1. Handler valida paginação e existência da horta no contexto de leitura.
2. Seleciona setores ativos dessa horta, ordenados por Code/Id; conta e projeta a página.
3. Horta inexistente: 404 resource.not_found. Horta existente sem setores: página vazia. Não inferir cinco setores nem criar S1 automaticamente na consulta.

## Alterações previstas

- `Domain/Gardens/IrrigationSector.cs` e configuração EF: GardenId obrigatório, código único na horta e exclusão restritiva.
- `Features/Gardens/ListIrrigationSectors/ListIrrigationSectorsQueryHandler.cs` e `ListIrrigationSectorsEndpoints.cs`.
- Delegação no registrador Gardens; completar migration/preparo explícito de teste dentro da etapa 1.

## Aceite

- Horta demonstrativa retorna S1/S2; fixtures de outra horta não aparecem.
- Setor inativo é omitido, embora seu registro seja preservado no banco.
- Código repetido na mesma horta é impedido pelo banco; código igual em hortas diferentes é permitido.
- Horta inexistente é 404; paginação inválida é 400; Page conserva ordenação e contagens.

**Limite:** não implementar manutenção, transferência de canteiros ou configuração de irrigação automática.
