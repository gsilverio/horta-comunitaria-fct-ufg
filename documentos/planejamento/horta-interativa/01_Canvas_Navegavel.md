# Etapa 1 — Prova de conceito do canvas 2D (sem backend)

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Primeiro incremento recomendado: tela experimental dentro da aplicação existente, dados fictícios em memória, terreno retangular com medidas explicitamente demonstrativas, grid, zoom/pan e um canteiro de 1 × 5 m. Nenhuma escrita HTTP.

**Status:** planejamento; implementação não iniciada nesta revisão. As tarefas originais abaixo são referência de evolução, subordinadas ao recorte revisado. Não constituem autorização de implementação.

## Conteúdo da proposta recebida

**Objetivo:** obter uma superfície visual navegável e medível.

### Tarefas

- [ ] Criar um módulo/tela isolada `HortaInterativa`.
- [ ] Implementar `Stage` e `Layer` do React Konva.
- [ ] Configurar tamanho responsivo do canvas e atualização no redimensionamento da tela.
- [ ] Implementar grid visível/oculto, com passo configurável.
- [ ] Definir conversão **metros ↔ pixels** e sistema de coordenadas local.
- [ ] Implementar zoom com limites mínimo e máximo, preferencialmente centrado no cursor.
- [ ] Implementar pan sem interferir no arraste dos objetos.
- [ ] Exibir limite demonstrativo de uma horta e marcação de escala.
- [ ] Testar seleção e movimentação de um retângulo de exemplo.

**Entregável:** editor demonstrativo com canvas, grid, zoom, pan, retângulo e escala.

**Critérios de conclusão:**

- Grid e objetos permanecem alinhados após zoom e pan.
- Posições e medidas não se alteram após aproximar/afastar.
- O canvas cabe adequadamente na interface em desktop e comporta navegação básica por toque.
- Nenhum dado ainda precisa ser salvo no banco.

---
