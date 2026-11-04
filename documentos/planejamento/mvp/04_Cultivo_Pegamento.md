# Fluxo 04 — Origem, cultivo, pegamento e observações

**Requisitos:** RF03/RF04/RF05/RF08/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [modelo de cultivo](../../arquitetura/Modelo_Cultivo_Producao.md), arquitetura integral. **Dependência:** canteiros/configurações do fluxo 02 e seleção de responsável existente.

## Resultado e percurso

Cadastrar espécie/cultivar e origem conhecida → abrir ciclo no canteiro → registrar plantio/reposição → avaliar sobrevivência → acompanhar observações → consultar pegamento sem duplicar avaliações.

## Dados e regras

| Registro | Campos do formulário e invariantes |
| --- | --- |
| Species/Cultivar | Espécie: commonName, scientificName?, description?; cultivar: speciesId, name, description?. Cultivar pertence à espécie; nomes não comprovam identidade botânica única |
| PropagationLot | code, speciesId, cultivarId?, materialType, origin?, receivedOn?, notes?. Código único por horta; não é estoque |
| CultivationCycle | bedId, speciesId, cultivarId?, startedOn, occupiedAreaM2, responsibleParticipantId?; referência da configuração vigente e área guardadas. Área positiva e <= área cultivável; status InProgress/Closed; encerramento não anterior ao início |
| Planting | cycleId, propagationLotId?, replacedPlantingId?, plantedOn, kind Initial/Replacement, plantedQuantity > 0 inteiro, plantSpacingCm?, rowSpacingCm?, responsibleParticipantId? |
| EstablishmentAssessment | plantingId, assessedOn, survivingQuantity inteiro entre 0 e plantedQuantity, method, notes?, responsibleParticipantId? |
| AgronomicObservation | cycleId, observedOn, category texto, description, measurement?, unit?, method?, responsibleParticipantId? |

MaterialType proposto: Seedling/Seed/Cutting/Other; classificar para não tratar germinação como pegamento de mudas. Avaliação deve ser posterior ou no dia do plantio. Plantio/observação/colheita devem estar no período do ciclo conhecido. Referência de reposição é outro plantio anterior do mesmo ciclo, sem autorreferência/circularidade. Lote/ciclo compartilham espécie e cultivares conhecidas não conflitam.

Origem desconhecida não impede plantio; não criar lote/cultivar fictícios. Medida de observação exige unidade/método, mas descrição de suspeita não exige diagnóstico. Alteração da configuração do canteiro não muda área de ciclo existente.

Fechar/corrigir datas do ciclo não pode deixar plantio, observação ou colheita válida fora do intervalo resultante: retornar conflito com dependentes a revisar. Correção da espécie/cultivar de lote ou ciclo não pode contradizer plantios/colheitas já vinculados; revisar dependentes explicitamente, sem cascata.

Não impor um ciclo ativo por canteiro ou resolver consórcio por inferência. Neste MVP, o indicador usa área de **cada ciclo**; controle de ocupação conjunta/consórcios é pendência funcional e não é apresentado como validado. Correção de área/configuração de ciclo só ocorre com motivo/version e recalcula seu indicador; não reclassifica área física global.

## Contratos e slices

Catálogos: POST/GET `/api/v1/species`, detalhe/PUT/inativação/histórico; POST/GET `/api/v1/species/{speciesId}/cultivars`, detalhe/PUT/inativação/histórico. Na horta: `/propagation-lots`, `/cultivation-cycles`, `/plantings`, `/establishment-assessments`, `/agronomic-observations`, todos com POST/GET e detalhe; fatos têm PUT/invalidation/history conforme contrato comum. Lotes são cadastros de origem com versão/histórico. Fechamento de ciclo: POST `/cultivation-cycles/{id}/closure` com endedOn, expectedVersion e reason.

