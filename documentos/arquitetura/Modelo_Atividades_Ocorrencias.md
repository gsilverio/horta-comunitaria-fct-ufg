# Modelo de atividades, participação e ocorrências

**Cobertura:** RF10, RF11 e RF15; usa RF12/RF13 e alimenta RF14. Ver [convenções comuns](Modelagem_Preliminar_Dados_Horta.md) e [diagramas](Diagramas_Relacionais_Horta.md#atividades).

Uma atividade reúne o trabalho realizado e, quando aplicável, a participação social/educacional. Uma ocorrência identifica o problema; suas várias intervenções são atividades, sem duplicar a ocorrência.

## Entidades e informações

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `atividade` | `horta_id`, `titulo`, `categoria`, `data`, `descricao?`, `dimensao_operacional`, `dimensao_social`, `dimensao_educacional`, `objetivo?`, `modalidade?`, `publico_total_informado?`, `lista_publico_completa?`, `canteiro_id?`, `setor_id?`, `ciclo_id?`, `ocorrencia_id?` | Um evento com participantes, instituições e usos de insumos |
| `participante` | `horta_id`, `nome_exibicao`, `categoria?`, `ativo` | Pessoa envolvida; não exige conta nem documento pessoal |
| `atividade_participante` | PK composta (`atividade_id`, `participante_id`), `papel?`, `horas_trabalho?`, `compoe_publico` | Uma pessoa uma vez em cada atividade |
| `instituicao` | `nome`, `sigla?`, `tipo?`, `ativo` | Catálogo de instituições envolvidas |
| `atividade_instituicao` | PK composta (`atividade_id`, `instituicao_id`), `papel?` | Vínculo entre instituição e ação |
| `insumo` | `nome`, `categoria?`, `unidade_referencia`, `ativo` | Catálogo de insumos usados |
| `uso_insumo` | `atividade_id`, `insumo_id`, `quantidade`, `unidade`, `observacoes?` | Uso de insumo em uma atividade |
| `ocorrencia` | `horta_id`, `aberta_em`, `categoria`, `descricao`, `situacao`, `resolvida_em?`, `resolucao?`, `responsavel_participante_id?`, `canteiro_id?`, `setor_id?`, `ciclo_id?`, `irrigacao_id?` | Um problema; zero ou muitas atividades de atendimento |

Atividade/ocorrência possuem os metadados comuns dos fatos. Atividade usa data; abertura/resolução da ocorrência usam instantes. Horas e quantidades são decimais. Público é contagem inteira. Dimensões, indicação de público e completude da lista são booleanos, com completude nula quando desconhecida.

## Contexto dos registros

Proposta: cada atividade tem no máximo um contexto específico entre canteiro, setor e ciclo. Sem ele, é geral da horta. Com ciclo, o canteiro e a espécie são derivados. `ocorrencia_id` é um vínculo de atendimento adicional, da mesma horta; se a atividade também informar contexto específico, ele precisa ser compatível com a ocorrência.

Na ocorrência, no máximo um entre canteiro, setor, ciclo e irrigação é informado como contexto principal. Irrigação permite derivar o setor; ciclo permite derivar o canteiro. Não duplicar referências deriváveis com valores contraditórios. A proposta admite ocorrência geral da horta.

Participante, contexto e ocorrência de uma atividade pertencem à mesma horta. Espécies, instituições e insumos são catálogos compartilháveis, sem compartilhar automaticamente os fatos nem dados pessoais entre hortas.

## Trabalho e insumos

- Horas individuais conhecidas não são negativas; desconhecidas permanecem nulas. Não inferir que todos trabalharam a duração total da atividade.
- A PK de `atividade_participante` impede repetir a pessoa no evento. O papel é informativo, não uma permissão de acesso ao sistema.
- Horas são somadas a partir dessa associação, antes de juntar instituições ou insumos. Três pessoas com duas horas cada resultam em seis horas.
- Uso de insumo exige quantidade positiva e unidade compatível com a grandeza cadastrada. A unidade é preservada no uso para não reinterpretar fatos antigos ao editar o catálogo.
- Vários usos do mesmo insumo podem ocorrer na atividade; consolidar apenas quantidades comparáveis. Não há entrada/saída de estoque, compra ou custo automático a partir do uso.

## Participação social e educação

- Uma atividade pode ter várias dimensões, conservando sua identidade. Uma ação social e educacional aparece nos dois recortes, mas conta uma vez no total geral.
- `compoe_publico` diferencia público de executantes que só precisam constar nas horas. A definição de quem compõe esse público será validada com a PO.
- `publico_total_informado` é o total conhecido da ação, incluindo as pessoas identificadas que compõem o público; não é um número de pessoas adicionais.
- Total informado não pode ser menor que o público nominal identificado. Quando houver lista completa, as contagens devem conferir. Lista parcial sem total permite mostrar o número identificado e a incompletude, sem afirmar público total.
- Uma atividade sem lista nominal pode guardar só o total agregado. Não gerar pessoas fictícias para representá-lo.
- Participações por evento e pessoas únicas são medidas distintas. Dados agregados não identificam pessoas únicas entre eventos. Deduplicação nominal usa `participante_id`, não apenas nomes iguais.
- Objetivo, modalidade, categoria e evidências oferecem o registro básico de ensino/pesquisa/extensão. Não criar entidades de turmas, projetos acadêmicos, materiais ou avaliações institucionais nesta etapa.

**Exemplo:** uma ação tem 20 pessoas de público total informado e oito delas identificadas nominalmente. Seu público é 20, não 28. Horas de executantes continuam calculadas pelos vínculos individuais, mesmo que nem todos componham esse público.

## Ocorrências e intervenções

Situação operacional e resolução descrevem o acompanhamento. Resolução não precede abertura. Alterações preservam o histórico. Uma ocorrência pode continuar aberta enquanto recebe várias atividades de atendimento.

**Exemplo:** falha de irrigação com uma inspeção e um reparo resulta em uma ocorrência e duas atividades. Contar as atividades não transforma a falha em duas ocorrências.

## Decisões a validar

Categorias, modalidades, situações, quem compõe o público, definição de lista completa, identificação mínima das pessoas, instituições envolvidas e registro de horas parciais. Datas de atendimento e critérios de resolução devem refletir o uso real, sem acrescentar um fluxo de aprovação.
