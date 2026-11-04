# Fluxo 08 — Participação social e educação

**Requisitos:** RF15/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [modelo de atividades](../../arquitetura/Modelo_Atividades_Ocorrencias.md), arquitetura integral. **Dependência:** mesma Activity do fluxo 06. Não criar outro módulo de eventos para duplicar a atividade.

## Resultado e percurso

Classificar atividade como social e/ou educacional → informar objetivo, instituições e público conhecido → consultar ações e participação → conferir origem/correções sem duplicar ações nas duas dimensões.

## Dados e regras

- Activity acrescenta isSocial, isEducational, objective?, modality? texto, reportedAudienceTotal? inteiro >= 0, audienceListComplete? boolean, institutions[]. IsOperational já existe. Pelo menos uma dimensão é true; permitir mais de uma no mesmo registro.
- ActivityParticipant acrescenta countsAsAudience. A mesma pessoa pode ter horas e compor público, sem duas linhas. Não assumir que todo trabalhador compõe público.
- Institution: name, acronym?, type? texto, isActive. ActivityInstitution: institutionId, role? texto; um vínculo por instituição/atividade.
- Público informado é o total conhecido, incluindo a parte nominal; não é público adicional. Deve ser >= participantes com countsAsAudience.
- Lista declarada completa: total informado, quando existir, deve ser igual à contagem nominal. Sem total informado, lista completa permite derivar a contagem. Lista parcial sem total tem público total desconhecido e mínimo nominal conhecido.
- Sem lista nominal é permitido informar público agregado. Não gerar pessoas fictícias para representar o total e não deduplicar pessoas por nome.

## Contratos e slices

Estender RegisterActivity/CorrectActivity/GetActivity/ListActivities do fluxo 06 com esses campos; usar mesmos IDs/histórico. Activity continua fato único. Lista recebe filtros isSocial?/isEducational? e institutionId?. Cadastro: POST/GET `/api/v1/institutions`, detalhe/PUT/inativação/histórico.

Slices Activities: CreateInstitution/ListInstitutions/GetInstitution/UpdateInstitution/InactivateInstitution e histórico do cadastro; atualizar slices existentes. Indicators: GetSocialEducationalParticipation, GET `/api/v1/gardens/{gardenId}/indicators/social-educational-participation?from=...&to=...&dimension=Social|Educational&institutionId=...`.

Data de competência: performedOn. Retornar atividades distintas totais e por dimensão, público conhecido por evento somado (com eventos sem total identificado), presenças nominais e pessoas identificadas distintas no período, mais Page de origens. Uma atividade multidimensional aparece nos dois recortes, mas uma vez no total geral. Público agregado nunca compõe “pessoas únicas”; rotular “pessoas identificadas” e cobertura nominal. Não somar público total com presenças nominais.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 08.1 — Dimensões | Migration da Activity/filhos, instituições, validações e gestão nos slices existentes | Uma atividade reúne dimensões e instituições sem duplicação |
| 08.2 — Formulário | UI de objetivo/dimensões/instituições/público, detalhe/histórico | Lista completa/parcial e público agregado distinguíveis |
| 08.3 — Indicador | Consulta e dashboard por dimensão/período/instituição | Ações, presenças, público e pessoas identificadas são medidas diferentes |

## Cenários de aceite

1. Uma atividade social e educacional = uma ação no geral e uma em cada recorte, com um histórico.
2. Público total 20, oito pessoas identificadas: público 20 e nominal 8, não 28. Mesmo participante em dois eventos: duas presenças e uma pessoa identificada.
3. Público agregado sem nomes não aumenta pessoas identificadas. Lista parcial sem total mostra mínimo nominal e informação incompleta.
4. Total menor que nominal e lista completa com total divergente são rejeitados; dois vínculos da mesma instituição são rejeitados.
5. Alterar público ou invalidar atividade modifica recortes; horas do fluxo 06 continuam contando uma vez. Aplicar verificações comuns.

**Validar com a PO:** modalidades, objetivos exigidos, definição de público e completude nominal. Não criar matrícula, disciplina, avaliação acadêmica ou gestão de cursos. **Evidências:** pendentes.
