# Regras de arquitetura do backend da horta

Este arquivo é a referência arquitetural do backend. Adapta a premissa dos arquivos `dotnet-vsa-ddd.mdc` do EasyDFe e `dotnet-btsa.mdc` ao domínio da horta. Seus caminhos, exceções e regras de negócio não são dependências deste projeto.

O `AGENTS.md` da raiz orienta o Codex a ler e seguir este arquivo antes de planejar, implementar ou revisar o backend. As regras ficam em Markdown, sem depender dos metadados de aplicação automática do Cursor.

## 1. Escopo e decisões

- Aplicar estas regras ao planejamento, implementação e revisão do backend. Elas não definem a organização interna do frontend React/TypeScript/Vite.
- Fontes funcionais: [requisitos da versão 0.0.1](../documentos/Documento_Tecnico_Requisitos_Horta.md) e [anexo da professora](../documentos/Dados_e_Indicadores_Preliminares_Horta_APOEMA.pdf). A [modelagem](../documentos/arquitetura/Modelagem_Preliminar_Dados_Horta.md) é preliminar, não um schema obrigatório. Os [fluxos iniciais](../documentos/planejamento/Fluxos_Iniciais_Horta.md) apoiam o planejamento dos casos de uso.
- Prioridade: coleta manual, armazenamento, gestão dos registros e dashboards. Compostagem, sensores e automação da irrigação estão fora da etapa atual.
- Baseline técnico: .NET 10 ou superior, C#, ASP.NET Core Minimal APIs, EF Core com PostgreSQL/Npgsql, OpenAPI/Swagger UI e Serilog como provedor de logs.
- PostgreSQL é o SGBD adotado, com `Npgsql.EntityFrameworkCore.PostgreSQL` como provedor EF. Fixar versões compatíveis do servidor, EF Core e Npgsql na implementação.
- Autenticação adotada em 12/11/2026: ASP.NET Core Identity com bearer token nativo, conforme a seção 11 e o [plano de autenticação](../documentos/planejamento/autenticacao/Plano_Autenticacao_Autorizacao.md). Hospedagem permanece a definir; não instalar nem configurar uma escolha como se já estivesse aprovada.
- **Etapa de identidade iniciada em 12/11/2026:** o MVP foi construído sem autenticação (decisão de 08/10/2026); a partir desta etapa, as regras de identidade autenticada e autorização das seções 7, 10, 11 e 13 são exigíveis em todo slice. Integridade entre hortas continua obrigatória.
- A autoria é o usuário autenticado, resolvido por `CurrentAuthor` a partir da claim da sessão e conferido como ativo no banco; nunca recebida do cliente. O antigo autor técnico `demo:operator` permanece em `users` como linha `IsDemonstration`, inativa e sem credenciais, apenas para o histórico já gravado. O participante responsável pelo trabalho permanece separado e opcional.
- O modo demonstrativo (`DemoMode`) foi removido. Toda rota exige autenticação, exceto login, renovação, logout, health checks e OpenAPI.
- Hipóteses funcionais identificadas no contrato do protótipo podem ser implementadas e testadas para demonstração quando a respectiva etapa for autorizada. Não apresentá-las como aprovadas pela PO; seu aceite ou ajuste deve ser registrado antes de promover o comportamento para uso operacional.
- Na implementação, fixar SDK, versões dos pacotes e ferramenta `dotnet-ef` compatíveis. Não usar versões flutuantes nem atualizar o major automaticamente porque a stack permite .NET 10+.
- Uma regra arquitetural define como implementar; não aprova fórmulas, campos obrigatórios, limites agronômicos ou novas funcionalidades. Confirmar essas decisões com a PO.
- Não gerar projetos, migrations ou infraestrutura apenas para preencher a árvore abaixo. O repositório permanece documental até haver uma tarefa de implementação.

## 2. Organização por Vertical Slices e DDD

