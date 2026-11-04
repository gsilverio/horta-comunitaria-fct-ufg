# Caso 03 — Selecionar canteiros elegíveis

**Etapa:** [1](../Etapa_01_Contexto.md). **Estado:** concluído tecnicamente em 08/10/2026; evidências no plano da etapa. **Referência:** RF02/RF07.

Dependências: Garden, IrrigationSector, Bed e BedSectorAssignment persistidos. Ler [contrato](../Contrato_Fluxo.md), especialmente vigências e data local.

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/beds?on=2026-10-08&sectorId={id}&page=1&pageSize=20` → 200 `Page<{id, code, name, assignmentId}>`.

1. Validar on obrigatório/DateOnly, setor obrigatório e paginação.
2. Resolver horta e setor ativo dessa horta; referência fora do contexto é 404.
3. Consultar canteiros ativos da horta com vínculo nesse setor: ValidFrom <= on e (ValidUntil null ou on < ValidUntil). Projetar uma linha por canteiro, ordenada por Code/Id.
4. Zero elegíveis retorna página vazia. Não substituir por setor atual nem utilizar a data de hoje no lugar da informada.

## Alterações previstas

- Entidades Bed/BedSectorAssignment em `Domain/Gardens`, configurações e contexto de teste da etapa 1.
- `Features/Gardens/ListBedsForIrrigation/ListBedsForIrrigationQueryHandler.cs` e `ListBedsForIrrigationEndpoints.cs`.
- Garantir FK e intervalo válido no banco. Neste recorte, vínculos só são gerados por preparo explícito/fixtures validados e não possuem rota de alteração. Criação concorrente de vínculos exige planejamento próprio quando esse cadastro for implementado.

## Aceite

- Contexto de teste: quatro canteiros em S1 no dia 08/10; nenhum antes de 01/10.
- Fixture com vínculo S1 terminando em 09/10 e S2 começando em 09/10: dia 08 seleciona S1; dia 09 seleciona S2, sem dupla vigência.
- Canteiro inativo, outro setor ou outra horta não aparece. Limites de página não duplicam canteiros.
- Data ausente/inválida é 400. Endpoint não grava nem altera associações.

**Limite:** dimensões, área e ciclos de cultivo não participam desta seleção.
