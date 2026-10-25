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
    # ---- Ortam Standardı v2 (2026-10-25): Tropik Pazar, Meyve Bahçesi, Sebze Bahçesi, Gece Pazarı
    "kiwi": FRUIT.format(what="kiwi fruit standing upright", look="fuzzy brown skin"),
    "lime": FRUIT.format(what="lime", look="glossy bright green skin with a tiny stem nub"),
    "guava": FRUIT.format(what="guava standing upright", look="pale yellow-green skin"),
    "passion_fruit": FRUIT.format(what="passion fruit", look="wrinkled deep purple skin"),
    "lychee": FRUIT.format(what="lychee", look="bumpy red skin with a tiny stem"),
    "avocado": FRUIT.format(what="avocado standing upright", look="dark green pebbly skin"),
    "papaya": FRUIT.format(what="papaya standing upright", look="smooth yellow-orange skin blushing green at the top"),
    "coconut": FRUIT.format(what="whole coconut", look="hairy brown husk"),
    "rambutan": FRUIT.format(what="rambutan", look="red skin covered in soft green-tipped hairs"),
    "mangosteen": FRUIT.format(what="mangosteen", look="deep purple round skin with a green leafy cap on top"),
    "jackfruit": FRUIT.format(what="jackfruit", look="huge oval green-yellow fruit with small knobbly bumps"),
    "durian": FRUIT.format(what="durian", look="big oval fruit covered in sharp green-brown spikes"),
    "yellow_apple": FRUIT.format(what="yellow apple standing upright", look="golden yellow skin, a short brown stem"),
    "plum": FRUIT.format(what="plum", look="dusky purple skin with a soft bloom"),
    "apricot": FRUIT.format(what="apricot", look="soft orange skin with a red blush"),
    "nectarine": FRUIT.format(what="nectarine", look="smooth red and yellow skin"),
    "quince": FRUIT.format(what="quince standing upright", look="lumpy golden yellow skin with a little fuzz"),
    "tangerine": FRUIT.format(what="tangerine", look="bright orange skin with a tiny green leaf"),
    "pomegranate": FRUIT.format(what="pomegranate", look="deep red skin with a small crown on top"),
    "fig": FRUIT.format(what="fig standing upright", look="purple skin, teardrop shape"),
    "blood_orange": FRUIT.format(what="blood orange", look="orange skin with a deep red blush"),
    "honeydew": FRUIT.format(what="honeydew melon", look="smooth pale green-cream skin"),
    "persimmon": FRUIT.format(what="persimmon", look="glossy orange skin with a green leafy calyx on top"),
    "golden_apple": FRUIT.format(what="golden apple standing upright", look="shining metallic gold skin, a short stem and a golden leaf"),
    "potato": FRUIT.format(what="potato", look="earthy tan skin with small eyes"),
    "carrot": FRUIT.format(what="carrot standing upright", look="orange root with a bushy green leafy top"),
    "cucumber": FRUIT.format(what="cucumber standing upright", look="dark green bumpy skin"),
    "onion": FRUIT.format(what="onion", look="papery golden-brown skin with a dry tip"),
    "beetroot": FRUIT.format(what="beetroot", look="deep purple-red root with green leaf stalks on top"),
    "turnip": FRUIT.format(what="turnip", look="white root with a purple top and green leaves"),
    "sweet_potato": FRUIT.format(what="sweet potato standing upright", look="dusky red-purple skin"),
    "butternut": FRUIT.format(what="butternut squash standing upright", look="smooth beige skin, bulb at the bottom"),
    "eggplant": FRUIT.format(what="eggplant standing upright", look="glossy deep purple skin with a green cap"),
    "kohlrabi": FRUIT.format(what="kohlrabi", look="round pale purple bulb with leaf stalks sprouting up"),
    "pumpkin": FRUIT.format(what="pumpkin", look="ribbed orange skin with a thick green stem"),
    "prize_marrow": FRUIT.format(what="giant prize-winning marrow standing upright", look="striped dark and light green skin"),
    "asian_pear": FRUIT.format(what="Asian pear", look="round golden-brown speckled skin"),
    "yuzu": FRUIT.format(what="yuzu", look="bumpy bright yellow skin with a small leaf"),
    "kumquat": FRUIT.format(what="kumquat", look="small oval orange skin"),
    "sugar_apple": FRUIT.format(what="sugar apple", look="knobbly pale green segmented skin"),
    "longan": FRUIT.format(what="longan", look="smooth tan skin"),
    "jujube": FRUIT.format(what="jujube", look="glossy red-brown skin"),
    "hami_melon": FRUIT.format(what="Hami melon", look="oval golden skin with fine netting"),
    "buddhas_hand": FRUIT.format(what="Buddha's hand citron", look="bright yellow fingered fruit"),
    "horned_melon": FRUIT.format(what="horned melon (kiwano)", look="spiky bright orange skin"),
    "snake_fruit": FRUIT.format(what="snake fruit (salak)", look="scaly reddish-brown skin like snake scales"),
    "pink_pineapple": FRUIT.format(what="pineapple standing upright", look="golden-pink diamond skin and a tall green crown"),
    "square_watermelon": FRUIT.format(what="perfect cube-shaped watermelon", look="dark and light green striped skin, square sides"),
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
    # Manav (tek karakter, 2026-10-25: elle soyma) ve Activities kartı; meyve ve bıçak oyunda ayrı/kodla.
    "grocer": ("a grocer sitting on a low wooden stool, full body side view, facing right, leaning slightly forward, "
               "both forearms raised in front of the chest: the far hand cupped open as if holding a round fruit, "
               "the near hand pinched as if holding a small paring knife, but do NOT draw any fruit or knife. The "
               "person is the SAME faceless simple figure as the rider in the reference (no facial features). Short "
               "hair, a mustard yellow shirt with rolled sleeves, a green canvas apron, brown trousers. No ground, "
               "no shadow"),
    "peeler_card": ("a grocer sitting on a small stool peeling a red apple by hand with a small knife, one long "
                    "curly unbroken red peel hanging down into a small bucket, wooden crates of fruit beside. Same "
                    "flat vector style as the reference, faceless figure like the rider, but no scooter. No sky, "
                    "no text"),
}

