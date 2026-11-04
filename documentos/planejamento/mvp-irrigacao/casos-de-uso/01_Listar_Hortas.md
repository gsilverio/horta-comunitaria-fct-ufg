# Caso 01 — Listar hortas

**Etapa:** [1](../Etapa_01_Contexto.md). **Estado:** concluído tecnicamente em 08/10/2026; evidências no plano da etapa. **Referência:** RF02, recorte de consulta.

Ler [contrato comum](../Contrato_Fluxo.md) e arquitetura conforme AGENTS.md. Este é o primeiro slice real e depende da infraestrutura e migration da etapa 1.

## Contrato e sequência

`GET /api/v1/gardens?page=1&pageSize=20` → 200 `Page<{id, code, name}>`.

1. Endpoint recebe paginação, injeta ListGardensQueryHandler e traduz Result.
2. Handler valida limites, consulta Garden com IsActive, conta resultados e projeta a página ordenada por Code/Id. Usar AsNoTracking.
3. Retornar Page com contagens e flags coerentes. Banco sem hortas retorna página vazia, não 404 e não um cadastro criado automaticamente.

## Alterações previstas

- `Domain/Gardens/Garden.cs`: factory e invariantes do cadastro definido no contrato.
- `Infrastructure/Persistence/Configurations/GardenConfiguration.cs`: tabela gardens, UUID e código único.
- `Features/Gardens/ListGardens/ListGardensQueryHandler.cs` e `ListGardensEndpoints.cs`.
- `GardensEndpointRegistration.cs`: delegação; registrador geral e Program conforme arquitetura. Result/paginação e infraestrutura são criados somente uma vez na etapa 1.

## Aceite

- Contexto preparado explicitamente aparece com seu ID; banco limpo retorna items vazio e totalCount 0.
- Horta inativa não aparece. Segunda horta de fixture permite testar ordenação e duas páginas sem duplicação.
- page 0 e pageSize > 100 retornam 400 sem executar consulta inválida.
- Teste de integração lê dados persistidos no PostgreSQL; OpenAPI registra contrato e rota.

**Limite:** nenhuma criação/edição/exclusão de horta pela API. Entregar este slice e os testes, sem iniciar outro caso não autorizado.
