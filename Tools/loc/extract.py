"""Oyun metinlerinin çeviri anahtarlarını toplar ve dil tablolarını denetler (2026-10-26).

Kaynaklar:
  - Assets/Scripts: L.T("..."), L.F("...", ...), L.N("...") ilk bağımsız değişkeni; L.P(n, "tekil", "çoğul")
    çoğul biçimi (anahtar odur). Bağımsız değişken düz metin değilse atlanır.
  - "// loc:catalog" işaretli dosyalarda insan metni gibi görünen tüm düz metinler (öğe/mekân adları, notlar):
    '#' ile başlamayan, '_' ya da '/' içermeyen, boşluk içeren ya da büyük harfle başlayanlar.
  - Assets/Data/CarCatalog.asset: boya adları (displayName) ve dolu unlockLabel'lar.
  - Tools/loc/scene_keys.txt: sahnedeki sabit etiketler (Unity menüsü "ScrollRoad/Dil Anahtarlarını Topla").

Kullanım:
  python -I Tools/loc/extract.py            # anahtarları Tools/loc/keys.txt'e yazar, dil tablolarını denetler
  python -I Tools/loc/extract.py --missing  # her dil için eksik anahtarları Tools/loc/missing_<kod>.txt'e yazar
"""
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SCRIPTS = os.path.join(ROOT, 'Assets', 'Scripts')
TABLES = os.path.join(ROOT, 'Assets', 'Resources', 'Localization')
LOC = os.path.join(ROOT, 'Tools', 'loc')
LANGS = ['zh-Hans', 'ru', 'es', 'pt-BR', 'de', 'ja', 'fr', 'ko', 'pl', 'tr']

CALL = re.compile(r'\bL\.(T|F|N|P)\(')
LITERAL = re.compile(r'(\$?@?)"((?:[^"\\\n]|\\.)*)"')


def unescape(s):
    out, i = [], 0
    while i < len(s):
        c = s[i]
        if c != '\\':
            out.append(c)
            i += 1
            continue
        e = s[i + 1]
        i += 2
        if e == 'n': out.append('\n')
        elif e == 't': out.append('\t')
        elif e == 'r': out.append('\r')
        elif e == 'u':
            out.append(chr(int(s[i:i + 4], 16)))
            i += 4
        else: out.append(e)
    return ''.join(out)


def split_args(src, start):
    """src[start] '(' sonrası: üst düzey bağımsız değişkenlerin ham metinleri."""
    depth, i, args, cur = 0, start, [], []
    in_str = False
    while i < len(src):
        c = src[i]
        if in_str:
            cur.append(c)
            if c == '\\':
                cur.append(src[i + 1])
                i += 2
                continue
            if c == '"':
                in_str = False
            i += 1
            continue
        if c == '"':
            in_str = True
            cur.append(c)
        elif c in '([{':
            depth += 1
            cur.append(c)
        elif c in ')]}':
            if depth == 0:
                args.append(''.join(cur).strip())
                return args
            depth -= 1
            cur.append(c)
        elif c == ',' and depth == 0:
            args.append(''.join(cur).strip())
            cur = []
        else:
            cur.append(c)
        i += 1
    return args


def literal_of(arg):
    m = re.fullmatch(r'"((?:[^"\\\n]|\\.)*)"', arg)
    return unescape(m.group(1)) if m else None


def strip_comments(src):
    # Satır yorumlarını at (metin içindeki // korunur).
    out = []
    for line in src.split('\n'):
        res, in_str, i = [], False, 0
        while i < len(line):
            c = line[i]
            if in_str:
                res.append(c)
                if c == '\\' and i + 1 < len(line):
                    res.append(line[i + 1])
                    i += 2
                    continue
                if c == '"': in_str = False
            elif c == '"':
                in_str = True
                res.append(c)
            elif line.startswith('//', i):
                break
            else:
                res.append(c)
            i += 1
        out.append(''.join(res))
    return '\n'.join(out)


def looks_like_text(s):
    if not s or s.startswith('#') or '_' in s or '/' in s or '\\' in s:
        return False
    if not re.search(r'[A-Za-z]', s):
        return False
    return ' ' in s or s[0].isupper()


