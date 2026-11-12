# Plano de autenticação e autorização

**Data:** 12/11/2026. **Escopo:** encerra o modo demonstrativo sem autenticação e inicia a etapa pós-MVP de identidade (RF01), seguindo o padrão adotado no projeto de gestão de colaboradores: ASP.NET Core Identity com bearer token nativo, refresh token em cookie HttpOnly e troca obrigatória da senha inicial. **Fora do escopo:** recuperação/redefinição de senha, cadastro público, perfil de consulta e concessões por horta.

## Decisões

| Tema | Decisão |
| --- | --- |
| Provedor | ASP.NET Core Identity (`AddIdentityCore` + `SignInManager`), sem `MapIdentityApi` e sem JWT. Access e refresh token são tickets do esquema bearer nativo, protegidos pelo Data Protection com chaves persistidas em `App_Data/keys`. |
| Sessão | Access token de 15 min no corpo da resposta, guardado só em memória no navegador e enviado em `Authorization: Bearer`. Refresh token de 7 dias apenas em cookie `__Secure-horta.refresh` (`HttpOnly`, `Secure`, `SameSite=Strict`, caminho `/api/v1/auth`), rotacionado a cada uso. |
| Revogação | Security stamp: logout, troca de senha, mudança de perfil e desativação invalidam os refresh tokens. O access token vigente expira em até 15 min; gravações conferem no banco se o autor continua ativo. |
| Perfis | Dois perfis fixos, um por usuário: `Administrator` (Adm) e `Operator` (Operador). Guardados na coluna `Profile` (ENUM PostgreSQL `user_profile`) e emitidos como claim de papel; sem tabelas de papéis/permissões, pois não há perfis configuráveis nesta etapa. |
| Autorização | Fallback policy exige usuário autenticado em toda rota. Policy `Administrator` aplicada por endpoint nas ações administrativas. Exceções anônimas: login, refresh, logout, health checks, OpenAPI. |
| Primeiro Adm | Comando explícito `--bootstrap-admin`, no mesmo padrão do `--seed-embrapa`: lê `Bootstrap__Email`, `Bootstrap__DisplayName` e `Bootstrap__Password` do ambiente, só cria se não houver Adm ativo e exige troca de senha no primeiro acesso. Não roda no startup nem em migrations. |
| Demais usuários | Criados pelo Adm na tela **Usuários**, com senha inicial que deve ser trocada no primeiro acesso. O Adm altera nome, perfil e situação; não redefine senhas (sem fluxo de reset nesta etapa). |
| Proteções | Lockout após 5 falhas por 15 min; resposta genérica no login; senha com 12+ caracteres, 4 distintos, sem senhas comuns ou nome/e-mail; rate limit de 10 req/min por IP nas rotas de autenticação; `Cache-Control: no-store` nas respostas de autenticação. |
| Autoria | `DemoAuthorContext` é substituído por `CurrentAuthor`, que resolve o autor pela claim do usuário autenticado. O usuário técnico `demo:operator` permanece apenas como autor histórico (`IsDemonstration = true`), sem credenciais. |

## Matriz de acesso

Ambos os perfis consultam todos os dados, históricos e indicadores.

| Ação | Adm | Operador |
| --- | :---: | :---: |
| Registrar, corrigir e invalidar fatos de campo: irrigação, lotes de propagação, ciclos, plantios, avaliações de pegamento, observações, medições, colheitas, secagem, atividades, ocorrências, lançamentos financeiros e fotografias | ✓ | ✓ |
| Manter estrutura: hortas, setores, canteiros, configurações, vínculos com setor e mapa | ✓ | — |
| Manter catálogos: espécies, cultivares, crescimento visual, variáveis ambientais, pontos de amostragem, insumos, instituições e participantes | ✓ | — |
| Gerenciar usuários | ✓ | — |
| Trocar a própria senha e sair | ✓ | ✓ |

A matriz segue os perfis propostos nos requisitos (Administração mantém cadastros e acessos; Equipe de campo registra dados). Hipótese do protótipo; confirmar com a PO antes de uso operacional. O detalhamento por página está em [Acesso do perfil Operador](Acesso_Perfil_Operador.md).

## Backend

1. Pacote `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.9.
2. `Infrastructure/Identity/`: `AppUser : IdentityUser<Guid>` (substitui `Domain/Identity/User`), `UserProfile`, opções, `AuthTokenService`, cookie de refresh, claims, política de senha, filtro de troca pendente, `CurrentAuthor`, extensões de registro e comando `--bootstrap-admin`.
3. `AppDbContext : IdentityUserContext<AppUser, Guid>`; tabela `users` evolui por migration aditiva `AddIdentityAuthentication` (colunas do Identity, `Profile`, `MustChangePassword`, `IsDemonstration`, `CreatedAt`; tabelas `user_claims`, `user_logins`, `user_tokens`). A coluna `AuthenticationIdentifier` é convertida em `IsDemonstration` antes de ser removida.
4. Slices `Features/Auth/` (login, refresh, logout, usuário atual, troca de senha) e `Features/Users/` (listar, criar, alterar; somente Adm). Alterações de usuário entram no histórico com autoria.
5. `RequireAdministrator()` nos endpoints administrativos da matriz. Remoção de `DemoMode`.
6. OpenAPI com esquema Bearer; 401/403 em `ProblemDetails`.

## Frontend

1. `src/services/session.ts`: token em memória, renovação única compartilhada; `http.ts` anexa o token, renova uma vez em 401 e encerra a sessão se falhar.
2. `features/auth`: provedor de sessão, página **Entrar**, página **Trocar senha** (obrigatória com senha inicial), menu da conta no cabeçalho com nome, perfil, "Trocar senha" e "Sair".
3. `features/users`: lista e formulário de usuários, visível só para Adm.
4. Ocultação das ações administrativas para o Operador (o servidor continua sendo a garantia); 403 apresenta mensagem compreensível.
5. Imagens protegidas (fotografias e catálogo) carregadas pelo cliente HTTP autenticado.

## Validação desta etapa

Build Release da API, `npm run build` e `npm run lint`. Testes de integração/E2E dispensados nesta fase pelo responsável; os testes existentes que dependem do modo demonstrativo precisam ser adaptados à autenticação antes de voltarem a rodar. Não aplicar migrations nem criar usuários automaticamente para conferir telas.
