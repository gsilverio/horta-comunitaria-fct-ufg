# Experimento inicial: mudar a experiência visual

[Índice](README.md) | [Parecer técnico](Analise_Coerencia.md)

## Pergunta que queremos responder

É mais compreensível organizar a horta por um mapa com objetos selecionáveis, medidas e painel de propriedades dentro da aplicação atual?

O experimento foi implementado como o primeiro incremento desta revisão. O rascunho permanece local e demonstrativo; a persistência ainda não foi iniciada.

## Recorte E0

- Entrar em uma tela experimental a partir do contexto de uma horta, com indicação permanente de desenho demonstrativo não salvo.
- Exibir terreno retangular fictício de 20 × 20 m e um canteiro de 1 × 5 m. Esses números servem ao teste e não representam a implantação da FCT/UFG.
- Grade opcional, indicador de escala, ajustar desenho à tela, zoom limitado e pan.
- Selecionar o canteiro pelo desenho ou por uma lista HTML; mover por arraste e coordenadas numéricas.
- Painel com posição e dimensões em metros; zoom/pan só alteram a câmera.
- Restaurar o exemplo e sair da tela com aviso de descarte quando houver alterações.

O ID da horta mantém o contexto de navegação; não transforma o desenho fictício em sua planta. Não carregar dados operacionais. O rascunho vive em memória, e recarregar a página restaura o exemplo, com esse comportamento comunicado.

## O que aproveitar

Reutilizar AppShell, Tailwind, navegação por hash, controles e proteção de rascunho existentes. Criar uma feature específica com cálculos puros separados dos componentes de canvas. Avaliar `react-konva`/Konva em versões compatíveis fixas, preservando React e os demais pacotes. Estado local primeiro.

Nenhuma alteração de backend, schema, API, cadastro, área cultivável, vínculo setorial ou indicador foi necessária para E0. Irrigação e sensores ficam fora. A tela usa SVG nativo para validar o comportamento sem bloquear a experiência por uma instalação de biblioteca gráfica; React Konva continua uma opção para uma etapa posterior, se o teste justificar.

## Implementação realizada

1. Coordenadas em metros, com origem no canto superior esquerdo, x para a direita e y para baixo; câmera com escala e deslocamento separados do modelo.
2. A tela foi ligada à aplicação pela rota contextual `#/hortas/{id}/horta/interativa`.
3. Terreno e canteiro são renderizados em SVG; a lista HTML e o painel numérico oferecem seleção alternativa.
4. Movimentação, restauração, descarte protegido por guarda de navegação, zoom, pan e grade foram implementados.
5. `npm run build` e `npm run lint` foram executados com sucesso. Ainda falta avaliação com usuário e inspeção manual em navegador.

## Roteiro de avaliação

| Ação | Evidência esperada |
| --- | --- |
| Abrir a experiência | Fica claro que é um desenho fictício não salvo |
| Selecionar pela forma e pela lista | Mesmo objeto e painel; seleção reconhecível sem depender apenas de cor |
| Mover para x=3 m, y=4 m | Desenho e campos concordam; medida segue 1 × 5 m |
| Aproximar, deslocar e afastar | Posição física e dimensão permanecem iguais |
| Ajustar pela entrada numérica | Operação possível sem arrastar |
| Redimensionar a janela | Canvas se adapta sem alterar o modelo |
| Sair com alteração e recusar descarte | Rascunho permanece |
| Restaurar/reabrir exemplo | Retorna ao estado demonstrativo e não altera cadastros |

Registrar por tarefa: concluiu sozinho/com ajuda/não concluiu, dificuldade percebida e observações. Não há meta de desempenho aprovada; observar fluidez com o exemplo e, depois, um cenário fictício de 20 canteiros.

## Estado após E1

O editor agora permite adicionar formas por arraste ou pelos botões de inserção, editar propriedades, girar retângulos com encaixe em ângulos retos, alinhar objetos por guias temporárias nos eixos X/Y, selecionar múltiplos objetos com Shift/Ctrl ou por janela de arraste, mover e girar a seleção em conjunto, alterar o raio de círculos, remover objetos, usar snap de 0,5 m e desfazer/refazer. Para mover o quadro em qualquer zoom, Espaço + arraste, Alt + arraste ou o botão do meio deslocam a vista. O botão **Gerar mandala** abre uma prévia parametrizada: o padrão é um centro circular com três canteiros curvilíneos, e o usuário pode escolher o modo retangular com suas próprias divisões e anéis. Ao confirmar, cada canteiro continua editável por centro, raio do anel, abertura angular, profundidade radial e ângulo. Sobreposições são indicadas visualmente no rascunho. Nenhuma dessas ações cria ou altera registros do backend.

## Quando avançar

Avançar para E2 quando seleção, câmera, medidas, criação, histórico e o gerador local de mandala forem compreensíveis e consistentes na avaliação com usuário. Curvas e setores de anel verdadeiros entram somente se a experimentação justificar essa ampliação. Falhas de precisão ou dificuldade de interação devem ser resolvidas antes de ampliar o conjunto de formas.

Ao final de E1, haverá evidência para decidir se o editor merece persistência e associação a cadastros. Isso será outro recorte; não inclui automaticamente operação, sensores ou irrigação.
