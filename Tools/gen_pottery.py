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
    # ---- Ortam Standardı v2 (2026-10-25): Yunan Atölyesi, Japon Fırını, Fas Çarşısı, Modern Stüdyo
    "kylix": POT.format(what="an ancient Greek kylix, a very wide shallow drinking cup on a short stem foot", look="Black glaze with a terracotta orange band"),
    "lekythos": POT.format(what="an ancient Greek lekythos oil flask, slim body, narrow neck, small handle on the right", look="Terracotta orange with black bands"),
    "skyphos": POT.format(what="an ancient Greek skyphos, a deep cup with two small handles on both sides", look="Black glaze with a thin orange line"),
    "olive_jar": POT.format(what="a round Greek storage jar", look="Plain terracotta with a pale wavy band"),
    "drinking_bowl": POT.format(what="a wide shallow Greek clay bowl", look="Terracotta outside, black glaze inside"),
    "mezze_plate": "a small Greek clay plate lying flat and horizontal, seen from the side exactly at eye level, so it looks like a thin wide shape. White glaze with a blue rim. No ground, no shadow, no text",
    "hero_vase": POT.format(what="an ancient Greek black-figure vase", look="Terracotta orange with black silhouettes of a running hero and a horse, black bands"),
    "krater": POT.format(what="an ancient Greek bell krater, a wide mixing bowl with two handles near the base of the rim", look="Black glaze with orange figures of dancers"),
    "hydria": POT.format(what="an ancient Greek hydria water jar with a vertical handle at the back and two side handles", look="Terracotta with black meander pattern"),
    "owl_jug": POT.format(what="a round Greek jug painted with an owl", look="Terracotta with a big black owl, the owl has two round eyes"),
    "dolphin_plate": "a Greek clay plate lying flat and horizontal, seen from the side exactly at eye level, so it looks like a thin wide shape. Black glaze with orange dolphins along the rim. No ground, no shadow, no text",
    "olympic_amphora": POT.format(what="a tall ancient Greek amphora with two handles and a pointed foot", look="Black glaze with an orange runner and a golden olive wreath band"),
    "yunomi": POT.format(what="a tall Japanese yunomi tea cup without a handle", look="Grey-blue ash glaze with dripping runs"),
    "rice_bowl": POT.format(what="a Japanese rice bowl", look="White porcelain with a simple blue wave pattern"),
    "sake_bottle": POT.format(what="a Japanese sake bottle (tokkuri) with a round body and narrow neck", look="Dark brown glaze with a cream band"),
    "soba_cup": POT.format(what="a Japanese soba cup, slightly flared", look="Indigo blue with a white dotted pattern"),
    "sushi_plate": "a long rectangular Japanese ceramic plate lying flat and horizontal, seen from the side exactly at eye level, so it looks like a thin wide shape. Green oribe glaze. No ground, no shadow, no text",
    "incense_pot": POT.format(what="a small round Japanese incense pot", look="Celadon green glaze"),
    "raku_bowl": POT.format(what="a Japanese raku tea bowl, hand-shaped", look="Crackled black and copper glaze"),
    "tea_caddy": POT.format(what="a small Japanese tea caddy jar with an ivory lid", look="Glossy brown-black glaze"),
    "ikebana_vase": POT.format(what="a tall slim Japanese ikebana vase", look="Matte black with a single red brush stroke"),
    "kintsugi_bowl": POT.format(what="a Japanese tea bowl repaired with kintsugi", look="Cream glaze with shining golden crack lines"),
    "dragon_jar": POT.format(what="a round Asian jar painted with a dragon", look="White porcelain with a blue dragon winding around it"),
    "moon_jar": POT.format(what="a large round moon jar", look="Soft milky white glaze, perfectly plain"),
    "tagine": POT.format(what="a Moroccan tagine, a flat round base dish with a tall cone-shaped lid", look="Terracotta with a painted cream lid"),
    "spice_jar": POT.format(what="a small Moroccan spice jar", look="Saffron yellow glaze with a dark pattern"),
    "couscous_bowl": POT.format(what="a wide Moroccan bowl", look="Green glaze with a white star pattern"),
    "clay_cup": POT.format(what="a simple Moroccan clay cup", look="Plain red-brown clay with a thin painted band"),
    "oil_jar": POT.format(what="a Moroccan clay oil jar", look="Terracotta with a white zigzag band"),
    "serving_dish": "a wide Moroccan ceramic serving dish lying flat and horizontal, seen from the side exactly at eye level, so it looks like a thin wide shape. Cobalt blue with white patterns. No ground, no shadow, no text",
    "blue_tagine": POT.format(what="a Moroccan tagine with a tall cone lid", look="Bright cobalt blue glaze with white and yellow geometric patterns"),
    "zellige_plate": "a Moroccan plate lying flat and horizontal, seen from the side exactly at eye level, so it looks like a thin wide shape. Colorful zellige tile pattern in teal, white and terracotta. No ground, no shadow, no text",
    "lantern_pot": POT.format(what="a Moroccan ceramic lantern shaped like a tall onion with a pointed top", look="Terracotta with pierced star patterns"),
    "brass_ewer": POT.format(what="a Moroccan ewer with a long spout and a handle", look="Teal glaze with a shiny brass rim"),
    "peacock_vase": POT.format(what="a tall Moroccan vase", look="Peacock blue and green glaze with gold patterns"),
    "sultans_jar": POT.format(what="a grand lidded Moroccan jar", look="Deep red glaze with gold and turquoise patterns, a small golden finial on the lid"),
    "espresso_cup": POT.format(what="a small modern espresso cup with a small handle on the right", look="Matte charcoal outside, white inside rim"),
    "planter": POT.format(what="a modern cylinder planter", look="Matte terracotta pink"),
    "bud_vase": POT.format(what="a small modern bud vase with a slim neck", look="Pale sage green matte glaze"),
    "ramen_bowl": POT.format(what="a deep modern ramen bowl", look="Speckled oatmeal glaze with a dark rim"),
    "milk_jug": POT.format(what="a modern milk jug with a handle on the right", look="Glossy white"),
    "dinner_plate": "a modern dinner plate lying flat and horizontal, seen from the side exactly at eye level, so it looks like a thin wide shape. Matte grey with a raw clay rim. No ground, no shadow, no text",
    "speckled_vase": POT.format(what="a modern speckled vase", look="Cream glaze with dark brown speckles"),
    "ribbed_lamp": POT.format(what="a modern ceramic lamp base with vertical ribs", look="Matte mustard yellow"),
    "bubble_vase": POT.format(what="a modern bubble-shaped vase with a very small neck", look="Glossy cobalt blue"),
    "neon_jar": POT.format(what="a modern jar", look="Bright neon pink and orange gradient glaze"),
    "marbled_bowl": POT.format(what="a modern bowl", look="Marbled swirls of white, black and terracotta clay"),
    "platinum_vase": POT.format(what="a tall elegant modern vase", look="Shiny platinum silver glaze"),
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

        with ThreadPoolExecutor(8) as pool:
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
