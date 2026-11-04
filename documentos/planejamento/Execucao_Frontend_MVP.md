# Execução do frontend da prévia do MVP

Entrega autorizada em 08/10/2026 para concluir os fluxos restantes da interface. Referências: [planejamento FE](frontend/README.md), [requisitos](../Documento_Tecnico_Requisitos_Horta.md), [contrato implementado da API](../arquitetura/Contrato_Backend_MVP.md) e [Get Started](../Get_Started.md). RF12 permanece adiado e RF01 permanece pós-MVP. Implementação técnica não constitui aceite da PO.

## Funcionalidades entregues

| Área | Cadastros e fatos | Ações específicas |
| --- | --- | --- |
| Organização | Horta, setor, canteiro | Primeira horta sem seed, situação cadastral, configuração de área, associação de setor e consulta de vigências |
| Irrigação | Irrigação manual existente, integrada à navegação | Situação/duração, litros conhecidos e cobertura por setor; regressão preservada |
| Ambiente | Variável, ponto de amostragem, medição | Unidade de referência, método, qualidade e consulta de umidade |
| Cultivo | Espécie, cultivar, lote de origem, ciclo, plantio/reposição, avaliação, observação | Referências compatíveis, área preservada do ciclo, registros relacionados, avaliações do plantio e encerramento do ciclo |
| Produção | Colheita, lote de secagem | Saldo/destinações de colheita, lista de origens de secagem, conclusão com pesagem final |
| Atividades | Participante, insumo, instituição, atividade | Participações/horas, insumos com quantidade/unidade, instituições, finalidade operacional/social/educacional, público nominal/informado |
| Ocorrências | Ocorrência | Situação Open/InProgress, providências como atividades vinculadas, resolução e histórico |
| Custos | Lançamento previsto/realizado | Competência, categoria, BRL, contexto geral/canteiro/ciclo e comparação |
| Visão geral | Dez consultas HTTP reais | Nove indicadores prioritários, participação social/educacional e custos; origem, grupos, cobertura e definição |

São 21 recursos novos com formulários concretos, além da irrigação existente. Cadastros oferecem criação, edição, inativação e histórico; fatos oferecem criação, correção completa com motivo/versão, invalidação e histórico. Não há exclusão física, editor JSON de entidades ou geração dinâmica de formulários. Dados de contexto e comandos de cada domínio permanecem em tipos, formulários e serviços próprios.

## Navegação e estado

Rotas usam `#/hortas/{id|_}/{área}/{recurso}/{id|novo}?filtros`. `_` representa ausência explícita de horta; a primeira horta pode ser criada sem seleção preexistente. As áreas são `horta`, `irrigacao`, `ambiente`, `cultivo`, `producao`, `atividades`, `ocorrencias`, `custos` e `visao-geral`. Listas e indicadores preservam filtros/páginas na URL. Rascunhos, observações e motivos permanecem no estado do formulário.

Links de indicadores conservam o recorte para **Voltar ao indicador com os mesmos filtros**. Voltar/avançar do navegador também respeitam os filtros. Cabeçalho mantém a horta selecionada. Horta inativa conserva consultas e histórico; formulários locais ficam indisponíveis para novas escritas, enquanto catálogos globais continuam administráveis.

Consultas têm AbortSignal e descartam respostas antigas. Seletores carregam todas as páginas; referências inativas ficam identificadas e indisponíveis para novos fatos. Mudanças de espécie/ciclo/contexto removem escolhas incompatíveis. Datas civis permanecem `yyyy-MM-dd`; instantes são convertidos de/para `America/Sao_Paulo`, sem depender do fuso do navegador. Medidas usam vírgula/ponto e validação de precisão, sem arredondamento silencioso. Vazio é desconhecido, enquanto zero informado permanece zero.

Criação de fatos conserva operationId e payload diante de resposta incerta. Cadastros, cujo contrato não é idempotente, bloqueiam nova criação até revisar a lista. Falhas conhecidas permitem corrigir o formulário. Conflito de versão conserva o rascunho, apresenta o estado atual e exige reconhecimento explícito; não reenvia automaticamente. Confirmação de gravação continua visível quando a leitura seguinte falha. Navegação solicita descarte de rascunho e bloqueia abandono de envio pendente.

