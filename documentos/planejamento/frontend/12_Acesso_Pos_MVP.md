# FE12 — Acesso, privacidade e passagem à operação real

**Requisitos:** RF01/RNF02/RNF05. **Estado:** pós-MVP, por decisão anterior do responsável; implementação de identidade não autorizada nesta tarefa. **Fontes:** [requisitos](../../Documento_Tecnico_Requisitos_Horta.md), [contrato comum](../mvp/Contrato_Comum.md) e regras de arquitetura.

## Cobertura e limite atual

RF01 consta dos requisitos e permanece pendente; adiamento não significa entrega. A aplicação atual é demonstrativa em Development, com autor técnico resolvido pelo servidor. Participantes responsáveis não são usuários autenticados. Preparar o autor técnico manualmente não concede permissões nem implementa login.

Antes da exposição pública/uso operacional, definir provedor de identidade, perfis, alcance por horta e permissões com a PO e preparar backend correspondente. Perfis citados nos requisitos são propostas, não uma matriz aprovada. A API atual recusa inicialização fora do modo demonstrativo permitido.

## Percurso futuro a detalhar após decisões

Entrar → recuperar contexto permitido → navegar às features autorizadas → receber tratamento de sessão expirada/acesso negado → sair. O plano dessa etapa deve definir autenticação/retorno, leitura e manutenção conforme perfil, alteração de vínculo/permissão e impacto dos rascunhos na expiração. Não escolher agora biblioteca, tokens, storage de credenciais ou endpoints fictícios.

O servidor aplica todas as permissões e integridade entre hortas; esconder botão é apenas apresentação. UI distingue falha de rede, expiração de sessão e proibição conforme contrato efetivo. Histórico migra a autoria conforme decisão específica, preservando registros demonstrativos identificáveis.

## Privacidade e operação

- Dados pessoais mínimos, sem nomes/descritivos em logs ou URLs. Histórico e arquivos respeitam alcance de acesso definido.
- Testes de interface usam dados fictícios em bancos exclusivos; não expor o modo demonstrativo para contornar a etapa de acesso.
- Backup/restauração RNF05 exige procedimento operacional e verificação real do backend/banco/arquivos antes do uso operacional; não é uma tela de backup presumida.
- RNF02 inclui identidade e proteção efetivas, ainda não concluídas. Validar HTTPS, armazenamento de segredos e exposição junto ao planejamento operacional autorizado.

## Marcos posteriores

1. Registrar decisões PO/identidade e aprovar matriz de acesso e alcance por horta.
2. Entregar backend de acesso e detalhar contrato frontend; só então implementar entrada, sessão, acesso negado e navegação permitida.
3. Validar percursos por perfil e tentativas de acesso direto pelo servidor, além de privacidade dos diagnósticos.
4. Verificar restauração e preparação operacional, registrar aceite e transição de autoria antes de habilitar uso real.

Este plano mantém rastreabilidade de todos os requisitos sem antecipar implementação pós-MVP.
