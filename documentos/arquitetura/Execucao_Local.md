# Execução local da aplicação

Estado atual: backend dos fluxos 02–09 implementado, com interfaces pendentes, RF12 adiado e RF01 pós-MVP. Para contratos, preparação e testes atuais, consulte [Get Started](../Get_Started.md), [contrato do backend](Contrato_Backend_MVP.md) e [evidências](../planejamento/Execucao_Backend_MVP.md). As verificações antigas abaixo permanecem como registro histórico.

O backend implementa as etapas [1 — contexto](../planejamento/mvp-irrigacao/Etapa_01_Contexto.md), [2 — registro e consumo](../planejamento/mvp-irrigacao/Etapa_02_Registro_Consumo.md) e [3 — correção e histórico](../planejamento/mvp-irrigacao/Etapa_03_Correcao_Historico.md): catálogos demonstrativos, registro manual, listagem, detalhe, correção, invalidação, histórico e consumo de água. A [etapa 4](../planejamento/mvp-irrigacao/Etapa_04_Interface_PO.md) conecta esse percurso à interface React/Tailwind. As hipóteses funcionais continuam sujeitas à PO.

## Projetos e versões

O projeto existente `HortaComunitaria.Api/HortaComunitaria.Api.csproj` é o executável do backend; `tests/HortaComunitaria.Api.IntegrationTests` contém a integração. A solução existente fica em `HortaComunitaria.Api/HortaComunitaria.Api.slnx`. O template de Controllers/WeatherForecast foi substituído por Minimal APIs. `hortacomunitaria-webapp` contém o frontend e seus testes Playwright.

| Componente | Versão fixada |
| --- | --- |
| SDK .NET | 10.0.301, sem roll-forward em global.json |
| ASP.NET Core OpenAPI / host de testes | 10.0.9 |
| EF Core / Relational / Design / dotnet-ef | 10.0.9 |
| Npgsql EF Core | 10.0.3 |
| PostgreSQL local / imagem de testes | 17.0 |
| Serilog.AspNetCore / Compact JSON | 10.0.0 / 3.0.0 |
| Swagger UI | 10.3.0 |
| Microsoft.OpenApi | 2.7.5 |
| xUnit / runner / Microsoft.NET.Test.Sdk | 2.9.3 / 3.1.4 / 17.14.1 |
| Testcontainers.PostgreSql | 4.15.0 |

Os pacotes transitivos estão em `packages.lock.json`, por projeto; o manifest local `dotnet-tools.json` fixa o CLI. Microsoft.OpenApi foi fixado explicitamente na versão corrigida indicada pelo [advisory oficial](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc), sem suprimir a auditoria NuGet.

## PostgreSQL demonstrativo

O [panorama de cultivos](Cultivos_no_Panorama.md) usa a migration `AddVisualGrowthTiming`. Aplicar migrations pelo fluxo habitual antes de usar a nova consulta; prazos opcionais são configurados nos detalhes de espécie/cultivar. Não há prazos agronômicos automáticos no seed.

O [catálogo Embrapa](Catalogo_Embrapa.md) tem uma importação administrativa explícita autorizada em 24/10/2026, via `--seed-embrapa`. Startup HTTP continua sem seed automático; os relatos anteriores de remoção de seed descrevem a decisão de 08/10.

Decisão de 08/10/2026: seed removido da aplicação. As evidências históricas abaixo registram verificações anteriores à remoção; para execução atual e testes, seguir o [Get Started](../Get_Started.md).

Validação da remoção: 126 testes de integração aprovados com PostgreSQL 17 local, incluindo inicialização sem hortas/setores/canteiros/vínculos/participantes/autores automáticos. As fixtures passaram a preparar dados explicitamente em bancos `horta_test_`, sem usar serviço de seed da aplicação. Build/lint e 12 testes Playwright com API/PostgreSQL reais aprovados; o setup E2E confirma banco vazio antes de criar seu contexto exclusivo. O SQL do Get Started foi executado em banco descartável com as duas migrations: uma horta, dois setores, quatro canteiros/vínculos, um participante e um autor técnico, sem fatos automáticos. Os bancos/processos de validação foram descartados/encerrados.

Na primeira execução, os casos funcionais passaram, mas o descarte de duas classes excedeu o timeout padrão durante checkpoints do PostgreSQL local. A limpeza das fixtures usa agora prazo de 180 segundos por descarte; a suíte completa foi repetida com sucesso, sem falha de cleanup. Os bancos deixados pela primeira tentativa também foram removidos, preservando `horta_demo`.

### Instância instalada localmente

É possível usar o PostgreSQL **17** já instalado, sem iniciar outro servidor. Nesta máquina, o serviço `postgresql-x64-17` usa a porta **5432**; o PostgreSQL 16 usa **5433** e não é a versão adotada para esta aplicação.

Use apenas `HortaComunitaria.Api/appsettings.json` como base e `HortaComunitaria.Api/appsettings.Development.json` para o desenvolvimento. O segundo guarda a conexão local com credenciais e é ignorado pelo Git. Nesta máquina já está configurado. Em outra instalação, crie/edite esse arquivo com sua conexão e os opt-ins demonstrativos:

