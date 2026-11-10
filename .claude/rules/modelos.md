# Modelos por responsabilidade

- Desenvolvimento, decisões de implementação, alterações de código e revisão final: Opus 5.5 (`claude-opus-5-5`), com esforço médio (`medium`). O padrão do projeto está em `.claude/settings.json`.
- Exploração do repositório, localização de arquivos, levantamento de referências e entendimento inicial: delegar ao subagente `Explore`, configurado com `model: haiku` em `.claude/agents/Explore.md`. O Haiku explora e resume; a implementação permanece com Opus 5.5.
- Enviar ao explorador uma pergunta delimitada, os caminhos relevantes e as restrições da tarefa. Solicitar um resumo com evidências de arquivo/linha, evitando trazer arquivos inteiros ao contexto principal.
- O explorador trabalha somente em leitura. Não lhe atribuir alterações, comandos de escrita, aplicação de migrations, preparação de dados ou commit/push.
- A preferência de modelo não altera a arquitetura, o escopo autorizado ou as validações exigidas. Se o modelo solicitado estiver indisponível na conta/provedor, informar a limitação; não declarar que está usando um modelo diferente do efetivo.
- Configurações de sessão, ambiente ou organização podem prevalecer sobre o padrão do projeto. Conferir o modelo/esforço efetivos no Claude Code ao iniciar; instruções Markdown não trocam o modelo de uma sessão já aberta.