- Um único projeto executável, `HortaComunitaria.Api`, preservando o nome e a localização do projeto criado pelo responsável em 08/10/2026. Projetos de teste separados em `tests/` são permitidos. A árvore abaixo permanece ilustrativa; os comandos de execução usam os caminhos reais documentados em [Execução local](../documentos/arquitetura/Execucao_Local.md). Não criar class libraries `Domain`, `Application`, `Infrastructure` ou `Shared` por padrão.
- `Domain/` contém entidades, value objects, agregados e políticas de negócio. Não depende de HTTP, EF Core, DI, infraestrutura ou features.
- `Features/` organiza casos de uso, com endpoint, handler, contratos e validação próximos. Não organizar a aplicação em pastas globais de controllers, DTOs, handlers ou services.
- `Infrastructure/` contém adaptadores e configuração: EF, migrations, autenticação/autorização, arquivos, tradução HTTP de resultados e observabilidade.
- `Common/` contém somente tipos pequenos, independentes e realmente compartilhados, como `Result<T>`, `Error`, `Unit` e paginação. O domínio pode depender desses tipos neutros.
- Dependências permitidas: `Features -> Domain, Common, Infrastructure`; `Infrastructure -> Domain, Common`; `Domain -> Common`; `Common` não depende das outras pastas. `Program.cs` compõe a aplicação. Extensões de composição/registro em infraestrutura podem referenciar handlers e registros de endpoints dos slices; adaptadores de persistência, arquivos e identidade não dependem de casos de uso.
- Namespaces acompanham as pastas. Tipos e membros C# em inglês; documentação, mensagens e textos destinados às pessoas em pt-BR.

Estrutura alvo ilustrativa, a ser criada conforme os casos de uso forem implementados:

```text
HortaComunitaria.Api/
    Common/
      Results/
      Pagination/
    Domain/
      Gardens/
      Cultivation/
      Environment/
      Irrigation/
      Activities/
      Occurrences/
      Costs/
      Evidence/
    Features/
      Gardens/
        CreateBed/
          CreateBedCommandHandler.cs
          CreateBedEndpoints.cs
        GardensEndpointRegistration.cs
      Cultivation/
        RegisterPlanting/
        RegisterHarvest/
      Irrigation/
        RegisterIrrigation/
      Indicators/
        GetWaterConsumption/
        GetSeedlingEstablishment/
    Infrastructure/
      DependencyInjection/
      Endpoints/
      Persistence/Configurations/
      Migrations/
      Identity/
      Files/
      Observability/
    Program.cs
tests/HortaComunitaria.Api.IntegrationTests/
```

- Os nomes lógicos em português na modelagem descrevem o negócio. Manter correspondência explícita com os nomes C#; não criar dois conceitos para a mesma entidade por causa da tradução.
- A organização por domínio não cria microsserviços nem bancos separados. A primeira horta é a FCT/UFG; o vínculo com a horta é contexto de dados e acesso, não uma plataforma de tenants.
- Reutilização concreta dentro de um domínio pode ficar em `Features/{Domain}/Shared/`. Não chamar o handler HTTP de outro slice como forma de compartilhar regra.
- Não criar um agregado para toda a horta nem carregar todas as suas navegações para uma operação local. Limites de agregados devem acompanhar as invariantes aprovadas; tabelas e FKs não definem esses limites automaticamente.

## 3. Padrões e limites de abstração

- Usar factories para criação de domínio, Result para falhas esperadas e DI para composição.
- Separar comandos e consultas por handlers; não criar infraestrutura de CQRS distribuído, event sourcing ou bancos de leitura separados.
- Não usar MediatR ou equivalentes. Endpoints injetam e chamam handlers concretos diretamente.
- Usar `AppDbContext` diretamente nos handlers. Não criar repositories, repository genérico, `IAppDbContext` ou Unit of Work apenas para envolver o EF Core.
- Interfaces representam fronteiras reais, como armazenamento de arquivos ou contexto do usuário. Não criar uma interface para cada classe concreta.
- Domain services/policies só quando a regra não pertencer naturalmente a uma entidade ou value object. Regras não ficam em endpoints, mapeamentos EF ou validadores de HTTP.
- Não introduzir workers, Hangfire, RabbitMQ, Redis, Elasticsearch, cache distribuído ou stack Grafana como baseline. Uma futura necessidade concreta exige decisão documentada antes da inclusão.
- Domain Events são opcionais e exigem consumidor real. Não criar eventos ou outbox para operações locais que terminam em uma transação no banco.

## 4. Anatomia do slice e endpoints