```json
{
  "Observability": { "ConsoleFormat": "Text" },
  "DemoMode": {
    "Enabled": true
  },
  "Persistence": {
    "ApplyMigrationsOnStartup": true,
    "ConnectionString": "Host=127.0.0.1;Port=5432;Database=horta_demo;Username=SEU_USUARIO;Password=SUA_SENHA_LOCAL"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

As credenciais reais ficam somente no arquivo local ou no ambiente. O usuário precisa acessar o banco e executar as migrations nele. Crie `horta_demo` na instância se ainda não existir; não use um banco de outra aplicação. Com `Persistence:ApplyMigrationsOnStartup=true`, permitido somente em Development, o startup aplica as migrations existentes, sem inserir dados. Não cria um novo servidor nem muda sua autenticação. Também é possível manter false e aplicar as migrations pelo EF CLI conforme abaixo.

O frontend continua usando o proxy para a API na porta 5163; a conexão PostgreSQL pertence ao backend. Não colocar usuário/senha do banco em variáveis `VITE_*`.

Configuração validada nesta máquina em 08/10/2026: `127.0.0.1:5432`, PostgreSQL **17.0 do Windows**, banco dedicado `horta_demo`, usuário `postgres`, credencial somente no `appsettings.Development.json` ignorado. O banco foi criado vazio; as migrations `InitialContext` e `IrrigationRegistration` foram aplicadas e o seed demonstrativo executado. Build Release terminou sem erros/avisos; readiness respondeu 200 e as consultas retornaram uma horta, dois setores, quatro canteiros elegíveis e consumo NoData, sem fatos de irrigação artificiais. Há também um container publicando a porta 5432; a identificação acima foi conferida pela conexão SQL real, sem alterar os serviços existentes.

### Alternativa com Docker Compose

Na raiz, copie `.env.example` para `.env`, defina a senha local e execute:

```powershell
docker compose up -d postgres
```

O compose contém apenas PostgreSQL, na porta 54329 do loopback. Também é possível usar PostgreSQL 17 instalado localmente, em um banco dedicado. Não reutilize dados operacionais para esta demonstração.

Para o Compose, ajuste `Persistence:ConnectionString` no `appsettings.Development.json` para a porta **54329**, banco/usuário **horta_demo** e a senha definida no `.env`. Preserve ou ajuste deliberadamente o destino da configuração existente antes de executar. Development e `.env` são ignorados pelo Git; não há arquivo adicional de configuração da API.

| Configuração | Comportamento |
| --- | --- |
| DemoMode:Enabled | Opt-in obrigatório; somente Development |
| Persistence:ConnectionString | Conexão local obrigatória |
| Persistence:ApplyMigrationsOnStartup | Default false; true somente Development |

A API recusa startup fora de Development ou sem modo demo explícito. O host usa o carregamento padrão: `appsettings.json`, arquivo do ambiente e variáveis de ambiente, que prevalecem. O EF CLI lê os mesmos arquivos a partir da raiz ou da pasta do projeto; selecione Development com `DOTNET_ENVIRONMENT` (ou `ASPNETCORE_ENVIRONMENT`). Sem variável de ambiente, a factory usa Production e exige conexão fornecida para esse ambiente. Nunca inclua conexão/senha na linha de comando versionada. O arquivo base mantém modo demo e migrations automáticas desabilitados. Não há configuração de seed.

A simplificação dos arquivos foi verificada em 08/10/2026: build Release sem erros/avisos, sete testes de inicialização/health/logs aprovados com bancos PostgreSQL descartáveis, EF CLI consultando as migrations aplicadas tanto pela raiz quanto pela pasta da API e readiness HTTP 200 usando Development. O seed existente permaneceu idempotente. O encerramento intencional do host pelo tooling EF não é registrado como falha de startup. A API usada na verificação foi encerrada; o banco e o serviço PostgreSQL foram preservados.

## Restore, migration e execução

Execute na raiz:

```powershell
dotnet restore HortaComunitaria.Api/HortaComunitaria.Api.slnx --locked-mode
dotnet tool restore
$env:DOTNET_ENVIRONMENT='Development'
dotnet ef database update --project HortaComunitaria.Api --startup-project HortaComunitaria.Api
dotnet run --project HortaComunitaria.Api --launch-profile http
```

A API usa a porta 5163 do perfil existente. Acesse [Swagger](http://localhost:5163/swagger/index.html), [OpenAPI](http://localhost:5163/openapi/v1.json), [liveness](http://localhost:5163/health/live) e [readiness](http://localhost:5163/health/ready).

As migrations InitialContext e IrrigationRegistration foram geradas pelo EF CLI. A segunda adiciona irrigations, irrigation_beds e audit_entries sem apagar dados dos catálogos. Para futuras alterações, use os caminhos reais:

```powershell
dotnet ef migrations add NomeDaMigration --project HortaComunitaria.Api --startup-project HortaComunitaria.Api --context AppDbContext --output-dir Infrastructure/Migrations
dotnet ef migrations script --project HortaComunitaria.Api --output tmp/migrations.sql
```

Revise o SQL antes de aplicar. O startup só aplica migrations quando explicitamente habilitado. Não insere cadastros ou fatos; consulte o [Get Started](../Get_Started.md) para preparar explicitamente o contexto e testar as features disponíveis.

Sem seed na aplicação: dados da demonstração anterior permanecem no banco; remover o código não os apaga. Hortas/setores/canteiros/participantes ainda têm somente consultas HTTP, e sua criação/manutenção está pendente nas próximas entregas do MVP.

## Frontend local

Com PostgreSQL e API demonstrativa iniciados conforme acima, use Node **24.15.0** e npm **11.12.1**. Em outro terminal, na raiz:

```powershell
cd hortacomunitaria-webapp
npm ci
npm run dev
```

Acesse [http://127.0.0.1:5173](http://127.0.0.1:5173). O proxy Vite encaminha `/api` para `http://127.0.0.1:5163`; o browser usa a própria origem. `.env.example` do frontend documenta `VITE_API_BASE_URL` e `API_PROXY_TARGET`, sem segredos. O [README do frontend](../../hortacomunitaria-webapp/README.md) detalha configuração, versões, comandos e rotas consumidas.

