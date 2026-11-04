# Observabilidade do backend

Implementação da seção 12 das [regras arquiteturais](../../.codex/arquitetura-backend.md). Serilog recebe ILogger dos handlers e adaptadores. Development usa console legível; a base conserva JSON compacto, com os mesmos eventos/propriedades. A API funciona sem servidor externo de logs.

Revisão de 08/10/2026: a leitura de `TesteTecnico.Api/Program.cs`/appsettings, `EasyDFe.Api/Infrastructure/Logging/SerilogConfiguration.cs`, `EasyDFe.Integrador/Infrastructure/Logging/IntegradorSerilogConfiguration.cs` e mensagens de handlers/consumidores orientou a adaptação. Foram aproveitados mensagens que descrevem ações, templates estruturados, SourceContext, enriquecimento da máquina/contexto e níveis coerentes. A Horta acrescenta identificação do processo e etapas para acompanhar seu percurso inteiro. Auditoria permanece transacional nos comandos; sinks Loki/OTLP, arquivos rotativos, filas e infraestrutura das referências não são dependências da aplicação.

O início agora aparece em Information. Handler, resumo HTTP e erro global compartilham ProcessId; a falha conserva a etapa em que ocorreu. As mensagens de registro, repetição sem duplicação, correção e invalidação descrevem o resultado concreto, além do evento estável. Não há mensagem artificial na entrada/saída de cada método.

## Como localizar um processo

`ProcessName` identifica o caso de uso ou operação técnica; `ProcessId` distingue uma execução. `TraceId` conecta HTTP, processo e resposta de erro. `SourceContext` aponta a classe emissora. Nos eventos de processo, `ElapsedMs` mede a duração; `ResultCount` conta itens conforme a operação. Não há processo de seed na aplicação.

| Campo | Leitura |
| --- | --- |
| Application / Environment / MachineName | Aplicação, ambiente e instância emissora |
| Module | Domínio ou operação técnica: Gardens, Irrigation, Indicators, Persistence, Host etc. |
| Phase | Received, Started, Executing, Completed, Rejected, Failed, Cancelled ou RequestCompleted |
| StepName / StepNumber | Etapa significativa e sua posição; falha/rejeição conserva a etapa corrente |
| EventName / Outcome / ErrorCode | Evento estável, resultado e código de rejeição/falha |

Uma tentativa nova recebe outro ProcessId; reenvios podem conservar OperationId e apontar à mesma IrrigationId. Os IDs de recurso podem estar no escopo ou no evento da etapa, conforme sua disponibilidade. TraceId pode reunir operações distribuídas e não substitui a identidade local. Etapas são marcadas antes da operação correspondente; mensagens de gravação concluída só aparecem após confirmação.

| ProcessName implementado | Origem |
| --- | --- |
| Gardens.ListGardens | Consulta de hortas |
| Gardens.ListIrrigationSectors | Consulta de setores |
| Gardens.ListBedsForIrrigation | Seleção histórica, com GardenId/SectorId/On validado |
| Activities.ListParticipants | Seleção de responsáveis, com GardenId |
| Irrigation.RegisterIrrigation | Inclusão/repetição, com GardenId/OperationId/SectorId |
| Irrigation.ListIrrigations | Listagem paginada, com GardenId/SectorId |
| Irrigation.GetIrrigation | Detalhe histórico, com GardenId/IrrigationId |
| Irrigation.CorrectIrrigation | Correção, com GardenId/IrrigationId/SectorId/ExpectedVersion |
| Irrigation.InvalidateIrrigation | Invalidação, com GardenId/IrrigationId/ExpectedVersion |
| Irrigation.ListIrrigationHistory | Histórico paginado, com GardenId/IrrigationId |
| Indicators.GetWaterConsumption | Resumo/cobertura e página de setores, com GardenId/SectorId |
| Persistence.ApplyMigrations | Migration opt-in em Development |
| Health.DatabaseReadiness | Indisponibilidade do PostgreSQL |
| Host.Startup | Inicialização da API |

Nomes das rotas acompanham os processos de consulta. O evento HTTP acrescenta RequestMethod, RoutePattern, StatusCode e Elapsed; a rota é um template estável para agrupamento. Campos variam conforme a origem: health/startup não possuem um recurso de negócio e não simulam um.

## Exemplo de evento

Exemplo ilustrativo de saída JSON, com identificadores fictícios e duração ilustrativa:

