# FE00 — Navegação e contexto da aplicação

**Estado:** implementado tecnicamente; aceite da PO pendente. **Referências:** RNF04 e contexto de RF02–RF16. **Dependência:** [irrigação existente](01_Irrigacao.md). Ler [comportamentos/aceite](Comportamentos_Aceite.md) e regras do frontend integrais.

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Resultado e percurso

Abrir aplicação → selecionar horta → acessar módulo entregue → registrar/consultar → abrir origem/detalhe → voltar ao recorte. A interface cresce a partir da irrigação existente e não vira uma coleção de menus sem função.

## Organização da experiência

| Área proposta | Entrada e telas de destino | Condição de habilitação |
| --- | --- | --- |
| Visão geral | Indicadores disponíveis e consulta da origem | FE11.1, inicialmente consumo |
| Horta | Hortas, setores, canteiros e vigências | Etapas FE02 correspondentes |
| Irrigação | Lista/consumo, formulário, detalhe/histórico | Já entregue; adaptar à navegação |
| Ambiente | Pontos/variáveis, medições e umidade | FE03 por etapa |
| Cultivo | Espécies/origem, ciclos, plantios, avaliações e observações | FE04 por etapa |
| Produção | Colheitas, perdas e lotes de secagem | FE05 por etapa |
| Atividades | Trabalho/insumos e dimensões social/educacional | FE06; estender com FE08 |
| Ocorrências | Abertura, acompanhamento e providências | FE07 |
| Custos | Lançamentos previstos/realizados e comparação | FE09 |

Participantes, instituições e insumos são cadastros de apoio acessíveis pelo fluxo consumidor; não são módulos de contas/estoque. Evidências ficam nos detalhes dos registros; histórico acompanha o domínio. A ação social/educacional usa a mesma atividade, não uma segunda tela de criação duplicada.

## Rotas de tela propostas

São destinos de navegação futuros, distintos das rotas `/api/v1`; atualmente o fluxo está em `/` com estado local. Validar desenho e solução de roteamento na FE00.1 antes de alterar a aplicação; esta tarefa não instala biblioteca.

| Destino | Caminho alvo ilustrativo |
| --- | --- |
| Visão geral da horta | `/hortas/:gardenId/visao-geral` |
| Lista/formulário/detalhe de domínio | `/hortas/:gardenId/<modulo>`, `/novo`, `/:id` |
| Correção de registro | `/<modulo>/:id/correcao` dentro da mesma horta |
| Configurações de canteiro | `/hortas/:gardenId/canteiros/:id` com seções de vigência |
| Catálogo compartilhado | `/catalogos/especies`, `/catalogos/insumos`, `/catalogos/instituicoes` quando entregue |

A horta da URL e a seleção visível precisam coincidir. Recurso inexistente/fora do contexto tem mensagem e saída válida, sem mostrar detalhe antigo. Ao atualizar uma rota, carregar o recurso pela API; browser voltar/avançar deve funcionar. Filtros e página podem estar na URL quando isso permitir retorno/compartilhamento, sem observações, motivos, nomes pessoais ou rascunhos nela.

## Estados de contexto e navegação

- Carregamento/erro/nenhuma horta mantêm explicação e recuperação. Não fixar a horta pelos UUIDs do contexto de teste. Quando FE02.1 estiver entregue, a lista/criação de hortas deve ser acessível mesmo sem horta selecionada: é a entrada para preparar uma aplicação vazia pela própria interface.
- Troca de horta limpa seleções locais incompatíveis, filtros específicos e rascunhos descartados explicitamente. Uma escrita em andamento/tentativa incerta exige resolver o envio antes de sair do contexto.
- Navegação por celular permite acessar ações principais e retornar; foco vai ao título da tela e volta à ação relevante após fechar um diálogo. Identificar item atual e manter nomes claros.
- Exibir ambiente/autoria demonstrativos, sem login ou perfil autenticado fictício. Ainda não publicar a demonstração para acesso externo.

## Etapas

| Etapa | Entrega | Dependência e aceite | Estado |
| --- | --- | --- | --- |
| FE00.1 | Definir rotas reais e navegação responsiva; extrair composição compartilhada somente onde houver uso | Reutilizar fluxo atual; justificar solução de roteamento, confirmar acesso direto/reload/voltar | Não iniciada |
| FE00.2 | Contexto de horta coerente entre URL, seletores e consultas | Troca de horta não mistura dados; referência inválida e falha têm saída recuperável | Não iniciada |
| FE00.3 | Integrar módulos progressivamente e retorno aos recortes | Habilitar só etapas realmente integradas; regressão completa de irrigação | Não iniciada |

## Cenários de aceite

1. Abrir detalhe de irrigação pela navegação, atualizar página e voltar mantém contexto e dados confirmados.
2. Trocar horta não restaura setor/canteiros/responsável da anterior, mesmo com resposta atrasada.
3. Rota inexistente ou ID de outra horta não exibe recurso de uma consulta anterior.
4. Celular/teclado permitem abrir formulário, cancelar com decisão sobre rascunho e retornar à lista.
5. Navegação não oferece funcionalidades planejadas como se estivessem implementadas; irrigação continua passando seus cenários reais.

**Pendências:** validar nomes/agrupamentos com a PO; roteamento hash implementado conforme a [execução](../Execucao_Frontend_MVP.md). **Evidências:** registradas tecnicamente; aceite funcional pendente.
