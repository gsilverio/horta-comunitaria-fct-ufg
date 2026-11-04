# Primeiro fluxo do MVP: irrigação manual e consumo de água

**Situação:** Etapas 1–4 implementadas e verificadas tecnicamente; roteiro de demonstração pronto, reunião/aceite da PO pendentes. **Data:** 08/10/2026.

## Objetivo e escolha do fluxo

Permitir que a equipe registre uma irrigação, confira sua origem no dashboard e corrija um erro sem perder o histórico. Esse recorte conecta coleta, armazenamento, gestão e visualização e usa o consumo de água, um dos indicadores prioritários do [anexo da professora](../../Dados_e_Indicadores_Preliminares_Horta_APOEMA.pdf). Não depende de a horta já ter produção ou de um fluxo completo de cultivo.

Fluxo demonstrável: **selecionar horta e setor → informar irrigação e canteiros atendidos → salvar → consultar registros e consumo por período/setor → corrigir ou invalidar com motivo → conferir histórico e novo resultado**.

É uma sugestão de primeira entrega para validar com a PO, dentro do [planejamento completo dos fluxos iniciais](../mvp/README.md). Não representa o MVP completo dos nove indicadores. As hipóteses de cálculo e formulário do [contrato](Contrato_Fluxo.md) tornam o protótipo implementável, mas não constituem aprovação da PO nem substituem os requisitos 0.0.1.

## Cobertura e limites

| Referência | Recorte desta entrega |
| --- | --- |
| RF02 | Consultar horta, setores e canteiros demonstrativos com vínculos históricos; manutenção de cadastros depois |
| RF07 | Registrar eventos manuais concluídos/interrompidos e canteiros atendidos |
| RF13 | Listar, detalhar, corrigir e invalidar apenas irrigações, preservando histórico |
| RF14 | Um indicador: consumo conhecido de água em litros, com cobertura e origem verificáveis |
| RNF01, RNF03, RNF04, RNF06 | Integridade, histórico, formulário pt-BR utilizável em celular e ausência de dados distinta de zero |

