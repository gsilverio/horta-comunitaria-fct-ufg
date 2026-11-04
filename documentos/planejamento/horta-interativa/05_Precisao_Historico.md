# Etapa 5 — Ferramentas de produtividade e regras espaciais

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Desfazer/refazer, descarte, snap, encaixe angular e indicação de conflitos operam somente sobre o rascunho. As guias de alinhamento entre objetos ajudam a experimentar e não representam uma regra espacial persistida.

**Status:** recorte local parcialmente implementado como E1: desfazer/refazer, descarte, snap e indicação de sobreposição. Guias avançadas e regras espaciais completas continuam futuras.

## Conteúdo da proposta recebida

**Objetivo:** deixar a edição segura e confortável para uso real.

### Tarefas

- [x] Implementar snap-to-grid configurável de 0,5 m e opção de desligar o alinhamento.
- [x] Implementar guias de alinhamento entre objetos, com encaixe temporário nos eixos X e Y.
- [x] Implementar desfazer/refazer das operações do editor.
- [x] Distinguir alterações locais pendentes de alterações salvas.
- [x] Manter as formas dentro dos limites do terreno demonstrativo.
- [x] Detectar sobreposições e indicar os objetos envolvidos no rascunho.
- [x] Permitir reposicionamento preciso por campos numéricos.
- [x] Criar lista de objetos do layout e facilitar encontrar/selecionar um objeto por nome.
- [x] Exibir indicadores simples de medida, área e coordenadas.

**Entregável do recorte E1:** editor com histórico local de ações, snap configurável, encaixe de rotação em ângulos retos, guias de alinhamento, lista de objetos, campos de precisão e aviso de sobreposição.

No E1, a sobreposição é calculada pela interseção das geometrias primitivas do rascunho: retângulos orientados e círculos. O editor ainda permite o cruzamento para preservar a experimentação e apenas sinaliza os objetos envolvidos; isso não é uma regra espacial persistida nem substitui uma validação futura para polígonos e curvas.

**Critérios de conclusão:**

- O usuário pode desfazer uma movimentação ou exclusão acidental.
- Uma sobreposição inválida é identificada antes do salvamento.
- Existe forma acessível de editar sem depender apenas de drag-and-drop.

---