def collect():
    keys = {}  # anahtar -> ilk konum
    warnings = []
    for dp, _, files in os.walk(SCRIPTS):
        for f in files:
            if not f.endswith('.cs'): continue
            path = os.path.join(dp, f)
            rel = os.path.relpath(path, ROOT).replace('\\', '/')
            raw = open(path, encoding='utf-8').read()
            src = strip_comments(raw)
            for m in CALL.finditer(src):
                kind = m.group(1)
                args = split_args(src, m.end())
                line = src.count('\n', 0, m.start()) + 1
                want = [2] if kind == 'P' else [0]
                for idx in want:
                    if idx >= len(args): continue
                    lit = literal_of(args[idx])
                    if lit is None:
                        if args[idx].startswith('$"'):
                            warnings.append(f'{rel}:{line}: L.{kind} içinde $"..." (sabit anahtar olmalı)')
                        continue
                    keys.setdefault(lit, f'{rel}:{line}')
            if 'loc:catalog' in raw:
                for i, line_text in enumerate(src.split('\n'), 1):
                    for lm in LITERAL.finditer(line_text):
                        if lm.group(1): continue  # $"..." ya da @"..."
                        s = unescape(lm.group(2))
                        if looks_like_text(s):
                            keys.setdefault(s, f'{rel}:{i}')
    # Araç kataloğu
    asset = os.path.join(ROOT, 'Assets', 'Data', 'CarCatalog.asset')
    section = None
    for i, line in enumerate(open(asset, encoding='utf-8'), 1):
        if re.match(r'^  paints:', line): section = 'paints'
        elif re.match(r'^  models:', line): section = 'models'
        m = re.match(r'^\s+(displayName|unlockLabel): (.*)$', line)
        if not m: continue
        value = m.group(2).strip()
        if value.startswith('"'): value = json.loads(value)
        if not value: continue
        if m.group(1) == 'unlockLabel' or section == 'paints':
            keys.setdefault(value, f'Assets/Data/CarCatalog.asset:{i}')
    # Sahne etiketleri
    scene = os.path.join(LOC, 'scene_keys.txt')
    if os.path.exists(scene):
        for i, line in enumerate(open(scene, encoding='utf-8'), 1):
            line = line.rstrip('\n')
            if line: keys.setdefault(line.replace('\\n', '\n').replace('\\\\', '\\'), f'scene:{i}')
    else:
        warnings.append('Tools/loc/scene_keys.txt yok: Unity menüsü "ScrollRoad/Dil Anahtarlarını Topla"')
    return keys, warnings


PLACEHOLDER = re.compile(r'\{(\d+)(?::[^}]*)?\}')
TAG = re.compile(r'<[^>]+>')


def check_value(key, value):
    """Biçim yer tutucuları ve zengin metin etiketleri anahtardakiyle aynı olmalı."""
    problems = []
    for form in value.split('|'):
        if sorted(set(PLACEHOLDER.findall(key))) != sorted(set(PLACEHOLDER.findall(form))):
            problems.append('yer tutucu')
        if sorted(TAG.findall(key)) != sorted(TAG.findall(form)):
            problems.append('etiket')
    return sorted(set(problems))


def main():
    keys, warnings = collect()
    os.makedirs(LOC, exist_ok=True)
    with open(os.path.join(LOC, 'keys.json'), 'w', encoding='utf-8') as f:
        json.dump(dict(sorted(keys.items())), f, ensure_ascii=False, indent=1)
    print(f'{len(keys)} anahtar → Tools/loc/keys.json')
    for w in warnings: print('UYARI', w)
    for lang in LANGS:
        path = os.path.join(TABLES, lang + '.json')
        table = json.load(open(path, encoding='utf-8')) if os.path.exists(path) else {}
        missing = [k for k in keys if k not in table]
        extra = [k for k in table if k not in keys]
        bad = [(k, p) for k, v in table.items() if k in keys for p in [check_value(k, v)] if p]
        print(f'{lang:8} {len(table):5} çeviri, {len(missing):4} eksik, {len(extra):3} fazla, {len(bad):3} biçim hatası')
        for k, p in bad[:10]: print('   biçim', p, repr(k)[:80], '→', repr(table[k])[:80])
        if '--missing' in sys.argv:
            with open(os.path.join(LOC, f'missing_{lang}.txt'), 'w', encoding='utf-8') as f:
                json.dump(missing, f, ensure_ascii=False, indent=0)


if __name__ == '__main__':
    main()
