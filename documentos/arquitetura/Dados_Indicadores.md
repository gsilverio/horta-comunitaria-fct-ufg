# Dados de origem dos indicadores

**Cobertura:** RF14 e os fatos de RF02–RF11, RF15 e RF16. Este documento liga os dashboards à [modelagem](Modelagem_Preliminar_Dados_Horta.md). As fórmulas abaixo são propostas de interpretação e exemplos verificáveis; o anexo e os requisitos exigem validação com a PO.

## Nove indicadores prioritários

| Indicador | Fatos e relações | Interpretação proposta e pendências |
| --- | --- | --- |
| Consumo de água | `irrigacao.volume_l` | Somar volumes conhecidos dos eventos válidos elegíveis no período. Mostrar eventos sem volume e medidos/estimados separadamente quando necessário. Validar data de competência e eventos em andamento. |
| Umidade do solo | `medicao_ambiental` → variável, ponto e contexto histórico | Mostrar histórico por ponto, unidade e método. Resumo temporal, qualidade elegível, cobertura espacial e eventual média precisam de definição; não combinar métodos incompatíveis. |
| Irrigação por setor | `irrigacao.setor_id`, início, duração e volume | Agrupar eventos/volume pelo setor registrado no fato, sem multiplicar pelos canteiros atendidos. Validar tratamento de interrupções e eventos que cruzam períodos. |
| Pegamento das mudas | `plantio` → avaliação selecionada | Proposta: sobreviventes da avaliação / quantidade daquele plantio × 100. Escolher uma avaliação elegível por plantio conforme janela/método; não somar avaliações sucessivas. Reposição permanece separada. |
| Produtividade | `colheita` → ciclo e área ocupada | Mostrar massas por espécie e, quando comparável, kg/m². Para um ciclo, massa elegível / área ocupada. Base fresca/aproveitável, período, seleção de ciclos e consórcios ainda precisam de validação. |
| Perdas | Massa fresca e aproveitável da `colheita` | Proposta de triagem: fresca − aproveitável; percentual sobre fresca quando positivo. Informações desconhecidas impedem completar esse cálculo. Validar o que constitui perda. |
| Rendimento após secagem | `lote_secagem.massa_seca_total_kg` e itens de origem | Proposta: massa seca total / soma da massa destinada ao lote × 100. Usar lote com dados completos e denominador positivo. Recorte por espécie exige origem única ou atribuição confiável. |
| Horas de trabalho | `atividade_participante.horas_trabalho` → atividade | Somar horas individuais conhecidas das atividades válidas no período. Mostrar incompletude quando houver horas não informadas; não multiplicar novamente pelo número de participantes. |
| Ocorrências operacionais | `ocorrencia` e atividades de atendimento | Contar ocorrências distintas, separando situação quando necessário. Abertura no período e estoque de ocorrências ainda abertas são recortes diferentes; validar qual apresentar. |

Não há entidade para digitar o valor final de dashboard ou um motor de fórmulas. Fatos, vínculos e definições validadas são a fonte. A opção de guardar consolidações depende de necessidade futura e da atualização correta após correções.

## Dimensões complementares

| Dimensão | Origem e cuidados |
| --- | --- |
| Social | Atividades sociais, instituições e público. Separar ações, presenças por evento, público total informado e pessoas únicas identificadas. Público agregado não comprova pessoas únicas. |
| Educacional | Atividades educacionais, objetivo/modalidade e evidências. Uma atividade social/educacional aparece nos dois recortes, conservando sua identidade no total geral. |
| Econômica | Lançamentos por natureza, período/categoria e contexto conhecido. Exibir previsto/realizado separados; não ratear despesas gerais automaticamente. |

As dimensões entram conforme prioridade e disponibilidade da coleta, sem cadastros especializados adicionais.

## Critérios comuns de consulta

1. Restringir fatos à horta autorizada, período e filtros compatíveis.
2. Considerar validade, dependências e qualidade conforme a definição aprovada.
3. Agregar o fato antes de juntar coleções filhas: horas antes de instituições/insumos, volume antes de canteiros e colheitas antes de destinações.
4. Não usar o cadastro atual para reclassificar setor/área históricos.
5. Exibir unidade, período, regra de cálculo e cobertura disponível. Cobertura de frequência esperada só existe após definir frequência e população de coleta.
6. Recalcular os resultados afetados por correções/invalidações, mantendo rastreabilidade até os fatos usados.

### Ausência, zero e dados parciais

- Sem fatos: apresentar “Sem dados”; não zero observado.
- Fatos com informação essencial desconhecida: “Indisponível” ou resultado parcial claramente indicado, conforme o indicador.
- Zero medido/informado: pode ser um resultado válido, desde que o contexto e o denominador permitam interpretá-lo.
- Soma parcial: expor, por exemplo, litros conhecidos e quantidade de eventos sem volume. Não chamar essa soma de consumo total completo.
- Denominador nulo/zero: percentual indisponível; não realizar divisão nem retornar 0% automaticamente.

### Área e percentuais agregados

Somar numeradores/denominadores comparáveis evita média simples de percentuais com bases diferentes. Para produtividade, duas colheitas do mesmo ciclo usam sua área uma vez, não uma área por colheita. Uma área física reutilizada em ciclos sucessivos é diferente da soma de áreas cultivadas por ciclo; o dashboard não pode tratá-las como o mesmo denominador. Esse recorte e as áreas em consórcio precisam de definição da PO.

## Exemplos para validar com a PO

| Situação | Resultado esperado na interpretação proposta |
| --- | --- |
| 120 L em um evento atendendo quatro canteiros | 120 L no setor, não 480 L |
| 36 sobreviventes entre 40 mudas; outra avaliação do mesmo plantio depois | 90% na avaliação escolhida, sem somar as duas contagens |
| Duas colheitas de 3 kg e 2 kg no mesmo ciclo com 5 m² | 5 kg e 1 kg/m² para esse ciclo, sem duplicar a área |
| 10 kg frescos e 8 kg aproveitáveis | 2 kg de perda de triagem; 20% se essa base for aprovada |
| 5 kg destinados a um lote e 1 kg seco total | 20% de rendimento do lote; origem mista não autoriza massa seca por espécie |
| Três participantes com duas horas individuais | Seis horas, ainda que haja várias instituições ou insumos relacionados |
| Uma falha com inspeção e reparo | Uma ocorrência e duas atividades |
| Público total informado de 20 com oito pessoas identificadas | Público total 20; pessoas únicas entre eventos dependem da parte nominal |
| R$ 90 previstos e R$ 100 realizados | Totais separados e diferença de R$ 10, para recortes compatíveis |
| Irrigação sem volume informado | Incompletude identificada; não inventar volume zero |

## Definições antes da implementação

Registrar para cada indicador aprovado: significado, fatos elegíveis, data de competência, unidade/método, fórmula, filtros, comportamento de dados parciais e frequência de coleta. Confirmar também a seleção de avaliações, área de produtividade, conclusão dos lotes, interpretação de público e categorias de despesas. Uma proposta documentada ou um exemplo conferido não substitui essa validação.
