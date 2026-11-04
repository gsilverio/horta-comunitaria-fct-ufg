# Planejamento dos fluxos iniciais do MVP

**Estado:** irrigação entregue com interface; backend dos fluxos 02–09 implementado nesta execução autorizada, com interfaces implementadas tecnicamente; RF12 adiado pelo responsável. [Evidências do backend](../Execucao_Backend_MVP.md). **Base funcional:** [requisitos 0.0.1](../../Documento_Tecnico_Requisitos_Horta.md), enviados para validação da PO, e [anexo da professora](../../Dados_e_Indicadores_Preliminares_Horta_APOEMA.pdf).

O MVP reúne os registros manuais e seus dashboards. Cada fluxo abaixo tem seu próprio Markdown, com dependências, etapas de implementação, regras de negócio, contratos e exemplos de aceite. Não é uma autorização para implementar todas as etapas numa única tarefa.

Autenticação/autorização (RF01) ficam fora deste planejamento, por decisão do responsável pelo projeto. Não há etapa de login, tokens ou permissões; a autoria demonstrativa segue a [arquitetura](../../../.codex/arquitetura-backend.md). Nenhum outro requisito funcional é acrescentado para preencher a modelagem preliminar.

## Ordem de entrega proposta

| Ordem | Fluxo e plano | Requisitos | Dependências funcionais |
| --- | --- | --- | --- |
| 01 | [Irrigação manual, consulta e consumo](01_Irrigacao.md) | RF02 parcial, RF07, RF13, RF14 | Cadastros mínimos demonstrativos; quatro etapas já detalhadas |
| 02 | [Organização da horta e seus cadastros](02_Organizacao_Horta.md) | RF02 | Base de persistência e histórico do fluxo 01 |
| 03 | [Coletas ambientais e umidade do solo](03_Coletas_Ambientais.md) | RF06, RF13, RF14 | Organização e vínculos históricos |
| 04 | [Origem, cultivo, pegamento e observações](04_Cultivo_Pegamento.md) | RF03–RF05, RF08, RF13, RF14 | Canteiros e configurações de área |
| 05 | [Colheita, perdas e secagem](05_Producao_Secagem.md) | RF09, RF13, RF14 | Ciclos do fluxo 04 |
| 06 | [Atividades e horas de trabalho](06_Atividades_Horas.md) | RF10, RF13, RF14 | Horta, participantes; ciclo só quando usado |
| 07 | [Ocorrências e providências](07_Ocorrencias.md) | RF11, RF13, RF14 | Atividades para registrar intervenções |
| 08 | [Participação social e educação](08_Social_Educacao.md) | RF15, RF13, RF14 | Estende a mesma atividade do fluxo 06 |
| 09 | [Custos previstos e realizados](09_Custos.md) | RF16, RF13, RF14 | Horta; ciclo/canteiro opcionais |
| 10 | [Documentos e fotografias](10_Evidencias.md) | RF12, RF13 | Registros de destino implementados |

Essa ordem favorece a primeira demonstração pelo fluxo 01. Os fluxos 03 e 06–09 podem ser repriorizados com a PO respeitando suas dependências. Não implementar placeholders de tabelas/telas para antecipar fluxos posteriores.

## Como executar uma etapa

1. Receber autorização para **um fluxo e uma etapa**, ou para um caso de uso delimitado.
2. Ler AGENTS.md, arquitetura integral, [contrato comum](Contrato_Comum.md), plano do fluxo e caso de uso quando houver. O fluxo 01 também usa seu [contrato específico](../mvp-irrigacao/Contrato_Fluxo.md).
3. Confirmar dependências no código/testes. Criar somente entidades, migrations e slices necessários à etapa autorizada.
4. Implementar o percurso vertical até o banco e suas consultas; incluir interface na etapa indicada. Não criar uma fundação completa de todos os módulos antes dos casos de uso.
5. Executar verificações da etapa, registrar evidências e parar. Uma etapa parcialmente verificada não está concluída.

Prompt para agentes:

> Execute somente o fluxo XX, etapa Y, em documentos/planejamento/mvp, seguindo as leituras obrigatórias e os contratos. Não implemente RF01 nem outros fluxos. As hipóteses são do protótipo para apresentação à PO. Não invente regra para resolver uma pendência funcional: registre-a e deixe somente a parte dependente pendente. Implemente as regras no domínio, coordene persistência nos handlers e mantenha endpoints finos conforme .codex/arquitetura-backend.md. Execute os testes significativos da etapa; registre comandos, resultados e limitações. Atualize apenas o estado efetivamente concluído e pare.

## Cobertura dos indicadores

| Indicador prioritário | Fluxo responsável |
| --- | --- |
| Consumo de água e irrigação por setor | 01; completar contagem/duração por setor no fluxo 02 |
| Umidade do solo | 03 |
| Pegamento das mudas | 04 |
| Produtividade, perdas e rendimento após secagem | 05 |
| Horas de trabalho | 06 |
| Ocorrências operacionais | 07 |

RF13 é aplicado aos registros de cada fluxo, e RF14 aos seus indicadores; não são módulos genéricos de edição/fórmulas. Os fluxos 08 e 09 completam os recortes social, educacional e econômico usando seus dados disponíveis.

## Estado e validação

O fluxo 01 tem as quatro etapas concluídas tecnicamente. O backend dos fluxos 02–09 foi autorizado e implementado nesta execução; suas interfaces permanecem pendentes. RF12 foi adiado explicitamente pelo responsável. Consulte as [evidências do backend](../Execucao_Backend_MVP.md) e o [contrato implementado](../../arquitetura/Contrato_Backend_MVP.md). As hipóteses de formulário/indicador continuam sujeitas à PO; testes técnicos não aprovam requisitos funcionais. O [planejamento do frontend](../frontend/README.md) detalha a experiência ainda a implementar. O [Get Started](../../Get_Started.md) usa as operações de criação e gestão da API, sem seed automático.
