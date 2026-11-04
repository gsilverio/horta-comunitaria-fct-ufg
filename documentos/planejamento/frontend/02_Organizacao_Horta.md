# FE02 — Horta, setores, canteiros e vigências

**Requisitos:** RF02/RF13; irrigação por setor de RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 02](../mvp/02_Organizacao_Horta.md), [modelo de organização](../../arquitetura/Modelo_Organizacao_Horta.md) e [aceite comum](Comportamentos_Aceite.md).

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso

Selecionar/cadastrar horta → cadastrar setor/canteiro → consultar detalhe do canteiro em uma data → configurar área e associação por vigência → registrar fatos no contexto correto → conferir histórico e irrigação por setor. Seletores de contexto e consultas administrativas são separados; ambos estão disponíveis.

## Telas e ações

| Tela/seção | Dados, filtros e ações |
| --- | --- |
| Hortas | Lista por identificação/situação; formulário de código, nome, localização e áreas conhecidas; detalhe, edição, inativação e histórico |
| Setores da horta | Código, nome, descrição opcional; lista, criação, detalhe, edição, inativação e histórico |
| Catálogo de canteiros | Código, nome/localização e situação; lista administrativa, criar, abrir detalhe, editar/inativar com motivo/versão |
| Configuração do canteiro | Data de início, dimensões conhecidas e área cultivável; tabela de configurações passada/vigente/futura; consultar data e criar sucessora |
| Associação de setor | Setor e início de vigência; tabela de vínculos e motivo quando exigido; revisar mudança antes de confirmar |
| Irrigação por setor | Período/setor, litros conhecidos, contagens concluídas/interrompidas, duração conhecida e sem duração; abrir irrigações do recorte |

Quantidades/dimensões não são fixadas em 20 canteiros/cinco setores ou no contexto de teste. Não calcular área efetiva automaticamente a partir de comprimento/largura. Inativação preserva consultas históricas; não oferecer exclusão física como manutenção. A criação da primeira horta fica acessível sem depender de um cadastro preexistente, permitindo substituir o preparo manual temporário.

## Integração e disponibilidade

| Necessidade | Backend correspondente | Situação atual |
| --- | --- | --- |
| Seletores de horta/setor/canteiros elegíveis | Consultas do fluxo 01 | Disponíveis para irrigação |
| Criar/editar/detalhar/inativar cadastros e seu histórico; catálogo administrativo de canteiros | 02.1 | Disponível no backend e integrado à interface |
| Consultar/configurar áreas e associações por data, proteção de dependentes | 02.2 | Disponível no backend e integrado à interface |
| Contagens/duração e cobertura por setor | 02.3 — extensão de water-consumption | Disponível no backend e integrado à interface |

Os nomes/rotas HTTP completos estão no contrato implementado e no OpenAPI atual. A expansão frontend começa apenas com a operação correspondente pronta, sem recriar tipos de cadastro de outro módulo.

## Estados específicos

- Código duplicado/área inválida mostra erro no campo. Área opcional desconhecida não vira zero; área cultivável obrigatória segue o contrato de configuração.
- Sem configuração ou associação na data escolhida, identificar a ausência e orientar ao cadastro disponível; não inventar vigência ou setor.
- Proposta de sucessão diária mostra início inclusivo e fim exclusivo de cada intervalo, valores anteriores e futuros. Não oferecer arrastar intervalos, edição retroativa livre ou mudança intradiária enquanto forem pendências funcionais.
- Em dependência/conflito, preservar a proposta e apresentar fatos a revisar retornados pela API; não mudar as irrigações do passado junto com o setor atual.
- Setor/canteiro inativo continua identificado no detalhe histórico e não aparece como seleção ativa de novo fato.

## Etapas

| Etapa | Entrega de frontend | Dependência e aceite | Estado |
| --- | --- | --- | --- |
| FE02.1 | Listas/formulários/detalhes de horta, setor e canteiro; gestão/histórico | API 02.1; cadastrar canteiro adicional com identidade estável, tratar código duplicado | Não iniciada |
| FE02.2 | Configuração e associação por vigência; consulta por data | API 02.2; passado e futuro distintos, conflito com dependentes revisável | Não iniciada |
| FE02.3 | Completar visão de irrigação por setor e integração aos seletores | API 02.3/FE01; litros, contagens e duração parcial sem multiplicação | Não iniciada |

## Aceite verificável

1. Criar canteiro adicional pela interface, recarregar e selecioná-lo sem alterar fixture/UUID fixo.
2. B1 muda de S1 para S2 em 10/11: evento de 09/11 continua em S1; seleção de 10/11 segue S2. Consultar ambos no detalhe/histórico.
3. Configuração nova não modifica área de referência de ciclo anterior; dimensões ausentes permanecem desconhecidas.
4. Mudanças concorrentes de vínculo ou edição com versão antiga mostram conflito e preservam o rascunho, sem sucessão duplicada.
5. 120 L e irrigação interrompida de 20 L somam 140 L conhecidos; falta de duração é apresentada como cobertura parcial, não tempo zero.
6. Inativar cadastro mantém identificação dos fatos e impede nova escolha conforme API; celular/teclado permitem consultar tabelas de vigência e confirmar alterações.

**Pendências da PO:** áreas coletáveis, vigência diária, primeira associação histórica e política de revisão do passado. **Evidências:** pendentes.