Componentes comuns compartilham controles acessíveis, paginação, estados de consulta e histórico de leitura. Hooks comuns tratam comportamento técnico. Serviços HTTP usam o único cliente Axios; diagnósticos contêm status/code/traceId, sem conteúdo de formulários. Tailwind permanece a base visual; páginas de recursos e indicadores carregam por demanda.

## Complemento necessário do backend

Foram adicionadas somente três consultas administrativas: `GET /gardens/catalog`, `GET /gardens/{gardenId}/sectors/catalog` e `GET /gardens/{gardenId}/participants/catalog`. Retornam os DTOs completos, situação/versão e filtro opcional `isActive`, com paginação e ordenação estável. Os seletores anteriores continuam com seus contratos ativos; `/beds/catalog` já existia. Sem essas consultas, a interface não poderia administrar cadastros inativos corretamente.

As novas slices seguem o padrão arquitetural, Result, ProblemDetails, projeções de leitura e ProcessLog/Serilog. Não adicionam tabelas, migrations, appsettings ou seed. `AdministrativeCatalogTests` verificou filtros, situação/versão, páginas sem duplicação, contexto de horta inativa, horta inexistente, lista vazia e erro de paginação em PostgreSQL real: **1 teste aprovado, zero falhas/ignorados**, build Release sem avisos.

## Verificações do frontend

Em 08/10/2026: **20 testes E2E aprovados, zero falhas**, além de build de produção com TypeScript estrito e lint aprovados. O conjunto reúne 12 cenários da irrigação e oito percursos restantes. Após ajustar tokens de cor e a apresentação dos detalhes, o cenário de celular foi repetido e **aprovado**, incluindo participações, insumos e instituições no formulário de atividade. Screenshots `mvp-mobile-detail.png`, `mvp-mobile-indicator.png`, `mvp-mobile-activity.png` e `mvp-desktop-indicator.png` foram inspecionados: detalhes legíveis, grupos de formulário contidos e ausência de rolagem horizontal em 390 px. Ajustes finais de rótulos e alinhamento de checkbox preservam os mesmos controles e contratos.

A suíte usa Chromium em `Asia/Tokyo`, API .NET real, PostgreSQL 17 e um banco exclusivo `horta_e2e_<uuid>`. O setup confirma ausência de cadastros automáticos e insere fixtures somente nesse banco. Os processos e o banco descartável são encerrados/removidos ao terminar. O banco da aplicação não é alterado pelos testes.

Os cenários exercitam criação em todos os domínios, vigências, reload e históricos, medida zero/qualidade/fuso, avaliações com recálculo do indicador, produção/perdas parciais, saldo/destinação/secagem, atividades com filhos e público, resolução de ocorrência, custos, precisão, resposta perdida com idempotência, versão concorrente com rascunho preservado, resposta perdida de cadastro, leitura de horta inativa e primeira organização sem hortas. A regressão de irrigação inclui 101 canteiros, contextos entre hortas, descartes de respostas antigas e microssegundos.

Evidências de navegador ficam em `hortacomunitaria-webapp/test-results/` e relatório em `playwright-report/`, ignorados pelo Git. Não são cópias de entrega. A demonstração manual e os valores esperados estão no [Get Started](../Get_Started.md).

## Pendências mantidas

- RF12: upload/documentos/fotografias adiado explicitamente; sem tela ou armazenamento simulado.
- RF01: identidade e permissões permanecem pós-MVP; autoria demonstrativa explícita em Development.
- Aprovação funcional da PO: nomes/agrupamentos, critérios de indicadores e hipóteses agrícolas/social/educacionais permanecem sujeitos à validação.
- Coleta automática, sensores, Grafana e implantação de produção não integram esta prévia.


## Reorganização visual da prévia — 08/10/2026

Marca e paleta preservadas conforme a referência aprovada. AppShell separa cabeçalho institucional, navegação lateral agrupada, seleção de horta, abas de cada área e conteúdo. No celular, o menu usa dialog nativo com Escape/restauração de foco; o conteúdo ocupa a largura disponível. O atalho para o conteúdo não altera a rota.

Listas recebem hierarquia de título/ação, filtros e contagem de resultados; os principais fatos exibem informações de contexto sem abrir o detalhe (valores, quantidades, áreas, massas, categorias e datas). Formulários compartilham um painel de largura limitada e ações acessíveis durante a rolagem. Atividades agrupam identificação, local/vínculos, finalidade/público e coleções relacionadas. Indicadores apresentam resumo em cartões e explicação expansível, mantendo disponibilidade e cobertura visíveis. Nenhum cálculo de indicador foi transferido para o frontend.

