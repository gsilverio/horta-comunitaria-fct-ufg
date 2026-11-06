# Reestruturação visual — entrada e gestão da horta

Data: 04/11/2026. Estado: implementação aplicada às telas tradicionais de organização; build e lint aprovados. Integração dispensada pelo responsável; revisão visual completa e regressão manual do editor/panorâmica permanecem pendentes. Base preservada no commit `68b2a00`, enviado para `origin/main` antes deste planejamento.

## Objetivo e recorte

Organizar a experiência de escolher uma horta, consultar seus setores e canteiros e manter seus dados. A interface deve ter hierarquia clara, formulários legíveis e padrões consistentes no celular e no desktop, reduzindo superfícies, bordas e cartões aninhados.

Este plano detalha a apresentação e a navegação de RF02/RF13 e RNF04 dos [requisitos](../../Documento_Tecnico_Requisitos_Horta.md). Complementa [FE00](00_Navegacao_Contexto.md) e [FE02](02_Organizacao_Horta.md) no recorte atual; descrições históricas divergentes não orientam esta revisão. A [modelagem](../../arquitetura/Modelagem_Preliminar_Dados_Horta.md) continua preliminar; contratos e regras de negócio permanecem os existentes. As decisões técnicas continuam exclusivamente nas [regras de frontend](../../../.codex/arquitetura-frontend.mdc).

| Dentro desta revisão | Limite |
| --- | --- |
| Homepage | A entrada atual é a lista de hortas; não criar landing page ou outra aplicação |
| Estrutura de navegação | Marca, contexto, localização atual, menu responsivo e acessos de organização |
| Hortas | Listagem, dados, edição e formulário tradicional |
| Setores | Listagem, detalhe com canteiros e formulários tradicionais |
| Canteiros | Lista geral e por setor, ficha, retomada de cadastro e formulários tradicionais de identificação/área/setor |
| Visão geral | Apenas continuidade da navegação externa; manter conteúdo, indicadores e panorâmica nesta fase |

Preservar integralmente o cadastro interativo: etapas, ferramentas, desenho, catálogo visual, painel de plantio, persistência e navegação interna. Preservar também renderização panorâmica, câmera, gestos, texturas, sprites, projeção, densidade e acesso aos detalhes. Os destinos atuais de criar/selecionar horta continuam levando ao editor quando já o fazem.

Irrigação, ambiente, cultivo, produção, atividades, ocorrências, custos e indicadores detalhados permanecem com seus fluxos atuais. Os cultivos/plantios acessados pela ficha do canteiro continuam funcionando sem redesenho dos respectivos formulários. Não há mudanças de API, migrations, autenticação ou requisitos funcionais.

## Diagnóstico do código atual

- `AppShell.tsx` oferece menu e contexto, mas a área Horta oculta a navegação de seções. Lista de hortas, editor e manutenção ficam sob uma mesma entrada com destinos diferentes, dificultando descobrir a gestão tradicional.
- `GardenPage.tsx` é a homepage real. O destino principal da linha é o editor, embora o texto da página destaque organização de setores/canteiros; o destino precisa ficar explícito.
- `OrganizationLayout.tsx` já compartilha cabeçalho e linhas. É o ponto de partida para padronizar as listas, sem criar outra coleção de componentes equivalentes.
- `styles.css` acumula regras globais, componentes genéricos e sobrescritas de organização/interatividade. `.panel`, `.editor-panel`, `.organization-disclosure` e `.detail-facts > div` empilham superfícies e tratamentos diferentes.
- `EditorFrame.tsx` é usado em vários módulos e no cadastro interativo. Uma alteração global nele alcançaria telas fora do recorte.
- `BedPage.tsx` concentra consulta, manutenção, cadastro incompleto e acesso ao cultivo. A ficha precisa ordenar essas tarefas visualmente, preservando suas operações.

