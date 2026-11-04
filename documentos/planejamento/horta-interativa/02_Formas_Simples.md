# Etapa 2 — Ferramentas de criação e edição de geometrias simples

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Continuar no rascunho local: retângulos e círculos, painel numérico e seleção por lista HTML. Setor, nesta experiência, é apenas agrupamento visual demonstrativo, sem criar ou alterar IrrigationSector.

**Status:** implementado como E1 no frontend, em rascunho local. Persistência, cadastro de entidades reais e integração operacional continuam fora desta etapa.

## Conteúdo da proposta recebida

**Objetivo:** criar, editar e representar setores e canteiros de formatos básicos.

### Tarefas

- [x] Criar modelo de `LayoutElement` independente da biblioteca de desenho.
- [x] Implementar ferramentas `select`, `pan`, `rectangle` e `circle`.
- [x] Criar objetos por clique/arraste e por botões de inserção.
- [x] Mostrar medidas em metros durante a criação e no painel.
- [x] Selecionar objetos e exibir painel lateral de propriedades.
- [x] Permitir mover, redimensionar, rotacionar retângulos e alterar raio de círculos.
- [x] Normalizar os valores no modelo local, sem escala interna de renderização.
- [x] Implementar exclusão com confirmação.
- [x] Implementar estados visuais diferentes para retângulo, círculo, selecionado e conflito.
- [x] Adicionar o sistema de ferramentas, com uma ferramenta ativa por vez.

**Entregável:** criação e edição de retângulos/círculos em memória, com propriedades numéricas, seleção, remoção, rotação de retângulos e raio de círculos.

**Critérios de conclusão:**

- Um retângulo de 1 × 5 m mantém essa dimensão após ser movido e recarregado do estado local.
- O painel de propriedades e o desenho permanecem sincronizados.
- Seleção e arraste não acionam acidentalmente a criação de novas formas.

---
