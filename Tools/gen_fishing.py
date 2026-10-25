"""Olta ortamı görselleri (balıkçı + iskele, göl balıkları): Gemini ile oyunun düz vektör stilinde.

Stil referansı olarak sürücülü bir araç (vesper.png: yüzsüz sade figür) verilir. Zemin düz MAVİ
(#0000FF): yeşil zemin yeşil/zeytin balıkları (levrek, turna, karabalık) silebilirdi, macenta da
gökkuşağı alabalığının pembe şeridini. Zemin her yerde saydamlaştırılır, kenar halkasındaki mavi
karışımı alfaya çevrilir, kırpılır, en uzun kenar 512 px yapılır, kenarlar 4'ün katına doldurulur.

Çıktı: Assets/Art/Fishing/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/fishing_<ad>.png
Kullanım (proje kökünden): python Tools/gen_fishing.py <ad> ["<tarif>"]   (tarif yoksa TARIFLER'den)
Gereken: GOOGLE_API_KEY.
"""
import time
import base64
import io
import os
import sys
from pathlib import Path

import requests
from PIL import Image

MODEL = "gemini-3.1-flash-image"
REFERENCE = Path("Assets/StreamingAssets/Vehicles/vesper.png")
KEY = (0, 0, 255)

PROMPT = (
    "The attached image is from my game. Draw {desc}, in EXACTLY the same art style as the attached image: "
    "flat vector illustration, no outlines, simple two-tone shading with one slightly darker tone per color, "
    "soft natural colors, rounded friendly shapes, the same level of (low) detail. Whole object visible, "
    "centered. Plain solid pure blue (#0000FF) background, no water, no ground, no shadow, no text, "
    "no other objects, and do not use that pure blue anywhere on the object."
)

FISH = ("a {name} fish, side view, facing right, whole fish from mouth to tail fin, {look}, a small simple "
        "round eye")

