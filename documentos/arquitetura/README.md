# Arquitetura e modelagem

Esta pasta reúne a modelagem de dados, seus diagramas e as explicações das decisões técnicas do projeto.

- [Visão da modelagem](Modelagem_Preliminar_Dados_Horta.md): convenções, inventário das 37 entidades e cobertura dos requisitos RF01–RF16.
- [Diagrama relacional geral](Diagrama_Relacional_Geral.md): relações principais da base inteira.
- [Diagramas por domínio](Diagramas_Relacionais_Horta.md): chaves e cardinalidades detalhadas.
- [Organização da horta](Modelo_Organizacao_Horta.md).
- [Cultivo e produção](Modelo_Cultivo_Producao.md).
- [Ambiente e irrigação manual](Modelo_Ambiente_Irrigacao.md).
- [Atividades, participação e ocorrências](Modelo_Atividades_Ocorrencias.md).
- [Acesso, histórico e evidências](Modelo_Acesso_Historico_Evidencias.md).
- [Custos básicos](Modelo_Custos.md).
- [Dados de origem dos indicadores](Dados_Indicadores.md): fatos, recortes e exemplos a validar com a PO.

Os modelos são preliminares e se baseiam nos requisitos 0.0.1 e no anexo da professora. O primeiro fluxo de irrigação possui consultas dos cadastros de contexto, registro/consumo, correção/histórico e interface React/Tailwind; o restante do modelo continua preliminar. Os diagramas ficam nesta pasta; o documento de requisitos e suas versões PDF/DOCX são preservados.

- [Execução local](Execucao_Local.md): projetos, versões, configuração, migrations e evidências.
- [Deploy com Docker](Deploy_Docker.md): stack `horta-comunitaria-fct-ufg`, rede `shared-tunnel`, migrations, primeiro Adm e cenário demonstrativo `--seed-demo`.
- [Observabilidade](Observabilidade.md): contrato dos eventos implementados e diagnóstico com Serilog.
- [Catálogo Embrapa](Catalogo_Embrapa.md): 113 cultivares, imagens licenciadas e importação explícita offline.
- [Cultivos no panorama](Cultivos_no_Panorama.md): pixel art, fases configuráveis e densidade ilustrativa.
- [Fotografias de campo](Fotografias_Campo.md): recorte de RF12, armazenamento local atrás de `IFileStorage`, vínculo único e contrato HTTP.

Novos detalhamentos e decisões de arquitetura ficam nesta pasta. As regras de desenvolvimento ficam em [.codex/arquitetura-backend.md](../../.codex/arquitetura-backend.md) e [.codex/arquitetura-frontend.mdc](../../.codex/arquitetura-frontend.mdc), vinculadas pelo `AGENTS.md`; os requisitos e documentos gerais permanecem na [pasta de documentos](../).
