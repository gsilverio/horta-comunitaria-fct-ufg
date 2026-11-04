# Etapa 7 — Integração das duas experiências e modo operação

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Recortar agora apenas a navegação para entrar/sair da experiência e preservar o contexto da horta. Painéis de irrigação, sensores, culturas, indicadores e sincronização operacional ficam fora. A integração com cadastros é futura.

**Status:** planejamento; implementação não iniciada nesta revisão. As tarefas originais abaixo são referência de evolução, subordinadas ao recorte revisado. Não constituem autorização de implementação.

## Conteúdo da proposta recebida

**Objetivo:** consolidar a convivência do CRUD com a interface interativa.

### Tarefas

- [ ] Incluir a ação “Abrir Horta Interativa” nas telas administrativas.
- [ ] Disponibilizar navegação de volta ao cadastro/listagem sem perder o contexto.
- [ ] Implementar modo visualização, sem ferramentas de alteração espacial.
- [ ] Clicar em um setor ou canteiro abre painel com suas informações atuais.
- [ ] Oferecer “Abrir cadastro” e “Editar propriedades”, conforme permissão.
- [ ] Tratar registros criados no CRUD sem posição no mapa com lista “Não posicionados”.
- [ ] Definir atualização de dados após edição administrativa em outra aba/tela.
- [ ] Aplicar permissões: visualizar layout não significa poder modificá-lo.
- [ ] Revisar identidade visual e adaptar layout a telas menores.
- [ ] Adicionar camada opcional para visualizar cultura/status **somente se os dados correspondentes já estiverem disponíveis**.

**Entregável:** duas interfaces funcionando sobre os mesmos registros, com uma experiência visual operacional coerente.

**Critérios de conclusão:**

- Um canteiro criado no CRUD pode ser colocado no mapa sem recriar seu cadastro.
- O clique em um canteiro mostra seus dados reais, não conteúdo demonstrativo.
- Atualizações de nome/setor/canteiro aparecem nas duas experiências.
- Usuário sem permissão de edição não consegue alterar o layout, inclusive via API.

---
