# FE01 — Irrigação manual e consumo de água

**Requisitos:** RF07; RF02/RF13/RF14 no recorte de irrigação. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [contrato real do fluxo](../mvp-irrigacao/Contrato_Fluxo.md), [etapa de interface](../mvp-irrigacao/Etapa_04_Interface_PO.md), [plano funcional 01](../mvp/01_Irrigacao.md) e [aceite comum](Comportamentos_Aceite.md).

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## O que já está entregue

Selecionar horta/setor → buscar canteiros elegíveis pelo dia local → registrar irrigação → consultar lista/consumo → detalhar/corrigir/invalidar → conferir histórico. A feature existente `src/features/irrigation` usa API/PostgreSQL reais, Tailwind e autoria demonstrativa. As evidências da etapa 4 registram 12 testes; não refazer o fluxo ou usar sua conclusão para declarar o MVP completo.

| Tela/estado existente | Campos, filtros e ações |
| --- | --- |
| Lista e consumo | Período inclusivo, setor, validade da lista; páginas de registros e setores; abrir origem |
| Registro/correção | Setor/canteiros, início/fim em São Paulo, situação, duração ativa, volume/origem/método, vazão, responsável e observações; motivo na correção |
| Detalhe | Valores confirmados, contexto, responsável separado da autoria, versão e datas; corrigir ou invalidar válido |
| Histórico | Autor/motivo, operação e valores/vínculos antes/depois; paginação |
| Recuperação | Erros por campo, resposta incerta, leitura após gravação e 409 com rascunho/revisão |

API disponível: consultas de contexto, POST/lista/detalhe/PUT de irrigações, invalidação/histórico e water-consumption. O consumo responde volume/cobertura e eventos por setor; ainda não entrega contagens por situação e duração do [fluxo funcional 02.3](../mvp/02_Organizacao_Horta.md).

## Expansão planejada

| Etapa | Trabalho | Aceite | Estado |
| --- | --- | --- | --- |
| FE01.1 | Preservar registro/gestão/consumo existentes | Percurso 120 → 100 → Invalid/NoData, histórico e retry | Concluída tecnicamente, em 08/10/2026 |
| FE01.2 | Integrar às rotas/contexto de FE00; acesso pela visão geral | Abrir origem com recorte efetivo, reload/voltar e regressão | Não iniciada |
| FE01.3 | Integrar novos cadastros/vigências e irrigação por setor | Seleção por dia, leitura histórica e resumo sem reclassificação | Aguardar FE02/API 02.2–02.3 |

## Regressões obrigatórias nas próximas entregas

1. 120 L para quatro canteiros permanece 120 L; paginação não muda resumo.
2. Volume vazio, zero e estimativa continuam diferentes. Interrupted não desaparece do consumo elegível.
3. Correção para 100 L mantém 120 no histórico; invalidação preserva detalhe e remove elegibilidade.
4. Reenvio após perda da resposta mantém chave/conteúdo e um fato; falha de leitura após gravação não induz nova criação.
5. Conflito preserva rascunho; atualização de horário não depende do fuso do browser; correção idêntica preserva precisão temporal.
6. Canteiro transferido de setor continua identificado no passado; escolha futura segue a nova vigência, quando essa API existir.

**PO:** [roteiro preparado](../Validacao_PO_MVP_Irrigacao.md), reunião/aceite pendentes. **Limite:** irrigação por setor completa pertence a FE02.3; não acrescentar outros indicadores calculados pelo cliente.