- Cada caso de uso possui sua própria pasta em `Features/{Domain}/{UseCase}/`.
- Handler: `{UseCase}CommandHandler.cs` ou `{UseCase}QueryHandler.cs`, com `HandleAsync(..., CancellationToken cancellationToken)` retornando `Task<Result<TResponse>>`. Sem payload, usar `Result<Unit>`.
- Requests/responses são records no arquivo do handler; se o tamanho justificar, ficam em arquivo do próprio slice. Não criar uma biblioteca de contratos C# para o frontend TypeScript.
- Endpoint: `{UseCase}Endpoints.cs`, na mesma pasta, expondo `Map{UseCase}Endpoints(this RouteGroupBuilder group)`.
- Endpoint faz binding, chama o handler e traduz o resultado. Não consulta o banco, valida regras agronômicas ou calcula indicadores diretamente.
- `{Domain}EndpointRegistration.cs` apenas delega aos slices. Não contém `MapGet`, `MapPost` ou regras de negócio.
- `Infrastructure/Endpoints/EndpointRouteBuilderExtensions.cs` reúne `MapAllEndpoints`. `Program.cs` configura serviços/pipeline e chama esse índice.
- Apenas Minimal APIs; não usar `ApiController`, `AddControllers` ou `MapControllers`.
- Base dos contratos HTTP: `/api/v1`. Recursos da horta usam contexto explícito, por exemplo `/api/v1/gardens/{gardenId:guid}/beds` e `/api/v1/gardens/{gardenId:guid}/irrigations`.
- Catálogos compartilhados, como espécies, podem ter rotas fora do grupo da horta quando sua natureza justificar, mantendo autorização própria. Não exigir cabeçalhos de tenant/empresa herdados do EasyDFe.
- Nomes de rotas únicos, tags por domínio, `.WithSummary(...)` e `.WithDescription(...)` em pt-BR. Usar constraints de parâmetros e registrar rotas literais antes das parametrizadas por clareza.
- Health checks e documentação são endpoints técnicos registrados pela infraestrutura; não precisam de slice de negócio.

## 5. Handlers, DI e estilo C#

- Nullable reference types e implicit usings habilitados; namespaces file-scoped, records para contratos imutáveis e tipos sealed quando não houver extensão prevista.
- Preferir construtores primários em handlers e serviços que apenas capturam dependências. Entidades usam construtores/factories conforme a seção de domínio.
- Registrar handlers, validadores, serviços que usam EF e `AppDbContext` como scoped. Singleton somente para serviços thread-safe sem dependências scoped.
- Agrupar composição em extensões como `AddApplication`, `AddPersistence` e `AddObservability`, somente para responsabilidades existentes.
- Usar Options tipadas com validação e `ValidateOnStart` para configurações obrigatórias; injetar `TimeProvider` para relógio substituível em testes.
- Usar apenas `appsettings.json` como base e `appsettings.Development.json` para desenvolvimento, por decisão do responsável em 08/10/2026. O host usa o carregamento padrão do ASP.NET Core; a factory do EF lê a mesma base e o arquivo do ambiente selecionado, com variáveis de ambiente prevalecendo. A conexão local com credenciais fica no Development ignorado pelo Git; configurações de produção e segredos são fornecidos pelo ambiente. Não criar outro arquivo appsettings local.
- Não usar service locator, `BuildServiceProvider` no registro de serviços ou dependências injetadas que não são utilizadas.
- Validar formato/limites do request explicitamente no início do handler. FluentValidation é permitido quando útil, sem replicar as invariantes do domínio.
- Propagar cancellation tokens em consultas, gravações e operações de arquivos. Não usar `.Result`, `.Wait()`, `async void` ou `Task.Run` para simular I/O assíncrono.
- Não executar operações paralelas no mesmo `DbContext`.

## 6. Domínio e Result

- Entidades e value objects encapsulam invariantes com construtores privados/protegidos e factories estáticas `Create` que retornam `Result<T>`.
- Alterações ocorrem por métodos com intenção, como registrar avaliação ou corrigir pesagem. Não expor setters públicos que permitam ignorar regras ou alterar autoria/histórico.
- Agregados referenciam outros agregados por identidade. Coordenar uma operação entre agregados no handler, sob a estratégia transacional definida para o caso de uso.
- `Result<T>`, `Error` e `Unit` ficam em `Common/Results/`. Não duplicar versões desses tipos por domínio ou slice.
- `Result<T>` possui `IsSuccess`, valor disponível apenas em sucesso, erro estruturado apenas em falha e `Match`. Impedir resultados com sucesso e erro simultâneos.
- `Error` contém código estável, mensagem e categoria, por exemplo `Validation`, `NotFound` ou `Conflict`. Domínio não armazena status HTTP e não produz `ProblemDetails`.
- Rejeições esperadas retornam `Result.Failure`. Bugs e falhas técnicas inesperadas usam exceções; não capturar toda exceção e convertê-la em erro de validação.
- IDs próprios gerados pela aplicação usam `Guid.CreateVersion7()`, inclusive em testes quando houver geração. Chaves compostas e IDs externos são preservados. UUID v7 não substitui timestamps nem garante ordem estrita de eventos.

