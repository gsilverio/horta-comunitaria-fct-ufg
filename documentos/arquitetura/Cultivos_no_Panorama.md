# Cultivos no panorama

Incremento autorizado em 24/10/2026: pixel art por espécie/cultivar, fases de plantio, crescimento intermediário e colheita, e densidades visuais baseadas na quantidade cadastrada. O responsável escolheu **registros reais + prazos configurados**, sem inventar prazos agronômicos para o seed.

## Dados e fases

`GET /api/v1/gardens/{gardenId}/crop-visuals?on=YYYY-MM-DD&page=1&pageSize=100` retorna ciclos válidos vigentes na data, com espécie/cultivar, chaves editoriais, área ocupada, quantidade inicial, datas e fase. Página limitada a 100, ordenação por canteiro e ciclo; o frontend percorre todas as páginas. Cada página usa Repeatable Read, sem prometer snapshot único entre páginas ou em relação à consulta independente da geometria.

Somente plantios e colheitas válidos até a data participam; ciclos encerrados na data já não aparecem. Configurar prazos não altera fatos ou indicadores. O panorama usa o dia atual em `America/Sao_Paulo`, independente do período dos cartões do dashboard.

| Fase | Critério e apresentação |
| --- | --- |
| AwaitingPlanting | Sem plantio inicial elegível: nenhum sprite, quantidade desconhecida |
| Planted | Plantio registrado, sem prazos: sprite inicial e indicação de evolução não configurada |
| Seedling | Idade inferior ao prazo intermediário configurado: estimativa inicial |
| Growing | Idade entre prazo intermediário e prazo de colheita: estimativa de crescimento |
| HarvestExpected | Prazo de colheita atingido: **previsão**, não confirmação de maturidade |
| HarvestRecorded | Colheita válida com massa positiva: evidência real, possivelmente parcial; não esvazia o canteiro |
| Mixed | Datas de plantio diferentes ou reposições: mantém o ícone inicial de referência e identifica que não há fase conjunta estimada |

Colheita registrada prevalece sobre mistura e estimativas. Sem quantidade inicial, mesmo uma colheita não autoriza inventar quantos símbolos desenhar. Colheita com massa zero não confirma esta fase. Avaliações de pegamento não são convertidas em estimativa de sobrevivência neste incremento.

## Configuração de prazos

Em **Cultivo → Espécies → detalhe → Configurar fases visuais**, informar dias para crescimento intermediário e colheita prevista. A mesma ação existe no detalhe de cultivar. Campos vazios na cultivar herdam a espécie; configuração própria da cultivar prevalece integralmente. Nenhum prazo é incluído automaticamente no seed Embrapa.

Contratos PUT:

- `/api/v1/species/{id}/visual-growth`
- `/api/v1/species/{speciesId}/cultivars/{id}/visual-growth`

Corpo: `visualGrowthDays`, `visualHarvestDays`, `expectedVersion`, `reason`. Os dois prazos são inteiros positivos, com colheita posterior ao crescimento; ambos nulos removem a configuração. Domínio valida, banco reforça com checks, e versão/auditoria seguem o cadastro existente. Correções comuns de nome/descrição preservam os prazos. A migration `AddVisualGrowthTiming`, gerada pelo EF CLI, adiciona só quatro colunas opcionais e dois checks.

### Prazos conforme o livro da Embrapa (11/11/2026)

A pedido do responsável, os prazos fictícios de 5/10 e 5/15 dias foram substituídos por valores do livro *Cultivares da Embrapa Hortaliças (1981-2023)*. A gravação foi feita pelo `PUT /api/v1/species/{id}/visual-growth` no banco local, com versão esperada e motivo citando as páginas. Não houve seed, migration nem SQL direto. Cultivares não receberam prazo próprio e herdam o valor da espécie.

Critérios:

