# DOCUMENTO TÉCNICO DE REQUISITOS

## Sistema de Gestão e Acompanhamento da Horta Comunitária FCT/UFG

**Versão do documento:** 0.0.1  
**Data:** 07/10/2026  
**Situação:** proposta inicial para apresentação e validação com a PO.

## 1. Sumário executivo

Este documento define o escopo e os requisitos iniciais do sistema de dados e dos dashboards da horta. A revisão com a PO deverá confirmar os registros indispensáveis, as permissões e os indicadores da primeira versão. A organização dos requisitos segue o exemplo SMR_Descritivo_Tecnico.pdf, utilizado apenas como referência de estrutura.

### 1.1 Objetivo e referências

Centralizar a coleta, o armazenamento e a gestão dos dados da Horta Comunitária FCT/UFG, em parceria com o Instituto APOEMA, e apresentar informações para acompanhar suas atividades e resultados.

A referência principal é [Dados_e_Indicadores_Preliminares_Horta_APOEMA.pdf](Dados_e_Indicadores_Preliminares_Horta_APOEMA.pdf), de 30/09/2026. O documento [HORTA COMUNITÁRIA_.pdf](HORTA%20COMUNIT%C3%81RIA_.pdf) fornece o contexto geral do projeto. Os detalhamentos abaixo são propostas para validação conjunta.

### 1.2 Escopo inicial

- Sistema integrado de coleta manual, armazenamento, consulta e gestão dos dados da horta.
- Dashboards de indicadores ambientais, produtivos, sociais, educacionais e econômicos, começando pelo conjunto prioritário da professora.

A primeira versão prioriza os cadastros, os registros de campo e nove indicadores: consumo de água, umidade do solo, irrigação por setor, pegamento das mudas, produtividade, perdas, rendimento após a secagem, horas de trabalho e ocorrências operacionais. Os registros sociais, educacionais e econômicos entram de forma básica, conforme os dados disponíveis e a prioridade definida pela PO.

Compostagem, integração com sensores e automação da irrigação ficam fora desta revisão. A modelagem preliminar está em documentação separada e não compõe este documento de requisitos.

### 1.3 Contexto de uso

A horta ainda não possui uma base histórica de produção. A configuração preliminar prevê 20 canteiros e cinco setores de irrigação, sujeita a confirmação; os cadastros devem permitir ajustes nessas quantidades e na organização física.

| Perfil proposto | Uso principal |
| --- | --- |
| Administração e coordenação | Manter cadastros, acessos e corrigir informações |
| Equipe de campo | Registrar atividades e dados coletados |
| Consulta | Acompanhar registros e dashboards autorizados |

O uso esperado é cadastrar a organização da horta, registrar o cultivo e sua rotina, consultar os indicadores e corrigir informações quando necessário. As permissões de cada perfil serão confirmadas com a PO.

### 1.4 Tecnologias previstas

Backend em .NET 10 ou superior, ASP.NET Core e Entity Framework Core; frontend em TypeScript, React e Vite. O sistema gerenciador de banco de dados permanece a definir. Esta entrega contempla somente documentação.

## 2. Requisitos funcionais

Cada requisito apresenta resumo, fluxo principal, fluxos alternativos e mensagens, regras de negócio e validações, campos envolvidos, critério de aceitação e prioridade sugerida. **Alta** indica prioridade do núcleo inicial; **Média** indica apoio ou dimensão complementar, cuja inclusão será acordada com a PO. Os quadros agrupam informações esperadas nos formulários; as obrigatoriedades finais serão validadas.

### RF01 - Acesso e permissões

**Resumo:** identificar os usuários e limitar as ações conforme seu perfil.

**Fluxo principal:**

1. A pessoa se identifica no sistema.
2. O sistema verifica o acesso e apresenta as funcionalidades autorizadas.

**Fluxos alternativos e mensagens:**

- acesso inválido ou sem permissão apresenta uma mensagem compreensível e impede a ação.

**Regras de negócio e validações:**

- pessoas com perfil de consulta não alteram registros.
- A forma de autenticação será definida com a equipe.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Identificação | identificação, nome |
| Acesso | perfil e situação da conta |

**Critério de aceitação:** uma pessoa com perfil de consulta consegue visualizar os dados permitidos e não consegue alterá-los.

**Prioridade sugerida:** Alta

### RF02 - Horta, setores e canteiros

**Resumo:** cadastrar a organização física da horta.

**Fluxo principal:**

1. A coordenação cadastra a horta, seus setores e canteiros.
2. Informa a associação entre os canteiros e os setores.

