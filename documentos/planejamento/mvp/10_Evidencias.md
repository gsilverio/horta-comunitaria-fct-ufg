# Fluxo 10 — Documentos e fotografias

**Requisitos:** RF12 e consulta/histórico de RF13. **Estado:** adiado pelo responsável em 08/10/2026; não implementar nesta entrega.

**Leituras:** [contrato comum](Contrato_Comum.md), [modelo de evidências](../../arquitetura/Modelo_Acesso_Historico_Evidencias.md), arquitetura integral. **Dependência:** registros de destino já implementados. Este plano não adiciona requisito de acesso; RF01 fica fora do MVP.

## Resultado e percurso

Abrir um registro → anexar arquivo e descrição → confirmar vínculo → consultar/download a partir da origem → corrigir metadados ou invalidar sem deixar vínculo para arquivo ausente.

## Dados e regras

- Evidence: title, description?, documentDate?, storageReference, originalName, contentType, sizeBytes, fileStatus Pending/Ready/Failed, autoria/instante e validade/versão. Destino não é caminho de disco enviado pelo usuário.
- EvidenceLink: evidenceId e exatamente uma FK concreta para destino do modelo preliminar. Criar somente colunas/relações de destinos já implementados; no fluxo completo, lista inclui horta, setor, canteiro, lote de propagação, ciclo, plantio, avaliação, coleta, observação, colheita, secagem, irrigação, atividade, ocorrência e lançamento financeiro.
- Todas as relações são na mesma horta. Unicidade do par arquivo/destino, FK real e check de exatamente um destino por linha. Request pode trazer targetType/targetId enum tipado; o handler traduz para FK concreta, sem substituir relações operacionais por referência textual.
- Proposta para demonstração: arquivo em diretório local configurado e ignorado pelo Git, atrás de uma interface de armazenamento; banco só metadados/referência. Não escolher provedor definitivo/cloud nesta etapa.
- Limites propostos configuráveis: 10 MiB por arquivo, PDF/DOCX/JPEG/PNG. Definir e verificar extensão, MIME e assinatura/estrutura compatível no servidor antes de aceitar. Nome original é metadado, storage key é gerada no servidor. Limites/formatos aguardam a PO e não são restrições aprovadas dos requisitos.

## Contratos e slices

Prefixo `/api/v1/gardens/{gardenId}`: POST `/evidences` multipart com arquivo/title/description?/documentDate?/targetType/targetId/operationId; GET `/evidences?targetType=...&targetId=...`, GET/PUT `/evidences/{id}`, GET `/evidences/{id}/content`, invalidação/histórico. PUT corrige metadados/vínculos com expectedVersion/reason, sem substituir bytes silenciosamente; outro arquivo é nova evidência.

Mesma operationId deve comparar metadados/destino e hash do conteúdo inicial: Ready existente retorna a evidência; conteúdo diferente dá 409; envio ainda Pending dá 409 identificável, sem criar outro vínculo. Interrupção/Failed permite retentar a mesma tentativa de modo controlado, preservando histórico e referência, nunca afirmando upload concluído. Não criar fila/worker.

Slices Evidence: UploadEvidence/ListEvidenceForRecord/GetEvidence/DownloadEvidence/CorrectEvidenceMetadata/InvalidateEvidence/ListEvidenceHistory. Não criar navegador público de arquivos nem armazenamento dentro do banco.

## Etapas de implementação

| Etapa | Trabalho | Saída e aceite |
| --- | --- | --- |
| 10.1 — Upload e vínculo | Adapter local, configurações, metadata/link/migration, validação e slices de upload/detalhe/download | Arquivo correto vira Ready somente depois de bytes e vínculo confirmados |
| 10.2 — Recuperação e gestão | Correção/invalidação/histórico, retry controlado e limpeza de órfãos documentada | Falhas não deixam origem apontando para arquivo inexistente |
| 10.3 — Interface | Anexo/lista/download na página de cada destino implementado; progresso/erro | Foto/documento consultável a partir da ocorrência/cultivo, com mensagens claras |

## Consistência do envio

Não manter transação EF aberta durante upload. Validar destino/arquivo; criar tentativa Pending com histórico Created atomicamente e escrever fora da transação. Em seguida confirmar Ready, EvidenceLink e histórico de mudança de estado juntos, revalidando destino. Usar operação técnica FileStateChanged no histórico de evidência, sem simular motivo de correção humana. Se falhar, manter estado de falha rastreável e limpar bytes sem vínculo; se limpeza falhar, documentar retry local por comando administrativo, sem publicar endpoint de manutenção. Download só entrega Ready/Valid, após resolver horta/vínculo e verificar disponibilidade real; falta de bytes é falha explícita, não retorno vazio. Transições técnicas incrementam versão e preservam autor/criação; Ready é condição para a resposta final de sucesso do upload.

Invalidação preserva metadados/histórico e não apaga bytes automaticamente. Consulta normal não apresenta Failed/Pending como evidência pronta. Não incluir metadados do caminho físico em respostas ou logs.

## Cenários de aceite

1. Foto válida anexada à ocorrência/ciclo aparece nessa origem e pode ser obtida pela rota de conteúdo, sem expor caminho local.
2. Arquivo acima do limite, tipo incompatível, tentativa de path traversal e destino de outra horta são rejeitados sem vínculo pronto.
3. Falha no upload ou confirmação DB não cria Ready sem arquivo; retry não duplica evidência/link. Limpeza malsucedida fica registrada para revisão.
4. Dois destinos preenchidos, vínculo duplicado e destino inexistente são impedidos. Correção/invalidação mantém versões/antes/depois.
5. Interface mostra falha sem assumir sucesso, conserva descrição para retry e não aceita dois cliques como dois anexos. Aplicar verificações comuns.

**Validar com a PO:** tipos/tamanhos, descrição/data relevantes e momento de anexar. Retenção, provedor definitivo e controles de acesso terão planejamento posterior. **Evidências:** pendentes.