Diagnóstico baseado nos arquivos; a implementação deverá começar por capturas da interface real em desktop e celular para validar a composição e registrar a comparação.

## Direção visual

Uma área de trabalho clara, com verde como cor de ação e identidade, superfícies neutras e tipografia legível. A organização deve vir de alinhamento, espaçamento, títulos e divisórias. Cartões ficam restritos a blocos independentes que precisem de destaque; uma linha de cadastro, um grupo de campos ou cada atributo do detalhe não recebe cartão próprio.

| Elemento | Padrão a aplicar neste recorte |
| --- | --- |
| Hierarquia | Caminho de navegação → título e contexto → ação principal → filtros → conteúdo → paginação |
| Tipografia | Fonte atual; texto de leitura/entrada em 16 px; títulos de página 28–36 px; metadados 14 px |
| Espaçamento | Escala de 4/8 px; 16–24 px entre campos; 24–32 px entre seções |
| Cor | Reutilizar tokens `horta-*`; acento verde, divisórias discretas e estados com texto e ícone |
| Superfícies | Fundo neutro e conteúdo contínuo; sem sombras nas listas e sem painel dentro de painel |
| Ações | Uma ação principal por contexto; secundárias discretas; manutenção destrutiva separada |
| Ícones | Reutilizar `Icon`; acompanhar ações com rótulo, sem símbolos decorativos repetidos |
| Formato | Raios pequenos consistentes em controles; sem alternância entre estilos arredondados e quadrados nas telas revisadas |
| Largura | Listas ocupam a área útil; formulários limitam a linha de leitura a aproximadamente 720 px; editor mantém sua área atual |

Usar Tailwind nos componentes e os tokens existentes. Não acrescentar biblioteca nesta etapa: os controles nativos e os componentes existentes atendem ao recorte. Reavaliar somente diante de uma necessidade concreta de interação acessível.

## Navegação proposta

1. **Entrada / Hortas:** título curto, explicação de uma linha, ação “Nova horta”, filtro de situação e lista. Remover a faixa ilustrativa “1. Horta → 2. Setores → 3. Canteiros” desta listagem; ela não corresponde a uma tarefa em andamento.
2. **Selecionar horta:** manter o destino atual do editor, com rótulo explícito “Configurar horta”. Disponibilizar ação secundária “Ver setores” para a gestão tradicional, sem exigir passagem pelo editor. Quando `nextModule` estiver presente, preservar o destino solicitado.
3. **Contexto selecionado:** oferecer acessos claros a “Visão geral”, “Configurar horta”, “Setores”, “Canteiros” e “Dados da horta”. Os acessos de gestão aparecem nas telas tradicionais; não inserir abas ou barras dentro do editor/panorâmica.
4. **Setores → canteiros:** o nome da linha abre o próximo nível. O caminho no topo permite voltar à horta/setor; manter o filtro e a página de origem quando disponíveis na URL.
5. **Ficha do canteiro:** dados essenciais e situação primeiro; cultivo existente em seguida; manutenção e histórico depois. Pendência de área/setor recebe uma mensagem acionável próxima ao título.

No desktop, manter menu lateral com contexto e item atual. No celular, cabeçalho compacto com marca e menu; contexto e destinos ficam no menu acessível, sem duplicação de seletores na mesma tela. Todos os módulos já existentes continuam acessíveis; esta revisão não reorganiza seus fluxos internos.

### Composição das telas

| Tela | Conteúdo e ações |
| --- | --- |
| Homepage / Hortas | “Hortas” + “Nova horta”; situação; linhas com nome, localização e estado; ação principal de configuração e link de setores; paginação |
| Setores da horta | Caminho; nome da horta; “Novo setor”; situação; linhas com nome/descrição; acessos aos dados da horta e a todos os canteiros |
| Canteiros do setor | Caminho; nome do setor; “Novo canteiro”; linhas com nome, área conhecida e pendência; manutenção do setor em seção secundária |
| Todos os canteiros | Caminho; título; situação; nome, setor, área e estado de cadastro; paginação; acesso aos setores para criação contextual |
| Ficha do canteiro | Nome e situação; setor e medidas em lista de definição sem caixas por atributo; cultivos existentes; seção de vigências; histórico e manutenção |
| Formulário tradicional | Caminho; título e instrução curta; campos agrupados por assunto; dados complementares; erros; cancelar e salvar no mesmo rodapé |