**Fluxos alternativos e mensagens:**

- código repetido ou área inválida impede a gravação e indica o campo a revisar.

**Regras de negócio e validações:**

- quantidades e dimensões são ajustáveis.
- Uma mudança de setor deve preservar a identificação dos registros anteriores.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Identificação | nome, código, localização |
| Organização física | dimensões, área cultivável, setor e situação |

**Critério de aceitação:** a equipe consegue alterar a organização dos canteiros sem perder o histórico das irrigações.

**Prioridade sugerida:** Alta

### RF03 - Espécies e origem do material

**Resumo:** identificar espécies, cultivares e a origem das sementes ou mudas.

**Fluxo principal:**

1. A equipe cadastra a espécie e informa a cultivar quando conhecida.
2. Registra a origem do material utilizado.

**Fluxos alternativos e mensagens:**

- cultivar ou origem desconhecida permanece identificada como não informada.

**Regras de negócio e validações:**

- uma cultivar deve corresponder à espécie selecionada.
- O cadastro não inclui gestão de estoque.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Cultivo | espécie, cultivar |
| Material | tipo de material, origem e data de recebimento, quando conhecida |

**Critério de aceitação:** um plantio pode ser consultado com sua espécie e procedência disponível, sem exigir informações desconhecidas.

**Prioridade sugerida:** Alta

### RF04 - Ciclos de cultivo

**Resumo:** organizar o acompanhamento do cultivo por canteiro, espécie e período.

**Fluxo principal:**

1. A equipe abre um ciclo e informa a área ocupada.
2. Relaciona os registros do cultivo e encerra o acompanhamento ao final.

**Fluxos alternativos e mensagens:**

- área incompatível com o canteiro ou datas inconsistentes exigem revisão.

**Regras de negócio e validações:**

- um ciclo pode ter várias colheitas.
- O acompanhamento de espécies em consórcio e a divisão de área precisam de validação com a PO.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Cultivo | canteiro, espécie, cultivar |
| Acompanhamento | início, encerramento, área ocupada, responsável e situação |

**Critério de aceitação:** duas colheitas do mesmo cultivo aparecem no mesmo ciclo, separado do cultivo seguinte.

**Prioridade sugerida:** Alta

### RF05 - Plantio, replantio e pegamento

**Resumo:** registrar mudas plantadas e avaliar sua sobrevivência.

**Fluxo principal:**

1. A equipe informa o plantio ou replantio.
2. Registra depois a quantidade de mudas sobreviventes em uma avaliação.

**Fluxos alternativos e mensagens:**

- quantidade de sobreviventes superior à plantada impede a confirmação.

**Regras de negócio e validações:**

- replantios ficam identificados separadamente.
- A janela e o método de avaliação serão validados; avaliações sucessivas não devem ser somadas.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Plantio | ciclo, origem do material, data, tipo de plantio, quantidade, espaçamento |
| Avaliação | data/contagem da avaliação |

**Critério de aceitação:** 36 sobreviventes de 40 mudas permitem calcular 90% de pegamento, sem misturar a reposição posterior.

**Prioridade sugerida:** Alta

### RF06 - Registros ambientais

**Resumo:** registrar manualmente as condições ambientais, priorizando a umidade do solo.

**Fluxo principal:**

1. A equipe seleciona a variável e o local de coleta.
2. Informa a data, o valor, a unidade e o método.

**Fluxos alternativos e mensagens:**

- informação incompatível ou duvidosa é sinalizada para revisão.

**Regras de negócio e validações:**

- medidas com unidades ou métodos diferentes não são agregadas como equivalentes.
- Temperatura e precipitação serão registradas conforme disponibilidade.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Coleta | variável, local, data/hora |
| Medição | valor, unidade, método, responsável e observação |

**Critério de aceitação:** o histórico de umidade mostra o local, a unidade e a data de cada medição utilizada.

**Prioridade sugerida:** Alta

### RF07 - Irrigações manuais

**Resumo:** registrar a irrigação realizada e o consumo de água informado.

**Fluxo principal:**

1. A equipe seleciona o setor e os canteiros atendidos.
2. Registra horário, duração, volume e eventuais interrupções.

**Fluxos alternativos e mensagens:**

- volume desconhecido permanece sem informação; uma irrigação interrompida recebe essa situação.

**Regras de negócio e validações:**

- distinguir volume medido de estimado e registrar o método.
- O volume de um evento não é multiplicado pela quantidade de canteiros atendidos.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Evento | setor, canteiros, início, fim, tempo ativo |
| Consumo e execução | volume, vazão quando utilizada, método, situação e responsável |

