"""keys.json'daki anahtarları kaynak dosyaya göre gruplayıp numaralar (Tools/loc/order.json).
Var olan numaralar korunur, yeni anahtarlar sona eklenir. Kullanım: python -I Tools/loc/order.py [başlangıç bitiş]"""
import json, os, sys
LOC = os.path.dirname(os.path.abspath(__file__))
keys = json.load(open(os.path.join(LOC, 'keys.json'), encoding='utf-8'))
path = os.path.join(LOC, 'order.json')
order = json.load(open(path, encoding='utf-8')) if os.path.exists(path) else []
known = set(order)
groups = {}
for k, src in keys.items():
    if k not in known: groups.setdefault(src.split(':')[0], []).append(k)
first = ['scene', 'Assets/Scripts/UI/', 'Assets/Scripts/Platform', 'Assets/Scripts/Steam', 'Assets/Scripts/Companion', 'Assets/Scripts/Orders']
def rank(f):
    if 'RareNotes' in f: return (9, f)
    for i, p in enumerate(first):
        if f.startswith(p): return (i, f)
    return (6 if 'Catalog' in f or 'Environment.cs' in f or 'Gear' in f or 'Happenings' in f else 5, f)
for f in sorted(groups, key=rank): order += groups[f]
json.dump(order, open(path, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
if len(sys.argv) == 3:
    a, b = int(sys.argv[1]), int(sys.argv[2])
    for i in range(a, min(b, len(order)) + 1):
        print(f'{i}\t{json.dumps(order[i-1], ensure_ascii=False)[1:-1]}')
else:
    print(len(order), 'anahtar numaralı')