Prefixo dos recursos locais: `/api/v1/gardens/{gardenId}`. Listas têm filtros from/to quando são fatos, mais bedId?/cycleId?/speciesId?/plantingId? conforme recurso, e validade. Consultas de ciclos usam início como data de competência e situação opcional.

Slices Cultivation: CreateSpecies/ListSpecies/GetSpecies/UpdateSpecies/InactivateSpecies; CreateCultivar/ListCultivars/UpdateCultivar/InactivateCultivar; RegisterPropagationLot/ListPropagationLots/GetPropagationLot/CorrectPropagationLot; OpenCultivationCycle/ListCultivationCycles/GetCultivationCycle/CorrectCultivationCycle/CloseCultivationCycle; RegisterPlanting/ListPlantings/GetPlanting/CorrectPlanting; RegisterEstablishmentAssessment/ListEstablishmentAssessments/GetEstablishmentAssessment/CorrectEstablishmentAssessment; RegisterAgronomicObservation/ListAgronomicObservations/GetAgronomicObservation/CorrectAgronomicObservation. Cada fato acrescenta Invalidate.../List...History específicos quando entra sua etapa; ciclos e lotes com dependentes válidos não são invalidados. Indicators: GetSeedlingEstablishment.

## Indicador do protótipo

GET `.../indicators/seedling-establishment?from=...&to=...&speciesId=...&kind=...&method=...`.

Hipótese: selecionar plantios válidos de mudas no período **do plantio**; usar uma avaliação válida por plantio, a mais recente até to e não anterior ao plantio. Desempate por assessedOn/RecordedAt/Id. Material desconhecido ou que não seja muda fica fora do percentual e tem contagem explícita de não elegíveis; plantios sem avaliação compõem cobertura. Filtrar method antes de selecionar a última avaliação e manter métodos separados. Reposição separada por kind.

Por plantio: sobreviventes/plantados × 100. Grupo compatível: soma sobreviventes/soma plantados **somente dos plantios com avaliação selecionada**; mostrar quantidade sem avaliação e base parcial. Não somar avaliações sucessivas nem usar média simples de percentuais. Retornar Page de origens/avaliação selecionada e resumos por método/natureza. Taxa exibida com duas casas, calculada decimal, sem arredondar entradas.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 04.1 — Origem | Espécies/cultivares/lotes e formulários de seleção/cadastro | Origem opcional, consistência espécie/cultivar e histórico de alterações |
| 04.2 — Ciclo e plantio | Abrir/fechar/corrigir ciclo, registrar/corrigir/inutilizar plantio, detalhe/histórico e UI | Vínculo de área congelado, reposição distinta e proteção de dependentes |
| 04.3 — Avaliação e observação | Avaliações/observações com gestão e UI | Sobreviventes válidos, suspeita sem diagnóstico, unidade/método coerentes |
| 04.4 — Pegamento | Indicador e dashboard com origem e cobertura | Última avaliação selecionada, reposições separadas, sem dupla contagem |

## Cenários de aceite

1. 40 mudas → 36 sobreviventes = 90%; segunda avaliação com 32 substitui a selecionada, não soma 68. Reposição de 4 mantém plantio original 40.
2. Plantios 40/36 e 10/5 no mesmo método: 41/50 = 82%, não média de 90% e 50%. Plantio sem avaliação gera parcialidade.
3. Mais sobreviventes que plantados, avaliação anterior, espécie/cultivar incompatível e contexto de outra horta são rejeitados.
4. Reduzir quantidade plantada abaixo de avaliação dependente, ou invalidar plantio com avaliação válida, dá 409 record.has_dependents. Rever filhos explicitamente.
5. Correção/invalidação altera percentual e história; observar canteiro/ciclo sem medida é permitido. Aplicar verificações comuns.

**Validar com a PO:** elegibilidade de material, janela/método da avaliação, seleção da última, unidade de ciclo e ocupação conjunta. Essas hipóteses não são aprovadas pelos testes. **Evidências:** pendentes.
