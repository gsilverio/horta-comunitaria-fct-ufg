# Modelagem preliminar dos dados da horta

Esta proposta cobre os fluxos RF01 a RF16 dos [requisitos iniciais, versão 0.0.1](../Documento_Tecnico_Requisitos_Horta.md), com base no [anexo da professora, de 30/09/2026](../Dados_e_Indicadores_Preliminares_Horta_APOEMA.pdf). Organiza os dados registrados manualmente e usados nos dashboards. Entidades e relações dependem de validação com a PO; não há implementação, schema ou migrations.

## Como ler a modelagem

- [Organização da horta](Modelo_Organizacao_Horta.md): canteiros, setores e suas configurações ao longo do tempo.
- [Cultivo e produção](Modelo_Cultivo_Producao.md): origem do material, ciclos, plantios, avaliações, colheitas e secagem.
- [Ambiente e irrigação](Modelo_Ambiente_Irrigacao.md): coletas manuais, métodos, unidades e eventos de irrigação.
- [Atividades e ocorrências](Modelo_Atividades_Ocorrencias.md): trabalho, insumos, participação social/educacional e tratamento de falhas.
- [Acesso, histórico e evidências](Modelo_Acesso_Historico_Evidencias.md): permissões, autoria, correções e arquivos relacionados.
- [Custos básicos](Modelo_Custos.md): valores previstos e realizados, com contexto opcional de cultivo.
- [Dados dos indicadores](Dados_Indicadores.md): fatos de origem, recortes, exemplos e definições pendentes.
- [Diagramas relacionais](Diagramas_Relacionais_Horta.md): visão geral e diagramas detalhados por domínio.

O [planejamento dos fluxos](../planejamento/Fluxos_Iniciais_Horta.md) descreve a sequência de uso. Cada fluxo grava entidades deste modelo; os indicadores consultam esses fatos. Uma relação entre tabelas não define, por si só, um agregado DDD.

## Convenções e decisões da proposta

Nomes em português são lógicos e destinados à discussão do negócio. A correspondência com C# e tabelas PostgreSQL está no inventário. Colunas físicas e enums seguirão as [regras do backend](../../.codex/arquitetura-backend.md), sem duplicá-las aqui.

| Notação | Significado |
| --- | --- |
| `id` | Identidade própria; UUID para entidades, salvo PK composta explicitada |
| `_id` | Referência concreta a outra entidade |
| `?` | Informação opcional/possivelmente desconhecida na proposta |
| Data | Dia de calendário, como plantio ou colheita |
| Instante | Data/hora com referência temporal, como coleta, irrigação ou autoria |
| Decimal | Grandeza com unidade explícita; escala/arredondamento ainda a acordar |
| Contagem | Inteiro não negativo, quando conhecido |

Referências essenciais para uma relação existir, como colheita → ciclo, são obrigatórias estruturalmente. Isso não torna todos os campos do formulário obrigatórios. Obrigatoriedades de coleta e limites agronômicos permanecem sujeitos à PO. Campos descritivos são textos; situações/naturezas de conjunto fixo serão enums; `ativo` é booleano. Conjuntos permitidos ainda serão definidos.

### Autoria e validade dos fatos

Plantios, avaliações, observações, colheitas, secagens, medições, irrigações, atividades, ocorrências e custos possuem os metadados abaixo. Cadastros e associações também têm alterações rastreáveis, sem exigir cópia desses campos em todo filho do mesmo registro.

| Campo comum | Finalidade |
| --- | --- |
| `registrado_por_id` → `usuario` | Quem inseriu o fato; obtido da identidade autenticada |
| `registrado_em` | Instante de inclusão, distinto da data em que o fato aconteceu |
| `situacao_registro` | Válido ou invalidado; separado da situação operacional do evento |
| `versao` | Versão esperada numa correção concorrente |

Responsável/executante, quando conhecido, referencia `participante` e pode ser diferente do usuário autor. Correções preservam a identidade do fato e registram antes/depois e motivo em `auditoria`, na mesma operação. Associações alteradas são identificadas por sua chave composta no histórico. A invalidação não apaga o fato nem libera automaticamente a remoção dos seus pais.

Se um dado usado por outro registro for invalidado, revisar a dependência: corrigir/inutilizar o filho ou impedir a invalidação que o tornaria incoerente. Não deixar uma destinação de secagem válida com colheita de origem inválida, por exemplo. A política por fluxo será confirmada antes da implementação.

### Contexto e histórico

- Fatos e cadastros locais pertencem à mesma horta, diretamente ou por referências. Não permitir vínculos entre hortas diferentes.
- Identidades de canteiro/setor são estáveis. Associação histórica fica em `vinculo_canteiro_setor`; condição física fica em `configuracao_canteiro`.
- Nesta proposta, vigências usam datas e intervalos `[início, fim)`, com fim opcional. Mudanças intradiárias são uma pendência de granularidade, não um horário artificial à meia-noite.
- Irrigação guarda seu setor; medições e canteiros atendidos preservam o vínculo histórico conhecido. Associação atual não reclassifica registros anteriores.
- Inativar cadastros referenciados preserva os fatos. Correções que afetem o passado exigem revisão explícita do histórico.
- `null` significa não informado, não zero observado. Não inventar massas, volumes, horas, origem do material ou público.

