"""Hız göstergesi (ibre) skin görsellerini üretir — Artifact maketiyle aynı geometri.

Çıktı: Assets/Resources/Gauges/ (GaugeSkins çalışma zamanında Resources.Load ile yükler)
  <skin>_face.png         512x512  kart kadranı: çerçeve, çentikler, rakamlar, birim, sayaç hücreleri
  <skin>_needle.png       512x512  ibre + göbek (yukarı bakar, merkez pivot)
  <skin>_lit.png          512x512  yalnız Dijital: yanan segmentler (radial fill ile doldurulur)
  <skin>_mini_face.png    128x128  görev çubuğu için sade kadran (rakamsız, kalın çentik)
  <skin>_mini_needle.png  128x128
  <skin>_mini_lit.png     128x128  yalnız Dijital
  <skin>_plate.png        512x128  görev çubuğu plakası (206x42 tasarım, dikeyde ortalı)

Koordinatlar 200 birimlik kadran uzayında (maketteki SVG ile aynı). 4x süper örnekleme ile çizilir.
Kullanım: python Tools/make_gauges.py
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Gauges")
FONTS = os.path.join(ROOT, "Assets", "Fonts")
FONT_BOLD = os.path.join(FONTS, "Overpass-ExtraBold.ttf")
FONT_MONO = os.path.join(FONTS, "OverpassMono-SemiBold.ttf")
FONT_DECO = os.path.join(ROOT, "Tools", "fonts", "PoiretOne-Regular.ttf")

MIN, MAX, SWEEP, START = -60.0, 220.0, 270.0, 225.0
KN = 1 / 1.852
SS = 4  # süper örnekleme

SKINS = {
    "classic": dict(face="#F4F1E6", bezel="#1A1C22", tick="#1A1C22", num="#1A1C22", needle="#E8644A", hub="#1A1C22",
                    unit="#6B6A63", rev="#B9B5A8", odoBg="#1A1C22", odoCell="#2B2E36", odoLastBg="#F4F1E6"),
    "tabela": dict(face="#0E6B3E", bezel="#0A4F2D", ring="#FFFFFF", tick="#FFFFFF", num="#FFFFFF", needle="#F2B33D",
                   hub="#0A3A22", unit=(255, 255, 255, 184), rev=(255, 255, 255, 90), odoBg="#0A3A22", odoCell="#124A2E",
                   odoLastBg="#F2B33D"),
    "sport": dict(face="#14161B", bezel="#2C2F37", tick="#E9E6DC", num="#E9E6DC", needle="#FF4B3E", hub="#0B0C0F",
                  unit="#8D8A82", rev="#4A4C52", red="#E23B3B", italic=True, odoBg="#0B0C0F", odoCell="#1E2026",
                  odoLastBg="#FF4B3E"),
    "retro": dict(face="#EFE4C8", face2="#D8C79E", bezel="chrome", tick="#3A2E22", num="#3A2E22", needle="#1B1B1B",
                  tip="#C8323C", hub="#3A2E22", unit="#7A6A52", rev="#B8A985", deco=True, odoBg="#3A2E22",
                  odoCell="#4A3C2E", odoLastBg="#C8323C"),
    "lcd": dict(lcd=True, face="#0E1512", bezel="#1F2823", lit="#A6F06B", dim=(166, 240, 107, 33),
                unit=(166, 240, 107, 153), odoBg="#08100C", odoCell="#101C15", odoLastBg="#A6F06B"),
    "harbor": dict(face="#14304F", bezel="#0D2238", tick="#E8F1F7", num="#E8F1F7", needle="#F2B33D", hub="#0D2238",
                   unit=(232, 241, 247, 178), rev=(232, 241, 247, 77), knots=True, odoBg="#0D2238", odoCell="#1A3B5E",
                   odoLastBg="#F2B33D"),
    "airspeed": dict(face="#101113", bezel="#2A2C30", tick="#F2F2F0", num="#F2F2F0", needle="#F2F2F0", hub="#2A2C30",
                     unit="#9A9A96", rev="#3A3C40", knots=True, arcs=[(30, 90, "#3DBE5C"), (90, 110, "#E8C23A")],
                     redline=115, odoBg="#000000", odoCell="#1C1D20", odoLastBg="#F2F2F0"),
}


def rgba(c):
    if isinstance(c, tuple):
        return c if len(c) == 4 else c + (255,)
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), 255)


def angle_for(kmh):
    kmh = max(MIN, min(MAX, kmh))
    return START - (kmh - MIN) / (MAX - MIN) * SWEEP


class Canvas:
    """200 birimlik uzayda çizen, size piksele inen yardımcı."""

    def __init__(self, size, w=200.0, h=200.0):
        self.size = size
        self.k = size * SS / w  # birim -> süper örnekli piksel
        self.img = Image.new("RGBA", (int(round(w * self.k)), int(round(h * self.k))), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img, "RGBA")  # yarı saydam renkler karışsın, delmesin
        self.w, self.h = w, h

    def P(self, x, y):
        return (x * self.k, y * self.k)

    def circle(self, cx, cy, r, fill):
        self.d.ellipse([(cx - r) * self.k, (cy - r) * self.k, (cx + r) * self.k, (cy + r) * self.k], fill=rgba(fill))

    def ring(self, cx, cy, r, width, color):
        self.d.ellipse([(cx - r) * self.k, (cy - r) * self.k, (cx + r) * self.k, (cy + r) * self.k],
                       outline=rgba(color), width=max(1, int(round(width * self.k))))

    def arc(self, cx, cy, r, from_kmh, to_kmh, width, color, dash=None):
        # PIL açıları: 0 = sağ, saat yönünde. Matematik açıları saat yönünün tersi.
        a0, a1 = angle_for(from_kmh), angle_for(to_kmh)
        steps = max(8, int(abs(a0 - a1) * 2))
        pts = []
        for i in range(steps + 1):
            a = math.radians(a0 + (a1 - a0) * i / steps)
            pts.append((cx + r * math.cos(a), cy - r * math.sin(a)))
        if dash:
            on, off = dash
            # yay boyunca kesik: uzunluk birimiyle
            seg, acc, draw_on, cur = [], 0.0, True, [pts[0]]
            for p, q in zip(pts, pts[1:]):
                L = math.dist(p, q)
                acc += L
                cur.append(q)
                if acc >= (on if draw_on else off):
                    if draw_on:
                        self.polyline(cur, width, color)
                    draw_on = not draw_on
                    acc = 0.0
                    cur = [q]
            if draw_on and len(cur) > 1:
                self.polyline(cur, width, color)
        else:
            # Dolu halka dilimi: kalın çizgi parçaları arasında boşluk/çizgi kalmasın.
            outer, inner = [], []
            for i in range(steps + 1):
                a = math.radians(a0 + (a1 - a0) * i / steps)
                outer.append((cx + (r + width / 2) * math.cos(a), cy - (r + width / 2) * math.sin(a)))
                inner.append((cx + (r - width / 2) * math.cos(a), cy - (r - width / 2) * math.sin(a)))
            self.poly(outer + inner[::-1], color)

    def polyline(self, pts, width, color):
        w = max(1, int(round(width * self.k)))
        self.d.line([self.P(*p) for p in pts], fill=rgba(color), width=w, joint="curve")

    def line(self, p0, p1, width, color, round_cap=False):
        w = width * self.k
        self.d.line([self.P(*p0), self.P(*p1)], fill=rgba(color), width=max(1, int(round(w))))
        if round_cap:
            for p in (p0, p1):
                x, y = self.P(*p)
                self.d.ellipse([x - w / 2, y - w / 2, x + w / 2, y + w / 2], fill=rgba(color))

    def poly(self, pts, fill):
        self.d.polygon([self.P(*p) for p in pts], fill=rgba(fill))

    def rrect(self, x, y, w, h, r, fill=None, outline=None, width=1.0):
        box = [x * self.k, y * self.k, (x + w) * self.k, (y + h) * self.k]
        self.d.rounded_rectangle(box, radius=r * self.k, fill=rgba(fill) if fill else None,
                                 outline=rgba(outline) if outline else None,
                                 width=max(1, int(round(width * self.k))) if outline else 0)

    def text(self, x, y, s, font_path, size, color, italic=False, spacing=0.0):
        """(x, y) metnin ortası."""
        font = ImageFont.truetype(font_path, int(round(size * self.k)))
        # harf aralığı için karakter karakter genişlik
        widths = [font.getlength(ch) for ch in s]
        total = sum(widths) + spacing * self.k * (len(s) - 1)
        asc, desc = font.getmetrics()
        layer = Image.new("RGBA", (int(total) + 8, asc + desc + 8), (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer, "RGBA")
        cx = 4.0
        for ch, w in zip(s, widths):
            ld.text((cx, 4), ch, font=font, fill=rgba(color))
            cx += w + spacing * self.k
        if italic:
            shear = 0.2
            extra = int(layer.height * shear)
            layer = layer.transform((layer.width + extra, layer.height), Image.AFFINE,
                                    (1, shear, -extra, 0, 1, 0), resample=Image.BICUBIC)
        # dikey ortalama: rakamların görünür yüksekliği
        bbox = layer.getbbox() or (0, 0, layer.width, layer.height)
        bw, bh = bbox[2] - bbox[0], bbox[3] - bbox[1]
        px, py = self.P(x, y)
        self.img.alpha_composite(layer, (int(round(px - bbox[0] - bw / 2)), int(round(py - bbox[1] - bh / 2))))

    def gradient_circle(self, cx, cy, r, stops, vertical=True, radial=False):
        """Dikey ya da radyal degrade dolu daire."""
        size = int(round(2 * r * self.k))
        layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        px = layer.load()
        for j in range(size):
            for i in range(size):
                dx, dy = (i + 0.5) / size * 2 - 1, (j + 0.5) / size * 2 - 1
                if dx * dx + dy * dy > 1:
                    continue
                if radial:
                    t = min(1.0, math.hypot(dx, dy + 0.16) / 1.2)
                else:
                    t = (dy + 1) / 2
                px[i, j] = lerp_stops(stops, t)
        mask = Image.new("L", (size, size), 0)
        ImageDraw.Draw(mask).ellipse([0, 0, size - 1, size - 1], fill=255)
        layer.putalpha(mask)
        x0, y0 = self.P(cx - r, cy - r)
        self.img.alpha_composite(layer, (int(round(x0)), int(round(y0))))

    def save(self, path, out_w=None, out_h=None):
        out = self.img.resize((out_w or self.size, out_h or int(round(self.size * self.h / self.w))), Image.LANCZOS)
        # Saydam piksellerin rengi siyah kalmasın (bilinear/mipmap kenarda koyu hale bırakmasın).
        out = dilate_transparent(out)
        out.save(path)


def lerp_stops(stops, t):
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        if t <= t1:
            f = 0 if t1 == t0 else (t - t0) / (t1 - t0)
            a, b = rgba(c0), rgba(c1)
            return tuple(int(a[i] + (b[i] - a[i]) * f) for i in range(4))
    return rgba(stops[-1][1])


def dilate_transparent(img, passes=3):
    px = img.load()
    w, h = img.size
    for _ in range(passes):
        updates = []
        for y in range(h):
            for x in range(w):
                if px[x, y][3] > 0:
                    continue
                acc = [0, 0, 0]
                n = 0
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 0:
                        c = px[nx, ny]
                        acc[0] += c[0]; acc[1] += c[1]; acc[2] += c[2]
                        n += 1
                if n:
                    updates.append((x, y, (acc[0] // n, acc[1] // n, acc[2] // n, 0)))
        if not updates:
            break
        for x, y, c in updates:
            px[x, y] = c
    return img


CHROME = [(0, "#F7F7F5"), (0.45, "#9EA2A6"), (0.55, "#C9CCCF"), (1, "#6E7276")]


def draw_dial(c, s, mini):
    cx = cy = 100
    if s["bezel"] == "chrome":
        c.gradient_circle(cx, cy, 99, CHROME)
    else:
        c.circle(cx, cy, 99, s["bezel"])
    if s.get("face2"):
        c.gradient_circle(cx, cy, 92, [(0, s["face"]), (1, s["face2"])], radial=True)
    else:
        c.circle(cx, cy, 92, s["face"])
    if s.get("ring"):
        c.ring(cx, cy, 88, 1.6, s["ring"])

    if s.get("lcd"):
        n, gap = (14, 3.5) if mini else (28, 1.5)
        r, w = (70, 26) if mini else (78, 12)
        # Kalın yay parçaları üst üste biniyor: yarı saydam çizilince çizgili görünüyordu, opak karışım kullan.
        face, dim = rgba(s["face"]), rgba(s["dim"])
        a = dim[3] / 255
        dim_opaque = tuple(int(face[i] + (dim[i] - face[i]) * a) for i in range(3)) + (255,)
        for i in range(n):
            k0 = MIN + (MAX - MIN) * i / n + gap
            k1 = MIN + (MAX - MIN) * (i + 1) / n - gap
            c.arc(cx, cy, r, k0, k1, w, dim_opaque)
        if not mini:
            c.text(cx, 110, "KM/H", FONT_MONO, 9, s["unit"], spacing=2)
        return

    for a0, a1, col in s.get("arcs", []):
        c.arc(cx, cy, 80, a0 / KN, a1 / KN, 14 if mini else 6, col)
    if s.get("red"):
        c.arc(cx, cy, 78 if mini else 82, 180, 220, 16 if mini else 7, s["red"])
    if mini:
        c.arc(cx, cy, 84, MIN, 0, 6, s["rev"])
    else:
        c.arc(cx, cy, 84, MIN, 0, 2, s["rev"], dash=(3, 3))

    f = KN if s.get("knots") else 1.0
    if mini:
        step = 20 if s.get("knots") else 40
    else:
        step = 5 if s.get("knots") else 10
    major = step if mini else 20
    label = 20 if s.get("knots") else 40
    u = math.ceil(MIN * f / step) * step
    while u <= MAX * f + 1e-6:
        kmh = u / f
        a = math.radians(angle_for(kmh))
        is_major = u % major == 0
        r1 = 64 if mini else (73 if is_major else 79)
        p0 = (cx + 86 * math.cos(a), cy - 86 * math.sin(a))
        p1 = (cx + r1 * math.cos(a), cy - r1 * math.sin(a))
        col = s["rev"] if u < 0 else s["tick"]
        c.line(p0, p1, 9 if mini else (2.6 if is_major else 1.3), col, round_cap=True)
        if not mini and u >= 0 and u % label == 0:
            pl = (cx + 60 * math.cos(a), cy - 60 * math.sin(a))
            if s.get("deco"):
                c.text(pl[0], pl[1], str(int(u)), FONT_DECO, 15, s["num"])
            else:
                c.text(pl[0], pl[1], str(int(u)), FONT_BOLD, 12, s["num"], italic=s.get("italic", False))
        u += step
    if s.get("redline"):
        a = math.radians(angle_for(s["redline"] / KN))
        c.line((cx + 88 * math.cos(a), cy - 88 * math.sin(a)), (cx + 70 * math.cos(a), cy - 70 * math.sin(a)),
               9 if mini else 3.5, "#E23B3B")
    if not mini:
        a = math.radians(angle_for(-40))
        c.text(cx + 68 * math.cos(a), cy - 68 * math.sin(a), "R", FONT_BOLD, 10, s["rev"])
        c.text(cx, 67, "KNOTS" if s.get("knots") else "KM/H", FONT_MONO, 8.5, s["unit"], spacing=1.5)


def draw_odometer(c, s, ox, oy, cw, ch, gap):
    c.rrect(ox - 3, oy - 3, 6 * cw + 5 * gap + 6, ch + 6, 3, fill=s["odoBg"])
    for i in range(6):
        x = ox + i * (cw + gap)
        c.rrect(x, oy, cw, ch, 1.5, fill=s["odoLastBg"] if i == 5 else s["odoCell"])


def draw_needle(c, s, mini):
    cx = cy = 100
    if mini:
        c.poly([(93, 104), (98.5, 26), (101.5, 26), (107, 104)], s["needle"])
        if s.get("tip"):
            c.poly([(98.5, 26), (101.5, 26), (102.5, 48), (97.5, 48)], s["tip"])
        c.circle(cx, cy, 17, s["hub"])
    else:
        c.poly([(97.6, 104), (99.2, 24), (100.8, 24), (102.4, 104)], s["needle"])
        if s.get("tip"):
            c.poly([(99.2, 24), (100.8, 24), (101.3, 40), (98.7, 40)], s["tip"])
        c.circle(cx, cy, 8, s["hub"])
        c.circle(cx, cy, 3, s["needle"])


def draw_lit(c, s, mini):
    cx = cy = 100
    n, gap = (14, 3.5) if mini else (28, 1.5)
    r, w = (70, 26) if mini else (78, 12)
    for i in range(n):
        k0 = MIN + (MAX - MIN) * i / n + gap
        k1 = MIN + (MAX - MIN) * (i + 1) / n - gap
        if (k0 + k1) / 2 >= 0:  # geri vites bölgesi yanmaz
            c.arc(cx, cy, r, k0, k1, w, s["lit"])


# Görev çubuğu plakası (206x42 tasarım birimi), 512x128'lik POT dokuya dikeyde ortalı.
PLATE_W, PLATE_H = 206.0, 42.0
PLATE_TEX_H = PLATE_W * 128 / 512  # 51.5 birim


def draw_plate(s):
    c = Canvas(512, PLATE_W, PLATE_TEX_H)
    oy = (PLATE_TEX_H - PLATE_H) / 2
    if s["bezel"] == "chrome":
        layer = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
        pxs = layer.load()
        y0, y1 = int(oy * c.k), int((oy + PLATE_H) * c.k)
        for y in range(y0, y1):
            col = lerp_stops([(0, "#F4F4F2"), (0.5, "#A9ADB1"), (1, "#7C8085")], (y - y0) / max(1, y1 - y0))
            for x in range(layer.width):
                pxs[x, y] = col
        mask = Image.new("L", c.img.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle([0, y0, layer.width - 1, y1 - 1], radius=8 * c.k, fill=255)
        layer.putalpha(mask)
        c.img.alpha_composite(layer)
    else:
        c.rrect(0, oy, PLATE_W, PLATE_H, 8, fill=s["bezel"])
    if s.get("ring"):
        c.rrect(2.5, oy + 2.5, PLATE_W - 5, PLATE_H - 5, 6, outline=s["ring"], width=1.2)
    if s.get("red"):
        c.rrect(48, oy + 37, PLATE_W - 56, 2, 1, fill=s["red"])
    if s.get("lcd"):
        c.rrect(46, oy + 4, PLATE_W - 51, 34, 4, fill=s["face"])
    draw_odometer(c, s, 53, oy + 12.5, 10.5, 17, 1.26)
    return c


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, s in SKINS.items():
        face = Canvas(512)
        draw_dial(face, s, mini=False)
        draw_odometer(face, s, 100 - (6 * 12.5 + 5 * 1.5) / 2, 128 if s.get("lcd") else 132, 12.5, 17, 1.5)
        face.save(os.path.join(OUT, f"{name}_face.png"))

        mini = Canvas(128)
        draw_dial(mini, s, mini=True)
        mini.save(os.path.join(OUT, f"{name}_mini_face.png"))

        if s.get("lcd"):
            lit = Canvas(512)
            draw_lit(lit, s, mini=False)
            lit.save(os.path.join(OUT, f"{name}_lit.png"))
            mlit = Canvas(128)
            draw_lit(mlit, s, mini=True)
            mlit.save(os.path.join(OUT, f"{name}_mini_lit.png"))
        else:
            needle = Canvas(512)
            draw_needle(needle, s, mini=False)
            needle.save(os.path.join(OUT, f"{name}_needle.png"))
            mneedle = Canvas(128)
            draw_needle(mneedle, s, mini=True)
            mneedle.save(os.path.join(OUT, f"{name}_mini_needle.png"))

        plate = draw_plate(s)
        plate.save(os.path.join(OUT, f"{name}_plate.png"), 512, 128)
        print("ok", name)


if __name__ == "__main__":
    main()