- **Colheita prevista:** média, por espécie, dos prazos das cultivares do livro. Para faixas, usa-se o ponto médio. Quando o livro separa por região, vale o Centro-Oeste/"demais regiões"; quando separa por sistema, vale o transplante de mudas.
- **Crescimento intermediário:** metade do prazo de colheita, arredondada. O livro não define essa fase, e o domínio exige os dois prazos juntos.
- **Sem prazo no livro:** espécie fica vazia e pode ser preenchida depois em **Cultivo → Espécies → Configurar fases visuais**. A lista de pendências está em [Espécies sem prazo de colheita](Especies_Sem_Prazo_de_Colheita.md).
- **Referências diferentes:** o livro conta a partir da semeadura, do transplante ou da emergência, conforme a ficha. A aplicação conta sempre a partir da data de plantio registrada. É estimativa, não maturidade confirmada.

| Espécie | Colheita (dias) | Crescimento (dias) | Base no livro |
| --- | ---: | ---: | --- |
| Abóbora | — | — | BRS Brasileirinha: só floração; colheita por tamanho do fruto (p. 10) |
| Abóbora híbrida | 100 | 50 | Jabras 90–110 (p. 12) |
| Alface | 40 | 20 | Leila, Lélia, Mediterrânea: 35–45 (p. 14–18) |
| Alho | 134 | 67 | Amarante 130–150, Hozan 150, Ito 95–130 fora do Sul (p. 20–24) |
| Batata | 106 | 53 | Ana 110, Camila 100–110, Cecília 110, Clara 100, Contenda 100, Potira 110; IPRBel sem dias (p. 220–232) |
| Batata-doce | 138 | 69 | 9 cultivares, 120 a 150–180 (p. 26–40, 234) |
| Berinjela | 60 | 30 | Ciça, 60 após transplantio (p. 42) |
| Cebola | 149 | 75 | 7 cultivares, Centro-Oeste/transplante (p. 44–56) |
| Cenoura | 99 | 50 | Brasília, Carmela, Esplanada, Paranoá, Planaltina, Kuronan; Alvorada e Planalto sem dias (p. 58–72) |
| Couve-brócolis | 80 | 40 | Ramoso de Brasília, início da colheita (p. 74) |
| Ervilha | 95 | 48 | 8 de grão seco (90–110) e 5 de grão verde (81–86); Sulina sem dias (p. 76–102) |
| Grão-de-bico | 121 | 61 | 6 cultivares, 108–140 (p. 104–114) |
| Lentilha | 120 | 60 | Precoz 110–120, Silvina 125 (p. 116–118) |
| Mandioquinha-salsa | 240 | 120 | Início da colheita aos 8 meses (p. 120–126) |
| Melão | 70 | 35 | Anton 75, Araguaia 70, Eldorado 300 60–70 (p. 128–132) |
| Milho-doce | 85 | 43 | 5 cultivares, 80–100 (p. 134–142) |
| Mostarda-branca | 130 | 65 | Gisilba (p. 144) |
| Pepino | 43 | 22 | 6 cultivares, 36–50 após semeadura (p. 146–156) |
| Pimenta (*C. chinense*) | 88 | 44 | Araçari 78, demais 90; Juruti sem dias (p. 158–180) |
| Pimenta dedo-de-moça | 70 | 35 | BRS Mari, ~70 após transplante (p. 170) |
| Pimenta e pimentão (*C. annuum*) | — | — | Brasilândia só início da frutificação; Ema só precocidade relativa; Garça, Sarakura e Acará sem dias (p. 162–166, 176, 182) |
| Repolho | — | — | União sem prazo (p. 184) |
| Tomate | 84 | 42 | 12 cultivares, 60–120; Sena, Kiara e Nagai sem dias; em Couto, os 80 dias referem-se à floração (p. 186–216) |

Observação de catálogo: a cultivar **BRS Alfa São Francisco** (p. 236) está cadastrada como cebola, mas a ficha descreve batata-doce. Ela não entrou em nenhuma média, e o vínculo não foi alterado.

### Destaque de canteiro pronto

