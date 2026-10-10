"""Yeni araç modeli üretir: car_coral.png'yi stil referansı olarak Gemini'ye verir, düz YEŞİL
(#00FF00) zeminde yandan (sağa bakan) aracı çizdirir, zemini kenarlardan flood-fill ile
saydamlaştırır, kenardaki yeşil karışımını saydamlığa çevirip yeşil taşmasını temizler, kırpar ve
512 px genişliğe getirir. (Beyaz zemin beyaz gemi üst yapılarını da siliyordu; rembg düz vektörde
camları siliyordu.)

Gövde mercan kalmalı: oyun renk varyantlarını ve silüetleri mercan tonundan çalışma zamanında
türetir (Assets/Scripts/Garage/VehicleArt.cs). Çıktı doğrudan oyunun taban görselidir:
Assets/StreamingAssets/Vehicles/<model>.png (kurucudaki Def(...) id'siyle aynı ad).

Kullanım (proje kökünden): python Tools/gen_vehicle.py <model> "<araç tarifi>" [çıktı klasörü]
  ör. python Tools/gen_vehicle.py pickup "a small pickup truck with an open cargo bed"
  Tarif "free:" ile başlarsa teker/gövde rengi kuralı uygulanmaz (gemi, uçak, balon, sürücülü motor).
Gereken: GOOGLE_API_KEY ortam değişkeni.
"""
import base64
import io
import os
import sys
from collections import deque
from pathlib import Path

import requests
from PIL import Image, ImageFilter

MODEL = "gemini-3.1-flash-image"
REFERENCE = Path("Assets/StreamingAssets/Vehicles/car.png")

PROMPT = (
    "The attached image is a car from my game. Draw {desc} in EXACTLY the same art style: flat vector "
    "illustration, no outlines, simple two-tone shading (lighter coral body, slightly darker coral lower "
    "band), the same coral body color, the same light blue windows, the same dark grey tires with light grey "
    "hubcaps and the same wheel size, a small yellow headlight. Side view, facing right, whole vehicle visible, "
    "centered. Plain solid pure green (#00FF00) background, no ground, no shadow, no text, no other objects."
)


# Tekerleksiz/serbest araçlar (gemi, uçak, balon, sürücülü motor) için: yalnız stil bağlanır.
FREE_PROMPT = (
    "The attached image is a car from my game. Draw {desc}, in EXACTLY the same art style as the attached "
    "car: flat vector illustration, no outlines, simple two-tone shading with one slightly darker tone per "
    "color, the same soft saturated palette, rounded friendly shapes, the same level of (low) detail. "
    "Side view, facing right, whole object visible, centered. Plain solid pure green (#00FF00) background, "
    "no ground, no water, no shadow, no text, no other objects, and do not use that green anywhere on the object."
)


def generate(desc):
    ref = base64.b64encode(REFERENCE.read_bytes()).decode()
    body = {
        "contents": [{"parts": [
            {"text": (FREE_PROMPT.format(desc=desc[5:]) if desc.startswith("free:") else PROMPT.format(desc=desc))},
            {"inline_data": {"mime_type": "image/png", "data": ref}},
        ]}],
        "generationConfig": {"responseModalities": ["IMAGE"]},
    }
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent"
    r = requests.post(url, params={"key": os.environ["GOOGLE_API_KEY"]}, json=body, timeout=180)
    r.raise_for_status()
    for cand in r.json().get("candidates", []):
        for part in cand.get("content", {}).get("parts", []):
            data = part.get("inlineData") or part.get("inline_data")
            if data:
                return Image.open(io.BytesIO(base64.b64decode(data["data"]))).convert("RGBA")
    raise RuntimeError("Yanıtta görsel yok: " + r.text[:400])


