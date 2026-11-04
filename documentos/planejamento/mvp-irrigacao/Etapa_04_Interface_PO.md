# Etapa 4 — Interface mínima e demonstração à PO

> Atualização de 08/10/2026: seed removido por decisão do responsável. Os passos/evidências de execução original abaixo são históricos; o código atual inicia sem inserir dados, e os testes preparam fixtures explícitas em bancos isolados. Consulte o [Get Started](../../Get_Started.md) para executar a versão atual.

**Estado:** concluída tecnicamente em 08/10/2026; reunião e aceite funcional da PO pendentes. **Dependência:** etapa 3 implementada e verificada.

**Leitura obrigatória:** AGENTS.md, [arquitetura do frontend](../../../.codex/arquitetura-frontend.mdc) integral, [contrato do fluxo](Contrato_Fluxo.md), OpenAPI real da API e documentação de execução. Ler a [arquitetura do backend](../../../.codex/arquitetura-backend.md) integralmente antes de qualquer ajuste nele.

## Resultado e limite

Uma interface React/TypeScript/Vite com Tailwind CSS permite percorrer o fluxo inteiro usando a API e PostgreSQL. Uma página principal e um detalhe/histórico bastam. Não criar login, menu de módulos futuros, CRUD de cadastros ou dashboard com cartões de indicadores ainda inexistentes.

## Sequência de trabalho

1. Usar o projeto React/TypeScript/Vite existente em `hortacomunitaria-webapp/`, fixando versões e preservando o lockfile. Tailwind CSS é a base de desenvolvimento visual por decisão do responsável em 08/10/2026. Seguir a arquitetura do frontend: `src/features/irrigation`, componentes usados por esse fluxo e cliente HTTP. Não adicionar framework de estado, biblioteca visual ou gerador de clientes sem necessidade concreta. URL da API em configuração local; nenhuma regra de negócio depende do frontend.
2. Buscar hortas, setores, canteiros elegíveis na data do início e participantes. Consumir todas as páginas necessárias às seleções, sem assumir quatro canteiros. Ao mudar horta, setor ou data, limpar seleções incompatíveis; descartar respostas antigas para não misturar contextos. Indicar dados e autoria demonstrativos.
3. Criar formulário pt-BR com unidades, indicação de desconhecido e status Completed/Interrupted. Entrada vazia é null; zero informado é 0. Converter instantes com offset de forma explícita para o fuso do fluxo, sem depender silenciosamente do fuso do navegador. Não calcular volume por vazão. Gerar operationId uma vez por tentativa lógica de criação e preservá-la em retry; só gerar nova após sucesso ou mudança deliberada do payload.
4. Mostrar lista paginada, filtros de período/setor e consumo conhecido com unidade, cobertura, estimativas e estados NoData/Unavailable/Partial/Available. Permitir conferir os registros do mesmo recorte. Paginar setores sem mudar o resumo. Listagem e consumo usam os mesmos filtros do usuário, sem fabricar totais locais.
5. Abrir detalhe e histórico; permitir correção completa e invalidação com motivo. Enviar expectedVersion obtida do detalhe. Depois de sucesso, recarregar detalhe, histórico, lista e consumo. Em 409, explicar conflito e oferecer recarregar, preservando o rascunho; nunca sobrescrever ou reenviar automaticamente com versão nova.
6. Exibir carregamento, falha com opção de tentar novamente, ausência de resultados e erros por campo quando fornecidos. Bloquear envio duplo no formulário sem depender disso para a idempotência no servidor. Configurar CORS apenas para a origem local efetivamente usada. Atualizar Execucao_Local.md para subir banco, API e frontend.

## Verificações de aceite

- Build de produção e verificação TypeScript sem erros. Teste significativo do fluxo da interface, preferencialmente E2E com API/PostgreSQL reais e dados isolados: registrar 120 L para quatro canteiros, conferir 120, corrigir para 100, conferir histórico, invalidar e conferir NoData. Não substituir essa prova por mocks do próprio contrato.
- Conferir manualmente em largura de celular: campos/unidades, navegação, botões, tabela/lista e mensagens legíveis, sem exigir rolagem horizontal para registrar o fato.
- Exercitar volume desconhecido, zero observado, erro da API, retry da criação e conflito de edição. Validar entrada de instante em navegador com fuso diferente do configurado.
- Atualização da página preserva dados salvos via banco. Seleção de outra horta não mantém setor/canteiros/responsável da anterior.
- Nenhum pedido carrega autor informado pelo usuário; UI e histórico mostram `Operador de demonstração`. Não há dados pessoais reais nos fixtures.