Na panorâmica, quando um cultivo do canteiro está na fase **Colheita prevista**, o canteiro recebe contorno dourado pulsante, brilho nos símbolos das plantas e faíscas. Essa fase significa que a data de plantio somada ao prazo de colheita da espécie ou cultivar já foi atingida. O nome acessível do canteiro inclui "pronto para colheita", e a legenda explica o destaque. Com `prefers-reduced-motion`, o brilho fica estático.

## Quantidade e desenho

A referência é a soma das quantidades dos **plantios iniciais válidos** do ciclo até a data, dividida pela área ocupada do cultivo. Reposições não são somadas novamente; sua existência é informada. A quantidade inicial não é uma contagem de plantas vivas.

| Faixa estética | Quantidade inicial / área ocupada | Símbolos pretendidos por cultivo |
| --- | --- | ---: |
| Baixa | até 4 plantas/m² | 2 |
| Média | acima de 4 até 12 plantas/m² | 4 |
| Alta | acima de 12 plantas/m² | 6 |

Os limites são parâmetros de apresentação, **não** recomendação de espaçamento, produtividade ou lotação agronômica. Nunca desenhar mais símbolos que a quantidade inicial. Sem quantidade ou área válida, não assumir zero ou criar plantas. Exemplo para 4 m²: 8 plantas → baixa/2 símbolos; 24 → média/4; 80 → alta/6. Redução solicitada em 06/11/2026 para diminuir a sobreposição sem alterar as quantidades cadastradas.

Há orçamento máximo de 12 símbolos por canteiro, distribuídos em rodadas entre cultivos; em um consórcio muito grande a cena é uma amostra e o inspetor mantém a lista de todos os ciclos. Zoom não altera quantidade, seleção de assets ou posições no terreno. Distribuição respeita retângulos rotacionados, canteiros circulares e setores de anel. Ainda não há delimitação física da área de cada ciclo no desenho; símbolos de consórcios compartilham o canteiro e não representam a posição real de cada planta.

No formulário contextual de plantio, uma prévia identifica a densidade visual da quantidade digitada. O contrato continua separando abertura de cultivo e registro do plantio: não transformar quantidade planejada em fato antes de plantar.

## Assets e cultivares

`tools/catalog/generate_crop_sprites.py` gera **102 SVGs próprios**, em grade inteira de 32 × 40 pixels: três fases para os 23 grupos do catálogo, um fallback genérico e 10 variações editoriais. Arquivos em `hortacomunitaria-webapp/public/assets/pixel-garden/crops/`, manifesto em `src/features/interactive-garden/crop-assets.json`.

Todas as 113 cultivares possuem mapeamento explícito. Cultivares sem diferença visual comprovada compartilham o asset da espécie e o inspetor informa isso. Overrides de tonalidade das alfaces das páginas 14/16/18 e pimentas das páginas 158/160/168/172/174/178/180 usam as características das fichas do livro; não afirmam distinguir todas as cultivares visualmente. O estágio inicial é simplificado e pode ser semelhante entre espécies.

Chaves editoriais preservam a associação após renomeações. Cadastros manuais com nome científico reconhecido usam a espécie correspondente; os demais recebem ícone genérico identificado, sem adivinhar a espécie pelo nome comum. Fotos Commons permanecem separadas: estes SVGs não são conversões/derivações das fotos, nem substituem os créditos delas.

Sprites são recursos estáticos da aplicação e não são duplicados em Base64 por linha do banco. Isso mantém o pacote pequeno e editável; a exceção de fotos Base64 do catálogo permanece restrita às fotografias. Todos os arquivos são locais, sem chamada externa para desenhar.

Dados de cultivo não são gravados no rascunho geométrico do `sessionStorage`: são recarregados do servidor ao entrar na visão geral. Rascunhos sem vínculo cadastral não exibem plantas reais. Em falha de consulta, exibir erro/indisponibilidade; não reaproveitar as mudas decorativas antigas como se fossem registros.

