"""Çeviri kaynaklarını (Tools/loc/src/*.txt) oyunun dil tablolarına birleştirir (2026-10-26).

Kaynak biçimi (numara = Tools/loc/order.json'daki sıra, 1'den başlar; başlıktaki İngilizce yalnız hatırlatma):
    #12 Next unlock: ...
    tr<TAB>Sonraki açılış: ...
    de<TAB>...
Satır sonu "\\n" olarak yazılır. Çoğul biçimler "|" ile ayrılır (ru/pl: bir|birkaç|çok; diğerleri: bir|çok).
Çıktı: Assets/Resources/Localization/<kod>.json. Kullanım: python -I Tools/loc/merge.py
"""
import glob
import json
import os
import re

LOC = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(LOC))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Localization')
CODES = {'tr': 'tr', 'de': 'de', 'fr': 'fr', 'es': 'es', 'pt': 'pt-BR', 'pl': 'pl', 'ru': 'ru', 'zh': 'zh-Hans', 'ja': 'ja', 'ko': 'ko'}

order = json.load(open(os.path.join(LOC, 'order.json'), encoding='utf-8'))
tables = {code: {} for code in CODES.values()}
errors = 0
for path in sorted(glob.glob(os.path.join(LOC, 'src', '*.txt'))):
    idx = None
    for n, line in enumerate(open(path, encoding='utf-8'), 1):
        line = line.rstrip('\n').rstrip('\r')
        if not line.strip():
            continue
        m = re.match(r'^#(\d+)\b', line)
        if m:
            idx = int(m.group(1))
            continue
        if '\t' not in line or idx is None:
            print(f'{os.path.basename(path)}:{n}: anlaşılmadı: {line[:60]!r}')
            errors += 1
            continue
        lang, text = line.split('\t', 1)
        if lang not in CODES:
            print(f'{os.path.basename(path)}:{n}: bilinmeyen dil {lang!r}')
            errors += 1
            continue
        if not 1 <= idx <= len(order):
            print(f'{os.path.basename(path)}:{n}: numara yok {idx}')
            errors += 1
            continue
        tables[CODES[lang]][order[idx - 1]] = text.replace('\\n', '\n')

os.makedirs(OUT, exist_ok=True)
rank = {k: i for i, k in enumerate(order)}
for code, table in tables.items():
    items = sorted(table.items(), key=lambda kv: rank[kv[0]])
    with open(os.path.join(OUT, code + '.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(dict(items), f, ensure_ascii=False, indent=1)
    print(f'{code:8} {len(table)}')
print('hata:', errors)
