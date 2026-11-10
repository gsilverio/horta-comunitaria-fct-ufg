# Horta Comunitária FCT/UFG

Trabalhe no projeto existente. Comunique-se em pt-BR; mantenha identificadores de código em inglês. Esta configuração orienta o Claude Code e não autoriza implementar novos fluxos, publicar ou alterar dados por conta própria.

## Arquitetura e contexto

- Antes de planejar, implementar ou revisar frontend, leia integralmente `rules/arquitetura-frontend.md` nesta pasta. Para backend, leia `rules/arquitetura-backend.md`. Em tarefas que cruzem as duas áreas, leia ambas.
- As regras são espelhos gerados de `.codex/arquitetura-frontend.mdc` e `.codex/arquitetura-backend.md`, que continuam sendo a fonte única. Edite decisões na origem e execute `pwsh -File .claude/scripts/sync-architecture.ps1` a partir da raiz. Para verificar divergências, acrescente `-Check`.
- Consulte `AGENTS.md`, `README.md`, os requisitos e os planos referenciados pelas regras antes de decidir comportamento funcional. Documentos preliminares não equivalem a requisitos aprovados.
- Frontend: `hortacomunitaria-webapp/`, React/TypeScript/Vite/Tailwind. Backend: `HortaComunitaria.Api/`, .NET/Minimal APIs/EF Core/PostgreSQL. Não criar outra aplicação ou arquitetura paralela.

## Trabalho de interface

- Use a skill `ui-ux-pro-max` para tarefas de estrutura visual, componentes, responsividade e UX. Leia primeiro o contexto da aplicação em `.claude/skills/ui-ux-pro-max/references/horta-context.md`.
- Direção definida em 06/11/2026: decisões de UX guiadas por quem desenvolve; interface administrativa sóbria e compacta; cores atuais da horta; desktop prioritário, com celular completo.
- Preserve o cadastro interativo e a renderização panorâmica quando a tarefa for uma reestruturação de gestão. Alterações nessas áreas exigem pedido específico; pedidos anteriores não são autorização permanente.

## Modelos de trabalho

Use Opus 5.5 com esforço médio para desenvolvimento e Haiku para exploração, conforme `rules/modelos.md`. O padrão está em `.claude/settings.json`; o explorador somente leitura está em `.claude/agents/Explore.md`.

## Verificação e entrega

- Frontend: em `hortacomunitaria-webapp`, use `npm ci`, `npm run build` e `npm run lint` conforme a tarefa; confira visual e teclado quando houver alteração de interface.
- Backend: `dotnet build HortaComunitaria.Api/HortaComunitaria.Api.csproj -c Release`; demais verificações seguem as regras da área.
- Nesta fase, o responsável dispensou testes de integração. Não execute a suíte de integração/E2E com API e banco por padrão. Registre o que foi e não foi validado; isso não remove requisitos futuros de validação.
- Não executar seed automático, migrations ou comandos de escrita no banco para conferir uma tela. Dados demonstrativos devem continuar identificados como fictícios.
- Não versionar dependências instaladas, builds, capturas, logs, configurações locais ou credenciais. Documentação final fica em `documentos/`, conforme `AGENTS.md`.
- Execute commit/push quando solicitados. Ao concluir, informe alterações, validações e limitações concretas.

## Referências da configuração

- [Memória e regras do Claude Code](https://code.claude.com/docs/en/memory)
- [Skills do Claude Code](https://code.claude.com/docs/en/skills)

Estrutura baseada na documentação oficial consultada em 06/11/2026. Inclui seleção de modelo e explorador somente leitura; não adiciona hooks, permissões amplas ou configurações pessoais.