TARIFLER = {
    "fisherman": (
        "a fisherman standing at the very end of a short wooden pier, side view, facing right. The pier is a "
        "plank deck on the left half of the picture with two straight wooden posts going straight down from "
        "the deck, the posts are as long as the man is tall. The man is a faceless simple figure like the rider "
        "in the reference: beige bucket hat, olive green jacket, grey trousers, dark boots. He holds a long thin "
        "dark spinning fishing rod with both hands at his waist; the rod points up and forward to the right at "
        "about 40 degrees and is longer than the man is tall; a small spinning reel hangs under the rod near his "
        "hands. No fishing line, no float, no water"
    ),
    # Nehir (2026-10-14): aynı balıkçı, iskelesiz; kıyının çakıl/taşları kodla çizilir.
    "fisherman_river": (
        "a fisherman standing, side view, facing right, feet together at the bottom of the picture. The man is "
        "the SAME faceless simple figure as the rider in the reference: beige bucket hat, olive green jacket, "
        "and dark green chest waders with dark boots. He holds a long thin dark spinning fishing rod with both "
        "hands at his waist; the rod points up and forward to the right at about 40 degrees and is longer than "
        "the man is tall; a small spinning reel hangs under the rod near his hands. No ground, no rocks, no "
        "fishing line, no float, no water"
    ),
    # Kıyı (2026-10-14): mendirekte LRF; mendirek blokları kodla çizilir.
    "fisherman_coast": (
        "a fisherman standing, side view, facing right, feet together at the bottom of the picture. The man is "
        "the SAME faceless simple figure as the rider in the reference: navy blue baseball cap, light grey "
        "windbreaker jacket, a small olive shoulder bag, dark blue jeans and white sneakers. He holds a long "
        "thin dark spinning fishing rod with both hands at his waist; the rod points up and forward to the "
        "right at about 40 degrees and is longer than the man is tall; a small spinning reel hangs under the "
        "rod near his hands. No ground, no rocks, no fishing line, no float, no water"
    ),
    # Açık deniz (2026-10-14): balıkçı + kayık TEK istekte.
    "fisherman_boat": (
        "a fisherman standing in a small wooden rowing boat, side view, facing right. The boat is a simple "
        "short wooden rowboat seen exactly from the side, light brown planks with a darker brown top rim, the "
        "bottom of the hull touches the bottom of the picture, the boat is about three times as long as the "
        "man is wide and its bow points to the right. The man stands in the middle of the boat; he is the SAME "
        "faceless simple figure as the rider in the reference: yellow rain jacket with hood down, dark grey "
        "trousers, dark boots, grey beanie. He holds a long thin dark spinning fishing rod with both hands at "
        "his waist; the rod points up and forward to the right at about 40 degrees and is longer than the man "
        "is tall; a small spinning reel hangs under the rod near his hands. No oars, no fishing line, no float, "
        "no water, no waves"
    ),
    "atlantic_mackerel": FISH.format(name="Atlantic mackerel", look="slim torpedo body, green-blue back with "
                                     "dark wavy tiger stripes, silver-white belly, small finlets near the forked tail"),
    "bonito": FISH.format(name="Atlantic bonito", look="stout torpedo body, steel-blue back with dark slanted "
                          "stripes running down the upper sides, silver belly, deeply forked tail"),
    "little_tunny": FISH.format(name="little tunny (false albacore)", look="tuna-shaped body, dark blue back "
                                "with wavy dark worm-like markings on the rear upper half, silver belly, a few "
                                "dark dots under the pectoral fin, forked tail"),
    "mahi": FISH.format(name="mahi-mahi (dolphinfish)", look="tall blunt forehead, long body tapering to a "
                        "forked tail, bright green-gold back and yellow sides with small blue spots, a long dorsal "
                        "fin running from head to tail"),
    "amberjack": FISH.format(name="greater amberjack", look="long strong body, olive-brown back, silver sides "
                             "with a soft amber-yellow stripe along the middle, a dark band through the eye, forked tail"),
    "bluefin": FISH.format(name="Atlantic bluefin tuna", look="huge thick torpedo body, dark navy blue back, "
                           "silver-white belly with faint pale spots, short yellow finlets near the sickle-shaped tail"),
    # Buz deliği (2026-10-14): tabureli balıkçı tek görsel; buz ve delik kodla.
    "fisherman_ice": (
        "an ice fisherman sitting on a small low folding stool, side view, facing right, the stool legs touch "
        "the bottom of the picture. The man is the SAME faceless simple figure as the rider in the reference, "
        "dressed warmly: red knit hat with a pom-pom, thick dark green padded parka, grey snow trousers, big "
        "dark winter boots resting on the ground in front of the stool. He holds a very short ice fishing rod "
        "(about as long as his forearm) in one hand in front of him; the short rod points forward and slightly "
        "down to the right, a tiny reel on it. No ice, no hole, no snow, no fishing line, no other objects"
    ),
    "yellow_perch": FISH.format(name="yellow perch", look="golden-yellow sides with six or seven dark olive "
                                "vertical bars, olive back, orange lower fins, spiny dorsal fin"),
    "whitefish": FISH.format(name="lake whitefish", look="silver body with a small head and small mouth, "
                             "slightly humped back with an olive-brown tint, forked tail"),
    "arctic_char": FISH.format(name="Arctic char", look="dark olive-green back with pale cream spots, bright "
                               "red-orange belly, lower fins red with white front edges"),
    "burbot": FISH.format(name="burbot", look="long eel-like body with a mottled brown and yellow pattern, a "
                          "single whisker under the chin, long low dorsal and anal fins reaching a rounded tail"),
    "lake_trout": FISH.format(name="lake trout", look="dark grey-green body covered with many pale cream "
                              "spots, deeply forked tail, white edges on the lower fins"),
    "taimen": FISH.format(name="Siberian taimen", look="very long thick body, copper-red tail and lower fins, "
                          "olive-brown back, sides with many small dark x-shaped spots, big wide head"),
    # Altın Sazan (2026-10-14): Göl'ün gizli efsanesi.
    "golden_carp": FISH.format(name="common carp", look="deep heavy body covered with large shining bright "
                               "golden scales, golden-orange fins and tail, two small whiskers at the mouth, "
                               "a few lighter gold highlights on the scales"),
    "horse_mackerel": FISH.format(name="Atlantic horse mackerel", look="slim silver body with a blue-green "
                                  "back, a row of hard bony scutes along the curved side line, forked tail, big eye"),
    "garfish": FISH.format(name="garfish", look="very long thin needle-like body, long beak-like jaws, "
                           "green-blue back and silver sides, small fins set far back"),
    "gilthead": FISH.format(name="gilthead seabream", look="deep oval silver-grey body, a golden band between "
                            "the eyes, a dark patch at the start of the gill cover, forked dark tail"),
    "bluefish": FISH.format(name="bluefish", look="stout streamlined body, blue-green back fading to silver, "
                            "a big mouth with a jutting lower jaw, a dark spot at the base of the pectoral fin"),
    "seabass": FISH.format(name="European seabass", look="streamlined silver body with a grey-blue back, "
                           "two separate dorsal fins, the first one spiny, a dark spot on the gill cover"),
    "leerfish": FISH.format(name="leerfish (garrick)", look="long deep silver body with a dark wavy side line, "
                            "long sickle-shaped dorsal and anal fins, deeply forked dark tail"),
    "grayling": FISH.format(name="European grayling", look="slim silver-grey body with small dark spots near "
                            "the head, a very large tall sail-like dorsal fin with purple and red edges, small mouth"),
    "brown_trout": FISH.format(name="brown trout", look="golden-brown sides with black spots and a few red "
                               "spots ringed in pale blue, darker brown back, creamy yellow belly"),
    "ide": FISH.format(name="ide (orfe)", look="deep silver body with a bronze-grey back, reddish-orange "
                       "lower fins and tail, small head"),
    "asp": FISH.format(name="asp", look="long streamlined silver body with a blue-grey back, a large upturned "
                       "mouth, grey fins with a reddish tint on the lower fins"),
    "barbel": FISH.format(name="common barbel", look="long bronze-olive body, flat pale belly, four short "
                          "whiskers (barbels) under the snout, orange-red lower fins and tail"),
    "huchen": FISH.format(name="huchen (Danube salmon)", look="very long slim torpedo body, copper-red sides "
                          "fading to a grey-green back with small dark x-shaped spots, pale belly"),
    "perch": FISH.format(name="European perch", look="olive green back fading to a pale yellow belly, "
                         "six dark vertical bars on the sides, bright orange-red lower fins and tail, spiny dorsal fin"),
    "chub": FISH.format(name="chub", look="thick rounded body, bronze-silver sides with dark-edged scales, "
                        "grey-brown back, orange-red pelvic and anal fins, dark tail"),
    "trout": FISH.format(name="rainbow trout", look="silver sides with a soft pink-red stripe along the middle, "
                         "olive back, small black dots on the back and tail"),
    "bass": FISH.format(name="largemouth bass", look="olive green body, a dark broken stripe along the side, "
                        "a very big mouth, pale belly"),
    "zander": FISH.format(name="zander (pikeperch)", look="long slim grey-silver body with faint dark bars, "
                          "two dorsal fins, the first one spiny, a pointed head with a pale glassy eye"),
    "pike": FISH.format(name="northern pike", look="long torpedo-shaped body, green sides with rows of pale "
                        "yellow oval spots, flat duck-bill snout, dorsal fin far back near the tail"),
}