## Roteiro para apresentar à PO

1. Explicar que os cadastros e a autoria são demonstrativos; formular os dados conforme o que a equipe sabe coletar hoje.
2. Selecionar S1 e B1–B4, registrar 120 L medidos e mostrar que os quatro canteiros não multiplicam o volume.
3. Registrar outro evento com volume desconhecido e mostrar o aviso de informação parcial; em um recorte vazio, mostrar Sem dados.
4. Corrigir 120 para 100 L com motivo, conferir o valor anterior no histórico e o novo resultado. Invalidar e conferir que o fato continua consultável, mas sai do indicador.
5. Perguntar quais campos atrapalham ou faltam, como se obtém o volume, se os filtros são úteis, como tratar cruzamento de dias e qual seria o próximo fluxo prioritário.

## Registro da validação e encerramento

Preparar `documentos/planejamento/Validacao_PO_MVP_Irrigacao.md` com roteiro, exemplos e tabela: decisão/hipótese, resposta da PO, ajuste necessário, responsável e estado. Antes da reunião, respostas e aprovação ficam **pendentes**. Não preencher aceites fictícios nem considerar uma demonstração técnica como reunião realizada.

Concluir esta etapa significa ter interface testada e roteiro pronto para apresentar. A validação real da PO é um marco posterior, dependente da reunião. Registrar [evidências](README.md#acompanhamento) e pendências; o próximo fluxo, autenticação e ampliação de cadastros exigem planejamento próprio.

## Evidências da execução

Execução em 08/10/2026 no projeto existente `hortacomunitaria-webapp`, organizada por feature conforme as regras. Node 24.15.0/npm 11.12.1 e dependências diretas fixadas, lockfile preservado; Tailwind CSS/plugin Vite 4.3.3 instalado e aplicado como base de estilos, com tokens, padrões reutilizados e utilitários responsivos. Folhas CSS do template removidas. Cliente Axios comum, conversão explícita de fuso, TypeScript estrito e nenhum ajuste de contrato do backend.

- `npm ci`: instalação reproduzível e auditoria com zero vulnerabilidades.
- `npm run lint`: aprovado, incluindo hooks e testes.
- `npm run test:e2e`: executou `npm run build` (TypeScript da aplicação/configuração/testes e assets de produção) e **12 testes aprovados em 36,6 s**, sem falhas/retries, após a refatoração Tailwind.
- API Release real e PostgreSQL 17 nativo em loopback, cluster isolado. Setup cria banco exclusivo, aplica migrations/seed, inicia API 5279 e preview 5183; teardown encerra hosts/remove banco. Não reutiliza banco operacional ou host já iniciado.
- Percurso 120 L → 100 L → Invalid/NoData, quatro canteiros, histórico tipado antes/depois, autoria e persistência após reload; null/zero/estimativa e erro de precisão; resposta perdida após gravação e retry idempotente; conflito real por invalidação e por correção com rascunho preservado e revisão explícita; correção idêntica preservando microssegundos; 101 canteiros em duas páginas; troca de setor/horta, resposta antiga descartada e recuperação de consulta e confirmação de gravação preservada quando a leitura seguinte falha.
- Chromium em Asia/Tokyo confirmou transporte UTC e fuso America/Sao_Paulo; cenários históricos verificaram mudança de offset e rejeição de horário inexistente/ambíguo.
- Layout de 390 px sem rolagem horizontal, foco inicial/Tab e screenshots de celular/desktop inspecionados visualmente. Evidências locais em `hortacomunitaria-webapp/test-results/` e relatório em `playwright-report/`, ignorados pelo Git.

O [README do frontend](../../../hortacomunitaria-webapp/README.md) e a [execução local](../../arquitetura/Execucao_Local.md) documentam instalação/proxy/testes. O [roteiro da PO](../Validacao_PO_MVP_Irrigacao.md) foi preparado com exemplos e tabela de decisões; respostas, reunião e aceite continuam **pendentes**. Docker/compose não exercitados nesta máquina; o PostgreSQL nativo foi efetivamente utilizado nos testes. Autenticação, publicação e demais fluxos continuam fora desta entrega.
