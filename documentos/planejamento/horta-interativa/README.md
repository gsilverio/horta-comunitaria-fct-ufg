# Planejamento da Horta Interativa 2D

Revisão em 21/10/2026 da proposta `Planejamento_Editor_2D_Horta_Interativa.md`, versão 1.0, recebida pelo usuário. O documento recebido foi tratado como material de análise; suas instruções de implementação foram recortadas para o experimento local E1 descrito aqui.

**Foco confirmado pelo usuário:** mudar inicialmente apenas o modo interativo. Irrigação, sensores e outras integrações operacionais não entram agora.

## Por onde começar

Leia a [análise de coerência e prontidão](Analise_Coerencia.md) e o [roteiro da experimentação inicial](Experimento_Inicial.md). A base foi usada para implementar e avaliar o primeiro incremento local; o editor já lê a horta, os setores e os canteiros reais, mas ainda não persiste o layout.

| Etapa | O que a pessoa conseguirá fazer | Prioridade atual |
| --- | --- | --- |
| [0 — Levantamento](00_Levantamento.md) | Entender o que aproveitar e as decisões visuais pendentes | Auditoria técnica realizada; decisões de produto pendentes |
| [1 — Canvas navegável](01_Canvas_Navegavel.md) | Ver terreno, selecionar/mover um canteiro, aproximar e deslocar a vista | Primeiro experimento |
| [1.5 — Projeção do CRUD](09_Integracao_Projecao_CRUD.md) | Projetar a área da horta e agrupar canteiros pelos setores cadastrados | Implementado como leitura integrada; layout ainda local |
| [2 — Formas simples](02_Formas_Simples.md) | Criar retângulos/círculos e ajustar medidas por campos | Implementado no E1 |
| [3 — Formas livres e curvas](03_Formas_Livres_Curvas.md) | Editar polígonos e segmentos de anel | Após validar formas simples |
| [4 — Mandala](04_Mandala.md) | Gerar e ajustar um desenho parametrizado | Gerador local inicial implementado; setores curvos futuros |
| [5 — Precisão e histórico local](05_Precisao_Historico.md) | Alinhar, desfazer e identificar problemas do desenho | Recorte local implementado no E1; regras espaciais completas futuras |
| [6 — Persistência](06_Persistencia_Futura.md) | Salvar e recuperar layout no servidor | Futura; fora da primeira experiência |
| [7 — Navegação e integração](07_Navegacao_Futura.md) | Entrar/sair do modo visual; futuramente ligar aos cadastros | Só navegação básica agora |
| [8 — Validação](08_Validacao.md) | Avaliar se o editor é compreensível e mantém medidas | Transversal desde a etapa 1 |

Cada etapa preserva as tarefas e critérios da proposta recebida em uma seção identificada, precedida do recorte adequado ao projeto. Itens originais sobre API, operação e permissões não fazem parte do experimento atual.

## Marcos menores que o MVP original

1. **E0 — Experiência navegável:** etapa 0 técnica + etapa 1 + entrada/saída da tela + validação básica.
2. **E1 — Editor local:** etapa 2 + gerador local de mandala, desfazer/descarte, snap e conflitos visuais do recorte da etapa 5. Implementado; falta avaliação com usuário.
3. **E1.5 — Projeção do CRUD:** leitura da área da horta, agrupamento horta → setor → canteiro e projeção proporcional das dimensões disponíveis. Implementado como leitura; falta persistência espacial.
3. **E2 — Geometrias avançadas:** polígonos e setores de anel verdadeiros da etapa 3, além de precisão complementar.
4. **Futuro — Layout persistido:** novo recorte das etapas 6–8, sujeito a decisão posterior. Não inclui automaticamente irrigação ou sensores.

O MVP original de etapas 0–7 é amplo demais para responder à pergunta inicial sobre interação. E0 já permite observar essa mudança na aplicação; E1 permite experimentar criação e edição.

## Referências de projeto

- [Cultivos no panorama](../../arquitetura/Cultivos_no_Panorama.md): incremento autorizado em 24/10/2026 com registros reais, prazos visuais configurados, sprites por grupo/cultivar e faixas de densidade. Layout segue local; esta leitura não introduz sensores ou automação.

- [Regras do frontend](../../../.codex/arquitetura-frontend.mdc) e [backend](../../../.codex/arquitetura-backend.md), fontes únicas das regras arquiteturais.
- [Requisitos](../../Documento_Tecnico_Requisitos_Horta.md) e [modelagem preliminar](../../arquitetura/Modelagem_Preliminar_Dados_Horta.md).
- [Situação documentada do frontend](../Execucao_Frontend_MVP.md).
