# Diagramas relacionais por domínio

Detalhamento da [modelagem preliminar](Modelagem_Preliminar_Dados_Horta.md), complementar ao [diagrama geral](Diagrama_Relacional_Geral.md). Cada bloco mostra chaves e campos essenciais à leitura das relações; os modelos de domínio contêm o restante do dicionário, as unidades e as regras.

`PK` indica chave primária; `FK`, referência; `UK`, unicidade individual. `||` é exatamente um, `o|`/`|o` zero ou um e `o{` zero ou muitos. PK composta contém todos os campos marcados `PK` na associação. Comentário “opcional” indica nulabilidade proposta. Nomes são lógicos; não são as colunas físicas da futura implementação.

Metadados comuns e FKs de responsável são descritos na [visão da modelagem](Modelagem_Preliminar_Dados_Horta.md), sem repeti-los em cada caixa/aresta. Cada `responsavel_participante_id` aponta para um participante da horta do fato; cada `registrado_por_id`, para seu usuário autor.

Linhas tracejadas (`..`) indicam relações que não compõem a PK do filho; linhas contínuas (`--`) são usadas nas associações cuja PK contém a referência ao pai. A notação segue a [documentação de diagramas ER do Mermaid](https://mermaid.js.org/syntax/entityRelationshipDiagram.html).

<a id="organizacao"></a>

## Organização

**RF02.** Ver [modelo](Modelo_Organizacao_Horta.md).

```mermaid
erDiagram
    HORTA {
        uuid id PK
        string codigo UK
        string nome
    }
    SETOR {
        uuid id PK
        uuid horta_id FK
        string codigo
    }
    CANTEIRO {
        uuid id PK
        uuid horta_id FK
        string codigo
    }
    CONFIGURACAO_CANTEIRO {
        uuid id PK
        uuid canteiro_id FK
        decimal area_cultivavel_m2
        date vigente_de
        date vigente_ate "opcional"
    }
    VINCULO_CANTEIRO_SETOR {
        uuid id PK
        uuid canteiro_id FK
        uuid setor_id FK
        date vigente_de
        date vigente_ate "opcional"
    }
    HORTA ||..o{ SETOR : organiza
    HORTA ||..o{ CANTEIRO : possui
    CANTEIRO ||..o{ CONFIGURACAO_CANTEIRO : tem_configuracao
    CANTEIRO ||..o{ VINCULO_CANTEIRO_SETOR : tem_vigencia
    SETOR ||..o{ VINCULO_CANTEIRO_SETOR : recebe
```

Códigos de setor/canteiro são únicos no contexto da horta, não isoladamente. Vigências válidas não se sobrepõem para o mesmo canteiro, conforme a hipótese de um setor por data.

## Cultivo

**RF03–RF05, RF08 e RF09.** Ver [modelo](Modelo_Cultivo_Producao.md).

```mermaid
erDiagram
    ESPECIE {
        uuid id PK
        string nome_comum
        string nome_cientifico "opcional"
    }
    CULTIVAR {
        uuid id PK
        uuid especie_id FK
        string nome
    }
    LOTE_PROPAGACAO {
        uuid id PK
        uuid horta_id FK
        uuid especie_id FK
        uuid cultivar_id FK "opcional"
        string origem "opcional"
        date recebido_em "opcional"
    }
    CICLO_CULTIVO {
        uuid id PK
        uuid canteiro_id FK
        uuid configuracao_canteiro_id FK
        uuid especie_id FK
        uuid cultivar_id FK "opcional"
        date iniciado_em
        date encerrado_em "opcional"
        decimal area_ocupada_m2
    }
    PLANTIO {
        uuid id PK
        uuid ciclo_id FK
        uuid lote_propagacao_id FK "opcional"
        uuid plantio_reposto_id FK "opcional"
        date data
        enum natureza
        int quantidade_plantada
    }
    AVALIACAO_PEGAMENTO {
        uuid id PK
        uuid plantio_id FK
        date data
        int quantidade_sobrevivente
        string metodo
    }
    OBSERVACAO_AGRONOMICA {
        uuid id PK
        uuid ciclo_id FK
        date data
        string descricao
        decimal medida "opcional"
        string unidade "opcional"
        string metodo "opcional"
    }
    COLHEITA {
        uuid id PK
        uuid ciclo_id FK
        date data
        decimal massa_fresca_kg
        decimal massa_aproveitavel_kg "opcional"
    }
    LOTE_SECAGEM {
        uuid id PK
        uuid horta_id FK
        string codigo
        date iniciado_em
        date encerrado_em "opcional"
        decimal massa_seca_total_kg "opcional"
    }
    ITEM_SECAGEM {
        uuid id PK
        uuid lote_secagem_id FK
        uuid colheita_id FK
        decimal massa_destinada_kg
        decimal massa_seca_atribuida_kg "opcional"
    }
    ESPECIE ||..o{ CULTIVAR : possui
    HORTA ||..o{ LOTE_PROPAGACAO : recebe
    ESPECIE ||..o{ LOTE_PROPAGACAO : identifica
    CULTIVAR |o..o{ LOTE_PROPAGACAO : especifica
    CANTEIRO ||..o{ CICLO_CULTIVO : abriga
    CONFIGURACAO_CANTEIRO ||..o{ CICLO_CULTIVO : referencia_area
    ESPECIE ||..o{ CICLO_CULTIVO : identifica
    CULTIVAR |o..o{ CICLO_CULTIVO : especifica
    CICLO_CULTIVO ||..o{ PLANTIO : registra
    LOTE_PROPAGACAO |o..o{ PLANTIO : origina
    PLANTIO |o..o{ PLANTIO : reposto_por
    PLANTIO ||..o{ AVALIACAO_PEGAMENTO : avaliado_em
    CICLO_CULTIVO ||..o{ OBSERVACAO_AGRONOMICA : observado_em
    CICLO_CULTIVO ||..o{ COLHEITA : produz
    HORTA ||..o{ LOTE_SECAGEM : processa
    LOTE_SECAGEM ||..o{ ITEM_SECAGEM : recebe
    COLHEITA ||..o{ ITEM_SECAGEM : fornece
```

Proposta de unicidade do item: `(lote_secagem_id, colheita_id)`. Total seco do lote e atribuições de origem não são parcelas a somar juntas. A identidade dos pais é preservada ao corrigir dados.

## Ambiente

**RF06 e RF07.** Ver [modelo](Modelo_Ambiente_Irrigacao.md).

```mermaid
erDiagram
    VARIAVEL_AMBIENTAL {
        uuid id PK
        string codigo UK
        string grandeza
        string unidade_referencia
    }
    PONTO_COLETA {
        uuid id PK
        uuid horta_id FK
        uuid canteiro_id FK "opcional"
        uuid setor_id FK "opcional"
        string codigo
    }
    MEDICAO_AMBIENTAL {
        uuid id PK
        uuid ponto_coleta_id FK
        uuid variavel_ambiental_id FK
        uuid vinculo_canteiro_setor_id FK "opcional"
        datetime coletado_em
        decimal valor
        string unidade
        string metodo
        enum qualidade
    }
    IRRIGACAO {
        uuid id PK
        uuid setor_id FK
        datetime iniciado_em
        datetime encerrado_em "opcional"
        decimal duracao_ativa_min "opcional"
        decimal volume_l "opcional"
        decimal vazao_l_min "opcional"
        enum forma_obtencao_volume "opcional"
    }
    IRRIGACAO_CANTEIRO {
        uuid irrigacao_id PK, FK
        uuid canteiro_id PK, FK
        uuid vinculo_canteiro_setor_id FK
    }
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
```

Ponto possui no máximo um contexto canteiro/setor. O vínculo histórico da medição só se aplica ao ponto de canteiro. Volume fica apenas em `IRRIGACAO`.

## Atividades

**RF10, RF11 e RF15.** Ver [modelo](Modelo_Atividades_Ocorrencias.md).

```mermaid
erDiagram
    ATIVIDADE {
        uuid id PK
        uuid horta_id FK
        uuid canteiro_id FK "opcional"
        uuid setor_id FK "opcional"
        uuid ciclo_id FK "opcional"
        uuid ocorrencia_id FK "opcional"
        date data
        boolean dimensao_operacional
        boolean dimensao_social
        boolean dimensao_educacional
        int publico_total_informado "opcional"
        boolean lista_publico_completa "opcional"
    }
    PARTICIPANTE {
        uuid id PK
        uuid horta_id FK
        string nome_exibicao
    }
    ATIVIDADE_PARTICIPANTE {
        uuid atividade_id PK, FK
        uuid participante_id PK, FK
        decimal horas_trabalho "opcional"
        boolean compoe_publico
    }
    INSTITUICAO {
        uuid id PK
        string nome
    }
    ATIVIDADE_INSTITUICAO {
        uuid atividade_id PK, FK
        uuid instituicao_id PK, FK
    }
    INSUMO {
        uuid id PK
        string nome
        string unidade_referencia
    }
    USO_INSUMO {
        uuid id PK
        uuid atividade_id FK
        uuid insumo_id FK
        decimal quantidade
        string unidade
    }
    OCORRENCIA {
        uuid id PK
        uuid horta_id FK
        uuid canteiro_id FK "opcional"
        uuid setor_id FK "opcional"
        uuid ciclo_id FK "opcional"
        uuid irrigacao_id FK "opcional"
        datetime aberta_em
        datetime resolvida_em "opcional"
        enum situacao
    }
    HORTA ||..o{ ATIVIDADE : organiza
    HORTA ||..o{ PARTICIPANTE : cadastra
    HORTA ||..o{ OCORRENCIA : acompanha
    CANTEIRO |o..o{ ATIVIDADE : contextualiza
    SETOR |o..o{ ATIVIDADE : contextualiza
    CICLO_CULTIVO |o..o{ ATIVIDADE : contextualiza
    CANTEIRO |o..o{ OCORRENCIA : contextualiza
    SETOR |o..o{ OCORRENCIA : contextualiza
    CICLO_CULTIVO |o..o{ OCORRENCIA : contextualiza
    IRRIGACAO |o..o{ OCORRENCIA : contextualiza
    OCORRENCIA |o..o{ ATIVIDADE : atendida_por
    ATIVIDADE ||--o{ ATIVIDADE_PARTICIPANTE : envolve
    PARTICIPANTE ||--o{ ATIVIDADE_PARTICIPANTE : participa
    ATIVIDADE ||--o{ ATIVIDADE_INSTITUICAO : envolve
    INSTITUICAO ||--o{ ATIVIDADE_INSTITUICAO : participa
    ATIVIDADE ||..o{ USO_INSUMO : utiliza
    INSUMO ||..o{ USO_INSUMO : utilizado_em
```

Contextos opcionais são alternativas. Vínculo com ocorrência é atendimento, não outro evento duplicado. Público informado inclui a parte nominal elegível; horas pertencem à associação individual.

## Acesso

**RF01.** Ver [modelo transversal](Modelo_Acesso_Historico_Evidencias.md).

```mermaid
erDiagram
    USUARIO {
        uuid id PK
        string identificador_autenticacao UK
        uuid participante_id FK, UK "opcional"
        boolean ativo
    }
    PERFIL {
        uuid id PK
        string codigo UK
    }
    PERMISSAO {
        uuid id PK
        string codigo UK
    }
    PERFIL_PERMISSAO {
        uuid perfil_id PK, FK
        uuid permissao_id PK, FK
    }
    USUARIO_PERFIL {
        uuid usuario_id PK, FK
        uuid perfil_id PK, FK
        uuid horta_id PK, FK
    }
    PARTICIPANTE |o..o| USUARIO : pode_ter_conta
    USUARIO ||--o{ USUARIO_PERFIL : recebe
    PERFIL ||--o{ USUARIO_PERFIL : concede
    HORTA ||--o{ USUARIO_PERFIL : delimita
    PERFIL ||--o{ PERFIL_PERMISSAO : possui
    PERMISSAO ||--o{ PERFIL_PERMISSAO : autoriza
```

## Evidências

**RF12.** Ver [modelo transversal](Modelo_Acesso_Historico_Evidencias.md).

```mermaid
erDiagram
    direction LR
    EVIDENCIA {
        uuid id PK
        uuid horta_id FK
        uuid registrado_por_id FK
        datetime registrado_em
        string referencia_armazenamento
        string tipo_conteudo
        long tamanho_bytes
    }
    VINCULO_EVIDENCIA {
        uuid id PK
        uuid evidencia_id FK
        uuid horta_id FK "alternativa"
        uuid setor_id FK "alternativa"
        uuid canteiro_id FK "alternativa"
        uuid lote_propagacao_id FK "alternativa"
        uuid ciclo_id FK "alternativa"
        uuid plantio_id FK "alternativa"
        uuid avaliacao_pegamento_id FK "alternativa"
        uuid medicao_ambiental_id FK "alternativa"
        uuid observacao_agronomica_id FK "alternativa"
        uuid colheita_id FK "alternativa"
        uuid lote_secagem_id FK "alternativa"
        uuid irrigacao_id FK "alternativa"
        uuid atividade_id FK "alternativa"
        uuid ocorrencia_id FK "alternativa"
        uuid lancamento_financeiro_id FK "alternativa"
    }
    HORTA ||..o{ EVIDENCIA : guarda
    USUARIO ||..o{ EVIDENCIA : registra
    EVIDENCIA ||..o{ VINCULO_EVIDENCIA : apoia
    HORTA |o..o{ VINCULO_EVIDENCIA : alvo
    SETOR |o..o{ VINCULO_EVIDENCIA : alvo
    CANTEIRO |o..o{ VINCULO_EVIDENCIA : alvo
    LOTE_PROPAGACAO |o..o{ VINCULO_EVIDENCIA : alvo
    CICLO_CULTIVO |o..o{ VINCULO_EVIDENCIA : alvo
    PLANTIO |o..o{ VINCULO_EVIDENCIA : alvo
    AVALIACAO_PEGAMENTO |o..o{ VINCULO_EVIDENCIA : alvo
    MEDICAO_AMBIENTAL |o..o{ VINCULO_EVIDENCIA : alvo
    OBSERVACAO_AGRONOMICA |o..o{ VINCULO_EVIDENCIA : alvo
    COLHEITA |o..o{ VINCULO_EVIDENCIA : alvo
    LOTE_SECAGEM |o..o{ VINCULO_EVIDENCIA : alvo
    IRRIGACAO |o..o{ VINCULO_EVIDENCIA : alvo
    ATIVIDADE |o..o{ VINCULO_EVIDENCIA : alvo
    OCORRENCIA |o..o{ VINCULO_EVIDENCIA : alvo
    LANCAMENTO_FINANCEIRO |o..o{ VINCULO_EVIDENCIA : alvo
```

Exatamente uma das 15 FKs de destino é preenchida por vínculo. Evidência e destino pertencem à mesma horta. A nulabilidade individual não permite vínculo sem alvo.

## Histórico

**RF13.** Ver [modelo transversal](Modelo_Acesso_Historico_Evidencias.md).

```mermaid
erDiagram
    AUDITORIA {
        uuid id PK
        uuid horta_id FK "opcional_para_cadastro_global"
        uuid usuario_id FK
        string tipo_registro
        json chave_registro
        enum operacao
        string motivo "quando aplicavel"
        datetime ocorrido_em
        json valores_anteriores "opcional"
        json valores_novos "opcional"
    }
    HORTA |o..o{ AUDITORIA : contexto
    USUARIO ||..o{ AUDITORIA : autoria
```

Tipo/chave identificam o alvo no histórico, inclusive chave composta; não são FK para uma entidade fictícia de registros. A operação e a auditoria são confirmadas juntas. Fatos da horta exigem contexto; identidade/catálogo global pode não tê-lo, com acesso administrativo específico.

## Custos

**RF16.** Ver [modelo](Modelo_Custos.md).

```mermaid
erDiagram
    LANCAMENTO_FINANCEIRO {
        uuid id PK
        uuid horta_id FK
        uuid canteiro_id FK "opcional"
        uuid ciclo_id FK "opcional"
        date data
        string categoria
        decimal valor_brl
        enum natureza
    }
    HORTA ||..o{ LANCAMENTO_FINANCEIRO : registra
    CANTEIRO |o..o{ LANCAMENTO_FINANCEIRO : contextualiza
    CICLO_CULTIVO |o..o{ LANCAMENTO_FINANCEIRO : contextualiza
```

Canteiro e ciclo são alternativas de contexto, não campos a preencher juntos. Previsto/realizado são naturezas diferentes. RF14 usa essas entidades e os demais fatos conforme [Dados dos indicadores](Dados_Indicadores.md), sem tabela de valores finais de dashboard.
