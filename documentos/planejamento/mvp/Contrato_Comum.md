# Contrato comum dos fluxos do MVP

As [regras arquiteturais](../../../.codex/arquitetura-backend.md) continuam sendo a única fonte de convenções técnicas. Este documento concentra o comportamento transversal de RF13/RF14 para os planos dos fluxos 02–10. O [fluxo 01](../mvp-irrigacao/Contrato_Fluxo.md) tem seu contrato detalhado compatível com este.

## Escopo de execução

Implementar apenas o que foi autorizado na etapa. Nenhum plano inclui autenticação/autorização. Usar autor demonstrativo do servidor, separado do participante responsável. Referências de outra horta são rejeitadas mesmo nesse modo; não existe transferência de fatos entre hortas pela edição de GardenId.

Cada etapa cria seus slices `Features/{Domain}/{UseCase}` com CommandHandler ou QueryHandler e Endpoints conforme a arquitetura. Nomes de casos de uso listados nos planos são os nomes dos arquivos; contratos ficam no próprio slice. Não criar framework de CRUD, motor de fórmulas ou tabela de valor final de dashboard.

## Entrada e consulta

- Rotas de fatos: `/api/v1/gardens/{gardenId:guid}/...`; catálogos compartilhados podem ficar fora da horta. IDs de recurso usam Guid e Guid.Empty é rejeitado. Propriedades JSON são camelCase.
- Datas agrícolas: DateOnly; instantes: offset obrigatório, armazenados UTC. Períodos from/to locais inclusivos, fuso America/Sao_Paulo, com limites UTC apropriados para instantes. A data de competência de cada indicador está no plano específico.
- Listagem: Page, default 20/máximo 100; fatos ordenados por data/instante decrescente e Id decrescente. Cadastros por código/nome e Id. Filtros aplicados antes de materialização, sem N+1.
- Textos com trim. Defaults do protótipo, salvo campo específico: código até 40; nome até 120; descrição/observações até 1000; método até 200; motivo de alteração 1–500. Opcional vazio vira null. Não inventar campos obrigatórios extras.
- Volumes, áreas, massas, duração, medidas e horas: decimal numeric(14,3) no protótipo; dinheiro numeric(14,2). Inteiros para contagens. Rejeitar escala/capacidade excedida, não truncar. Unidade explícita conforme o fluxo.
- Campo desconhecido é null; zero coletado é zero. Não criar linha fictícia para ausência de informação. Precisão e limites são decisões do protótipo para discutir com a PO.
- Enum de request: string PascalCase tipada, rejeita números/valores indefinidos; enum persistido: PostgreSQL ENUM nativo, conforme regras. Categoria livre prevista no plano é texto limitado, sem criar enum/catalogue obrigatório por inferência.

## Criação e alterações

Criação de **fato de campo** usa operationId para repetição, unicidade por horta e tipo de fato, comparação com payload inicial normalizado e imutável. Mesmo conteúdo/chave retorna o estado atual (200); diferente é 409. Nova criação: 201/Location. Não inventar unicidade por dia ou local. Cadastros usam suas chaves de negócio únicas e retornam 409 na duplicação.

Fatos possuem RecordedByUserId, RecordedAt, UpdatedAt?, RecordValidity Valid/Invalid e Version. Cadastros editáveis precisam de versão e histórico das mudanças, sem transformar a versão em dado editável. Comandos não recebem identidade de autoria.

Correção: `PUT /{id}` com representação completa editável + expectedVersion + reason. Invalidação: `POST /{id}/invalidation` com expectedVersion/reason. Histórico: `GET /{id}/history`, paginado por instante/Id decrescente. Cadastros usam inativação em vez de invalidação quando aplicável. Nunca oferecer exclusão física de fatos ou histórico.

Criação e alteração confirmam o estado e AuditEntry juntos. Version inicia em 1, incrementa por mudança efetiva; preserva autoria/criação originais. Correção normalizada sem mudança, com versão correta, retorna estado atual sem incrementar/auditar. Versão antiga é 409; fato invalidado não é corrigido/reativado nesta versão. Falha de auditoria reverte a alteração. Snapshot inclui vínculos alterados e antes/depois, sem multiplicar os fatos de origem.

**Dependências:** não invalidar um pai com fatos válidos dependentes nem reduzir uma quantidade abaixo do uso já confirmado. Retornar 409 `record.has_dependents`, identificando os vínculos a revisar. O usuário revisa dependentes explicitamente; não criar cascata, workflow de aprovação ou reversão automática. Especificações de saldo e vigência nos planos complementam essa regra. Leitura de saldo/intervalo e gravação devem compartilhar proteção transacional apropriada, testada sob concorrência no PostgreSQL.

Erros: 400 formato/invariante (`validation.invalid_request` ou código específico do domínio), 404 recurso inexistente/fora da horta (`resource.not_found`), 409 duplicação, versão, invalidado ou dependência. Result/ProblemDetails com campos e traceId conforme arquitetura, sem SQL/stack trace. Não criar 401/403 simulados.

## Dashboards e interface

Somar fatos válidos, respeitando dependências válidas e filtros. Nunca multiplicar o fato por coleções filhas. Leituras múltiplas que compõem um resultado usam snapshot consistente, conforme a estratégia já definida no fluxo de consumo.

Resposta informa período, unidade, filtros, definição resumida, contagens de origem e situação: NoData, Unavailable, Partial ou Available, seguindo a semântica do fluxo 01. Percentual não existe com denominador desconhecido/zero. Resultado parcial não é chamado total completo. Não combinar métodos/unidades incompatíveis nem fazer média simples de percentuais de bases diferentes.

A etapa de interface consome API real, com carregamento/erro/vazio, null/zero e mensagens pt-BR. Após alteração, recarrega registros, detalhe, histórico e indicadores afetados. Rascunho é preservado em conflito de versão, sem retry automático da intenção. Formularios precisam funcionar em celular; nenhuma tela de módulos ainda não entregues.

## Aceite comum de cada etapa com código

1. Build Release sem warnings nos projetos afetados; TypeScript/build do frontend quando alterado.
2. Integração PostgreSQL real com migrations e asserts de conteúdo. Cobrir referências de outra horta, null/zero, repetição de criação e rollback/concorrência nas escritas implementadas.
3. Fórmulas com exemplos do plano, filtros nas bordas e efeito de correção/invalidação. Testes não significam aceite da PO.
4. OpenAPI, documentação local de execução e status/evidências atualizados. Limitação concreta registrada, sem marcar conclusão fictícia.

Pendências funcionais expressamente marcadas exigem definição antes da implementação do comportamento dependente. O agente não resolve pendência impondo restrição nova ao usuário; pode continuar o restante da etapa que seja independente.
