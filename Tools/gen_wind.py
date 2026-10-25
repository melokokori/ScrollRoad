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

# Ortam Standardı v2: diğer vadilerin yapıları (BUILDING'in aynısı, stil vadinin).
VALLEY = ("{what}, seen exactly from the side like a flat stage set (straight side view, no perspective, no "
          "roof seen from above), standing upright on nothing. {look}. The windows are dark and unlit. {style} style, "
          "simple shapes. No ground, no grass, no trees, no people, no fence, no shadow, no text, no sky")

TARIFLER = {
    # ---- Ortam Standardı v2 (2026-10-25): Kıyı, Çöl, Karlı Geçit, Şehir
    "fish_market": VALLEY.format(what="a seaside fish market hall", look="Pale blue painted wooden walls, a striped blue and white awning, crates of fish under it", style="Seaside town"),
    "lighthouse": VALLEY.format(what="a tall red and white striped lighthouse with a small keeper's cottage", look="Red and white bands, a black lantern room on top", style="Seaside town"),
    "boathouse": VALLEY.format(what="a wooden boathouse", look="Weathered teal planks, a wide arched door at the front with a small rowing boat inside", style="Seaside town"),
    "seaside_pier": VALLEY.format(what="a Victorian seaside pier pavilion on stilts", look="White pavilion with a domed roof and bunting on wooden stilts", style="Seaside town"),
    "aquarium": VALLEY.format(what="a modern aquarium building", look="Curved white walls, big blue-tinted glass windows with fish shapes painted on", style="Seaside town"),
    "lifeboat_station": VALLEY.format(what="a lifeboat station with a slipway", look="Red roof, white walls, big doors with an orange lifeboat inside", style="Seaside town"),
    "oasis_inn": VALLEY.format(what="a desert oasis inn", look="Sandy adobe walls, rounded corners, a palm-leaf shade over the door", style="Desert village"),
    "caravanserai": VALLEY.format(what="a caravanserai trading post", look="Sand-colored walls with a big arched gateway and small towers at each end", style="Desert village"),
    "date_farm": VALLEY.format(what="a date farm house with tall date palms beside it", look="Low adobe house with a flat roof and two date palms with orange fruit", style="Desert village"),
    "solar_farm": VALLEY.format(what="a small solar farm building", look="A low white control hut with rows of tilted blue solar panels beside it", style="Desert village"),
    "sand_fort": VALLEY.format(what="an old desert fort", look="Tall sandy mud-brick walls with crenellations and a round corner tower", style="Desert village"),
    "mirage_palace": VALLEY.format(what="a shimmering desert palace", look="White domes and slender towers with turquoise tiles and golden tips", style="Desert village"),
    "ski_lodge": VALLEY.format(what="an alpine ski lodge", look="Dark wooden chalet with a steep snowy roof, a balcony and a pair of skis by the door", style="Alpine village"),
    "mountain_hut": VALLEY.format(what="a small stone mountain hut", look="Grey stone walls, a wooden door, a snowy roof and a small chimney", style="Alpine village"),
    "cable_car": VALLEY.format(what="a cable car station", look="A concrete station with a big wheel on top and a red cable car cabin hanging out", style="Alpine village"),
    "ice_rink": VALLEY.format(what="an outdoor ice rink pavilion", look="A wooden pavilion with a striped red roof over a small pale blue ice rink", style="Alpine village"),
    "research_station": VALLEY.format(what="a polar research station", look="Orange boxy modules on stilts with a radio mast and a satellite dish", style="Alpine village"),
    "ice_hotel": VALLEY.format(what="an ice hotel", look="Walls made of translucent pale blue ice blocks, an arched entrance and an ice sculpture", style="Alpine village"),
    "diner": VALLEY.format(what="an American retro diner", look="Shiny chrome and red stripes, big windows, a round neon-style sign without letters", style="City"),
    "train_station": VALLEY.format(what="a classic city train station", look="Red brick building with a big arched glass window and a clock tower", style="City"),
    "theatre": VALLEY.format(what="an old city theatre", look="Cream stone facade with columns, red curtains in the windows and a marquee with bulbs and no letters", style="City"),
    "stadium": VALLEY.format(what="a small football stadium", look="Curved grey stands with floodlight towers at both ends", style="City"),
    "art_museum": VALLEY.format(what="a modern art museum", look="A swirling white curved building with tall glass panels", style="City"),
    "skyscraper": VALLEY.format(what="a tall glass skyscraper", look="Blue-green glass tower with a spire on top", style="City"),
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

        with ThreadPoolExecutor(6) as pool:
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