As listas usam uma única sequência de linhas com divisórias. Em telas largas, alinhar metadados e ações em colunas; em telas estreitas, empilhar o conteúdo da mesma linha, sem transformar cada item em cartão e sem duplicar markup interativo. Não introduzir busca ou filtros sem suporte completo da API nem filtrar apenas a página carregada como se fosse todo o catálogo.

## Formulários e retorno da tarefa

- Nome e identificação aparecem primeiro. Campos complementares ficam abaixo com títulos claros; recolher apenas grupos opcionais extensos.
- Labels ficam acima, unidades nos rótulos e ajuda junto ao campo. Placeholder não substitui label. Campos relacionados podem formar duas colunas a partir de 640 px; nome, descrição e mensagens mantêm largura integral.
- Manter os passos atuais de identificação e conclusão do canteiro: a primeira gravação pode já ter ocorrido. Exibir qual etapa está salva e qual falta, sem aparentar que cancelar desfaz o cadastro persistido.
- Uma ação de salvar por formulário, com verbo específico. Cancelar retorna ao contexto de origem; rascunho alterado, escrita em andamento e resultado incerto mantêm as proteções existentes.
- Erros de campo ficam associados ao controle; erros de grupos recolhidos abrem o grupo e recebem foco. Conflito preserva o rascunho e oferece comparação explícita.
- Após salvar, manter a confirmação e o destino já definido pelo fluxo. Recarregamento de consulta que falhou após gravação não deve sugerir outra criação.
- Inativação e histórico ficam em manutenção, sem competir com a tarefa principal; motivo, versão e consulta dos dados anteriores permanecem disponíveis.

## Responsividade e acessibilidade

| Faixa | Comportamento esperado |
| --- | --- |
| 320–639 px | Menu em diálogo; margens de 16 px; listas e campos em uma coluna; ações quebram linha sem sobreposição |
| 640–1023 px | Margens de 24 px; campos relacionados em duas colunas quando legíveis; ações alinhadas ao conteúdo |
| A partir de 1024 px | Menu lateral; margens de 32 px; listas amplas e formulários com largura de leitura controlada |

Alvos de toque de pelo menos 44 px, foco visível, nomes longos com quebra de linha e sem rolagem horizontal da página. Validar zoom de 200%, teclado, menu com Escape e restauração de foco, título anunciado após navegação, labels/erros e contraste. Nenhuma barra fixa poderá cobrir campos, mensagens ou ações ao abrir o teclado do celular.

## Sequência de implementação

| Etapa | Trabalho delimitado | Evidência para concluir |
| --- | --- | --- |
| 1. Referência visual | Capturar homepage, setores, canteiros, ficha e formulário; capturar editor/panorâmica como referência de preservação | Percursos e tamanhos atuais registrados localmente, sem versionar screenshots temporários |
| 2. Navegação e padrões | Aplicar estrutura e padrões às telas tradicionais de organização; tornar destinos explícitos | Menu, contexto e caminho coerentes por clique, acesso direto, reload e voltar/avançar |
| 3. Homepage e listas | Unificar cabeçalhos, filtros, linhas, estados e paginação em `OrganizationLayout` e consumidores | Mesma hierarquia nas quatro listas, com dados reais e sem nova consulta agregadora |
| 4. Fichas e formulários | Reordenar dados/manutenção e aplicar apresentação delimitada aos formulários tradicionais | Criar, editar, cancelar, retomar pendência e revisar conflito preservam os dados |
| 5. Regressão | Build, lint, E2E com API/PostgreSQL isolados, revisão por teclado e larguras propostas | Critérios abaixo verificados e limitações registradas na execução do frontend |

