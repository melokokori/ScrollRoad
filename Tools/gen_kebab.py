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
    # ---- Ortam Standardı v2 (2026-10-25): Yunan Tavernası, Şavurma Tezgâhı, Taqueria, Gece Izgarası
    "souvlaki_pita": DISH.format(what="a souvlaki pita wrap on a small white plate", look="grilled pork pieces, tomato, onion and tzatziki in a folded pita"),
    "greek_salad": DISH.format(what="a Greek salad in a white bowl", look="tomato, cucumber, red onion, olives and a block of feta"),
    "fries_plate": DISH.format(what="a plate of golden fries with a lemon wedge", look="crispy fries sprinkled with oregano"),
    "tzatziki": DISH.format(what="a bowl of tzatziki with pita triangles", look="white yogurt dip with cucumber and olive oil"),
    "lemon_potatoes": DISH.format(what="a dish of Greek lemon potatoes", look="golden roasted potato wedges with herbs"),
    "bread_basket": DISH.format(what="a small basket of sesame bread", look="crusty round sesame bread slices"),
    "kleftiko": DISH.format(what="lamb kleftiko on a plate", look="slow-roasted lamb with potatoes and herbs on baking paper"),
    "octopus": DISH.format(what="a grilled octopus tentacle plate", look="charred octopus with lemon and olive oil"),
    "mezze_board": DISH.format(what="a wooden mezze board", look="olives, feta, dolmades, tzatziki and pita"),
    "loukoumades": DISH.format(what="a plate of loukoumades", look="golden honey doughnut balls with cinnamon"),
    "kontosouvli": DISH.format(what="kontosouvli pork on a long skewer over a plate", look="big juicy charred pork chunks on a spit"),
    "taverna_feast": DISH.format(what="a grand Greek feast platter", look="souvlaki skewers, gyros, salad, potatoes and pita on a big blue-rimmed platter"),
    "falafel_wrap": DISH.format(what="a falafel wrap cut in half", look="falafel balls, salad and tahini in flatbread"),
    "hummus": DISH.format(what="a bowl of hummus", look="creamy hummus with olive oil swirl, chickpeas and paprika"),
    "fattoush": DISH.format(what="a bowl of fattoush salad", look="lettuce, tomato, radish and crispy pita chips"),
    "manakish": DISH.format(what="a manakish flatbread", look="round flatbread topped with green za'atar"),
    "pickles": DISH.format(what="a small dish of pink pickled turnips and cucumbers", look="bright pink turnip sticks and green pickles"),
    "lentil_soup": DISH.format(what="a bowl of red lentil soup", look="orange soup with a lemon wedge"),
    "kibbeh": DISH.format(what="a plate of kibbeh", look="golden-brown torpedo-shaped croquettes with mint"),
    "mansaf": DISH.format(what="a mansaf platter", look="lamb on yellow rice with almonds and yogurt sauce"),
    "knafeh": DISH.format(what="a tray of knafeh", look="orange crispy pastry with melted cheese and pistachios"),
    "mixed_grill": DISH.format(what="a mixed grill platter", look="kofta, shish tawook and lamb skewers with grilled tomatoes"),
    "maqluba": DISH.format(what="a maqluba upside-down rice dome on a platter", look="rice dome with eggplant, chicken and pine nuts"),
    "royal_mezze": DISH.format(what="a royal mezze spread on a brass tray", look="many small bowls of dips, salads and grilled meats"),
    "taco_trio": DISH.format(what="three tacos on a plate", look="corn tortillas with meat, onion and cilantro, lime wedges"),
    "quesadilla": DISH.format(what="a quesadilla cut in wedges", look="golden tortilla with melted cheese"),
    "nachos": DISH.format(what="a plate of nachos", look="tortilla chips with cheese, jalapeños and salsa"),
    "elote": DISH.format(what="an elote corn cob on a stick", look="grilled corn with mayo, cheese and chili powder"),
    "guacamole": DISH.format(what="a stone bowl of guacamole with chips", look="chunky green guacamole"),
    "burrito": DISH.format(what="a burrito cut in half", look="flour tortilla with rice, beans and meat"),
    "enchiladas": DISH.format(what="a plate of enchiladas", look="rolled tortillas in red sauce with cheese"),
    "mole": DISH.format(what="a plate of chicken mole poblano", look="chicken in dark mole sauce with sesame seeds and rice"),
    "pozole": DISH.format(what="a bowl of red pozole", look="red broth with hominy, radish and cabbage"),
    "tamales": DISH.format(what="tamales on a plate", look="corn husk wrapped tamales, one opened"),
    "churros": DISH.format(what="churros with chocolate sauce", look="sugar-coated churros and a cup of chocolate"),
    "golden_trompo": DISH.format(what="a small golden al pastor trompo platter", look="a spit of marinated pork topped with pineapple and tacos around it"),
    "yakitori": DISH.format(what="yakitori skewers on a plate", look="grilled glazed chicken skewers with spring onion"),
    "gyoza": DISH.format(what="a plate of gyoza dumplings", look="pan-fried golden dumplings with dipping sauce"),
    "onigiri": DISH.format(what="two onigiri rice balls", look="white triangle rice balls with nori"),
    "edamame": DISH.format(what="a bowl of edamame", look="green soybean pods with salt"),
    "kimchi": DISH.format(what="a small dish of kimchi", look="red spicy fermented cabbage"),
    "ramen": DISH.format(what="a bowl of ramen", look="noodles, pork slices, egg, nori and spring onion"),
    "bulgogi": DISH.format(what="a sizzling plate of bulgogi", look="marinated beef with onions and sesame"),
    "char_siu": DISH.format(what="char siu pork slices on rice", look="red-glazed barbecued pork"),
    "peking_duck": DISH.format(what="a Peking duck platter", look="crispy glossy duck slices with pancakes and cucumber"),
    "wagyu_skewer": DISH.format(what="wagyu beef skewers on a plate", look="marbled seared beef cubes on skewers"),
    "katsu_curry": DISH.format(what="a plate of katsu curry", look="breaded pork cutlet with golden curry and rice"),
    "imperial_banquet": DISH.format(what="an imperial banquet tray", look="duck, dumplings, noodles and skewers on a red lacquer tray"),
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
