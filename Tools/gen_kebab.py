"""Kebap (dikey döner) ortamı görselleri (usta, porsiyonlar, seçici kartı): Gemini ile oyunun düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Porsiyonlar yandan hafif yukarıdan, tabağıyla birlikte,
zeminsiz; şiş, et, ocak ve bıçak oyunda kodla çizilir. Usta WALK kalıbındaki gibi referanstaki yüzsüz figür,
SOLA bakar, eli yumruk (bıçak kodla, 2026-10-25 dikey döner). Mavi anahtar renk yüzünden tariflerde mavi yok.

Çıktı: Assets/Art/Kebab/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/kebab_<ad>.png
Kullanım (proje kökünden): python Tools/gen_kebab.py <ad> ["<tarif>"]   |   python Tools/gen_kebab.py --all
          ustadan sonra: python Tools/gen_kebab.py --split
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
    # ---- Dört döner kültürü (2026-10-25 gece, kullanıcı: "ortaya çıkan yemekler alakasız"): her mekânın her porsiyonu
    # ustanın şişten kestiği etle yapılır. Yunan tavernası = gyros, şavurma tezgâhı = shawarma, taqueria = al pastor
    # (trompo), gece ızgarası = Berlin gece dönercisi. Et hep ince kesilmiş, kızarmış dilimler (şiş/köfte yok).
    "chicken_gyros": DISH.format(what="a chicken gyros pita wrap on a small white plate", look="pita folded around pale golden sliced chicken, tomato, onion, white tzatziki and a few fries"),
    "gyros_plate": DISH.format(what="a gyros plate", look="a pile of thin sliced browned pork gyros meat, fries, tomato slices, red onion, a scoop of white tzatziki and pita triangles on a white plate"),
    "gyros_baguette": DISH.format(what="a gyros baguette sandwich on a small white plate", look="a crusty baguette split open and stuffed with thin sliced browned gyros meat, tomato, onion and tzatziki"),
    "gyros_box": DISH.format(what="an open paper takeaway box of gyros and fries", look="thin sliced browned gyros meat over golden fries with white tzatziki drizzled on top"),
    "open_pita": DISH.format(what="an open-faced gyros pita on a white plate", look="a flat round pita topped with thin sliced browned gyros meat, tomato, onion and paprika, not folded"),
    "yogurt_gyros": DISH.format(what="gyros with yogurt on a white plate", look="thin sliced browned gyros meat over pita squares, covered with thick white yogurt and red tomato sauce"),
    "double_pita": DISH.format(what="two gyros pita wraps side by side on a white plate", look="two folded pitas packed with thin sliced browned gyros meat, tomato, onion, tzatziki and fries"),
    "feta_gyros": DISH.format(what="a gyros pita with feta on a white plate", look="a folded pita with thin sliced browned gyros meat, crumbled white feta cheese, tomato and olives"),
    "spicy_gyros": DISH.format(what="a spicy gyros pita on a white plate", look="a folded pita with thin sliced browned gyros meat, red chili sauce, sliced red chili peppers and onion"),
    "lamb_gyros": DISH.format(what="a lamb gyros plate", look="thin sliced dark browned lamb gyros meat, lemon potatoes, a lemon wedge and fresh oregano on a white plate"),
    "gyros_tower": DISH.format(what="a tall stacked gyros platter on a round golden-rimmed plate", look="a tall pile of thin sliced browned gyros meat on layered pitas, crowned with tzatziki, tomato and fries around it"),
    "arabi": DISH.format(what="an arabi shawarma platter", look="a shawarma wrap cut into small toasted bite-size pieces, with fries, white garlic sauce and pink pickled turnips on a white plate"),
    "shawarma_plate": DISH.format(what="a shawarma plate", look="a big heap of thin sliced spiced golden-brown chicken shawarma meat as the main thing, a small scoop of rice, white garlic sauce and pink pickles on a white plate"),
    "saj_shawarma": DISH.format(what="a saj shawarma wrap on a small white plate", look="a very thin large saj flatbread rolled tight around sliced spiced chicken shawarma and garlic sauce, toasted"),
    "samoon": DISH.format(what="a samoon shawarma sandwich on a small white plate", look="a diamond-shaped crusty samoon bread stuffed with sliced spiced shawarma meat, tomato and pickles"),
    "shawarma_fries": DISH.format(what="a tray of shawarma fries", look="golden fries topped with sliced spiced chicken shawarma, white garlic sauce and parsley"),
    "beef_shawarma": DISH.format(what="a beef shawarma wrap on a small white plate", look="flatbread with thin sliced dark browned beef shawarma, tahini sauce, tomato and parsley"),
    "toum_special": DISH.format(what="a shawarma wrap with extra garlic sauce on a small white plate", look="a toasted wrap of sliced chicken shawarma with a big bowl of fluffy white toum garlic sauce beside it"),
    "shawarma_fatteh": DISH.format(what="a bowl of shawarma fatteh", look="crispy pita pieces under sliced chicken shawarma, warm white yogurt, pine nuts and parsley"),
    "kaak_shawarma": DISH.format(what="a kaak shawarma sandwich on a small white plate", look="a round sesame kaak bread ring split open and filled with sliced shawarma meat and garlic sauce"),
    "lamb_shawarma": DISH.format(what="a lamb shawarma plate", look="thin sliced dark browned lamb shawarma on rice with almonds, tahini and grilled tomato on a white plate"),
    "royal_shawarma": DISH.format(what="a royal shawarma platter on a large round brass tray", look="mountains of sliced chicken and beef shawarma, rice, fries, garlic sauce, pickles and flatbread"),
    "gringa": DISH.format(what="a gringa on a small white plate", look="a flour tortilla folded over red marinated sliced al pastor pork, melted cheese and pineapple pieces"),
    "torta_pastor": DISH.format(what="a torta al pastor sandwich on a small white plate", look="a crusty Mexican bread roll stuffed with red marinated sliced al pastor pork, avocado slices and onion"),
    "alambre": DISH.format(what="an alambre on a small cast iron plate", look="chopped red al pastor pork with green peppers, onion and melted cheese, tortillas on the side"),
    "volcan": DISH.format(what="three volcanes al pastor on a white plate", look="crispy flat tostadas with melted cheese and red marinated al pastor pork on top"),
    "pastor_quesadilla": DISH.format(what="a pastor quesadilla cut in wedges on a white plate", look="golden tortilla with melted cheese and red marinated sliced al pastor pork inside"),
    "costra": DISH.format(what="two tacos de costra on a small white plate", look="tacos wrapped in a crispy golden fried cheese crust, filled with red marinated al pastor pork and cilantro, the plate only slightly larger than the tacos"),
    "pastor_burrito": DISH.format(what="a burrito al pastor cut in half on a white plate", look="flour tortilla with red marinated sliced al pastor pork, rice, beans and pineapple"),
    "huarache_pastor": DISH.format(what="a huarache al pastor on a white plate", look="a long oval masa flatbread topped with red al pastor pork, salsa, crumbled white cheese and cilantro"),
    "pineapple_special": DISH.format(what="tacos al pastor with a grilled pineapple wedge on a white plate", look="three corn tortilla tacos with red al pastor pork and a big caramelized pineapple slice"),
    "golden_trompo": DISH.format(what="a small golden al pastor trompo platter", look="a spit of marinated pork topped with pineapple and tacos around it"),
    "tacos_arabes": DISH.format(what="tacos arabes on a white plate", look="thick soft pita-like flatbreads rolled around sliced spiced pork, with a small cup of chipotle sauce"),
    "doner_brot": DISH.format(what="a Berlin doner kebab in toasted bread on a small white plate", look="a wedge of toasted Turkish flatbread stuffed with thin sliced browned doner meat, lettuce, red cabbage, tomato, onion and white herb sauce"),
    "berlin_durum": DISH.format(what="a Berlin durum wrap cut in half on a small white plate", look="thin yufka flatbread rolled around sliced browned doner meat, red cabbage, lettuce and white sauce"),
    "doner_teller": DISH.format(what="a doner plate", look="a big pile of thin sliced browned doner meat, fries and salad with red cabbage and white sauce on a white plate"),
    "doner_box": DISH.format(what="an open cardboard doner box", look="golden fries topped with thin sliced browned doner meat, white garlic sauce and red chili sauce"),
    "lahmacun_roll": DISH.format(what="a lahmacun doner roll cut in half on a small white plate", look="a thin round lahmacun rolled around sliced browned doner meat, lettuce, tomato and lemon"),
    "doner_halloumi": DISH.format(what="a doner bread with grilled halloumi on a small white plate", look="toasted Turkish flatbread with sliced browned doner meat, grilled halloumi slices and salad"),
    "gemuse_doner": DISH.format(what="a vegetable chicken doner in toasted bread on a small white plate", look="toasted flatbread stuffed with sliced chicken doner, roasted potato, carrot and zucchini pieces, feta crumbles and a lemon squeeze"),
    "cheese_doner": DISH.format(what="a cheese doner in toasted bread on a small white plate", look="toasted flatbread with sliced browned doner meat under thick melted golden cheese"),
    "mit_alles": DISH.format(what="an overstuffed doner with everything on a small white plate", look="toasted flatbread bursting with sliced doner meat, salad, red cabbage, tomato, onion, white sauce, red chili sauce and feta"),
    "scharf_doner": DISH.format(what="an extra spicy doner in toasted bread on a small white plate", look="sliced doner meat with lots of red chili flakes, red hot sauce and green chili peppers"),
    "midnight_teller": DISH.format(what="a huge late-night doner platter on an oval plate", look="a mountain of sliced browned doner meat, fries, rice, salad, white sauce and red chili sauce"),
    "golden_doner": DISH.format(what="a luxury doner in toasted bread on a round golden plate", look="sliced browned doner meat, thin gold leaf flakes, rocket leaves and a glossy sauce"),
    # Kebapçının yeni porsiyonları (dönerden: tombik, pideli, kaşarlı dürüm) ve saf döner Sultan tabağı.
    "tombik": DISH.format(what="a tombik doner on a small white plate", look="a round puffy tombik bread split open and stuffed with thin sliced browned doner meat, tomato, onion and pickles"),
    "pideli_doner": DISH.format(what="a pideli doner on a white plate", look="thin sliced browned doner meat over pide bread squares, melted golden butter, a scoop of white yogurt and a grilled green pepper"),
    "kasarli_durum": DISH.format(what="a cheese durum wrap cut in half on a small white plate", look="thin lavash rolled around sliced browned doner meat with stringy melted cheese and tomato"),
    # Sıradan porsiyonlar (deftere girmez).
    "lavash_wrap": DISH.format(what="a doner kebab lavash wrap (durum) cut in half on a small white plate",
                               look="thin flatbread rolled around sliced brown grilled meat, tomato and lettuce peeking out"),
    "half_bread": DISH.format(what="half a loaf of Turkish bread stuffed with doner kebab", look="crusty golden bread "
                              "filled with sliced brown grilled meat, onions and tomato"),
    "rice_plate": DISH.format(what="a white plate of doner kebab over rice", look="sliced brown grilled meat on a "
                              "mound of white rice pilaf, a grilled green pepper and a tomato slice"),
    "doner_portion": DISH.format(what="a white plate of doner kebab with salad", look="sliced brown grilled meat next "
                               "to shredded lettuce, red onion and tomato wedges"),
    "pita": DISH.format(what="a doner kebab in a round pita pocket", look="golden pita bread filled with sliced brown "
                        "grilled meat, lettuce and tomato"),
    "cag_kebab": DISH.format(what="cag kebab on a small metal skewer lying on lavash bread on a white plate", look="thin "
                          "slices of brown grilled lamb on a flat skewer, a lemon wedge and red onion on the side"),
    # Kebapçının nadir porsiyonları (menü).
    "iskender": DISH.format(what="an Iskender kebab on a white plate", look="sliced doner meat over bread cubes, "
                            "red tomato sauce, melted golden butter and a scoop of white yogurt on the side"),
    "chicken_doner": DISH.format(what="a chicken doner wrap on a white plate", look="pale golden sliced chicken in "
                                 "a flatbread wrap with pickles and garlic sauce"),
    "pita_gyros": DISH.format(what="a Greek gyros pita on a white plate", look="pita folded around sliced pork gyros, "
                         "tomato, onion, white tzatziki and a few fries"),
    "shawarma_wrap": DISH.format(what="a shawarma wrap on a white plate", look="flatbread wrap with sliced spiced meat, "
                            "white garlic sauce and pickled turnips"),
    "tacos_pastor": DISH.format(what="three tacos al pastor on a white plate", look="small corn tortillas with red "
                             "marinated sliced pork, pineapple pieces, onion and cilantro"),
    "sultans_platter": DISH.format(what="a grand round golden serving platter of doner", look="a mountain of thin sliced browned doner meat, iskender with tomato sauce and yogurt, pide pieces, "
                                   "grilled peppers, tomatoes, rice and a golden rim, no skewers"),
    # Usta (tek karakter) ve Activities kartı.
    "chef": ("a doner kebab master standing, full body side view, facing LEFT, the near arm raised forward at "
             "chest height with the hand CLOSED in a fist as if gripping the handle of a long knife, but do NOT draw "
             "the knife. The other arm relaxed at the side. The person is the SAME faceless simple figure as the "
             "rider in the reference (no facial features). White chef jacket with rolled sleeves, a small white cap, "
             "a dark red apron, dark trousers. No ground, no shadow, no knife"),
    "kebab_card": ("a vertical doner kebab stand: a tall cone of browned grilled meat on a vertical steel spit "
                   "turning in front of a glowing orange vertical grill panel, a steel drip tray below, a plate "
                   "with a doner wrap beside it. " + ONLY + ". No sky, no text"),
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


# Ustanın ön kolu (2026-10-25, dikey döner): chef.png aynı tuvalde ikiye ayrılır — chef_arm (yumruk + bıçak sapı + ön
# kol) ve chef_body (geri kalan). Kol oyunda dirsekten (ARM_PIVOT, piksel, sol üstten) döner ve gövdenin ARKASINDA
# çizilir: dirseği kolluk örter. Kolluğun altına giren ucu ten rengi bir şeritle uzatılır ki dönünce boşluk açılmasın.
# Sayılar chef.png'ye özel: usta yeniden üretilirse yeniden ölçülmeli (KebabSceneView.ArmPivotPx aynı değer).
ARM_CUT_X, ARM_MAX_Y, ARM_PIVOT = 76, 232, (84, 188)


def split_chef():
    from PIL import ImageDraw

    img = Image.open(OUT / "chef.png").convert("RGBA")
    w, h = img.size
    px = img.load()
    arm = Image.new("RGBA", (w, h))
    body = img.copy()
    ap, bp = arm.load(), body.load()
    for y in range(min(h, ARM_MAX_Y)):
        for x in range(min(w, ARM_CUT_X)):
            if px[x, y][3] > 0:
                ap[x, y] = px[x, y]
                bp[x, y] = (0, 0, 0, 0)
    ImageDraw.Draw(arm).line([(64, 183), (104, 196)], fill=(239, 174, 142, 255), width=26)
    arm.save(OUT / "chef_arm.png")
    body.save(OUT / "chef_body.png")
    print("tamam chef_arm, chef_body")


def main():
    arg = sys.argv[1]
    if arg == "--split":
        split_chef()
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
