# FE06 — Atividades, participantes, insumos e horas

**Requisitos:** RF10/RF13/RF14. **Estado:** implementado tecnicamente; aceite da PO pendente. **Fontes:** [plano funcional 06](../mvp/06_Atividades_Horas.md) e [aceite comum](Comportamentos_Aceite.md).

**Execução atual:** [Interfaces e integração entregues](../Execucao_Frontend_MVP.md). Os critérios abaixo orientam validação; testes técnicos não constituem aceite funcional.

## Percurso e telas

Selecionar horta → cadastrar participantes/insumos → registrar atividade → informar participações e consumos conhecidos → consultar horas → abrir atividade de origem → corrigir/invalidar e consultar histórico.

| Tela | Campos e ações |
| --- | --- |
| Participantes | Listar, criar, detalhar, corrigir e inativar; nome de exibição e categoria conforme contrato. Participante não é conta de acesso |
| Insumos | Catálogo com nome e unidade; criar, corrigir/inativar conforme dependências. Sem estoque, saldo ou compras |
| Atividades | Lista por período/contexto/tipo/validade, criar e abrir detalhe; paginação e recuperação de erros |
| Formulário | Data, tipo, descrição, contexto geral ou canteiro/setor/ciclo, responsável e coleções de participantes/insumos |
| Participações | Selecionar cada participante uma vez; horas individuais conhecidas ou não informadas. Adicionar/remover linhas antes de salvar |
| Consumos | Insumo, quantidade positiva e unidade do catálogo; diferentes usos legítimos não são eliminados por terem o mesmo insumo |
| Detalhe | Dados, contexto, participações/consumos, autoria e versão; correção completa com motivo, invalidação e histórico |
| Horas | Total conhecido, participações com/sem horas, cobertura e origens no recorte retornado |

Participações sem horas não significam zero trabalho; lista vazia e participações com horas desconhecidas são situações distintas. Quantidade obrigatória de insumo não pode virar zero ao deixar o campo vazio. Trocar o contexto limpa seleções incompatíveis e preserva os campos independentes. Não multiplicar horas por número de insumos ou instituições.

## Etapas e dependências

| Etapa | Entrega | Dependência real |
| --- | --- | --- |
| FE06.1 | Manutenção de participantes e insumos, seletores da horta/catálogo | API 06.1; hoje GET de participantes não basta para cadastrar |
| FE06.2 | Criar/listar/detalhar/corrigir/invalidar atividades com coleções; histórico | API 06.2 e contextos efetivamente disponíveis |
| FE06.3 | Consulta de horas com cobertura e navegação às atividades | API 06.3; composição FE11 |

FE07 e FE08 estendem essa mesma atividade. A interface deve admitir suas seções quando entregues, sem criar agora campos sem contrato ou gravar atividades duplicadas.

## Cenários de aceite

- Cadastrar participante e selecioná-lo em uma atividade; inativação impede seleção nova e preserva identificação histórica.
- Três participações de duas horas retornam seis horas; adicionar dois insumos mantém seis.
- Deixar horas vazias, salvar e reabrir preserva desconhecido; informar zero preserva zero observado. Total/cobertura vêm da API.
- Bloquear participante repetido e quantidade de insumo vazia/incompatível; mostrar erro na linha correspondente.
- Alterar uma participação com motivo atualiza detalhe, histórico e indicador; conflito preserva rascunho e exige revisão.
- Invalidar atividade exclui suas horas do indicador e mantém sua consulta histórica.

Pendências: categorias, tipos e unidades de catálogo precisam da validação prevista no plano funcional. Não acrescentar presença, escala, pagamentos ou credenciais.
