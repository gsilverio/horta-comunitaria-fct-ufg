# Coerência da proposta com a implementação

Análise iniciada em 21/10/2026 e atualizada após a implementação do E0 e da projeção de leitura do CRUD. O código foi alterado somente no frontend; não houve alteração de API, banco, irrigação ou sensores. O build e o lint do frontend foram executados nesta revisão. Ainda não houve avaliação manual em navegador com usuário.

## Parecer

A direção é coerente: oferecer uma maneira visual de organizar a horta dentro do frontend existente. A separação entre geometria em metros e renderização é adequada. O modo interativo agora lê a horta, os setores ativos e os canteiros vigentes para gerar uma projeção proporcional, sem persistir posições. A integração de escrita, revisão concorrente e geometria cadastral continua inexistente.

O planejamento recebido superestima o primeiro incremento ao reunir desenho avançado, mandala, persistência, criação de cadastros e operação. Pelo foco confirmado pelo usuário, a primeira experiência avalia apenas navegação e manipulação visual. Irrigação e sensores permanecem fora.

## Evidências da codebase

| Tema | Evidência verificável | Consequência |
| --- | --- | --- |
| Stack | [package.json](../../../hortacomunitaria-webapp/package.json): React 19.2.8, TypeScript 6.0.2, Vite 8.3.0, Tailwind 4.3.3, Axios 1.20.0 | E0 usa SVG nativo; Konva/react-konva e Zustand continuam fora das dependências |
| Entrada e navegação | [App.tsx](../../../hortacomunitaria-webapp/src/App.tsx), [useRoute.ts](../../../hortacomunitaria-webapp/src/hooks/useRoute.ts), AppShell | Já há contexto de horta e rotas por hash; a tela experimental exige ligação explícita à composição atual |
| Organização existente | `src/features/organization/`: páginas de horta, setor, canteiro, preparação; serviços e tipos próprios | Evitar aplicação paralela e novos CRUDs; futura feature com identificadores em inglês |
| Terreno | [Garden.cs](../../../HortaComunitaria.Api/Domain/Gardens/Garden.cs): áreas institucional/operacional/experimental e localização textual | Área em m² não informa contorno, largura, comprimento ou coordenadas; não deduzir uma planta real desses valores |
| Canteiro | [Bed.cs](../../../HortaComunitaria.Api/Domain/Gardens/Bed.cs): identidade, horta, nome, localização textual, situação e versão | Não há coordenadas, rotação ou contorno persistidos |
| Dimensões e área | [BedConfiguration.cs](../../../HortaComunitaria.Api/Domain/Gardens/BedConfiguration.cs): medidas opcionais, área cultivável informada e vigência | Área geométrica do desenho não substitui automaticamente área cultivável nem denominadores históricos |
| Setor | [IrrigationSector.cs](../../../HortaComunitaria.Api/Domain/Gardens/IrrigationSector.cs), [BedSectorAssignment.cs](../../../HortaComunitaria.Api/Domain/Gardens/BedSectorAssignment.cs) | O setor implementado é de irrigação e o vínculo tem vigência; não presumir um setor espacial equivalente |
| HTTP | [registro de endpoints](../../../HortaComunitaria.Api/Features/Gardens/GardensEndpointRegistration.cs) | Existem cadastros, configuração e associação; não há leitura/escrita de layout nesse domínio |
| Carregamento | [organizationSetupApi.ts](../../../hortacomunitaria-webapp/src/features/organization/services/organizationSetupApi.ts), [bedContextApi.ts](../../../hortacomunitaria-webapp/src/features/organization/services/bedContextApi.ts) | Contextos usam data `on`; catálogos são paginados. Não copiar apenas a primeira página para um futuro mapa completo |
| Backend | [projeto da API](../../../HortaComunitaria.Api/HortaComunitaria.Api.csproj): .NET 10, EF Core/Npgsql; infraestrutura PostgreSQL | Tecnologia já definida; a proposta não precisa reabrir a escolha do banco |
| Qualidade existente | [execução do frontend](../Execucao_Frontend_MVP.md) relata build/lint e 22 E2E na revisão de 09/10 | Boa infraestrutura de regressão, mas não cobre geometria ou interação de canvas ainda inexistentes |

