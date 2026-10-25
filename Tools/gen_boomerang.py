"""Bumerang ortamı görselleri (atıcı, hedefler, seçici kartı): Gemini ile oyunun düz vektör stilinde.

Üretim ve temizleme gen_fishing.py ile aynı (vesper.png stil referansı, düz mavi zemin saydamlaştırılır,
kırpılır, en uzun kenar 512 px, alt kenara yaslanır). Hedefler tam yandan, zeminsiz; bumerang, zemin ve uzak
kaya oyunda kodla çizilir. Atıcı WALK kalıbındaki gibi referanstaki yüzsüz figür (eli boş: bumerang kodla).
Mavi anahtar renk yüzünden tariflerde mavi yok.

Çıktı: Assets/Art/Boomerang/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/boomerang_<ad>.png
Kullanım (proje kökünden): python Tools/gen_boomerang.py <ad> ["<tarif>"]   |   python Tools/gen_boomerang.py --all
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

from gen_fishing import finish, generate

TARGET = ("{what}, seen exactly from the side (straight side view, no perspective), standing upright on nothing. "
          "{look}. Simple shapes. No ground, no grass, no people, no animals, no shadow, no text, no letters, no sky")

ONLY = ("Same flat vector style as the reference but do NOT draw the rider, the scooter or any person - only "
        "this object")

TARIFLER = {
    # Sıradan hedefler (deftere girmez).
    "straw_target": TARGET.format(what="a round straw archery target on a wooden tripod stand", look="A golden "
                                  "straw bale disc facing the viewer with painted red, white and yellow rings"),
    "tin_cans": TARGET.format(what="a small pyramid of six tin cans stacked on a wooden crate", look="Silver tin "
                              "cans with red labels without any text, a light wooden crate"),
    "bottle_post": TARGET.format(what="a green glass bottle standing on top of a wooden fence post", look="A "
                                 "weathered grey-brown post, a dark green bottle with a cork"),
    "pumpkin_stump": TARGET.format(what="an orange pumpkin sitting on a tree stump", look="A round orange "
                                   "pumpkin with a short green stem on a brown cut tree stump showing rings"),
    "clay_pot_post": TARGET.format(what="a terracotta clay pot standing on a short wooden post", look="A round "
                                   "orange-brown clay pot with a narrow neck on a thick wooden post"),
    "wooden_sign": TARGET.format(what="a wooden plank board target on two short legs", look="Light wooden planks "
                                 "with three painted red and white rings, no writing"),
    # Avustralya çölünün nadir hedefleri (hedef defteri).
    "brass_bell": TARGET.format(what="a big brass bell hanging from a wooden A-frame", look="A shiny golden brass "
                                "bell with a dark clapper, a sturdy dark wooden frame"),
    "red_kite": TARGET.format(what="a red diamond-shaped kite caught at the top of a tall thin wooden pole",
                              look="A bright red kite with a long wavy tail of small yellow bows hanging down the pole"),
    "weather_vane": TARGET.format(what="an old iron rooster weather vane on a tall thin pole", look="A black "
                                  "iron rooster silhouette on an arrow, four short direction arms below it "
                                  "without letters, a dark grey pole"),
    "gong": TARGET.format(what="a large round bronze gong hanging in a wooden frame", look="A golden bronze "
                          "gong disc with a raised centre, a dark red wooden frame with curled top ends"),
    "paper_lantern": TARGET.format(what="a big round red paper lantern hanging from a curved wooden post",
                                   look="A glowing red paper lantern with golden top and bottom caps and a "
                                   "golden tassel"),
    "golden_trophy": TARGET.format(what="a shiny golden trophy cup on a tall wooden pedestal", look="A big "
                                   "golden cup with two curved handles on a dark wooden pedestal"),
    # ---- Ortam Standardı v2 (2026-10-25): Kumsal, Çayır, Kar Ovası, Eski Harabeler
    "sandcastle": TARGET.format(what="a sandcastle with two towers", look="Golden sand towers with small crenellations and a tiny red flag on top"),
    "coconut_post": TARGET.format(what="a brown coconut sitting on top of a wooden post", look="A hairy brown coconut on a pale weathered post"),
    "beach_ball": TARGET.format(what="a striped beach ball resting on a short wooden post", look="A beach ball with red, yellow and white segments"),
    "sand_bucket": TARGET.format(what="a plastic sand bucket with a spade standing in it", look="A bright red bucket with a yellow handle and a yellow spade"),
    "driftwood": TARGET.format(what="a target made of driftwood planks on two legs", look="Pale grey-white driftwood planks with painted red rings"),
    "buoy_post": TARGET.format(what="a round red and white buoy hanging on a wooden post", look="A striped red and white round buoy, a short rope"),
    "ship_bell": TARGET.format(what="a shiny brass ship's bell hanging from a wooden post with a rope", look="A polished golden bell on a dark wooden post with a short rope pull"),
    "message_bottle": TARGET.format(what="a glass bottle with a rolled paper message inside, standing on a wooden post", look="A pale green bottle with a cork and a rolled cream scroll inside"),
    "giant_conch": TARGET.format(what="a giant pink conch shell resting on a wooden stand", look="A big spiral shell, cream outside and glossy pink inside"),
    "surf_trophy": TARGET.format(what="a surfboard standing upright with a small trophy cup on a stand beside it", look="A turquoise and white surfboard with a red stripe, a silver cup"),
    "pirate_flag": TARGET.format(what="a black pirate flag on a tall thin wooden pole", look="A black flag with two white crossed bones, no skull, waving to the right"),
    "golden_pearl": TARGET.format(what="an open oyster shell holding a big golden pearl on a wooden pedestal", look="A grey-blue oyster shell open wide, a glowing golden pearl inside"),
    "hay_bale": TARGET.format(what="a round hay bale with a painted target on its face", look="A golden straw round bale with red and white rings painted on the side"),
    "scarecrow": TARGET.format(what="a friendly scarecrow on a pole", look="A straw-stuffed figure with a sack head without a face, a straw hat, a checked shirt and arms spread out"),
    "milk_churn": TARGET.format(what="an old metal milk churn", look="A silver metal milk churn with a lid and two side handles"),
    "watering_can": TARGET.format(what="a metal watering can on a wooden crate", look="A pale green watering can with a long spout on a light wooden crate"),
    "horseshoe_post": TARGET.format(what="a horseshoe hanging on a short wooden post", look="A dark iron horseshoe hanging from a nail on a weathered post"),
    "birdhouse": TARGET.format(what="a wooden birdhouse on a tall thin pole", look="A small red birdhouse with a white roof and a round hole"),
    "rooster_vane": TARGET.format(what="a copper rooster weather vane on a tall thin pole", look="A shiny copper rooster on an arrow, four short direction arms below it without letters"),
    "cow_bell": TARGET.format(what="a big brass cow bell hanging from a wooden frame", look="A golden brass bell with a brown leather strap"),
    "blue_ribbon": TARGET.format(what="a big blue prize rosette ribbon pinned to a wooden board on a post", look="A round blue rosette with two hanging tails, no writing"),
    "windpump": TARGET.format(what="a small farm windpump on a lattice tower", look="A grey steel lattice tower with a round wheel of many metal blades and a tail fin"),
    "barn_lantern": TARGET.format(what="an old oil barn lantern hanging from a curved wooden post", look="A red metal lantern with a glowing warm flame inside its glass"),
    "golden_horseshoe": TARGET.format(what="a shiny golden horseshoe on a wooden plaque on a post", look="A gleaming gold horseshoe on a dark wooden plaque"),
    "snowman": TARGET.format(what="a snowman", look="Three white snowballs, a carrot nose, a red scarf, two twig arms, a black hat, no face details other than the nose"),
    "ice_block": TARGET.format(what="a clear blue-white ice block standing on end", look="A translucent pale cyan ice block with white frosty edges"),
    "sled": TARGET.format(what="an old wooden sled standing upright against a short post", look="Light wooden slats on curved red metal runners"),
    "ski_post": TARGET.format(what="a pair of crossed skis tied to a wooden post", look="Two red skis with white tips crossed in an X on a dark post"),
    "pine_cone": TARGET.format(what="a giant pine cone on a tree stump", look="A big brown pine cone with layered scales on a cut stump with a little snow"),
    "snowball_stack": TARGET.format(what="a pyramid of snowballs on a wooden board", look="Round white snowballs stacked in a small pyramid on a light plank"),
    "sleigh_bell": TARGET.format(what="a string of golden sleigh bells on a red leather strap hanging from a wooden post", look="Round golden jingle bells on a red strap"),
    "ice_crystal": TARGET.format(what="a big ice crystal snowflake sculpture on a pedestal", look="A six-pointed pale cyan ice snowflake standing on a white ice pedestal"),
    "aurora_kite": TARGET.format(what="a long kite with a rainbow tail caught at the top of a tall thin pole", look="A violet and green kite with a long flowing teal, green and violet tail"),
    "igloo_lantern": TARGET.format(what="a small snow igloo glowing warmly from inside", look="White snow blocks in a dome with a round doorway glowing orange"),
    "ice_trophy": TARGET.format(what="a clear ice trophy cup on a wooden pedestal", look="A frosty pale cyan cup with two handles on a dark wooden base"),
    "snow_globe": TARGET.format(what="a big snow globe on a wooden base", look="A clear glass dome with a tiny pine tree and falling snowflakes inside, a dark red base"),
    "stone_urn": TARGET.format(what="an ancient carved stone urn", look="A grey-green weathered stone urn with carved bands and moss"),
    "clay_mask": TARGET.format(what="a round clay mask hanging on a wooden post", look="A terracotta mask with simple carved eye holes and a zigzag band"),
    "totem_post": TARGET.format(what="a carved wooden totem post", look="Stacked carved wooden shapes with geometric patterns in faded red and teal, no faces"),
    "carved_tablet": TARGET.format(what="a carved stone tablet standing upright", look="A grey stone slab with carved spirals and step patterns, no letters"),
    "torch_post": TARGET.format(what="a burning torch on a tall wooden post", look="A wrapped torch with a bright orange flame on a dark post bound with vines"),
    "monkey_statue": TARGET.format(what="a small stone monkey statue on a block", look="A sitting grey stone monkey with moss on a square stone base"),
    "jade_idol": TARGET.format(what="a small green jade idol figure on a stone pedestal", look="A polished green jade figurine with simple carved patterns, no face details"),
    "sun_disc": TARGET.format(what="a big golden sun disc in a stone frame", look="A round golden disc with sun rays carved around it, set in a grey stone arch"),
    "temple_bell": TARGET.format(what="a bronze temple bell hanging under a small stone arch", look="A dark bronze bell with green patina under a carved stone arch"),
    "feather_crown": TARGET.format(what="a feather crown on a wooden stand", look="A golden headband with tall red, green and blue feathers fanning upward"),
    "ruby_eye": TARGET.format(what="a stone pillar with a huge red ruby set in it", look="A grey carved stone pillar holding a glowing faceted red ruby"),
    "golden_mask": TARGET.format(what="a golden ceremonial mask on a wooden pedestal", look="A shining gold mask with simple geometric patterns and turquoise inlays, empty eye holes"),
    # Atıcı (tek karakter) ve Activities kartı.
    "thrower": ("a person standing, full body side view, facing right, in a throwing pose: leaning slightly back, "
                "the right arm raised up and back behind the head with the hand open and EMPTY (holding nothing), "
                "the left arm stretched forward pointing ahead. The person is the SAME faceless simple figure as the "
                "rider in the reference (no facial features). Wide-brimmed tan outback hat, khaki shirt with rolled "
                "sleeves, brown shorts, sturdy brown boots. No ground, no shadow, no boomerang"),
    "boomerang_card": ("a carved wooden boomerang with a painted orange stripe flying toward a round straw archery "
                       "target on a tripod, a few short speed lines behind the boomerang. " + ONLY + ". No ground, "
                       "no sky, no text"),
}

OUT = Path("Assets/Art/Boomerang")
RAW = Path("Tools/art_source/raw")
PREFIX = "boomerang"


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
