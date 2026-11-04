# Etapa 8 — Testes, validação com usuários e disponibilização

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Validação começa na etapa 1 e acompanha todas as entregas. Nesta fase avaliar interação e geometria; cenários de API/permissões/publicação pertencem a uma futura integração, sem bloquear o protótipo local.

**Status:** planejamento; implementação não iniciada nesta revisão. As tarefas originais abaixo são referência de evolução, subordinadas ao recorte revisado. Não constituem autorização de implementação.

## Conteúdo da proposta recebida

**Objetivo:** garantir estabilidade e adequação ao espaço físico real.

### Tarefas

- [ ] Criar testes unitários para geometrias, conversão de coordenadas, cálculos de área e validações.
- [ ] Criar testes de integração para operações em lote, IDs e regras de permissão.
- [ ] Testar salvamento/carregamento de layouts retangulares e mandalas.
- [ ] Testar navegação com zoom/pan, seleção e edição em diferentes tamanhos de tela.
- [ ] Testar datasets representativos, incluindo proposta demonstrativa de 20 canteiros e cinco setores.
- [ ] Validar com a equipe responsável medidas, formas, nomenclatura e associação setorial.
- [ ] Realizar testes de usabilidade: criar um canteiro, ajustar medidas, gerar mandala, salvar e consultar.
- [ ] Criar documentação curta de uso do editor.
- [ ] Disponibilizar inicialmente por permissão ou feature flag, mantendo CRUDs acessíveis.
- [ ] Registrar limitações e oportunidades para versões futuras.

**Entregável:** versão 1 utilizável e apta a ser integrada ao fluxo normal da plataforma.

**Critérios de conclusão:**

- Usuários conseguem executar os fluxos essenciais sem treinamento extenso.
- O layout salvo reproduz corretamente medidas e formas após nova abertura.
- Nenhuma ação no editor compromete registros utilizados pelos CRUDs.
- As limitações conhecidas estão documentadas e aceitas pela equipe.

---
