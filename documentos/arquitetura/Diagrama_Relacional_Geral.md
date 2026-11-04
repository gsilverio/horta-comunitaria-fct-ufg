# Diagrama relacional geral da horta

Esta visão reúne as 37 entidades da [modelagem preliminar](Modelagem_Preliminar_Dados_Horta.md), cobrindo RF01–RF16. Os nomes são lógicos, não DDL PostgreSQL. As relações principais estão abaixo; campos, FKs opcionais de contexto e destinos de evidência estão nos [diagramas detalhados](Diagramas_Relacionais_Horta.md).

```mermaid
erDiagram
    direction LR
    HORTA ||..o{ SETOR : organiza
    HORTA ||..o{ CANTEIRO : possui
    CANTEIRO ||..o{ CONFIGURACAO_CANTEIRO : tem_configuracao
    CANTEIRO ||..o{ VINCULO_CANTEIRO_SETOR : tem_vigencia
    SETOR ||..o{ VINCULO_CANTEIRO_SETOR : recebe
    CANTEIRO ||..o{ CICLO_CULTIVO : abriga
    CONFIGURACAO_CANTEIRO ||..o{ CICLO_CULTIVO : referencia_area
    ESPECIE ||..o{ CULTIVAR : possui
    ESPECIE ||..o{ CICLO_CULTIVO : identifica
    CULTIVAR |o..o{ CICLO_CULTIVO : especifica
    HORTA ||..o{ LOTE_PROPAGACAO : recebe
    ESPECIE ||..o{ LOTE_PROPAGACAO : identifica
    CULTIVAR |o..o{ LOTE_PROPAGACAO : especifica
    CICLO_CULTIVO ||..o{ PLANTIO : registra
    LOTE_PROPAGACAO |o..o{ PLANTIO : origina
    PLANTIO |o..o{ PLANTIO : reposto_por
    PLANTIO ||..o{ AVALIACAO_PEGAMENTO : avaliado_em
    CICLO_CULTIVO ||..o{ OBSERVACAO_AGRONOMICA : observado_em
    CICLO_CULTIVO ||..o{ COLHEITA : produz
    HORTA ||..o{ LOTE_SECAGEM : processa
    LOTE_SECAGEM ||..o{ ITEM_SECAGEM : recebe
    COLHEITA ||..o{ ITEM_SECAGEM : fornece
    HORTA ||..o{ PONTO_COLETA : define
    CANTEIRO |o..o{ PONTO_COLETA : contextualiza
    SETOR |o..o{ PONTO_COLETA : contextualiza
    PONTO_COLETA ||..o{ MEDICAO_AMBIENTAL : localiza
    VARIAVEL_AMBIENTAL ||..o{ MEDICAO_AMBIENTAL : define_grandeza
    VINCULO_CANTEIRO_SETOR |o..o{ MEDICAO_AMBIENTAL : preserva_contexto
    SETOR ||..o{ IRRIGACAO : recebe
    IRRIGACAO ||--o{ IRRIGACAO_CANTEIRO : atende
    CANTEIRO ||--o{ IRRIGACAO_CANTEIRO : atendido_em
    VINCULO_CANTEIRO_SETOR ||..o{ IRRIGACAO_CANTEIRO : preserva_contexto
    HORTA ||..o{ ATIVIDADE : organiza
    HORTA ||..o{ PARTICIPANTE : cadastra
    ATIVIDADE ||--o{ ATIVIDADE_PARTICIPANTE : envolve
    PARTICIPANTE ||--o{ ATIVIDADE_PARTICIPANTE : participa
    ATIVIDADE ||--o{ ATIVIDADE_INSTITUICAO : envolve
    INSTITUICAO ||--o{ ATIVIDADE_INSTITUICAO : participa
    ATIVIDADE ||..o{ USO_INSUMO : utiliza
    INSUMO ||..o{ USO_INSUMO : utilizado_em
    HORTA ||..o{ OCORRENCIA : acompanha
    IRRIGACAO |o..o{ OCORRENCIA : contextualiza
    OCORRENCIA |o..o{ ATIVIDADE : atendida_por
    HORTA ||..o{ LANCAMENTO_FINANCEIRO : registra
    CANTEIRO |o..o{ LANCAMENTO_FINANCEIRO : contextualiza
    CICLO_CULTIVO |o..o{ LANCAMENTO_FINANCEIRO : contextualiza
    PARTICIPANTE |o..o| USUARIO : pode_ter_conta
    USUARIO ||--o{ USUARIO_PERFIL : recebe
    PERFIL ||--o{ USUARIO_PERFIL : concede
    HORTA ||--o{ USUARIO_PERFIL : delimita
    PERFIL ||--o{ PERFIL_PERMISSAO : possui
    PERMISSAO ||--o{ PERFIL_PERMISSAO : autoriza
    HORTA ||..o{ EVIDENCIA : guarda
    USUARIO ||..o{ EVIDENCIA : registra
    EVIDENCIA ||..o{ VINCULO_EVIDENCIA : apoia
    CICLO_CULTIVO |o..o{ VINCULO_EVIDENCIA : alvo
    ATIVIDADE |o..o{ VINCULO_EVIDENCIA : alvo
    OCORRENCIA |o..o{ VINCULO_EVIDENCIA : alvo
    LANCAMENTO_FINANCEIRO |o..o{ VINCULO_EVIDENCIA : alvo
    USUARIO ||..o{ AUDITORIA : autoria
    HORTA |o..o{ AUDITORIA : contexto
```

## Leitura e limites

- `||`: exatamente um; `o|`/`|o`: zero ou um; `o{`: zero ou muitos.
- Entidade repetida em outros diagramas é a mesma entidade. Associações tornam explícitas as relações N:N.
- FKs opcionais de contexto são alternativas sujeitas às regras de cada modelo; não representam obrigação de preencher todos os contextos.
- Evidências têm 15 tipos de destinos concretos previstos no diagrama detalhado, com exatamente um destino por vínculo.
- Autoria/validade dos fatos e responsáveis seguem as [convenções comuns](Modelagem_Preliminar_Dados_Horta.md). As arestas repetitivas dessas referências foram omitidas nesta visão geral.
- Auditoria identifica a chave do registro alterado como histórico; não cria uma FK polimórfica ou um novo registro produtivo.
- RF14 consulta os fatos. Dashboard e valor final de indicador não são entidades para digitação manual.

Não há implementação. Cardinalidades, campos indispensáveis e protocolos agronômicos continuam sujeitos à validação indicada nos modelos.