React 19.2.8, TypeScript 6.0.2, Vite 8.3.0 e Tailwind CSS 4.3.3 são fixados no package.json/lockfile. Tailwind usa o plugin oficial de Vite, tokens em `@theme` e utilitários responsivos nos componentes, seguindo a [arquitetura do frontend](../../.codex/arquitetura-frontend.mdc). A UI oferece o fluxo completo da irrigação, com autoria demonstrativa, unidades e horários em America/Sao_Paulo. Os totais e o histórico vêm da API.

Para verificar a interface nesta pasta:

```powershell
npm run build
npm run lint
npx playwright install chromium
$env:HORTA_E2E_POSTGRES_URL='postgres://USUARIO:SENHA@127.0.0.1:PORTA/postgres'
npm run test:e2e
```

Substitua a conexão no ambiente por uma conta de um PostgreSQL **17** local dedicado a testes, com permissão de criar bancos. A API precisa estar restaurada com `--locked-mode`. A suíte cria/removerá somente seu próprio banco `horta_e2e_<uuid>`, inicia API em **5279** e preview em **5183** e encerra esses processos no teardown. Não depende do banco demonstrativo manual nem de servidores já iniciados. Chromium usa Asia/Tokyo para verificar a conversão de fuso; logs, screenshots e relatórios são ignorados pelo Git.

O [roteiro de demonstração](../planejamento/Validacao_PO_MVP_Irrigacao.md) está pronto; a reunião e o aceite funcional continuam pendentes.

## Consultas

| Método e rota | Seleção |
| --- | --- |
| GET /api/v1/gardens | Hortas ativas |
| GET /api/v1/gardens/{gardenId}/sectors | Setores ativos da horta |
| GET /api/v1/gardens/{gardenId}/beds?on=2026-10-08&sectorId={sectorId} | Canteiros ativos no vínculo vigente |
| GET /api/v1/gardens/{gardenId}/participants | Responsáveis ativos da horta |

Use os IDs das hortas e setores cadastrados pela interface ou pelo [Get Started](../Get_Started.md). A horta fictícia de demonstração e seus cadastros foram removidos do banco local em 09/10/2026, por solicitação do responsável; a horta `horta1` foi preservada. Não havia fatos nem histórico vinculados à horta removida. O arquivo HTTP usa variáveis para os IDs dos cadastros escolhidos.

Todas as consultas retornam Page, com page/pageSize e contagens; o contrato preserva a propriedade JSON `page`. O tipo CLR usa `PageNumber`, pois C# não permite um membro `Page` dentro de `Page<T>`. Data/setor são obrigatórios na seleção de canteiros. Erros possuem code/traceId; validações de campos também possuem errors. Há exemplos no arquivo HTTP do projeto.

O vínculo histórico guarda GardenId para reforçar a mesma horta por FKs compostas com Bed e IrrigationSector; não existe entidade adicional para isso. Não há cadastro de vínculos pela API nesta etapa. A factory rejeita sobreposições nos fixtures/seed; qualquer futura escrita concorrente precisa da proteção transacional prevista no plano.

## Registro e consumo

| Método e rota após /api/v1/gardens/{gardenId} | Resultado |
| --- | --- |
| POST /irrigations | 201 + Location + detalhe; repetição normalizada: 200; conteúdo diferente com a mesma operationId: 409 |
| GET /irrigations?from=2026-10-08&to=2026-10-08 | Page de resumos; sectorId opcional, validity Valid/Invalid, default Valid |
| GET /irrigations/{id} | Detalhe, inclusive inválido, com canteiros/vínculos e autoria demonstrativa |
| PUT /irrigations/{id} | Formulário completo + expectedVersion + reason; 200 detalhe; conflito de versão/invalidado: 409 |
| POST /irrigations/{id}/invalidation | expectedVersion + reason; 200 detalhe invalidado; conflito: 409 |
| GET /irrigations/{id}/history?page=1&pageSize=20 | Page de inclusões/alterações/invalidação, autor, motivo e snapshots antes/depois |
| GET /indicators/water-consumption?from=2026-10-08&to=2026-10-08 | Resumo do recorte inteiro e bySector paginado; sectorId opcional |

