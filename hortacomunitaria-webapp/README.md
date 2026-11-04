# Frontend — Horta Comunitária FCT/UFG

O fluxo de organização segue **horta → setor → canteiro → cultivo e plantio**. Cada nível mostra seus próprios cadastros, uma ação principal e o caminho de retorno. O menu mantém todos os módulos e integra a marca à lateral; Organização aparece antes de No campo. No celular, marca e botão do menu ficam juntos, sem faixa exclusiva para a logo.

## Executar

Use Node **24.15.0**, npm **11.12.1**, PostgreSQL **17** e a API local na porta **5163**. React **19.2.8**, TypeScript **6.0.2**, Vite **8.3.0** e Tailwind CSS **4.3.3** permanecem na stack existente.

```powershell
npm ci
npm run dev
```

Abra [a interface local](http://127.0.0.1:5173). O [Get Started](../documentos/Get_Started.md) descreve banco, migrations e autoria técnica. A aplicação não cria dados de exemplo.

O proxy usa `API_PROXY_TARGET=http://127.0.0.1:5163` e `VITE_API_BASE_URL=/api/v1`. Somente variáveis públicas podem usar o prefixo VITE; conexão PostgreSQL fica no backend.

## Percurso

O [panorama de cultivos](../documentos/arquitetura/Cultivos_no_Panorama.md) mostra sprites de três fases e densidade ilustrativa dos plantios confirmados. Em Cultivo → Espécies ou Cultivares → detalhe → **Configurar fases visuais**, defina os prazos opcionais; sem configuração não há avanço automático por tempo. A API precisa da migration `AddVisualGrowthTiming`. Os assets são locais e a consulta de cultivos é separada do rascunho geométrico.

1. **Nova horta → Cadastrar horta** abre os setores da horta criada.
2. **Novo setor → Cadastrar setor** abre os canteiros desse setor.
3. **Novo canteiro → Continuar** salva a identificação. **Concluir cadastro** confirma área, setor e data de início.
4. **Ver canteiro** mostra área e setor atuais e os cultivos associados. **Adicionar cultivo** permite escolher a planta, cadastrar uma espécie sem perder o preenchimento e informar data/área. Depois, é possível registrar o plantio e sua quantidade ou voltar ao canteiro para fazê-lo mais tarde. Histórico e alterações por data ficam recolhidos.
5. O caminho no topo permite retornar ao setor, à horta ou a todas as hortas.

A área Horta usa o caminho hierárquico, sem abas concorrentes. O menu lateral dá acesso aos demais módulos; a troca de horta volta à lista de hortas. Listas compartilham apresentação, ações e paginação. Localização e descrição são opcionais; áreas da horta ficam recolhidas. Nenhum código manual é exigido.

**Todos os canteiros e cadastros pendentes** permite retomar cadastros incompletos e consultar inativos. Em falha parcial, a conclusão consulta o estado persistido antes de retomar. Em conflito, o rascunho é preservado e a versão atual precisa ser revisada explicitamente.

## Organização técnica

Seguir as [regras do frontend](../.codex/arquitetura-frontend.mdc). A composição atual usa `features/organization/pages/OrganizationPage.tsx`; páginas, componentes, hooks e serviços HTTP permanecem separados. O cliente Axios comum normaliza erros. A navegação hash mantém contexto e paginação na URL, sem rascunhos.

Rotas principais: `#/hortas/_/horta/hortas`, `#/hortas/{gardenId}/horta/setores`, `#/hortas/{gardenId}/horta/setores/{sectorId}` e `#/hortas/{gardenId}/horta/canteiros/{bedId}`. Formulários usam `novo`, edição usa `edit=1` e conclusão do canteiro usa `setup=1`. Rotas antigas de preparação abrem os setores. Os demais módulos mantêm suas rotas. Dentro do canteiro, `cultivate=1` abre a definição do cultivo e `plantingCycle={id}` abre o plantio, verificando que o cultivo pertence àquele canteiro.

Os contratos administrativos e de vigência permanecem os existentes, sem migration. A lista de canteiros do setor consulta `GET /gardens/{gardenId}/beds?sectorId=...&on=...` com paginação e carrega os detalhes dos itens daquela página. Área e setor são os vigentes em `America/Sao_Paulo`.

## Verificação

```powershell
npm run build
npm run lint
npx playwright install chromium
$env:HORTA_E2E_POSTGRES_URL='postgres://USUARIO:SENHA@127.0.0.1:PORTA/postgres'
npm run test:e2e
```

O E2E executa todos os módulos: `organization.spec.ts` cobre a organização, plantio pelo canteiro e menu; `mvp.spec.ts` e `irrigation.spec.ts` preservam a regressão dos demais fluxos.

O setup usa um banco descartável `horta_e2e_<uuid>`, API real em **5279** e preview em **5183**, e encerra seus processos e banco ao terminar. As portas precisam estar livres. Não altera o banco da aplicação. As fixtures são exclusivas dos testes; o navegador usa Asia/Tokyo para verificar que as datas continuam no fuso da horta.

Relatórios, screenshots e logs ficam em `playwright-report/` e `test-results/`, ignorados pelo Git. A validação cobre percurso completo, cultivo/plantio pelo canteiro, nova espécie durante o preenchimento, retomada após resposta perdida, cadastros pendentes, contexto/paginação, conflito de edição, todos os módulos do menu e layout de 390 px com teclado. Aceite funcional da PO permanece pendente.

O menu permanece disponível também na listagem de hortas, no desktop e no celular. Ao escolher uma área sem horta selecionada, selecione a horta na lista para abrir a área desejada.
O rodapé ocupa o fim da janela nas páginas curtas e acompanha o final do conteúdo nas páginas longas, com o mesmo layout em todas as áreas.
O seletor de horta fica no menu e inicia com “Selecione uma horta” quando não há contexto. A seleção abre a área escolhida; sem hortas ativas disponíveis, oferece cadastrar uma horta.

## Experimento do modo interativo

A configuração padrão da horta abre diretamente o editor em `#/hortas/{gardenId}/horta/interativa`. Medidas reais são opcionais; o grid usa unidades de desenho (u), sem equivalência com metros. A primeira etapa obrigatória é **Delimitar terreno**: a tela abre sem terreno cadastrado e sem quadrado padrão. Marque os vértices de um contorno irregular e clique no primeiro ponto para fechar o terreno diretamente no grid; o botão Concluir terreno continua disponível. **Editar terreno** permite arrastar vértices ou redesenhar o contorno diretamente no grid; ampliar e reduzir não ficam limitados ao enquadramento inicial da tela. Cancelar preserva o terreno anterior. Contornos cruzados ou que excluem objetos existentes são rejeitados. Os objetos permanecem dentro do terreno desenhado.

Após concluir o terreno, delimite pelo menos um setor antes de inserir canteiros ou mandalas. Canteiros parcialmente sobrepostos também são associados automaticamente ao setor. Quando houver mais de um setor sobreposto, o vínculo atual prevalece enquanto houver interseção; caso contrário, usa-se o primeiro setor desenhado que intersecta o canteiro. Mover objetos e editar limites atualiza essa associação no rascunho.

Cada mandala acrescenta canteiros com IDs próprios e preserva os objetos anteriores. As peças são canteiros, e o centro é uma referência visual. Seleção múltipla, movimento, rotação, propriedades, desfazer/refazer dos objetos e recarregamento preservam as mandalas. O contorno do terreno tem desfazer de pontos antes da conclusão e pode ser redesenhado depois.

**Salvar configuração** confirma terreno, setores, canteiros e suas posições no PostgreSQL. Novos setores e canteiros desenhados geram cadastros e o vínculo inicial do canteiro ao setor, na mesma transação auditada. O rascunho em sessionStorage continua identificado como não salvo; a visão panorâmica usa exclusivamente o mapa confirmado pela API. Ao abrir em outra aba ou navegador, a configuração salva é recuperada. Remover uma forma do mapa preserva seu cadastro e histórico. Alterar o setor de um canteiro já vinculado exige registrar a nova vigência no cadastro antes de salvar o mapa.

Teste isolado do editor (Chromium, porta 5186 livre, respostas cadastrais fictícias interceptadas no navegador):

```powershell
node node_modules/@playwright/test/cli.js test --config playwright.editor.config.ts
```

Esse teste verifica terreno obrigatório sem contorno padrão, edição/ampliação/redução e cancelamento, contorno irregular, medidas ausentes, bloqueio sem setor, sobreposição parcial, duas mandalas, IDs únicos, desfazer/refazer e recarregamento. Não substitui os E2E com API/PostgreSQL descritos acima.

As etapas do mapa usam três cartões responsivos com numeração, estado atual, conclusão e dependências explícitas. A etapa ativa tem indicação textual e aria-current; as anteriores podem ser reabertas quando disponíveis.

O painel separado de delimitação foi removido e o grid ocupa mais espaço. Nos dois mapas, a roda comum rola a página e Ctrl/Command + roda aplica zoom ancorado no cursor, com normalização para mouse/trackpad e sem rolar a página simultaneamente. Os botões +/− continuam disponíveis. Na panorâmica, o cálculo de zoom e arraste respeita as margens do SVG; em dispositivos de toque, a cena permite rolagem vertical. Os testes isolados cobrem fechamento no ponto inicial, edição por arraste, rolagem e zoom nos dois mapas.

O painel Objetos do desenho foi removido; os objetos são selecionados diretamente no mapa. Durante a delimitação/edição do terreno, formatos retangular, quadrado, circular e em L podem ser escolhidos explicitamente. Eles geram nós editáveis e só são aplicados ao concluir. Clique esquerdo adiciona um nó (no segmento mais próximo em contornos fechados); clique direito sobre um nó o remove, sem abrir o menu do navegador. Arrastar move o nó; Delete/Backspace também o remove quando focado. O primeiro nó mantém a opção de fechar o terreno.

Para terreno irregular, use **Setor livre** para marcar um contorno por nós ou **Setor com contorno do terreno** para reaproveitar o limite da horta. Conclua pelo primeiro nó ou por Concluir setor. Selecione um setor livre e abra suas propriedades para Editar contorno do setor. A validação considera o polígono real, preserva os vínculos dos canteiros e rejeita limites fora do terreno. Canteiros adicionados pelos botões procuram uma posição dentro da área real do setor. A panorâmica também desenha os contornos livres.

A remoção pelo botão direito é capturada no próprio SVG, com tolerância de 22 px em torno do nó, antes dos elementos sobrepostos; o evento de menu de contexto apenas impede o menu nativo. Os nós editáveis são desenhados acima dos objetos. Os testes cobrem remoção única, setor sob o nó, setores livres, terreno em L, edição e recarregamento.

A seleção de formatos usa cartões inspirados em jogos de construção, com miniaturas de terreno em pixel art feitas em SVG, relevo, destaque de hover e indicação textual/aria-pressed da base escolhida. O layout mostra quatro opções no desktop e duas colunas no celular.

A caixa de ferramentas separa Terreno, Setores e Canteiros em três grupos com aparência de construção de jogo. Cada grupo informa seu propósito e dependências; em Canteiros, desenhar com arraste e inserir formas prontas são ações distintas. Câmera/grade e histórico ficam em barras separadas. **Remover terreno** apaga o contorno salvo apenas quando não há setores visuais. A remoção atualiza o rascunho persistido, sua base de restauração e o histórico local, impedindo que o terreno reapareça ao desfazer ou recarregar.

As ferramentas e os formatos agora ficam integrados ao bloco do mapa em um menu acoplado no canto superior direito. Recolher o menu amplia a área de desenho; seu conteúdo tem rolagem própria, sem acionar a câmera nem rolar a página. Em telas pequenas, o menu ocupa a largura disponível acima do grid e pode ser recolhido. Os formatos do terreno usam cartões compactos em duas colunas dentro do próprio grupo Terreno.

O desenho usa barra horizontal de câmera/grade/histórico sobre o grid e menu lateral contínuo à direita, alinhado ao topo do conjunto. As ferramentas e os formatos no menu usam listas e divisórias, sem cartões internos, mantendo as miniaturas e estados de seleção. O limite geral de 1480 px e o limite adicional da área Organização foram removidos; o conteúdo principal aproveita a largura disponível em telas grandes, com margens responsivas.

O conjunto do desenho usa CSS Grid explícito: barra superior e grid na coluna principal, lateral abrangendo as duas linhas. A malha inclui linhas principais a cada cinco unidades. Em Setores, o seletor identifica um setor específico do mapa para **Editar setor** ou **Excluir setor**; a exclusão usa somente seu ID, mesmo quando outros objetos estavam selecionados.

Durante a edição de terreno ou setor livre, arraste uma aresta para deslocar seus dois nós perpendicularmente ao segmento, ampliando ou reduzindo o contorno sem mudar a direção da aresta. A malha magnética continua aplicada. Um clique sem arraste sobre a aresta insere um nó; os nós existentes permanecem arrastáveis e removíveis pelo botão direito. Com a aresta focada, as setas do teclado fazem ajustes de 0,5 unidade. A conclusão continua validando cruzamentos, terreno e objetos existentes.

Na panorâmica, setores são contornos tracejados sem preenchimento; o botão Setores permite mostrar/ocultar limites e nomes sem alterar texturas ou sprites. Setores visuais substituem os envelopes estimados, evitando renderização duplicada. Caixas de canteiros usam faces visíveis pela orientação do polígono, altura proporcional à câmera e textura de madeira projetada individualmente na face vertical. A faixa útil do asset é enquadrada por SVG, sem modificar o arquivo. Plantas acompanham o nível elevado do solo, mantendo proporções. Canteiros retangulares, girados, circulares e curvilíneos usam a mesma construção de volume.

O arraste da câmera panorâmica usa coordenadas locais do SVG e termina ao soltar, cancelar ou sair da área da cena. Arrastar sobre canteiros também move a câmera; um clique sem arraste abre os detalhes. No toque, o gesto dentro da cena movimenta apenas a câmera; a roda comum continua rolando a página e Ctrl/Command + roda controla o zoom.

A opção **Mostrar informações no mapa** inicia desmarcada. Quando ativada, mostra tipo e nome em etiquetas com contraste e linhas de referência, evitando sobreposição e priorizando a seleção. Quando não há espaço, os detalhes continuam disponíveis pelo clique no elemento. O painel distingue unidades de desenho (u/u²) de medidas reais.

### Estação de cultivo no canteiro

Clique em um canteiro do editor (ou selecione-o com Enter/Espaço) para abrir o painel lateral de cultivo integrado ao desenho, substituindo o menu de construção. A ficha única de plantio reúne data, área e quantidade, com campos complementares recolhidos. O fluxo começa por **escolher espécie no catálogo visual → salvar desenho, se necessário → preparar área real e setor → cadastrar cultivo → registrar plantio**. O catálogo usa a base Embrapa importada na API, nove opções por página, fotos em quadrados, seleção por clique/teclado e créditos/licenças disponíveis em cada imagem. A espécie escolhida permanece ao paginar e segue para o formulário sem nova seleção. Os formulários existentes preservam validação, concorrência, campos opcionais e repetição segura. Área do grid não vira m²: a área cultivável real precisa ser informada para abrir o cultivo.

A escolha inclui espécie, variedade opcional, início e área ocupada; o plantio inclui data, quantidade, tipo inicial/reposição e, opcionalmente, lote de origem, espaçamentos e responsável. Cultivos salvos sem plantio aparecem como pendentes e podem ser retomados ao reabrir o canteiro, sem criar outro cultivo. O menu também mantém acesso à edição da forma/posição. Os registros confirmados aparecem nos cadastros e na panorâmica.

O editor é a entrada padrão ao criar/selecionar uma horta ou abrir a área Horta. A rota antiga de preparação leva ao mesmo editor; os cadastros tradicionais continuam nas seções de setores/canteiros. Na panorâmica, setores com plantio confirmado em cultivo ativo recebem [grama trabalhada](../documentos/arquitetura/Grama_Trabalhada.md), usando a mesma paleta, escala e alinhamento da grama original e transição suave nas bordas. Ciclos sem plantio, encerrados e rascunhos não ativam essa variação.

Ao selecionar um canteiro na panorâmica por clique, toque ou teclado, a câmera aproxima e centraliza a peça na área livre ao lado do painel. A transição respeita a preferência por movimento reduzido e é interrompida por arraste, zoom manual ou Enquadrar horta. No celular, os dados aparecem abaixo da cena para não cobrir o canteiro. **Ver detalhes do canteiro** abre o cadastro pelo ID confirmado da mesma horta, com identificação, setor, área/medidas reais, cultivos, plantios e histórico, e oferece retorno à panorâmica. Formas sem vínculo cadastral não recebem esse link.
