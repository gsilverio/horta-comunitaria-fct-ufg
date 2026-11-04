# Get Started — executar e testar a aplicação

Este guia descreve o código disponível hoje. O backend possui criação e gestão de organização, ambiente, cultivo, colheita/secagem, atividades, ocorrências, participação social/educacional e custos, com histórico e dez consultas de indicadores. O fluxo de organização da interface foi revisado, incluindo cultivo e plantio a partir do canteiro, e o menu mantém os demais módulos; Swagger/HTTP continuam disponíveis para conferir os contratos. RF12 foi adiado pelo responsável e RF01 continua pós-MVP. Consulte o [contrato implementado](arquitetura/Contrato_Backend_MVP.md) e o [planejamento do frontend](planejamento/frontend/README.md).

## Como a aplicação funciona

```mermaid
flowchart LR
    Pessoa[Interface React + Tailwind: 5173] --> Proxy[Proxy Vite /api]
    Proxy --> API[Minimal APIs .NET: 5163]
    API --> DB[PostgreSQL 17: 5432]
    API --> Logs[Serilog: console estruturado]
```

A interface envia comandos à API. O servidor valida horta/setor/vínculos de canteiros na data, resolve a autoria, grava em PostgreSQL e calcula os indicadores. Corrigir ou invalidar preserva histórico. Uma irrigação de 120 L atendendo quatro canteiros consome 120 L no total. Volume desconhecido permanece desconhecido; zero informado é uma medida explícita.

A aplicação atual usa autoria técnica `Operador de demonstração`, separada do participante responsável pelo trabalho. Login e permissões RF01 estão previstos para pós-MVP. O modo atual funciona somente em Development com habilitação explícita.

## 1. Pré-requisitos e configuração

Na raiz do repositório, use SDK .NET **10.0.301**, Node **24.15.0**, npm **11.12.1** e PostgreSQL **17**. Confira `dotnet --version`, `node --version` e `npm --version`. As dependências estão fixadas nos arquivos de versão/lock.

Nesta máquina, o banco `horta_demo` já foi criado no PostgreSQL local acessível em `127.0.0.1:5432`. A conexão está no arquivo local ignorado `HortaComunitaria.Api/appsettings.Development.json`. O backend usa apenas o `appsettings.json` normal e o arquivo de Development; variáveis de ambiente podem sobrescrever valores. Não colocar conexão PostgreSQL em variáveis do frontend.

Em outra instalação, crie um banco dedicado chamado `horta_demo` pelo pgAdmin ou outro cliente PostgreSQL e prepare `appsettings.Development.json`:

```json
{
  "DemoMode": { "Enabled": true },
  "Observability": { "ConsoleFormat": "Text" },
  "Persistence": {
    "ApplyMigrationsOnStartup": true,
    "ConnectionString": "Host=127.0.0.1;Port=5432;Database=horta_demo;Username=SEU_USUARIO;Password=SUA_SENHA_LOCAL"
  }
}
```

Use sua credencial local, sem versioná-la. Não há seed: iniciar a aplicação ou aplicar migrations **não cria cadastros nem registros**. As migrations criam/evoluem o esquema. Os dados que já existiam no banco são preservados.

## 2. Iniciar o backend

No primeiro terminal, na raiz:

```powershell
dotnet restore HortaComunitaria.Api/HortaComunitaria.Api.slnx --locked-mode
dotnet tool restore
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet ef database update --project HortaComunitaria.Api --startup-project HortaComunitaria.Api
dotnet run --project HortaComunitaria.Api --launch-profile http
```

Com migrations automáticas habilitadas, o startup também confere o esquema; o comando EF permite aplicá-lo explicitamente antes. O banco precisa existir e a conta precisa ter permissão para as migrations.