Modificar `App.tsx`, `AppShell.tsx` e `Navigation.tsx` somente no necessário para distinguir destinos e contexto. Concentrar a apresentação do recorte em `features/organization`. Não substituir o roteamento hash ou a camada de estado/HTTP.

Se `EditorFrame`, `RecordDetail` ou `Fields` exigirem outra aparência, adicionar uma opção explícita de apresentação usada apenas pelas telas tradicionais deste recorte, mantendo a aparência padrão dos outros consumidores. Evitar sobrescritas por `.organization-shell`: o editor interativo também está sob essa classe. Classes de escopo devem identificar a tela tradicional, não todo o módulo Horta.

Não alterar `features/interactive-garden/**`, `cropVisuals`, projeção, serviços de desenho/cultivo, assets do mapa ou regras CSS do editor/panorâmica. Não alterar estilos globais de `button`, `input`, `fieldset`, `.panel` ou tokens compartilhados para obter um resultado local. Caso uma alteração externa influencie dimensões ou foco das áreas preservadas, ajustar a implementação antes de concluir.

## Critérios de aceite

1. Homepage oferece primeira horta e escolha entre configurar e consultar setores; `nextModule` continua levando à área solicitada.
2. Hortas, setores e canteiros compartilham cabeçalho, posição dos filtros, ações e paginação. Nenhum atributo de detalhe recebe cartão isolado.
3. Uma pessoa chega da lista de hortas ao canteiro pelos links visíveis e retorna ao setor sem perder a identificação da horta.
4. Criação tradicional, retomada de canteiro incompleto, edição com motivo, inativação e histórico continuam consumindo os contratos existentes.
5. Carregamento, lista vazia, filtro vazio, erro, cadastro pendente e inatividade têm mensagem e saída apropriadas; dados antigos não aparecem como resultado atualizado.
6. A interface funciona em 320, 390, 768, 1280 e 1920 px, com nomes extensos, listas paginadas e zoom de 200%; sem transbordamento horizontal nas telas reestruturadas.
7. Teclado permite percorrer menu, lista, formulário e retorno; foco e mensagens de erro continuam perceptíveis.
8. Editor interativo e panorâmica preservam aparência interna e comportamento. Verificar desenhar/salvar/reabrir, cadastro interativo, câmera/zoom/toque e abrir canteiro/retornar à panorâmica pelos testes existentes e comparação visual.
9. `npm run build`, `npm run lint` e os E2E afetados passam. Validar `organization.spec.ts` e `layout.spec.ts`, além dos cenários de `interactive-cultivation.spec.ts` e `crop-visuals.spec.ts` para as fronteiras preservadas; regressão de outros módulos quando componentes compartilhados forem tocados.

## Entrega e limpeza do repositório

O commit de base inclui aplicação, migrations, contratos, testes, lockfiles, documentos recebidos/entregues, instruções e ferramentas de reprodução do catálogo. Esses arquivos têm função mesmo quando não são consumidos no runtime do browser.

Antes desse commit foram removidos, após busca sem referências: `teste.html` (resposta HTML do Wikimedia), `src/assets/hero.png`, `src/assets/react.svg`, `src/assets/vite.svg`, `public/favicon.svg` e `public/icons.svg` do frontend. Assets do editor/panorâmica foram preservados. Credenciais locais, dependências, builds e relatórios permanecem ignorados pelo Git.

Este plano é posterior ao push da base. Sua implementação deve ser uma entrega separada e limitada ao recorte acima, com atualização dos READMEs e do registro de execução conforme o comportamento realmente verificado. O aceite funcional da PO e as pendências agronômicas existentes continuam separados da validação visual.
