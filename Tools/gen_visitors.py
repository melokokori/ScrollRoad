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
