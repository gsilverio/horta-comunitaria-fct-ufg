# Etapa 3 — Polígonos e canteiros curvos

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Introduzir polígonos e segmentos de anel após validar as interações básicas. Documentar origem, eixos, pivô de rotação, orientação dos ângulos, cruzamento de 360 graus e tolerância geométrica.

**Status:** planejamento; implementação não iniciada nesta revisão. As tarefas originais abaixo são referência de evolução, subordinadas ao recorte revisado. Não constituem autorização de implementação.

## Conteúdo da proposta recebida

**Objetivo:** permitir geometrias incompatíveis com um grid retangular rígido.

### Tarefas

- [ ] Implementar ferramenta `polygon`: clique para adicionar pontos, concluir e cancelar desenho.
- [ ] Implementar edição de vértices de polígonos.
- [ ] Validar quantidade mínima de pontos, área positiva e auto-interseção.
- [ ] Implementar `annular-sector` com `Shape`/caminho personalizado.
- [ ] Permitir editar centro, raios e ângulos pelo painel de propriedades.
- [ ] Criar *handles* apropriados para formatos curvos (não depender apenas de `Transformer`).
- [ ] Desenvolver funções puras para área, limites e pontos de contorno.
- [ ] Testar seleção por clique em formas côncavas ou curvas.
- [ ] Definir convenção para ângulos (unidade, direção de crescimento e normalização).

**Entregável:** editor com quatro tipos de geometria: retângulo, círculo, polígono e segmento de anel.

**Critérios de conclusão:**

- É possível desenhar um canteiro curvo e mudar seu raio interno/externo.
- É possível desenhar um setor irregular por pontos.
- Formas inválidas são rejeitadas com orientação ao usuário.
- Testes unitários cobrem os cálculos geométricos principais.

---
