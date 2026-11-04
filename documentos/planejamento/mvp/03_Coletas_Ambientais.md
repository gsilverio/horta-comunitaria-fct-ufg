# Fluxo 03 — Coletas ambientais e umidade do solo

**Requisitos:** RF06/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [modelo ambiental](../../arquitetura/Modelo_Ambiente_Irrigacao.md), arquitetura integral. **Dependência:** organização/vínculos do fluxo 02. Não exige ciclo de cultivo.

## Resultado e percurso

Definir variável e ponto de coleta → registrar valor manual com unidade/método/instante → consultar histórico por ponto → visualizar umidade do solo → corrigir/inutilizar informação com história preservada.

## Dados e contratos

- EnvironmentalVariable: code, name, quantity, referenceUnit, description?, isActive. Não é dispositivo. Começar por soil_moisture; demais variáveis previstas no RF06 podem ser cadastradas sem criar outro modelo.
- SamplingPoint: code, description, location?, bedId? ou sectorId? (no máximo um); GardenId da rota. Ponto geral é permitido. Depois de existir coleta, não deslocar seu contexto: criar outro ponto.
- EnvironmentalMeasurement: samplingPointId, variableId, collectedAt, value, unit, method, quality, responsibleParticipantId?, notes?; contexto histórico de setor quando conhecido. Quality do protótipo: Normal/Questionable, separada de Valid/Invalid.
- Valor obrigatório com método/unidade; não impor faixa 0–100 a toda grandeza. Unidade deve corresponder à grandeza cadastrada; método obrigatório texto. Comparação de métodos usa identificador textual normalizado, sem fundir nomes distintos automaticamente.

Catálogos globais: POST/GET `/api/v1/environmental-variables`, PUT `/{id}` e inativação/histórico. Ponto: POST/GET `/api/v1/gardens/{gardenId}/sampling-points`, detalhe/PUT/inativação/histórico; contexto usado não é campo mutável. Fatos: POST/GET `.../environmental-measurements`, GET/PUT `.../{id}`, invalidação/histórico. Filtros de lista: from/to, pointId?, variableId?, unit?, method?, quality?, validity?.

Indicador: GET `.../indicators/soil-moisture?from=...&to=...&pointId=...&unit=...&method=...`. Retorna Page de observações válidas com valor, instante, qualidade, unidade/método e ID de origem, mais contagens Normal/Questionable e última observação **do recorte** por grupo. Não calcular média global/por horta, previsão ou umidade atual presumida. Quality Questionable aparece sinalizada; não excluir silenciosamente. Fórmula agregada aguarda a PO.

Slices Environment: CreateEnvironmentalVariable/ListEnvironmentalVariables/UpdateEnvironmentalVariable/InactivateEnvironmentalVariable; CreateSamplingPoint/ListSamplingPoints/GetSamplingPoint/UpdateSamplingPoint/InactivateSamplingPoint; RegisterEnvironmentalMeasurement/ListEnvironmentalMeasurements/GetEnvironmentalMeasurement/CorrectEnvironmentalMeasurement/InvalidateEnvironmentalMeasurement/ListEnvironmentalMeasurementHistory. Histórico de catálogos conforme contrato comum. Indicators: GetSoilMoisture.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 03.1 — Contexto de coleta | Entidades/catálogos/pontos, migrations, slices e seleção na UI | Ponto geral ou de canteiro/setor, sem exigir ambos ou inventar coordenadas |
| 03.2 — Registro e gestão | Medição, contexto histórico, idempotência, detalhe, correção/invalidação/histórico | Medição manual persistida; mudança de setor não reclassifica coleta |
| 03.3 — Visualização | Consulta de umidade, tabela/serie temporal por grupo compatível e navegação à origem | Valores/qualidade visíveis, sem média incompatível ou dados ausentes iguais a zero |

## Cenários de aceite

1. Duas coletas de umidade em um ponto/método/unidade aparecem em ordem temporal; método diferente produz grupo separado.
2. Ponto sem coleta tem NoData. Coleta com valor zero conserva 0; Questionable é sinalizada. Não criar coleta com value null.
3. Ponto de B1 registra vínculo histórico de setor quando disponível; vínculo inexistente fica null e não é substituído posteriormente pelo atual.
4. Unidade incompatível, contexto duplo, responsável/ponto de outra horta e tentativa de deslocar ponto já usado são rejeitados.
5. Correção altera série e histórico; invalidação remove observação elegível. Cobrir concorrência, repetição e rollback conforme contrato comum.

**Hipóteses para a PO:** métodos, unidades por variável, qualidade e se basta histórico/última medição para o primeiro dashboard. Frequência e cobertura espacial não são inventadas. **Evidências:** pendentes.
