# Fluxo 02 — Organização da horta e cadastros

**Requisitos:** RF02; suporte a RF07/RF13/RF14. **Estado:** backend e interface implementados tecnicamente; aceite da PO pendente. Verificações em [Execução do backend](../Execucao_Backend_MVP.md) e [Execução do frontend](../Execucao_Frontend_MVP.md).

**Leituras:** [contrato comum](Contrato_Comum.md), [organização dos dados](../../arquitetura/Modelo_Organizacao_Horta.md), arquitetura integral conforme AGENTS.md. **Dependência:** base demonstrativa do [fluxo 01](../mvp-irrigacao/README.md); reutilizar seus tipos e slices, sem recriar host ou banco.

## Resultado e percurso

Cadastrar horta/setores/canteiros → configurar área e associação de setor por vigência → registrar fatos nesses contextos → consultar irrigação por setor com história preservada. Quantidades do anexo não são limites do cadastro.

## Dados e contratos

| Recurso | Campos editáveis deste requisito |
| --- | --- |
| Garden | code, name, location?, institutionalAreaM2?, operationalAreaM2?, experimentalAreaM2? |
| IrrigationSector | code, name, description?; horta vem da rota |
| Bed | code, name, location?; horta vem da rota |
| BedConfiguration | validFrom, lengthMeters?, widthMeters?, cultivableAreaM2 |
| BedSectorAssignment | sectorId, validFrom; fim anterior é fechado pela operação de sucessão |

Completar apenas esses campos nos cadastros do fluxo 01; acrescentar BedConfiguration. Áreas/dimensões conhecidas positivas. Não calcular área efetiva obrigatoriamente por comprimento × largura. Ativo é controlado por operação própria; versão/histórico seguem contrato comum.

Prefixo `/api/v1/gardens`: POST/GET na raiz; GET/PUT `/{gardenId}`; POST `/{gardenId}/sectors` e `/{gardenId}/beds`, GET dos setores; GET/PUT dos respectivos `.../{id}`; POST `.../{id}/inactivation` com expectedVersion/reason. A consulta de canteiros elegíveis existente permanece; listagem administrativa fica em `/{gardenId}/beds/catalog` para não conflitar com seu contrato.

Configuração: POST/GET `/{gardenId}/beds/{bedId}/configurations`; associação: POST/GET `.../sector-assignments`. Criação inicial exige motivo só quando é correção histórica; sucessão recebe expectedBedVersion e reason. Detalhe do canteiro retorna configuração e associação vigentes em `on`, não todas as irrigações. Histórico usa rotas dos respectivos cadastros.

Slices Gardens: CreateGarden/UpdateGarden/GetGarden/InactivateGarden; CreateIrrigationSector/UpdateIrrigationSector/GetIrrigationSector/InactivateIrrigationSector; CreateBed/UpdateBed/GetBed/ListBedCatalog/InactivateBed; ConfigureBed/ListBedConfigurations; AssignBedToSector/ListBedSectorAssignments; consultas de histórico específicas. Reutilizar listagens da primeira entrega.

## Etapas de implementação

| Etapa | Trabalho autorizado quando solicitada | Saída e aceite |
| --- | --- | --- |
| 02.1 — Cadastros | Comandos/detalhes de horta, setor e canteiro, versões e auditoria; UI simples de cadastro | Criar canteiro adicional sem alterar fixture; códigos únicos, dados opcionais e inativação preservados |
| 02.2 — Vigências | BedConfiguration, comandos/consultas de configuração e associação; atualizar validação de irrigação para disputar o mesmo recurso | Sucessão atômica, sem sobreposição ou reclassificação do passado |
| 02.3 — Irrigação por setor | Estender GetWaterConsumption com contagem Completed/Interrupted, soma de duração conhecida e quantidade sem duração por setor; atualizar UI/OpenAPI | Consumo continua 120 L/4 canteiros; duração parcial identificada; histórico do setor permanece |

## Regras de vigência e concorrência

- Vigência diária `[início,fim)`; um vínculo/configuração por canteiro em cada dia. Proposta do MVP: operações de sucessão começam hoje ou depois, preservam o registro anterior e abrem o seguinte; mudanças intradiárias/correção retroativa complexa ficam pendentes de definição da PO.
- Primeira associação/configuração pode ter início histórico, desde que não sobreponha intervalos nem contradiga fatos existentes. Não oferecer edição livre de intervalo já usado.
- Transação bloqueia a linha Bed; encerra intervalo aberto e cria sucessor. Comandos que registram irrigação nesse canteiro devem usar a mesma proteção durante validação/gravação; ordenar IDs de locks. Concorrência com alteração de setor precisa de teste real.
- Se a mudança tornaria incompatível um fato já registrado no período afetado, rejeitar 409 record.has_dependents e indicar os fatos a revisar. Não mover o fato para outro setor automaticamente.
- Inativar cadastro impede novas seleções/escritas; leitura e fatos anteriores permanecem. Horta inativa não aceita novos fatos. Atualização de nome/código não altera identidade dos fatos.

## Cenários de aceite

1. B1 passa de S1 para S2 em 10/11: evento de 09/11 continua S1; seleção de 10/11 retorna S2. Dois comandos concorrentes não abrem dois vínculos.
2. Nova área efetiva não modifica a área de referência de ciclo anterior. Dimensões ausentes são null.
3. FKs inválidas, setor de outra horta, intervalo sobreposto e versão desatualizada são rejeitados; auditoria e cadastro confirmam juntos.
4. Setor com 120 L, evento Interrupted de 20 L e outro sem duração: volume conhecido 140 L; estados e cobertura de duração corretos, sem multiplicação por canteiros.
5. Aplicar verificações comuns e conferir UI de celular. Nenhuma exclusão física ou cadastro de sensores.

**Validar com a PO:** áreas coletáveis e vigência diária. Não declarar resolvidos consórcios, ocupação conjunta ou mudanças intradiárias. **Evidências das etapas:** pendentes.
