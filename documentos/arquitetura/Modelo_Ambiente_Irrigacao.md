# Modelo de ambiente e irrigação manual

**Cobertura:** RF06 e RF07; relações com RF02, RF11–RF14. Ver [convenções comuns](Modelagem_Preliminar_Dados_Horta.md) e [diagramas](Diagramas_Relacionais_Horta.md#ambiente).

Coletas e irrigações são fatos distintos. A coleta registra condição ambiental e método; a irrigação registra um evento de aplicação de água e seus canteiros atendidos.

## Entidades e informações

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `variavel_ambiental` | `codigo`, `nome`, `grandeza`, `unidade_referencia`, `descricao?`, `ativo` | Catálogo de grandezas, começando por umidade do solo |
| `ponto_coleta` | `horta_id`, `codigo`, `descricao`, `localizacao?`, `canteiro_id?`, `setor_id?`, `ativo` | Local da horta, com no máximo um contexto específico |
| `medicao_ambiental` | `ponto_coleta_id`, `variavel_ambiental_id`, `coletado_em`, `valor`, `unidade`, `metodo`, `qualidade`, `vinculo_canteiro_setor_id?`, `responsavel_participante_id?`, `observacoes?` | Um ponto e uma variável; contexto histórico conhecido |
| `irrigacao` | `setor_id`, `iniciado_em`, `encerrado_em?`, `duracao_ativa_min?`, `volume_l?`, `vazao_l_min?`, `forma_obtencao_volume?`, `metodo_volume?`, `situacao`, `responsavel_participante_id?`, `observacoes?` | Setor do evento; zero ou muitos canteiros atendidos |
| `irrigacao_canteiro` | PK composta (`irrigacao_id`, `canteiro_id`), `vinculo_canteiro_setor_id` | Uma associação por canteiro efetivamente atendido no evento |

Medições e irrigações possuem os metadados comuns de autoria, validade e versão. Coleta/início/fim são instantes; valor, volume, vazão e duração ativa são decimais. A unidade da medição é preservada no fato, mesmo que o catálogo mude posteriormente.

## Coletas e contexto

- Um ponto pertence à horta. Pode apontar para um canteiro, um setor ou ser geral; não aponta para canteiro e setor simultaneamente.
- O código do ponto é único na horta. Uma vez usado, mudar seu contexto cria um novo ponto ou exige correção rastreada dos fatos afetados; não deslocar silenciosamente o histórico.
- Quando o ponto for de canteiro, guardar o vínculo de setor conhecido na data local da coleta. Vínculo nulo indica ausência de associação conhecida, não autorização para usar o setor atual posteriormente.
- O vínculo histórico, quando preenchido, pertence ao canteiro do ponto e é vigente na data da coleta. Pontos de setor/gerais não preenchem essa referência de canteiro.
- Unidade e método precisam acompanhar o valor. Medidas incompatíveis permanecem separadas; não calcular uma média entre métodos diferentes de umidade só por ambos usarem percentuais.
- Qualidade distingue uma informação duvidosa; é diferente de invalidação. A elegibilidade no dashboard depende da política validada com a PO.
- Não impor limites universais de 0 a 100 a todas as variáveis; grandeza, unidade e método definem as validações aplicáveis. Não transformar ausência de coleta em uma linha com zero.

Temperatura do solo/ambiente, umidade do ar e precipitação usam o mesmo formato manual quando houver coleta. Não há entidades de sensores ou comandos de equipamentos.

## Irrigação e consumo

- Cada irrigação pertence a um setor. Não exigir quatro canteiros nem limitar a cinco setores.
- O volume pertence ao evento. A associação `irrigacao_canteiro` registra atendimento, sem repetir volume ou criar automaticamente um rateio.
- Cada canteiro associado é da mesma horta e seu vínculo histórico corresponde ao setor na data local do início. A associação preserva esse vínculo após uma mudança de setor.
- Início/fim são consistentes, quando conhecidos. Duração ativa pode ser menor que a duração transcorrida devido a interrupções.
- Volume, vazão e duração conhecidos não são negativos; vazão usada para estimar um volume positivo precisa ser positiva. Valor desconhecido continua nulo.
- `forma_obtencao_volume` distingue medido/estimado; se volume for informado, registrar seu método. Estimativa `vazão × minutos ativos` só é usada quando as entradas e sua hipótese de vazão forem adequadas.
- Situação operacional pode indicar conclusão/interrupção, sem invalidar o evento. Uma irrigação interrompida com 20 L efetivamente aplicados pode contribuir com 20 L.
- Uma falha pode originar `ocorrencia`; intervenções são atividades relacionadas a essa ocorrência. O vínculo não cria eventos duplicados de irrigação.

**Exemplo:** 120 L aplicados em um evento que atende quatro canteiros são 120 L no setor. Sem distribuição medida, o modelo não afirma que cada canteiro recebeu 30 L.

## Decisões a validar

Unidades e métodos ambientais, qualidade elegível, frequência e localização da coleta, estimativa de volume, tratamento de eventos em andamento e períodos que atravessam dias. Com vigência por dia, mudanças intradiárias de setor precisam de definição específica. Consumo por canteiro e automação não são inferidos deste modelo.
