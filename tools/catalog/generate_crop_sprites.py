"""Gera SVGs próprios de pixel art, sem converter ou reutilizar as fotos do Commons.

Grade inteira 32 × 40, três fases, perfis por grupo e overrides documentados
por ficha. Os SVGs são assets estáticos de apresentação, não dados agronômicos.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'hortacomunitaria-webapp/public/assets/pixel-garden/crops'
OUT.mkdir(parents=True, exist_ok=True)
catalog = json.loads((ROOT/'HortaComunitaria.Api/Infrastructure/Persistence/Seeds/Data/embrapa-catalog.json').read_text(encoding='utf-8'))
PROFILES = {
    'abobora': ('vine','#dc9d43'), 'abobora-hibrida': ('vine','#5a783c'),
    'alface': ('leaf','#9dce59'), 'alho': ('bulb','#eee1c5'),
    'batata-doce': ('tuber','#b67582'), 'berinjela': ('fruit','#713e9b'),
    'cebola': ('bulb','#d6ac65'), 'cenoura': ('root','#ef8738'),
    'brocolis': ('flower','#5b9945'), 'ervilha': ('pod','#9ccf58'),
    'grao-de-bico': ('grain','#e5c58b'), 'lentilha': ('grain','#bc9b67'),
    'mandioquinha': ('root','#e2bf66'), 'melao': ('vine','#e6cf66'),
    'milho-doce': ('corn','#edc24b'), 'mostarda': ('mustard','#f2d75c'),
    'pepino': ('vine','#649642'), 'pimenta-chinense': ('pepper','#ed8056'),
    'pimenta-annuum': ('pepper','#d64d3f'), 'pimenta-baccatum': ('pepper','#de6745'),
    'repolho': ('cabbage','#9bbd75'), 'tomate': ('tomato','#dd5146'),
    'batata': ('tuber','#bb946c'), 'generic': ('leaf','#90b65d'),
}
# Cores expressamente descritas nas respectivas fichas do livro. Sem diferença
# comprovada, a cultivar reutiliza o sprite da espécie e o manifesto informa isso.
OVERRIDES = {14: '#839c40',16: '#b2d772',18: '#4d9149',158: '#e4c74b',160:'#c84539',168:'#e5ae36',172:'#dd4c3e',174:'#ed963d',178:'#e7c741',180:'#e99d78'}

def sprite(family, color, stage):
    cells={}
    def box(x,y,w,h,c):
        for a in range(x,x+w):
            for b in range(y,y+h):
                if 0<=a<32 and 0<=b<40: cells[a,b]=c
    def leaf(x,y,w=7,h=4,c='#619748'):
        box(x+1,y,w-2,h,c); box(x,y+1,w,h-2,c); box(x+2,y+1,w-4,1,'#91ba64')
    def fruit(x,y,w,h,c=color):
        box(x+2,y,w-4,h,c); box(x+1,y+1,w-2,h-2,c);box(x,y+3,w,h-6,c)
        box(x+2,y+2,2,max(1,h//3),'#f1dc9a' if family in ('root','bulb','corn','tuber') else '#ffffff55')
    box(10,35,12,2,'#00000022'); box(12,34,8,1,'#5b442e')
    if stage=='seedling':
        box(15,26,2,9,'#447342');leaf(8,26,8,4,color if family=='leaf' else '#659d4e');leaf(17,23,7,4);box(16,25,1,3,'#9fc66a')
    elif family in ('leaf','cabbage','flower'):
        leaf(6,27,11,6);leaf(15,27,11,6);leaf(8,22,9,8);leaf(16,21,8,8)
        if stage=='harvest':
            if family=='leaf':
                leaf(5,24,10,7,color);leaf(17,23,10,7,color);leaf(9,20,13,10,color);leaf(12,17,8,10,color)
            elif family=='cabbage':
                fruit(8,19,16,15,color);box(14,21,1,10,'#c1d59a');box(9,26,12,1,'#74945a')
            else:
                for x,y in [(7,22),(13,18),(19,22),(12,25)]: leaf(x,y,8,7,color)
    elif family in ('root','bulb','tuber'):
        for x,y in [(9,15),(14,12),(18,16)]:
            box(x,y,2,18,'#42723c');leaf(x-4,y,7,3)
        if stage=='harvest':
            if family=='root':
                box(12,25,9,5,color);box(13,30,7,4,color);box(15,34,3,4,color);box(16,38,1,1,color);box(12,27,3,1,'#ffde88')
            else:
                fruit(8,24,16,12,color)
                if family=='bulb':
                    box(14,21,4,5,color);box(15,27,1,8,'#b99d79')
                else: fruit(18,30,10,7,color)
    elif family=='vine':
        box(7,31,20,2,'#406c37');leaf(4,23,10,7);leaf(15,22,12,8);leaf(9,28,9,6)
        if stage=='harvest':
            if color=='#649642':
                fruit(14,24,8,13,color);box(17,27,1,7,'#a5bf64')
            else:
                fruit(11,23,15,12,color);box(18,21,2,3,'#6c6736');box(17,25,1,8,'#b0b363')
    else:
        box(15,12,2,22,'#497c3d');leaf(6,19,10,4);leaf(17,17,9,4);leaf(7,27,10,4);leaf(17,26,9,4)
        if family=='corn':
            box(15,6,2,7,'#bfad59');box(12,8,8,1,'#d7c778');leaf(6,13,10,3);leaf(17,11,10,3)
            if stage=='harvest':
                fruit(19,19,6,13,color)
                for y in (22,25,28):box(20,y,3,1,'#ab8b35')
        elif family in ('pod','grain'):
            if stage=='harvest':
                for x,y in [(7,22),(19,20),(18,29)]:
                    fruit(x,y,6,9,color)
                    if family=='pod':box(x+2,y+2,1,5,'#507b3d')
        elif family=='mustard':
            if stage=='growing':
                for x,y in [(12,8),(18,11),(6,16),(21,17)]:
                    box(x,y,5,2,color);box(x+1,y-1,2,4,color);box(x+1,y,1,1,'#a78431')
            else:
                for x,y in [(9,12),(19,15),(8,23),(19,26)]:
                    box(x,y,2,7,'#b49b59');box(x+1,y+1,1,4,'#d2bb79')
        elif stage=='harvest':
            for x,y in [(7,23),(19,20),(19,29)]:
                fruit(x,y,7,7 if family=='tomato' else 10,color)
                box(x+2,y-1,3,2,'#4b793b')
    rects=''.join(f'<rect x="{x}" y="{y}" width="1" height="1" fill="{c}"/>' for (x,y),c in sorted(cells.items()))
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="32" height="40" viewBox="0 0 32 40" shape-rendering="crispEdges">{rects}</svg>\n'

assets={};cultivars={}
def emit(key,family,color):
    stages={}
    for stage in ('seedling','growing','harvest'):
        name=f'{key}-{stage}.svg';(OUT/name).write_text(sprite(family,color,stage),encoding='utf-8')
        stages[stage]='/assets/pixel-garden/crops/'+name
    assets[key]=stages

for key,(family,color) in PROFILES.items():emit(key,family,color)
for species in catalog['species']:
    key=species['key']
    for cultivar in species['cultivars']:
        page=cultivar['sourcePage'];asset=key
        if page in OVERRIDES:
            asset=f'{key}-page-{page}';emit(asset,PROFILES[key][0],OVERRIDES[page])
        cultivars[f'embrapa-2023:page-{page}']={'asset':asset,'sharedWithSpecies':page not in OVERRIDES,'sourcePage':page}
data={'species':{'embrapa-2023:'+s['key']:s['key'] for s in catalog['species']},'cultivars':cultivars,'assets':assets}
(ROOT/'hortacomunitaria-webapp/src/features/interactive-garden/crop-assets.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(len(assets)*3,'sprites;',len(cultivars),'cultivares mapeadas')
