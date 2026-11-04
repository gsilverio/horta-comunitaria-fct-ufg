# Etapa 0 — Levantamento da aplicação e decisões de domínio

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

O levantamento técnico está registrado na análise. Confirmar somente decisões visuais pendentes: dispositivo prioritário, limite demonstrativo e linguagem de interação. Não iniciar modelagem de irrigação ou sensores.

**Status:** planejamento; implementação não iniciada nesta revisão. As tarefas originais abaixo são referência de evolução, subordinadas ao recorte revisado. Não constituem autorização de implementação.

## Conteúdo da proposta recebida

**Objetivo:** entender como os CRUDs atuais trabalham e evitar duplicação de entidades ou regras.

### Tarefas

- [ ] Identificar frontend, sistema de rotas, biblioteca de componentes e solução de estado.
- [ ] Mapear entidades reais `Horta`, `Setor` e `Canteiro`, relacionamentos, DTOs, validações e endpoints.
- [ ] Identificar permissões de leitura, criação, edição e exclusão.
- [ ] Verificar suporte atual a dimensões, área, ordenação e localização física.
- [ ] Diferenciar **setor espacial** de **setor de irrigação** junto à equipe do projeto.
- [ ] Decidir se a primeira versão permitirá edição do contorno externo da horta ou somente limite retangular parametrizado.
- [ ] Definir regras para canteiros que ainda não foram posicionados e dados pré-existentes.
- [ ] Validar visualmente a abordagem **React Konva** com um desenho simples.
- [ ] Documentar as decisões e os contratos resultantes desta etapa.

**Entregável:** mapa do domínio real, inventário de endpoints e documento curto de decisões arquiteturais.

**Critério de conclusão:** é possível descrever exatamente como um canteiro existente no CRUD será representado no canvas e vice-versa, sem criar cadastros paralelos.

---
