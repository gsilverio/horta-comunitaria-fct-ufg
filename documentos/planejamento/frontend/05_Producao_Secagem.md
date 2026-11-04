# FE05 — Colheita, produtividade, perdas e secagem

**Requisitos:** RF09/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 05](../mvp/05_Producao_Secagem.md), [dados dos indicadores](../../arquitetura/Dados_Indicadores.md) e [aceite comum](Comportamentos_Aceite.md). **Dependência:** ciclos de FE04/API 04.

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso

Abrir ciclo → registrar colheita/massas conhecidas → consultar produtividade e perdas → selecionar origens para um lote de secagem → destinar massa → acompanhar/finalizar com pesagem seca → consultar rendimento e histórico → corrigir/inutilizar preservando dependências e saldos.

## Telas, campos e ações

| Tela/seção | Conteúdo |
| --- | --- |
| Colheitas | Período da colheita, ciclo/espécie e validade; massas fresca/aproveitável, responsável, detalhe e ações de gestão |
| Formulário de colheita | Ciclo, data, massa fresca em kg, massa aproveitável quando medida, motivo de perda e responsável conhecido |
| Detalhe de colheita | Origem/ciclo/área de referência, massas, destinações e saldo conhecido retornado; corrigir/invalidar e consultar histórico |
| Lotes de secagem | Código, início/fim, situação, método/responsável; filtros pelo início e situação; criar/abrir/corrigir lote |
| Composição de lote | Selecionar uma ou várias colheitas e massa destinada por origem; conferir saldos atuais; representação completa na correção |
| Finalização | Fim, massa seca total conhecida, motivo e versão; confirmação pelo servidor |
| Produtividade/perdas/rendimento | Três resultados separados com unidade, competência, cobertura e origem; filtros efetivamente suportados |

Massas são informadas em kg, sem conversão implícita de gramas ou soma de massa fresca com seca. Ausência de pesagem aproveitável/seca não é zero. Se peso seco ainda não foi medido, manter lote pendente/InProgress conforme proposta funcional, sem finalização falsa.

## Integração necessária

Todas as APIs deste fluxo estão **planejadas**: colheitas e sua gestão/histórico; lotes/composição/finalização/gestão/histórico; produtividade, perdas e rendimento. Etapas 05.1–05.4 determinam disponibilidade. Confirmar retorno de destinações/saldo, conflitos e referências elegíveis; não inferir saldo subtraindo dados da página de origens.

## Estados específicos

- Na hipótese do protótipo, colheita sem massa aproveitável conhecida não permite destinação: indicar pesagem pendente e ação de corrigir origem, sem assumir disponibilidade da massa fresca.
- Seleção/alteração de origem refaz saldo e contexto. Uma colheita não aparece duas vezes no mesmo lote; lotes distintos podem usar partes da mesma colheita quando o servidor confirmar saldo.
- Saldo insuficiente/conflito concorrente preserva composição proposta e oferece revisar estado atual; não reduz a quantidade automaticamente para “fazer caber”.
- Reduzir/inutilizar colheita com lote válido dependente exige revisar esse lote. Correção de lote mostra listas anterior/nova e motivo no histórico.
- Lote misto conserva todas as origens. Rendimento por espécie sem atribuição confiável é indisponível; não ratear massa seca ou inventar produtividade global.

## Etapas

| Etapa | Entrega | Dependência e aceite | Estado |
| --- | --- | --- | --- |
| FE05.1 | Registro/lista/detalhe/gestão de colheita, integrado ao ciclo | API 05.1; massas distintas e ausência preservada | Não iniciada |
| FE05.2 | Produtividade e perdas com origens | API 05.2; área uma vez por ciclo e perda com cobertura | Não iniciada |
| FE05.3 | Lotes/destinações/saldo/finalização e histórico | API 05.3; composição real e conflito concorrente revisável | Não iniciada |
| FE05.4 | Rendimento por lote e recortes elegíveis | API 05.4; massa destinada como base e lote misto identificado | Não iniciada |

## Aceite verificável

Conferir as hipóteses de fórmula com o servidor e registrar sua validação funcional separadamente.

1. Colheitas de 3 + 2 kg em ciclo de 5 m² mostram 5 kg e 1 kg/m²; a área não é duplicada pelo número de colheitas.
2. 10 kg frescos/8 aproveitáveis mostram 2 kg e 20% de perda na hipótese; aproveitável null não mostra perda zero.
3. Lote com 3 + 2 kg destinados e 1 kg seco mostra rendimento de 20%; não atribui 0,6/0,4 kg às origens automaticamente.
4. Colheita com 4 kg aproveitáveis e duas tentativas concorrentes de destinar 3 kg: somente uma confirma; a outra mantém rascunho e informa conflito, sem lote parcial.
5. Corrigir/inutilizar lote modifica saldo/rendimento e preserva antes/depois; correção de colheita incompatível com destinações é bloqueada com vínculos revisáveis.
6. Lote misto por espécie não oferece percentual atribuído sem base; ausência de colheita e coleta incompleta são situações distintas.
7. Paginação/retorno ao ciclo preservam recorte; celular permite editar composição de lote por itens legíveis, sem tabela de entrada horizontal.

**Pendências da PO:** base fresca/área, perda de triagem, pesagem antes da destinação, competência e conclusão da secagem. **Evidências:** pendentes.
