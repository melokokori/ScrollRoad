"""Kebap (çağ kebabı) ortamı görselleri (usta, porsiyonlar, seçici kartı): Gemini ile oyunun düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Porsiyonlar yandan hafif yukarıdan, tabağıyla birlikte,
zeminsiz; şiş, et, ocak ve bıçak oyunda kodla çizilir. Usta WALK kalıbındaki gibi referanstaki yüzsüz figür,
SOLA bakar, eli boş (bıçak kodla). Mavi anahtar renk yüzünden tariflerde mavi yok.

Çıktı: Assets/Art/Kebab/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/kebab_<ad>.png
Kullanım (proje kökünden): python Tools/gen_kebab.py <ad> ["<tarif>"]   |   python Tools/gen_kebab.py --all
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

DISH = ("{what}, seen from the side and slightly above, {look}. Simple shapes, appetizing. No table, no ground, "
        "no shadow, no text, no hands, no people")

ONLY = ("Same flat vector style as the reference but do NOT draw the rider, the scooter or any person - only "
        "this object")

TARIFLER = {
    # Sıradan porsiyonlar (deftere girmez).
    "lavash_wrap": DISH.format(what="a doner kebab lavash wrap (durum) cut in half on a small white plate",
                               look="thin flatbread rolled around sliced brown grilled meat, tomato and lettuce peeking out"),
    "half_bread": DISH.format(what="half a loaf of Turkish bread stuffed with doner kebab", look="crusty golden bread "
                              "filled with sliced brown grilled meat, onions and tomato"),
    "rice_plate": DISH.format(what="a white plate of doner kebab over rice", look="sliced brown grilled meat on a "
                              "mound of white rice pilaf, a grilled green pepper and a tomato slice"),
    "salad_plate": DISH.format(what="a white plate of doner kebab with salad", look="sliced brown grilled meat next "
                               "to shredded lettuce, red onion and tomato wedges"),
    "pita": DISH.format(what="a doner kebab in a round pita pocket", look="golden pita bread filled with sliced brown "
                        "grilled meat, lettuce and tomato"),
    "skewer": DISH.format(what="cag kebab on a small metal skewer lying on lavash bread on a white plate", look="thin "
                          "slices of brown grilled lamb on a flat skewer, a lemon wedge and red onion on the side"),
    # Kebapçının nadir porsiyonları (menü).
    "iskender": DISH.format(what="an Iskender kebab on a white plate", look="sliced doner meat over bread cubes, "
                            "red tomato sauce, melted golden butter and a scoop of white yogurt on the side"),
    "chicken_doner": DISH.format(what="a chicken doner wrap on a white plate", look="pale golden sliced chicken in "
                                 "a flatbread wrap with pickles and garlic sauce"),
    "gyros": DISH.format(what="a Greek gyros pita on a white plate", look="pita folded around sliced pork gyros, "
                         "tomato, onion, white tzatziki and a few fries"),
    "shawarma": DISH.format(what="a shawarma wrap on a white plate", look="flatbread wrap with sliced spiced meat, "
                            "white garlic sauce and pickled turnips"),
    "al_pastor": DISH.format(what="three tacos al pastor on a white plate", look="small corn tortillas with red "
                             "marinated sliced pork, pineapple pieces, onion and cilantro"),
    "sultans_platter": DISH.format(what="a grand round golden serving platter of mixed kebabs", look="sliced doner, "
                                   "skewers, grilled peppers, tomatoes, rice and a golden rim"),
    # Usta (tek karakter) ve Activities kartı.
    "chef": ("a kebab master standing, full body side view, facing LEFT, one arm stretched forward at chest height "
             "with the hand EMPTY (holding nothing). The person is the SAME faceless simple figure as the rider in the "
             "reference (no facial features). White chef jacket, a small white cap, a dark red apron, dark trousers, "
             "a thick moustache is NOT needed. No ground, no shadow, no knife"),
    "kebab_card": ("a horizontal cag kebab: a long cone of browned grilled meat on a horizontal steel skewer turning "
                   "over a small brick firebox with glowing embers and small flames, a plate with sliced kebab in front. "
                   + ONLY + ". No sky, no text"),
}

OUT = Path("Assets/Art/Kebab")
RAW = Path("Tools/art_source/raw")
PREFIX = "kebab"


def make(name, desc=None):
    raw = generate(desc or TARIFLER[name])
    RAW.mkdir(parents=True, exist_ok=True)
    raw.save(RAW / f"{PREFIX}_{name}.png")
    OUT.mkdir(parents=True, exist_ok=True)
    finish(raw).save(OUT / f"{name}.png")
    print("tamam", name, flush=True)


def main():
    arg = sys.argv[1]
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