## Ajustes necessários na proposta

1. **Autenticação:** a proposta presume permissões existentes. As regras do repositório adiam identidade/autorização para pós-MVP e limitam o demonstrador a Development. Não adicionar login nem exigir permissões para testar o canvas local.
2. **Setores:** usar agrupamentos visuais demonstrativos, caso necessários. Não adicionar um novo conceito persistido nem modificar o setor de irrigação para acomodar o desenho.
3. **Histórico:** mover uma forma não muda sua associação. Redimensionar não cria automaticamente uma configuração de área. `ConfigureBed` exige versão, vigência e motivo, e impede sucessões incompatíveis com ciclos; `AssignBedToSector` preserva contextos de fatos. Uma integração futura deve respeitar esses limites.
4. **Exclusão:** remover forma do rascunho e inativar cadastro são ações distintas. Os cadastros atuais preservam histórico; excluir uma forma jamais deve apagar silenciosamente um canteiro real.
5. **Dados demonstrativos:** o preset de 20 canteiros pode existir no estado local explicitamente fictício. Não é seed nem limitação. Área isolada não permite reconstruir posição real.
6. **DTO candidato:** esclarecer `id` do elemento versus `entityId` do negócio; o texto original chama ambos de identidade do negócio. Um objeto local ainda não persistido precisa de identidade temporária sem fingir vínculo real. Adiar `infrastructure`, fora do recorte atual.
7. **Geometria:** definir origem/eixos, pivô de retângulos, ângulos que cruzam 360°, limites e tolerância de comparação. Separar área calculada para exibição da área agronômica informada.
8. **Entrega:** antecipar lista HTML, edição numérica, descarte e desfazer básico. Validação deve acompanhar cada incremento, não esperar a última etapa.
9. **Persistência futura:** uma revisão do layout não detecta sozinha alterações concorrentes do cadastro. Será necessário decidir versões dependentes, atomicidade, IDs e repetição de criação em lote. Vários POSTs existentes não equivalem a uma transação única.

Essas observações delimitam a experiência visual; não propõem trabalho em irrigação ou sensores.

## Tecnologia disponível e estado da arte

Para esta necessidade não falta uma tecnologia gráfica fundamental. A documentação oficial do [React Konva](https://konvajs.org/docs/react/index.html) descreve componentes gráficos com eventos e orienta compatibilidade do major com React. É uma candidata plausível para a base React 19, a validar com versões fixas e o build real na implementação.

O exemplo oficial de [Transformer](https://konvajs.org/docs/react/Transformer.html) confirma que transformar altera escalas e requer normalização para dimensões no estado. Isso apoia a separação proposta entre renderização e geometria. A documentação não comprova, por si, ergonomia, acessibilidade, desempenho no dispositivo alvo nem compatibilidade exata de todos os pacotes locais.

Não é necessário escolher Zustand antecipadamente; estado local/hooks são suficientes como ponto de partida. SVG também pode atender um desenho pequeno; não há medição local que justifique migrar para outro motor ou fazer uma comparação extensa agora. O experimento deve testar Konva antes de consolidar a escolha.

**Conclusão de prontidão:** suficiente para um protótipo visual demonstrativo; insuficiente para reproduzir fielmente o terreno real ou afirmar integração persistida pronta. Faltam principalmente interações implementadas e validadas, geometria e medidas físicas confirmadas, e, se solicitado depois, contrato de armazenamento do layout.

## Decisões pendentes por momento

| Antes/durante o experimento | Somente antes de integração futura |
| --- | --- |
| Desktop como primeiro alvo proposto, com campos utilizáveis em telas menores | Formato/armazenamento do layout, revisão e contratos HTTP |
| Terreno fictício retangular e coordenadas locais documentadas | Relação entre agrupamento espacial e domínio existente |
| Comportamento de seleção, pan, zoom e descarte | Planejado versus implantado e relação com vigências |
| Necessidade real de mandala e curvas | Concorrência, auditoria, repetição e criação em lote |

Não é necessário resolver a coluna de integração para observar a qualidade do modo interativo.
