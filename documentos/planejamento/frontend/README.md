# Frontend do MVP — planejamento e execução

**Data:** 08/10/2026. **Estado:** FE00–FE09 e FE11 implementados tecnicamente nesta prévia. RF12 permanece adiado por decisão do responsável; RF01 permanece pós-MVP. As definições funcionais e o aceite da PO continuam pendentes.

A interface usa React/TypeScript, Vite e Tailwind e consome a API real. A navegação parte da horta e percorre cadastros → registros → indicadores → origem → correção/invalidação e histórico. Não há seed de negócio nem fórmulas calculadas pelo navegador.

Consulte o [Get Started](../../Get_Started.md), a [execução do frontend](../Execucao_Frontend_MVP.md), o [contrato do backend](../../arquitetura/Contrato_Backend_MVP.md), os [requisitos](../../Documento_Tecnico_Requisitos_Horta.md) e os [comportamentos de aceite](Comportamentos_Aceite.md). As regras técnicas permanecem na [arquitetura do frontend](../../../.codex/arquitetura-frontend.mdc).

## Entregas

| Plano | Requisitos | Interface disponível |
| --- | --- | --- |
| [FE00 — Navegação/contexto](00_Navegacao_Contexto.md) | RNF04; RF02–RF16 | Áreas, horta na URL, filtros, acesso direto, voltar/avançar e primeira horta |
| [FE01 — Irrigação](01_Irrigacao.md) | RF07/RF13/RF14 | Registro, correção, invalidação, histórico, consumo e situação/duração por setor |
| [FE02 — Organização](02_Organizacao_Horta.md) | RF02/RF13 | Hortas, setores, canteiros e consulta/criação de vigências de área e setor |
| [FE03 — Ambiente](03_Coletas_Ambientais.md) | RF06/RF13/RF14 | Variáveis, pontos, coleta manual, histórico e umidade |
| [FE04 — Cultivo](04_Cultivo_Pegamento.md) | RF03–RF05/RF08/RF13/RF14 | Espécies, cultivares, material de origem, ciclos, plantios/reposições, avaliações e observações |
| [FE05 — Produção](05_Producao_Secagem.md) | RF09/RF13/RF14 | Colheitas, saldo/destinações, secagem/finalização e três indicadores |
| [FE06 — Atividades](06_Atividades_Horas.md) | RF10/RF13/RF14 | Participantes, insumos, instituições, atividades com participações e horas |
| [FE07 — Ocorrências](07_Ocorrencias.md) | RF11/RF13/RF14 | Abertura, acompanhamento, providência como atividade, resolução e contagem |
| [FE08 — Social/educação](08_Social_Educacao.md) | RF15/RF13/RF14 | Mesma atividade de FE06, finalidade/público/instituições e consulta sem duplicar atividades |
| [FE09 — Custos](09_Custos.md) | RF16/RF13/RF14 | Previstos/realizados, contextos opcionais, correção, invalidação e comparação |
| [FE10 — Evidências](10_Evidencias.md) | RF12/RF13 | Pendente por decisão do responsável; não há upload nesta entrega |
| [FE11 — Indicadores](11_Dashboards_Indicadores.md) | RF14/RF13 | Visão geral com consultas reais, cobertura, grupos e links às origens |
| [FE12 — Acesso](12_Acesso_Pos_MVP.md) | RF01/RNF02/RNF05 | Pós-MVP; autoria técnica explícita na demonstração |

Os formulários de cadastro permitem criação, edição, inativação e histórico. Registros de fatos permitem criação, correção completa com versão/motivo, invalidação e histórico. Encerramento de ciclo, conclusão da secagem e transição/resolução de ocorrência são ações próprias, conforme o contrato da API.

## Indicadores

| Indicador | Origem e apresentação |
| --- | --- |
| Consumo de água | Litros conhecidos, cobertura e estimativas; sem multiplicar por canteiros |
| Irrigação por setor | Litros, situações e duração com cobertura própria, na mesma consulta de consumo |
| Umidade do solo | Coletas por ponto/unidade/método e qualidade, com grupos separados |
| Pegamento | Plantios elegíveis e última avaliação, base/cobertura e grupos |
| Produtividade | Massa fresca e área preservada por ciclo |
| Perdas | Massas conhecidas e cobertura da massa aproveitável |
| Rendimento após secagem | Destinações efetivas e pesagem final, sem rateio criado no frontend |
| Horas de trabalho | Horas individuais conhecidas e participações sem horas |
| Ocorrências operacionais | Ocorrências distintas por categoria/situação |
| Participação social/educacional | Atividades distintas, presenças nominais, pessoas identificadas e público informado |
| Custos básicos | Valores previstos/realizados e comparação do recorte |

As dez consultas HTTP incluem água e irrigação por setor juntas. Os nove indicadores prioritários e os dois recortes adicionais estão integrados. Ausência, indisponibilidade, cobertura parcial e valores conhecidos são apresentados conforme a API; zero não substitui ausência de coleta.

## Verificação e aceite

Build estrito, lint, testes com navegador/API/PostgreSQL real e revisão visual são registrados em [Execução do frontend](../Execucao_Frontend_MVP.md). Fixtures existem somente no banco descartável dos testes. Não representam carga inicial da aplicação ou aprovação da PO.

Os planos individuais preservam o percurso, as hipóteses e os cenários de aceite. RF12, autenticação, coleta automática, sensores, Grafana e implantação de produção não fazem parte desta prévia.

## Revisão de usabilidade

A [reestruturação visual da entrada e gestão da horta](Reestruturacao_Visual_Horta.md), planejada em 04/11/2026, delimita a próxima revisão à homepage, navegação e telas tradicionais de hortas, setores e canteiros. Define padrões responsivos com Tailwind e preserva integralmente o cadastro interativo e a renderização panorâmica. A implementação foi aplicada às telas tradicionais após o push da base. Build e lint aprovados; integração dispensada pelo responsável e revisão visual completa pendente.

A [revisão dos fluxos](../Revisao_Fluxos_Frontend_MVP.md) corrige seleção automática da horta, preparação física, orientação de dependências e entrada pelo dashboard consolidado. A existência das telas anteriores não representava conclusão da jornada por navegação; os testes foram ampliados para esse percurso.
