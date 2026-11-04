# Comportamentos e aceite da interface

**Aplicação:** todos os [planos FE](README.md). **Estado:** referência de validação; comportamentos implementados e verificações descritos na [execução do frontend](../Execucao_Frontend_MVP.md). Aceite da PO pendente. A [arquitetura do frontend](../../../.codex/arquitetura-frontend.mdc) concentra as regras técnicas. Este documento descreve experiência, condições de integração e verificação funcional.

## Condição de integração

Para cada etapa, registrar a operação da API, campos editáveis/nullable, seletores e páginas, tipos/valores permitidos, comandos de ciclo de vida, filtros/data de competência, erro/conflito e indicador afetado. Confirmar no OpenAPI e por chamada real. Os recursos dos planos funcionais são propostas até sua implementação; não declarar um endpoint disponível só porque aparece no Markdown.

Se faltar contrato, manter a etapa **aguardando API/definição**, sem preencher telas com totais fictícios, tratar 404 como NoData ou guardar o fato em localStorage. O trabalho independente de navegação/acessibilidade pode avançar na etapa autorizada, mas não é integração concluída. A nomenclatura da modelagem não determina nomes TypeScript ou payloads.

## Percurso comum da pessoa

1. Selecionar horta e contexto do fluxo. Na troca de contexto, revisar/limpar seleções incompatíveis; respostas atrasadas não restauram o contexto anterior.
2. Abrir formulário no contexto ou pela ação da lista. Labels e unidades acompanham os controles; informações opcionais são identificadas como não informadas. O domínio define quais ausências permitem salvar: medição ambiental sem valor e custo sem valor, por exemplo, ficam como rascunho, não como fato desconhecido confirmado.
3. Salvar por ação explícita, bloquear duplo envio e mostrar confirmação. Criação de fato com resposta incerta oferece confirmar a tentativa original preservando chave/conteúdo; não oferecer um novo registro como recuperação automática.
4. Consultar lista com filtros/paginação; abrir detalhe; conferir origem, contexto histórico, autoria e evidências disponíveis. Voltar mantendo o recorte de consulta quando compatível.
5. Corrigir com motivo/versão ou invalidar/inativar segundo o domínio; revisar dependentes quando houver conflito. Depois da confirmação, recarregar os dados afetados e conferir o histórico.

Escolha de responsável pelo trabalho não é autenticação nem autoria. Durante o MVP, a interface indica dados/autoria demonstrativos. Catálogo inativo não desaparece do detalhe histórico; o formulário identifica referências antigas indisponíveis em vez de trocá-las silenciosamente.

## Estados e respostas

| Situação observada | Resposta da interface | Próxima ação |
| --- | --- | --- |
| Consulta em andamento | Carregamento identificado; contexto anterior não apresentado como novo | Aguardar; cancelar ao mudar contexto |
| Consulta sem resultados confirmada | Ausência no recorte, mantendo filtros visíveis | Ajustar filtros ou criar fato quando possível |
| Falha de rede/servidor | Mensagem e recuperação da consulta, sem resultados fabricados | Tentar novamente; diagnóstico por traceId quando disponível |
| Referência removida/inativada | Preservar rascunho e mostrar o campo/contexto afetado | Selecionar referência válida ou consultar a histórica |
| Erro de campo | Mensagem associada ao controle e dados preservados | Corrigir o campo |
| Resultado de criação incerto | Avisar que pode ter sido salvo; manter tentativa e dados | Confirmar tentativa; não duplicar |
| Gravação confirmada, leitura seguinte falha | Confirmação de gravação continua visível junto da falha de leitura | Recarregar; não criar novamente |
| Versão desatualizada | Rascunho preservado, envio suspenso e revisão do estado atual | Recarregar, comparar valores/vínculos e reconhecer versão explicitamente |
| Dependentes impedem alteração | Motivo e vínculos retornados pela API, com links disponíveis | Revisar dependentes sem cascata automática |
| Fato invalidado/cadastro inativo | Estado textual, detalhe e histórico preservados | Consultar; novas ações somente se previstas no contrato |