RF01 e controles de autorização ficam para o pós-MVP, por decisão do responsável pelo projeto. A autoria demonstrativa segue a [exceção arquitetural](../../../.codex/arquitetura-backend.md#1-escopo-e-decisões). Backup operacional (RNF05), hospedagem e uso em produção não são entregas desse protótipo local.

Ficam para outros planejamentos: CRUD completo de cadastros, mudança de setor pela interface, plantio, avaliações, produção, demais indicadores, medições ambientais, atividades, ocorrências, custos, anexos e administração de pessoas. Também não entram sensores, irrigação automática, compostagem, offline ou exportações.

## Etapas e dependências

| Ordem | Plano executável | Resultado verificável |
| --- | --- | --- |
| 1 | [Contexto mínimo e primeira consulta](Etapa_01_Contexto.md) | API → handler → PostgreSQL → resposta com cadastros demonstrativos |
| 2 | [Registro e consumo](Etapa_02_Registro_Consumo.md) | Irrigação persistida, consulta de origem e indicador pela API |
| 3 | [Correção e histórico](Etapa_03_Correcao_Historico.md) | Alteração atômica, conflito de versão e atualização dos resultados |
| 4 | [Interface e validação com a PO](Etapa_04_Interface_PO.md) | Percurso completo em React com dados da API e roteiro de demonstração |

Executar uma etapa por tarefa autorizada, em ordem. Cada etapa termina em sua própria verificação; não iniciar a seguinte automaticamente. A primeira cria a infraestrutura necessária para uma consulta real, sem gerar todas as pastas, tabelas ou abstrações da modelagem geral. O end-to-end completo com interface chega na etapa 4.

## Casos de uso em arquivos separados

Os planos de etapa coordenam a entrega. Os arquivos abaixo detalham cada fluxo implementável, usando o contrato comum para evitar definições divergentes.

| Etapa | Fluxo / planejamento |
| --- | --- |
| 1 | [Listar hortas](casos-de-uso/01_Listar_Hortas.md) |
| 1 | [Listar setores](casos-de-uso/02_Listar_Setores.md) |
| 1 | [Selecionar canteiros elegíveis](casos-de-uso/03_Selecionar_Canteiros.md) |
| 1 | [Selecionar responsável](casos-de-uso/04_Selecionar_Responsavel.md) |
| 2 | [Registrar irrigação](casos-de-uso/05_Registrar_Irrigacao.md) |
| 2 | [Listar irrigações](casos-de-uso/06_Listar_Irrigacoes.md) |
| 2 | [Consultar detalhe](casos-de-uso/07_Consultar_Irrigacao.md) |
| 2 | [Consultar consumo de água](casos-de-uso/08_Consultar_Consumo.md) |
| 3 | [Corrigir irrigação](casos-de-uso/09_Corrigir_Irrigacao.md) |
| 3 | [Invalidar irrigação](casos-de-uso/10_Invalidar_Irrigacao.md) |
| 3 | [Consultar histórico](casos-de-uso/11_Consultar_Historico.md) |
| 4 | [Percorrer o fluxo pela interface](casos-de-uso/12_Fluxo_Interface.md) |

## Como entregar a tarefa a outro agente

Prompt sugerido, substituindo `NN` pelo número autorizado:

> Implemente somente a Etapa NN do MVP de irrigação, ou o caso de uso expressamente indicado nessa etapa. Leia AGENTS.md, integralmente .codex/arquitetura-backend.md, documentos/planejamento/mvp-irrigacao/Contrato_Fluxo.md, o arquivo da etapa e o .md do caso de uso solicitado. Confirme as dependências pelo código e pelos testes, sem confiar apenas no status do plano. Não implemente outras etapas ou o modelo completo. Siga as rotas, regras e cenários definidos; decisões do protótipo continuam sujeitas à PO. Execute as verificações aplicáveis. Atualize o status apenas do que foi concluído, relate testes, arquivos alterados e limitações e pare ao terminar o escopo autorizado. Se houver incompatibilidade entre documentos, registre-a antes de ampliar o escopo.

Para reduzir releituras, o contrato concentra as decisões comuns; cada etapa lista os slices e cenários necessários. Consultar os modelos originais apenas para esclarecer relações, não como lista de tudo que deve ser construído. A arquitetura continua sendo a única fonte das convenções técnicas.

## Acompanhamento

| Etapa | Estado | Evidência |
| --- | --- | --- |
| 1 | Concluída tecnicamente | Restore bloqueado, build Release sem warnings, 29 testes com PostgreSQL real, SQL/snapshot revisados e smoke HTTP. [Evidências](Etapa_01_Contexto.md#evidências-da-execução) e [execução](../../arquitetura/Execucao_Local.md). Docker/compose não exercitados. |
| 2 | Concluída tecnicamente | Registro, listagem, detalhe e consumo; 84 testes com PostgreSQL real, build Release sem warnings, migration/snapshot/OpenAPI e smoke HTTP revisados. [Evidências](Etapa_02_Registro_Consumo.md#evidências-da-execução). Docker/compose não exercitados; PO pendente. |
| 3 | Concluída tecnicamente | Correção, invalidação e histórico; 126 testes com PostgreSQL real, build Release sem warnings, snapshot/OpenAPI e smoke HTTP revisados. [Evidências](Etapa_03_Correcao_Historico.md#evidências-da-execução). Docker/compose não exercitados; PO pendente. |
| 4 | Concluída tecnicamente | Interface React/TypeScript/Vite com Tailwind, build/lint e 12 testes com API/PostgreSQL reais; celular/teclado e fuso conferidos. [Evidências](Etapa_04_Interface_PO.md#evidências-da-execução) e [roteiro da PO](../Validacao_PO_MVP_Irrigacao.md). Reunião/aceite pendentes. |

Ao concluir uma etapa, preencher evidência com os comandos executados e os testes/cenários aprovados. Trabalho parcial continua em andamento. Não marcar aprovação da PO a partir de testes automatizados.

## Validação antes de ampliar o MVP

Na demonstração, confirmar com a PO: se irrigação é um bom primeiro registro; como obter e classificar volume; se setor e canteiros bastam como contexto; quais campos são indispensáveis; e como tratar eventos sem volume e que atravessam dias. Registrar aceites, ajustes e pendências conforme a etapa 4. Só então planejar o próximo fluxo — uma possibilidade é plantio → avaliação de pegamento → indicador, sem compromisso de implementação agora.
