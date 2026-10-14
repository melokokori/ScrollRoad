"""Bumerang ortamı görselleri (atıcı, hedefler, seçici kartı): Gemini ile oyunun düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Hedefler tam yandan, zeminsiz; bumerang, zemin ve uzak
kaya oyunda kodla çizilir. Atıcı WALK kalıbındaki gibi referanstaki yüzsüz figür (eli boş: bumerang kodla).
Mavi anahtar renk yüzünden tariflerde mavi yok.

Çıktı: Assets/Art/Boomerang/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/boomerang_<ad>.png
Kullanım (proje kökünden): python Tools/gen_boomerang.py <ad> ["<tarif>"]   |   python Tools/gen_boomerang.py --all
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

TARGET = ("{what}, seen exactly from the side (straight side view, no perspective), standing upright on nothing. "
          "{look}. Simple shapes. No ground, no grass, no people, no animals, no shadow, no text, no letters, no sky")

ONLY = ("Same flat vector style as the reference but do NOT draw the rider, the scooter or any person - only "
        "this object")

TARIFLER = {
    # Sıradan hedefler (deftere girmez).
    "straw_target": TARGET.format(what="a round straw archery target on a wooden tripod stand", look="A golden "
                                  "straw bale disc facing the viewer with painted red, white and yellow rings"),
    "tin_cans": TARGET.format(what="a small pyramid of six tin cans stacked on a wooden crate", look="Silver tin "
                              "cans with red labels without any text, a light wooden crate"),
    "bottle_post": TARGET.format(what="a green glass bottle standing on top of a wooden fence post", look="A "
                                 "weathered grey-brown post, a dark green bottle with a cork"),
    "pumpkin_stump": TARGET.format(what="an orange pumpkin sitting on a tree stump", look="A round orange "
                                   "pumpkin with a short green stem on a brown cut tree stump showing rings"),
    "clay_pot_post": TARGET.format(what="a terracotta clay pot standing on a short wooden post", look="A round "
                                   "orange-brown clay pot with a narrow neck on a thick wooden post"),
    "wooden_sign": TARGET.format(what="a wooden plank board target on two short legs", look="Light wooden planks "
                                 "with three painted red and white rings, no writing"),
    # Avustralya çölünün nadir hedefleri (hedef defteri).
    "brass_bell": TARGET.format(what="a big brass bell hanging from a wooden A-frame", look="A shiny golden brass "
                                "bell with a dark clapper, a sturdy dark wooden frame"),
    "red_kite": TARGET.format(what="a red diamond-shaped kite caught at the top of a tall thin wooden pole",
                              look="A bright red kite with a long wavy tail of small yellow bows hanging down the pole"),
    "weather_vane": TARGET.format(what="an old iron rooster weather vane on a tall thin pole", look="A black "
                                  "iron rooster silhouette on an arrow, four short direction arms below it "
                                  "without letters, a dark grey pole"),
    "gong": TARGET.format(what="a large round bronze gong hanging in a wooden frame", look="A golden bronze "
                          "gong disc with a raised centre, a dark red wooden frame with curled top ends"),
    "paper_lantern": TARGET.format(what="a big round red paper lantern hanging from a curved wooden post",
                                   look="A glowing red paper lantern with golden top and bottom caps and a "
                                   "golden tassel"),
    "golden_trophy": TARGET.format(what="a shiny golden trophy cup on a tall wooden pedestal", look="A big "
                                   "golden cup with two curved handles on a dark wooden pedestal"),
    # Atıcı (tek karakter) ve Activities kartı.
    "thrower": ("a person standing, full body side view, facing right, in a throwing pose: leaning slightly back, "
                "the right arm raised up and back behind the head with the hand open and EMPTY (holding nothing), "
                "the left arm stretched forward pointing ahead. The person is the SAME faceless simple figure as the "
                "rider in the reference (no facial features). Wide-brimmed tan outback hat, khaki shirt with rolled "
                "sleeves, brown shorts, sturdy brown boots. No ground, no shadow, no boomerang"),
    "boomerang_card": ("a carved wooden boomerang with a painted orange stripe flying toward a round straw archery "
                       "target on a tripod, a few short speed lines behind the boomerang. " + ONLY + ". No ground, "
                       "no sky, no text"),
}

OUT = Path("Assets/Art/Boomerang")
RAW = Path("Tools/art_source/raw")
PREFIX = "boomerang"


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
