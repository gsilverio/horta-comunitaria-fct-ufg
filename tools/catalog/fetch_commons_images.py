"""Prepara imagens offline via Action API. Reexecutar preserva escolhas existentes.

Uso: python tools/catalog/fetch_commons_images.py [chave ...]
As imagens são ilustrativas, nunca comprovam a identidade de uma cultivar.
"""
import base64
import hashlib
import html
import io
import json
import re
import sys
import time
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'HortaComunitaria.Api/Infrastructure/Persistence/Seeds/Data'
API = 'https://commons.wikimedia.org/w/api.php'
AGENT = 'HortaComunitariaCatalog/1.0 (https://github.com/gsilverio/horta-comunitaria-fct-ufg; educational catalog)'
ALLOWED = {'CC0', 'Public domain', 'CC BY-SA 4.0', 'CC BY-SA 3.0', 'CC BY-SA 2.0', 'CC BY 4.0', 'CC BY 3.0', 'CC BY 2.0'}

def get(url):
    parsed = urllib.parse.urlparse(url)
    if parsed.scheme != 'https' or parsed.hostname not in ('commons.wikimedia.org','upload.wikimedia.org','thumb.wikimedia.org'):
        raise ValueError('Host de imagem não permitido')
    for attempt in range(4):
        try:
            with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':AGENT}),timeout=45) as response:
                content = response.read(12_000_001)
                if len(content)>12_000_000: raise ValueError('Resposta excede 12 MB')
                return content
        except urllib.error.HTTPError as exc:
            if exc.code not in (429,502,503,504) or attempt==3: raise
            time.sleep(min(int(exc.headers.get('Retry-After','5')),30))

def plain(value):
    return html.unescape(re.sub('<[^>]+>', '', value)).strip()

catalog = json.loads((DATA/'embrapa-catalog.json').read_text(encoding='utf-8'))
target = DATA/'commons-images.json'
images = json.loads(target.read_text(encoding='utf-8')) if target.exists() else {}
for species in catalog['species']:
    key = species['key']
    if sys.argv[1:] and key not in sys.argv[1:]: continue
    if key in images: continue
    overrides = {
        'abobora-hibrida': '"Tetsukabuto"',
        'alho': 'intitle:"Garlic" bulbs -disease -Fusarium',
        'cebola': 'intitle:"Onion" bulb -viviparum',
        'brocolis': 'intitle:"Broccoli" -Romanesco -cauliflower -soup -cream -cooked -dish -pizza',
        'milho-doce': '"sweet corn" raw -cooked -Liat -dish',
        'pepino': 'intitle:"Cucumber" -disease -Stagonosporopsis -pickled',
        'repolho': 'intitle:"Cabbage" -savoy -sabauda -ornamental -red',
    }
    params = dict(action='query',format='json',formatversion=2,generator='search',gsrnamespace=6,gsrsearch=overrides.get(key,species['imageQuery'])+' filetype:bitmap',gsrlimit=8,prop='imageinfo',iiprop='url|extmetadata|mime',iiurlwidth=480,maxlag=5)
    titles = {
        'brocolis': 'File:Broccoli.jpg',
        'pepino': 'File:Concombre (Cucumis sativus).jpg',
        'repolho': 'File:Starr 080103-1276 Brassica oleracea var. capitata.jpg',
    }
    if key in titles:
        params = dict(action='query',format='json',formatversion=2,titles=titles[key],prop='imageinfo',iiprop='url|extmetadata|mime',iiurlwidth=480,maxlag=5)
    result = json.loads(get(API+'?'+urllib.parse.urlencode(params)))
    if 'error' in result: raise RuntimeError(result['error'])
    for page in sorted(result.get('query',{}).get('pages',[]),key=lambda p:p.get('index',0)):
        info=page['imageinfo'][0]
        meta=info.get('extmetadata',{})
        value=lambda field: plain(meta.get(field,{}).get('value',''))
        license_name=value('LicenseShortName')
        if license_name not in ALLOWED or not info['mime'].startswith('image/') or not value('Artist'): continue
        url=info.get('thumburl',info['url'])
        raw=get(url)
        picture=Image.open(io.BytesIO(raw))
        picture.thumbnail((480,480))
        # Recompressão explicitamente registrada nos créditos.
        output=io.BytesIO()
        picture.convert('RGB').save(output,format='JPEG',quality=82,optimize=True)
        data=output.getvalue()
        if len(data)>180_000: continue
        images[key]=dict(base64=base64.b64encode(data).decode('ascii'),mimeType='image/jpeg',sha256=hashlib.sha256(data).hexdigest(),sourceUrl=info['descriptionurl'],downloadUrl=url,title=page['title'],author=value('Artist'),license=license_name,licenseUrl=value('LicenseUrl') or ('https://creativecommons.org/publicdomain/zero/1.0/' if license_name=='CC0' else 'https://commons.wikimedia.org/wiki/Commons:Public_domain'),attribution=value('Attribution') or value('Credit'),changes='Redimensionada até 480 px e recomprimida em JPEG; imagem ilustrativa do grupo, não da cultivar.',retrievedAt=datetime.now(timezone.utc).isoformat(),reviewed=False)
        target.write_text(json.dumps(images,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(key, page['title'], license_name, len(data),flush=True)
        break
    else: raise RuntimeError('Nenhuma imagem utilizável para '+key)
    time.sleep(1)

preview=Image.new('RGB',(960,((len(images)+3)//4)*155),'white')
draw=ImageDraw.Draw(preview)
for index,species in enumerate(catalog['species']):
    if species['key'] not in images: continue
    picture=Image.open(io.BytesIO(base64.b64decode(images[species['key']]['base64'])))
    picture.thumbnail((225,125))
    x=(index%4)*240; y=(index//4)*155
    preview.paste(picture,(x,y))
    draw.text((x,y+127),species['key'],fill='black')
folder=ROOT/'tmp/catalog'
folder.mkdir(parents=True,exist_ok=True)
preview.save(folder/'commons-review.jpg')
