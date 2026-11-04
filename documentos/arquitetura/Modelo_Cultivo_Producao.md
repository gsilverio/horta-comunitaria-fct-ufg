# Modelo de cultivo e produção

**Cobertura:** RF03–RF05, RF08 e RF09; alimenta RF14 e usa RF12/RF13. Ver [convenções comuns](Modelagem_Preliminar_Dados_Horta.md) e [diagramas](Diagramas_Relacionais_Horta.md#cultivo).

O fluxo liga a origem do material ao plantio, acompanha o cultivo em um ciclo e registra cada colheita. A secagem mantém sua origem sem exigir atribuições de massa que a equipe não mediu.

## Espécies, origem e ciclos

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `especie` | `nome_comum`, `nome_cientifico?`, `descricao?`, `ativo` | Catálogo com cultivares, lotes e ciclos |
| `cultivar` | `especie_id`, `nome`, `descricao?`, `ativo` | Pertence a uma espécie |
| `lote_propagacao` | `horta_id`, `codigo`, `especie_id`, `cultivar_id?`, `tipo_material`, `origem?`, `recebido_em?`, `observacoes?` | Material de origem para zero ou muitos plantios |
| `ciclo_cultivo` | `canteiro_id`, `configuracao_canteiro_id`, `especie_id`, `cultivar_id?`, `iniciado_em`, `encerrado_em?`, `area_ocupada_m2`, `responsavel_participante_id?`, `situacao` | Reúne plantios, observações e colheitas de um cultivo |

Recebimento, início e encerramento são datas. O lote é um identificador de procedência; não possui saldo de estoque. O código é único na horta. Origem ou cultivar desconhecida permanece sem informação.

Um ciclo pertence a um canteiro e uma espécie; cultivar é opcional. Essa unidade é uma proposta para distinguir cultivos sucessivos, não uma decisão definitiva sobre consórcios. A configuração referenciada pertence ao mesmo canteiro, e a área ocupada é positiva e compatível com a área cultivável. Encerramento não precede o início.

A configuração de referência corresponde à condição física usada ao abrir o cultivo. Não impor um único ciclo ativo por canteiro nem considerar a ocupação conjunta resolvida apenas por validar cada área isoladamente: sobreposição, consórcio e limite de ocupação conjunta precisam da política acordada com a PO.

Cultivar de ciclo ou lote deve pertencer à espécie informada. Material e ciclo precisam ter a mesma espécie; cultivares conhecidas não podem conflitar. Não obrigar a preencher uma cultivar desconhecida só para construir a relação.

## Plantio, avaliações e observações

| Entidade | Campos propostos, além de `id` e metadados comuns | Relações |
| --- | --- | --- |
| `plantio` | `ciclo_id`, `lote_propagacao_id?`, `plantio_reposto_id?`, `data`, `natureza`, `quantidade_plantada`, `espacamento_entre_plantas_cm?`, `espacamento_entre_linhas_cm?`, `responsavel_participante_id?` | Um ciclo; origem opcional; reposição ligada ao plantio anterior quando identificável |
| `avaliacao_pegamento` | `plantio_id`, `data`, `quantidade_sobrevivente`, `metodo`, `observacoes?`, `responsavel_participante_id?` | Uma avaliação de um plantio específico |
| `observacao_agronomica` | `ciclo_id`, `data`, `categoria`, `descricao`, `medida?`, `unidade?`, `metodo?`, `responsavel_participante_id?` | Uma observação do cultivo; fotografias via evidência |

- Um ciclo tem vários plantios. Natureza distingue plantio inicial e reposição; a reposição não altera a quantidade original do plantio anterior.
- Quando preenchida, `plantio_reposto_id` aponta para outro plantio do mesmo ciclo, de data não posterior. Não permitir autorreferência nem cadeia circular. Reposição sem origem individual conhecida pode ficar identificada apenas pela natureza.
- Quantidade plantada é inteira e positiva; sobreviventes ficam entre zero e a quantidade daquele plantio. Espaçamentos conhecidos são positivos.
- Um plantio tem várias avaliações em datas diferentes. Não somar sobreviventes de avaliações sucessivas nem impor unicidade por dia sem definir o protocolo de coleta.
- Observação registra evidência ou suspeita, sem exigir diagnóstico. Medida exige unidade e método compatíveis; descrição pode existir sem medida.

**Exemplo:** plantio de 40 mudas, avaliação com 36 sobreviventes e posterior reposição de quatro mudas são três fatos distintos. O plantio inicial mantém seu exemplo de pegamento de 90%; a reposição terá sua própria avaliação.

## Colheita e secagem

| Entidade | Campos propostos, além de `id` e metadados comuns | Relações |
| --- | --- | --- |
| `colheita` | `ciclo_id`, `data`, `massa_fresca_kg`, `massa_aproveitavel_kg?`, `motivo_perda?`, `responsavel_participante_id?` | Um ciclo; zero ou muitas destinações à secagem |
| `lote_secagem` | `horta_id`, `codigo`, `metodo?`, `iniciado_em`, `encerrado_em?`, `situacao`, `massa_seca_total_kg?`, `responsavel_participante_id?` | Lote que recebe uma ou mais origens ao registrar uma destinação |
| `item_secagem` | `lote_secagem_id`, `colheita_id`, `massa_destinada_kg`, `massa_seca_atribuida_kg?`, `metodo_atribuicao?` | Associação entre lote e colheita com as massas dessa origem |

Massas são decimais em kg; datas de secagem são datas de calendário nesta proposta. Colheita e lote pertencem à mesma horta. Um lote pode ser preparado sem itens, mas não possui rendimento calculável antes de receber massa.

- Massa fresca e massas conhecidas não são negativas; aproveitável não supera fresca. Massa destinada é positiva.
- Perda na triagem é calculada apenas quando fresca e aproveitável forem conhecidas. Não confundir a perda de água da secagem com descarte da colheita.
- Uma colheita pode alimentar vários lotes; um lote pode receber várias colheitas. Proposta: um item por par lote/colheita, somando a destinação dessa origem no lote.
- O saldo considera a soma das destinações válidas de uma colheita. A proposta limita essa soma à massa aproveitável conhecida; se ela for desconhecida, a massa fresca é apenas um teto e a disponibilidade para secagem precisa de revisão com a equipe.
- Destinações concorrentes disputam o mesmo saldo e precisam ser confirmadas sem ultrapassá-lo. Corrigir/inutilizar uma colheita já destinada exige revisar os itens afetados.
- `massa_seca_total_kg` é a pesagem do lote. A massa seca atribuída por item só é informada quando sua origem puder ser identificada ou o método de atribuição estiver validado.
- Quando todas as origens tiverem atribuição completa, sua soma deve conferir com o total do lote. Atribuições parciais não podem ultrapassá-lo. Não somar o total do lote novamente com suas atribuições.
- Se a secagem misturar espécies e só houver pesagem total, o rendimento do lote pode ser apresentado; massa seca por espécie continua indisponível até haver atribuição confiável.

**Exemplo:** duas colheitas destinam 3 kg e 2 kg a um lote, que termina com 1 kg seco. O lote tem 5 kg destinados e rendimento exemplificativo de 20%. A massa seca de cada origem não é inventada pela proporção de entrada.

## Decisões a validar

Unidade do ciclo, consórcios e ocupação de área; janela/método de pegamento; tratamento de sementes em relação às mudas; definição de massa fresca/aproveitável; disponibilidade para secagem; pesagem por origem ou por lote; atribuição em lotes mistos; critérios de conclusão e seleção das avaliações. Fórmulas constam em [Dados dos indicadores](Dados_Indicadores.md) como propostas.