Implementação com Tailwind e SVGs locais, sem novo design system ou dependência. Rotas, contratos HTTP, histórico, proteção de rascunho e confirmações de concorrência permanecem nos fluxos existentes.

Validação final da reorganização: build de produção e lint aprovados; **20 testes E2E aprovados (1,8 min)** em PostgreSQL isolado. Capturas de desktop (irrigação, indicador, lista e atividade) e celular (detalhe, indicador, atividade e menu) revisadas. Os testes verificam abertura do menu, descarte recusado, Escape e restauração do foco, além dos fluxos anteriores. A regressão revelou envio antes do carregamento dos seletores; formulários agora desabilitam Salvar até os cadastros de apoio estarem disponíveis e sem erro. A explicação expansível do indicador também foi verificada.


## Correção da cronologia e dashboard integrado — 08/10/2026

A [revisão dos fluxos](Revisao_Fluxos_Frontend_MVP.md) documenta os bloqueios encontrados e a sequência de cada requisito. A horta criada passa a ser o contexto selecionado; os detalhes ligam horta → setor → canteiro → área/associação, e a preparação confere vigências por data com canteiros paginados. Formulários dependentes orientam cadastros ausentes, e ciclo confere área na data escolhida. As áreas mostram a sequência de uso sem transformar campos opcionais em obrigatórios.

A entrada agora é o dashboard com dez consultas independentes, agrupadas por dimensão, período, disponibilidade/cobertura, barras com valores e detalhamento/origens. Água permanece um indicador e uma tela operacional própria; não é mais a visão geral inteira. Período acompanha a navegação entre indicadores. Não foram adicionados endpoints, migrations, seeds ou cálculos de negócio no cliente.

Verificação final: build de produção aprovado e **22 testes E2E aprovados (1,7 min)**, zero falhas, com navegador/API/PostgreSQL real e banco exclusivo descartável. O percurso inicial agora usa cliques reais desde banco sem hortas até irrigação e dashboard; outro cenário cria segunda horta e confirma troca automática de contexto. Novos cenários cobrem orientação de dependências, dez cartões, zero explícito, volume, período, erro isolado e recuperação. Capturas `flows-dashboard-desktop.png`, `flows-dashboard-mobile.png` e `flows-prerequisites.png` ficam em test-results, ignorado pelo Git. A revisão visual conferiu o painel consolidado e a orientação de pré-requisitos; o teste de celular verifica ausência de rolagem horizontal.

Uma rodada intermediária perdeu o servidor de preview; a validação foi repetida por completo. O setup agora exige a porta de preview e mantém o arquivo de diagnóstico aberto para ambos os processos. A execução final acima terminou normalmente e limpou seu banco/processos.

## Entrada por horta e retirada de códigos manuais — 09/10/2026

Esta revisão substitui a seleção automática descrita na etapa anterior. A entrada lista hortas sem menus internos; criar leva à ação Entrar na horta. Depois da entrada, a preparação conduz a setores, canteiros, área e associação por vigência. Todas as hortas retorna à lista. Dados da horta abre o cadastro do contexto atual. A navegação preserva proteção de rascunhos e consulta de inativos.

Foram retirados os códigos dos sete formulários de cadastro que os exigiam. Criações omitem code e correções preservam o legado no servidor; o UUID continua identificando os cadastros. Listas e seletores mostram nomes ou dados descritivos. IsSoilMoisture substitui a dependência de código digitado no indicador de umidade. A migration gerada pelo EF preserva a classificação dos registros antigos.

Verificação: build Release sem warnings de compilação, build frontend e lint aprovados; **12 testes de integração** e **22 testes E2E** aprovados (última execução E2E: 1,5 min), com bancos PostgreSQL descartáveis. A suíte cobre criação sem code no payload, correção sem alterar a identidade, histórico, migração de variável legada, classificação explícita no indicador, banco sem hortas, entrada obrigatória, troca entre hortas, dependências e os fluxos anteriores. Capturas garden-lobby-desktop.png e garden-lobby-mobile.png foram inspecionadas; navegação sem menus antes da entrada e sem overflow em 390 px. O build Vite mantém o aviso existente de chunk acima de 500 kB.

Nenhum banco operacional foi alterado durante os testes. A configuração local Development aplica migrations ao iniciar: reiniciar a API carrega o novo contrato e aplica a coluna antes de servir a interface.
