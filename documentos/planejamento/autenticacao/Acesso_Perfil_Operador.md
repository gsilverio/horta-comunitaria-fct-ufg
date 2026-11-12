# Acesso do perfil Operador

**Data:** 12/11/2026. **Base:** [plano de autenticação e autorização](Plano_Autenticacao_Autorizacao.md). A matriz é hipótese do protótipo e ainda precisa ser confirmada pela PO.

O Operador corresponde à **equipe de campo** dos requisitos: consulta todos os dados da horta e registra, corrige e invalida os fatos coletados no dia a dia. A estrutura da horta, os catálogos e os usuários são mantidos pelo perfil **Adm**.

A interface oculta as ações que o Operador não pode executar. Se uma delas for aberta por link direto, aparece o aviso "Ação restrita ao perfil Adm". A garantia está no servidor: essas rotas respondem **403** ao Operador mesmo quando chamadas fora da interface.

## Resumo

| O Operador pode | O Operador não pode |
| --- | --- |
| Entrar, trocar a própria senha e sair | Criar, editar ou desativar usuários |
| Consultar todas as hortas, cadastros, registros, históricos, fotos e indicadores | Criar, editar ou inativar hortas, setores e canteiros |
| Registrar, corrigir e invalidar fatos de campo | Configurar área de canteiro, vincular canteiro a setor ou salvar o mapa |
| Enviar, corrigir legenda e invalidar fotografias | Manter catálogos: espécies, cultivares, fases visuais, variáveis ambientais, pontos de coleta, insumos, instituições e participantes |

Corrigir e invalidar exigem motivo e preservam o histórico, como para qualquer perfil. Nenhum perfil exclui registros fisicamente.

## Por área da aplicação

| Área / página | Consultar | Registrar, corrigir e invalidar |
| --- | :---: | :---: |
| **Visão geral** (panorâmica e indicadores: água, umidade, pegamento, produtividade, perdas, secagem, horas, ocorrências, participação, custos) | ✓ | — (somente leitura para todos) |
| **Horta** › Hortas, Dados da horta, Setores, Canteiros | ✓ | ✗ (Adm) |
| **Horta** › Canteiro › Adicionar cultivo | ✓ | ✓ (registra plantio) |
| **Horta** › Mapa da horta | ✓ | ✗ para salvar o desenho (Adm); o plantio pelo mapa é permitido |
| **Irrigação** | ✓ | ✓ |
| **Ambiente** › Medições | ✓ | ✓ |
| **Ambiente** › Variáveis, Pontos | ✓ | ✗ (Adm) |
| **Cultivo** › Espécies, Cultivares (inclui fases visuais) | ✓ | ✗ (Adm) |
| **Cultivo** › Origem do material (lotes de propagação, inclusive inativar) | ✓ | ✓ |
| **Cultivo** › Ciclos (abrir, corrigir, encerrar, invalidar) | ✓ | ✓ |
| **Cultivo** › Plantios e reposições, Avaliações de pegamento, Observações | ✓ | ✓ |
| **Produção** › Colheitas, Secagens (criar, corrigir, concluir, invalidar) | ✓ | ✓ |
| **Fotos** (enviar, corrigir legenda, invalidar) | ✓ | ✓ |
| **Atividades** › Atividades | ✓ | ✓ |
| **Atividades** › Participantes, Insumos, Instituições | ✓ (inclusive para selecionar nos registros) | ✗ (Adm) |
| **Ocorrências** (abrir, mudar situação, resolver, corrigir, invalidar) | ✓ | ✓ |
| **Custos** (lançamentos) | ✓ | ✓ |
| **Usuários** | ✗ | ✗ (Adm) |
| **Conta** › Trocar senha | — | ✓ (a própria) |

Uma horta inativa continua disponível para consulta e não aceita novos registros, qualquer que seja o perfil.

## Conta e sessão

- O primeiro acesso usa a senha inicial entregue pela administração, e a troca é obrigatória antes de usar o restante da aplicação.
- A sessão fica aberta por até 7 dias sem novo login. Trocar a senha ou sair encerra as sessões em outros dispositivos.
- Após 5 senhas erradas seguidas, a conta fica bloqueada por 15 minutos.
- Ainda não há recuperação de senha. Em caso de esquecimento, a pessoa deve procurar a administração da horta.
- Os registros gravados pelo Operador ficam no histórico em nome dele, independentemente do participante indicado como responsável pelo trabalho.
- Se o Adm mudar o perfil da conta ou desativá-la, as sessões abertas são encerradas. Uma conta desativada não grava mais, mesmo antes de o token vigente expirar.