Use o [arquivo HTTP](../../HortaComunitaria.Api/HortaComunitaria.Api.http) ou Swagger para registrar 120 L medidos no S1, B1–B4, em 08/10/2026 às 08:00–08:30 -03:00. O exemplo define uma operationId v7 para permitir reenvio: preserve-a ao repetir o envio e gere outra para outro evento legítimo. O POST retorna o id e Location para consultar a origem.

Instantes de entrada exigem offset e são persistidos/retornados em UTC. Volume (L), vazão (L/min) e duração ativa (minutos) aceitam até três casas decimais e 11 dígitos inteiros; excesso é rejeitado antes de gravar. Volume desconhecido usa null em volumeLiters, volumeSource e volumeMethod; zero conhecido exige origem e método. A autoria sempre vem de demo:operator no servidor, separada do responsável opcional selecionado.

O indicador soma uma vez o volume de cada evento válido, incluindo Interrupted. No exemplo, resumo e S1 mostram 120 L e um evento, mesmo com quatro canteiros; S2 mostra NoData e volume null. Com pageSize=1, S2 aparece na segunda página e o resumo continua 120 L. As situações NoData, Unavailable, Partial e Available distinguem ausência, desconhecido e zero observado; includesEstimatedVolume informa a presença de estimativas válidas.

from/to são datas locais inclusivas em America/Sao_Paulo; o evento entra pelo dia do início, mesmo atravessando meia-noite. O indicador lê resumo, contagens e página na mesma transação RepeatableRead. Requisições distintas podem observar estados diferentes; recarregue as consultas após gravar. Não há rateio por dia/canteiro nem cálculo automático pela vazão.

Irrigação, filhos e snapshot Created são confirmados em um SaveChanges transacional. O índice único ux_irrigations_garden_operation protege retries simultâneos; somente essa violação é tratada como disputa da operação. CreationPayload jsonb guarda a entrada inicial normalizada e imutável no EF: instantes UTC, medidas normalizadas, textos com trim e canteiros ordenados. Comparação estrutural tolera a ordenação das propriedades pelo jsonb. Metadados não participam do payload nem são aceitos como autoria do cliente.

PostgresEnums centraliza MapEnum para host, EF CLI e testes: irrigation_status (completed/interrupted), volume_source (measured/estimated), record_validity (valid/invalid) e audit_operation (created/corrected/invalidated). Os contratos JSON mantêm PascalCase e rejeitam enums numéricos. A propriedade C#/coluna Validity utiliza o enum RecordValidity. Disponibilidade é calculada, sem enum/tabela persistidos.

As FKs compostas protegem horta, setor, canteiro e vínculo histórico. O filho referencia o fato por GardenId/IrrigationId e o vínculo por GardenId/BedId/IrrigationSectorId/AssignmentId. Factory/handler garantem que os filhos usam o setor do fato; IrrigationSectorId não integra uma chave alternativa do fato, pois o EF considera propriedades de chave imutáveis e a correção permite alterar o setor. AuditEntry nesta entrega referencia concretamente irrigations por GardenId/EntityKey; não é uma auditoria polimórfica genérica. Isso impede histórico órfão e exclusão do fato auditado. Antes de auditar outros domínios, será necessário definir seus vínculos sem remover essa integridade. Version inicia em 1 e é token de concorrência.

## Correção, invalidação e histórico

O PUT exige todos os campos do formulário, incluindo campos opcionais explicitamente como null, mais expectedVersion positivo e reason com 1–500 caracteres após trim. Não recebe operationId nem autoria. Revalida contexto e vínculos pelo dia local corrigido; preserva id, horta, autor/instante de inclusão e CreationPayload. Correção efetiva incrementa a versão uma vez e atualiza UpdatedAt; formulário normalizado igual retorna 200 sem alteração, incremento ou entrada de histórico. As resoluções de contexto usadas na criação e correção ficam no mesmo componente concreto; um handler não chama outro.

A invalidação preserva os valores e canteiros, marca Invalid e incrementa a versão. O detalhe e o histórico continuam disponíveis, assim como a listagem com validity=Invalid; o consumo e a listagem padrão desconsideram esse registro. Versão desatualizada retorna 409 com irrigation.version_conflict; invalidado com a versão atual retorna 409 com irrigation.record_invalid. Após corrigir ou invalidar, repetir o POST inicial com a mesma operationId e o conteúdo original retorna o estado atual, sem novo histórico.

As alterações executam um SaveChanges na mesma transação ReadCommitted que confirma o fato, filhos e AuditEntry. Falha no histórico reverte tudo; DbUpdateConcurrencyException produz rollback e 409 específico, sem retry automático. O UPDATE do fato verifica a versão original e mantém o lock até concluir a projeção da resposta e confirmar a transação. A correção sem mudança verifica a versão atual com SELECT FOR UPDATE, sem UPDATE artificial: uma disputa também pode retornar 409. Filhos mantidos atualizam vínculo/setor preservando sua chave; somente canteiros removidos/incluídos são excluídos/adicionados.

Detalhe fora da transação de escrita e histórico paginado usam RepeatableRead para manter fato/filhos e contagem/página no mesmo snapshot. O histórico pertence à horta/irrigação selecionada, ordenado por occurredAt/id descendentes; before/after são objetos tipados, com valores, versão, validade e IDs de canteiros/vínculos. Created tem before=null. A autoria demonstrativa vem do servidor e permanece consultável mesmo após desativação do usuário. Não há rota de edição do histórico, exclusão do fato ou reativação.

