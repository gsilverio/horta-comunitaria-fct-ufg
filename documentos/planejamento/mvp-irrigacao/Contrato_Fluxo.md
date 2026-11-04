# Contrato do protótipo de irrigação

Leitura comum das quatro etapas. As [regras arquiteturais](../../../.codex/arquitetura-backend.md) definem organização, PostgreSQL, Result, erros, migrations e testes. Este contrato fixa o recorte funcional e as hipóteses para a demonstração. Fontes: [RF02, RF07, RF13 e RF14](../../Documento_Tecnico_Requisitos_Horta.md), [organização](../../arquitetura/Modelo_Organizacao_Horta.md), [irrigação](../../arquitetura/Modelo_Ambiente_Irrigacao.md), [histórico](../../arquitetura/Modelo_Acesso_Historico_Evidencias.md) e [indicadores](../../arquitetura/Dados_Indicadores.md).

## 1. Entidades que este fluxo utiliza

| Tipo C# / tabela | Conteúdo mínimo |
| --- | --- |
| `Garden` / `gardens` | Id, Code, Name, IsActive |
| `IrrigationSector` / `irrigation_sectors` | Id, GardenId, Code, Name, IsActive |
| `Bed` / `beds` | Id, GardenId, Code, Name, IsActive |
| `BedSectorAssignment` / `bed_sector_assignments` | Id, BedId, IrrigationSectorId, ValidFrom, ValidUntil? |
| `Participant` / `participants` | Id, GardenId, DisplayName, IsActive; somente seleção de responsável |
| `User` / `users` | Id, AuthenticationIdentifier, DisplayName, IsActive; somente autor técnico do protótipo |
| `Irrigation` / `irrigations` | Id, GardenId, IrrigationSectorId, campos do formulário, RecordedByUserId, RecordedAt, UpdatedAt?, RecordValidity, Version, OperationId, CreationPayload |
| `IrrigationBed` / `irrigation_beds` | PK (IrrigationId, BedId), BedSectorAssignmentId; nenhum volume |
| `AuditEntry` / `audit_entries` | Id, GardenId, UserId, EntityType, EntityKey, Operation, Reason?, OccurredAt, Before?, After? |

Não criar tabelas dos demais domínios nem `BedConfiguration`: dimensões não participam deste cálculo. `GardenId` na irrigação facilita escopo; sua consistência com o setor é obrigatória. O responsável é `ResponsibleParticipantId?`, separado do autor do registro. Valores atuais vivem no fato; histórico guarda snapshots dos campos e canteiros alterados, nunca outra irrigação para somar.

`User` contém somente o autor `demo:operator` definido nas regras, sem login ou CRUD. `Participant` é cadastro demonstrativo independente, sem dados pessoais reais. Não há endpoints de escrita desses cadastros nesta entrega; eles são preparados explicitamente no ambiente. O seed foi removido por decisão do responsável em 08/10/2026; consulte o [Get Started](../../Get_Started.md).

Campos de catálogo: código até 40 e nome até 120 caracteres, obrigatórios e normalizados com trim. Código único por horta (o de Garden é global). Vigências são datas locais `[ValidFrom, ValidUntil)`, com fim nulo para intervalo aberto. Não há edição desses intervalos nesta entrega; o preparo explícito e os testes devem impedir sobreposição por canteiro e preservar intervalos já referenciados.

## 2. Formulário e regras

| Campo JSON | Tipo e regra do protótipo |
| --- | --- |
| `sectorId` | UUID obrigatório, setor ativo da horta |
| `bedIds` | Array de UUIDs, pode estar vazio quando o atendimento individual não é conhecido; rejeitar duplicados |
| `startedAt` | Instante obrigatório com offset; persistir UTC |
| `endedAt` | Instante com offset ou null; quando conhecido, não anterior ao início |
| `activeDurationMinutes` | Decimal ou null, não negativo; se fim conhecido, não supera a duração transcorrida |
| `volumeLiters` | Decimal ou null, não negativo; zero é informação coletada |
| `flowRateLitersPerMinute` | Decimal ou null, não negativo; apenas informativo nesta entrega |
| `volumeSource` | `Measured`, `Estimated` ou null; obrigatório quando volume conhecido |
| `volumeMethod` | Texto até 200 ou null; obrigatório e não vazio quando volume conhecido |
| `status` | `Completed` ou `Interrupted`; sem controle de evento em andamento |
| `responsibleParticipantId` | UUID ou null; quando preenchido, participante ativo da horta |
| `notes` | Texto até 1000 ou null |

