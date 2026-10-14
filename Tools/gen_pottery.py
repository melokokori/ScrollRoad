"""Çömlekçi çarkı ortamı görselleri (çömlekçi, pişmiş/sırlı çömlekler, seçici kartı): Gemini ile oyunun düz
vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Çömlekler tam yandan, dik, zeminsiz; çark ve şekillenen
çamur oyunda kodla çizilir (çamurun profili PotteryEnvironment.Shape). Çömlekçi WALK kalıbındaki gibi
referanstaki yüzsüz figür; çarkı ve çamuru çizmez (kodla). Mavi anahtar renk yüzünden tariflerde mavi yok
(İznik vazosunda lacivert yerine yeşil/kırmızı).

Çıktı: Assets/Art/Pottery/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/pottery_<ad>.png
Kullanım (proje kökünden): python Tools/gen_pottery.py <ad> ["<tarif>"]   |   python Tools/gen_pottery.py --all
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

POT = ("{what}, seen exactly from the side (straight side view, no perspective, the opening seen as a thin line), "
       "standing upright on nothing, perfectly symmetrical. {look}. Simple shapes. No ground, no table, no "
       "shadow, no text, no flowers, no other objects")

ONLY = ("Same flat vector style as the reference but do NOT draw the rider, the scooter or any person - only "
        "this object")

TARIFLER = {
    # Sıradan çömlekler (deftere girmez).
    "bowl": POT.format(what="a wide shallow handmade ceramic bowl", look="Cream glaze inside, warm terracotta "
                       "outside with a thin brown band"),
    "flower_pot": POT.format(what="a classic terracotta flower pot", look="Plain orange-brown clay, tapered "
                             "sides and a thick rolled rim"),
    "mug": POT.format(what="a handmade ceramic mug with a handle on the right", look="Speckled sage green "
                      "glaze, straight sides"),
    "jar": POT.format(what="a round handmade storage jar with a short neck", look="Honey yellow glaze dripping "
                      "over a brown clay base"),
    "cup": POT.format(what="a small round handmade tea bowl without a handle, as wide as it is tall", look="Off-white "
                      "glaze with a soft rust-red rim"),
    "plate": ("a handmade ceramic plate lying flat and horizontal, seen from the side exactly at eye level, so it "
              "looks like a very wide, very thin horizontal shape with a slightly raised rim at both ends, much wider "
              "than tall. Cream glaze with a brown edge. No ground, no table, no shadow, no text, no food"),
    # Atölyenin nadir çömlekleri (çömlek rafı).
    "water_jug": POT.format(what="a traditional Turkish clay water jug (testi) with one handle on the right and "
                            "a small spout", look="Unglazed warm red-brown clay with two thin white painted bands"),
    "tall_vase": POT.format(what="a tall slender ceramic vase", look="Glossy deep green glaze fading to cream at "
                            "the neck"),
    "amphora": POT.format(what="an ancient Greek amphora with two handles and a pointed base", look="Orange clay "
                          "with simple black geometric bands and a meander key pattern, no figures"),
    "teapot": POT.format(what="a round handmade ceramic teapot with a spout on the left, a handle on the right "
                         "and a small lid", look="Warm mustard yellow glaze with white dots"),
    "tulip_vase": POT.format(what="an Ottoman Iznik-style ceramic vase", look="White glaze painted with red "
                             "tulips and green leaves, a thin red rim"),
    "golden_urn": POT.format(what="an ornate round urn with a lid", look="Shining gold glaze with darker gold "
                             "patterns"),
    # Çömlekçi (tek karakter) ve Activities kartı.
    "potter": ("a potter sitting on a low wooden stool, full body side view, facing right, leaning forward with "
               "both forearms stretched forward at waist height and the hands cupped together as if shaping clay "
               "on a pottery wheel, but do NOT draw the wheel or any clay. The person is the SAME faceless simple "
               "figure as the rider in the reference (no facial features). Hair tied back with a headscarf, rolled "
               "sleeves, a clay-stained brown apron, dark trousers. No ground, no shadow"),
    "pottery_card": ("a potter's wheel with a half-made terracotta vase spinning on it and a small wooden shelf "
                     "with three finished glazed pots behind. " + ONLY + ". No sky, no text"),
}

OUT = Path("Assets/Art/Pottery")
RAW = Path("Tools/art_source/raw")
PREFIX = "pottery"


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