## 7. Contratos HTTP e frontend

- Não retornar entidades EF, navegações, proxies ou `IQueryable`. Projetar dados para DTOs e mapear explicitamente, sem biblioteca de mapeamento por padrão.
- JSON usa propriedades camelCase. Enums de contrato usam nomes legíveis em PascalCase, com conjunto permitido documentado no OpenAPI.
- Requests de enum usam tipo CLR com conversão string que rejeite números e validação de valores definidos. Query/path também devem rejeitar representações numéricas; não confiar só no binding padrão.
- Respostas podem mapear enums para strings; não documentar status como texto livre. O formato HTTP não depende do armazenamento físico do enum.
- Datas de calendário usam `YYYY-MM-DD`. Instantes exigem offset na entrada e são normalizados em UTC. Unidades de medidas e casas decimais devem estar claras no contrato.
- Campos não coletados são `null` ou possuem situação explícita; não omitir a diferença entre desconhecido e zero.
- Centralizar em infraestrutura a tradução de Result: 400 para validação, 404 para recurso ausente e 409 para conflito de estado/concorrência. Autenticação/autorização respondem 401/403 conforme o caso; não fingir ausência de autenticação com um erro de negócio.
- Usar `ProblemDetails`/`ValidationProblemDetails` com código estável, erros por campo quando aplicável e trace ID. Não retornar stack trace, SQL ou mensagens internas do provedor.
- Criação síncrona retorna 201 e `Location`; consultas retornam 200 ou 404; alterações retornam 200 ou 204 conforme o contrato. Não usar 202 quando o trabalho já terminou.
- Padronizar coleções com `Page<T>`: `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`. Limites/defaults ficam em `PaginationRequest`, não duplicados nos endpoints.
- Listagens possuem ordenação estável e desempate por ID; validar filtros e limites antes de executar a consulta.
- Expor OpenAPI coerente com os contratos reais e uma UI Swagger compatível com a versão adotada. Não copiar filtros específicos de uma biblioteca dos projetos de referência sem verificar sua necessidade.
- O frontend consome contratos HTTP e erros padronizados. Regras de integridade e autorização são sempre verificadas no backend, mesmo quando a interface já valida.

## 8. EF Core, persistência e migrations