## Verificação

Build Release do backend sem warnings; build TypeScript/Vite e ESLint aprovados. Treze testes de integração aprovados em PostgreSQL 17 descartável, incluindo prazos, herança/override, auditoria e concorrência, fases por data, reposições, colheita/invalidação, encerramento e regressões do seed/cadastros. A conferência final da política de fases foi repetida em dois testes, e o EF CLI não encontrou mudanças pendentes do modelo.

Três testes Playwright cobrem limites/ausência da densidade e orçamento, percurso real de configuração dos prazos e panorama com API/PostgreSQL, desktop e celular, colheita parcial e carregamento dos 102 SVGs/113 mapeamentos. Capturas de inspeção ficam em `test-results/`, ignoradas pelo Git. Não foi aplicada migration nem seed no banco de uso; contextos de teste são descartados.

## Demonstração local solicitada em 06/11/2026

Na horta existente `horta teste`, foram preenchidos os três canteiros vagos pela API, sem seed automático, migration ou alteração do desenho. Os três ciclos e plantios anteriores (couve-brócolis, cenoura e berinjela) foram conferidos antes/depois e preservados integralmente.

| Canteiro | Espécie | Data do plantio | Quantidade cadastrada | Fase em 06/11/2026 |
| --- | --- | --- | ---: | --- |
| Retângulo 3 | Alface | 29/10/2026 | 8 | Crescimento intermediário |
| Mandala · Anel 1 · Canteiro 1 | Tomate | 28/10/2026 | 24 | Crescimento intermediário |
| Mandala · Anel 1 · Canteiro 2 | Pepino | 25/10/2026 | 60 | Colheita prevista |

Somente esses canteiros sem configuração receberam área demonstrativa de 4 m², sem inferência a partir do grid. Alface, tomate e pepino receberam prazos visuais fictícios de 5 dias para crescimento e 10 para colheita prevista, com motivo registrado na auditoria; os prazos das espécies já plantadas foram preservados. Colheita prevista seleciona o asset maduro, sem confirmar disponibilidade agronômica ou registrar colheita.

Conferência somente de leitura da panorâmica com API local: 8 símbolos dos cultivos anteriores, 6 intermediários e 6 de colheita prevista, tanto no desktop quanto no celular. Não foi executada a suíte de integração nesta etapa. Os testes existentes tiveram suas expectativas de contagem atualizadas para 2/4/6 e orçamento de 12.

## Ampliação da demonstração local em 06/11/2026

A horta teste recebeu oito novos canteiros (07–14) em um setor de diversidade na parte livre do terreno. O mapa passou à versão 2, preservando as formas, posições e dimensões dos elementos anteriores. Os seis canteiros, ciclos e plantios já existentes foram comparados antes/depois e permaneceram intactos.

| Novo canteiro | Espécie | Plantio | Fase na data da preparação |
| --- | --- | --- | --- |
| 07 | Abóbora | 05/11/2026 | Mudas iniciais |
| 08 | Cebola | 29/10/2026 | Crescimento intermediário |
| 09 | Ervilha | 20/10/2026 | Colheita prevista |
| 10 | Milho-doce | 27/10/2026 | Crescimento intermediário |
| 11 | Repolho | 18/10/2026 | Colheita prevista |
| 12 | Alho | 04/11/2026 | Mudas iniciais |
| 13 | Mostarda-branca | 31/10/2026 | Crescimento intermediário |
| 14 | Pimenta dedo-de-moça | Ainda não registrado | Cultivo aberto, aguardando plantio |

Total local: 14 canteiros e 14 espécies, com cinco fases disponíveis. As espécies novas receberam prazos ilustrativos fictícios de 5/15 dias e áreas demonstrativas de 4 m²; não são parâmetros agronômicos. O desenho usa unidades livres e não foi convertido em m². A preparação ocorreu explicitamente pela API, sem seed automático ou migration. A panorâmica real foi conferida no navegador após persistir os dados.