def remove_background(img, tolerance=28):  # eski beyaz zemin yolu (yeşil zemine geçmeden önceki araçlar)
    """Kenarlardan başlayıp zemine benzeyen (beyaza yakın) bağlı pikselleri saydamlaştırır.
    Saydam piksellerin rengi siyah: beyaz kalırsa Unity sıkıştırması koyu zeminde kırıntı gösteriyor."""
    w, h = img.size
    px = img.load()
    seen = bytearray(w * h)
    q = deque()
    for x in range(w):
        q.extend(((x, 0), (x, h - 1)))
    for y in range(h):
        q.extend(((0, y), (w - 1, y)))

    def bg(c):
        return c[0] > 255 - tolerance and c[1] > 255 - tolerance and c[2] > 255 - tolerance

    while q:
        x, y = q.popleft()
        i = y * w + x
        if seen[i]:
            continue
        seen[i] = 1
        if not bg(px[x, y]):
            continue
        px[x, y] = (0, 0, 0, 0)
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx]:
                q.append((nx, ny))

    # İkinci geçiş: saydam alana bağlı açık NÖTR gri pikseller de zemindir (Gemini zeminde soluk
    # gri çizgi/gürültü bırakabiliyor). Jant grileri koyu teker içinde kaldığı için ulaşılmaz.
    q = deque((x, y) for y in range(h) for x in range(w) if px[x, y][3] == 0)
    seen2 = set(q)
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen2:
                seen2.add((nx, ny))
                r, g, b, a = px[nx, ny]
                if a > 0 and min(r, g, b) > 150 and max(r, g, b) - min(r, g, b) < 22:
                    px[nx, ny] = (0, 0, 0, 0)
                    q.append((nx, ny))

    # Üçüncü geçiş: gövdeden (alfa > 200) 2 px'ten uzak yarı saydam kırıntılar (soluk gri noktalar).
    solid = img.getchannel("A").point(lambda v: 255 if v > 200 else 0).filter(ImageFilter.MaxFilter(5))
    sp = solid.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3] and not sp[x, y]:
                px[x, y] = (0, 0, 0, 0)

    # Kenar halesi: saydam komşusu olan açık renkli pikselleri yumuşat.
    out = img.copy()
    op = out.load()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if any(px[x + dx, y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                light = min(r, g, b)
                if light > 200:
                    op[x, y] = (r, g, b, int(a * (255 - light) / 55))
    return out


KEY = (0, 255, 0)


def key_distance(c):
    return ((c[0] - KEY[0]) ** 2 + (c[1] - KEY[1]) ** 2 + (c[2] - KEY[2]) ** 2) ** 0.5


def remove_green(img, tolerance=110, soft=220):
    """Yeşil zemini HER YERDE (teker içi, sürücü ile gövde arası gibi kapalı boşluklar dahil)
    saydamlaştırır; nesnede yeşil kullanılmaması istendiği için güvenli. Saydam alana değen kenar
    halkasında (3 px) alfa yeşile uzaklıktan hesaplanır ve yeşil taşması (G kanalı) kırpılır."""
    w, h = img.size
    px = img.load()
    for y in range(h):
        for x in range(w):
            if key_distance(px[x, y]) <= tolerance:
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
            if g <= max(r, b):
                continue  # yeşil karışımı yok: nesnenin kendi kenarı
            alpha = int(min(1.0, key_distance((r, g, b)) / soft) * a)
            g = max(r, b)  # yeşil taşmasını kırp
            px[x, y] = (r, g, b, alpha) if alpha > 8 else (0, 0, 0, 0)
    return img


def defringe(img, radius=2):
    """Kenardaki yarı saydam piksellerin rengini yakındaki opak piksellerin ortalamasıyla değiştirir:
    beyaz zeminle karışmış açık saçak koyu zeminde kesik açık çizgi gibi görünüyordu. Yakınında
    opak piksel olmayan soluk kırıntılar silinir."""
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


def despill(img):
    """Son güvenlik: yeşile kaçan pikselleri (yeşil zeminden kalan kenar noktaları) nötrler.
    Yalnız mercan tabanlara ve sabit renkli araçlara uygulanır (yeşil boya varyantları sonradan üretilir)."""
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a and g > max(r, b) + 15:
                px[x, y] = (r, max(r, b), b, a)
    return img


def main():
    vid, desc = sys.argv[1], sys.argv[2]
    out_dir = Path(sys.argv[3]) if len(sys.argv) > 3 else Path("Assets/StreamingAssets/Vehicles")
    out_dir.mkdir(parents=True, exist_ok=True)
    raw = generate(desc)
    raw_dir = Path("Tools/art_source/raw")  # Assets dışında: oyuna girmez; yeniden işlemek için
    raw_dir.mkdir(parents=True, exist_ok=True)
    raw.save(raw_dir / f"{vid}.png")
    img = remove_green(raw.copy())
    bbox = img.getbbox()
    img = img.crop(bbox)
    img = img.resize((512, round(512 * img.height / img.width)), Image.LANCZOS)
    img = despill(defringe(img))
    # Kenarlar 4'ün katı olmalı (dolgu üstte): yoksa Unity BC7 sıkıştıramıyor, doku 4 kat büyük kalıyor.
    W, H = (img.width + 3) // 4 * 4, (img.height + 3) // 4 * 4
    padded = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    padded.paste(img, ((W - img.width) // 2, H - img.height))
    img = padded
    path = out_dir / f"{vid}.png"
    img.save(path)
    print(path, img.size)


if __name__ == "__main__":
    main()
