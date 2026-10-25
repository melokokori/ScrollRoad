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

# Çağlara göre (Ortam Standardı v2): ITEM'in aynısı, stil çağın.
ERA = ("{what}, seen exactly from the side like a flat stage set (straight side view, no perspective), standing "
       "upright on nothing. {look}. Simple shapes, {era} style. No ground, no grass base, no people, no animals, no "
       "water, no shadow, no text, no sky")

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
    # ---- Ortam Standardı v2 (2026-10-25): Orta Çağ, Rönesans, Sanayi Çağı, Uzay Çağı
    "cottage": ERA.format(what="a small medieval cottage", look="Cream timber-framed walls with dark brown beams, a steep golden thatched roof, a tiny wooden door", era="medieval"),
    "windmill": ERA.format(what="a medieval post windmill", look="A tall wooden body with a small roof, four lattice sails in an X shape", era="medieval"),
    "chapel": ERA.format(what="a small stone chapel", look="Grey stone walls, a red tiled roof, a small bell turret with a cross, one arched window", era="medieval"),
    "market_stall": ERA.format(what="a medieval market stall", look="A wooden table with a striped red and cream awning, baskets of apples and bread", era="medieval"),
    "stone_well": ERA.format(what="a stone village well", look="A round grey stone wall, a small wooden roof on two posts, a rope and bucket", era="medieval"),
    "oak_tree": ERA.format(what="a big old oak tree", look="A thick brown trunk and a wide round crown of dark green leaves", era="medieval"),
    "tower_of_pisa": ERA.format(what="the Leaning Tower of Pisa", look="A white marble round bell tower with stacked rows of arches, clearly leaning to the right", era="medieval"),
    "notre_dame": ERA.format(what="Notre-Dame cathedral in Paris, front facade", look="Two square grey stone towers, a big round rose window in the middle, three arched doorways", era="medieval"),
    "hagia_sophia": ERA.format(what="the Hagia Sophia", look="A huge pinkish-sand dome over a square building, smaller half domes, four slim minarets", era="medieval"),
    "alhambra": ERA.format(what="the Alhambra palace", look="Warm reddish fortress walls and square towers on a low hill, cypress trees, small arched windows", era="medieval"),
    "angkor_wat": ERA.format(what="Angkor Wat temple", look="Grey-sand stone temple with five tall corncob-shaped towers, the middle one tallest, stepped terraces", era="medieval"),
    "machu_picchu": ERA.format(what="Machu Picchu", look="Grey stone terraces and small stone houses on a green mountain ridge, a tall green peak behind", era="medieval"),
    "townhouse": ERA.format(what="a Renaissance Italian townhouse", look="Ochre plastered walls, green shutters, a terracotta tiled roof, an arched doorway", era="Renaissance"),
    "cypress": ERA.format(what="a tall slim cypress tree", look="A narrow dark green flame-shaped tree on a short brown trunk", era="Renaissance"),
    "fountain": ERA.format(what="a stone town fountain", look="A round grey stone basin with a carved column in the middle spraying water in arcs", era="Renaissance"),
    "bell_tower": ERA.format(what="a tall Renaissance bell tower", look="A slim square brick tower with an open arched belfry and a small pointed roof", era="Renaissance"),
    "stone_bridge": ERA.format(what="a small arched stone bridge", look="Pale stone with two round arches and low walls on top", era="Renaissance"),
    "workshop": ERA.format(what="an artist's workshop", look="A small stone building with a wide open doorway, an easel with a painting standing outside", era="Renaissance"),
    "florence_dome": ERA.format(what="Florence Cathedral with its great dome", look="A huge red-tiled octagonal dome with white ribs and a small lantern on top, white and green marble walls", era="Renaissance"),
    "st_peters": ERA.format(what="St Peter's Basilica in Rome", look="A tall pale grey dome on a drum with columns, a wide classical facade below", era="Renaissance"),
    "taj_mahal": ERA.format(what="the Taj Mahal", look="A white marble mausoleum with a big onion dome, four slim white minarets, a pointed arch entrance", era="Renaissance"),
    "forbidden_city": ERA.format(what="a hall of the Forbidden City in Beijing", look="A wide red hall with golden yellow double curved roofs on a white stone terrace", era="Renaissance"),
    "st_basils": ERA.format(what="Saint Basil's Cathedral in Moscow", look="Colorful swirled onion domes in red, green, gold and white on brick towers", era="Renaissance"),
    "versailles": ERA.format(what="the Palace of Versailles", look="A long cream palace facade with many tall windows, a grey roof with gold trim, a fountain in front", era="Renaissance"),
    "factory": ERA.format(what="a Victorian factory", look="A red brick building with a saw-tooth roof and two tall brick chimneys puffing grey smoke", era="Industrial Revolution"),
    "brick_house": ERA.format(what="a Victorian brick house", look="Red brick walls, a grey slate roof with two chimneys, white window frames", era="Industrial Revolution"),
    "water_tower": ERA.format(what="a wooden water tower", look="A round wooden tank with a cone roof on four tall steel legs with a ladder", era="Industrial Revolution"),
    "locomotive": ERA.format(what="a steam locomotive", look="A dark green steam engine with red wheels, a tall black funnel and a puff of white steam", era="Industrial Revolution"),
    "gas_lamp": ERA.format(what="a Victorian street gas lamp", look="A tall black iron post with a glowing yellow glass lantern on top", era="Industrial Revolution"),
    "terrace": ERA.format(what="a row of terraced houses", look="Three joined narrow brick houses with white doors and chimney pots on the roof", era="Industrial Revolution"),
    "big_ben": ERA.format(what="the Big Ben clock tower", look="A tall golden-sand stone gothic tower with a big white clock face and a pointed dark spire", era="Industrial Revolution"),
    "eiffel_tower": ERA.format(what="the Eiffel Tower", look="A tall tapering bronze-brown iron lattice tower with arches at the base", era="Industrial Revolution"),
    "statue_of_liberty": ERA.format(what="the Statue of Liberty", look="A pale green copper statue holding up a torch with a golden flame, standing on a stone pedestal", era="Industrial Revolution"),
    "tower_bridge": ERA.format(what="Tower Bridge in London", look="Two pale stone gothic towers joined by a high walkway, a road bridge between them", era="Industrial Revolution"),
    "crystal_palace": ERA.format(what="the Crystal Palace", look="A long glass and white iron exhibition hall with a big arched glass roof in the middle", era="Industrial Revolution"),
    "brooklyn_bridge": ERA.format(what="the Brooklyn Bridge", look="Two stone towers with pointed double arches and fanning steel cables", era="Industrial Revolution"),
    "apartments": ERA.format(what="a modern apartment block", look="A tall white concrete building with rows of windows and small balconies", era="modern"),
    "radio_mast": ERA.format(what="a red and white radio mast", look="A tall thin lattice tower striped red and white with small antennas", era="modern"),
    "satellite_dish": ERA.format(what="a big satellite dish", look="A white round dish antenna tilted up on a grey stand", era="modern"),
    "modern_house": ERA.format(what="a modern flat-roof house", look="White cube walls, big glass windows, a flat roof, a small tree beside it", era="modern"),
    "petrol_station": ERA.format(what="a small petrol station", look="A flat red canopy over two pumps and a small white shop", era="modern"),
    "wind_turbine": ERA.format(what="a white wind turbine", look="A tall white tower with three long slim blades", era="modern"),
    "sydney_opera": ERA.format(what="the Sydney Opera House", look="White overlapping shell-shaped roofs on a sand-colored base", era="modern"),
    "cn_tower": ERA.format(what="the CN Tower in Toronto", look="A very tall slim grey concrete tower with a round pod near the top and a thin antenna", era="modern"),
    "atomium": ERA.format(what="the Atomium in Brussels", look="Nine shiny silver spheres joined by silver tubes in a cube shape, standing on three legs", era="modern"),
    "burj_khalifa": ERA.format(what="the Burj Khalifa skyscraper", look="A very tall silver glass tower stepping in as it rises to a needle point", era="modern"),
    "observatory_dome": ERA.format(what="a giant observatory telescope", look="A big white round dome with an open slit and a telescope pointing out at the sky", era="modern"),
    "launch_pad": ERA.format(what="a Moon rocket on its launch tower", look="A tall white rocket with black bands next to an orange steel launch tower", era="space age"),
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

        with ThreadPoolExecutor(8) as pool:
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