Volume, vazão e duração aceitam até três casas decimais (`numeric(14,3)`). Rejeitar excesso de escala/capacidade, sem truncar. Normalizar texto opcional vazio para null. Se volume desconhecido, source e method devem ser null; o cliente não inventa método, volume ou zero. Não calcular volume automaticamente a partir da vazão nem impor limites agronômicos sem validação.

Um evento pode atender vários canteiros, mas pertence a um setor. Cada canteiro escolhido deve estar ativo, ser da horta e possuir exatamente um vínculo com esse setor na data local de `startedAt`. Gravar a referência a esse vínculo. A consulta histórica usa o setor e os vínculos registrados, não o setor atual. Não exigir quatro canteiros ou cinco setores.

Correções usam as mesmas regras. Se mudar início, setor ou canteiros, resolver novamente os vínculos na nova data e auditar o contexto anterior. Não apagar eventos nem permitir corrigir registro invalidado; reativação fica fora do protótipo. Interrupção não significa invalidação: 20 L conhecidos de evento interrompido entram no consumo.

## 3. Consultas e indicador

Filtros de período usam `from` e `to` como datas locais inclusivas no fuso `America/Sao_Paulo`. Ambos são obrigatórios nas consultas de registros e consumo; `from <= to`. Transformar em intervalo UTC fechado no início e aberto no início do dia seguinte a `to`. Um evento pertence ao período pela data do início, mesmo atravessando dias; **essa é uma hipótese para validação, sem rateio diário**.

Listagem admite `sectorId?` e `validity?` (`Valid`/`Invalid`), default `Valid`. Consulta por canteiro e consumo por canteiro ficam para depois. O detalhe encontra também invalidado. Ordenação de eventos: `startedAt` decrescente, depois Id decrescente. Catálogos: Code/DisplayName crescente, depois Id. Paginação padrão 20, máximo 100, página >= 1; implementar uma vez no tipo comum.

**Consumo do protótipo:** somar `VolumeLiters` dos eventos válidos selecionados com volume conhecido, uma vez por evento. Não juntar canteiros antes da soma. A consulta aceita `sectorId?`; retorna um resumo do recorte e `bySector` como Page de linhas de setores ativos ou com eventos válidos no recorte (ordenadas por Code/Id). Paginação afeta só `bySector`, nunca o resumo. Linhas incluem setores sem eventos, para não sugerir consumo zero.

Cada resumo/linha tem `knownVolumeLiters`, `eventCount`, `eventsWithKnownVolume`, `eventsWithoutVolume`, `availability` e `includesEstimatedVolume`. Unidade `L`; informar também período e fuso. `availability` é:

| Situação | Resultado |
| --- | --- |
| Nenhum evento válido | `NoData`, volume null, contagens 0 |
| Eventos, nenhum volume conhecido | `Unavailable`, volume null |
| Parte dos eventos com volume | `Partial`, soma conhecida e contagens de cobertura |
| Todos os eventos com volume | `Available`, soma conhecida, inclusive 0 observado |

`includesEstimatedVolume` considera somente eventos válidos com volume conhecido e source `Estimated`. Não afirmar que a soma representa todo o consumo real da horta; representa os registros disponíveis. Calcular resumo, contagens e linhas em uma transação de leitura com snapshot consistente (`RepeatableRead`), sem cache ou tabela de indicador. O formulário, a listagem e o indicador são leituras distintas; após alteração a interface refaz as consultas.

## 4. Contratos HTTP

Prefixo: `/api/v1/gardens`. Todos os IDs de rota têm constraint Guid. Catálogos retornam `Page<T>`; só campos necessários à seleção, sem entidades EF.

| Método e caminho após o prefixo | Slice / resultado |
| --- | --- |
| `GET /` | `Gardens/ListGardens`: id, code, name de hortas ativas |
| `GET /{gardenId}/sectors` | `Gardens/ListIrrigationSectors`: id, code, name de setores ativos |
| `GET /{gardenId}/beds?on=YYYY-MM-DD&sectorId=UUID` | `Gardens/ListBedsForIrrigation`: id, code, name, assignmentId dos canteiros ativos com vínculo vigente; on e sectorId obrigatórios |
| `GET /{gardenId}/participants` | `Activities/ListParticipants`: id, displayName de participantes ativos |
| `POST /{gardenId}/irrigations` | `Irrigation/RegisterIrrigation`: formulário + operationId obrigatório; 201 e Location do detalhe |
| `GET /{gardenId}/irrigations` | `Irrigation/ListIrrigations`: filtros acima, Page de resumos incluindo id, sectorId/name, startedAt, volumeLiters, status, validity, version |
| `GET /{gardenId}/irrigations/{id}` | `Irrigation/GetIrrigation`: formulário salvo, id, canteiros/vínculos históricos, validade, versão e metadados de autoria/instantes |
| `GET /{gardenId}/indicators/water-consumption` | `Indicators/GetWaterConsumption`: filtros de período/setor, resumo e bySector |
| `PUT /{gardenId}/irrigations/{id}` | `Irrigation/CorrectIrrigation`: formulário completo + expectedVersion + reason; 200 com detalhe atualizado |
| `POST /{gardenId}/irrigations/{id}/invalidation` | `Irrigation/InvalidateIrrigation`: expectedVersion + reason; 200 com detalhe atualizado |
| `GET /{gardenId}/irrigations/{id}/history` | `Irrigation/ListIrrigationHistory`: Page de operações com motivo, antes/depois, autor demonstrativo e instante; ordenação instante decrescente/Id |

