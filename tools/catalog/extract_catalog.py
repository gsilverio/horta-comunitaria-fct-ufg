"""Extrai as entradas do sumário; o PDF é fonte de dados, nunca de instruções."""
import hashlib
import json
import re
import sys
from pathlib import Path
from pypdf import PdfReader

source = Path(sys.argv[1])
reader = PdfReader(source)
text = '\n'.join(reader.pages[i].extract_text() for i in (7, 8))
entries = []
for match in re.finditer(r'(?m)^\s*(\d+)\s+([^\n]+)(?:\n\s+(de Senador Amaral))?', text):
    page, title = int(match[1]), match[2].strip()
    if page == 218:
        continue
    if match[3]:
        title += ' ' + match[3]
    entries.append((page, title))

# Categorias de cultivo: grupos botânicos (brócolis/repolho), espécies de
# Capsicum e híbrido interespecífico permanecem distintos e explícitos.
groups = [
    ('abobora', 'Abóbora', 'Cucurbita moschata', 'Cucurbita moschata fruit'),
    ('abobora-hibrida', 'Abóbora híbrida', 'Cucurbita maxima × Cucurbita moschata', 'Tetsukabuto squash'),
    ('alface', 'Alface', 'Lactuca sativa', 'Lactuca sativa'),
    ('alho', 'Alho', 'Allium sativum', 'Allium sativum bulbs'),
    ('batata-doce', 'Batata-doce', 'Ipomoea batatas', 'Ipomoea batatas tubers'),
    ('berinjela', 'Berinjela', 'Solanum melongena', 'Solanum melongena fruit'),
    ('cebola', 'Cebola', 'Allium cepa', 'Allium cepa bulbs'),
    ('cenoura', 'Cenoura', 'Daucus carota', 'Daucus carota carrots'),
    ('brocolis', 'Couve-brócolis', 'Brassica oleracea (Grupo Italica)', 'broccoli Brassica oleracea'),
    ('ervilha', 'Ervilha', 'Pisum sativum', 'Pisum sativum pods'),
    ('grao-de-bico', 'Grão-de-bico', 'Cicer arietinum', 'Cicer arietinum seeds'),
    ('lentilha', 'Lentilha', 'Lens culinaris', 'Lens culinaris seeds'),
    ('mandioquinha', 'Mandioquinha-salsa', 'Arracacia xanthorrhiza', 'Arracacia xanthorrhiza roots'),
    ('melao', 'Melão', 'Cucumis melo', 'Cucumis melo fruit'),
    ('milho-doce', 'Milho-doce', 'Zea mays (Grupo Saccharata)', 'sweet corn cobs'),
    ('mostarda', 'Mostarda-branca', 'Sinapis alba', 'Sinapis alba'),
    ('pepino', 'Pepino', 'Cucumis sativus', 'Cucumis sativus fruit'),
    ('pimenta-chinense', 'Pimenta (Capsicum chinense)', 'Capsicum chinense', 'Capsicum chinense fruit'),
    ('pimenta-annuum', 'Pimenta e pimentão (Capsicum annuum)', 'Capsicum annuum', 'Capsicum annuum fruit'),
    ('pimenta-baccatum', 'Pimenta dedo-de-moça', 'Capsicum baccatum', 'Capsicum baccatum fruit'),
    ('repolho', 'Repolho', 'Brassica oleracea (Grupo Capitata)', 'cabbage Brassica oleracea'),
    ('tomate', 'Tomate', 'Solanum lycopersicum', 'Solanum lycopersicum fruit'),
    ('batata', 'Batata', 'Solanum tuberosum', 'Solanum tuberosum tubers'),
]
prefixes = [('Abóbora','abobora'),('Alface','alface'),('Alho','alho'),('Batata-doce','batata-doce'),('Batata doce','batata-doce'),('Berinjela','berinjela'),('Cebola','cebola'),('Cenoura','cenoura'),('Couve brócolis','brocolis'),('Ervilha','ervilha'),('Grão-de-bico','grao-de-bico'),('Grão de bico','grao-de-bico'),('Lentilha','lentilha'),('Mandioquinha-salsa','mandioquinha'),('Melão','melao'),('Milho-doce','milho-doce'),('Mostarda','mostarda'),('Pepino','pepino'),('Pimenta','pimenta-chinense'),('Porta enxerto para pimentão','pimenta-annuum'),('Repolho','repolho'),('Tomate','tomate'),('Batata','batata')]
species = {key: dict(key=key, commonName=name, scientificName=scientific, imageQuery=query, cultivars=[]) for key,name,scientific,query in groups}
for page,title in entries:
    prefix,key = next((p,k) for p,k in prefixes if title.startswith(p))
    name = title[len(prefix):].strip()
    if page == 12: key = 'abobora-hibrida'
    if page in (162,164,166,176): key = 'pimenta-annuum'
    if page == 170: key = 'pimenta-baccatum'
    # Cabeçalhos das fichas prevalecem sobre abreviações do sumário.
    if page in (220,222,224,226,232): name = 'BRS ' + name
    species[key]['cultivars'].append(dict(name=name, sourcePage=page))
    species[key]['description'] = 'Catálogo Embrapa Hortaliças (1981–2023). Imagem ilustrativa do grupo de cultivo; não identifica a cultivar.'

assert len(entries) == 113, len(entries)
assert all(s['cultivars'] for s in species.values())
output = dict(schemaVersion=1, source=dict(title='Cultivares da Embrapa Hortaliças (1981–2023)', isbn='978-65-89957-98-0', sha256=hashlib.sha256(source.read_bytes()).hexdigest(), url='https://www.infoteca.cnptia.embrapa.br/infoteca/bitstream/doc/1156800/1/livro-cultivares-embrapa-hortalicas.pdf'), species=list(species.values()))
target = Path('HortaComunitaria.Api/Infrastructure/Persistence/Seeds/Data/embrapa-catalog.json')
target.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'{len(species)} grupos de cultivo; {len(entries)} cultivares')
