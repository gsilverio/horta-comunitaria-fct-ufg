# Contexto da Horta Comunitária FCT/UFG

Leia este contexto antes de consultar os catálogos da skill. As recomendações de busca auxiliam as decisões; os contratos existentes, as regras de arquitetura e a solicitação atual continuam determinando a implementação.

## Decisões confirmadas em 06/11/2026

- Quem guia as decisões de UX nesta fase: quem está desenvolvendo. Isso define a interlocução de design; não significa que o público final seja composto apenas por desenvolvedores. Perfis de uso final e aceite funcional continuam nos requisitos e nas validações da PO.
- Direção: visual administrativo sóbrio e compacto, mantendo as cores da horta.
- Prioridade: desktop para gestão, com todas as tarefas também utilizáveis no celular.
- Preferências já explicitadas: menos cartões, formulários claros e consistência entre listas, detalhes e navegação. Não reproduzir o tema ou a arquitetura visual do EasyDFe.

## Fontes e stack

- Arquitetura: `.codex/arquitetura-frontend.mdc`, espelhada em `.claude/rules/arquitetura-frontend.md`. Leia integralmente antes de planejar ou implementar frontend. Para mudanças de contrato, leia também a arquitetura do backend.
- Contexto funcional: `documentos/Documento_Tecnico_Requisitos_Horta.md`, `documentos/planejamento/frontend/README.md` e o plano do fluxo afetado.
- Estado visual: `hortacomunitaria-webapp/src/styles.css`, `src/components/` e os componentes da feature afetada. Os caminhos `src/` desta referência são relativos a `hortacomunitaria-webapp/`.
- React 19, TypeScript estrito, Vite e Tailwind 4. Navegação hash e cliente HTTP existentes. Consultas de stack usam `react` e, quando necessário, `html-tailwind`; exemplos Next.js da skill não são a stack deste projeto.
- Componentes Metronic estão disponíveis em `src/components/ui`, `data-grid`, `helpers`, `libs`, `image-input` e `stepper`. Reutilize-os quando adequados; não instale outro kit ou copie estilos do projeto doador por conveniência. Atualmente somente a listagem de hortas aplica o DataGrid.

## Critérios para decidir a interface

- Antes de mudar uma tela, identifique tarefa principal, contexto da horta, entrada, saída e estados de carregamento, vazio, erro, gravação e conflito. Preserve os percursos já existentes.
- Use cabeçalhos claros, uma ação principal e agrupamento por tarefa. Prefira linhas, divisórias e seções a um cartão por atributo. Cards continuam possíveis quando agrupam conteúdo com função real.
- Compacto significa reduzir espaço desperdiçado, não reduzir legibilidade, alvo de toque ou foco. Preserve controles de pelo menos 44 px e leitura confortável, incluindo teclado e zoom.
- Listagens de dados usam paginação e filtros reais da API. Não aplique ordenação/busca apenas sobre a página atual como se abrangesse o catálogo inteiro.
- Formulários agrupam campos por assunto, exibem unidades, erros locais e ações previsíveis. Mantenha proteção de rascunho, motivo, histórico e versão esperada.
- O header contém a identificação demonstrativa à direita e o contexto da horta ao lado. O seletor aparece com múltiplas hortas; com uma, a aplicação usa a única disponível. Horta no menu lateral abre a gestão de hortas. Preserve a navegação global e os acessos diretos.
- Layout desktop prioritário com adaptação completa para 320/390/768 px; confira também desktop em 1280/1920 px quando relevante. Tabelas podem ter rolagem interna identificada; a página não deve transbordar.
- Reutilize tokens `horta-*` de `styles.css`: forest para identidade/ações, canvas para fundo, ink para texto, muted para secundário, line para divisórias, soft para ênfase, danger para risco e focus para foco. Verifique contraste no uso concreto. Não impor paletas, fontes remotas ou modo escuro sugeridos automaticamente.
- Use os ícones existentes; Lucide já está disponível. Mantenha semântica, espessura e tamanhos coerentes. Fotos das espécies vêm da API existente; não invente URLs ou substitua os assets do panorama.
- Animações devem ajudar a compreensão e respeitar movimento reduzido. Prefira CSS/Tailwind para transições simples; uma busca que retorna GSAP não autoriza instalar GSAP.

## Limites do domínio e do escopo

- Percurso operacional: horta → setor → canteiro → cultivo/plantio. A interface registra fatos manuais e consulta informações retornadas pela API.
- Autenticação é pós-MVP; a identidade atual é Operador de demonstração. Não gerar login, perfil, sair ou sistema de usuários.
- Ausência de dado não equivale a zero. Não calcular indicadores agrícolas no browser, presumir fórmulas, datas agronômicas ou converter unidades livres do desenho em m².
- Reestruturação visual tradicional não inclui alterar cadastro interativo, geometria, câmera, assets ou lógica panorâmica. Uma mudança nessas áreas deve estar explicitamente no pedido atual.
- Não executar escrita no banco, preparar demonstração, aplicar migrations ou seed apenas para obter uma captura. Dados reais e demonstrativos têm autorização e finalidade próprias.

## Como usar a busca neste projeto

Resolva o caminho de `scripts/search.py` dentro da pasta desta skill. Exemplos a partir da raiz do repositório:

```powershell
py -3 .claude/skills/ui-ux-pro-max/scripts/search.py "admin compact navigation" --domain ux
py -3 .claude/skills/ui-ux-pro-max/scripts/search.py "form error validation" --stack react
py -3 .claude/skills/ui-ux-pro-max/scripts/search.py "responsive table layout" --stack html-tailwind
```

Para uma direção visual ampla, use `--design-system` como proposta e confronte o resultado com os tokens e decisões acima. Não substitua a identidade por uma recomendação automática nem gere uma landing page de marketing para um fluxo administrativo. Não use `--persist` por padrão; decisões aprovadas pertencem à documentação em `documentos/planejamento/frontend/`. Se a persistência do gerador for solicitada, use destino explícito e não trate o arquivo gerado como fonte superior à arquitetura ou ao contexto aprovado.

Leia `quick-reference.md` somente nos tópicos relevantes. `pro-rules.md` contém recomendações de aplicativos nativos: aplique apenas as compatíveis com web, sem importar convenções de iOS/Android ou exigir dependências nativas.

## Verificação

Para mudanças de UI, execute build/lint e confira interação, responsividade, teclado e estados afetados. A dispensa de integração desta fase está em `.claude/CLAUDE.md`; não afirme integração validada sem executá-la. Em tarefas apenas de documentação/configuração da skill, verifique referências, sincronização e funcionamento da busca; não precisa reconstruir a aplicação.
