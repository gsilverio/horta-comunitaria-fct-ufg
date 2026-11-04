# Caso 09 — Corrigir irrigação

**Etapa:** [3](../Etapa_03_Correcao_Historico.md). **Estado:** concluído tecnicamente; [evidências](../Etapa_03_Correcao_Historico.md#evidências-da-execução), PO pendente. **Referência:** RF13.

Dependências: criação, detalhe, histórico de inclusão e consulta de consumo. Ler [contrato](../Contrato_Fluxo.md) e etapa 3.

## Contrato e sequência

`PUT /api/v1/gardens/{gardenId}/irrigations/{id}`. Body: formulário completo + expectedVersion inteiro positivo + reason. Resposta 200 detalhe atualizado.

1. Validar request/motivo; carregar pelo escopo de horta, incluindo vínculos.
2. Rejeitar Invalid (409 record_invalid) e versão diferente (409 version_conflict). Não adotar silenciosamente a versão do banco.
3. Resolver novamente relações do contexto corrigido e aplicar invariantes do domínio. Guardar snapshot do estado anterior; comparação normalizada igual retorna estado atual, sem alteração ou histórico.
4. Alterar evento/filhos, incrementar Version e UpdatedAt; guardar AuditEntry Corrected com antes/depois, motivo, autor técnico e instante. Preservar autoria/criação/CreationPayload originais.
5. Confirmar juntos. Concorrência do EF dá 409; sem retry da intenção. Resposta usa os dados confirmados.

## Alterações e aceite

- Slice `Features/Irrigation/CorrectIrrigation`, arquivos CommandHandler/Endpoints correspondentes, métodos de domínio e registrador.
- 120 → 100 L produz versão 2, histórico e consumo 100. Mudar data/setor muda elegibilidade do indicador com contexto anterior preservado.
- Rejeição/falha de auditoria conserva fato, filhos e versão anteriores. Duas alterações da mesma versão confirmam só uma.
- Motivo obrigatório; versão obsoleta conflita até em correção sem mudança. Repetição da criação original não compara com o estado corrigido.

**Limite:** sem correção de histórico, reativação ou sobrescrita automática.
