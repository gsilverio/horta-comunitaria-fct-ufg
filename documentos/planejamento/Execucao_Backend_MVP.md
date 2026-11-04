# Execução do backend restante do MVP

Em 08/10/2026, o responsável autorizou executar o backend pendente dos requisitos e planos existentes. O alcance desta tarefa compreende os fluxos 02–09, reutilizando a irrigação já entregue. Interfaces não fazem parte desta execução. RF01 continua pós-MVP; RF12 foi explicitamente adiado pelo responsável. As hipóteses funcionais dos planos são comportamentos do protótipo, sujeitos ao aceite da PO.

| Fluxo | Estado do backend nesta execução | Verificação |
| --- | --- | --- |
| 02 — Organização | Cadastros, versões, históricos, vigências de área/setor e extensão do consumo implementados | Códigos, no-op, versões, sobreposição no banco, concorrência e proteção de irrigações/coletas/ciclos |
| 03 — Coletas ambientais | Variáveis/pontos, registro, gestão/histórico e umidade implementados | Zero conhecido, qualidade Questionable, contexto histórico, reenvio e ponto usado não deslocável |
| 04 — Cultivo e pegamento | Espécies/cultivares/origens, ciclos/fechamento, plantios/reposições, avaliações/observações e gestão implementados | Área congelada, datas/quantidades, dependentes, 36/40 = 90%, ponderação 41/50 = 82%, última avaliação e material desconhecido |
| 05 — Colheita e secagem | Colheitas, lotes/origens, conclusão, saldo, gestão/histórico, produtividade/perdas/rendimento implementados | Área usada uma vez; aproveitável null; concorrência 3 + 3 kg contra saldo 4; invalidação libera saldo; lote misto sem atribuição por espécie |
| 06 — Atividades e horas | Participantes/insumos, agregado Activity, gestão/histórico e horas implementados | 3 × 2 h = 6 h com vários usos/instituições, horas null/zero, troca de filhos/no-op/stale version, rollback por falha da auditoria |
| 07 — Ocorrências | Abertura, acompanhamento, resolução, gestão/histórico, intervenções e indicador implementados | Resolução preservada, dependência da atividade impede invalidação, ocorrência não multiplicada por intervenções |
| 08 — Social/educação | Dimensões/público na mesma Activity, instituições e indicador implementados | Ação multidimensional conta uma vez; público total inclui nominal; pessoas identificadas distintas por ID |
| 09 — Custos | Registro previsto/realizado, gestão/histórico, totais e origens implementados | 90/100 → diferença 10; correção 95 → diferença 5; null/zero, centavos, reenvio, invalidação e resumo independente da página |
| 10 — Evidências | Adiado por decisão do responsável | Nenhum endpoint ou armazenamento implementado |

Fatos possuem operação idempotente, autoria no servidor, versão/validade e auditoria na mesma transação, incluindo os filhos. Cadastros possuem criação, edição, inativação e histórico. Consultas usam projeção/paginação e snapshots consistentes; logs Serilog identificam processo, etapa, operação/recurso, versão e correlação HTTP.

## Verificação técnica em 08/10/2026

- Build Release final: **zero erros e avisos**.
- **152 casos distintos do backend com resultado positivo** ao longo das verificações. A regressão completa inicial teve 148/150; duas expectativas antigas foram ajustadas para o esquema ampliado. A repetição dos casos afetados teve **28/28**, incluindo dois novos cenários. Não é alegado um único comando completo de 152/152.
- Um teste antigo esperava somente os quatro enums da irrigação: agora confere os onze enums nativos. Outro teste de FK também gerava sobreposição de vigência: passou a usar canteiro sem vigência para isolar a FK. A exclusão de períodos continua testada separadamente.
- Regressão da interface de irrigação com API/PostgreSQL reais: **12/12 Playwright**, incluindo versão, reenvio, histórico, celular e fuso. A limpeza da fixture foi adaptada às novas FKs; nenhum fluxo de produto do frontend foi implementado nesta tarefa.
- SQL idempotente revisado; EF CLI confirmou **nenhuma alteração pendente no modelo**. A evolução aplicada não remove tabelas, colunas ou dados.
- Migrations geradas pelo EF CLI: `CatalogManagement`, `MvpManualRecords`, `MvpIntegrityChecks`. Aplicadas também ao banco local `horta_demo` na porta 5432: cinco migrations no total, cadastros existentes preservados e nenhuma carga automática de fatos.
- Inicialização real temporária: readiness saudável e OpenAPI com os novos comandos, incluindo POST de colheita. Instância de verificação encerrada; servidor PostgreSQL preservado. Fixtures usam bancos exclusivos e fazem a limpeza ao terminar.

Após configurar as conexões locais conforme o [Get Started](../Get_Started.md), os comandos na raiz são:

```powershell
dotnet build HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --no-restore
dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release
dotnet ef migrations has-pending-model-changes --project HortaComunitaria.Api --startup-project HortaComunitaria.Api
```

Para repetir a verificação focada:

```powershell
dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release --filter 'FullyQualifiedName~Mvp|FullyQualifiedName~CatalogWriteTests|FullyQualifiedName~PersistenceIntegrityTests|FullyQualifiedName~IrrigationContractTests'
```

Consulte o [contrato implementado](../arquitetura/Contrato_Backend_MVP.md) para rotas, filtros, retornos e decisões de persistência; o [Get Started](../Get_Started.md) para criar o contexto e percorrer as features por HTTP. Interfaces dos fluxos 02–09, RF12 e autenticação continuam pendentes nos respectivos planos. Aceite técnico não substitui aprovação das hipóteses pela PO.
