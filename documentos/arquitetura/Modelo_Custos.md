# Modelo de custos básicos

**Cobertura:** RF16, com acesso, evidências, histórico e consultas de RF01/RF12–RF14. Ver [convenções comuns](Modelagem_Preliminar_Dados_Horta.md) e [diagramas](Diagramas_Relacionais_Horta.md#custos).

O fluxo registra despesas previstas ou realizadas e permite consultá-las por período e categoria. A proposta mantém um único tipo de lançamento, sem contabilidade completa.

## Entidade e informações

| Entidade | Campos propostos, além de `id` e metadados comuns | Relações |
| --- | --- | --- |
| `lancamento_financeiro` | `horta_id`, `data`, `descricao`, `categoria`, `valor_brl`, `natureza`, `canteiro_id?`, `ciclo_id?` | Uma horta; no máximo um contexto específico |

Data é de calendário; valor é decimal em reais nesta proposta. Natureza distingue previsto/realizado. Categoria é uma classificação a validar, como implantação ou manutenção; não confundir com um plano de contas.

## Relações e integridade

- Sem contexto específico, a despesa é geral da horta. Pode apontar para canteiro ou ciclo, nunca para ambos na mesma linha.
- Ciclo permite derivar o canteiro e a espécie. O contexto precisa pertencer à horta do lançamento.
- Valor não pode ser negativo. Valor desconhecido não constitui uma despesa de zero; permanece uma informação não coletada antes de confirmar o lançamento.
- Despesa realizada pode existir sem previsão. Não exigir pareamento individual nem inferir que previsto e realizado sejam dois pagamentos.
- Totais de previsto e realizado aparecem separados. A comparação exige períodos, categorias e contextos compatíveis.
- Arquivos comprovantes usam o vínculo de evidência, respeitando permissões.
- Correção/invalidação preserva o histórico e modifica os totais elegíveis.

## Uso nos indicadores

**Exemplo:** R$ 90 previstos e R$ 100 realizados aparecem em duas naturezas, com diferença exemplificativa de R$ 10. Não são somados como despesa de R$ 190.

Valores gerais não são distribuídos automaticamente entre canteiros ou espécies. Uso de insumo não gera custo por conta própria. Custo de produção, rateio, receitas, estoque, pagamentos e faturamento não fazem parte deste modelo inicial.

## Decisões a validar

Categorias, competência da data, moeda única, obrigatoriedade de comprovantes, recortes da comparação e identificação do contexto conhecido. Custo por espécie só pode usar lançamentos diretamente vinculados ao ciclo, deixando claro que não inclui custos gerais não atribuídos.
