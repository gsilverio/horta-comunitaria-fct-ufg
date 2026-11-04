# Modelo de organização da horta

**Cobertura:** RF02, com contexto para RF04, RF06, RF07 e RF13. Ver [convenções comuns](Modelagem_Preliminar_Dados_Horta.md) e [diagramas](Diagramas_Relacionais_Horta.md#organizacao).

O fluxo começa pelo cadastro da horta, seus setores e canteiros. A identidade do canteiro permanece a mesma quando sua dimensão ou associação de setor muda.

## Entidades e informações

| Entidade | Campos propostos, além de `id` | Relações |
| --- | --- | --- |
| `horta` | `codigo`, `nome`, `localizacao?`, `area_institucional_m2?`, `area_operacional_m2?`, `area_experimental_m2?`, `ativo` | Origem dos cadastros e fatos locais |
| `setor` | `horta_id`, `codigo`, `nome`, `descricao?`, `ativo` | Uma horta; vários vínculos de canteiros e irrigações |
| `canteiro` | `horta_id`, `codigo`, `nome`, `localizacao?`, `ativo` | Uma horta; várias configurações, vínculos e ciclos |
| `configuracao_canteiro` | `canteiro_id`, `comprimento_m?`, `largura_m?`, `area_cultivavel_m2`, `vigente_de`, `vigente_ate?` | Um canteiro; referência de vários ciclos |
| `vinculo_canteiro_setor` | `canteiro_id`, `setor_id`, `vigente_de`, `vigente_ate?` | Um canteiro e um setor; referência histórica de registros de campo |

Áreas e dimensões são decimais; vigências são datas nesta proposta. A área cultivável representa a área efetiva informada, sem supor que todo canteiro seja retangular ou que toda a área institucional esteja cultivada.

## Relações e integridade

- Uma horta possui zero ou muitos setores e canteiros. O cadastro pode ser construído gradualmente.
- Códigos de setor e canteiro são únicos dentro da horta; código da horta identifica seu cadastro.
- Cada configuração pertence a um único canteiro. Configurações válidas não se sobrepõem no período.
- Cada vínculo associa um canteiro a um setor da mesma horta. A proposta considera no máximo um setor por canteiro em uma data, permitindo um intervalo sem setor ainda definido.
- `vigente_ate`, quando preenchido, é posterior a `vigente_de`. Encerrar o vínculo antigo e abrir o novo preserva as referências anteriores.
- Área cultivável e dimensões conhecidas precisam ser positivas. Diferenças entre área efetiva e produto das dimensões devem ser explicáveis, sem assumir uma fórmula obrigatória para qualquer geometria.
- Um ciclo referencia a configuração usada como base e guarda sua própria área ocupada. A alteração posterior do canteiro não muda retroativamente o denominador do cultivo.

## Aplicação no fluxo

1. Cadastrar canteiro e configuração inicial.
2. Associá-lo ao setor com início de vigência conhecido.
3. Abrir ciclos e registrar fatos usando esse contexto.
4. Em uma mudança, encerrar a configuração/associação anterior e criar a sucessora.
5. Em uma correção do passado, revisar os fatos afetados e registrar o motivo no histórico; não tratar a correção como uma simples mudança futura.

**Exemplo:** um canteiro passa do setor A para B em 10/11. A irrigação de 09/11 continua no setor A; a mudança não aumenta ou transfere o consumo histórico para B.

## Decisões a validar

- Vigência por dia é suficiente ou mudanças no mesmo dia precisam de horário?
- Quais áreas e dimensões serão efetivamente coletadas?
- Um canteiro pode pertencer a vários setores simultaneamente? Esta proposta assume que não.
- Como registrar ampliação de área durante um ciclo e consórcios? A política não está definida.

As referências do anexo — inclusive áreas, número de setores e dimensões dos canteiros — orientam o planejamento e não restringem o cadastro.
