"""Döner kapı ortamı görselleri (yürüyen ziyaretçiler): Gemini ile oyunun düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px). Figürler yandan, SAĞA yürür; yüzsüz sade figür.

Çıktı: Assets/Art/Door/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/door_<ad>.png
Kullanım (proje kökünden): python Tools/gen_visitors.py <ad> ["<tarif>"]   (tarif yoksa TARIFLER'den)
                           python Tools/gen_visitors.py --all   (eksik olanların hepsi)
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

WALK = ("a person walking, full body side view, facing and walking to the right, mid-stride with one leg "
        "forward. The person is the SAME faceless simple figure as the rider in the reference (no facial "
        "features). {look}. No ground, no shadow")

CREATURE = ("ONLY ONE single figure: {who}. Full body side view, facing and walking to the right, mid-stride. "
            "Same flat vector style as the reference but do NOT draw the rider or any person. No ground, no shadow")

TARIFLER = {
    # Sıradan ziyaretçiler (deftere girmez).
    "walker_office": WALK.format(look="A man in a dark grey business suit with a white shirt and a red tie, "
                                 "short dark hair, carrying a brown leather briefcase in one hand"),
    "walker_student": WALK.format(look="A young woman with a long brown ponytail, mustard yellow hoodie, light "
                                  "blue jeans, white sneakers and a small dark green backpack"),
    "walker_coat": WALK.format(look="A woman with short black hair in a long red winter coat, black boots, "
                               "carrying one small white paper shopping bag"),
    "walker_elder": WALK.format(look="An elderly man with grey hair, a brown flat cap, a beige cardigan, brown "
                                "trousers, walking slowly with a wooden walking cane"),
    "walker_teen": WALK.format(look="A teenage boy with big headphones over his ears, green t-shirt, khaki "
                               "shorts, red sneakers, holding a skateboard under one arm"),
    "walker_dress": WALK.format(look="A woman with curly dark hair in a teal knee-length dress, a small tan "
                                "handbag on her shoulder, flat shoes"),
    "walker_courier": WALK.format(look="A delivery courier in an orange cap and orange polo shirt, dark grey "
                                  "trousers, carrying a brown cardboard parcel with both hands in front of him"),
    "walker_tourist": WALK.format(look="A tourist man in a pale green patterned short-sleeve shirt, beige "
                                  "shorts, a straw sun hat and a small black camera hanging on his chest"),
    # AVM'nin nadir ziyaretçileri (misafir defteri).
    "santa": WALK.format(look="Santa Claus: red suit with white fur trim, black belt with gold buckle, black "
                         "boots, long white beard, red hat with a white pom-pom, a big brown sack over his shoulder"),
    "astronaut": WALK.format(look="An astronaut in a bulky white spacesuit with a round helmet and a dark gold "
                             "reflective visor, a white backpack life-support unit, small orange patches on the arms"),
    "mascot": WALK.format(look="A person in a big fluffy brown teddy bear mascot costume with a huge round bear "
                          "head with small round ears, a cream belly patch and a red bow tie"),
    "shopper": WALK.format(look="A woman with a blonde bun and sunglasses, pink jacket, carrying a huge stack of "
                           "many colourful shopping bags and gift boxes in her arms, piled higher than her head"),
    "mime": WALK.format(look="A street mime artist in a black and white horizontal striped shirt, black "
                        "trousers with suspenders, white gloves, a black beret and a plain white painted face"),
    "knight": WALK.format(look="A medieval knight in shining silver plate armour with a closed helmet with a "
                          "small red plume, a red tunic over the armour, a sword in a scabbard at his hip"),
    # AVM'nin gizli efsanesi.
    # İnsan olmayanlar: WALK'taki "referanstaki figür" cümlesi yanına ikinci bir insan çizdiriyordu.
    "bigfoot": CREATURE.format(who="Bigfoot, a huge tall hairy ape-like creature covered in shaggy dark brown "
                               "fur, long arms, big bare feet, walking upright, casually carrying one small white "
                               "shopping bag"),
    # Otel.
    "bellhop": WALK.format(look="A hotel bellhop in a red uniform jacket with gold buttons and a small round red "
                           "pillbox hat, black trousers, carrying a brown suitcase in each hand"),
    "bride": WALK.format(look="A bride in a long white wedding gown with a long white veil, holding a small "
                         "bouquet of pink and white flowers"),
    "rockstar": WALK.format(look="A rock star with long messy black hair and dark sunglasses, black leather "
                            "jacket, ripped black jeans, boots, carrying a black guitar case"),
    "magician": WALK.format(look="A stage magician in a black tailcoat with a long red-lined black cape, a tall "
                            "black top hat, white gloves, holding a short black magic wand with a white tip"),
    "filmstar": WALK.format(look="A glamorous film star woman with wavy platinum blonde hair, big dark "
                            "sunglasses, a long shiny gold evening gown and a white fluffy fur stole"),
    "king": WALK.format(look="A king with a golden crown, a long red velvet royal cape with white ermine trim, "
                        "a white beard, holding a golden sceptre"),
    # Havalimanı.
    "pilot": WALK.format(look="An airline pilot in a navy blue uniform with gold stripes on the sleeves and a "
                         "navy pilot cap, pulling a small black rolling suitcase behind him"),
    "attendant": WALK.format(look="A flight attendant woman in a burgundy red uniform with a matching small hat and "
                             "a neat bun, a red neck scarf, pulling a small rolling suitcase"),
    "backpacker": WALK.format(look="A young backpacker with a huge tall hiking backpack with a rolled sleeping mat "
                              "on top, a bandana, khaki shorts and hiking boots"),
    "surfer": WALK.format(look="A tanned surfer with long sun-bleached hair, colourful floral board shorts, "
                          "barefoot, carrying a long white and orange surfboard horizontally under one arm"),
    "mountaineer": WALK.format(look="A mountaineer in a bright orange down jacket, snow goggles on the forehead, "
                               "a wool hat, a big backpack with a coiled rope and an ice axe"),
    "diver": WALK.format(look="An old-fashioned deep-sea diver in a heavy brass diving helmet with round "
                         "windows, a bulky canvas diving suit and heavy weighted boots"),
    # Müze.
    "painter": WALK.format(look="An art student painter with a black beret, a paint-splattered white smock over dark grey trousers, "
                           "holding a wooden paint palette and a brush"),
    "guide": WALK.format(look="A museum tour guide woman in a navy blazer, holding up a small yellow flag on a "
                         "stick high above her head"),
    "archaeologist": WALK.format(look="An archaeologist with a tan wide-brim explorer hat, khaki shirt and "
                                 "trousers, a leather satchel, holding a small clay pot"),
    "ballerina": WALK.format(look="A ballerina in a pale pink tutu and pink ballet shoes, hair in a tight bun, "
                             "walking lightly on her toes"),
    "centurion": WALK.format(look="A Roman centurion in bronze armour with a red tunic, a bronze helmet with a "
                             "tall red transverse crest, a red cape and sandals, holding a round shield"),
    "mummy": WALK.format(look="An Egyptian mummy wrapped from head to toe in old cream bandages with a few loose "
                         "trailing ends, arms stretched forward"),
    # Gökdelen.
    "window_cleaner": WALK.format(look="A window cleaner in grey overalls and a yellow safety helmet, carrying a "
                                  "bucket in one hand and a long squeegee over his shoulder"),
    "ceo": WALK.format(look="A wealthy business tycoon in a dark navy pinstripe three-piece suit, slicked-back "
                       "grey hair, holding a smartphone to his ear and a slim black briefcase"),
    "firefighter": WALK.format(look="A firefighter in a tan fire-resistant coat with yellow reflective stripes, "
                               "a red helmet, black boots, carrying an axe over the shoulder"),
    "superhero": WALK.format(look="A generic superhero in a purple bodysuit with a long flowing red cape, red "
                             "boots and a small black eye mask, no logo"),
    "robot": CREATURE.format(who="a friendly retro robot made of silver metal boxes, a square head with a small "
                             "antenna, round dial on its chest, bendy tube arms, walking on two legs"),
    "alien": CREATURE.format(who="a small friendly green alien with a big round head, large black eyes, two thin "
                             "antennae, wearing a tiny silver jumpsuit, walking on two legs"),
}

OUT = Path("Assets/Art/Door")
RAW = Path("Tools/art_source/raw")


def make(name, desc=None):
    raw = generate(desc or TARIFLER[name])
    RAW.mkdir(parents=True, exist_ok=True)
    raw.save(RAW / f"door_{name}.png")
    OUT.mkdir(parents=True, exist_ok=True)
    finish(raw).save(OUT / f"{name}.png")
    print("tamam", name, flush=True)


def main():
    arg = sys.argv[1]
    if arg == "--all":
        for name in TARIFLER:
            if (OUT / f"{name}.png").exists():
                continue
            try:
                make(name)
            except Exception as e:  # biri düşerse kalanlar sürsün
                print("HATA", name, e, flush=True)
    elif arg == "--reprocess":
        name = sys.argv[2]
        raw = Image.open(RAW / f"door_{name}.png").convert("RGBA")
        finish(raw).save(OUT / f"{name}.png")
        print("tamam", name)
    else:
        make(arg, sys.argv[2] if len(sys.argv) > 2 else None)


if __name__ == "__main__":
    main()
