# FE07 — Ocorrências e providências

**Requisitos:** RF11/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 07](../mvp/07_Ocorrencias.md) e [aceite comum](Comportamentos_Aceite.md). **Dependência:** FE06 para registrar atividades vinculadas.

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso e telas

Abrir ocorrência → acompanhar situação → registrar atividade/providência vinculada → resolver com justificativa → consultar histórico e contagens → retornar à origem.

| Tela | Conteúdo e ações |
| --- | --- |
| Lista | Período de abertura, categoria, local/contexto, situação e validade; criar e detalhar, com paginação |
| Abertura | Data/hora, categoria, descrição, contexto e responsável quando conhecido; campos obrigatórios conforme contrato |
| Detalhe | Identidade da ocorrência, abertura, situação atual, resolução, providências, autoria e versão |
| Providência | Abrir formulário de atividade FE06 com vínculo à ocorrência; consultar a atividade original |
| Gestão | Comandos próprios para situação/resolução; correção/invalidação com motivo e histórico |
| Indicador | Ocorrências distintas abertas no período e distribuição conforme API; situação atual explicitamente identificada |

Aberta, em andamento e resolvida são situações de uma mesma ocorrência, sem criar outra identidade a cada transição. Corrigir um texto não resolve uma ocorrência. Resolução exige motivo e instante não anterior à abertura. Não oferecer reabertura enquanto não houver regra/contrato aprovado.

Uma intervenção é a atividade vinculada, acessível tanto por FE06 quanto por este detalhe. Registrar três atividades não cria três ocorrências nem altera a contagem da origem. Ocorrências legítimas com mesmo dia/categoria/local não devem ser deduplicadas pela interface.

## Etapas

| Etapa | Entrega | Dependência |
| --- | --- | --- |
| FE07.1 | Abertura, lista/detalhe, correção/invalidação e histórico | API 07.1 |
| FE07.2 | Registrar/consultar providências e resolução da ocorrência | API 07.2; FE06.2 |
| FE07.3 | Contagens e navegação às ocorrências | API 07.3; FE11 |

## Cenários de aceite

- Abrir uma ocorrência e duas providências; consultar uma identidade de ocorrência e duas atividades.
- Resolver depois da abertura com justificativa; detalhe/histórico exibem a transição. Data anterior é rejeitada junto ao campo.
- Distinguir ocorrências abertas no recorte de estoque de pendências atual; não apresentar ambos como a mesma medida.
- Resolver ocorrência antiga não a transforma em abertura do período atual. Filtros de situação referem-se à situação indicada pela API.
- Corrigir/inutilizar com concorrência preserva o rascunho; atualização bem-sucedida recompõe lista, detalhe e contagens.

Categorias, recortes e regras de resolução seguem as hipóteses do plano funcional, sujeitas à PO. Não acrescentar tarefas recorrentes, SLA ou notificações automáticas.
