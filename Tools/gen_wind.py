"""Rüzgârgülü ortamı görselleri (elektrik verilen yapılar + seçici kartı): Gemini ile oyunun düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Yapılar tam yandan (perspektifsiz), zeminsiz çizilir;
türbinin kendisi oyunda kodla çizilir (pervane döner), buradaki turbine_card yalnız Activities kartı içindir.
Mavi anahtar renk yüzünden tariflerde mavi çatı/boya yok.

Çıktı: Assets/Art/Wind/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/wind_<ad>.png
Kullanım (proje kökünden): python Tools/gen_wind.py <ad> ["<tarif>"]   (tarif yoksa TARIFLER'den)
                           python Tools/gen_wind.py --all   (eksik olanların hepsi)
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

BUILDING = ("{what}, seen exactly from the side like a flat stage set (straight side view, no perspective, no "
            "roof seen from above), standing upright on nothing. {look}. The windows are dark and unlit. Cosy "
            "countryside village style, simple shapes. No ground, no grass, no trees, no people, no fence, no "
            "shadow, no text, no sky")

TARIFLER = {
    # Sıradan evler (deftere girmez).
    "home_cottage": BUILDING.format(what="a small one-storey stone cottage", look="Cream stone walls, a steep "
                                    "dark red tiled roof, a small brick chimney, one green front door and two small "
                                    "square windows with white frames"),
    "home_farmhouse": BUILDING.format(what="a two-storey farmhouse", look="White painted wooden walls, a dark grey "
                                      "gable roof, a covered front porch with two posts, four windows with dark "
                                      "green shutters"),
    "home_barn": BUILDING.format(what="a classic red barn", look="Dark red wooden plank walls with white trim, a "
                                 "big white X-braced double door in the middle, a small hay loft window above, a "
                                 "dark grey gambrel roof"),
    "home_townhouse": BUILDING.format(what="a narrow three-storey townhouse", look="Warm yellow ochre plaster "
                                      "walls, a dark brown steep roof with a small dormer, a wooden door and six "
                                      "tall narrow windows"),
    "home_bungalow": BUILDING.format(what="a wide low modern bungalow", look="Light grey walls with natural wood "
                                     "panels, a flat roof with a small solar panel, a big wide window and a "
                                     "white door"),
    "home_shop": BUILDING.format(what="a small village corner shop", look="Pale green walls, a striped red and "
                                 "white awning over a big shop window, a dark red roof, a small hanging sign "
                                 "without any text"),
    # Tepeler'in nadir yapıları (şebeke defteri).
    "bakery": BUILDING.format(what="a small village bakery", look="Warm brick walls, a brown tiled roof with a "
                              "smoking chimney, a big round arched shop window with loaves of bread inside, a "
                              "hanging golden pretzel sign"),
    "dairy": BUILDING.format(what="a dairy farm building with a tall grain silo next to it", look="White walls "
                             "with a dark red roof, a tall round silver metal silo with a domed top on its right, "
                             "a black and white cow standing in front of the door"),
    "school": BUILDING.format(what="a small village school", look="Red brick walls, a white bell tower with a "
                              "small brass bell on the roof, a big clock on the front, a row of tall windows, a "
                              "white double door"),
    "cinema": BUILDING.format(what="a small old-fashioned village cinema", look="Cream art deco facade with a "
                              "big rectangular marquee board with many small round light bulbs around its edge "
                              "and no letters, red velvet curtain behind the glass doors, a dark red roof"),
    "fairground": BUILDING.format(what="a small fairground Ferris wheel with a little ticket booth", look="A "
                                  "white metal Ferris wheel with eight colourful gondolas in red, yellow, green "
                                  "and orange, standing on a white A-frame stand, a tiny striped red and white "
                                  "ticket booth at its foot"),
    "observatory": BUILDING.format(what="a small hilltop observatory", look="A round white stone tower with a "
                                   "big silver-grey dome on top, the dome has a narrow open slit with a telescope "
                                   "poking out, a small wooden door"),
    # Activities seçicisindeki kart görseli (oyunda türbin kodla çizilir).
    "turbine_card": ("a modern three-blade white wind turbine standing on a small rounded green grassy hill, "
                     "side view, the whole turbine visible from the hill to the tip of the top blade, a tiny "
                     "cottage with a red roof at the foot of the hill. No sky, no clouds, no text"),
}

OUT = Path("Assets/Art/Wind")
RAW = Path("Tools/art_source/raw")


def make(name, desc=None):
    raw = generate(desc or TARIFLER[name])
    RAW.mkdir(parents=True, exist_ok=True)
    raw.save(RAW / f"wind_{name}.png")
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
        raw = Image.open(RAW / f"wind_{name}.png").convert("RGBA")
        finish(raw).save(OUT / f"{name}.png")
        print("tamam", name)
    else:
        make(arg, sys.argv[2] if len(sys.argv) > 2 else None)


if __name__ == "__main__":
    main()
