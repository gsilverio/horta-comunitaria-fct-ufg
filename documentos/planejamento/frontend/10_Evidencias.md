# FE10 — Documentos e fotografias nos registros de origem

**Requisitos:** RF12/RF13. **Estado:** pendente por decisão do responsável (RF12). **Fontes:** [plano funcional 10](../mvp/10_Evidencias.md) e [aceite comum](Comportamentos_Aceite.md). **Dependência:** contrato de upload/armazenamento e pelo menos um destino entregue.

**Execução atual:** [RF12 adiado](../Execucao_Frontend_MVP.md); este plano continua pendente.

## Percurso e interface

Abrir detalhe do registro → escolher arquivo e descrição → enviar → aguardar confirmação da disponibilidade → visualizar/baixar ou recuperar falha → consultar gestão e histórico.

| Seção | Conteúdo e ações |
| --- | --- |
| Evidências do detalhe | Somente arquivos associados àquela origem; descrição, tipo, tamanho, estado, autoria e data |
| Envio | Um arquivo, descrição/metadados exigidos e destino já definido pelo detalhe; feedback acessível de envio |
| Disponibilidade | Pendente: confirmação em processamento; disponível: abrir/baixar; falhou: erro recuperável e tentativa explícita conforme contrato |
| Gestão | Corrigir metadados/inutilizar com motivo e histórico; substituir conteúdo cria outra evidência |

O término da transferência não significa arquivo disponível. Usar estados confirmados pela API; nunca fabricar porcentagem de processamento do servidor. Download somente quando Ready. Nome original e descrição não podem expor caminhos locais, URLs de armazenamento ou conteúdo em logs.

Reenvio de operação incerta conserva identidade/conteúdo conforme contrato. Outro arquivo requer outra operação após resolver a tentativa anterior. O navegador pode exigir selecionar o arquivo novamente depois de reload; não prometer recuperação dos bytes nem armazenamento offline. Erros de tamanho/tipo aparecem junto ao campo; validação real do conteúdo cabe ao servidor.

PDF/DOCX/JPG/PNG e limite proposto de 10 MiB ainda dependem de PO/contrato. A UI usa a política efetivamente retornada ou acordada com o backend; não publicar limites provisórios como definitivos. O adaptador de armazenamento não implica integrar um provedor de nuvem nesta etapa.

## Etapas

| Etapa | Entrega | Dependência |
| --- | --- | --- |
| FE10.1 | Anexar à primeira origem entregue, listar estados e baixar disponíveis | API 10.1; destino concreto |
| FE10.2 | Recuperação de falhas/reenvios e confirmação de disponibilidade | Contratos de recuperação/gestão do plano funcional 10.2 |
| FE10.3 | Gestão/histórico e extensão aos outros destinos autorizados | Contratos 10.1–10.2; detalhes dos domínios; corresponde à entrega de interface 10.3 |

## Cenários de aceite

- Anexar fotografia a um registro e recarregar; vínculo/estado e disponibilidade vêm da API.
- Simular falha de armazenamento real: preservar metadados e mostrar Failed; não oferecer download inexistente.
- Repetir tentativa incerta não duplica associação; arquivo diferente não reutiliza a mesma identidade indevidamente.
- Rejeitar tipo/tamanho incompatível com mensagem recuperável e sem perder descrição.
- Evidência de outra horta/destino não aparece no detalhe atual; origem inválida segue a política de consulta/gestão acordada.
- Trocar arquivo mantém evidência anterior e histórico; não sobrescrever bytes silenciosamente.

Não criar um gerenciador geral de arquivos, editor documental ou integração externa sem requisito.
