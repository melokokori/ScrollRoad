"""Plak tablası ortamı görselleri (nadir plak kapakları + seçici kartı): Gemini ile oyunun düz vektör stilinde.

Kapaklar kare ve tamamen opak (kenardan kenara albüm kapağı, YAZISIZ): anahtar renk silinmez, ortadan kare
kırpılıp 512 px yapılır. Activities kartı için pikap resmi gen_fishing.py'nin mavi anahtarlı temizliğinden geçer.
Pikap ve plak oyunda kodla çizilir. Gerçek albüm/sanatçı yok; adlar kurgusal.

Çıktı: Assets/Art/Vinyl/<ad>.png (ArtImportSettings: Sprite). Ham: Tools/art_source/raw/vinyl_<ad>.png
Kullanım (proje kökünden): python Tools/gen_vinyl.py <ad> ["<tarif>"]   |   python Tools/gen_vinyl.py --all
Gereken: GOOGLE_API_KEY.
"""
import sys
from pathlib import Path

from PIL import Image

import base64
import io
import os

import requests

from gen_fishing import MODEL, REFERENCE, finish, generate

# Kapaklar için ayrı istem: gen_fishing.PROMPT "düz mavi zemin" ister, kapakta bu mavi çerçeve olarak kalıyordu.
COVER_PROMPT = (
    "The attached image is from my game. Draw {desc}, in EXACTLY the same art style as the attached image: flat "
    "vector illustration, no outlines, simple two-tone shading, soft natural colors, rounded friendly shapes, the "
    "same level of (low) detail. The artwork must fill the entire square canvas with no frame, no border, no margin "
    "and no plain background colour around it. Absolutely no text, letters, numbers or logos anywhere."
)


def generate_cover(desc):
    ref = base64.b64encode(REFERENCE.read_bytes()).decode()
    body = {
        "contents": [{"parts": [
            {"text": COVER_PROMPT.format(desc=desc)},
            {"inline_data": {"mime_type": "image/png", "data": ref}},
        ]}],
        "generationConfig": {"responseModalities": ["IMAGE"], "imageConfig": {"aspectRatio": "1:1"}},
    }
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent"
    r = requests.post(url, params={"key": os.environ["GOOGLE_API_KEY"]}, json=body, timeout=240)
    r.raise_for_status()
    for cand in r.json().get("candidates", []):
        for part in cand.get("content", {}).get("parts", []):
            data = part.get("inlineData") or part.get("inline_data")
            if data:
                from PIL import Image as _I
                return _I.open(io.BytesIO(base64.b64decode(data["data"]))).convert("RGBA")
    raise RuntimeError("Yanıtta görsel yok: " + r.text[:400])

COVER = "a square vinyl record album cover artwork, {art}"

TARIFLER = {
    "cover_rainy": COVER.format(art="lo-fi mood: a cosy window at night with rain drops on the glass, a warm desk "
                                "lamp, a steaming mug and a sleeping orange cat on the sill, muted teal and warm "
                                "orange colours"),
    "cover_praia": COVER.format(art="bossa nova mood: a sunny beach promenade with wavy black and white mosaic "
                                "pavement, two palm trees, a calm sea and a small sailboat, warm sand and turquoise "
                                "colours"),
    "cover_nightdrive": COVER.format(art="synthwave mood: a sports car seen from behind driving on an empty road "
                                     "towards a big striped setting sun over dark purple mountains, magenta and "
                                     "orange gradient sky"),
    "cover_bluesmoke": COVER.format(art="jazz mood: a saxophone player silhouette in a smoky dim club, a single "
                                    "spotlight, deep navy and gold colours, curling smoke"),
    "cover_mirrorball": COVER.format(art="disco mood: a big shining mirror ball hanging over a lit-up dance floor "
                                     "with coloured squares, rays of light, pink, purple and gold colours"),
    "cover_goldengroove": COVER.format(art="funk mood: a golden vinyl record with flames and a pair of platform "
                                       "shoes dancing, rich gold, brown and orange colours, a burst of rays"),
    # Activities seçicisindeki kart görseli (oyunda pikap kodla çizilir).
    "turntable_card": ("a retro record player turntable with a wooden base, a black vinyl record with a red label "
                       "on the platter and a silver tone arm, seen from above at a slight angle, a small stack of "
                       "two record sleeves leaning next to it. No text, no table, no background objects"),
}

OUT = Path("Assets/Art/Vinyl")
RAW = Path("Tools/art_source/raw")


def finish_cover(raw):
    side = min(raw.width, raw.height)
    left, top = (raw.width - side) // 2, (raw.height - side) // 2
    img = raw.crop((left, top, left + side, top + side)).convert("RGB")
    return img.resize((512, 512), Image.LANCZOS).convert("RGBA")


def make(name, desc=None):
    raw = (generate_cover if name.startswith("cover_") else generate)(desc or TARIFLER[name])
    RAW.mkdir(parents=True, exist_ok=True)
    raw.save(RAW / f"vinyl_{name}.png")
    OUT.mkdir(parents=True, exist_ok=True)
    (finish_cover(raw) if name.startswith("cover_") else finish(raw)).save(OUT / f"{name}.png")
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
        raw = Image.open(RAW / f"vinyl_{name}.png").convert("RGBA")
        (finish_cover(raw) if name.startswith("cover_") else finish(raw)).save(OUT / f"{name}.png")
        print("tamam", name)
    else:
        make(arg, sys.argv[2] if len(sys.argv) > 2 else None)


if __name__ == "__main__":
    main()
