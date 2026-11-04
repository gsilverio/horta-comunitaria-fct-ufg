# FE09 — Custos previstos e realizados

**Requisitos:** RF16/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 09](../mvp/09_Custos.md) e [aceite comum](Comportamentos_Aceite.md).

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso e telas

Selecionar horta → registrar lançamento previsto ou realizado → consultar por competência/natureza/contexto → comparar valores conhecidos → abrir origem → corrigir/inutilizar com motivo e histórico.

| Tela | Conteúdo |
| --- | --- |
| Lançamentos | Data de competência, natureza, categoria, descrição, valor em BRL e contexto; criar, filtrar, paginar e detalhar |
| Formulário | Natureza prevista/realizada obrigatória, data, categoria/descrição, valor obrigatório com duas casas; contexto geral ou canteiro/ciclo opcional |
| Detalhe | Origem/contexto, valor/natureza, autoria/versão, correção, invalidação e histórico |
| Comparação | Totais previstos/realizados conhecidos no mesmo recorte, disponibilidade e diferença com definição explícita |

Naturezas são lançamentos independentes. Não converter automaticamente previsto em realizado nem vincular um ao outro sem contrato. Valor vazio é erro, não lançamento zero. Zero, se permitido pelo contrato, é uma informação explícita. Exibir BRL e rejeitar precisão excessiva sem arredondar silenciosamente.

Quando apenas uma natureza tem registros, mostrar a ausência da outra e a cobertura retornada. A comparação proposta é realizado menos previsto no recorte conhecido; sua interpretação depende da definição e do aceite da PO. Não chamar o resultado de lucro, economia comprovada ou orçamento total.

## Etapas e aceite

| Etapa | Entrega | Dependência |
| --- | --- | --- |
| FE09.1 | Criar/listar/detalhar lançamentos com recortes de competência | API 09.1; horta. Ciclo apenas quando entregue |
| FE09.2 | Correção/invalidação e histórico | API 09.2 |
| FE09.3 | Comparação previsto/realizado com origens e cobertura | API 09.3; FE11 |

- Cadastrar previsto 100 e realizado 120 no mesmo recorte; mostrar totais e diferença 20 conforme API, sem pareamento fictício.
- Consultar período vazio: ausência de dados permanece distinta de total zero observado.
- Registrar custo geral sem canteiro/ciclo; selecionar contexto de outra horta deve ser rejeitado pelo servidor.
- Entrada com três casas decimais gera erro; edição/reload preservam natureza, competência e valor.
- Corrigir/inutilizar lançamento recompõe comparação e preserva histórico; conflito mantém rascunho.

Categorias, regra de valores e interpretação econômica precisam seguir a validação do plano funcional. Sem pagamentos, fornecedores, estoque, rateio automático ou contabilidade completa.
