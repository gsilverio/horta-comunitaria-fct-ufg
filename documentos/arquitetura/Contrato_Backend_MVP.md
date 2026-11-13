# Contrato implementado do backend do MVP

Implementação dos fluxos 02–09 autorizada em 08/10/2026, integrada à irrigação existente. RF12 foi adiado explicitamente pelo responsável; RF01 continua pós-MVP. As regras funcionais propostas nos planos continuam sujeitas à PO. A interface integra estes recursos; consulte a [execução do frontend](../planejamento/Execucao_Frontend_MVP.md).

## Recursos e operações

Todos os prefixos abaixo incluem `/api/v1`. O Swagger e `/openapi/v1.json` descrevem os campos obrigatórios, tipos e respostas da versão executada.

| Prefixo | Recursos | Operações |
| --- | --- | --- |
| Global | `/gardens`, `/species`, `/environmental-variables`, `/inputs`, `/institutions` | POST de criação; GET de lista; GET/PUT `/{id}`; POST `/{id}/inactivation`; GET `/{id}/history` |
| Espécie | `/species/{speciesId}/cultivars` | Criação, lista, detalhe, edição, inativação e histórico |
| Horta `/gardens/{gardenId}` | `/sectors`, `/beds`, `/participants`, `/propagation-lots`, `/sampling-points` | Criação, edição, detalhe, inativação e histórico; listas específicas por recurso |
| Horta | `/environmental-measurements`, `/cultivation-cycles`, `/plantings`, `/establishment-assessments`, `/agronomic-observations`, `/harvests`, `/drying-lots`, `/activities`, `/occurrences`, `/financial-entries`, `/irrigations` | POST de registro; GET de lista; GET/PUT `/{id}`; POST `/{id}/invalidation`; GET `/{id}/history` |
| Canteiro | `/gardens/{gardenId}/beds/{bedId}/configurations`, `/sector-assignments` | POST de nova vigência e GET do histórico de vigências |
| Ciclo | `/gardens/{gardenId}/cultivation-cycles/{id}/closure` | POST com endedOn, expectedVersion e reason |
| Secagem | `/gardens/{gardenId}/drying-lots/{id}/completion` | POST com endedOn, totalDryMassKg, expectedVersion e reason |
| Ocorrência | `/gardens/{gardenId}/occurrences/{id}/status`, `/resolution` | POST de acompanhamento Open/InProgress ou resolução com data e descrição |

`GET /gardens/{gardenId}/beds` continua sendo seleção por setor/data para irrigação. A listagem administrativa fica em `/beds/catalog`. O detalhe de canteiro aceita `on=yyyy-MM-dd` para configuração e setor vigentes naquele dia. Lotes de origem são cadastros com `isActive` e `/inactivation`, separados da validade de fatos agrícolas.

## Consultas administrativas de apoio ao frontend

`GET /gardens/catalog`, `GET /gardens/{gardenId}/sectors/catalog` e `GET /gardens/{gardenId}/participants/catalog` retornam os DTOs completos de cadastro, incluindo `isActive` e `version`, com paginação e filtro opcional `isActive=true|false`. Sem filtro incluem ativos e inativos, ordenados por código/nome e ID. `GET /beds/catalog` já cumpre esse papel para canteiros. Horta inexistente nas consultas locais retorna 404; horta inativa pode ser consultada para histórico.

Os GET de seleção `/gardens`, `/sectors` e `/participants` preservam seu contrato e elegibilidade original. A interface não usa seletores ativos para administrar inativos. Estas três consultas não exigem nova migration ou seed e seguem o padrão de logs estruturados existente.

## Escrita, histórico e conflitos

- A criação retorna **201 + Location**. IDs de entidades são UUID v7 gerados pelo servidor. Não enviar autoria, data de registro ou versão inicial.
- Fatos exigem `operationId`: repetir a mesma operação e conteúdo normalizado retorna **200** com o registro atual; outro conteúdo na mesma chave retorna **409 record.operation_conflict**. Catálogos usam suas chaves definidas, sem deduplicar pessoas, espécies ou instituições por nome.
- PUT envia a representação completa dos campos editáveis, incluindo **null explícito** nos opcionais obrigatórios do contrato, mais `expectedVersion` e `reason`. Uma mudança efetiva incrementa a versão e grava antes/depois; ausência de mudança mantém versão e histórico. Versão desatualizada retorna 409.
- Invalidação exige versão/motivo e preserva o registro. Não há exclusão física. Cadastros inativos permanecem consultáveis, mas não aceitam novos vínculos.
- Dependentes válidos podem impedir mudanças contraditórias e invalidações: ciclo com fatos, plantio com avaliações/reposições, colheita com massa destinada, ocorrência com intervenções. Retorno **409 record.has_dependents** exige revisar os dependentes explicitamente.
- Invalidação/correção de lote de secagem recalcula saldo. `drying.insufficient_balance` indica destinação acima do aproveitável conhecido. GET da colheita mostra `allocatedMassKg` e `remainingUsableMassKg`; saldo desconhecido permanece null.
- Autoria técnica é resolvida pelo servidor. `responsibleParticipantId` representa quem executou o trabalho e é separado do autor demonstrativo. Nomes e descrições dos formulários não são registrados nos logs.
- Fato e auditoria são confirmados na mesma transação, incluindo listas de participantes, insumos, instituições e origens de secagem.
- Custo da água (12/11/2026, a pedido do responsável): `POST /irrigations` aceita `waterPricePerCubicMeterBrl` opcional (R$/m³, positivo, até 4 casas). Com volume conhecido, o servidor lança na mesma transação um custo realizado (`category` "Água", sem canteiro/ciclo), com valor `volumeLiters / 1000 × preço` arredondado a centavos e histórico próprio. Sem volume ou com valor zero, nada é lançado. O preço entra na chave de repetição somente quando informado. Correção ou invalidação da irrigação não altera o custo; ele é revisado em Custos. Custos de sementes e mudas continuam registrados em Custos, com vínculo opcional ao ciclo (`?cycleId=` pré-seleciona o ciclo no formulário).