Cada motivo de 409 é apresentado conforme o code real. Não resolver conflito trocando a versão/operação e reenviando sem revisão. Antes de descartar um rascunho alterado por cancelamento/navegação, permitir decidir entre continuar editando e descartá-lo; não guardar dados pessoais automaticamente como solução offline.

## Indicador e consulta de origem

Exibir título, unidade, período, filtros efetivos, data de competência/fuso aplicável, definição e contagens/cobertura do resultado retornado. Distinguir Sem dados, Indisponível, Parcial e Disponível conforme o contrato; ausência de API é falha de integração, não ausência de coleta. Zero conhecido permanece zero; percentual sem denominador elegível permanece indisponível.

A ação de conferir origem abre a lista/detalhe do mesmo recorte. Filtros não suportados por um indicador não são enviados ou aplicados apenas na aparência. Paginação de origens mantém o resumo do recorte inteiro da API. Gráficos, quando necessários, têm tabela/descrição acessível equivalente e respeitam grupos compatíveis. Frequência esperada e metas agronômicas não são presumidas.

## Critérios transversais e RNFs

| Requisito | Verificação de frontend | Dependência/limite |
| --- | --- | --- |
| RNF01 — Integridade | Contexto consistente, erro de referência/precisão/dependência visível; retorno confirmado sem duplicar origem | Servidor confirma invariantes, concorrência e persistência |
| RNF02 — Segurança/privacidade | Sem segredos no bundle/logs, fixtures fictícios, coleta mínima, sem autoria enviada pelo cliente | Controle de acesso efetivo depende de RF01 pós-MVP; demonstração não equivale a segurança operacional |
| RNF03 — Rastreabilidade | Autor, inclusão, data do fato, motivo e antes/depois consultáveis após alteração | Histórico e autoria vêm da API |
| RNF04 — Usabilidade | Formulário pt-BR em celular/desktop, unidades, foco, teclado, erros associados e anúncio de estados | Verificar tamanhos de tela e dados/textos longos; sem rolagem horizontal para registrar |
| RNF05 — Recuperação | Após restauração coordenada pela operação, listas, detalhes, histórico e evidências continuam consultáveis | Backup/restore não é feito pelo browser nem entregue como tela nova; teste operacional em FE12 |
| RNF06 — Indicadores | Confrontar resultado/origem, desconhecido/zero/parcial, filtros, grupos e efeito de correção | Fórmulas e recortes exigem validação da PO e implementação no servidor |

## Evidência de conclusão de uma etapa

- Contratos/rotas efetivos registrados e sem mock como prova final de integração.
- Build/lint do frontend aprovados; regressão da irrigação e do domínio afetado quando houver alteração compartilhada.
- E2E significativo com API e PostgreSQL reais, banco exclusivo/fixtures fictícios: registro, reload persistido, filtro/origem, alteração com motivo, histórico e indicador, conforme os recursos entregues.
- Exercitar erros de campo/rede, retry sem duplicação, versão concorrente com rascunho, referência incompatível, catálogos/coleções além da primeira página e respostas antigas quando aplicáveis.
- Conferir unidade/precisão, desconhecido/zero e período/data de competência; instantes em navegador com outro fuso, datas agrícolas sem deslocamento UTC.
- Conferir formulário e consultas em celular, teclado/foco, mensagens e textos longos; estados indicados por texto, além de cor.
- Atualizar status do FE e a [execução local](../../arquitetura/Execucao_Local.md), vinculando comando, resultado e limitações. Evidências temporárias permanecem ignoradas; respostas/aceites da PO são registros reais, não consequência do build.

Para registrar entrega, usar: etapa/data; contrato verificado; cenários/comandos e resultado; telas/estados conferidos; limitações; decisões da PO; próximo item dependente. Não criar uma suíte que só confira existência de pastas ou repita componentes.
