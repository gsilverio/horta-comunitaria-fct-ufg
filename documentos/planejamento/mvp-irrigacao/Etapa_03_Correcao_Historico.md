# Etapa 3 — Corrigir, invalidar e preservar histórico

**Estado:** concluída tecnicamente em 08/10/2026. **Dependência:** etapa 2 implementada e verificada. Validação funcional da PO pendente.

**Leitura obrigatória:** AGENTS.md, [arquitetura integral](../../../.codex/arquitetura-backend.md), [contrato](Contrato_Fluxo.md) e documentação de execução existente.

## Resultado e limite

Corrigir ou invalidar uma irrigação com motivo, versão esperada e histórico atômico. O indicador deve refletir imediatamente o estado confirmado do fato. Sem exclusão, reativação, edição de cadastros ou API genérica de auditoria.

## Sequência de trabalho

1. Adicionar métodos de domínio de correção e invalidação, sem setters públicos. Correção reaplica as invariantes locais; versão de concorrência deve avançar uma vez por alteração efetiva. Motivo vazio não é aceito. Se o payload normalizado for igual ao estado atual, retornar 200 com o estado existente, sem novo histórico/versão, desde que a versão esperada confira.
2. Implementar `Features/Irrigation/CorrectIrrigation` com CommandHandler/Endpoints e PUT definido no contrato. Conferir versão e validade, resolver novamente relações alteradas e preservar antes/depois, inclusive filhos. Manter RecordedAt/RecordedByUserId e CreationPayload originais.
3. Implementar InvalidateIrrigation com CommandHandler/Endpoints. Invalidação preserva evento/filhos, marca Invalid e grava motivo, autor e instante. Repetição com versão antiga é 409; registro já invalidado com versão atual também é 409. Não usar exclusão física ou cascata.
4. Confirmar mudança do fato e histórico no mesmo SaveChanges/transação. Mapear DbUpdateConcurrencyException especificamente para `irrigation.version_conflict`; não repetir a alteração automaticamente. Versão enviada pelo cliente não é a nova versão a persistir.
5. Implementar ListIrrigationHistory com QueryHandler/Endpoints, filtrado por horta e irrigação. Resposta paginada contém Created/Corrected/Invalidated, motivos, instantes, antes/depois e autoria demonstrativa. Histórico não tem rota de escrita. Consultas existentes devem respeitar validade e estado corrigido.

## Verificações de aceite

- Sequência 120 L → correção para 100 L → invalidação: versões 1/2/3; totais 120/100/NoData; três entradas, sem somar snapshots. Cadastro, autor e instante originais preservados.
- Corrigir volume null para conhecido e vice-versa atualiza disponibilidade e cobertura. Corrigir início para outro dia ou setor altera os filtros de consumo e guarda ambos os contextos no histórico.
- Motivo vazio, dado inválido ou vínculo incompatível não altera fato, versão nem histórico.
- Duas alterações concorrentes partindo da mesma versão: só uma confirma; a outra recebe 409. Há um incremento e uma entrada de alteração, sem sobrescrita silenciosa.
- Falha na escrita do histórico reverte também fato e filhos. Correção sem mudança com versão correta não cria entrada; versão desatualizada é conflito.
- Após corrigir, reenviar a criação original com a mesma operationId retorna o evento atual sem duplicar; comparar com CreationPayload original, não com o volume corrigido.
- Invalidado continua no detalhe, na consulta com filtro Invalid e no histórico; sai da listagem default e do consumo. Referência de outra horta é 404.
- Build Release sem warnings, testes PostgreSQL aprovados e OpenAPI atualizado. Nenhuma dependência de autenticação para esses cenários do protótipo.

## Entrega ao próximo agente

Registrar [evidências](README.md#acompanhamento) e exemplos de PUT/invalidação/conflito. A API está pronta para o percurso da interface. Parar aqui, sem iniciar React ou outros indicadores.

## Evidências da execução

- `dotnet restore HortaComunitaria.Api/HortaComunitaria.Api.slnx --locked-mode`: aprovado.
- `dotnet build HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --no-restore`: aprovado, zero warnings/erros.
- `dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --no-build --no-restore`: **126 aprovados, zero falhas/ignorados**, com PostgreSQL 17.0 nativo em cluster temporário isolado. Resultado local ignorado pelo Git: `tests/HortaComunitaria.Api.IntegrationTests/TestResults/stage3-verified.trx`.
- `dotnet ef migrations has-pending-model-changes --project HortaComunitaria.Api --startup-project HortaComunitaria.Api --configuration Release --no-build`: nenhuma mudança. Campos, enum de operações e FKs necessários já constavam da migration anterior; não foi criada migration vazia.
- A suíte cobre versões 1/2/3, antes/depois e autoria, normalização sem alteração, precisão temporal, volume conhecido/desconhecido/zero/estimado, correção de data/setor e vínculos, paginação, campos obrigatórios, enums e isolamento entre hortas. Falha controlada ao inserir auditoria reverte fato e filhos. Disputas entre duas correções e entre correção/invalidação confirmam um vencedor e um 409; alteração durante correção sem mudança também é conflito. Detalhe e histórico têm leituras consistentes.
- Host HTTP real iniciado com migration/seed opt-in em banco vazio descartável: criação 201; correção 200/versão 2; formulário igual 200 sem incremento; versão antiga 409 com code/traceId; invalidação 200/versão 3; histórico com três entradas; retry do POST original retornando versão 3 invalidada. Consumo 120 L → 100 L → NoData. OpenAPI respondeu 200 e o console Serilog manteve processos e correlação sem exceções.

Os [exemplos HTTP](../../../HortaComunitaria.Api/HortaComunitaria.Api.http), a [execução local](../../arquitetura/Execucao_Local.md) e a [observabilidade](../../arquitetura/Observabilidade.md) foram atualizados. API e cluster temporários foram encerrados após a verificação. Docker/compose continuam sem verificação de ciclo de vida nesta máquina, pois a engine estava indisponível. Nenhum frontend ou outro indicador foi implementado.