# Soyulmuş hâlin eti (PeelerEnvironment.Color2) ve görselin üstünde özgün kalan kısım (sap/yaprak/taç).
FLESH = {
    "red_apple": (250, 237, 194), "green_apple": (247, 242, 199), "pear": (250, 242, 204), "orange": (252, 168, 64),
    "lemon": (252, 240, 158), "peach": (252, 204, 115), "mango": (252, 194, 56), "pineapple": (252, 224, 102),
    "dragon_fruit": (247, 245, 240), "cantaloupe": (252, 168, 97), "pomelo": (252, 209, 189),
    "watermelon": (242, 92, 97),
    "kiwi": (150, 196, 70), "lime": (206, 232, 140), "guava": (246, 140, 140), "passion_fruit": (250, 200, 60), "lychee": (246, 242, 230), "avocado": (214, 226, 120), "papaya": (246, 130, 70), "coconut": (250, 248, 240), "rambutan": (246, 242, 232), "mangosteen": (250, 248, 244), "jackfruit": (250, 200, 70), "durian": (250, 230, 150), "yellow_apple": (250, 240, 196), "plum": (240, 200, 90), "apricot": (252, 190, 90), "nectarine": (252, 206, 110), "quince": (250, 236, 190), "tangerine": (252, 160, 50), "pomegranate": (230, 60, 80), "fig": (230, 110, 110), "blood_orange": (180, 30, 50), "honeydew": (190, 230, 140), "persimmon": (250, 150, 60), "golden_apple": (252, 240, 190), "potato": (250, 236, 180), "carrot": (250, 150, 50), "cucumber": (210, 236, 170), "onion": (250, 240, 220), "beetroot": (170, 30, 80), "turnip": (250, 248, 240), "sweet_potato": (250, 150, 60), "butternut": (250, 160, 60), "eggplant": (246, 240, 210), "kohlrabi": (240, 246, 220), "pumpkin": (250, 170, 60), "prize_marrow": (240, 240, 200), "asian_pear": (250, 244, 220), "yuzu": (250, 236, 150), "kumquat": (252, 190, 80), "sugar_apple": (250, 248, 236), "longan": (240, 240, 226), "jujube": (246, 236, 200), "hami_melon": (250, 170, 90), "buddhas_hand": (252, 240, 200), "horned_melon": (120, 190, 70), "snake_fruit": (250, 244, 230), "pink_pineapple": (250, 160, 180), "square_watermelon": (240, 80, 90),
}
TOP_KEEP = {
    "red_apple": 0.22, "green_apple": 0.2, "pear": 0.16, "orange": 0.06, "lemon": 0.04, "peach": 0.1,
    "mango": 0.05, "pineapple": 0.42, "dragon_fruit": 0.02, "cantaloupe": 0.03, "pomelo": 0.05, "watermelon": 0.02,
    "kiwi": 0.02, "lime": 0.03, "guava": 0.05, "passion_fruit": 0.04, "lychee": 0.05, "avocado": 0.04, "papaya": 0.04, "coconut": 0.02, "rambutan": 0.02, "mangosteen": 0.18, "jackfruit": 0.03, "durian": 0.04, "yellow_apple": 0.2, "plum": 0.06, "apricot": 0.06, "nectarine": 0.08, "quince": 0.12, "tangerine": 0.05, "pomegranate": 0.12, "fig": 0.1, "blood_orange": 0.05, "honeydew": 0.03, "persimmon": 0.16, "golden_apple": 0.2, "potato": 0.02, "carrot": 0.22, "cucumber": 0.03, "onion": 0.1, "beetroot": 0.24, "turnip": 0.22, "sweet_potato": 0.02, "butternut": 0.05, "eggplant": 0.16, "kohlrabi": 0.3, "pumpkin": 0.08, "prize_marrow": 0.04, "asian_pear": 0.08, "yuzu": 0.08, "kumquat": 0.06, "sugar_apple": 0.04, "longan": 0.06, "jujube": 0.08, "hami_melon": 0.03, "buddhas_hand": 0.06, "horned_melon": 0.04, "snake_fruit": 0.08, "pink_pineapple": 0.42, "square_watermelon": 0.02,
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


# Manavın bıçak tutan eli (2026-10-25): grocer.png aynı tuvalde ikiye ayrılır — grocer_hand (HAND_BOX içi: çimdikleyen
# el ve ön kolu) meyvenin ÖNÜNDE, grocer_body (geri kalan, meyveyi tutan avuç dahil) ARKASINDA çizilir. Hareket yok,
# yalnız katman. Kutu grocer.png'ye özel (sol üstten piksel): manav yeniden üretilirse yeniden ölçülmeli.
HAND_BOX = (215, 0, 336, 190)


def split_grocer():
    img = Image.open(OUT / "grocer.png").convert("RGBA")
    hand = Image.new("RGBA", img.size)
    body = img.copy()
    x0, y0, x1, y1 = HAND_BOX
    region = img.crop(HAND_BOX)
    hand.paste(region, (x0, y0))
    body.paste(Image.new("RGBA", region.size), (x0, y0))
    hand.save(OUT / "grocer_hand.png")
    body.save(OUT / "grocer_body.png")
    print("tamam grocer_hand, grocer_body")


def main():
    arg = sys.argv[1]
    if arg == "--split":
        split_grocer()
        return
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

        with ThreadPoolExecutor(6) as pool:
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
