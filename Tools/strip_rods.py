"""Balıkçı görsellerinden makarayı ve elden sonraki kamış ucunu siler; kamışın ölçülerini yazar.

Olta skinleri (FishingGear, 2026-10-15) kamışın elden uca kısmını ve makarayı kodla, skin rengiyle çiziyor.
Bu betik Assets/Art/Fishing/fisherman*.png içinden o parçaları temizler (sap/el arkası kalır) ve
FishingSceneView'daki Look değerlerini (normalize, x soldan, y alttan) yazdırır:
  RodHand   kamışın elden çıktığı nokta (ekseni üzerinde)
  RodWidth  o noktada kamış kalınlığı (görsel yüksekliğine oranla)
  ReelFoot  makara ayağının kamışa değdiği nokta, ReelCenter makara gövdesi, ReelSize gövde çapı

Özgün görseller Tools/art_source/fishing_rods/ altına bir kez yedeklenir; betik hep oradan okur (tekrar
çalıştırılabilir). Kullanım: python Tools/strip_rods.py [--preview <klasör>]
"""
import math
import os
import shutil
import sys
from collections import deque

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "Art", "Fishing")
SRC = os.path.join(ROOT, "Tools", "art_source", "fishing_rods")

# Elle seçilen: sap üzerindeki bir nokta (ellerin altında), RodTip (FishingSceneView Look ile aynı, normalize),
# makara gövdesinden bir tohum piksel. Koordinatlar görsel pikseli, y aşağı.
IMAGES = {
    "fisherman": dict(grip=(305, 188), tip=(0.994, 0.970), seed=(360, 172)),
    "fisherman_river": dict(grip=(90, 262), tip=(0.995, 0.977), seed=(172, 247)),
    "fisherman_coast": dict(grip=(105, 262), tip=(0.992, 0.996), seed=(163, 245)),
    "fisherman_boat": dict(grip=(232, 178), tip=(0.902, 0.99), seed=(288, 166)),
    "fisherman_ice": dict(grip=(240, 250), tip=(0.995, 0.6), seed=(285, 262)),
}


def is_skin(c):
    r, g, b, a = c
    return a > 60 and r > 195 and 110 < g < 205 and 80 < b < 175 and r - g > 28


def is_warm(c):
    """Ten, mantar sap, sarı/zeytin mont: makara ve kamış (lacivert, gri, gümüş) değil."""
    r, g, b, a = c
    return a > 60 and r > b + 15


def is_navy(c):
    r, g, b, a = c
    return a > 40 and r < 90 and g < 100 and b < 130 and b >= r


def project(p, a, d):
    """p'nin a'dan d yönündeki eksene izdüşümü: (t, dik uzaklık işaretli)."""
    vx, vy = p[0] - a[0], p[1] - a[1]
    return vx * d[0] + vy * d[1], vx * -d[1] + vy * d[0]


def fit_axis(img, grip, tip):
    """Kamış eksenini navy piksellere en küçük karelerle oturtur (elden uca kısım)."""
    px = img.load()
    w, h = img.size
    d = (tip[0] - grip[0], tip[1] - grip[1])
    L = math.hypot(*d)
    d = (d[0] / L, d[1] / L)
    pts = []
    for y in range(h):
        for x in range(w):
            t, n = project((x, y), grip, d)
            if 0.35 * L < t < 0.97 * L and abs(n) < 6 and is_navy(px[x, y]):
                pts.append((x, y))
    # x'e göre y doğrusu (kamışlar yatıktan çapraza: dik değil)
    mx = sum(p[0] for p in pts) / len(pts)
    my = sum(p[1] for p in pts) / len(pts)
    sxx = sum((p[0] - mx) ** 2 for p in pts)
    sxy = sum((p[0] - mx) * (p[1] - my) for p in pts)
    k = sxy / sxx
    a = (mx, my)
    d = (1.0, k)
    L = math.hypot(*d)
    return a, (d[0] / L, d[1] / L)


def width_at(img, a, d, t):
    """Eksenin t noktasında dik doğrultuda navy kalınlığı."""
    px = img.load()
    w, h = img.size
    c = (a[0] + d[0] * t, a[1] + d[1] * t)
    count = 0
    for s in range(-12, 13):
        x, y = int(round(c[0] - d[1] * s)), int(round(c[1] + d[0] * s))
        if 0 <= x < w and 0 <= y < h and is_navy(px[x, y]):
            count += 1
    return count


