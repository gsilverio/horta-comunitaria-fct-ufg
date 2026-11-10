# Primeira entrega — conferência dos dados prioritários

Conferência de 10/11/2026 contra a tabela "Dados inicialmente considerados prioritários". Escopo: **cadastrais e rastreabilidade**, **produtivos e agronômicos** e **operacionais**. Dados ambientais, irrigação e sensores ficam fora desta entrega (as telas existentes não foram alteradas).

## Cobertura

| Dado prioritário | Onde registrar na aplicação | Situação |
| --- | --- | --- |
| **Cadastrais e rastreabilidade** | | |
| Identificação dos canteiros e setores | Horta → Setores / Canteiros (código, nome, área, vínculo histórico com setor) | Já existia |
| Espécie e cultivar | Cultivo → Espécies / Cultivares (catálogo Embrapa) | Já existia |
| Origem das sementes ou mudas | Cultivo → Origem do material (tipo, origem, recebimento) | Já existia |
| Datas de plantio, replantio e colheita | Plantios e reposições (natureza Inicial/Reposição); Produção → Colheitas | Já existia |
| Quantidade de plantas e espaçamento | Plantio: quantidade, espaçamento entre plantas e entre linhas (cm) | Já existia |
| Manejos realizados | Atividades vinculadas ao canteiro/ciclo; **sugestões de categoria** (capina, adubação, controle de pragas…) | Sugestões adicionadas |
| Responsáveis pelos registros | Autor técnico (demonstração) em todo registro + "Responsável pelo trabalho" (participante) | Já existia |
| **Produtivos e agronômicos** | | |
| Mudas plantadas e sobreviventes | Plantio + Avaliações de pegamento; indicador Pegamento | Já existia |
| Crescimento das plantas | Observação agronômica com medida e unidade (sugestões cm, folhas…) | Sugestões adicionadas |
| Pragas, doenças ou deficiências | Observação agronômica (categorias sugeridas; suspeita sem diagnóstico) | Sugestões adicionadas |
| Datas de colheita, massa fresca, aproveitável e perdas | Colheita (massa fresca, aproveitável, motivo da perda); indicadores Produtividade e Perdas | Já existia |
| Massa após a secagem | Produção → Secagens (destinação do saldo e pesagem seca); indicador Secagem | Já existia |
| Produtividade | Indicador Produtividade (calculado no servidor) | Já existia |
| Capacidade de rebrote | Observação agronômica, categoria "Capacidade de rebrote" | Sugestão adicionada |
| **Operacionais** | | |
| Atividades realizadas e horas de trabalho | Atividades com participantes e horas individuais; indicador Horas | Já existia |
| Uso de composto, fertilizantes e insumos | Atividades → Insumos (catálogo) + usos na atividade | Já existia |
| Capina, reposição de mudas, manutenção | Categorias sugeridas de atividade; reposição também como Plantio "Reposição" | Sugestões adicionadas |
| Registros fotográficos | **Aba Fotos** nos registros + **Fotos → Galeria** ([decisão](../arquitetura/Fotografias_Campo.md)) | **Implementado nesta entrega** |
| Intercorrências de campo | Ocorrências (abertura, andamento, resolução, atividades vinculadas); sugestões de categoria | Sugestões adicionadas |

## Roteiro sugerido para a demonstração

1. Horta → Canteiros: abrir um canteiro e mostrar setor, área e a aba Fotos.
2. Cultivo → Ciclos: abrir o ciclo, registrar plantio (quantidade e espaçamento) e uma reposição.
3. Avaliação de pegamento → indicador Pegamento.
4. Observação agronômica (praga ou crescimento) com foto na aba Fotos.
5. Colheita (fresca/aproveitável/perda) → Secagem → indicadores Produtividade, Perdas e Secagem.
6. Atividade de capina com participantes, horas e insumo → indicador Horas.
7. Ocorrência com foto, providência vinculada e resolução.
8. Fotos → Galeria: filtrar por tipo e abrir o registro de origem.

## Validação desta entrega

- `dotnet build -c Release`: 0 erros, 0 avisos. `npm run build`: aprovado. `npm run lint`: 0 erros (1 aviso pré-existente do TanStack Table no `GardenDataGrid`).
- Interface conferida no navegador (desktop e 375 px): aba Fotos, galeria com filtros, visualização ampliada e link para o registro vinculado.
- Após reiniciar a API (migration `AddEvidenceFiles` aplicada no startup de Development), o fluxo foi exercitado pela API local com imagem gerada e identificada como fictícia: envio 201; retry com a mesma `operationId` 200 sem duplicar arquivo; arquivo que não é imagem, enum numérico e vínculo inexistente rejeitados (400/400/404); conteúdo devolvido idêntico; correção de legenda 200 e versão antiga 409; invalidação preservando o arquivo. Ficaram no banco de demonstração duas fotos de teste (uma válida, uma invalidada) vinculadas a um ciclo.
- A seleção de arquivo pela interface não foi automatizada (o navegador de teste não envia arquivos); o envio pela tela usa o mesmo endpoint validado acima.
- Testes de integração/E2E não executados, conforme dispensa da fase.

## Pendências para a PO

Limites e formatos de foto, inclusão de documentos (PDF), categorias sugeridas (são apenas sugestões, texto continua livre) e se "capacidade de rebrote" deve ganhar campo estruturado na colheita.
