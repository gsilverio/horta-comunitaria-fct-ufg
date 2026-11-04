# Fluxo 06 — Atividades e horas de trabalho

**Requisitos:** RF10/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [atividades](../../arquitetura/Modelo_Atividades_Ocorrencias.md), arquitetura integral. **Dependência:** horta e participantes mínimos; ciclo só quando selecionado. Não exige cultivo para atividade geral.

## Resultado e percurso

Registrar atividade → associar participantes e horas individuais conhecidas → registrar insumos usados → consultar esforço por período → corrigir dados preservando histórico.

## Dados e regras

- Participant: aproveitar o cadastro mínimo existente; adicionar criação/edição/inativação/histórico de nome/categoria?, sem dados pessoais adicionais.
- Activity: title, category texto, performedOn, description?, bedId?/sectorId?/cycleId? (no máximo um), participants[], inputUsages[]. Contexto geral permitido. Dimensão operacional true neste fluxo; social/educacional são acrescentadas pelo fluxo 08.
- ActivityParticipant: participantId, role? texto, workHours? >= 0; um participante por atividade. Desconhecido não é zero e lista pode estar vazia quando não se conhecem as pessoas. Público não é inferido a partir de executantes.
- Input: name, category? texto, referenceUnit, isActive. InputUsage: inputId, quantity > 0, unit compatível com cadastro. Vários usos do mesmo insumo são permitidos; não criar saldo/estoque ou custo automaticamente.
- Contexto/participantes são da mesma horta; ciclo, se informado, permite derivar canteiro. Uma correção de filhos registra lista anterior/nova na mesma transação da atividade.

## Contratos e slices

Prefixo local `/api/v1/gardens/{gardenId}`: POST/GET `/participants`, detalhe/PUT/inactivation/history (reutilizar ListParticipants); POST/GET `/activities`, GET/PUT `/{id}`, invalidação/histórico. Catálogo de insumos POST/GET `/api/v1/inputs`, detalhe/PUT/inativação/histórico; referenciado por ID, não replicado em cada atividade.

Payload de atividade contém os campos acima; participantes e usos são confirmados junto com o pai. Listagem filtra from/to por performedOn, contexto opcional e validade. Detalhe contém todos os filhos necessários à edição, sem carregar outras atividades da pessoa.

Slices Activities: CreateParticipant/GetParticipant/UpdateParticipant/InactivateParticipant; CreateInput/ListInputs/UpdateInput/InactivateInput; RegisterActivity/ListActivities/GetActivity/CorrectActivity/InvalidateActivity/ListActivityHistory; históricos de cadastros conforme contrato. Indicators: GetWorkHours, GET `.../indicators/work-hours?from=...&to=...&participantId=...&category=...`.

Indicador soma horas individuais conhecidas das atividades válidas no período, antes de joins de insumos/instituições. Retorna horas conhecidas, atividades distintas, registros de participação com/sem horas, disponibilidade e Page de origens. Filtro participantId considera apenas a participação dessa pessoa. Cobertura é dos participantes registrados; lista vazia não comprova ausência de trabalho.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 06.1 — Participantes e insumos | Cadastros/gestão/seleção mínimos, migrations, UI | Pessoa sem conta e insumo com unidade, sem estoque |
| 06.2 — Atividade | Registro e gestão do agregado com filhos/histórico e UI | Participação única por pessoa, contexto geral e horas null preservados |
| 06.3 — Horas | Consulta/indicador e dashboard | 3 × 2 h = 6 h; insuficiência de coleta explícita |

## Cenários de aceite

1. Três pessoas, duas horas cada = seis horas, mesmo com dois insumos e futuras instituições vinculadas.
2. Dois participantes com horas 2/null: soma conhecida 2, Partial. Todas horas null: Unavailable; zero informado: 0 observado; nenhuma atividade: NoData.
3. Atividade sem participantes registrados tem horas indisponíveis, não 0; informar isso separadamente da cobertura nominal.
4. Participante duplicado, horas negativas, unidade incompatível e contexto de outra horta são rejeitados. Mesmo insumo em duas utilizações legítimas é permitido.
5. Correção/invalidação atualiza origem/horas/histórico; operação repetida não duplica filhos. Aplicar verificações comuns e interface de celular.

**Validar com a PO:** categorias e maneira de registrar horas individuais. Não presumir duração de atividade igual às horas de cada pessoa. **Evidências:** pendentes.