## Listas e indicadores

Fatos exigem `from`/`to` em `yyyy-MM-dd`, inclusivos. Instantes usam o dia local de `America/Sao_Paulo`, convertido para um intervalo UTC semiaberto. Catálogos/históricos e fatos usam `page`/`pageSize`. Ordenação tem desempate por ID. Os filtros disponíveis estão no OpenAPI; enums são os nomes textuais exatos, sem códigos numéricos.

Indicadores usam apenas fatos válidos e retornam origens paginadas. O resumo cobre o recorte inteiro, mesmo com `pageSize=1`. Grupos de umidade/pegamento também são paginados, com seu próprio total. Disponibilidade: `NoData`, `Unavailable`, `Partial` ou `Available`; desconhecido não vira zero.

| Rota após `/gardens/{gardenId}/indicators` | Recorte e interpretação |
| --- | --- |
| `/water-consumption` | Início da irrigação; litros contados uma vez, situação e cobertura da duração separadas; sectorId opcional |
| `/soil-moisture` | Instante da coleta da variável com `isSoilMoisture=true`; pointId/unit/method; últimas observações do recorte por grupo, Normal/Questionable sinalizados; sem média global |
| `/seedling-establishment` | Dia do plantio; speciesId/kind/method; última avaliação válida até to, com método filtrado antes da seleção; ponderação por quantidades e grupos separados |
| `/productivity` | Dia da colheita; cycleId/speciesId; soma fresca / área ocupada por ciclo, sem repetir a área por colheita |
| `/harvest-losses` | Dia da colheita; cycleId/speciesId; fresca − aproveitável conhecido e taxa ponderada dos elegíveis |
| `/drying-yield` | Início do lote; lotId/speciesId; seco / destinado × 100 nos concluídos; lote misto não recebe massa seca atribuída por espécie |
| `/work-hours` | Dia da atividade; participantId/category; soma de horas individuais conhecidas, sem multiplicação por insumos/instituições |
| `/operational-occurrences` | Abertura; category/status; fatos distintos pelo estado atual, sem multiplicação por intervenções |
| `/social-educational-participation` | Dia da atividade; dimension=Social ou Educational/institutionId; ação multidimensional conta uma vez no total; público conhecido inclui o nominal; pessoas identificadas são distintas por ID |
| `/basic-costs` | Dia do lançamento; category/bedId/cycleId; previsto e realizado separados, diferença do recorte registrada com cobertura explícita |

## Persistência e decisões técnicas

As migrations foram geradas pelo EF CLI. Enums são nativos do PostgreSQL; relações locais usam FKs compostas com GardenId; valores têm precisão decimal explícita e checks locais. Vigências de área/setor têm exclusão GiST para impedir sobreposição, com `btree_gist`. Alteração de vigência não reclassifica irrigações, coletas ou áreas congeladas dos ciclos existentes.

Nesta implementação do protótipo, novas escritas de catálogo/fatos usam um advisory lock transacional comum, seguido do lock da horta, para serializar verificações de dependências entre módulos. Isso privilegia consistência; limita a concorrência de escrita e deverá ser refinado se a carga exigir. Irrigação mantém locks compartilhados da horta/canteiros e concorrência otimista entre edições. Destinações, correções e invalidações de secagem também bloqueiam as mesmas linhas de colheita em ordem de ID.

Auditoria de novos recursos guarda EntityType/EntityKey, horta quando local, autoria e snapshots escolhidos; a FK concreta anterior da irrigação foi preservada em IrrigationId com backfill. As relações de negócio usam FKs concretas. Não há endpoint de exclusão que deixe histórico órfão.

Os limites estruturais de listas de filhos protegem o tamanho do comando; não representam regras de capacidade da horta aprovadas pela PO. Não há seed, integração Grafana, sensores, estoque, rateio de massa seca/custos, contabilidade ou autenticação nesta entrega.

Consulte o [Get Started](../Get_Started.md) para preparar autoria e testar a sequência completa; [execução do backend](../planejamento/Execucao_Backend_MVP.md) para evidências e [observabilidade](Observabilidade.md) para acompanhar processos.

## Revisão de identificação — 09/10/2026

Por solicitação do responsável, `code` deixou de ser obrigatório nos comandos de criação/correção de horta, setor, canteiro, variável, ponto, origem de material e secagem. O campo permanece nas respostas e nos dados legados para compatibilidade. Quando omitido, os cadastros recebem seu próprio UUID; secagem deriva da operationId para manter o payload de retry estável. Correções preservam o código existente. Nomes não são chaves únicas. Variáveis expõem `isSoilMoisture`; o indicador não depende mais de um código digitado. A migration gerada pelo EF foi ajustada apenas para preservar a classificação legada, sem inserir dados de demonstração.
