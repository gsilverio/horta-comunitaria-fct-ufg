# Deploy com Docker

**Data:** 12/11/2026. **Destino:** esta máquina (Docker Desktop), publicada em `https://horta-comunitaria-fct-ufg.gsilverio.com.br` pelo túnel cloudflared já existente no projeto vizinho `gerenciamento-coloboradores`. A hospedagem definitiva continua a definir; este é um ambiente de demonstração para a reunião de validação.

## Componentes

Stack `horta-comunitaria-fct-ufg`, definida em [`deploy/compose.yaml`](../../deploy/compose.yaml):

| Serviço | Imagem | Papel |
| --- | --- | --- |
| `postgres` | `postgres:17.0` | Banco `horta`, volume `postgres-data`, sem porta publicada. |
| `migrate` | `deploy/api.Dockerfile`, target `migrate` | Bundle do EF Core (`efbundle`). Perfil `tools`: só roda quando chamado. |
| `api` | `deploy/api.Dockerfile`, target `api` | API .NET em `Production`, porta interna 8080. O volume `api-data` guarda `App_Data` (chaves do Data Protection e fotografias). Também executa os comandos explícitos. |
| `web` | `deploy/web.Dockerfile` | Build do Vite servido pelo nginx. Encaminha `/api/` e `/health/` à API. Porta local `127.0.0.1:5090`; alias `horta-app` na rede externa `shared-tunnel`. |

Só o `web` entra na `shared-tunnel`; banco e API ficam isolados na rede `horta-comunitaria-fct-ufg-net`. O túnel recebe HTTPS na Cloudflare e entrega `http://horta-app:8080`. Isso atende ao cookie de renovação `Secure`. Localmente, `http://127.0.0.1:5090` também funciona porque o navegador trata o loopback como origem segura.

Configuração por variáveis de ambiente (sem appsettings extra): `Persistence__ConnectionString`, `AllowedHosts` (domínio público, `localhost` e `127.0.0.1`) e `Authentication__RateLimitPerMinute=60`. Atrás do nginx e do túnel, todas as requisições chegam do mesmo IP interno; por isso o limite de autenticação passa a valer para a instância inteira e foi elevado. A API não processa `X-Forwarded-For` nesta etapa.

## Primeira subida

Na raiz do repositório. A senha do banco fica em `deploy/.env`, ignorado pelo Git (modelo em `deploy/.env.example`):

```bash
docker compose -f deploy/compose.yaml --profile tools build
docker compose -f deploy/compose.yaml up -d postgres
docker compose -f deploy/compose.yaml run --rm migrate
```

Primeiro administrador. A senha inicial existe só no ambiente do comando, e a interface exige a troca no primeiro acesso:

```powershell
$senha = Read-Host 'Senha inicial (12+ caracteres)'
docker compose -f deploy/compose.yaml run --rm -e Bootstrap__Email=adm@exemplo.org -e "Bootstrap__DisplayName=Nome da pessoa" -e "Bootstrap__Password=$senha" api --bootstrap-admin
Remove-Variable senha
```

Catálogo e cenário demonstrativo, nesta ordem:

```bash
docker compose -f deploy/compose.yaml run --rm api --seed-embrapa
docker compose -f deploy/compose.yaml run --rm api --seed-demo
docker compose -f deploy/compose.yaml up -d
```

Atualizações: `build`, `run --rm migrate` e `up -d`. Migrations nunca rodam no startup.

## Cenário demonstrativo (`--seed-demo`)

A reunião de validação pediu uma simulação com dados fictícios: cadastro do canteiro e da espécie, plantio, avaliação de sobrevivência, manejo e colheitas, até a consulta dos registros e indicadores. Também pediu atenção ao replantio, às colheitas sucessivas, ao ajuste de setores e canteiros e ao registro manual de irrigação antes dos sensores. O comando cria a horta **"Horta demonstrativa (dados fictícios)"** (código `DEMO-VALIDACAO`), com datas relativas ao dia da execução (12 semanas até hoje):

- 3 setores e 6 canteiros, com área cultivável e vigência de setor desde o início do período, além do mapa interativo salvo.
- 5 lotes de material (mudas e sementes) das espécies do catálogo Embrapa: alface BRS Leila, tomate BRS Sena, cenoura Brasília, pimentão e mostarda Gisilba.
- 6 ciclos. A alface tem replantio, duas colheitas, uma correção de pesagem com motivo, encerramento e um novo ciclo no mesmo canteiro. O tomate tem replantio, praga acompanhada e três colheitas. A mostarda tem colheitas por corte e rebrote. A cenoura é de semeadura direta, sem entrar no pegamento de mudas.
- 8 plantios (2 reposições), 7 avaliações de pegamento, observações de crescimento, germinação, florescimento e pragas, e 9 colheitas com perdas.
- 8 atividades: preparo, capina, adubação com insumo, tutoramento, mutirão social, controle de praga ligado à ocorrência e oficina educacional com escola e público informado.
- 22 irrigações manuais, com volumes estimados (regador) ou medidos (hidrômetro), e uma irrigação interrompida.
- 2 ocorrências: praga resolvida e vazamento ainda aberto, ligado à irrigação interrompida.
- Ajustes datados de hoje: o canteiro C04 passa do setor Sul ao Norte e o C06 é redimensionado. Os fatos anteriores mantêm o contexto histórico.

Os registros são autorados pelo primeiro Adm ativo e gravados pelos handlers reais, com as mesmas validações e o mesmo histórico da interface. Se a horta `DEMO-VALIDACAO` já existir, o comando não grava nada. Cada etapa confirma sua própria transação; uma falha no meio deixa a horta parcial, e a repetição passa a ser ignorada. Nesse caso, num banco só de demonstração, recrie o volume (`down -v`) e repita a primeira subida. Participantes, escola e insumos têm nomes marcados como fictícios.

## Publicação pelo túnel

O túnel `gestao-colaboradores` (compose em `gerenciamento-coloboradores`, configuração fora do Git em `deploy/cloudflared/config.yml`) recebeu a regra:

```yaml
  - hostname: horta-comunitaria-fct-ufg.gsilverio.com.br
    service: http://horta-app:8080
```

O CNAME foi criado com `cloudflared tunnel route dns gestao-colaboradores horta-comunitaria-fct-ufg.gsilverio.com.br`, seguindo o guia daquele repositório, e o container do túnel foi reiniciado. A stack da horta não depende do túnel para funcionar em `127.0.0.1:5090`.

## Verificação de 12/11/2026

- Build Release da API sem erros/avisos; as três imagens construídas.
- Migrations aplicadas pelo `migrate` em banco vazio, até `AddIdentityAuthentication`.
- `--bootstrap-admin`, `--seed-embrapa` (23 espécies, 113 cultivares) e `--seed-demo` concluídos. A segunda execução do `--seed-demo` respondeu `DemoScenarioAlreadyApplied`.
- Contagens no banco: 1 horta, 3 setores, 6 canteiros, 7 vigências, 7 configurações, 6 ciclos, 8 plantios, 7 avaliações, 9 colheitas, 8 atividades, 22 irrigações, 2 ocorrências, 1 mapa e 245 entradas de histórico.
- `/health/ready` Healthy local e pelo domínio público. Login 200 pelo nginx; no navegador, o primeiro acesso abre a troca obrigatória de senha. Os hostnames `colaboradores` e `spr-contracts` continuaram respondendo 200.
- Não validado: suíte de integração/E2E (dispensada nesta fase), navegação dos indicadores com a sessão após a troca de senha e lint do frontend.
