# Catálogo inicial Embrapa Hortaliças

Decisão do responsável em 24/10/2026: preparar catálogo inicial a partir de *Cultivares da Embrapa Hortaliças (1981–2023)*, ISBN 978-65-89957-98-0, com imagens obtidas pela API pública do Commons. A entrega prepara dados e API para o catálogo visual; a seleção com fotografias está integrada ao painel lateral de canteiros no modo interativo. A evolução posterior de sprites e fases na horta está em [Cultivos no panorama](Cultivos_no_Panorama.md).

## Conteúdo

113 cultivares das fichas das páginas 10–216 e 220–236, incluindo parcerias com outras unidades. A página 218 é divisória. São 23 entradas em `species`: espécies, grupos de cultivo da mesma espécie (brócolis/repolho) e um híbrido interespecífico; não são 23 espécies taxonômicas distintas.

Pimentas separadas em `Capsicum annuum`, `C. chinense` e `C. baccatum`. BRS Acará é porta-enxerto de `C. annuum`. Jabras é abóbora híbrida `Cucurbita maxima × C. moschata`. Gisilba é `Sinapis alba`, conforme a página 144. Nomes científicos ausentes nas fichas são identificações editoriais dos grupos, sujeitas a revisão agronômica.

O sumário preserva as 113 fichas; cabeçalhos corrigem abreviações BRS nas batatas. `Cucurbita` normaliza a grafia “Curcubita” da página 10. Não importar recomendações agronômicas como regras. A página 237 repete conteúdo de batata-doce na ficha de cebola; esse texto não foi importado. Fotografias e descrições extensas do livro não são reproduzidas.

[Manifesto editorial](../../HortaComunitaria.Api/Infrastructure/Persistence/Seeds/Data/embrapa-catalog.json): páginas das fichas e SHA-256 do PDF recebido. [Fonte Embrapa](https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1156800/1/livro-cultivares-embrapa-hortalicas.pdf). [Referência complementar de BRS Acará](https://www.embrapa.br/en/busca-de-solucoes-tecnologicas/-/produto-servico/5381/porta-enxerto-hibrido-para-pimentao-brs-acara).

## Imagens

Veja a [relação de entradas, quantidade de cultivares e créditos](Catalogo_Embrapa_Creditos.md).

[Pacote offline](../../HortaComunitaria.Api/Infrastructure/Persistence/Seeds/Data/commons-images.json): 23 JPEGs revisados, até 480 × 480 px e 180.000 bytes por imagem; cerca de 958 KB binários no total. Base64 custa aproximadamente 33% adicionais. Persistência na linha `species`, metadados em JSONB. Exceção restrita ao catálogo, sem mudar a política de uploads/evidências.

Metadados preservados: título, autor em texto simples, licença e URL, página Commons, download, data da coleta, transformações e SHA-256 dos bytes armazenados. Somente CC0, domínio público e versões CC BY / CC BY-SA explicitamente admitidas pelo script. Fotos CC BY-SA mantêm sua licença; redimensionamento e recompressão estão declarados.

O frontend deve mostrar autor, licença com link, origem e indicação de foto ilustrativa do grupo, sem afirmar identidade da cultivar. Crédito/licença também acompanham exportações. Não renderizar metadados como HTML.

## Execução

O seed não roda no startup, em migration ou hosted service; não consulta internet. JSONs são recursos incorporados no assembly publicado.

1. Configurar conexão conforme [Execução local](Execucao_Local.md).
2. Aplicar migrations explicitamente. `AddEmbrapaCatalogImages` altera só o esquema.
3. Criar o primeiro administrador conforme [Get Started](../Get_Started.md); a importação é atribuída ao Adm ativo mais antigo e não cria usuários/credenciais.
4. Executar:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet ef database update --project HortaComunitaria.Api
dotnet run --project HortaComunitaria.Api --no-launch-profile -- --seed-embrapa
```

Artefato publicado: `dotnet HortaComunitaria.Api.dll --seed-embrapa`, com conexão no ambiente. O comando não abre HTTP nem inicia hosted services; migrations são uma etapa anterior. A preparação serve ao deploy futuro. A API mantém sua restrição a Development enquanto não houver autenticação; autoria demonstrativa precisa ser revista antes do uso operacional.

Transação e lock compartilhado com os cadastros protegem a carga. Chaves editoriais únicas `embrapa-2023:<grupo>` e `embrapa-2023:page-<página>` preservam idempotência após renomeação/inativação. Reexecutar insere apenas ausentes; não atualiza ou reativa registros. Homônimo manual por nome comum/científico exige conciliação e causa rollback integral. Não há conciliação automática nesta versão.

Inclusões têm auditoria transacional com autor e instante UTC; registra identidade editorial e hash, sem duplicar Base64 no histórico/logs. Mudanças futuras em fotos/carga exigem atualização explícita; repetir o seed não altera cadastros existentes.

## API do catálogo visual

- `GET /api/v1/species` e `GET /api/v1/species/{id}`: acrescentam `imageUrl` opcional, sem Base64 na listagem.
- `GET /api/v1/species/{id}/image`: JSON com `base64`, `mimeType`, `sourceUrl`, `title`, `author`, `license`, `licenseUrl`, `attribution`, `changes`, `sha256`. Retorna 404 se espécie/imagem ausente.
- Consulta de cultivares mantém filtro por espécie; cultivar segue opcional.

Exibir com `data:${mimeType};base64,${base64}`. Buscar conforme os cartões entram em exibição e manter em memória no cliente.

## Reprodução

`tools/catalog/extract_catalog.py <pdf>` requer `pypdf` e reconstrói o manifesto. PDF permanece fora do repositório; conferir seu hash.

`tools/catalog/fetch_commons_images.py [chave ...]` requer Pillow e usa a [Action API](https://commons.wikimedia.org/w/api.php). Preserva imagens existentes; remover deliberadamente uma chave para substituí-la. Usa User-Agent, chamadas sequenciais, `maxlag`, allowlist de hosts, limite de resposta e retry limitado para 429/502/503/504. Respeitar a [etiqueta da API](https://www.mediawiki.org/wiki/API:Etiquette).

Gera `tmp/catalog/commons-review.jpg`. Resultados novos têm `reviewed: false`; conferir foto/créditos antes de marcar. Seed rejeita imagens sem revisão ou hash divergente. Temporários não são versionados.

## Validação

Build Release sem warnings. Doze testes de integração aprovados com PostgreSQL 17 real em bancos descartáveis, incluindo importação de 23 entradas/113 cultivares, auditoria de 136 inclusões, repetição sem duplicação, preservação de renomeações/inativação, rollback por conflito manual, retorno da imagem com hash/créditos e regressões dos cadastros existentes. Em 25/10/2026, a carga foi aplicada explicitamente ao banco local de demonstração, após autorização para remover a Alface manual e sua variedade, ambas sem vínculos de cultivo ou lotes. Foram conferidas 23 entradas com fotos e 113 cultivares. A seleção no frontend é paginada no painel lateral do editor; deploy operacional permanece fora desta etapa.
