# Etapa 1 — Contexto mínimo e primeira consulta

> Atualização de 08/10/2026: seed removido por decisão do responsável. Os passos/evidências de execução original abaixo são históricos; o código atual inicia sem inserir dados, e os testes preparam fixtures explícitas em bancos isolados. Consulte o [Get Started](../../Get_Started.md) para executar a versão atual.

**Estado:** concluída tecnicamente em 08/10/2026. **Autorização:** início do backend solicitado pelo responsável, preservando o projeto existente `HortaComunitaria.Api`. Hipóteses do protótipo continuam sujeitas à PO.

**Leitura obrigatória:** AGENTS.md, [arquitetura integral](../../../.codex/arquitetura-backend.md), [contrato do fluxo](Contrato_Fluxo.md). Não é necessário reler todos os modelos para executar este plano.

## Resultado e limite

Entregar uma consulta real de hortas demonstrativas passando por Minimal API, handler, AppDbContext e PostgreSQL, seguida das seleções de setor, canteiros e responsável. Isso constitui a espinha dorsal; não é um CRUD completo nem cadastro administrativo.

## Sequência de trabalho

1. Conferir ferramentas disponíveis. Fixar SDK .NET 10, EF/Npgsql compatíveis, PostgreSQL, Serilog, OpenAPI/Swagger e manifest local dotnet-ef em versões exatas. Registrar as escolhas efetivamente verificadas; não depender de versões flutuantes. Se precisar verificar APIs/pacotes, consultar documentação oficial. Usar somente o projeto existente `HortaComunitaria.Api` e `tests/HortaComunitaria.Api.IntegrationTests`, conforme ajuste de nomenclatura nas regras.
2. Criar os tipos comuns de Result/erro/Unit/paginação e a tradução HTTP prescritos nas regras. Criar apenas as entidades de catálogo da seção 1 do contrato: Garden, IrrigationSector, Bed, BedSectorAssignment, Participant e User. Não criar pastas vazias para outros domínios. Participant fica em `Domain/Activities`; User em `Domain/Identity`, sem serviços de autenticação.
3. Configurar AppDbContext, mapeamentos explícitos e uma migration gerada pelo EF CLI. FKs restritivas, códigos únicos, fim da vigência posterior ao início e nenhuma exclusão em cascata. Configurar conexão por variável/configuração local ignorada pelo Git, sem segredos no repositório.
4. Implementar primeiro `Features/Gardens/ListGardens`, incluindo seus arquivos `ListGardensQueryHandler.cs` e `ListGardensEndpoints.cs`; registrar via `GardensEndpointRegistration` e `MapAllEndpoints`. Só depois adicionar ListIrrigationSectors, ListBedsForIrrigation e `Features/Activities/ListParticipants` da mesma forma. Contratos e rotas são os da seção 4, sem acesso EF no endpoint.
5. Criar seed demonstrativo da seção 5: IDs estáveis, quantidades fictícias e configuração opt-in. Criar o operador técnico separado do participante. Reexecução do seed não altera cadastros usados nem duplica linhas; erro de seed deve ser visível. Não implementar endpoint para executar seed.
6. Configurar logs no console, ProblemDetails, health checks, documentação da API e bloqueio de startup fora do modo Development demonstrativo. Documentar a execução em `documentos/arquitetura/Execucao_Local.md`: ferramentas, configuração mínima, restore, migration, seed, inicialização e testes. PostgreSQL local pode ser disponibilizado por um compose mínimo; sem outros serviços. Se Docker não estiver disponível, documentar a limitação sem substituir os testes por EF InMemory.

## Verificações de aceite

- Build Release sem warnings; migrations aplicadas em PostgreSQL vazio e readiness respondendo conforme a disponibilidade do banco.
- ListGardens consulta o banco e retorna Page, não array fixo do seed. Setores, participantes e canteiros têm filtros/paginação/ordem do contrato.
- Seleção de B1–B4 em S1 em 08/10/2026 retorna quatro vínculos; antes da vigência não retorna esses canteiros. Data inválida é 400.
- Horta inexistente e setor de outra horta são 404. Código duplicado e FK inválida são impedidos pelo banco.
- Seed executado duas vezes conserva IDs e quantidades. Configuração de demonstração desabilitada ou ambiente diferente de Development impede startup, com mensagem explícita.
- Integração usa migrations da aplicação e PostgreSQL real. Assertar conteúdo e filtros; não basta verificar 200.

## Entrega ao próximo agente

Registrar versões escolhidas, configuração/comandos de execução e comandos de verificação em Execucao_Local.md; registrar evidências na [tabela do plano](README.md#acompanhamento). Se algo não pôde ser testado, deixar a etapa parcial e explicar a dependência concreta. Parar aqui: **nenhum registro de irrigação, cálculo de consumo ou frontend nesta etapa**.

## Evidências da execução

- Restore com `--locked-mode` e build Release da solução: sucesso, sem warnings.
- 29 testes de integração aprovados com WebApplicationFactory, migrations e PostgreSQL 17.0 nativo em cluster temporário isolado. Incluem conteúdo/paginação, vigências, isolamento entre hortas, integridade do banco, seed idempotente, configuração do modo demo, health checks e eventos Serilog.
- Migration gerada pelo EF CLI e SQL revisado; `has-pending-model-changes` confirmou ausência de diferença entre modelo e snapshot.
- Host HTTP real iniciado com migrations e seed opt-in: uma horta, quatro canteiros elegíveis, liveness/readiness 200 e validação 400 com traceId igual ao cabeçalho.
- Docker sem engine ativa: ciclo de vida de Testcontainers/compose não exercitado. Os cenários funcionais e do banco foram executados no PostgreSQL real pelo caminho alternativo da suíte.
- Versões, configuração e comandos em [Execução local](../../arquitetura/Execucao_Local.md); eventos em [Observabilidade](../../arquitetura/Observabilidade.md).
