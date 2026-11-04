# Revisão dos fluxos do frontend — atualizada em 09/10/2026

A revisão responde aos bloqueios de navegação da prévia. As telas de CRUD e os testes por URL direta não garantiam a jornada de quem começa sem cadastros. As regras funcionais e os cálculos continuam na API; esta revisão reorganiza a experiência e utiliza os contratos existentes.

## Problemas corrigidos

- Criar uma horta mantinha o contexto anterior (ou nenhum), obrigando a descobrir e selecionar a horta manualmente. A criação apresenta a ação Entrar na horta; a lista permite escolher explicitamente em qual horta trabalhar. Sem entrada, os menus internos ficam ocultos.
- Horta, setor e canteiro não ofereciam continuidade visível. Os detalhes agora levam ao próximo cadastro, preservando horta e setor.
- Canteiro recém-criado não estava pronto para todos os usos. Área e associação estavam escondidas. A preparação expõe cada confirmação, situação e data de vigência, com histórico separado.
- O catálogo de canteiros identifica registros; a configuração depende de uma data. O painel de preparação consulta o detalhe por data e pagina os canteiros em vez de presumir configuração a partir do catálogo.
- Formulários dependentes podiam abrir seletores vazios sem saída clara. A criação agora confere os pré-requisitos e indica os cadastros necessários antes de iniciar o rascunho. Um ciclo confere a área do canteiro na data de início escolhida.
- A visão geral abria o indicador de água. Dentro da horta, Visão geral abre um dashboard consolidado, separado da tela operacional de irrigação. A entrada da aplicação permanece na lista de hortas.

## Cronologia por requisito

| Fluxo | Sequência e orientação da interface | Dependências opcionais preservadas |
| --- | --- | --- |
| RF02 Organização | Cadastrar/escolher horta → entrar → setor → canteiro → área e associação por data → conferir preparação → campo | Dimensões desconhecidas não geram área calculada; histórico permanece acessível |
| RF03–RF05 Cultivo | Canteiro com área na data + espécie → ciclo → plantio → avaliação | Cultivar/origem desconhecidas permanecem opcionais; material não elegível não recebe pegamento inventado |
| RF06 Ambiente | Variável + ponto → coleta → consulta de umidade → origem | Ponto pode ser geral; não exige ciclo ou canteiro |
| RF07 Irrigação | Setor → registro; para selecionar canteiros, associá-los ao setor na data do evento | Canteiros atendidos e volume continuam opcionais; falta de associação oferece caminho de preparação |
| RF08 Observações | Ciclo → observação → consulta/histórico | Medida permanece opcional; RF12 segue adiado |
| RF09 Produção | Ciclo → colheita → pesagem aproveitável → destinação em lote → conclusão com massa seca → rendimento | Não inventa massa/saldo. Colheita sem pesagem oferece revisão; origem preselecionada ao iniciar secagem pelo detalhe |
| RF10/RF15 Atividades | Preparar participantes/insumos/instituições quando usados → atividade única com dimensões → participações/público → horas e participação | Atividade geral não exige canteiro; público agregado não exige lista nominal |
| RF11 Ocorrências | Abertura → atividades de providência → acompanhamento/resolução → indicador | Contexto local opcional; providência não duplica ocorrência |
| RF16 Custos | Lançamento previsto ou realizado → comparação → origem | Realizado não exige previsão, canteiro ou ciclo |
| RF13 Histórico | Detalhe → corrigir/inativar/invalidar conforme tipo → motivo/versão → histórico confirmado | Concorrência e resposta incerta preservam o fluxo de revisão existente |
| RF14 Dashboard | Horta → período → resumos por dimensão → indicador detalhado → origem → correção | Sem dados, indisponibilidade, parcialidade, zero e erro de serviço são distintos |

As áreas mostram uma sequência de links com os cadastros de apoio. As abas de ambiente e cultivo foram ordenadas pela preparação. O detalhe de ciclo oferece plantio, observação e colheita com contexto preenchido; o plantio oferece avaliação. Seletores de ciclo mostram espécie, canteiro, início e situação em português.

## Dashboard

A rota inicial e o logotipo abrem **Visão geral → Dashboard**. As dez consultas existentes compõem quatro grupos: ambiente/água, cultivo/produção, pessoas/operação e economia. Água inclui irrigação por setor, cobrindo os nove indicadores prioritários e os recortes social/econômico.

O painel mostra valores retornados pela API, unidade, competência temporal, cobertura e acesso ao detalhe. Barras com valores textuais comparam setores, situações das ocorrências e custos. Não soma páginas nem inventa média global de umidade, pegamento ou produtividade. Setores adicionais continuam no detalhamento paginado. O período se conserva entre cartões e abas dos indicadores; filtros específicos permanecem nos detalhes que os suportam.

Cada cartão carrega e trata falha independentemente. A ausência de registros aparece como Sem dados; zero registrado continua zero. Consultas possuem cancelamento e chave de horta/período para descartar respostas antigas.

## Validação

A suíte foi ampliada para testar a seleção automática ao criar uma segunda horta, pré-requisitos ausentes com navegação por cliques, dashboard integrado com zero/volume/período e recuperação de falha isolada. O percurso de banco vazio passou a usar os links reais: primeira horta → setor → canteiro → configuração/associação → irrigação → dashboard, sem fabricar URLs para pular a organização.

Os dados de teste pertencem somente ao banco PostgreSQL descartável da suíte. RF12, autenticação, dados automáticos e aceite funcional da PO não são ampliados por esta revisão. Resultados finais de verificação são registrados na execução do frontend.

## Revisão de entrada e identificação — 09/10/2026

A entrada passa a listar hortas sem selecionar automaticamente a primeira. Criar a horta leva ao detalhe com a ação Entrar na horta; somente ao entrar aparecem módulos, preparação, setores e canteiros. Todas as hortas permite sair do contexto. Os formulários não pedem código; os nomes e dados descritivos orientam as seleções. A variável de umidade possui classificação explícita para alimentar o indicador sem código especial. Esta revisão substitui o comportamento anterior de seleção automática após criação.