A precisão temporal segue o armazenamento PostgreSQL, de um microssegundo, enquanto .NET representa frações de 100 ns ([Npgsql](https://www.npgsql.org/doc/types/datetime.html)). Instantes de correção, metadados do relógio e snapshots são normalizados nessa precisão para evitar alterações artificiais e diferenças entre resposta e auditoria. O CreationPayload inicial continua preservando sua normalização UTC original, inclusive frações enviadas, para manter a compatibilidade dos retries já registrados; nunca é reconstruído a partir do estado corrigido. A precisão numérica permanece validada, sem arredondamento de medidas.

O gerador automático de comentários XML do OpenAPI 10.0.9 apresentou chaves duplicadas ao documentar Page<T> também aninhado no indicador. A aplicação usa nome de documento em variável para evitar essa interceptação, conforme [comportamento documentado pela Microsoft](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/openapi-comments?view=aspnetcore-10.0#enable-xml-documentation-in-an-aspnet-core-api-project). O XML continua gerado e validado na compilação; summaries, descriptions, formatos, campos obrigatórios e enums são definidos/conferidos explicitamente nos contratos HTTP. Há [relato equivalente no ASP.NET Core](https://github.com/dotnet/aspnetcore/issues/64378); a limitação foi reproduzida localmente nesta versão, sem atualização de pacotes para contorná-la.

## Verificação

```powershell
dotnet build HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --no-restore
dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --no-build --no-restore
```

Por padrão, os testes iniciam PostgreSQL 17.0 via Testcontainers. Sem Docker ativo, defina `HORTA_TEST_POSTGRES` no processo com uma conexão a um servidor PostgreSQL 17 dedicado a testes e permissão de criar bancos. A suíte cria bancos com prefixo `horta_test_` e UUID v7, aplica as migrations e remove somente esses bancos no teardown. Não usa EF InMemory.

Os bancos descartáveis usam Pooling=false para não reter um pool ocioso por caso até o teardown. A API normal mantém a configuração padrão do provedor. Interceptores exclusivos dos testes coordenam disputas de criação/alteração e gravação durante leituras do indicador/detalhe, sem sleeps ou endpoints de teste. O relógio dos testes é controlado.

Etapa 1, em 08/10/2026: restore com `--locked-mode`, build Release sem warnings e 29 testes aprovados com PostgreSQL 17.0 nativo, numa instância temporária de loopback na porta 55439 e cluster isolado. SQL revisado: seis tabelas, UUID/date, FKs RESTRICT, chaves únicas, check de intervalo e FKs compostas. `has-pending-model-changes` confirmou que o snapshot corresponde ao modelo.

O host HTTP real também foi iniciado num banco vazio com migration/seed opt-in. Consultas retornaram uma horta e quatro canteiros; health checks responderam 200 e paginação inválida retornou 400 com traceId igual a X-Trace-Id. Os eventos JSON do startup, seed e consultas foram conferidos no console. A demonstração não depende dessa instância temporária, encerrada após os testes.

Etapa 2, em 08/10/2026: **84 testes aprovados (zero falhas/ignorados)**, restore com `--locked-mode`, build Release sem warnings, SQL da migration IrrigationRegistration revisado e snapshot sem mudanças pendentes. A suíte cobre criação/histórico atômicos, rollback controlado, retries concorrentes, precisão, enums, contexto entre hortas, troca histórica de setor, bordas locais, cobertura/estimativas, paginação e snapshot consistente do indicador. OpenAPI e eventos Serilog também foram verificados. Evidência automatizada: `tests/HortaComunitaria.Api.IntegrationTests/TestResults/stage2-accepted.trx`, ignorada pelo Git.

O smoke HTTP com a migration final confirmou criação 201, retry 200, conflito 409 com code/traceId, detalhe/listagem e 120 L para quatro canteiros. Paginar bySector manteve 120 L no resumo e retornou S2=NoData na segunda página. PostgreSQL confirmou um fato, quatro filhos e uma inclusão. OpenAPI/liveness/readiness responderam 200; o console JSON manteve processos, recursos e traces identificáveis, sem exceções. API e cluster usados somente nessa verificação foram encerrados; configuração pessoal do repositório não foi alterada.

Etapa 3, em 08/10/2026: **126 testes aprovados (zero falhas/ignorados)**, restore com `--locked-mode` e build Release sem warnings. `has-pending-model-changes` confirmou o modelo sem nova migration: versão, UpdatedAt, validade, snapshots e enum de operações já estavam no schema. A suíte verificou correção/invalidação/histórico, rollback da auditoria e filhos, conflitos concorrentes, correção sem mudança, precisão temporal, contratos OpenAPI, logs e compatibilidade do retry inicial. Evidência automatizada: `tests/HortaComunitaria.Api.IntegrationTests/TestResults/stage3-verified.trx`, ignorada pelo Git.

O smoke HTTP confirmou 120 L → correção para 100 L → invalidação/NoData, versões 1/2/3 e três entradas de histórico. A correção igual manteve a versão 2; versão antiga retornou 409 com code/traceId; retry original retornou a versão 3 invalidada. OpenAPI respondeu 200 e o console JSON preservou os novos processos e traces, sem exceções. API, banco e cluster temporários foram encerrados após a verificação.

Etapa 4, em 08/10/2026: instalação reproduzível com `npm ci` (auditoria sem vulnerabilidades), lint e build de produção/TypeScript estrito aprovados, incluindo a configuração e os testes. **12 testes aprovados, zero falhas**, após a integração/refatoração para Tailwind CSS 4.3.3. O setup compilou a API Release e usou PostgreSQL 17 nativo em cluster isolado, com banco exclusivo e hosts HTTP reais. Foram verificados 120 L → 100 L → invalidação/NoData, quatro canteiros sem multiplicar volume, histórico/autoria, reload persistido, desconhecido/zero/estimativa, resposta de criação perdida com retry idempotente, conflitos reais com rascunho preservado, correção idêntica com microssegundos, catálogo de 101 canteiros, mudança de contexto, resposta antiga e recuperação de consulta. Conversão de fuso foi exercitada em Chromium/Asia/Tokyo e nas bordas históricas do horário de verão.

Screenshots `hortacomunitaria-webapp/test-results/mobile-form.png` e `desktop-dashboard.png` foram inspecionados visualmente após a refatoração; formulário de 390 px sem rolagem horizontal, labels/unidades legíveis e navegação por teclado com foco visível. Relatório em `hortacomunitaria-webapp/playwright-report/`, ignorado pelo Git. Os hosts e o banco E2E foram encerrados/removidos pela suíte; o cluster temporário foi encerrado depois da verificação. O [registro da PO](../planejamento/Validacao_PO_MVP_Irrigacao.md) permanece pendente, sem aceite fictício.

O Docker estava sem engine ativa; o ciclo de vida do container/compose não foi executado nesta máquina. As migrations, consultas e restrições foram efetivamente exercitadas no PostgreSQL real pelo caminho alternativo documentado. Testes técnicos não representam aprovação funcional da PO.

A estrutura de eventos e as configurações do console estão descritas em [Observabilidade](Observabilidade.md), seguindo a fonte única de [regras arquiteturais](../../.codex/arquitetura-backend.md).

Revisão de logs, em 08/10/2026: **130 testes da regressão aprovados, zero falhas/ignorados**, com PostgreSQL 17 local. Depois do ajuste final de apresentação, os **três testes de console Text/Json foram aprovados** (dois já pertenciam à regressão; 131 casos distintos validados). A compilação Release não produziu erros ou avisos. Foram verificados identidade compartilhada entre handler/HTTP/erro, isolamento de requisições concorrentes, cancelamento, última etapa preservada em falhas e registro único da exceção. A API real foi iniciada em portas temporárias nos dois formatos: readiness/consulta responderam 200 e os quatro eventos da consulta conservaram ProcessId/TraceId. Somente consultas foram executadas no banco da aplicação, com migrations automáticas desabilitadas. Os dois processos de verificação foram encerrados e não restaram bancos `horta_test_`/`horta_e2e_`. Nenhum coletor ou sink remoto foi instalado.


## Prévia completa do frontend — 08/10/2026

FE00–FE09 e FE11 foram implementados, com navegação, criação/manutenção dos 21 novos recursos, fatos, ações de ciclo/secagem/ocorrência, histórico e dez consultas de indicadores. RF12 continua adiado; RF01 continua pós-MVP. O [Get Started](../Get_Started.md) agora inclui o percurso completo pela interface, começando pela primeira horta sem seed.

A suíte integrada aprovou **20 testes E2E, zero falhas**, com Chromium/Asia-Tokyo, API real e PostgreSQL 17 em banco descartável. Build de produção/TypeScript estrito e lint aprovados. As três consultas administrativas necessárias para gerir inativos também foram verificadas em **um teste de integração aprovado**, com filtros, versões, paginação e contextos. Detalhes, evidências visuais e limites estão na [execução do frontend](../planejamento/Execucao_Frontend_MVP.md). Os testes preservam o banco configurado para a aplicação. Aceite funcional da PO permanece pendente.

### Navegação e validação da revisão dos fluxos

O frontend abre a lista de hortas, sem selecionar a primeira automaticamente. Cadastre ou entre em uma horta para acessar seus módulos e o Dashboard integrado. Para preparar cadastros, use Horta → Preparação; o roteiro do [Get Started](../Get_Started.md) percorre os links reais da primeira horta até os registros. A [revisão de usabilidade](../planejamento/Revisao_Fluxos_Frontend_MVP.md) detalha dependências e mudanças. A suíte atual inclui 22 cenários E2E com PostgreSQL descartável, aprovados na revisão de 08/10/2026. Nenhum seed ou appsettings adicional foi criado.

## Revisão da organização e ligação com cultivo — 09/10/2026

A área Horta segue Hortas → setores da horta → canteiros do setor. Cada lista abre seu nível seguinte; a criação da horta e do setor continua diretamente nos filhos. O canteiro usa identificação seguida de confirmação de área, setor e data, com retomada de gravações parciais. Histórico, inativação e mudanças por data ficam recolhidos. Os endpoints e o banco mantêm os contratos existentes.

Todos os módulos continuam no menu. Organização aparece acima de No campo; a marca foi integrada à lateral e o rótulo Prévia do MVP removido. A área Horta usa caminho hierárquico em vez das abas anteriores. No celular, a marca fica junto ao botão do menu.

O detalhe do canteiro integra cultivos e plantios. Adicionar cultivo fixa o canteiro, permite cadastrar uma espécie durante o preenchimento e informa data/área. O plantio usa o cultivo recém-criado e pode ser registrado na hora ou posteriormente. A tela confere o vínculo canteiro/cultivo ao abrir uma URL direta, e as consultas são paginadas. Não foi criado campo de espécie na identidade física do canteiro nem imposta exclusividade de cultivo.

O [README do frontend](../../hortacomunitaria-webapp/README.md) e o [Get Started](../Get_Started.md) descrevem o percurso e a suíte E2E. A seleção padrão mantém todos os testes, incluindo a regressão dos outros módulos e os cenários novos em `organization.spec.ts`.

O menu permanece disponível também na listagem de hortas, no desktop e no celular. Ao escolher uma área sem horta selecionada, selecione a horta na lista para abrir a área desejada.
O rodapé ocupa o fim da janela nas páginas curtas e acompanha o final do conteúdo nas páginas longas, com o mesmo layout em todas as áreas.
O seletor de horta fica no menu e inicia com “Selecione uma horta” quando não há contexto. A seleção abre a área escolhida; sem hortas ativas disponíveis, oferece cadastrar uma horta.


## Editor visual — revisão de 24/10/2026

O modo interativo permite desenhar terreno irregular sem medidas reais, delimitar setores e acrescentar múltiplas mandalas. O grid usa unidades livres, sem escala métrica. O botão **Salvar configuração** persiste a geometria e os vínculos iniciais na API; alterações ainda não salvas permanecem no rascunho visual da aba. O [README do frontend](../../hortacomunitaria-webapp/README.md) descreve as ferramentas, a resolução de setores sobrepostos e o teste isolado do editor.


### Terreno obrigatório no modo interativo

O fluxo começa pela delimitação do terreno, sem criar um quadrado padrão. Após concluir o contorno, são liberados setores e, depois, canteiros. O terreno pode ser ampliado, reduzido ou redesenhado por edição dos vértices; o enquadramento acompanha o desenho escolhido. As medidas reais seguem opcionais, e a geometria permanece como rascunho até usar Salvar configuração. O teste isolado do editor cobre também esse fluxo e a conservação do terreno após recarregar.

A apresentação das etapas do editor usa cartões responsivos, estados de edição/conclusão/bloqueio e orientação contextual. A revisão preserva as dependências terreno → setores → canteiros e usa os mesmos testes isolados do editor.

Fechamento do terreno disponível ao clicar no vértice inicial, com validação do contorno e dos objetos existentes. O painel de coordenadas foi removido e o grid ampliado. Editor e panorâmica usam roda comum para rolagem da página e Ctrl/Command + roda para zoom local; listener nativo não passivo impede conflito com a rolagem. A panorâmica considera as margens do SVG ao calcular zoom e arraste.

O editor oferece formatos de terreno retangular, quadrado, circular e em L, escolhidos pelo usuário e editáveis posteriormente. O painel de objetos foi removido. A lista de posições é editada por clique esquerdo para adicionar, direito sobre o nó para remover e arraste para mover; a validação ocorre ao concluir o terreno.

Setores de contorno livre estão disponíveis no editor local por desenho de nós ou cópia do contorno da horta. A associação usa a geometria real, e a panorâmica preserva o contorno. A remoção de nós usa captura de pointerdown no SVG, independente de overlays e menu de contexto. Não houve alteração de contratos HTTP ou banco.

As miniaturas dos formatos são SVGs locais no componente de seleção, com estados acessíveis de escolha e apresentação responsiva em desktop/celular. Não há dependência externa de imagens.

O menu do editor foi agrupado por terreno, setores e canteiros, preservando câmera e histórico separados. A remoção de terreno é bloqueada enquanto existirem setores visuais. Sem setores, limpa o contorno do rascunho e redefine o histórico/base de restauração, com teste de recarregamento e desbloqueio após excluir o último setor. Os cadastros do backend não são excluídos por essa ação.

Menu de construção integrado ao componente do mapa, à direita no desktop e acima do grid no celular, com recolhimento e rolagem independente. As caixas externas de ferramentas e formatos foram incorporadas a esse menu; o SVG preserva sua própria área de interação, sem ficar coberto pelos controles.

O layout principal não impõe mais teto de largura ao conteúdo das páginas. O editor foi conferido em 2560 px, com barra horizontal sobre o grid e lateral contínua sem cartões. Limites próprios de leitura/formulários permanecem nos componentes que os definem; a área principal e o mapa se expandem com a janela.

A lateral de setores permite seleção explícita por ID para edição/exclusão de um entre vários setores. O teste verifica edição de nome, exclusão isolada, preservação dos demais e recarregamento. O layout segue uma grade explícita com barra superior, área de desenho e lateral abrangendo as duas linhas.

O editor admite redimensionamento por arestas em terrenos e setores livres, com arraste dos dois extremos, captura do ponteiro e ajuste por teclado. Um clique simples na aresta preserva a inserção de nó. A validação do contorno continua sendo feita ao concluir a edição.

Revisão visual da panorâmica: setores exibidos como limites opcionais, sem superfícies que cubram a grama; faces das caixas com textura em coordenadas locais e altura proporcional à projeção. A validação isolada compara setores ligados/desligados, rotações, círculo, mandala, zoom e celular e verifica preservação dos sprites.

### Persistência do mapa interativo

Aplicar a migration `PersistGardenLayouts` com o procedimento de migrations deste documento e reiniciar a API após atualizar o código. `GET /api/v1/gardens/{gardenId}/layout` retorna versão, instante de salvamento e geometria; `PUT` recebe `operationId`, `expectedVersion` e `layout` (width, height, boundary, elements). As dimensões são unidades de desenho, sem conversão em medidas reais. Uma horta sem mapa retorna versão 0 e layout null.

O servidor valida terreno, formas e vínculos da mesma horta, cria os novos setores/canteiros e associações iniciais e confirma mapa e auditoria em uma transação. Repetir a última operação com o mesmo conteúdo recupera a confirmação; versão desatualizada exige revisão, sem sobrescrita automática. Uma falha de rede mantém a tentativa e bloqueia edição até repeti-la. Remover do mapa preserva os cadastros e o histórico; reassociação de canteiro existente exige vigência explícita no cadastro.

A visão panorâmica consulta a configuração confirmada, sem usar sessionStorage. Rascunhos não salvos não aparecem fora do editor. Testes de integração em PostgreSQL descartável cobrem persistência, rollback, isolamento por horta, repetição e concorrência.

Correção do arraste panorâmico: captura de ponteiro restrita à cena, encerramento em saída/cancelamento/perda de captura e conversão de coordenadas pela matriz do SVG. O teste isolado exercita seleção e arraste sobre canteiros, preservação do scroll da página, saída e perda de captura, além de toque em largura de celular.

Rótulos da panorâmica: desligados por padrão, ativados por checkbox acessível por teclado e independentes dos limites dos setores. Etiquetas abreviam nomes longos e evitam colisões; detalhes completos permanecem na seleção. Verificação isolada cobre alternância, teclado, geometria preservada e ausência de sobreposição.

### Configurar plantio pelo desenho

O menu de canteiro no editor compõe os serviços HTTP existentes de área/setor, ciclos de cultivo, espécies e plantios. Nenhuma migration nova é necessária para esta integração. O painel lateral começa pela escolha da espécie no catálogo fotográfico paginado (nove itens por página). A seleção é preservada ao paginar. Depois da escolha, canteiros sem vínculo solicitam o salvamento do mapa; em seguida vêm área real, dados do cultivo e quantidade plantada. Fotos são consultadas por espécie e mantidas em memória, com autor, licença, origem e indicação de imagem ilustrativa; ausência/falha da imagem não impede a seleção. O menu consulta novamente os registros confirmados, permite retomar um cultivo pendente e preserva a tentativa original em falhas de rede. O cenário `e2e/interactive-cultivation.spec.ts` usa API/PostgreSQL descartáveis e percorre o fluxo pelo navegador, incluindo criação de espécie, retomada e resposta perdida do plantio.

Para executar o E2E do catálogo completo em banco descartável, definir `HORTA_E2E_EMBRAPA=true` além de `HORTA_E2E_POSTGRES_URL`. O setup importa o pacote offline após criar a autoria técnica, somente no banco exclusivo do teste. No banco de uso, a importação permanece administrativa explícita e exige conciliar nomes manuais duplicados conforme [Catálogo Embrapa](Catalogo_Embrapa.md).

### Configuração padrão e ficha de plantio

A área Horta e a rota antiga de preparação abrem o editor; criar/selecionar uma horta também leva ao desenho. O painel de canteiro mantém seleção visual da espécie e usa ficha única de data, área ocupada e quantidade, pedindo área real somente quando não existe configuração na data. Variedade, lote, espaçamentos e responsável são opcionais. A confirmação salva área, ciclo e plantio pelas operações existentes, preservando chaves de operação e etapas confirmadas em falhas. Se o cultivo foi confirmado e o plantio rejeitado, a interface oferece retomar o cultivo existente. Não é uma transação única entre esses três endpoints.

Setores com plantio confirmado nos cultivos retornados pela API recebem grama trabalhada, sem usar a textura das caixas dos canteiros. Padrões e máscara se movem com a câmera, respeitam o contorno da horta e não dependem da opção de mostrar limites. Asset, ferramenta e prompt estão em [Grama trabalhada](Grama_Trabalhada.md).

### Foco e detalhes na panorâmica

A seleção de canteiros aproxima a câmera em uma transição de 320 ms, limitada pelo tamanho da peça e pelo espaço disponível ao lado das informações. Movimento reduzido aplica o enquadramento imediatamente. Interações manuais cancelam a animação; Enquadrar horta restaura a câmera geral. O painel fica abaixo da cena em telas pequenas. O link de detalhes usa gardenId e entityId do mapa confirmado, com retorno explícito ao panorama. A página existente do canteiro também apresenta código, medidas reais e início de vigência da área/setor, além dos cultivos, plantios e históricos já disponíveis. Nenhum contrato ou migration de backend mudou.
