# FE08 — Participação social e educação

**Requisitos:** RF15/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 08](../mvp/08_Social_Educacao.md) e [aceite comum](Comportamentos_Aceite.md). **Dependência:** atividade FE06/API 06.

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso e interface

Criar ou editar atividade → indicar dimensões operacional/social/educacional → descrever objetivo/modalidade → informar participantes nominais, público agregado e instituições → consultar participação e histórico.

Estender o formulário e o detalhe de FE06; uma atividade pode ter várias dimensões e aparece nas consultas correspondentes com a mesma identidade. Não criar uma atividade educacional adicional para contabilizar o mesmo encontro.

| Seção | Campos e ações |
| --- | --- |
| Dimensões e caracterização | Pelo menos uma dimensão; objetivo/modalidade e campos exigidos pelo contrato para social/educacional |
| Participantes | Reutilizar linhas de FE06; identificar quem conta como público e horas conhecidas individualmente |
| Público agregado | Quantidade total quando conhecida e identificação de cobertura nominal completa/parcial |
| Instituições | Cadastrar/manter conforme contrato, selecionar vínculos sem repetir instituição na mesma atividade |
| Consulta | Filtrar período, dimensão/modalidade e instituições quando suportado; abrir mesma atividade de origem |
| Resumo | Presenças, participantes distintos identificáveis, atividades e cobertura; não somar grandezas diferentes |

Público agregado inclui os participantes nominais marcados como público. Exemplo: total 20, dos quais oito identificados, significa 20 presenças, não 28. Com cobertura nominal completa, o total conhecido deve corresponder aos nominais; cobertura parcial permite total maior, nunca menor. Pessoas anônimas do total agregado não sustentam cálculo de participantes únicos.

## Etapas

| Etapa | Entrega | Dependência |
| --- | --- | --- |
| FE08.1 | Dimensões/caracterização e manutenção de instituições | API 08.1; FE06.2 |
| FE08.2 | Público agregado/nominal e instituições na mesma atividade; correção e histórico | Contratos 08.1; corresponde à entrega de formulário 08.2 |
| FE08.3 | Consultas sociais/educacionais com cobertura e origem | API 08.3; FE11 |

## Cenários de aceite

- Atividade operacional e educacional aparece em ambos os recortes sem duplicar suas horas.
- Total 20 com oito nominais parciais apresenta 20 presenças e oito pessoas identificáveis nessa atividade.
- Uma pessoa em dois encontros representa duas presenças e uma identidade distinta no recorte, conforme API.
- Total desconhecido continua desconhecido; não preenchê-lo com zero ou número de linhas por conveniência.
- Rejeitar total inferior aos nominais ou cobertura completa inconsistente, explicando junto aos campos.
- Correção das participações atualiza o mesmo histórico e os resumos afetados; nomes pessoais não vão a logs/URLs.

Pendentes de PO: modalidades, caracterização mínima, interpretação de público e recortes. Não acrescentar inscrições, certificados ou controle escolar.
