# Etapa 6 — Persistência e integração com a API

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Adiada, fora do foco inicial confirmado pelo usuário. Exige contrato próprio e decisão sobre o que salvar; não executar migrations nem integrar registros operacionais nesta experiência.

**Status:** planejamento; implementação não iniciada nesta revisão. As tarefas originais abaixo são referência de evolução, subordinadas ao recorte revisado. Não constituem autorização de implementação.

## Conteúdo da proposta recebida

**Objetivo:** transformar o protótipo em recurso real da plataforma.

### Tarefas

- [ ] Definir contrato de leitura e escrita do layout com base na API existente.
- [ ] Persistir a geometria independentemente dos detalhes visuais do Konva.
- [ ] Associar objetos gráficos aos IDs reais de horta/setor/canteiro.
- [ ] Implementar criação, atualização e remoção de registros respeitando as regras do domínio.
- [ ] Definir transação para operações em lote (ex.: gerar mandala com vários canteiros).
- [ ] Adicionar revisão/versão do layout para detectar alterações concorrentes.
- [ ] Validar geometria e autorizações no backend.
- [ ] Exibir resultados de validação e erros sem descartar o rascunho local.
- [ ] Confirmar descarte quando houver alterações pendentes.
- [ ] Implementar carga de um layout previamente salvo e conferir equivalência geométrica.

### Contratos conceituais — definir nomes após auditoria

```text
GET   [rota-existente-ou-nova]/hortas/{id}/layout
PUT   [rota-existente-ou-nova]/hortas/{id}/layout

Payload de atualização:
- revisão conhecida
- elementos alterados/criados/removidos
- geometrias de negócio
- vínculos com entidades do domínio
```

Não assumir que essas rotas já existem nem implementar endpoints paralelos aos atuais sem antes mapear a codebase.

**Entregável:** carregamento e salvamento reais, com tratamento de erros, validação e vínculo aos CRUDs.

**Critérios de conclusão:**

- Criar um canteiro visualmente e salvar produz o registro correto na aplicação.
- Modificar uma forma e recarregar a página preserva a geometria em metros.
- Duas alterações concorrentes não sobrescrevem dados silenciosamente.
- Falha de rede ou erro de validação não causa perda silenciosa do trabalho local.

---
