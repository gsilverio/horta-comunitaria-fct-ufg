# FE03 — Coletas ambientais e umidade do solo

**Requisitos:** RF06/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 03](../mvp/03_Coletas_Ambientais.md), [modelo ambiental](../../arquitetura/Modelo_Ambiente_Irrigacao.md) e [aceite comum](Comportamentos_Aceite.md). **Dependência:** organização de FE02/API 02; não exige ciclo de cultivo.

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso

Selecionar variável e ponto → registrar coleta manual → consultar medições por contexto/período → abrir detalhe/histórico → corrigir ou invalidar → conferir histórico/última observação de umidade por grupo compatível.

## Telas, campos e filtros

| Tela/seção | Conteúdo e ações |
| --- | --- |
| Variáveis ambientais | Código, nome, grandeza, unidade de referência e descrição; lista/cadastro/edição/inativação/histórico como catálogo de apoio |
| Pontos da horta | Código, descrição, localização opcional; ponto geral ou vínculo com canteiro/setor; detalhe e gestão |
| Nova medição/correção | Ponto, variável, instante, valor, unidade, método, qualidade, responsável e observações; motivo na correção |
| Lista/detalhe de medições | Período, ponto, variável, unidade/método, qualidade e validade; valor com unidade, contexto histórico e autoria |
| Umidade do solo | Histórico/última observação do recorte por ponto/unidade/método, contagens de qualidade e link à origem; série em tabela, gráfico quando útil |

Valor da medição é obrigatório: vazio não cria coleta com zero ou null. Zero coletado é preservado. Não aplicar faixa universal 0–100 a grandezas distintas. Qualidade Normal/Questionable é proposta do contrato; “Questionável” permanece visível e não significa “Invalidado”.

## Integração necessária

Todas as operações ambientais ainda são **planejadas**: catálogo de variáveis, pontos de coleta, medições com criação/lista/detalhe/correção/invalidação/histórico e soil-moisture. Integrar por etapas 03.1–03.3 e conferir enums, campos, filtros e páginas no OpenAPI real. A seleção de participantes/canteiros/setores reutiliza contexto já entregue; não inferir que GET beds da irrigação é uma lista geral de pontos.

## Estados e limites específicos

- Sem variável/ponto válido, orientar ao cadastro entregue e preservar rascunho; não inventar dispositivo, coordenadas ou local padrão.
- Mudar variável/ponto revisa unidade/contexto dependentes. Métodos/unidades diferentes aparecem separados no resultado; não converter ou fundir pelo texto parecido.
- Ponto já usado tem seu contexto protegido: conflito da API exige outro ponto/revisão, não move coletas antigas automaticamente.
- Sem coleta no recorte: Sem dados. Erro de serviço é recuperação da consulta. Última medição recebe data/qualidade e o rótulo “do período”, sem afirmar condição atual do solo.
- Temperatura/precipitação podem aparecer quando houver coleta e contrato; sensores, previsão e automação continuam fora do escopo.

## Etapas

| Etapa | Entrega | Dependência e aceite | Estado |
| --- | --- | --- | --- |
| FE03.1 | Catálogos/pontos, formulários e seleção contextual | API 03.1; ponto geral ou um contexto específico sem exigir ambos | Não iniciada |
| FE03.2 | Coleta manual, lista/detalhe, correção/invalidação/histórico | API 03.2; persistência, fuso, erros, tentativa de criação e vínculo histórico | Não iniciada |
| FE03.3 | Umidade com tabela/série e origem | API 03.3; grupos compatíveis, qualidade e última observação do recorte | Não iniciada |

## Aceite verificável

1. Duas coletas do mesmo grupo aparecem por instante; um método diferente permanece em outro grupo, sem média global.
2. Zero medido é 0; campo vazio impede salvar; ponto sem coleta é NoData. Medição Questionable é sinalizada por texto.
3. Instante informado em navegador com outro fuso é apresentado em São Paulo e confirmado com offset correto.
4. Alterar setor atual do canteiro não reclassifica contexto histórico da coleta; contexto desconhecido não é preenchido posteriormente por suposição.
5. Unidade incompatível/contexto duplo/referência de outra horta têm mensagem útil, dados preservados e nenhuma coleta parcial confirmada.
6. Correção muda a série e mantém antes/depois; invalidação remove elegibilidade; catálogos paginados e troca de ponto não repõem resposta antiga.

**Pendências da PO:** unidades/métodos, qualidade, frequência, cobertura espacial e suficiência de histórico/última medição. Nenhuma frequência, média, meta ou alerta agronômico é definido pela interface. **Evidências:** pendentes.
