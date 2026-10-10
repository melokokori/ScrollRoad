"""Kara araçlarının tekerlek konumlarını taban görsellerden bulur.

Çıktı: Assets/StreamingAssets/Vehicles/wheels.txt — satır başına bir model:
    <model> x y r hub; x y r hub; ...
x, y: tekerlek merkezi (görselin sol-alt köşesinden, genişlik/yükseklik oranı olarak),
r: lastik yarıçapı, hub: jant (göbek) yarıçapı — ikisi de görsel GENİŞLİĞİNE oranla.
Oyun (VehicleArt.Wheels) bu konumlara dönen jant işaretleri koyar.

Yöntem: tekerlekler görselin en altına değer. Alt kenara değen sütun öbeklerinin ortası merkez x'i
verir; yarıçap, merkez (x, alt - R) olmak üzere R taranarak bulunur: alt yarıda 0.9R halkası koyu
(lastik), 1.12R halkası koyu değilse en iyi R. Jant = R - (alttan yukarı ilk koyu bandın kalınlığı).

Kullanım: python Tools/find_wheels.py [--sheet kontrol.png] [model ...]
(model verilmezse aşağıdaki LAND listesi; --sheet işaretli kontrol sayfası çizer).
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VEHICLES = os.path.join(ROOT, "Assets", "StreamingAssets", "Vehicles")
OUT = os.path.join(VEHICLES, "wheels.txt")

# Tekerlekli kara araçları (Sno-Doo paletli: dahil değil). Sahne kurucudaki kataloğu izler.
LAND = [
    "car", "atmaca", "zefir", "coralsedan", "t10s", "dreier", "van", "impulso", "type3", "chaser", "towne",
    "panorama", "apparition",
    "duner", "taiga", "jinny", "pickup", "atv", "t10x", "landcrawler", "guardian", "wrangel", "mercatorg",
    "ibex", "escapade", "hammerhx", "toro",
    "mobilo", "pcx", "vesper", "javo", "chimp", "atlasgs", "anfield", "bonnevale", "baron", "heavyboy",
    "tarantula", "hayate", "monza",
]


def detect(model):
    rgba = np.asarray(Image.open(os.path.join(VEHICLES, model + ".png")).convert("RGBA")).astype(int)
    alpha = rgba[:, :, 3] > 100
    h, w = alpha.shape
    dark = alpha & (rgba[:, :, :3].max(axis=2) < 110)

    env = np.array([np.where(alpha[:, x])[0].max() if alpha[:, x].any() else -1 for x in range(w)])
    bottom = env.max()
    contact = env >= bottom - 3

    patches = []
    x = 0
    while x < w:
        if contact[x]:
            s = x
            while x < w and contact[x]:
                x += 1
            if patches and s - patches[-1][1] < w * 0.03:
                patches[-1][1] = x - 1
            else:
                patches.append([s, x - 1])
        else:
            x += 1

    def is_dark(px, py):
        return 0 <= px < w and 0 <= py < h and dark[py, px]

    wheels = []
    for s, e in patches:
        cx = (s + e) / 2
        best = None
        for r in range(int(h * 0.06), int(min(h, w) * 0.4)):
            cy = bottom - r + 1
            inside = outside = 0
            angles = np.linspace(185, 355, 30)
            for ang in angles:
                t = math.radians(ang)
                inside += is_dark(int(round(cx + 0.9 * r * math.cos(t))), int(round(cy - 0.9 * r * math.sin(t))))
                outside += is_dark(int(round(cx + 1.12 * r * math.cos(t))), int(round(cy - 1.12 * r * math.sin(t))))
            score = (inside - outside) / len(angles)
            if best is None or score > best[0]:
                best = (score, r)
        r = best[1]
        cy = bottom - r + 1

        tire = 0
        xi = int(round(cx))
        for y in range(bottom, -1, -1):
            if dark[y, xi]:
                tire += 1
            elif tire > 0:
                break
        hub = max(1, r - tire)
        wheels.append((cx / w, 1 - cy / h, r / w, hub / w))
    return wheels


def draw_sheet(path, results):
    cells = []
    for model, wheels in results.items():
        im = Image.open(os.path.join(VEHICLES, model + ".png")).convert("RGBA")
        bg = Image.new("RGBA", im.size, (90, 140, 90, 255))
        bg.alpha_composite(im)
        d = ImageDraw.Draw(bg)
        W, H = im.size
        for x, y, r, hub in wheels:
            cx, cy = x * W, (1 - y) * H
            d.ellipse([cx - r * W, cy - r * W, cx + r * W, cy + r * W], outline=(255, 0, 255), width=3)
            d.ellipse([cx - hub * W, cy - hub * W, cx + hub * W, cy + hub * W], outline=(0, 255, 255), width=3)
        bg.thumbnail((300, 170))
        cells.append((model, bg))
    cols, cw, ch = 5, 310, 190
    sheet = Image.new("RGB", (cols * cw, ((len(cells) + cols - 1) // cols) * ch), (40, 40, 40))
    d = ImageDraw.Draw(sheet)
    for k, (model, im) in enumerate(cells):
        x, y = (k % cols) * cw, (k // cols) * ch
        sheet.paste(im, (x + 5, y + 5))
        d.text((x + 5, y + 175), model, fill=(255, 255, 255))
    sheet.save(path)


def main():
    args = sys.argv[1:]
    sheet = None
    if "--sheet" in args:
        i = args.index("--sheet")
        sheet = args[i + 1]
        del args[i:i + 2]
    models = args or LAND

    existing = {}
    if os.path.exists(OUT):
        for line in open(OUT, encoding="utf-8"):
            if line.strip() and not line.startswith("#"):
                existing[line.split()[0]] = line.rstrip("\n")

    results = {m: detect(m) for m in models}
    for model, wheels in results.items():
        existing[model] = model + " " + "; ".join(" ".join(f"{v:.4f}" for v in wh) for wh in wheels)
        print(existing[model])

    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Tools/find_wheels.py ile üretildi: model x y r hub; ... (x,y merkez; r,hub genişliğe oranla)\n")
        for model in sorted(existing):
            f.write(existing[model] + "\n")
    if sheet:
        draw_sheet(sheet, results)


if __name__ == "__main__":
    main()
