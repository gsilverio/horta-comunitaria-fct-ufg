# Caso 12 — Percorrer irrigação pela interface

**Etapa:** [4](../Etapa_04_Interface_PO.md). **Estado:** concluído tecnicamente em 08/10/2026; aceite da PO pendente. **Referência:** RF07/RF13/RF14 e RNF04.

Dependência: todos os endpoints deste fluxo verificados. Ler a [arquitetura do frontend](../../../../.codex/arquitetura-frontend.mdc), o [contrato](../Contrato_Fluxo.md) e a etapa 4, que define verificações e roteiro da PO.

## Percurso e estados

1. Selecionar horta/setor; buscar canteiros na data de início e responsável opcional. Limpar contextos incompatíveis e descartar respostas antigas.
2. Informar formulário com unidades/null explícitos. Gerar operationId da tentativa e enviar à API. Em erro recuperável, manter dados e chave; sucesso mostra detalhe confirmado.
3. Selecionar período/setor e consultar listagem/consumo. Exibir ausência, indisponibilidade e parcialidade conforme API. Não recalcular indicador na interface.
4. Abrir detalhe/histórico, corrigir ou invalidar com motivo/expectedVersion e recarregar dados afetados.
5. Em 409, preservar rascunho e oferecer atualização do estado. Não trocar versão e reenviar sem revisão.

## Alterações e aceite

- Componentes de formulário, lista, resumo por setor, detalhe e histórico dentro de `hortacomunitaria-webapp/src/features/irrigation`.
- Cliente HTTP tipado pela API efetiva, tratando ProblemDetails; seletor não pode descartar silenciosamente opções de páginas posteriores.
- Percurso real 120 → 100 → Invalid confirma persistência, histórico e dashboard. Retry não duplica evento.
- Loading/erro/vazio, null/zero, layout de celular e fuso de navegador diferente conferidos.
- Autoria demonstrativa é exibida; não há telas de login, perfis ou módulos futuros.

**Limite:** finalizar conforme etapa 4; reunião/aceite real da PO permanece pendente até acontecer.

## Evidência

Interface implementada em `hortacomunitaria-webapp` com React/TypeScript/Vite/Tailwind, cliente Axios e API existente. Instalação reproduzível, build e lint aprovados; 12 testes com API/PostgreSQL reais, incluindo 120 → 100 → Invalid/NoData, retry, conflito, catálogos paginados, precisão/fuso e layout de celular/teclado. Consulte as [evidências da etapa](../Etapa_04_Interface_PO.md#evidências-da-execução) e o [roteiro da PO](../../Validacao_PO_MVP_Irrigacao.md), ainda sem respostas/aceite.
