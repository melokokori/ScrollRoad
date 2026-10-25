"""Görsel yuva manifesti (Ortam Standardı v2, 2026-10-25): oyundaki her görselin yuvası — klasör, dosya (= yuva
kimliği), boyut, dolu alanın kutusu ve hangi listede durduğu (sıradan / nadir / araç / diğer). İleride pixel art
aynı adla aynı klasöre konunca kod değişmeden yerine oturur (sahne kurucu dosyayı adıyla yükler; oyundaki boy
katalogdaki Size / Height ile, görselin piksel boyutundan bağımsız).

Kullanım (proje kökünden): python Tools/art_manifest.py  →  Docs/gorsel_manifest.md
"""
import re
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets" / "Art"
VEHICLES = ROOT / "Assets" / "StreamingAssets" / "Vehicles"
SCRIPTS = ROOT / "Assets" / "Scripts"
OUT = ROOT / "Docs" / "gorsel_manifest.md"

# Kataloglardaki kimlikler: nadirler Frequency taşır; R(...)/Rec(...)/B(...) yardımcıları ve Fruit(…, sıklık>0).
RARE_PATTERNS = [
    r'Id = "([a-z0-9_]+)", Name = "[^"]*", Frequency',
    r'\bR\("([a-z0-9_]+)"',
    r'\bRec\("([a-z0-9_]+)"',
    r'\bB\("([a-z0-9_]+)"',
    r'Fruit\("([a-z0-9_]+)", "[^"]*", (?!0f)[0-9.]+f',
]
COMMON_PATTERNS = [
    r'Id = "([a-z0-9_]+)", Name = "[^"]*", (?:Size|Height)',
    r'Id = "(walker_[a-z0-9_]+)", Name = "[^"]*" \}',
    r'\b(?:C|T|P|D)\("([a-z0-9_]+)"',
    r'Fruit\("([a-z0-9_]+)", "[^"]*", 0f',
]


# Olta: her tür deftere girer (sıradan/nadir ayrımı yok).
SPECIES_PATTERNS = [r'new FishSpecies \{ Id = "([a-z0-9_]+)"']


def ids(patterns):
    found = set()
    for cs in SCRIPTS.rglob("*.cs"):
        text = cs.read_text(encoding="utf-8-sig", errors="ignore")
        for p in patterns:
            found.update(re.findall(p, text))
    return found


def main():
    rares, commons, species = ids(RARE_PATTERNS), ids(COMMON_PATTERNS), ids(SPECIES_PATTERNS)
    lines = ["# Görsel yuva manifesti", "",
             "> `python Tools/art_manifest.py` ile üretilir (elle düzenleme). Ortam Standardı v2 · görsel yuvaları.", "",
             "Her satır bir yuva: dosya adı yuvanın kimliğidir. Pixel art'a geçerken aynı adla aynı klasöre koy; "
             "sahne kurucuyu (ScrollRoad → Sahneleri Kur) yeniden çalıştır. Oyundaki boy kataloglardan gelir "
             "(Size/Height), görselin pikselinden bağımsızdır; en-boy oranını korumak yeterli. `_flesh` görselleri "
             "`Tools/gen_peeler.py --flesh` ile bütün meyveden türetilir.", ""]
    total = 0
    folders = sorted([p for p in ART.iterdir() if p.is_dir()]) + [VEHICLES]
    for folder in folders:
        pngs = sorted(folder.glob("*.png"))
        if not pngs:
            continue
        rel = folder.relative_to(ROOT).as_posix()
        lines += [f"## {rel}  ({len(pngs)})", "", "| Yuva | Tür | Boyut (px) | Dolu alan (px) |", "|---|---|---|---|"]
        for png in pngs:
            name = png.stem
            with Image.open(png) as im:
                w, h = im.size
                box = im.convert("RGBA").getbbox()
            bw, bh = (box[2] - box[0], box[3] - box[1]) if box else (0, 0)
            base = name[:-6] if name.endswith("_flesh") else name
            kind = ("araç" if folder == VEHICLES else "tür (defter)" if base in species else "nadir" if base in rares
                    else "sıradan" if base in commons else "diğer")
            if name.endswith("_flesh"):
                kind += " (soyulmuş)"
            lines.append(f"| `{name}` | {kind} | {w}×{h} | {bw}×{bh} |")
            total += 1
        lines.append("")
    lines.insert(4, f"Toplam **{total}** görsel yuvası.")
    lines.insert(5, "")
    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(OUT, total)


if __name__ == "__main__":
    main()
