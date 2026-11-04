# Modelo de acesso, histórico e evidências

**Cobertura:** RF01, RF12 e RF13; suporte transversal a RF02–RF16. Ver [convenções comuns](Modelagem_Preliminar_Dados_Horta.md) e [diagramas](Diagramas_Relacionais_Horta.md#acesso).

Este modelo separa a identidade de acesso, as pessoas envolvidas na horta, os arquivos e o histórico das alterações. Não escolhe um provedor de autenticação nem cria um novo fluxo funcional de aprovação.

## Identidade e permissões

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `usuario` | `identificador_autenticacao`, `nome_exibicao`, `ativo`, `participante_id?` | Identidade de acesso; vínculo opcional e único com participante |
| `perfil` | `codigo`, `nome`, `descricao?`, `ativo` | Conjunto de permissões |
| `permissao` | `codigo`, `descricao` | Ação reconhecida pela aplicação |
| `perfil_permissao` | PK composta (`perfil_id`, `permissao_id`) | N:N entre perfis e ações |
| `usuario_perfil` | PK composta (`usuario_id`, `perfil_id`, `horta_id`) | Concessão de perfil num contexto de horta |

Identificador de autenticação é a chave estável da identidade, única no espaço do provedor escolhido. Formato e integração ainda serão definidos. Não presumir tabelas de senha, refresh tokens ou detalhes de uma biblioteca de identidade.

- Um usuário pode ter vários perfis; um perfil pode servir a vários usuários. A concessão é delimitada pela horta.
- Códigos de perfil/permissão são únicos. A lista inicial de ações e sua distribuição pelos perfis depende da PO.
- Administração/coordenação, equipe de campo e consulta são perfis propostos nos requisitos. Consulta não inclui escrita.
- Perfil organiza permissões; os catálogos podem ser definidos inicialmente pela aplicação. O modelo não exige telas para editar papéis ou criar permissões arbitrárias.
- Participante não precisa ter usuário. Na proposta inicial, uma conta aponta para no máximo um participante e esse vínculo não se repete em outra conta. A concessão de acesso continua em `usuario_perfil`, não nessa associação.
- Autoria vem do usuário autenticado; responsabilidade pelo trabalho vem do participante, quando conhecida.
- Desativar usuário preserva autoria. Conhecer o ID de um recurso ou ter conta ativa não prova acesso à sua horta.

## Arquivos e vínculos

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `evidencia` | `horta_id`, `registrado_por_id`, `registrado_em`, `titulo`, `descricao?`, `data_documento?`, `referencia_armazenamento`, `nome_original`, `tipo_conteudo`, `tamanho_bytes`, `situacao_arquivo` | Metadados do arquivo, com zero ou muitos vínculos |
| `vinculo_evidencia` | `evidencia_id` e exatamente uma FK de destino da lista abaixo | Liga um arquivo a um registro concreto |

Destinos iniciais: `horta_id`, `setor_id`, `canteiro_id`, `lote_propagacao_id`, `ciclo_id`, `plantio_id`, `avaliacao_pegamento_id`, `medicao_ambiental_id`, `observacao_agronomica_id`, `colheita_id`, `lote_secagem_id`, `irrigacao_id`, `atividade_id`, `ocorrencia_id` ou `lancamento_financeiro_id`.

Cada campo de destino é opcional isoladamente, mas exatamente um é preenchido por linha. As FKs concretas preservam integridade; não substituí-las por um tipo/id textual sem relação verificável. Um arquivo pode ter vários vínculos autorizados, sempre dentro da horta da evidência. Não repetir o mesmo par arquivo/destino.

Um upload pode ficar temporariamente sem vínculo enquanto o envio é concluído; limpeza de órfãos e limites de formato/tamanho serão definidos. A referência identifica o objeto armazenado, não um caminho fornecido pela pessoa. Download depende das permissões da horta e do registro, sem tornar o arquivo público pela existência do vínculo.

## Histórico de correções

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `auditoria` | `horta_id?`, `usuario_id`, `tipo_registro`, `chave_registro`, `operacao`, `motivo?`, `ocorrido_em`, `valores_anteriores?`, `valores_novos?` | Uma alteração por contexto e autor, ligada à identidade do registro alterado |

`chave_registro` preserva a chave simples ou composta do alvo; valores guardam apenas os campos necessários à rastreabilidade. Essa referência de histórico não é uma FK polimórfica: o histórico precisa continuar interpretável e não define relações operacionais entre entidades. A aplicação valida tipo/chave ao gravar; exclusões físicas de fatos continuam vedadas pela política de preservação.

- Corrigir ou invalidar exige motivo; inclusão não exige um motivo artificial.
- Alteração de dado e entrada de histórico são confirmadas juntas. Corrigir relações registra também os vínculos anteriores/novos.
- História conserva autor e instante, inclusive quando o usuário fica inativo. Não se edita pelo fluxo comum de registros.
- Edição envia versão esperada; conflito entre alterações exige revisão, sem sobrescrever automaticamente o trabalho anterior.
- Evitar dados pessoais desnecessários em snapshots; acesso ao histórico não se torna público.
- Cadastro de perfis, concessões e demais mudanças relevantes também precisam de autoria. Para fatos/concessões da horta, `horta_id` é obrigatório; nulo é reservado a alterações de identidade ou catálogos globais sem esse contexto. Essas entradas exigem permissão administrativa própria, sem exposição automática a quem consulta uma horta. Provisionamento técnico de identidades ainda depende do provedor escolhido.

**Exemplo:** corrigir uma colheita de 5 kg para 4,5 kg preserva o valor anterior, usuário, instante e motivo. Os indicadores usam o fato corrigido; o histórico não acrescenta mais 5 kg à produção.

## Decisões a validar

Forma de autenticação, matriz de acesso, visibilidade das evidências, tipos/tamanhos dos arquivos, retenção de histórico e dados pessoais mínimos. Papéis de participante não são perfis de acesso. A extensão dos destinos de evidência pode ser reduzida conforme os formulários prioritários.