```json
{
  "@t": "2026-10-08T15:00:00.0000000Z",
  "@mt": "Processo {ProcessName} concluído em {ElapsedMs:0.000} ms",
  "Application": "HortaComunitaria.Api",
  "Environment": "Development",
  "MachineName": "MAQUINA-DE-TESTE",
  "SourceContext": "HortaComunitaria.Api.Features.Gardens.ListGardens.ListGardensQueryHandler",
  "ProcessName": "Gardens.ListGardens",
  "ProcessId": "01a11c0e-0d43-77e9-83f0-25404edc4cc5",
  "EventName": "ProcessCompleted",
  "Module": "Gardens",
  "Phase": "Completed",
  "StepName": "Querying",
  "StepNumber": 1,
  "Outcome": "Succeeded",
  "TraceId": "aabbccddeeff00112233445566778899",
  "SpanId": "1122334455667788",
  "RequestId": "request-demo",
  "ElapsedMs": 12.5,
  "ResultCount": 1,
  "ErrorCode": null
}
```

Compact JSON preserva o template em `@mt`, o instante UTC em `@t`, a exceção em `@x` quando há falha e o nível em `@l` quando diferente de Information. Trace/Span podem também aparecer nos campos nativos `@tr`/`@sp` da versão do Serilog; TraceId/SpanId continuam explícitos para consulta.

## Configuração e diagnóstico

O nível está em `Serilog:MinimumLevel` no appsettings. Início, etapas e término são visíveis em Information. Os overrides do framework/EF/Npgsql permanecem configurados; não habilite logging de SQL, parâmetros ou sensitive data.

O formato fica em `Observability:ConsoleFormat`: `Json` no appsettings base e `Text` no Development local. Pode sobrescrever com `$env:Observability__ConsoleFormat = 'Text'` ou `Json`, sem criar outro arquivo. Valores diferentes de Text/Json impedem a inicialização. A configuração central fica em `ObservabilityExtensions`; não adicionar outro sink Console em WriteTo, para evitar duplicação. O bootstrap usa texto antes da configuração do host estar disponível.

O console Text usa o sink nativo do Serilog com `AnsiConsoleTheme.Literate`, horário curto, nível e mensagem com parâmetros coloridos, como na aplicação de referência. Processo/etapa aparecem quando presentes. SourceContext completo, ambiente e EventName permanecem nas propriedades estruturadas, sem ocupar o prefixo de toda linha. ProcessId/TraceId completos aparecem em linha de apoio no resumo HTTP e em eventos Warning/Error/Fatal. Eventos de framework sem processo não exibem campos vazios.