Histórico só desse tipo de registro, sem API genérica de auditoria. Metadados são atribuídos pelo servidor. Motivo obrigatório nas alterações: trim, 1–500 caracteres. `Version` inicia em 1 e incrementa em cada alteração; mapear como token de concorrência persistido. Data de criação e autor original não mudam.

Criação usa unicidade `(GardenId, OperationId)`. O servidor preserva `CreationPayload` imutável normalizado (sem metadados; IDs de canteiro ordenados, textos normalizados, instantes UTC) para comparação de repetição, inclusive após uma correção. Mesma chave/conteúdo retorna 200, Location e detalhe atual do evento; chave com conteúdo diferente retorna 409. Duas requisições concorrentes criam só um fato e uma entrada de inclusão no histórico. Tratar especificamente a constraint única, recarregar e comparar; não converter toda falha SQL em conflito.

Erros esperados seguem Result/ProblemDetails. Códigos mínimos: `validation.invalid_request` (400), `irrigation.invalid_period` (400), `irrigation.invalid_measurement` (400), `irrigation.invalid_bed_assignment` (400), `resource.not_found` (404), `irrigation.operation_conflict` (409), `irrigation.version_conflict` (409), `irrigation.record_invalid` (409). Campo/setor/participante de outra horta nunca é aceito; resolver no contexto e responder 404 para referência fora dele. Não criar 401/403 fictícios no modo demonstrativo.

Enums de negócio persistidos seguem ENUM nativo PostgreSQL conforme arquitetura; contratos usam os valores PascalCase listados. `RecordValidity`: Valid/Invalid; `AuditOperation`: Created/Corrected/Invalidated. Disponibilidade do indicador é enum de contrato calculado, não coluna/tipo PostgreSQL.

## 5. Dados de demonstração e resultados esperados

Sem seed na aplicação. Para o roteiro, preparar explicitamente uma horta fictícia, setores S1/S2, quatro canteiros B1–B4 vinculados a S1 desde 01/10/2026 sem fim, um participante fictício e o autor técnico conforme o [Get Started](../../Get_Started.md), ou usar contexto já existente. Migrations não inserem cadastros/fatos. Os testes preparam suas próprias fixtures somente em bancos descartáveis, incluindo segunda horta e mudanças de vínculo. Não usar as quantidades preliminares como dados reais.

Usar 08/10/2026 às 08:00–08:30 `-03:00`, 30 minutos ativos, S1 e B1–B4. Datas selecionadas na interface são editáveis; os testes usam relógio controlado. Os exemplos abaixo são cenários separados salvo a sequência expressamente indicada.

| Cenário | Resultado esperado |
| --- | --- |
| Um evento medido de 120 L, quatro canteiros | S1 e resumo = 120 L, um evento; S2 = NoData |
| Reenviar essa criação com a mesma operationId | Mesmo ID, um fato, uma inclusão no histórico, 120 L |
| Mesmo recorte com outro evento sem volume | Partial, 120 L conhecidos, 2 eventos, 1 sem volume |
| Só um evento de 0 L conhecido | Available, 0 L, um volume conhecido |
| Só um evento sem volume | Unavailable, volume null |
| Nenhum evento | NoData, volume null |
| Evento Interrupted com 20 L | Available, 20 L |
| Corrigir o evento de 120 para 100 L | Total 100 L, versão 2, histórico conserva 120 → 100 |
| Invalidar esse evento corrigido | NoData quando era o único; versão 3, fato e histórico preservados |

As quantidades, precisão, datas de recorte, classificação dos volumes e campos obrigatórios são decisões do protótipo para discussão. Não há autorização de operação real ou aprovação automática da PO.
