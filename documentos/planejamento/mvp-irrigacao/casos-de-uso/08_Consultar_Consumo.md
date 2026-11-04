# Caso 08 — Consultar consumo conhecido de água

**Etapa:** [2](../Etapa_02_Registro_Consumo.md). **Estado:** não iniciado. **Referência:** RF14.

Dependência: irrigação e setores persistidos. Ler [contrato](../Contrato_Fluxo.md), principalmente a matriz de disponibilidade e a hipótese de recorte temporal. Esta fórmula é do protótipo, pendente de validação da PO.

## Contrato e sequência

`GET /api/v1/gardens/{gardenId}/indicators/water-consumption?from=2026-10-08&to=2026-10-08&sectorId={id}&page=1&pageSize=20`.

Retorno: `{from, to, timeZone, unit:"L", summary, bySector:Page<linha>}`. Resumo e linha têm os campos de consumo/cobertura da seção 3 do contrato; linha também contém sectorId/code/name.

1. Validar período/paginação e contexto da horta/setor opcional.
2. Iniciar transação de leitura RepeatableRead. Filtrar eventos válidos por GardenId, início no intervalo UTC e setor opcional.
3. Calcular contagens, soma de volumes conhecidos e presença de estimados diretamente sobre Irrigation; nenhum join com IrrigationBed entra na agregação.
4. Selecionar setores ativos ou com eventos válidos no recorte, restritos ao filtro opcional; contar e paginar Code/Id. Combinar os agregados da página sem perder setores vazios. O resumo cobre o recorte inteiro independentemente da página.
5. Produzir NoData/Unavailable/Partial/Available, preservando null e zero. Encerrar a transação antes de devolver DTO.

## Alterações previstas

- `Features/Indicators/GetWaterConsumption/GetWaterConsumptionQueryHandler.cs` e `GetWaterConsumptionEndpoints.cs`; registrador Indicators.
- Se houver cálculo puro compartilhado, colocá-lo em política adequada; consulta e agregação EF permanecem neste slice. Nada de tabela water_consumption ou motor de fórmulas.

## Aceite

- Todos os cenários de consumo do contrato: 120/4 = 120, 0 observado, ausência, só desconhecidos, parcial e Interrupted com 20 L.
- Estimated define includesEstimatedVolume sem contar evento invalidado. Parte conhecida não vira total real presumido.
- Resumo permanece igual ao mudar página; soma não aumenta ao adicionar associação de canteiro ao mesmo evento.
- Setor S2 vazio aparece NoData; setor inválido/outra horta é 404. Eventos cruzando dias seguem início.
- Múltiplas consultas do indicador compartilham snapshot PostgreSQL; correções confirmadas aparecem na próxima chamada, sem cache.

**Limite:** não ratear por canteiro/dia, projetar consumo futuro ou comparar com metas ainda não aprovadas.
