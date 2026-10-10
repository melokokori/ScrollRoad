"""Garaj renkleri: car_coral.png'nin yalnız gövde piksellerini (mercan tonları) yeniden renklendirir.

Gövde = ton 8° civarı (±22°), doygunluk > 0.3. Pencere (mavi), teker (koyu gri), farlar (sarı)
dokunulmaz. Yeni renk, mercan tabanına göre oransal uygulanır: gölgeli alt gövde aynı oranda
koyu kalır. Gizli araç ayrıca camları lavanta parıltıya çevirir.

Kullanım: python Tools/make_car_variants.py   (proje kökünden)
"""
import colorsys
from pathlib import Path

from PIL import Image

SRC = Path("Assets/Art/Vehicle/car_coral.png")
OUT = Path("Assets/Art/Vehicle")

BASE_H, BASE_S, BASE_V = 8 / 360, 0.66, 0.95

# id -> (gövde rengi, cam rengi ya da None)
VARIANTS = {
    "car_cherry": ("#C8323C", None),
    "car_mustard": ("#E8B23A", None),
    "car_mint": ("#6CC9A1", None),
    "car_forest": ("#2F7D5B", None),
    "car_teal": ("#2BB3C0", None),
    "car_navy": ("#2E4A8C", None),
    "car_lavender": ("#9B86D6", None),
    "car_graphite": ("#4A4F5A", None),
    "car_whisper": ("#3A2552", "#C9B4F0"),  # gizli bölge ödülü
}

GLASS_H = 200 / 360


def hex_hsv(h):
    r, g, b = (int(h[i:i + 2], 16) / 255 for i in (1, 3, 5))
    return colorsys.rgb_to_hsv(r, g, b)


def hue_dist(a, b):
    d = abs(a - b) % 1.0
    return min(d, 1.0 - d)


def recolor(src, body, glass):
    th, ts, tv = hex_hsv(body)
    gh, gs, gv = hex_hsv(glass) if glass else (None, None, None)
    out = src.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            ph, ps, pv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if ps > 0.3 and hue_dist(ph, BASE_H) < 22 / 360:
                nh, ns, nv = th, min(1, ps * ts / BASE_S), min(1, pv * tv / BASE_V)
            elif glass and ps > 0.15 and hue_dist(ph, GLASS_H) < 25 / 360:
                nh, ns, nv = gh, min(1, ps * gs / 0.34), min(1, pv * gv / 0.95)
            else:
                continue
            nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, nv)
            px[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return out


def main():
    src = Image.open(SRC).convert("RGBA")
    for name, (body, glass) in VARIANTS.items():
        path = OUT / f"{name}.png"
        recolor(src, body, glass).save(path)
        print(path)


if __name__ == "__main__":
    main()
