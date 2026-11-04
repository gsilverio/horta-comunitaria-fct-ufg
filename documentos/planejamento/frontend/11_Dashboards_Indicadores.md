# FE11 — Visão geral, indicadores e navegação à origem

**Requisitos:** RF14/RF13. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [dados dos indicadores](../../arquitetura/Dados_Indicadores.md), [índice e cobertura](README.md), planos de cada domínio e [aceite comum](Comportamentos_Aceite.md).

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso e apresentação

Selecionar horta → escolher indicador e recorte suportado → ler resultado/definição/cobertura → consultar lista de fatos de origem → abrir detalhe → corrigir quando permitido → retornar ao mesmo recorte atualizado.

Entregar consultas incrementalmente, habilitando só indicadores efetivamente disponíveis. Cada apresentação inclui nome, unidade, período com semântica, filtros aplicados, origem, definição/fórmula validada ou hipótese identificada e cobertura. NoData, Unavailable, Partial e Available têm explicações próprias conforme API; desconhecido não é zero.

| Indicador | Unidade/semântica proposta | Origem e cuidado de apresentação |
| --- | --- | --- |
| Consumo de água | Litros conhecidos; período de início da irrigação | Irrigações válidas; quantidade sem volume, estimativas e cobertura; litros não multiplicam por canteiros |
| Umidade do solo | Unidade da variável/método; período de coleta | Medições por ponto, método e qualidade; grupos incompatíveis separados, sem média global inventada |
| Irrigação por setor | Litros, eventos/situações e duração conhecida | Mesmos eventos; duração e classificação dependem da extensão API 02.3 |
| Pegamento | Percentual com base observada e cobertura | Plantios de mudas elegíveis e avaliações; última avaliação elegível, sem somar avaliações do mesmo plantio |
| Produtividade | kg/m² conforme área de referência e período | Colheitas/ciclos; área contada segundo contrato, sem repetição por colheita |
| Perdas | kg e percentual quando calculável | Massas fresca/aproveitável; aproveitável desconhecida não é perda total |
| Rendimento após secagem | Percentual e massas de origem/resultante | Destinações efetivas e massa seca; lote em andamento não tem rendimento final |
| Horas | Horas individuais conhecidas | Participações de atividades válidas; não multiplicar por joins de insumos/instituições |
| Ocorrências | Quantidade de identidades distintas | Aberturas no recorte; situação atual/pendências identificadas separadamente |

Fórmulas e recortes propostos precisam da validação funcional indicada em cada plano. Não fazer agregações/fórmulas de negócio no browser nem extrair resumo de uma única página de registros. Paginação de origens não altera o total do recorte. Gráficos são opcionais quando esclarecem comparação/tendência, sempre acompanhados de valores e tabela acessível; nenhuma biblioteca adicional é decidida nesta tarefa.

## Filtros e retorno

- Horta é contexto comum; setor, canteiro, ciclo, espécie, método e outras dimensões só aparecem onde a API os suporta.
- Período por instantes de coleta/início, data de colheita ou competência financeira tem significado próprio; informar qual data compõe cada resultado.
- Navegar à origem leva o recorte compatível e preserva retorno/paginação. Não prometer que um filtro universal vale para todos os indicadores.
- Ao corrigir/inutilizar origem, recarregar o indicador por consulta real; resultado de consulta anterior não substitui o recorte atual.
- Resumos social/educacional e econômico complementam os nove prioritários, com unidades/cobertura próprios.

## Etapas e aceite

| Etapa | Entrega | Dependência |
| --- | --- | --- |
| FE11.1 | Compor consumo existente na navegação FE00, sem regressão | API existente; FE00 |
| FE11.2 | Umidade e irrigação por setor completos | API 03.3/02.3; FE03/FE02 |
| FE11.3 | Pegamento, produtividade, perdas e rendimento | API 04.4/05.2/05.4; FE04/FE05 |
| FE11.4 | Horas/ocorrências e resumos social/custos entregues | API 06.3/07.3/08.3/09.3; respectivos fluxos |

Verificar os exemplos de cálculo de cada plano por API/PostgreSQL reais; comparar resultado e fatos de origem no mesmo recorte. Em todo indicador, exercitar ausência de fatos, dados parcialmente conhecidos, zero observado quando permitido, paginação, alteração/invalidação e volta ao contexto. Registrar divergências de definição para PO; não ajustar fórmula no cliente para fechar exemplos.