**Critério de aceitação:** uma irrigação de 120 L que atende quatro canteiros continua representando 120 L no total do setor.

**Prioridade sugerida:** Alta

### RF08 - Observações agronômicas

**Resumo:** registrar crescimento, pragas, doenças, deficiências e rebrote.

**Fluxo principal:**

1. A equipe seleciona o ciclo.
2. Registra a observação, com medida ou fotografia quando disponível.

**Fluxos alternativos e mensagens:**

- uma suspeita é registrada como observação, sem exigir diagnóstico confirmado.

**Regras de negócio e validações:**

- identificar a data e o responsável.
- Medidas de crescimento ou rebrote precisam de método e unidade definidos.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Contexto | ciclo, data, categoria |
| Observação | descrição, medida/unidade opcional e responsável |

**Critério de aceitação:** a equipe consegue consultar a evolução das observações de um cultivo ao longo do período.

**Prioridade sugerida:** Média

### RF09 - Colheita, perdas e secagem

**Resumo:** acompanhar produção, perdas e resultado da secagem.

**Fluxo principal:**

1. A equipe registra a colheita e as massas fresca e aproveitável.
2. Quando houver secagem, informa a massa destinada e a massa seca obtida.

**Fluxos alternativos e mensagens:**

- dados ainda não medidos permanecem pendentes, sem preencher zero automaticamente.

**Regras de negócio e validações:**

- massas não podem ser negativas; massa aproveitável não supera a fresca.
- A secagem deve manter sua origem e usar a massa efetivamente destinada ao processo.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Colheita | ciclo, data, massa fresca, massa aproveitável, motivo de perda |
| Secagem | lote/período de secagem, massa destinada e massa seca |

**Critério de aceitação:** a equipe distingue a perda na triagem do rendimento após a secagem e consulta ambos por cultivo.

**Prioridade sugerida:** Alta

### RF10 - Atividades e horas de trabalho

**Resumo:** registrar atividades realizadas, participantes, esforço e insumos utilizados.

**Fluxo principal:**

1. A equipe informa a atividade e seu contexto.
2. Registra as pessoas envolvidas e as horas de trabalho conhecidas.

**Fluxos alternativos e mensagens:**

- atividade geral não exige canteiro; horas desconhecidas permanecem sem informação.

**Regras de negócio e validações:**

- cada participante aparece uma vez por atividade.
- Horas individuais são somadas sem multiplicar novamente pelo número de pessoas.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Contexto | atividade, categoria, data, canteiro/ciclo quando aplicável |
| Execução | participantes, horas e insumo/quantidade/unidade quando utilizado |

**Critério de aceitação:** três pessoas com duas horas registradas cada representam seis horas de trabalho.

**Prioridade sugerida:** Alta

### RF11 - Ocorrências operacionais

**Resumo:** acompanhar falhas, interrupções e providências de campo.

**Fluxo principal:**

1. A equipe abre uma ocorrência e informa o contexto.
2. Atualiza a situação e a resolução.

**Fluxos alternativos e mensagens:**

- uma ocorrência pode ficar em acompanhamento enquanto não houver solução.

**Regras de negócio e validações:**

- várias intervenções na mesma falha não criam ocorrências duplicadas.
- Diferenciar abertura e resolução.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Ocorrência | data, categoria, descrição, contexto |
| Acompanhamento | responsável, situação e resolução |

**Critério de aceitação:** uma falha com duas intervenções aparece como uma ocorrência no dashboard.

**Prioridade sugerida:** Alta

### RF12 - Documentos e fotografias

**Resumo:** associar documentos existentes e fotografias aos registros.

**Fluxo principal:**

1. A pessoa anexa o arquivo.
2. Informa a descrição e o registro relacionado.

**Fluxos alternativos e mensagens:**

- envio incompleto ou arquivo não permitido apresenta uma mensagem de falha.

**Regras de negócio e validações:**

- limites de tamanho e formatos serão definidos.
- O acesso aos arquivos segue as permissões do sistema.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Arquivo | título, arquivo, descrição |
| Vínculo | data e registro relacionado |

**Critério de aceitação:** uma fotografia pode ser consultada a partir da ocorrência ou do cultivo que documenta.

**Prioridade sugerida:** Média

### RF13 - Consulta, correção e histórico

**Resumo:** consultar os registros e corrigir informações com rastreabilidade.

**Fluxo principal:**