## Inventário das entidades

As 37 entidades formam uma única base. Associações e catálogos pequenos não representam módulos ou telas administrativas adicionais obrigatórias.

| Entidade lógica | Tipo C# proposto | Tabela proposta |
| --- | --- | --- |
| `horta` | `Garden` | `gardens` |
| `setor` | `IrrigationSector` | `irrigation_sectors` |
| `canteiro` | `Bed` | `beds` |
| `configuracao_canteiro` | `BedConfiguration` | `bed_configurations` |
| `vinculo_canteiro_setor` | `BedSectorAssignment` | `bed_sector_assignments` |
| `especie` | `Species` | `species` |
| `cultivar` | `Cultivar` | `cultivars` |
| `lote_propagacao` | `PropagationBatch` | `propagation_batches` |
| `ciclo_cultivo` | `CultivationCycle` | `cultivation_cycles` |
| `plantio` | `Planting` | `plantings` |
| `avaliacao_pegamento` | `EstablishmentAssessment` | `establishment_assessments` |
| `observacao_agronomica` | `AgronomicObservation` | `agronomic_observations` |
| `colheita` | `Harvest` | `harvests` |
| `lote_secagem` | `DryingBatch` | `drying_batches` |
| `item_secagem` | `DryingItem` | `drying_items` |
| `variavel_ambiental` | `EnvironmentalVariable` | `environmental_variables` |
| `ponto_coleta` | `SamplingPoint` | `sampling_points` |
| `medicao_ambiental` | `EnvironmentalMeasurement` | `environmental_measurements` |
| `irrigacao` | `Irrigation` | `irrigations` |
| `irrigacao_canteiro` | `IrrigationBed` | `irrigation_beds` |
| `atividade` | `Activity` | `activities` |
| `participante` | `Participant` | `participants` |
| `atividade_participante` | `ActivityParticipant` | `activity_participants` |
| `instituicao` | `Institution` | `institutions` |
| `atividade_instituicao` | `ActivityInstitution` | `activity_institutions` |
| `insumo` | `AgriculturalInput` | `agricultural_inputs` |
| `uso_insumo` | `InputUsage` | `input_usages` |
| `ocorrencia` | `Occurrence` | `occurrences` |
| `usuario` | `User` | `users` |
| `perfil` | `Role` | `roles` |
| `permissao` | `Permission` | `permissions` |
| `perfil_permissao` | `RolePermission` | `role_permissions` |
| `usuario_perfil` | `UserRole` | `user_roles` |
| `evidencia` | `Evidence` | `evidence` |
| `vinculo_evidencia` | `EvidenceLink` | `evidence_links` |
| `auditoria` | `AuditEntry` | `audit_entries` |
| `lancamento_financeiro` | `CostEntry` | `cost_entries` |

## Cobertura dos requisitos

| Requisito | Entidades ou relações que o sustentam |
| --- | --- |
| RF01 — Acesso | `usuario`, `perfil`, `permissao`, `perfil_permissao`, `usuario_perfil` |
| RF02 — Organização | `horta`, `setor`, `canteiro`, configurações e vínculos vigentes |
| RF03 — Espécies e origem | `especie`, `cultivar`, `lote_propagacao` e vínculo com plantio |
| RF04 — Ciclos | `ciclo_cultivo`, área ocupada e configuração de referência |
| RF05 — Plantio e pegamento | `plantio`, autorreferência de reposição, `avaliacao_pegamento` |
| RF06 — Ambiente | `variavel_ambiental`, `ponto_coleta`, `medicao_ambiental` |
| RF07 — Irrigação | `irrigacao`, `irrigacao_canteiro`, setor e vínculo histórico |
| RF08 — Agronomia | `observacao_agronomica`, ciclo e evidências |
| RF09 — Produção | `colheita`, `lote_secagem`, `item_secagem` |
| RF10 — Trabalho e insumos | `atividade`, `atividade_participante`, `insumo`, `uso_insumo` |
| RF11 — Ocorrências | `ocorrencia` e atividades de atendimento |
| RF12 — Arquivos | `evidencia`, `vinculo_evidencia` e destinos concretos |
| RF13 — Consulta e correção | Metadados dos fatos, `auditoria` e vínculos preservados |
| RF14 — Dashboards | Consultas sobre os fatos descritos em [Dados dos indicadores](Dados_Indicadores.md) |
| RF15 — Social e educação | Dimensões de `atividade`, participantes, instituições e público informado |
| RF16 — Economia básica | `lancamento_financeiro`, natureza e contexto opcional |

## Validações com a PO

Confirmar granularidade dos ciclos e áreas em consórcio, vigências, avaliações de pegamento, métodos/unidades de coleta, pesagem por lote ou origem na secagem, contagem de público, perfis e permissões. Confirmar também campos indispensáveis, fórmulas e frequência dos indicadores. Cada documento detalha suas pendências.

Dimensões e 20 canteiros/cinco setores do anexo são referências, não limites fixos nem dados automaticamente cadastrados. Compostagem, estoque de insumos, sensores, automação, contabilidade completa e cadastros acadêmicos especializados não entram nesta proposta.
