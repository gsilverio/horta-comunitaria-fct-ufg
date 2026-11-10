# Orientações do repositório

Antes de planejar, implementar ou revisar o backend, leia integralmente e siga [.codex/arquitetura-backend.md](.codex/arquitetura-backend.md). Esse arquivo é a fonte única das regras arquiteturais do backend; mantenha as decisões nele, sem duplicá-las aqui.

Antes de planejar, implementar ou revisar o frontend, leia integralmente e siga [.codex/arquitetura-frontend.mdc](.codex/arquitetura-frontend.mdc). Esse arquivo é a fonte única das regras arquiteturais do frontend; mantenha as decisões nele, sem duplicá-las aqui.

Consulte os requisitos e a modelagem preliminar referenciados nas regras. A criação das regras não autoriza iniciar a implementação da aplicação nem transforma decisões funcionais pendentes em requisitos aprovados.

O Claude Code usa a configuração em [.claude/CLAUDE.md](.claude/CLAUDE.md). As regras completas em `.claude/rules/` são espelhos gerados das duas fontes acima. Ao alterar uma regra de arquitetura em `.codex`, execute `pwsh -File .claude/scripts/sync-architecture.ps1` e confira com `-Check`. Não editar os espelhos diretamente nem manter decisões diferentes entre os agentes.

## Organização da documentação

- `documentos/`: requisitos em Markdown, PDF e DOCX e documentos gerais recebidos do projeto.
- `documentos/arquitetura/`: modelagem de dados, diagramas e explicações ou decisões técnicas.
- `documentos/planejamento/`: fluxos de uso e planejamento das entregas, vinculados aos requisitos.
- `.codex/`: instruções de desenvolvimento para o agente.

Atualize os links ao mover documentos. Não versionar arquivos temporários de geração/renderização; não manter cópias das entregas em `output/`.