1. A pessoa filtra os registros e abre o detalhe.
2. Com permissão, corrige ou invalida a informação com um motivo.

**Fluxos alternativos e mensagens:**

- consulta sem resultados informa a ausência de dados; alteração inconsistente indica os campos ou registros a revisar.

**Regras de negócio e validações:**

- preservar autor, data e informação anterior nas alterações.
- Registros invalidados não compõem os indicadores.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Consulta | período, tipo, setor, canteiro, espécie/ciclo |
| Correção | valores corrigidos e motivo |

**Critério de aceitação:** uma correção de pesagem atualiza o resultado do dashboard e mantém o valor anterior no histórico.

**Prioridade sugerida:** Alta

### RF14 - Dashboards e indicadores

**Resumo:** apresentar indicadores calculados a partir dos registros disponíveis.

**Fluxo principal:**

1. A pessoa acessa o dashboard e escolhe período e filtros aplicáveis.
2. Consulta o resultado e os registros de origem.

**Fluxos alternativos e mensagens:**

- ausência de dados ou informação insuficiente apresenta “Sem dados” ou “Indisponível”.

**Regras de negócio e validações:**

- começar pelos nove indicadores da seção 1.2.
- Exibir unidade, período e indicação de dados incompletos.
- Fórmulas, frequência e filtros serão validados com a PO antes da implementação.
- As demais dimensões são habilitadas conforme sua coleta.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Seleção | indicador, período, filtros |
| Resultado | resultado, unidade e registros de origem |

**Critério de aceitação:** o consumo por setor pode ser conferido nas irrigações registradas, e ausência de coleta não aparece como consumo zero.

**Prioridade sugerida:** Alta

### RF15 - Participação social e atividades educacionais

**Resumo:** registrar dados básicos de ações comunitárias, de ensino, pesquisa e extensão.

**Fluxo principal:**

1. A equipe classifica a atividade e informa objetivo, instituições e participantes ou público agregado.
2. Consulta os totais dessas dimensões.

**Fluxos alternativos e mensagens:**

- atividade sem lista nominal permite informar o público total conhecido.

**Regras de negócio e validações:**

- uma ação social e educacional mantém um único registro.
- Presenças por evento são diferentes de pessoas únicas; público agregado não permite identificar pessoas únicas entre eventos.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Atividade | atividade, dimensão, objetivo, instituição, modalidade |
| Participação | participantes/público |

**Critério de aceitação:** uma ação comunitária de educação ambiental aparece nos dois recortes, sem duplicar a atividade no total geral.

**Prioridade sugerida:** Média

### RF16 - Custos básicos e acompanhamento econômico

**Resumo:** registrar custos previstos e realizados para acompanhamento econômico.

**Fluxo principal:**

1. A equipe informa a despesa e sua natureza.
2. Consulta os totais por período e categoria.

**Fluxos alternativos e mensagens:**

- gasto sem previsão pode ser registrado; associação a canteiro ou ciclo é opcional.

**Regras de negócio e validações:**

- previsto e realizado aparecem separados.
- Valores não podem ser negativos.
- A atribuição de custos por espécie e os critérios de custo de produção dependem de validação; não há contabilidade completa.

**Campos envolvidos:**

| Grupo | Informações a registrar |
| --- | --- |
| Lançamento | data, descrição, categoria, valor, natureza |
| Contexto | canteiro/ciclo quando conhecido |

**Critério de aceitação:** R$ 90 previstos e R$ 100 realizados aparecem separadamente, com diferença de R$ 10.

**Prioridade sugerida:** Média

## 3. Requisitos não funcionais

| Código | Requisito inicial | Verificação esperada |
| --- | --- | --- |
| RNF01 | Integridade dos dados | Impedir referências inexistentes, valores incompatíveis e duplicação dos registros nos totais |
| RNF02 | Segurança e privacidade | Restringir consultas, alterações e arquivos conforme as permissões; coletar apenas os dados pessoais necessários |
| RNF03 | Rastreabilidade | Identificar autor e data do registro e preservar o histórico de correções |
| RNF04 | Usabilidade | Permitir registro e consulta em celular, com formulários em português, unidades visíveis e mensagens claras |
| RNF05 | Recuperação dos dados | Definir rotina de backup e verificar a restauração antes do uso operacional |
| RNF06 | Consistência dos indicadores | Conferir os cálculos com a PO; distinguir ausência de dados, zero observado e informação incompleta |

As metas de desempenho, a conectividade de campo e a rotina de backup serão definidas conforme as condições de uso. Esta versão não estabelece prazos de resposta ou operação offline.
