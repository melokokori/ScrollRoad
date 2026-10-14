"""Dünyayı döndüren el ortamı görselleri (gezegenin kenarındaki yapılar, el, seçici kartı): Gemini ile oyunun
düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Yapılar tam yandan, zeminsiz; gezegen, Güneş ve yıldızlar
oyunda kodla çizilir. El insan değil: CREATURE kalıbındaki gibi "sürücüyü çizme" denir (referanstaki figürü
ekliyordu). Mavi anahtar renk yüzünden tariflerde mavi yok (kart görselinde okyanus camgöbeği).

Çıktı: Assets/Art/Globe/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/globe_<ad>.png
Kullanım (proje kökünden): python Tools/gen_globe.py <ad> ["<tarif>"]   |   python Tools/gen_globe.py --all
                           python Tools/gen_globe.py --tip   (elin parmak ucunun sprite içindeki yeri)
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

ITEM = ("{what}, seen exactly from the side like a flat stage set (straight side view, no perspective), standing "
        "upright on nothing. {look}. Simple shapes, ancient world style. No ground, no grass base, no people, no "
        "animals, no water, no shadow, no text, no sky")

ONLY = ("Same flat vector style as the reference but do NOT draw the rider, the scooter or any person - only "
        "this object")

TARIFLER = {
    # Sıradan yıllar (deftere girmez).
    "hut": ITEM.format(what="a small round prehistoric hut", look="Warm brown mud walls, a tall conical golden "
                       "thatched straw roof, a small dark doorway"),
    "tent": ITEM.format(what="a nomad tent", look="Cream and dark red striped fabric, a peaked top with a small "
                        "wooden pole, an open dark doorway flap"),
    "olive_tree": ITEM.format(what="a single olive tree", look="A twisted grey-brown trunk and a rounded crown of "
                              "silvery olive-green leaves"),
    "stone_house": ITEM.format(what="a small ancient Mediterranean stone house", look="Pale sandy stone block "
                               "walls, a flat roof with a low parapet, a wooden door and one small window"),
    "haystack": ITEM.format(what="a round haystack", look="Golden straw piled into a dome, a wooden pole "
                            "sticking out of the top"),
    "watchtower": ITEM.format(what="a tall wooden watchtower", look="Four wooden legs with cross bracing, a "
                              "small lookout hut with a pointed thatched roof on top, a wooden ladder"),
    # İlk Çağ'ın harikaları (kronik).
    "stonehenge": ITEM.format(what="Stonehenge, the ancient stone circle", look="Grey weathered standing stones "
                              "with flat lintel stones laid across their tops, four trilithons side by side"),
    "pyramids": ITEM.format(what="the three pyramids of Giza", look="Sandy golden pyramids side by side, the "
                            "middle one the biggest, each with one lighter sunlit face"),
    "great_wall": ITEM.format(what="a short section of the Great Wall of China", look="Grey stone wall with "
                              "crenellations running left to right, one square stone watchtower in the middle "
                              "with a small dark red pagoda roof"),
    "colosseum": ITEM.format(what="the Roman Colosseum", look="Warm cream travertine stone, the curved outer "
                             "wall with three rows of arches and a top storey, partly ruined and lower on the "
                             "right side"),
    "lighthouse": ITEM.format(what="the ancient Lighthouse of Alexandria", look="A very tall three-tiered white "
                              "stone tower: wide square base, narrower octagonal middle, small round top with a "
                              "bright orange fire burning on it"),
    "hanging_gardens": ITEM.format(what="the Hanging Gardens of Babylon", look="Stepped terraces of sandy stone "
                                   "rising like a ziggurat, lush green plants, small palm trees and vines hanging "
                                   "over every terrace edge"),
    # El (oyunda gezegeni iter) ve Activities kartı.
    "hand": ("ONLY ONE single cartoon human hand seen from the side, the index finger stretched out pointing "
             "straight to the LEFT, perfectly horizontal; the other three fingers curled into the palm, the thumb "
             "resting along the top; the wrist ends on the right side in a short white shirt cuff. Warm light "
             "skin tone. " + ONLY + ". No shadow, no text"),
    "globe_card": ("a big cartoon hand coming from the right whose index finger is pushing and spinning a small "
                   "round planet; the planet has green and sandy continents and a pale teal-green ocean, a tiny "
                   "hut and a tiny pyramid stand on its rim. " + ONLY + ". No stars, no sky, no text"),
}

OUT = Path("Assets/Art/Globe")
RAW = Path("Tools/art_source/raw")


def make(name, desc=None):
    raw = generate(desc or TARIFLER[name])
    RAW.mkdir(parents=True, exist_ok=True)
    raw.save(RAW / f"globe_{name}.png")
    OUT.mkdir(parents=True, exist_ok=True)
    finish(raw).save(OUT / f"{name}.png")
    print("tamam", name, flush=True)


def tip():
    """Elin parmak ucu: en soldaki opak sütunun ortası (0..1, soldan / alttan) — GlobeSceneView.handTip."""
    img = Image.open(OUT / "hand.png").convert("RGBA")
    w, h = img.size
    px = img.load()
    for x in range(w):
        rows = [y for y in range(h) if px[x, y][3] > 128]
        if rows:
            y = sum(rows) / len(rows)
            print(f"handTip = ({x / w:.3f}, {1 - y / h:.3f})")
            return


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
    elif arg == "--tip":
        tip()
    elif arg == "--reprocess":
        name = sys.argv[2]
        raw = Image.open(RAW / f"globe_{name}.png").convert("RGBA")
        finish(raw).save(OUT / f"{name}.png")
        print("tamam", name)
    else:
        make(arg, sys.argv[2] if len(sys.argv) > 2 else None)


if __name__ == "__main__":
    main()
