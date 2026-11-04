# Etapa 1.5 — Projeção do cadastro no mapa

Esta etapa corrige a separação entre o cadastro administrativo e o rascunho visual sem criar ainda a persistência do layout.

## Hierarquia usada

O mapa recebe uma horta já cadastrada, consulta seus setores ativos e consulta os canteiros com o contexto vigente. A relação visual é:

```text
Horta
└── Setor(es) cadastrado(s)
    └── Canteiro(s) associado(s) na data consultada
```

O centro da mandala é uma referência geométrica. Ele não cria nem representa um setor. A mandala continua sendo um preset local que pode organizar canteiros existentes ou criar peças do rascunho.

## Regra de projeção atual

- A área da horta é a soma das áreas institucional, operacional e experimental informadas no cadastro.
- Como o cadastro atual não possui largura, comprimento ou contorno da horta, o mapa usa um envelope quadrado com lado `raiz quadrada da área`. Isso preserva a escala de área em metros, mas não afirma que o terreno é quadrado.
- Um canteiro usa comprimento e largura cadastrados quando ambos existem.
- Quando o canteiro só possui área cultivável, sua dimensão visual é estimada como um quadrado. A tela identifica essa estimativa.
- O setor não possui área nem geometria no domínio atual. O retângulo tracejado do setor é um envelope calculado a partir dos canteiros vinculados e é marcado como projeção.
- Se a soma das áreas dos canteiros exceder a área da horta, a tela mantém as dimensões cadastradas e sinaliza a inconsistência. Ela não reduz silenciosamente os canteiros.

## Limites desta etapa

- A leitura é real, mas a posição, a rotação e a forma editada continuam no `sessionStorage` do navegador.
- Ainda não há endpoint de layout, revisão concorrente, autorização de edição espacial ou transação para geração de mandala. Esses itens permanecem na [Etapa 6](06_Persistencia_Futura.md).
- Para uma projeção física fiel, o próximo contrato precisa definir as dimensões/contorno da horta e a geometria/área dos setores. Enquanto isso, o mapa deve chamar esses envelopes de projeções, não de limites cadastrais.

## Critérios de verificação

- Alterar a área cadastrada altera o envelope e invalida um rascunho salvo em escala diferente.
- Alterar dimensões, área ou associação de um canteiro também invalida o rascunho local daquela projeção, evitando exibir uma medida antiga como se fosse atual.
- Um canteiro configurado com comprimento e largura mantém essas medidas no mapa.
- Um canteiro apenas com área aparece como estimado.
- Canteiros ficam agrupados pelo setor vigente; os sem associação aparecem em “Sem setor associado”.
- A visão panorâmica reutiliza a mesma projeção e enquadra o envelope completo.