def generate(desc):
    ref = base64.b64encode(REFERENCE.read_bytes()).decode()
    body = {
        "contents": [{"parts": [
            {"text": PROMPT.format(desc=desc)},
            {"inline_data": {"mime_type": "image/png", "data": ref}},
        ]}],
        "generationConfig": {"responseModalities": ["IMAGE"]},
    }
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent"
    # Paralel üretimde kota (429) ve geçici sunucu hataları: artan beklemeyle yeniden dene. Hata metninde
    # URL (anahtar) yazılmasın diye yalnız durum kodu.
    for attempt in range(6):
        r = requests.post(url, params={"key": os.environ["GOOGLE_API_KEY"]}, json=body, timeout=240)
        if r.status_code != 429 and r.status_code < 500:
            break
        time.sleep(15 * (attempt + 1))
    if not r.ok:
        raise RuntimeError(f"HTTP {r.status_code}")
    for cand in r.json().get("candidates", []):
        for part in cand.get("content", {}).get("parts", []):
            data = part.get("inlineData") or part.get("inline_data")
            if data:
                return Image.open(io.BytesIO(base64.b64decode(data["data"]))).convert("RGBA")
    raise RuntimeError("Yanıtta görsel yok: " + r.text[:400])


def dist(c):
    return ((c[0] - KEY[0]) ** 2 + (c[1] - KEY[1]) ** 2 + (c[2] - KEY[2]) ** 2) ** 0.5