O formatador personalizado anterior foi removido: o Serilog não aplica temas quando recebe um `ITextFormatter` próprio, conforme a [documentação do sink Console](https://github.com/serilog/serilog-sinks-console#json-output). `ConsolePresentationEnricher` seleciona apenas os campos visuais no sink de texto; a renderização e as cores ficam com o sink nativo. A saída JSON mantém os campos originais e não recebe propriedades auxiliares de apresentação.

Exemplo ilustrativo (as cores são aplicadas no terminal):

```text
[22:48:12 INF] [Gardens.ListGardens/ValidatingRequest] Iniciando processo Gardens.ListGardens
[22:48:12 INF] [Gardens.ListGardens/Querying] Consultando hortas ativas
[22:48:12 INF] [Gardens.ListGardens/Querying] Processo Gardens.ListGardens concluído em 4.558 ms
[22:48:12 INF] [Gardens.ListGardens/Querying] HTTP GET /api/v1/gardens respondeu 200 em 5.229 ms
    ProcessId=01a11c0e-0d43-77e9-83f0-25404edc4cc5 TraceId=aabbccddeeff00112233445566778899
```

Reinicie a API para aplicar a configuração ao logger. Em um terminal interativo compatível com ANSI, Text aplica as cores automaticamente. Redirecionamento para arquivo/pipe conserva texto sem escapes de cor; JSON continua sem cores. O teste de console força o tema apenas na captura de teste para verificar os códigos ANSI efetivamente emitidos. Os níveis e filtros de framework/SQL permanecem os definidos no appsettings.

Etapas de criação: ValidatingRequest → CheckingOperation → ResolvingContext → ResolvingAuthor → Persisting → LoadingResult; repetição passa por ReplayingOperation. Correção: ValidatingRequest → LoadingRecord → ResolvingContext → Persisting ou CheckingVersion → ConfirmingTransaction. Invalidação: ValidatingRequest → LoadingRecord → Persisting → ConfirmingTransaction. Consultas registram CheckingGarden/CheckingContext quando necessário e Querying; consumo registra Aggregating e LoadingSectorPage. Migrations registram ApplyingMigrations. Filtros nas etapas são valores já validados, sem query arbitrária.

Rejeição esperada retorna Result e ProcessRejected em Warning com ErrorCode, sem stack trace. Uma exceção inesperada durante o handler chega ao middleware global: há um evento UnhandledException com a exceção, última etapa, duração e ProcessId preservados, e um resumo HttpRequestCompleted em Error. A execução interrompida não produz ProcessCompleted/ProcessRejected; handlers não capturam e repetem a exceção. Se a resposta já começou, o erro é propagado e o middleware HTTP registra a exceção uma vez, sem outro stack trace no middleware global. HTTP 4xx fica em Warning, sucesso em Information; health saudável e cancelamento do cliente ficam em Debug. Cancelamento registra Outcome=Cancelled, sem tratá-lo como falha técnica. Startup usa ApplicationStarting/Started em Information e ApplicationStartupFailed em Fatal; indisponibilidade no readiness usa DependencyUnavailable em Warning, sem expor detalhes na resposta.

O domínio não recebe ILogger e não chama Serilog. A fronteira que coordena o processo registra suas decisões; não há mensagens artificiais em cada método ou duplicação de auditoria nos logs. Payloads, nomes de participantes, corpo, query string, cabeçalhos, arquivos e conexão não são copiados para eventos.

Os testes capturam os eventos pelo sink de integração e comprovam correlação W3C, identidade compartilhada e isolamento de requisições concorrentes, etapas/resultado/duração, cancelamento, ausência de query arbitrária, rejeição sem exceção e registro único da falha técnica com última etapa. Testes de console verificam texto compacto, ausência de campos vazios, cores ANSI efetivamente emitidas, correlação completa no resumo e propriedades originais no JSON.

Na escrita, IrrigationRegistered traz IrrigationId e Outcome=Created; IrrigationReplayed traz o mesmo recurso e Outcome=Replayed, sem gerar outra inclusão no histórico. Ambos pertencem ao ProcessId do handler e são seguidos por ProcessCompleted. Um payload diferente para a mesma operação gera ProcessRejected com irrigation.operation_conflict, sem exceção do provedor. A constraint disputada é tratada especificamente após rollback do SaveChanges.

O host mantém um logger próprio até registrar uma falha de startup e depois o descarta, sem substituir/fechar um logger estático compartilhado. Isso também permite múltiplos hosts de integração com sinks independentes. A exceção de startup é emitida antes de descartar o provider, preservando a causa original.

OperationId e IDs do contexto ajudam a identificar tentativas e fatos; notes, volumeMethod, responsável, CreationPayload e snapshots de auditoria não são copiados para logs. Os testes de irrigação conferem essa ausência, os eventos do comando/indicador e retries concorrentes sem exceção duplicada.

IrrigationCorrected registra Outcome=Corrected e Version; IrrigationCorrectionUnchanged registra Outcome=Unchanged e a mesma versão; IrrigationInvalidated registra Outcome=Invalidated e Version. Todos são emitidos após confirmação da transação, no processo da requisição. ListIrrigationHistory emite ProcessCompleted com duração e quantidade da página. Conflitos de versão ou estado produzem ProcessRejected com ErrorCode, sem exceção/stack trace e sem nova tentativa automática. Motivos de alteração, incluindo motivos rejeitados, não entram nos logs; ficam somente no histórico persistido. A suíte verifica essas propriedades, a correlação HTTP e a ausência de snapshots/textos privados nos eventos.

## Integração posterior

A saída Json permite coleta posterior por um agente e um destino de logs escolhido pela equipe; Text facilita a leitura local sem perder propriedades estruturadas. Nenhum sink remoto, stack Grafana/Loki, worker ou serviço adicional foi incluído. Quando essa integração for necessária, a configuração do provedor fica na infraestrutura; os handlers continuam usando ILogger e o mesmo contrato de eventos.



## Processos dos novos fluxos do backend

Os comandos e consultas de organização, Environment, Cultivation, Activities, Occurrences, Costs e Indicators seguem o mesmo ProcessLog. ProcessName inclui módulo/caso de uso e SourceContext identifica o handler concreto. Fatos incluem OperationId no registro; revisões incluem ResourceId e ExpectedVersion, além de GardenId local. A confirmação transacional emite `<CasoDeUso>Confirmed` com ResourceId e Version, sem conteúdos do formulário. HTTP e processo compartilham ProcessId/TraceId; rejeições preservam código/etapa e exceções técnicas ficam na fronteira global.

## Validação do console colorido — 08/10/2026

Build Release sem avisos e cinco testes de console aprovados. A captura confirmou códigos ANSI reais no formato Text, IDs completos no resumo HTTP, eventos de framework sem campos vazios e JSON parseável com propriedades estruturadas preservadas e sem escapes de cor. A configuração local já seleciona Text; reiniciar o processo da API aplica o novo logger.
