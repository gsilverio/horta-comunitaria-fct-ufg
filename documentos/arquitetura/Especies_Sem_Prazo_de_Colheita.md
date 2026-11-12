# Espécies sem prazo de colheita

Levantamento de 11/11/2026, conferido no banco local `horta_demo`. Lista as espécies do catálogo que estão sem os prazos de **crescimento intermediário** e **colheita prevista** (`VisualGrowthDays` e `VisualHarvestDays`).

O livro *Cultivares da Embrapa Hortaliças (1981-2023)* não informa em quantos dias essas espécies ficam prontas para colheita, então os campos ficaram vazios. Sem esses prazos, a panorâmica mostra o plantio, mas não estima a fase nem destaca o canteiro quando chega a hora de colher.

Os critérios e os valores das demais espécies estão em [Cultivos no panorama](Cultivos_no_Panorama.md).

## Pendentes

| Espécie | Nome científico | Cultivares no catálogo | O que o livro informa |
| --- | --- | --- | --- |
| Abóbora | *Cucurbita moschata* | BRS Brasileirinha (p. 10) | Só a floração das flores femininas (60–70 dias após o plantio). A colheita é definida pelo tamanho do fruto (12–18 cm, uso ornamental), sem prazo em dias. |
| Pimenta e pimentão | *Capsicum annuum* | BRS Brasilândia (p. 162), BRS Ema (p. 164), BRS Garça (p. 166), BRS Sarakura (p. 176), BRS Acará (p. 182) | Brasilândia: só o início da frutificação (40 dias). Ema: só que é 14 dias mais precoce que Jalapeño TAM e Jalapeño M. Garça e Sarakura: sem dias. Acará é porta-enxerto para pimentão. |
| Repolho | *Brassica oleracea* (Grupo Capitata) | União (p. 184) | Nenhum prazo de ciclo ou colheita. |

## Como preencher

1. Em **Cultivo → Espécies**, abra a espécie e use **Configurar fases visuais**.
2. Informe os dois prazos, em dias contados a partir do plantio. O crescimento deve ser maior que zero e a colheita, maior que o crescimento. As demais espécies usam metade do prazo de colheita como crescimento.
3. No motivo, registre a fonte usada (outra publicação técnica, ficha do fornecedor de sementes ou observação da própria horta).

Também é possível configurar o prazo só em uma cultivar. A configuração da cultivar substitui a da espécie; quando a cultivar está sem prazo, ela herda o da espécie.

## Cultivares sem prazo próprio no livro

Estas espécies já têm prazo, calculado a partir de outras cultivares. As cultivares abaixo não trazem dias no livro e usam o valor da espécie, mas podem receber prazo próprio se houver outra fonte.

| Espécie | Cultivares |
| --- | --- |
| Batata | BRS IPRBel (p. 230) — ficha diz apenas "ciclo médio" |
| Cenoura | Alvorada (p. 58), BRS Planalto (p. 70) |
| Ervilha | BRS Sulina (p. 102) |
| Pimenta (*C. chinense*) | BRS Juruti (p. 168) |
| Tomate | BRS Sena (p. 188), BRS Kiara (p. 206), BRS Nagai (p. 210), BRS Couto (p. 194 — os 80 dias citados são da primeira floração) |

## Observação de catálogo

A cultivar **BRS Alfa São Francisco** (p. 236) está cadastrada como **Cebola**, mas a ficha do livro descreve uma batata-doce. Ela não entrou no cálculo de nenhuma espécie. Convém revisar o vínculo antes de lhe atribuir prazo.
