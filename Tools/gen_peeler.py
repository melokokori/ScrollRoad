"""Meyve soyacağı ortamı görselleri (meyveler, soyulmuş hâlleri, seçici kartı): Gemini ile oyunun düz vektör
stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Makine, kabuk şeridi ve kâse oyunda kodla çizilir.
Soyulmuş hâl yapay zekâyla DEĞİL, aynı görselden türetilir (--flesh): siluet birebir aynı kalsın, oyunda bıçak
çizgisinden yukarısı bu görselden kırpılıp bütün meyvenin üstüne çizilir. Renkler PeelerEnvironment ile aynı.

Çıktı: Assets/Art/Peeler/<ad>.png ve <ad>_flesh.png. Ham: Tools/art_source/raw/peeler_<ad>.png
Kullanım (proje kökünden): python Tools/gen_peeler.py <ad> ["<tarif>"]   |   python Tools/gen_peeler.py --all
                           python Tools/gen_peeler.py --flesh   (soyulmuş hâlleri yeniden türet)
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

FRUIT = ("a single whole {what}, seen exactly from the side, {look}. Simple shapes, juicy and fresh. No knife, "
         "no cut, no slice, no plate, no ground, no shadow, no text, no other fruit")

ONLY = ("Same flat vector style as the reference but do NOT draw the rider, the scooter or any person - only "
        "this object")

TARIFLER = {
    # Sıradan meyveler (deftere girmez).
    "red_apple": FRUIT.format(what="red apple standing upright", look="glossy red skin with a few tiny yellow "
                              "speckles, a short brown stem and one small green leaf on top"),
    "green_apple": FRUIT.format(what="green apple standing upright", look="bright green skin with light speckles, "
                                "a short brown stem on top"),
    "pear": FRUIT.format(what="pear standing upright", look="yellow-green skin with tiny brown speckles, a short "
                         "brown stem on top"),
    "orange": FRUIT.format(what="orange", look="bright orange dimpled skin, a tiny green stem nub on top"),
    "lemon": FRUIT.format(what="lemon lying on its side", look="bright yellow skin with pointed ends"),
    "peach": FRUIT.format(what="peach standing upright", look="soft orange-yellow skin with a rosy red blush, "
                          "a gentle crease down one side, a tiny stem"),
    # Tezgâhın nadir meyveleri (meyve kâsesi).
    "mango": FRUIT.format(what="mango standing upright", look="smooth skin blending green, yellow and red"),
    "pineapple": FRUIT.format(what="pineapple standing upright", look="golden-brown diamond-patterned skin and a "
                              "tall spiky green crown on top"),
    "dragon_fruit": FRUIT.format(what="dragon fruit (pitaya) standing upright", look="bright magenta-pink skin "
                                 "with green-tipped scale leaves"),
    "cantaloupe": FRUIT.format(what="cantaloupe melon", look="beige netted skin over a pale green-orange base, a "
                               "small stem scar on top"),
    "pomelo": FRUIT.format(what="pomelo", look="a big round pale yellow-green citrus with thick dimpled skin and a "
                           "slightly pointed top"),
    "watermelon": FRUIT.format(what="small round watermelon", look="dark green skin with lighter green zigzag "
                               "stripes, a tiny stem on top"),
    # Activities kartı (makine oyunda kodla).
    "peeler_card": ("a cast iron apple peeler machine clamped on a wooden table: a red crank handle on the left, a "
                    "red apple turning on its fork, a long curly unbroken red apple peel hanging down. " + ONLY +
                    ". No sky, no text"),
}

# Soyulmuş hâlin eti (PeelerEnvironment.Color2) ve görselin üstünde özgün kalan kısım (sap/yaprak/taç).
FLESH = {
    "red_apple": (250, 237, 194), "green_apple": (247, 242, 199), "pear": (250, 242, 204), "orange": (252, 168, 64),
    "lemon": (252, 240, 158), "peach": (252, 204, 115), "mango": (252, 194, 56), "pineapple": (252, 224, 102),
    "dragon_fruit": (247, 245, 240), "cantaloupe": (252, 168, 97), "pomelo": (252, 209, 189),
    "watermelon": (242, 92, 97),
}
TOP_KEEP = {
    "red_apple": 0.22, "green_apple": 0.2, "pear": 0.16, "orange": 0.06, "lemon": 0.04, "peach": 0.1,
    "mango": 0.05, "pineapple": 0.42, "dragon_fruit": 0.02, "cantaloupe": 0.03, "pomelo": 0.05, "watermelon": 0.02,
}

OUT = Path("Assets/Art/Peeler")
RAW = Path("Tools/art_source/raw")
PREFIX = "peeler"


def make(name, desc=None):
    raw = generate(desc or TARIFLER[name])
    RAW.mkdir(parents=True, exist_ok=True)
    raw.save(RAW / f"{PREFIX}_{name}.png")
    OUT.mkdir(parents=True, exist_ok=True)
    finish(raw).save(OUT / f"{name}.png")
    print("tamam", name, flush=True)


def flesh(name):
    """Bütün meyveden soyulmuş hâli: aynı siluet, renk meyve eti (tonlama özgün parlaklıktan), üstteki sap/taç
    (TOP_KEEP oranı) özgün kalır. Oyunda bıçağın üstü bu görselden kırpılarak çizilir — iki görsel birebir
    çakışır."""
    img = Image.open(OUT / f"{name}.png").convert("RGBA")
    w, h = img.size
    px = img.load()
    box = img.getbbox()
    keep_y = box[1] + TOP_KEEP[name] * (box[3] - box[1])
    lums = [0.3 * r + 0.59 * g + 0.11 * b for y in range(h) for x in range(w)
            for r, g, b, a in [px[x, y]] if a > 128 and y >= keep_y]
    mean = sum(lums) / max(1, len(lums))
    fr, fg, fb = FLESH[name]
    out = img.copy()
    op = out.load()
    for y in range(h):
        if y < keep_y:
            continue
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = 0.3 * r + 0.59 * g + 0.11 * b
            shade = max(0.72, min(1.1, 0.95 + (lum - mean) / 255 * 0.6))
            op[x, y] = (min(255, int(fr * shade)), min(255, int(fg * shade)), min(255, int(fb * shade)), a)
    out.save(OUT / f"{name}_flesh.png")
    print("et", name, flush=True)


def main():
    arg = sys.argv[1]
    if arg == "--flesh":
        for name in FLESH:
            flesh(name)
        return
    if arg == "--all":
        from concurrent.futures import ThreadPoolExecutor

        todo = [n for n in TARIFLER if not (OUT / f"{n}.png").exists()]

        def one(name):
            try:
                make(name)
            except Exception as e:  # biri düşerse kalanlar sürsün
                print("HATA", name, e, flush=True)

        with ThreadPoolExecutor(4) as pool:
            list(pool.map(one, todo))
    elif arg == "--reprocess":
        name = sys.argv[2]
        raw = Image.open(RAW / f"{PREFIX}_{name}.png").convert("RGBA")
        finish(raw).save(OUT / f"{name}.png")
        print("tamam", name)
    else:
        make(arg, sys.argv[2] if len(sys.argv) > 2 else None)


if __name__ == "__main__":
    main()