def remove_key(img, tolerance=120, soft=230):
    w, h = img.size
    px = img.load()
    for y in range(h):
        for x in range(w):
            if dist(px[x, y]) <= tolerance:
                px[x, y] = (0, 0, 0, 0)
    for _ in range(3):
        ring = []
        for y in range(h):
            for x in range(w):
                if px[x, y][3] == 0:
                    continue
                if any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] == 0
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    ring.append((x, y))
        for x, y in ring:
            r, g, b, a = px[x, y]
            if b <= max(r, g):
                continue  # mavi karışımı yok: nesnenin kendi kenarı
            alpha = int(min(1.0, dist((r, g, b)) / soft) * a)
            b = max(r, g)  # mavi taşmasını kırp
            px[x, y] = (r, g, b, alpha) if alpha > 8 else (0, 0, 0, 0)
    return img


def defringe(img, radius=2):
    w, h = img.size
    src = img.load()
    out = img.copy()
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = src[x, y]
            if a == 0 or a >= 230:
                continue
            acc = [0, 0, 0, 0]
            for ny in range(max(0, y - radius), min(h, y + radius + 1)):
                for nx in range(max(0, x - radius), min(w, x + radius + 1)):
                    c = src[nx, ny]
                    if c[3] >= 230:
                        acc[0] += c[0]; acc[1] += c[1]; acc[2] += c[2]; acc[3] += 1
            if acc[3]:
                op[x, y] = (acc[0] // acc[3], acc[1] // acc[3], acc[2] // acc[3], a)
            elif a < 60:
                op[x, y] = (0, 0, 0, 0)
    return out


def finish(raw):
    img = remove_key(raw.copy())
    img = img.crop(img.getbbox())
    scale = 512 / max(img.width, img.height)
    img = img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
    img = defringe(img)
    W, H = (img.width + 3) // 4 * 4, (img.height + 3) // 4 * 4
    padded = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    padded.paste(img, ((W - img.width) // 2, H - img.height))
    return padded


def main():
    name = sys.argv[1]
    if name == "--reprocess":
        name = sys.argv[2]
        raw = Image.open(Path("Tools/art_source/raw") / f"fishing_{name}.png").convert("RGBA")
    else:
        desc = sys.argv[2] if len(sys.argv) > 2 else TARIFLER[name]
        raw = generate(desc)
        raw_dir = Path("Tools/art_source/raw")
        raw_dir.mkdir(parents=True, exist_ok=True)
        raw.save(raw_dir / f"fishing_{name}.png")
    out_dir = Path("Assets/Art/Fishing")
    out_dir.mkdir(parents=True, exist_ok=True)
    finish(raw).save(out_dir / f"{name}.png")
    print("tamam", name)


if __name__ == "__main__":
    main()