def process(name, cfg, preview):
    src = os.path.join(SRC, name + ".png")
    img = Image.open(src).convert("RGBA")
    w, h = img.size
    px = img.load()
    tip = (cfg["tip"][0] * w, (1 - cfg["tip"][1]) * h)
    a, d = fit_axis(img, cfg["grip"], tip)
    t_tip, _ = project(tip, a, d)
    t_grip, _ = project(cfg["grip"], a, d)

    # El çıkışı: uçtan sapa doğru yürü; eksen bandında navy olmayan opak (ten, mantar sap) bulununca dur.
    t_hand = None
    t = t_tip - 3
    while t > t_grip:
        c = (a[0] + d[0] * t, a[1] + d[1] * t)
        blocked = False
        for s in (-1, 0, 1):
            x, y = int(round(c[0] - d[1] * s)), int(round(c[1] + d[0] * s))
            p = px[x, y]
            if p[3] > 160 and is_warm(p):
                blocked = True
        if blocked:
            t_hand = t + 2
            break
        t -= 0.5
    rod_w = max(width_at(img, a, d, t_hand + k) for k in (3, 5, 7))

    # Makara: tohumdan taşkın doldurma (ten ve kamış bandı engel).
    band = rod_w / 2 + 1.5
    seen = set()
    q = deque([cfg["seed"]])
    reel = []
    sx, sy = cfg["seed"]
    box = 34
    while q:
        x, y = q.popleft()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or abs(x - sx) > box or abs(y - sy) > box:
            continue
        seen.add((x, y))
        p = px[x, y]
        if p[3] < 30 or is_skin(p) or is_warm(p):
            continue
        tt, n = project((x, y), a, d)
        if abs(n) < band and tt > t_grip:
            continue
        reel.append((x, y))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
            q.append((x + dx, y + dy))
    rx0, rx1 = min(p[0] for p in reel), max(p[0] for p in reel)
    ry0, ry1 = min(p[1] for p in reel), max(p[1] for p in reel)
    # Ayak: makara piksellerinin kamışa en yakın olanı.
    foot = min(reel, key=lambda p: abs(project(p, a, d)[1]))
    t_foot, _ = project(foot, a, d)
    foot_axis = (a[0] + d[0] * t_foot, a[1] + d[1] * t_foot)
    # Gövde: kamıştan en uzak yarının ağırlık merkezi değil, kutu merkezi (sade çizim için yeterli).
    center = ((rx0 + rx1) / 2, (ry0 + ry1) / 2)
    size = max(rx1 - rx0, ry1 - ry0)

    # Silme: makara pikselleri + yarı saydam kenarları, elden sonraki kamış bandı ve kılavuz halkaları.
    out = img.copy()
    opx = out.load()
    erase = set(reel)
    for (x, y) in reel:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                xx, yy = x + dx, y + dy
                if 0 <= xx < w and 0 <= yy < h and not is_warm(px[xx, yy]):
                    erase.add((xx, yy))
    guide = rod_w / 2 + 8
    for y in range(h):
        for x in range(w):
            tt, n = project((x, y), a, d)
            if tt >= t_hand and abs(n) < guide and not is_warm(px[x, y]):
                erase.add((x, y))
    for (x, y) in erase:
        r, g, b, _ = opx[x, y]
        opx[x, y] = (r, g, b, 0)

    # Artık kırıntılar (makaranın krem makara kenarı, kılavuz ucu): makara kutusunda ya da elin ötesinde kalan
    # küçük (< 60 px) bağımsız lekeler silinir.
    seen = set()
    for y0 in range(h):
        for x0 in range(w):
            if (x0, y0) in seen or opx[x0, y0][3] <= 20:
                continue
            comp, q = [], deque([(x0, y0)])
            seen.add((x0, y0))
            while q:
                x, y = q.popleft()
                comp.append((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < w and 0 <= yy < h and (xx, yy) not in seen and opx[xx, yy][3] > 20:
                        seen.add((xx, yy))
                        q.append((xx, yy))
            if len(comp) >= 60:
                continue
            near_reel = all(rx0 - 6 <= x <= rx1 + 6 and ry0 - 6 <= y <= ry1 + 6 for x, y in comp)
            past_hand = all(project((x, y), a, d)[0] > t_hand - 4 for x, y in comp)
            if near_reel or past_hand:
                for x, y in comp:
                    r, g, b, _ = opx[x, y]
                    opx[x, y] = (r, g, b, 0)
    out.save(os.path.join(ART, name + ".png"))

    hand = (a[0] + d[0] * t_hand, a[1] + d[1] * t_hand)

    def norm(p):
        return (p[0] / w, 1 - p[1] / h)

    print(f"{name}: RodHand=({norm(hand)[0]:.3f}f, {norm(hand)[1]:.3f}f) RodWidth={rod_w / h:.4f}f "
          f"ReelFoot=({norm(foot_axis)[0]:.3f}f, {norm(foot_axis)[1]:.3f}f) "
          f"ReelCenter=({norm(center)[0]:.3f}f, {norm(center)[1]:.3f}f) ReelSize={size / h:.4f}f  "
          f"[px hand={hand[0]:.0f},{hand[1]:.0f} w={rod_w} reel={rx0}-{rx1},{ry0}-{ry1}]")

    if preview:
        k = 3
        bg = Image.new("RGBA", (w * 2, h), (255, 255, 255, 255))
        bg.alpha_composite(img, (0, 0))
        bg.alpha_composite(out, (w, 0))
        bg.convert("RGB").resize((w * 2 * k // 2, h * k // 2)).save(os.path.join(preview, name + "_strip.png"))


def main():
    preview = None
    if "--preview" in sys.argv:
        preview = sys.argv[sys.argv.index("--preview") + 1]
        os.makedirs(preview, exist_ok=True)
    os.makedirs(SRC, exist_ok=True)
    for name, cfg in IMAGES.items():
        backup = os.path.join(SRC, name + ".png")
        if not os.path.exists(backup):
            shutil.copy2(os.path.join(ART, name + ".png"), backup)
        process(name, cfg, preview)


if __name__ == "__main__":
    main()
