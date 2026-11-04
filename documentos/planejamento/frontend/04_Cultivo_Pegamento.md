# FE04 — Espécies, origem, ciclos, plantio e pegamento

**Requisitos:** RF03/RF04/RF05/RF08/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 04](../mvp/04_Cultivo_Pegamento.md), [modelo de cultivo](../../arquitetura/Modelo_Cultivo_Producao.md) e [aceite comum](Comportamentos_Aceite.md). **Dependência:** canteiros/configurações de FE02/API 02.

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso completo

Cadastrar espécie/cultivar → registrar origem conhecida do material → abrir ciclo com área de referência → registrar plantio ou reposição → avaliar sobrevivência → acompanhar observações → consultar pegamento e registros de origem → corrigir informações → encerrar ciclo sem invalidar seus registros dependentes.

## Telas e formulários

| Tela/seção | Dados e ações |
| --- | --- |
| Espécies/cultivares | Nome comum/científico e descrição disponíveis; cultivar pertencente à espécie; cadastro, edição, inativação e histórico |
| Origem do material | Código, espécie/cultivar, tipo de material, origem/data de recebimento e observações conhecidas; lista/detalhe/gestão, sem estoque |
| Ciclos por canteiro | Espécie/cultivar, início, área ocupada, responsável e situação; filtros de canteiro/espécie/período/situação; abrir ciclo e consultar referência de área |
| Detalhe do ciclo | Identificação/período/área original e seções de plantios, avaliações, observações e colheitas quando FE05 estiver entregue; correção, encerramento e histórico |
| Plantio/replantio | Ciclo, origem opcional, data, natureza, quantidade inteira, espaçamentos conhecidos e responsável; reposição com referência explícita ao plantio original |
| Avaliação | Plantio, data, sobreviventes, método, responsável/observação; conferir quantidade e avaliação anterior sem somá-las |
| Observação agronômica | Ciclo, data, categoria/descrição, responsável e medida/unidade/método opcionais; fotografias só após FE10 |
| Pegamento | Período do plantio, espécie, natureza/método; percentual, base elegível, plantios sem avaliação e avaliação selecionada em cada origem |

Espécie/cultivar globais e lotes de origem da horta são contextos distintos. Informações desconhecidas não geram cadastros fictícios. A troca de espécie remove cultivar/lote incompatível do rascunho e refaz seletores, sem mudar fatos já salvos.

## Integração necessária

APIs de espécies/cultivares/lotes, ciclos/fechamento, plantios, avaliações e observações, gestão/histórico e seedling-establishment estão **planejadas** em 04.1–04.4. Antes de cada etapa, verificar contrato real e catálogo elegível por contexto/período. Os nomes preliminares de entidades não são tipos do frontend. O detalhe deve trazer a referência histórica de área e os dados necessários à correção completa, sem consultar toda a produção para montar um formulário.

## Estados específicos

- Canteiro sem configuração na data não recebe área inventada; mostrar ausência e caminho de revisão em FE02, quando disponível.
- Origem/cultivar desconhecida continua opcional. Material desconhecido ou não elegível ao pegamento aparece na cobertura retornada, sem percentual presumido.
- Reposição é outro plantio; não incrementar silenciosamente o plantio original. Avaliações são consultadas separadamente, com a escolhida pela API identificada.
- Suspeita de praga/doença não exige diagnóstico; medida conhecida exige unidade/método. Sem fotografia, a observação textual permanece possível.
- Encerramento usa data/motivo/versão. Dependentes fora do intervalo ou quantidade abaixo de sobreviventes provocam revisão explícita; não cascata de alterações/invalidações.
- Não impor um único ciclo ativo por canteiro, calendário de consórcio, ocupação automática ou reabertura sem decisão/contrato.

## Etapas

| Etapa | Entrega de frontend | Dependência e aceite | Estado |
| --- | --- | --- | --- |
| FE04.1 | Espécies/cultivares, origem e seletores | API 04.1; origem opcional e consistência entre referências | Não iniciada |
| FE04.2 | Ciclo, plantio/reposição, encerramento e gestão/histórico | API 04.2; área de referência preservada e proteção de dependentes | Não iniciada |
| FE04.3 | Avaliações e observações no ciclo/plantio | API 04.3; contagem válida, suspeita sem diagnóstico, medidas coerentes | Não iniciada |
| FE04.4 | Pegamento com cobertura e origem | API 04.4; método/natureza e avaliação escolhida identificados | Não iniciada |

## Aceite verificável

Os percentuais abaixo são exemplos da hipótese descrita no plano funcional, sujeitos à validação da PO; são conferidos pelo retorno da API.

1. 40 mudas e avaliação com 36 sobreviventes mostram 90%; avaliação seguinte com 32 substitui a selecionada e não soma 68.
2. Reposição de quatro mudas fica separada do plantio original de 40; história da origem e avaliação é consultável.
3. Plantios 40/36 e 10/5 do mesmo grupo mostram 41/50 = 82%, sem média simples; outro sem avaliação gera cobertura parcial.
4. Mais sobreviventes que plantados, data anterior ou lote/cultivar incompatível geram erros associados e rascunho preservado.
5. Reduzir plantio abaixo de avaliação válida, alterar espécie incompatível com dependentes ou fechar ciclo com fatos fora do período apresenta conflito revisável.
6. Duas colheitas futuras aparecem no mesmo ciclo, separado do seguinte; mudança de configuração do canteiro não muda a área de referência desse ciclo.
7. Correção/invalidação permitidas pelo contrato atualizam consulta/pegamento/histórico; celular permite usar formulários e seções sem exigir planilha horizontal.

**Pendências da PO:** consórcio/ocupação, janela/método de avaliação, material elegível e seleção da avaliação. A implementação dependente aguarda definição; o planejamento não aprova hipóteses. **Evidências:** pendentes.
