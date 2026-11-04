# Caso 04 — Selecionar responsável pelo trabalho

**Etapa:** [1](../Etapa_01_Contexto.md). **Estado:** concluído tecnicamente em 08/10/2026; evidências no plano da etapa. **Referência:** RF07; cadastro mínimo de participante.

Dependência: Garden e Participant persistidos. Ler [contrato](../Contrato_Fluxo.md) e exceção de autoria na arquitetura.

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/participants?page=1&pageSize=20` → 200 `Page<{id, displayName}>`.

1. Validar paginação e existência da horta.
2. Projetar participantes ativos da horta ordenados por DisplayName/Id.
3. Lista vazia permite deixar o responsável desconhecido. Não criar um participante “não informado” nem assumir que o operador técnico executou o trabalho.

## Alterações previstas

- `Domain/Activities/Participant.cs`: Id, GardenId, DisplayName e IsActive, sem documentos pessoais ou vínculos de acesso.
- Configuração EF e um participante fictício preparado explicitamente no contexto de teste.
- `Features/Activities/ListParticipants/ListParticipantsQueryHandler.cs` e `ListParticipantsEndpoints.cs`, registrados via ActivitiesEndpointRegistration.
- User demonstrativo permanece independente: não aparece nesta lista só por existir como autor técnico.

## Aceite

- Participante do contexto de teste é retornado; inativos e participantes de outra horta são omitidos.
- Horta inexistente é 404; página vazia é 200. Nomes iguais não são deduplicados: identidade é o ID.
- API não expõe AuthenticationIdentifier nem inclui perfis/permissões na seleção.

**Limite:** nenhum CRUD de pessoas, autenticação ou fluxo de atividades nesta entrega.