- `AppDbContext` em `Infrastructure/Persistence/`; mapeamentos em `IEntityTypeConfiguration<T>` dentro de `Configurations/`, aplicados pelo assembly.
- Mapeamentos não devem exigir atributos de EF em entidades do domínio. Configurar nullability, precisão/escala, tamanho de strings, FKs, índices, checks e exclusão explicitamente.
- Tabelas PostgreSQL em snake_case via `ToTable`, com nomes coerentes com os tipos C#, por exemplo `cultivation_cycles` e `irrigations`.
- Colunas seguem as propriedades C# em PascalCase, por exemplo `GardenId` e `RecordedAt`. `HasColumnName` só em exceção documentada, como integração legada ou view. Não ativar conversão global para snake_case nem misturar convenções nas colunas.
- SQL textual usa aspas duplas nos identificadores PascalCase, como `"GardenId"`, preservando o casing físico. Valores são sempre parametrizados.
- IDs `Guid` são persistidos como `uuid`, mantendo a geração UUID v7 da seção de domínio; não armazenar IDs em `text`/`varchar`.
- Nomes em português da modelagem são lógicos e não obrigam nomes físicos idênticos. Documentar a correspondência quando não for óbvia.
- Usar `decimal` para dinheiro, massas, áreas, volumes e horas contabilizadas. Precisão e arredondamento são definidos por grandeza; rejeitar valores que excedam o contrato sem truncar silenciosamente.
- Instantes persistidos em UTC; datas agrícolas não são convertidas em instantes à meia-noite. Usar o fuso `America/Sao_Paulo` para recortes locais, com tradução compatível com o ambiente de execução.
- Todo enum de domínio persistido usa tipo `ENUM` nativo do PostgreSQL. Não usar `HasConversion<int>()`, conversão para string ou colunas `integer`/`text` em seu lugar.
- Tipos ENUM e seus labels usam snake_case, independentemente do PascalCase das colunas e do contrato HTTP. Exemplo: tipo `cultivation_status`, membro C# `InProgress`, label PostgreSQL `in_progress`.
- Centralizar o registro de enums em `Infrastructure/Persistence/`, por exemplo `PostgresEnums`. Modelo EF, migrations e provedor Npgsql devem conhecer os mesmos tipos/labels. Em EF Core/Npgsql 10+, usar `MapEnum<T>` em `UseNpgsql`; quando houver `NpgsqlDataSource` externo, registrar também nele. Não duplicar configurações incompatíveis nem copiar APIs de versões antigas. [Mapeamento oficial Npgsql](https://www.npgsql.org/efcore/mapping/enum.html).
- Adicionar valores a enum exige migration gerada pelo EF CLI, com revisão do `ALTER TYPE ... ADD VALUE`. Renomear/remover labels exige estratégia explícita para preservar os dados e a compatibilidade; não tratar a mudança como simples conversão de coluna.
- Não usar cascade delete que remova produção, avaliações, irrigações ou histórico por excluir um cadastro. Inativação/invalidação deve preservar os vínculos exigidos pelos requisitos.
- Checks/FKs/índices únicos reforçam as regras locais. Uma checagem no formulário ou no handler não substitui uma garantia de unicidade no banco.
- Consultas de leitura usam `AsNoTracking`, projeção, filtros e paginação antes de materializar. Evitar lazy loading, N+1 e carregar a base inteira para calcular um cartão.
- SQL específico do provedor fica na infraestrutura ou na orquestração de persistência do slice, parametrizado e testado. Nunca dentro de entidades do domínio.
- Gerar migrations e snapshot pelo EF CLI; não escrever ambos manualmente como substituto da geração. Revisar SQL, precisão, compatibilidade e perda de dados; ajustes pontuais na migration gerada precisam de justificativa.

Comando de referência para quando existir o projeto com o provedor Npgsql configurado:

```bash
dotnet ef migrations add <NomeMigration> --project HortaComunitaria.Api --startup-project HortaComunitaria.Api --context AppDbContext --output-dir Infrastructure/Migrations
```

- Versionar migrations, snapshot e manifest local de `dotnet-ef`; documentar `dotnet tool restore`.
- Produção aplica migrations em etapa controlada de deploy, antes de liberar tráfego. Não executar migrations concorrentes em todos os hosts nem usar `EnsureCreated` junto de migrations.
- Aplicação automática no startup só é permitida em Development, por configuração explícita e com falha de migration impedindo a inicialização.
- Não implementar seed automático na aplicação, por decisão do responsável em 08/10/2026. Exceções autorizadas: catálogo Embrapa (24/10/2026), com importação administrativa explícita, offline, idempotente e auditada via `--seed-embrapa`, atribuída ao primeiro Adm ativo; e primeiro administrador (12/11/2026), via comando explícito `--bootstrap-admin`, que lê as credenciais do ambiente, recusa-se se já houver Adm ativo e exige troca de senha. Nenhum roda no startup ou em migrations. Escopo e operação em [Catálogo Embrapa](../documentos/arquitetura/Catalogo_Embrapa.md). Migrations evoluem somente o esquema; fixtures de teste preparam dados exclusivamente em bancos descartáveis.

## 9. Transações, concorrência e repetição

- Uma gravação de negócio e seu histórico de alteração devem ser confirmados juntos na mesma transação. Middleware de log após a resposta não substitui essa auditoria.
- Para mudanças que cabem em um `SaveChangesAsync`, aproveitar a transação do EF. Usar transação explícita quando a operação exigir várias gravações ou proteção das leituras anteriores à decisão. [Referência EF](https://learn.microsoft.com/en-us/ef/core/saving/transactions).
- Edições usam token de concorrência persistido e versão esperada pelo cliente. `DbUpdateConcurrencyException` resulta em conflito revisável, sem sobrescrever automaticamente a alteração de outra pessoa. [Referência EF](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).
- Token de concorrência na linha não protege sozinho somas ou intervalos entre registros. Regras aprovadas de ocupação de área, vigência de setor e destinação de massa exigem estratégia transacional/constraint apropriada ao provedor.
- A estratégia deve proteger o recurso compartilhado: duas destinações de secagem que disputam a mesma colheita não podem confirmar um total acima da massa disponível. Testar a disputa no banco real.
- Se forem necessários locks, adquirir em ordem determinística, manter transações curtas e não fazer upload/chamada externa enquanto elas estiverem abertas.
- Retry técnico precisa respeitar a estratégia do provedor e não repetir efeitos externos. Não aplicar retry cego a rejeições de negócio ou correções concorrentes.
- Onde repetição de criação puder duplicar um fato de campo, definir proteção verificável, como uma chave de operação com escopo e unicidade. Mesma chave/conteúdo retorna o registro existente; conteúdo diferente gera conflito.
- Não inventar unicidade por data/canteiro para eventos que podem ocorrer várias vezes no dia. A proteção de repetição deve distinguir retry de outro registro legítimo.

## 10. Histórico e consistência dos indicadores

- Guardar autor, instante de inclusão e instante/data do fato como informações distintas. Correção/invalidação preserva motivo e valores anteriores/novos.
- O identificador do autor vem da identidade autenticada, não de um campo arbitrário enviado pelo frontend. Participante/executante e autor do registro são papéis diferentes.
- Registros invalidados não compõem os indicadores. Não exigir um workflow extra de aprovação para toda gravação sem requisito validado.
- Alteração de setor, dimensões do canteiro ou cadastro não reclassifica silenciosamente fatos anteriores. Consultar o contexto histórico correspondente à data do fato.
- Queries de indicadores são slices em `Features/Indicators/`. Cálculo puro reutilizado pode ficar em uma política de domínio; filtros, autorização e agregação EF ficam na consulta.
- Usar os fatos como fonte dos resultados. Não criar tabela de valores de dashboard digitáveis, fórmulas executáveis recebidas do usuário ou motor genérico de indicadores.
- Fórmula, unidade, recortes e elegibilidade precisam de validação com a PO. Manter uma definição documentada e testável; alterações de significado devem ser explicitadas.
- Agregar o fato principal antes de combinar filhos. Uma irrigação atendendo quatro canteiros mantém um volume; uma atividade com três participantes mantém um evento.
- Não transformar ausência em zero. Expor situação de disponibilidade e cobertura/incompletude quando necessária para interpretar o resultado; percentuais exigem denominador válido.
- Somar numeradores/denominadores compatíveis em percentuais agregados; não usar média simples de percentuais com bases diferentes. Unidades/métodos incompatíveis permanecem separados.
- Consultas com vários agregados precisam declarar sua consistência temporal quando isso afetar o resultado; não prometer um snapshot único com leituras independentes.
- Cache/consolidação só quando uma medição de desempenho justificar, com política de atualização após correções e equivalência aos fatos comprovada.

## 11. Acesso e arquivos

- Implementar autorização por ação e contexto de horta no servidor. `gardenId` no path/body não prova que a pessoa tem acesso.
- Seleção do recurso verifica vínculo com a horta autorizada. Referências a canteiro, setor ou ciclo de outra horta não podem ser gravadas por conhecer o ID.
- Resolver identidade e permissões pela infraestrutura, mantendo o domínio independente de `HttpContext` e claims. A escolha de provedor não exige construir um sistema de identidade próprio.
- Identidade: `AddIdentityCore<AppUser>` + `SignInManager` com bearer token nativo (`AddBearerToken`), sem `MapIdentityApi` nem JWT. `AppUser : IdentityUser<Guid>` fica em `Infrastructure/Identity/` e usa a tabela `users`, que também identifica a autoria; `AppDbContext` herda `IdentityUserContext<AppUser, Guid>` (tabelas `user_claims`, `user_logins`, `user_tokens`). Chaves do Data Protection persistidas em `Authentication:KeysPath`.
- Sessão: access token curto (15 min) no corpo e refresh token (7 dias) somente em cookie `__Secure-horta.refresh` `HttpOnly`/`Secure`/`SameSite=Strict` restrito a `/api/v1/auth`, rotacionado a cada uso. Logout, troca de senha, mudança de perfil e desativação renovam o security stamp e revogam os refresh tokens; gravações conferem no banco se o autor continua ativo. Lockout após 5 falhas por 15 min, resposta genérica no login, senha de 12+ caracteres sem senhas comuns ou dados da pessoa, rate limit das rotas de autenticação por IP e `Cache-Control: no-store` nas respostas de credenciais. Com senha inicial pendente, o filtro do grupo `/api/v1` responde 403 às demais rotas. Redefinição de senha não existe nesta etapa.
- Perfis: `Administrator` (Adm) e `Operator` (Operador), um por usuário, na coluna `Profile` (ENUM `user_profile`) emitida como claim de papel. A fallback policy exige usuário autenticado; `RequireAdministrator()` é aplicado por endpoint às ações de estrutura (hortas, setores, canteiros, configurações, vínculos e mapa), catálogos (espécies, cultivares, crescimento visual, variáveis, pontos, insumos, instituições, participantes) e usuários. Fatos de campo (registrar, corrigir, invalidar) e consultas valem para ambos. Todo novo slice declara em qual grupo se encaixa. Perfis por horta ou perfil de consulta exigirão tabelas de concessão e decisão da PO.
- Não expor administração, histórico ou dados pessoais anonimamente. Visibilidade pública de dashboards depende de requisito aprovado.
- CORS usa origens explícitas. Transporte, cookies/tokens e proteção contra CSRF devem corresponder ao mecanismo de autenticação escolhido, sem soluções incompatíveis combinadas por padrão.
- Arquivos ficam atrás de uma fronteira de armazenamento; banco guarda metadados/referência. Exceção autorizada em 24/10/2026: imagens ilustrativas do catálogo Embrapa podem ficar em Base64 na linha da espécie, com licença/procedência e consulta separada; não estender a uploads/evidências. Ver [Catálogo Embrapa](../documentos/arquitetura/Catalogo_Embrapa.md). Não confiar no nome fornecido para montar caminhos físicos.
- Validar tamanho/tipo permitido e autorização de upload/download; limites são configuração validada com a equipe. Fotografias/documentos não vão para logs.
- Upload e transação EF não são atomicamente unidos. Definir limpeza de arquivo órfão/estado de disponibilidade para falhas, sem confirmar um vínculo para arquivo inexistente.
- Segredos ficam em configuração local segura/variáveis apropriadas ao ambiente, fora do Git. Não gerar credenciais de produção em seed.

## 12. Logs, operação e documentação

- Usar `ILogger<T>` com Serilog como provedor. Templates estruturados, como `logger.LogInformation("Irrigação {IrrigationId} registrada", irrigationId)`; sem interpolação ou serialização indiscriminada de payloads.
- Logs preservam propriedades estruturadas em qualquer saída. Development usa a saída nativa colorida do Serilog (tema Literate), com horário HH:mm:ss, nível, processo/etapa e mensagem. Por solicitação do responsável após comparação visual em 08/10/2026, SourceContext completo, ambiente e EventName não se repetem no prefixo; ProcessId/TraceId completos aparecem em uma linha de apoio no resumo HTTP e em rejeições/falhas. Todas essas propriedades continuam estruturadas nos eventos. Não passar ITextFormatter personalizado ao sink Text, pois isso desabilita seu tema. A base mantém JSON compacto; selecionar Text ou Json por Observability:ConsoleFormat, sem outro arquivo appsettings. Identificar a aplicação/ambiente (`Application`, `Environment`), origem (`SourceContext`), processo estável (`ProcessName`), evento estável (`EventName`) e resultado (`Outcome`). O caso de uso compartilha um `ProcessId` UUID v7 entre handler, resumo HTTP e falha global; operações técnicas também possuem identidade própria. `Module`, `Phase`, `StepName` e `StepNumber` organizam o progresso, com duração monotônica `ElapsedMs`; IDs de recursos e contagens são propriedades adicionais, sem copiar o request.
- HTTP e processos compartilham `TraceId`, `SpanId` e `RequestId`. O trace ID W3C de 32 caracteres também aparece no cabeçalho `X-Trace-Id` e em `ProblemDetails.extensions.traceId`. Não aceitar um identificador de correlação arbitrário como nome de processo.
- Eventos `ProcessStarted`, `ProcessStep` e `ProcessCompleted` (Information), `ProcessRejected` (Warning), `HttpRequestCompleted` (Information em sucesso, Warning em 4xx, Error em 5xx) e `UnhandledException` (Error, com exceção única) distinguem progresso, sucesso, rejeição e falha técnica. Mensagens descrevem a ação/decisão do domínio; início e término são visíveis no nível padrão. Cancelamento do cliente fica em Debug, com Outcome=Cancelled, sem ser tratado como falha técnica. A falha global preserva a última etapa e os identificadores do caso de uso. Health checks saudáveis ficam em Debug; indisponibilidade de dependência em Warning. Inicialização registra eventos próprios e falha fatal impede o startup.
- Logs são emitidos nas fronteiras HTTP, handlers e adaptadores. Registrar etapas significativas (consulta, conferência de contexto, persistência e confirmação), sem entrada/saída artificial de cada método. Application, Environment, MachineName e SourceContext identificam a instância e a origem; códigos/eventos/propriedades permanecem estáveis e mensagens em pt-BR. O domínio permanece sem dependência de logging; suas rejeições são registradas pelo processo responsável. Comandos SQL, parâmetros EF e mensagens duplicadas de falhas do provedor são suprimidos; logs técnicos não substituem auditoria transacional. Exemplos e catálogo implementado ficam em [Observabilidade](../documentos/arquitetura/Observabilidade.md).
- Correlacionar com trace ID e identificadores do recurso quando necessários. Não registrar tokens, senhas, connection strings, arquivos ou dados pessoais desnecessários.
- Distinguir rejeição esperada de falha técnica e registrar a exceção uma vez no ponto responsável. Handler não repete o mesmo stack trace já registrado globalmente.
- Baseline de operação: logs no console e health checks `/health/live` e `/health/ready`. Readiness verifica o banco e apenas dependências indispensáveis; liveness não depende delas. Respostas públicas não expõem detalhes internos.
- Não tornar uma stack externa de observabilidade obrigatória para iniciar a API. Métricas/traces adicionais devem responder a uma necessidade operacional real.
- Documentar membros públicos/contratos em XML pt-BR, explicando invariantes e decisões relevantes, sem comentários que só repetem nomes.
- README permanece conciso e aponta para a documentação técnica. Instruções de execução, migrations e configuração do backend ficam em documentação própria quando houver implementação.
- Decisões relevantes ficam em `documentos/arquitetura/` quando surgirem, com contexto, decisão e consequência. Não criar ADRs vazias ou copiar opções dos projetos de referência como necessidades da horta.
- Preservar UTF-8 em `.md`, `.mdc`, `.csproj`, `.props` e `.targets`. Alterações de versão/configuração são localizadas; não regravar XML inteiro por parser que mude encoding/comentários.
- `Directory.Build.props`, se necessário, contém configurações comuns pequenas e claras. Não usá-lo como documentação ou arquivo de configuração da aplicação.

## 13. Testes e entrega

- Priorizar integração com xUnit, `WebApplicationFactory` e PostgreSQL real via Testcontainers, usando Npgsql e migrations da aplicação. EF InMemory ou outro SGBD não comprovam transações, enums, precisão e concorrência do banco de produção.
- Testes unitários de invariantes/cálculos puros podem complementar a suíte. Não copiar a proibição absoluta de unitários do EasyDFe nem escrever testes que apenas repetem a implementação.
- Cenários essenciais, conforme os slices implementados: permissão de consulta sem escrita; referências de outra horta; mudança de setor preservando histórico; 36/40 mudas = 90%; 120 L para quatro canteiros = 120 L; 3 pessoas x 2 h = 6 h; ausência de coleta diferente de zero.
- Cobrir correção com histórico e atualização dos indicadores, vínculos de arquivos autorizados, concorrência de edição e destinação de massa sem exceder saldo. Assertar os dados/resultados, não apenas o status HTTP.
- Separar testes de fórmulas já aprovadas dos exemplos ainda propostos. Uma fórmula testada não está automaticamente aprovada pela PO.
- Usar relógio controlado e dados isolados. Evitar sleeps arbitrários e `.ConfigureAwait(false)` em `[Fact]`/`[Theory]`.
- Build Release dos projetos afetados deve terminar sem warnings de compilação. Corrigir nullable, APIs obsoletas e parâmetros mortos; não silenciar avisos para passar no build.
- Quando existir código, os comandos de validação incluem build Release, testes dos casos afetados e revisão das migrations/contratos alterados. Documentar limitações concretas da verificação.
- Antes de concluir um slice, conferir: regra no lugar correto; autorização; Result/ProblemDetails; persistência/histórico coerentes; consulta limitada; teste significativo; OpenAPI e documentação atualizados.

## Referências técnicas

- [OpenAPI no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0).
- [Aplicação de migrations EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), incluindo cuidados de produção. As regras de aplicação acima são decisões deste projeto.

Mudanças nestas regras devem ser justificadas junto da alteração correspondente. Se uma tarefa exigir exceção, registrar seu alcance e a razão; não criar uma arquitetura paralela silenciosamente.
