# Fotografias de campo (RF12, recorte da primeira entrega)

**Situação:** implementado para demonstração local em 10/11/2026. Hipótese técnica do protótipo; formatos, limites, retenção e visibilidade ainda precisam de validação com a PO.

## Contexto

Os dados prioritários incluem "registros fotográficos e intercorrências de campo", e RF08 prevê observação "com medida ou fotografia". A [modelagem de evidências](Modelo_Acesso_Historico_Evidencias.md) propõe metadados no banco e conteúdo atrás de uma fronteira de armazenamento. A escolha do armazenamento definitivo continua aberta.

## Decisão

- **Uma foto, um registro.** Cada fotografia (`evidence_files`) tem exatamente uma FK concreta preenchida (check `num_nonnulls(...) = 1`), sempre na mesma horta via FK composta `(GardenId, Id)`. Destinos desta etapa: canteiro, ciclo, plantio/reposição, avaliação de pegamento, observação agronômica, colheita, secagem, atividade e ocorrência. A tabela de vínculos N:N da modelagem fica para quando houver necessidade real de uma foto em vários registros.
- **Envio no próprio registro.** A aba **Fotos** do detalhe é o ponto de envio, galeria e visualização. A página **Fotos → Galeria** lista tudo da horta, filtra por tipo/validade e leva ao registro vinculado. Assim nenhuma foto fica "solta".
- **Armazenamento local atrás de `IFileStorage`.** `LocalFileStorage` grava em `FileStorage:RootPath` (padrão `HortaComunitaria.Api/App_Data/evidences/AAAA-MM/<uuid>.<ext>`, ignorado pelo Git). A referência é gerada pelo servidor; o nome original serve só para exibição. Trocar por S3/Azure Blob/MinIO exige apenas outro adaptador.
- **Validação no servidor.** JPEG, PNG ou WebP conferidos pela assinatura do arquivo (não pelo nome/Content-Type), limite `FileStorage:MaxFileBytes` (10 MB padrão, máximo aceito na configuração 25 MB). O navegador reduz fotos acima de 1,5 MB para no máximo 2048 px (JPEG 85%) antes do envio — fotos de celular ficam com poucas centenas de KB.
- **Repetição e órfãos.** `operationId` único por horta; a mesma chave com o mesmo conteúdo (SHA-256 + vínculo) devolve a foto existente, conteúdo diferente gera 409. O arquivo é gravado antes da transação curta; se a confirmação falhar, o arquivo é removido. Uma queda do processo entre gravação e commit pode deixar um arquivo sem metadados (limpeza manual por enquanto).
- **Correção e invalidação.** Título, descrição e data da foto são corrigíveis com versão esperada e motivo. Invalidar retira a foto da galeria sem apagar arquivo, metadados ou histórico. Não há exclusão física. Registros invalidados/inativos não recebem novas fotos.
- **Antiforgery desativado no upload**: não há autenticação por cookie no protótipo; rever na etapa de identidade.

## Contrato HTTP

| Método e rota | Uso |
| --- | --- |
| `POST /api/v1/gardens/{gardenId}/evidences` (multipart) | `file`, `operationId`, `targetType`, `targetId`, `title`, `description?`, `capturedOn?` → 201 (`Location`) ou 200 em retry |
| `GET /api/v1/gardens/{gardenId}/evidences` | `targetType?`, `targetId?`, `validity?`, `page`, `pageSize` → `Page<Evidence>` |
| `GET /api/v1/gardens/{gardenId}/evidences/{id}/content` | Conteúdo da imagem (`nosniff`, ETag pelo SHA-256) |
| `PUT /api/v1/gardens/{gardenId}/evidences/{id}` | Corrigir legenda: `title`, `description`, `capturedOn`, `expectedVersion`, `reason` |
| `POST /api/v1/gardens/{gardenId}/evidences/{id}/invalidation` | `expectedVersion`, `reason` |

`targetType`: `Bed`, `CultivationCycle`, `Planting`, `EstablishmentAssessment`, `AgronomicObservation`, `Harvest`, `DryingLot`, `Activity`, `Occurrence`. O tipo não é persistido como enum: deriva da FK preenchida.

## Consequências e pendências

- Migration `AddEvidenceFiles` gerada pelo EF CLI (cria somente `evidence_files`). Em Development com `ApplyMigrationsOnStartup=true`, é aplicada ao reiniciar a API.
- Backup: copiar o banco **e** a pasta `App_Data/evidences`.
- Validar com a PO: formatos/limite, se documentos (PDF) entram, retenção, quem pode ver/baixar quando houver autenticação, e necessidade de uma foto em vários registros.
