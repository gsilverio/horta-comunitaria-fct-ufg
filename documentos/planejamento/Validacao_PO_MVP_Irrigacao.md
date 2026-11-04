# Validação com a PO — MVP de irrigação

**Estado:** roteiro preparado; reunião e aceite funcional pendentes. A implementação técnica não representa aprovação das hipóteses abaixo.

## Preparação

Seguir a [execução local](../arquitetura/Execucao_Local.md) para iniciar PostgreSQL, API em modo demo e frontend. Usar somente dados fictícios; apresentar a indicação de ambiente de demonstração e o autor técnico `Operador de demonstração`, separado do responsável pelo trabalho. O [contrato do protótipo](mvp-irrigacao/Contrato_Fluxo.md) descreve as hipóteses implementadas.

Antes da apresentação, escolher uma data/recorte sem eventos ou preparar explicitamente o contexto num banco demonstrativo dedicado conforme o [Get Started](../Get_Started.md). Não há seed automático. Não apagar dados operacionais para preparar a demonstração. Os exemplos usam 08/10/2026, mas os filtros e campos de data são editáveis.

## Roteiro de demonstração

1. Selecionar a horta fictícia e S1. No formulário, selecionar B1–B4; informar início 08/10/2026 às 08:00 e fim às 08:30, em São Paulo, duração ativa de 30 minutos, volume de 120 L medido e método de obtenção. Responsável e observações são opcionais.
2. Salvar e conferir detalhe, autoria demonstrativa e canteiros. Voltar à lista com o mesmo período/setor: o consumo conhecido é 120 L para um evento, mesmo com quatro canteiros. Conferir a origem no detalhe.
3. Corrigir o registro para 100 L com motivo `Conferência da medição`. Mostrar a versão seguinte e o histórico com 120 L antes e 100 L depois. Reabrir lista/indicador e confirmar 100 L.
4. Invalidar com motivo `Registro lançado por engano`. Confirmar preservação do detalhe/histórico, ausência na lista padrão e NoData no recorte quando era o único evento. Selecionar registros invalidados para consultá-lo novamente.
5. Em um cenário separado, registrar volume desconhecido deixando o campo vazio. Mostrar indisponibilidade; combinar com um registro de volume conhecido para mostrar informação parcial. Informar 0 L com origem/método e mostrar a diferença em relação ao desconhecido. Mostrar a indicação de estimativas.
6. Demonstrar o formulário em celular e discutir campos/unidades, seleção de canteiros, filtros e motivos. Explicar que conflito de edição exige recarregar/revisar e preserva o rascunho; não ocorre sobrescrita automática.
7. Registrar respostas reais da PO na tabela abaixo, indicando ajustes e responsáveis. Não preencher respostas antecipadamente.

## Decisões e hipóteses a validar

| Decisão / hipótese | Resposta da PO | Ajuste necessário | Responsável | Estado |
| --- | --- | --- | --- | --- |
| Irrigação manual como primeiro fluxo de coleta e demonstração | Pendente | A definir após reunião | A definir | Pendente |
| Campos indispensáveis, unidades e formulário em celular | Pendente | A definir após reunião | A definir | Pendente |
| Como obter volume e distinguir medido/estimado/desconhecido/zero | Pendente | A definir após reunião | A definir | Pendente |
| Consumo conhecido soma eventos válidos uma vez, incluindo interrupções | Pendente | A definir após reunião | A definir | Pendente |
| Um setor por evento, com seleção opcional dos canteiros atendidos | Pendente | A definir após reunião | A definir | Pendente |
| Evento pertence ao dia de início, sem rateio ao atravessar dias | Pendente | A definir após reunião | A definir | Pendente |
| Fuso America/Sao_Paulo e período inclusivo de consulta | Pendente | A definir após reunião | A definir | Pendente |
| Responsável opcional separado da autoria, demonstrativa nesta entrega | Pendente | A definir após reunião | A definir | Pendente |
| Correção/invalidação com motivo, preservação do histórico e sem reativação | Pendente | A definir após reunião | A definir | Pendente |
| Filtros e apresentação de cobertura/estimativas úteis ao acompanhamento | Pendente | A definir após reunião | A definir | Pendente |
| Próximo fluxo prioritário e necessidade de identidade no pós-MVP | Pendente | A definir após reunião | A definir | Pendente |

## Registro da reunião

- Data: pendente.
- Participantes: pendentes.
- Evidências e observações: pendentes.
- Aceite, ajustes ou nova demonstração: pendentes.

Registrar aceites apenas após a reunião. Publicação, autenticação, cadastro completo e outros indicadores precisam de escopo próprio; este roteiro encerra a preparação da demonstração local da etapa 4.