- [Readiness](http://localhost:5163/health/ready): deve responder 200 com banco acessível; não comprova que existem cadastros.
- [Swagger](http://localhost:5163/swagger/index.html): operações realmente disponíveis e seus contratos.
- [OpenAPI](http://localhost:5163/openapi/v1.json): descrição HTTP da implementação atual.
- [Liveness](http://localhost:5163/health/live): verifica que o processo responde.

## 3. Preparar autoria e escolher como criar os cadastros

Não há seed. Um banco novo permanece vazio após migrations. O autor demonstrativo ativo `demo:operator` precisa ser preparado explicitamente uma vez, porque RF01 ainda não tem cadastro/login. Em pgAdmin conectado ao banco dedicado, confira `users`; se ele não existir, execute:

```sql
INSERT INTO users ("Id", "AuthenticationIdentifier", "DisplayName", "IsActive")
VALUES ('01a11c0e-0d4c-7928-ab6b-fd88c78ae086', 'demo:operator', 'Operador de demonstração', true)
ON CONFLICT ("AuthenticationIdentifier") DO NOTHING;
```

Se já existir, confira se está ativo. Esse usuário técnico não tem senha e não representa o participante responsável. Todos os cadastros de negócio são criados pela interface ou por HTTP, com histórico. Para seguir somente pela interface, após preparar o autor vá à seção 4 e use o roteiro da seção 5; os exemplos HTTP abaixo são uma alternativa. Crie uma horta separada para cada roteiro independente; não execute como carga automática da aplicação.

No Swagger, crie horta → setor → canteiro → área e associação → participante. Ou execute explicitamente no PowerShell, com a API iniciada:

```powershell
$api = 'http://localhost:5163/api/v1'
function Post-Horta([string]$path, [object]$body) {
  Invoke-RestMethod -Method Post -Uri "$api$path" -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 12)
}
$horta = Post-Horta '/gardens' @{ name = 'Horta de teste' }
$prefixo = "/gardens/$($horta.id)"
$setor = Post-Horta "$prefixo/sectors" @{ name = 'Setor 1' }
$canteiro = Post-Horta "$prefixo/beds" @{ name = 'Canteiro 1' }
$area = Post-Horta "$prefixo/beds/$($canteiro.id)/configurations" @{
  cultivableAreaM2 = 5; validFrom = '2026-10-01'; expectedBedVersion = 1
}
$vinculo = Post-Horta "$prefixo/beds/$($canteiro.id)/sector-assignments" @{
  sectorId = $setor.id; validFrom = '2026-10-01'; expectedBedVersion = $area.bedVersion
}
$pessoa = Post-Horta "$prefixo/participants" @{ displayName = 'Participante de teste' }
```

Cada contexto muda a versão do canteiro; por isso a associação usa a versão retornada pela configuração. Inicial histórico é permitido; sucessões usam hoje ou uma data futura e motivo, sem reclassificar fatos existentes. Consulte `/beds/catalog` para administração e `/beds?sectorId=ID&on=DATA` para seleção de irrigação.

### Testar os outros fluxos do backend

O roteiro seguinte usa os IDs retornados acima. As datas são exemplos coerentes; escolha um período próprio se já houver outros registros. `operationId` identifica uma tentativa; conserve o mesmo valor ao repetir um envio incerto.

```powershell
# Ambiente: variável → ponto → coleta manual.
$variavel = Post-Horta '/environmental-variables' @{
  isSoilMoisture = $true; name = 'Umidade do solo'; quantity = 'Umidade'; referenceUnit = '%'
}
$ponto = Post-Horta "$prefixo/sampling-points" @{
  description = 'Ponto do canteiro'; bedId = $canteiro.id
}
$coleta = Post-Horta "$prefixo/environmental-measurements" @{
  operationId = [guid]::NewGuid(); samplingPointId = $ponto.id; variableId = $variavel.id
  collectedAt = '2026-10-08T08:00:00-03:00'; value = 30; unit = '%'; method = 'Manual'; quality = 'Normal'
}
# Cultivo: espécie → origem → ciclo → plantio → avaliação/observação.
$especie = Post-Horta '/species' @{ commonName = 'Alface' }
$origem = Post-Horta "$prefixo/propagation-lots" @{
  speciesId = $especie.id; materialType = 'Seedling'; origin = 'Doação'
}
$ciclo = Post-Horta "$prefixo/cultivation-cycles" @{
  operationId = [guid]::NewGuid(); bedId = $canteiro.id; speciesId = $especie.id
  startedOn = '2026-10-01'; occupiedAreaM2 = 5
}
$plantio = Post-Horta "$prefixo/plantings" @{
  operationId = [guid]::NewGuid(); cycleId = $ciclo.id; propagationLotId = $origem.id
  plantedOn = '2026-10-02'; kind = 'Initial'; plantedQuantity = 40
}
$avaliacao = Post-Horta "$prefixo/establishment-assessments" @{
  operationId = [guid]::NewGuid(); plantingId = $plantio.id
  assessedOn = '2026-10-03'; survivingQuantity = 36; method = 'Contagem'
}
$observacao = Post-Horta "$prefixo/agronomic-observations" @{
  operationId = [guid]::NewGuid(); cycleId = $ciclo.id; observedOn = '2026-10-04'
  category = 'Acompanhamento'; description = 'Folhas com manchas, diagnóstico pendente'
}
# Produção: colheita → destinação → pesagem final.
$colheita = Post-Horta "$prefixo/harvests" @{
  operationId = [guid]::NewGuid(); cycleId = $ciclo.id
  harvestedOn = '2026-10-05'; freshMassKg = 10; usableMassKg = 8
}
$secagem = Post-Horta "$prefixo/drying-lots" @{
  operationId = [guid]::NewGuid(); startedOn = '2026-10-06'
  items = @(@{ harvestId = $colheita.id; allocatedMassKg = 5 })
}
$conclusao = Post-Horta "$prefixo/drying-lots/$($secagem.id)/completion" @{
  endedOn = '2026-10-08'; totalDryMassKg = 1; expectedVersion = 1; reason = 'Pesagem final'
}
# Ocorrência e atividade são fatos separados, vinculados pela intervenção.
$ocorrencia = Post-Horta "$prefixo/occurrences" @{
  operationId = [guid]::NewGuid(); openedAt = '2026-10-08T07:00:00-03:00'
  category = 'Manutenção'; description = 'Revisar o setor'; sectorId = $setor.id
}
$insumo = Post-Horta '/inputs' @{ name = 'Adubo'; referenceUnit = 'kg' }
$instituicao = Post-Horta '/institutions' @{ name = 'Instituição de teste' }
$atividade = Post-Horta "$prefixo/activities" @{
  operationId = [guid]::NewGuid(); title = 'Inspeção orientada'; category = 'Manutenção'; performedOn = '2026-10-08'
  sectorId = $setor.id; occurrenceId = $ocorrencia.id; isOperational = $true; isSocial = $true; isEducational = $true
  reportedAudienceTotal = 20; audienceListComplete = $false
  participants = @(@{ participantId = $pessoa.id; workHours = 2; countsAsAudience = $true })
  inputUsages = @(@{ inputId = $insumo.id; quantity = 1; unit = 'kg' })
  institutions = @(@{ institutionId = $instituicao.id; role = 'Apoio' })
}
$resolucao = Post-Horta "$prefixo/occurrences/$($ocorrencia.id)/resolution" @{
  resolvedAt = '2026-10-08T09:00:00-03:00'; resolution = 'Inspeção concluída'; expectedVersion = 1; reason = 'Encerrar acompanhamento'
}
# Custos: natureza informada explicitamente, sem custo gerado por insumo.
$previsto = Post-Horta "$prefixo/financial-entries" @{
  operationId = [guid]::NewGuid(); entryDate = '2026-10-08'; description = 'Previsão'; category = 'Manutenção'; amountBrl = 90; nature = 'Planned'
}
$realizado = Post-Horta "$prefixo/financial-entries" @{
  operationId = [guid]::NewGuid(); entryDate = '2026-10-08'; description = 'Compra'; category = 'Manutenção'; amountBrl = 100; nature = 'Actual'
}
```

Se a variável de umidade já estiver cadastrada, obtenha seu ID por GET em vez de cadastrá-la novamente. Cultivar é opcional: POST `/species/{id}/cultivars` e use seu ID na origem/ciclo quando conhecido. Listas de filhos de atividade são obrigatórias; envie `[]` quando não houver informações, sem pessoas fictícias.

Consulte os indicadores do [contrato](arquitetura/Contrato_Backend_MVP.md) com `?from=2026-10-01&to=2026-10-31`. Para o roteiro isolado: pegamento 90%; produtividade 2 kg/m²; perda 2 kg/20%; secagem 20%; horas 2 h; uma ocorrência resolvida; uma ação social e educacional, público 20 e uma pessoa identificada; custo previsto 90/realizado 100/diferença 10. Umidade apresenta a observação e sua qualidade, sem média global.

### Conferir correção, invalidação e histórico

No Swagger, use GET do registro para conferir campos e versão. PUT recebe somente campos editáveis do contrato, com opcionais explicitamente null, `expectedVersion` atual e `reason`. Não copiar cegamente todo o GET: ele também contém metadados do servidor. Experimente custo 100 → 95 e confira diferença 5 e histórico 100 → 95. POST `/{id}/invalidation` recebe versão/motivo; GET `/{id}/history` preserva as versões.

Tente invalidar a colheita com secagem válida: deve responder 409, sem alteração parcial. Invalidar o lote de secagem libera seu saldo; GET da colheita mostra massa destinada e saldo restante. Teste duas edições com a mesma versão: somente a primeira confirma. Reenvie um POST de fato com mesmo operationId/payload: deve retornar 200 sem duplicar; conteúdo diferente com a mesma chave retorna 409.

## 4. Iniciar o frontend

No segundo terminal, na raiz:

```powershell
cd hortacomunitaria-webapp
npm ci
npm run dev
```

Abra [a aplicação](http://127.0.0.1:5173). O proxy padrão aponta à API em `127.0.0.1:5163`. Não é necessário configurar CORS para esse percurso local. O [README do frontend](../hortacomunitaria-webapp/README.md) explica as variáveis para alterar portas/destino.

## 5. Organizar a horta pela interface

A organização da horta segue **Hortas → setores da horta → canteiros do setor**. O menu lateral continua dando acesso a todos os módulos. O caminho no alto da tela identifica onde você está e permite voltar a qualquer nível. O logotipo sempre retorna à lista de hortas.

1. Em **Hortas**, clique em **Nova horta**. Informe o nome e, se souber, a localização. As áreas da horta são opcionais e ficam em uma seção recolhida.
2. **Cadastrar horta** abre diretamente os setores dessa horta. Use **Novo setor**, informe o nome e uma descrição opcional.
3. **Cadastrar setor** abre os canteiros desse setor. Use **Novo canteiro** e informe nome e localização opcional.
4. Clique em **Continuar**. A identificação já estará salva. Informe a área cultivável, confirme o setor e a data de início; depois use **Concluir cadastro**.
5. O detalhe confirma a área e o setor e mostra **O que está plantado aqui?**. Use **Adicionar cultivo** para escolher a planta, o início e a área ocupada. Se a planta não estiver no catálogo, **Cadastrar espécie** permite incluí-la e continuar sem perder o rascunho.
6. Após cadastrar o cultivo, registre a data e a quantidade efetivamente plantada ou use **Voltar ao canteiro** para registrar depois. Os cultivos e plantios ficam visíveis no próprio canteiro. O caminho no topo retorna ao setor ou à horta.

As listas usam o mesmo padrão: nome, informação de apoio e uma ação específica (**Ver setores**, **Ver canteiros**, **Ver canteiro**). A opção **Mostrar** permite consultar ativos, inativos ou todos onde houver catálogo administrativo. Paginação e retorno pelo navegador mantêm o contexto.

**Dados da horta**, no final da lista de setores, dá acesso à edição e ao histórico. No setor, **Dados e manutenção do setor** reúne essas ações. No detalhe do canteiro, alterações históricas de área/setor e auditoria ficam recolhidas; a informação principal é a organização atual.

Se a conclusão de um canteiro for interrompida, use **Todos os canteiros e cadastros pendentes** na horta, abra o canteiro e escolha **Completar cadastro**. A retomada confere os dados persistidos antes de gravar o que falta. Uma resposta incerta na criação da identificação exige consultar o catálogo antes de criar novamente.

O menu mantém acompanhamento, organização e trabalho de campo. Organização aparece acima de No campo. A marca fica integrada ao menu lateral; no celular, junto ao botão que abre o menu. Os demais percursos seguem disponíveis conforme o [planejamento completo](planejamento/frontend/README.md).

## 6. Disponibilidade atual

| Área | Interface atual |
| --- | --- |
| Hortas | Lista, cadastro, edição, inativação e histórico |
| Setores | Lista dentro da horta, cadastro, edição, inativação e histórico |
| Canteiros | Lista dentro do setor, cadastro em duas etapas, área/setor por data e histórico; catálogo geral para pendências e inativos |
| Cultivo pelo canteiro | Espécie, início e área; registro de plantio com contexto fixo, quantidade e data; retorno ao canteiro |
| Demais módulos e indicadores | Disponíveis pelo menu, com seus contratos e fluxos existentes |
| Login/permissões e arquivos | Permanecem fora deste recorte |

## 7. Executar verificações automáticas

Backend, na raiz:

```powershell
dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release
```

Sem configuração adicional, os testes usam Testcontainers e precisam de Docker. Para usar PostgreSQL 17 local, defina `HORTA_TEST_POSTGRES` com uma conexão a `postgres`, em conta com permissão de criar bancos. A fixture cria bancos `horta_test_<uuid>`, prepara explicitamente os dados de cada caso e descarta esses bancos ao terminar. Não aponta ao banco da aplicação. Há teste de inicialização que verifica que o banco permanece sem cadastros.

Exemplo para testar com a instância local, na raiz:

```powershell
$env:HORTA_TEST_POSTGRES = 'Host=127.0.0.1;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA_LOCAL'
dotnet test HortaComunitaria.Api/HortaComunitaria.Api.slnx -c Release
```

Substitua a credencial local e mantenha-a fora do versionamento. A limpeza tem prazo maior para acomodar checkpoints do PostgreSQL no Windows; a suíte pode levar alguns minutos.

Frontend, em `hortacomunitaria-webapp`:

```powershell
npm run build
npm run lint
npx playwright install chromium
$env:HORTA_E2E_POSTGRES_URL = 'postgres://USUARIO:SENHA@127.0.0.1:5432/postgres'
npm run test:e2e
```

Substitua a credencial local; caracteres especiais na URL precisam ser codificados. E2E usa banco exclusivo `horta_e2e_<uuid>`, aplica migrations, verifica ausência de dados automáticos e prepara fixtures somente nesse banco. Inicia seus próprios processos API 5279/preview 5183 e os encerra, descartando o banco. Essas portas precisam estar livres. Resultados ficam em `test-results/` e `playwright-report/`, ignorados pelo Git.

## 8. Diagnóstico rápido

| Sintoma | Conferência |
| --- | --- |
| API recusa startup | Development, DemoMode habilitado, conexão local configurada; verificar erro estruturado no terminal |
| Readiness 503 | Serviço PostgreSQL, porta, credencial e banco acessíveis |
| Nenhuma horta | `GET /gardens` e cadastros ativos; migrations não inserem dados |
| Nenhum canteiro | Setor correto e associação vigente no dia; não basta existir o canteiro |
| Consulta funciona, mas comando falha | Conferir autor técnico ativo `demo:operator` e erro/traceId; não basta ter participante |
| Frontend não consulta API | API 5163 ativa e proxy Vite no destino correto; conferir mensagem e traceId |
| Consumo diferente do exemplo | Mesmo dia/setor, somente válidos, outros registros no recorte e volume conhecido |

Logs Serilog preservam propriedades estruturadas; Development usa console legível. Use `Module`, `ProcessName`, `ProcessId`, `StepName`, `Phase` e `TraceId` para acompanhar uma operação; `EventName`, `Outcome` e `SourceContext` identificam evento, resultado e classe. Para saída JSON, selecione `Observability:ConsoleFormat=Json` no appsettings ou por variável de ambiente. Confira [observabilidade](arquitetura/Observabilidade.md) para exemplos. Encerre API e frontend com Ctrl+C nos terminais respectivos.

### Identificação dos cadastros (09/10/2026)

A interface não pede códigos para hortas, setores, canteiros, variáveis, pontos ou lotes. A API aceita `code` omitido; novos cadastros usam o UUID como referência técnica legada, e a secagem usa a chave de operação para preservar retries. Correções sem `code` preservam o valor anterior. Os nomes não viram chaves únicas. Lotes são apresentados pela origem/tipo/data ou método/data.

A migration `ExplicitSoilMoistureVariable` acrescenta `IsSoilMoisture` e preserva a classificação das variáveis antigas de código `soil_moisture`. Aplique as migrations antes de usar a versão nova da API. Não há seed nem remoção de registros.
