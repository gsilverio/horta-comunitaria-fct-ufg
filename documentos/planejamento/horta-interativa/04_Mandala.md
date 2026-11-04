# Etapa 4 — Gerador parametrizado de mandalas

[Índice e sequência](README.md) | [Análise da codebase](Analise_Coerencia.md)

## Recorte revisado para este projeto

Mandala é uma evolução opcional da experiência. Não exigir que o usuário conclua o gerador para avaliar o canvas. O recorte implementado é um preset local editável, sem seed, API ou criação de cadastros.

**Status:** primeira experimentação local implementada no editor E1. O padrão do gerador é um centro circular com três canteiros curvilíneos individuais, representados como setores de anel. O usuário pode escolher o formato retangular como configuração própria e ajustar divisões/anéis nesse modo. Tudo continua local e sem persistência.

## Conteúdo da proposta recebida

**Objetivo:** criar layouts radiais sem exigir desenho manual de cada canteiro.

### Parâmetros candidatos

- Centro da mandala `(x, y)`.
- Raio total.
- Número de divisões radiais.
- Número de anéis/faixas de plantio.
- Raio ou área central reservada.
- Largura dos caminhos radiais.
- Largura dos caminhos circulares ou distância entre anéis.
- Orientação/rotação global.

### Tarefas

- [x] Desenhar uma pré-visualização a partir dos parâmetros.
- [x] Gerar formas usando funções de geometria puras.
- [x] Permitir confirmar ou cancelar a geração antes de alterar o estado do editor.
- [x] Definir estratégia de nomeação automática para setores e canteiros.
- [x] Permitir excluir ou alterar individualmente partes da mandala gerada.
- [x] Validar raios, número de peças e limites da horta.
- [x] Evitar criar setores/canteiros duplicados ao repetir a geração, substituindo o rascunho somente após confirmação.
- [x] Definir comportamento quando a horta já contém objetos: a primeira experimentação substitui o rascunho após confirmação.
- [x] Criar um **preset demonstrativo** com três canteiros curvilíneos, mantendo o formato retangular como opção configurável.

**Entregável:** assistente “Gerar mandala” com prévia e personalização.

**Critérios de conclusão:**

- Uma mandala editável é gerada usando os parâmetros escolhidos.
- Suas partes são objetos individuais, selecionáveis e editáveis.
- A geometria não depende de imagem estática ou grid quadrado.
- O usuário consegue reorganizar canteiros após gerar a mandala.

**Nota geométrica:** a área de um segmento de anel sem recortes adicionais é `A = (θ / 2) × (R² - r²)`, com `θ` em radianos. Se forem abertos caminhos ou aplicados outros recortes, a área líquida deverá considerar as geometrias efetivamente resultantes.

## Limite conhecido desta experimentação

A geometria curvilínea é uma aproximação visual e de seleção por setores de anel, com área calculada pela fórmula de segmento anular. O modo retangular continua disponível para comparar layouts ou atender uma configuração diferente. Persistência, caminhos e recortes de circulação permanecem fora desta experimentação.

---
