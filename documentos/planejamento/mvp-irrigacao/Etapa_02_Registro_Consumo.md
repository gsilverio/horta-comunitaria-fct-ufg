# Etapa 2 — Registrar irrigação e consultar consumo

**Estado:** concluída tecnicamente em 08/10/2026. **Dependência:** etapa 1 confirmada pelo código e pela suíte de regressão. Hipóteses funcionais do protótipo continuam sujeitas à PO.

**Leitura obrigatória:** AGENTS.md, [arquitetura integral](../../../.codex/arquitetura-backend.md), [contrato](Contrato_Fluxo.md) e documentação de execução produzida na etapa 1.

## Resultado e limite

Registrar uma irrigação manual, persistir seu contexto e inclusão no histórico, consultar o fato e obter o consumo conhecido pela API. Não implementar edição, invalidação ou interface ainda.

## Sequência de trabalho

1. Criar `Domain/Irrigation` com Irrigation e IrrigationBed, os enums persistidos do contrato e métodos/factories que encapsulem invariantes locais. Criar AuditEntry para os metadados de inclusão; o registro não recebe autor do request. Separar regras locais do evento das consultas a cadastros/vigências, que o handler coordena.
2. Criar configurações EF e migration gerada: decimais, instantes UTC, ENUMs nativos, token Version, índice único GardenId/OperationId, PK composta dos canteiros e FKs para cadastros/vínculos/autoria. `CreationPayload` e snapshots podem ser jsonb, com serialização fora do domínio e normalização definida pelo contrato. Restrição de exclusão preserva fatos e histórico.
3. Implementar `Features/Irrigation/RegisterIrrigation` com CommandHandler/Endpoints. Validar request, resolver a horta, setor, responsável e vínculos históricos. Guardar evento, filhos e histórico Created em um SaveChanges atômico. Tratar repetição e disputa da chave de operação conforme contrato, sem criar outra inclusão. Não criar repositories ou serviço genérico de CRUD.
4. Implementar ListIrrigations e GetIrrigation com QueryHandler/Endpoints. Projetar DTOs e consultar filhos apenas para os IDs da página/detalhe. Não carregar todo o domínio nem fazer uma consulta por canteiro. Detalhe mostra autoria demonstrativa, volume desconhecido e contexto histórico.
5. Implementar `Features/Indicators/GetWaterConsumption` com QueryHandler/Endpoints. Calcular resumo, cobertura e página de setores a partir do mesmo snapshot. Somar pelo evento principal, não pelo join com canteiros. As quatro situações de disponibilidade e Estimated são obrigatórias desde esta etapa.
6. Atualizar OpenAPI e Execucao_Local.md com exemplos de registro e consulta, configuração dos ENUMs e migrations. Conferir que todos os endpoints aparecem nos respectivos registradores; Program continua composição.

## Verificações de aceite

- Matriz de resultados da seção 5 do contrato, até os cenários de correção/invalidação que pertencem à etapa 3. Exemplos separados e dados isolados.
- 120 L para quatro canteiros permanece 120 L; paginação de bySector não altera o resumo; S2 sem eventos aparece NoData. Setor filtrado tem resumo apenas desse setor.
- Datas nas bordas locais são incluídas corretamente. Um evento cruzando meia-noite pertence ao dia do início. Filtro invertido é 400.
- Volume/duração/vazão negativos, fim anterior, duração ativa excessiva, enum numérico/indefinido, precisão excessiva e volume conhecido sem método são rejeitados sem gravação parcial.
- Rejeitar IDs de outra horta, canteiro repetido, vínculo fora de vigência e canteiro de outro setor. Testar dois intervalos sequenciais: mudança posterior de setor não reclassifica evento antigo. Não criar endpoint de mudança para fazer esse teste.
- Repetição sequencial e duas criações simultâneas com mesma chave/payload geram um evento e uma inclusão. Chave igual com payload diferente dá 409. Repetição de evento legítimo com chave nova é permitida.
- Falha na gravação do histórico não deixa o fato confirmado. Testar rollback em PostgreSQL real por falha controlada de persistência, sem adicionar endpoint de teste.
- Build Release sem warnings, testes afetados aprovados, migration e OpenAPI revisados. Conferir conteúdo persistido e contagens, além dos status.

## Entrega ao próximo agente

Atualizar [evidências](README.md#acompanhamento), exemplos HTTP e limitações reais. Os slices de leitura e escrita devem funcionar sem frontend. Parar aqui; não antecipar a etapa 3.

## Evidências da execução

- Slices RegisterIrrigation, ListIrrigations, GetIrrigation e GetWaterConsumption registrados por domínio; autoria resolvida no servidor e invariantes em factories/value object. Não há endpoints de correção, invalidação ou histórico, nem frontend.
- Migration `20261008162052_IrrigationRegistration` e snapshot gerados pelo EF CLI, SQL revisado: três tabelas novas, quatro ENUMs nativos, numeric(14,3), jsonb, versão concorrente, índice único de operação e FKs RESTRICT. Setor não integra a chave alternativa do fato para manter o contexto corrigível na próxima etapa.
- Restore da solução com `--locked-mode`; build Release da solução sem erros/warnings; `dotnet ef migrations has-pending-model-changes --configuration Release --no-build` confirmou correspondência do modelo/snapshot.
- **84 testes de integração aprovados**, sem testes ignorados, via `dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --no-build --no-restore`, usando PostgreSQL 17.0 real e bancos isolados. Incluem os 29 cenários anteriores e 55 novos cenários de registro, consultas, consumo, contratos, persistência e logs.
- Matriz de disponibilidade completa; 120 L para quatro canteiros permanece 120 L; zero observado; estimativas/interrupção; resumo independente da página e filtro por setor; setores sem eventos e setor inativo com fato válido. Fixtures de validade verificam as leituras sem antecipar a API de invalidação.
- Bordas locais, evento cruzando meia-noite, ordenação estável, filtros inválidos/números em enums, precisão e medidas inválidas, referências de outra horta, canteiros repetidos/incompatíveis e dois intervalos sequenciais com mudança de setor preservando vínculos antigos.
- Retry normalizado e disputa simultânea da mesma operação: um fato/uma inclusão; conteúdo diferente resulta em 409; chave nova permite outro evento. Trigger de teste que falha na inserção da auditoria comprovou rollback de fato e filhos no PostgreSQL. Interceptores de teste coordenaram a concorrência e a gravação durante o snapshot do indicador, sem sleeps.
- Smoke em host HTTP real no loopback, banco vazio com migration/seed opt-in: criação 201, retry 200 com mesmo id, conflito 409 com código e trace ID coerentes, detalhe com quatro canteiros, listagem com um evento, consumo de 120 L e S2=NoData mesmo paginando. Banco confirmou um fato, quatro filhos e uma auditoria. OpenAPI e health checks responderam 200; logs JSON de comandos/consultas/HTTP foram conferidos sem exceções ou payloads.
- [Execução e decisões de persistência](../../arquitetura/Execucao_Local.md), [catálogo de logs](../../arquitetura/Observabilidade.md) e [exemplos HTTP](../../../HortaComunitaria.Api/HortaComunitaria.Api.http) atualizados. O workaround do cache XML do OpenAPI preserva a geração do XML e os contratos HTTP explícitos, conferidos nos testes.

Limitação concreta: Docker estava sem engine ativa; Testcontainers/compose não foram exercitados nesta máquina. Os mesmos mapeamentos/migrations foram verificados em PostgreSQL 17.0 nativo, com cluster temporário isolado. Testes não constituem aceite funcional da PO. Próxima entrega prevista: etapa 3, após autorização própria.
