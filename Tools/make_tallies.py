"""Ortam nesnelerinin (kadranın yerine gelen sayaç nesneleri) görsellerini üretir — 2026-10-25.

Kadran yalnız Yol'da kaldı; diğer 9 ortamın kartında sol üstte (128x128 yuva) o ortama özgü bir nesne var:
  Olta = balık kovası, Kapı = el sayacı, Rüzgârgülü = elektrik sayacı, Plak = plak kapağının arkası (parça listesi),
  Dünya = yaprak takvim, Bumerang = çetele tahtası, Çömlek = kil çuvalı, Soyacak = yazar kasa, Kebap = sıra numaratörü.
Her biri skin takımıyla değişir (SkinInfo.Tally = <takım>; Olta'da FishingGear.Tally).

Çıktı: Assets/Resources/Tallies/
  <takım>_<parça>.png   512x512, yuvanın tamamı (parçalar üst üste konur, hareketli olanlar ayrı katman)
  <takım>_preview.png   512x512, Dolap önizlemesi (katmanlar + örnek sayı)
  led_<0-9>.png         Kebap numaratörünün yedi segmentli rakamları (beyaz; oyunda takım rengiyle boyanır)
  bucket_tail.png       kovadan çıkan balık kuyruğu (beyaz-gri; oyunda takımla boyanır)
  bar.png, dot.png      Plak'ın parça çubuğu ve bitti işareti, kova sıçrama damlası (beyaz; oyunda boyanır)

Koordinatlar 200 birimlik yuva uzayında (kadranlarla aynı; C# tarafı TallyLayout ile aynı sayıları kullanır).
4x süper örnekleme ile çizilir. Düz vektör, kontur yok (kartın tabela diliyle aynı).
Kullanım: python Tools/make_tallies.py
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Tallies")
FONTS = os.path.join(ROOT, "Assets", "Fonts")
FONT_BOLD = os.path.join(FONTS, "Overpass-ExtraBold.ttf")
FONT_MONO = os.path.join(FONTS, "OverpassMono-SemiBold.ttf")

SIZE = 512
SS = 4
K = SIZE * SS / 200.0


def P(v):
    return v * K


def rgba(c, a=None):
    if isinstance(c, tuple):
        return c if a is None else c[:3] + (a,)
    c = c.lstrip("#")
    r, g, b = int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16)
    alpha = int(c[6:8], 16) if len(c) == 8 else 255
    return (r, g, b, alpha if a is None else a)


def font(path, size):
    return ImageFont.truetype(path, int(P(size)))


class Layer:
    def __init__(self, w=SIZE, h=SIZE):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rr(self, x0, y0, x1, y1, r, fill):
        self.d.rounded_rectangle([P(x0), P(y0), P(x1), P(y1)], radius=P(r), fill=rgba(fill))

    def rect(self, x0, y0, x1, y1, fill):
        self.d.rectangle([P(x0), P(y0), P(x1), P(y1)], fill=rgba(fill))

    def el(self, cx, cy, rx, ry=None, fill="#000"):
        ry = rx if ry is None else ry
        self.d.ellipse([P(cx - rx), P(cy - ry), P(cx + rx), P(cy + ry)], fill=rgba(fill))

    def ring(self, cx, cy, r, width, fill):
        self.d.ellipse([P(cx - r), P(cy - r), P(cx + r), P(cy + r)], outline=rgba(fill), width=int(P(width)))

    def poly(self, pts, fill):
        self.d.polygon([(P(x), P(y)) for x, y in pts], fill=rgba(fill))

    def line(self, pts, width, fill):
        self.d.line([(P(x), P(y)) for x, y in pts], fill=rgba(fill), width=max(1, int(P(width))), joint="curve")
        r = P(width) / 2
        for x, y in (pts[0], pts[-1]):  # yuvarlak uçlar
            self.d.ellipse([P(x) - r, P(y) - r, P(x) + r, P(y) + r], fill=rgba(fill))

    def arc(self, box, start, end, width, fill):
        x0, y0, x1, y1 = box
        self.d.arc([P(x0), P(y0), P(x1), P(y1)], start, end, fill=rgba(fill), width=int(P(width)))

    def text(self, x, y, s, size, fill, path=FONT_BOLD, anchor="mm", spacing=0.0):
        f = font(path, size)
        if spacing == 0:
            self.d.text((P(x), P(y)), s, font=f, fill=rgba(fill), anchor=anchor)
            return
        widths = [self.d.textlength(ch, font=f) for ch in s]
        total = sum(widths) + P(spacing) * (len(s) - 1)
        cx = P(x) - (total / 2 if anchor[0] == "m" else 0)
        for ch, w in zip(s, widths):
            self.d.text((cx, P(y)), ch, font=f, fill=rgba(fill), anchor="l" + anchor[1])
            cx += w + P(spacing)

    def glaze(self, draw):
        """Yarı saydam katman: ayrı tuvale çizip alfa harmanıyla üste koyar (ImageDraw dolguyu karıştırmaz)."""
        over = Layer(self.w, self.h)
        draw(over)
        self.im = Image.alpha_composite(self.im, over.im)
        self.d = ImageDraw.Draw(self.im)

    def under(self, other):
        self.im = Image.alpha_composite(other.im, self.im)
        self.d = ImageDraw.Draw(self.im)

    def final(self):
        return self.im.resize((self.w, self.h), Image.LANCZOS)


def save(im, name):
    im.save(os.path.join(OUT, name + ".png"))


def stack(*layers):
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    for layer in layers:
        out = Image.alpha_composite(out, layer if isinstance(layer, Image.Image) else layer.final())
    return out


def number(layer, x0, y0, x1, y1, s, size, fill, path=FONT_BOLD):
    """Önizleme için örnek sayı (oyunda TMP)."""
    layer.text((x0 + x1) / 2, (y0 + y1) / 2, s, size, fill, path=path)


# ---------------------------------------------------------------------------------------------- Kapı: el sayacı
DOOR = {
    "door_classic": dict(body="#C9CED4", rim="#7E868F", plate="#E8EDF1", dark="#2F3640", cell="#3A424D", ink="#2F3640"),
    "door_brass": dict(body="#C9A24A", rim="#7A5E22", plate="#F2E2B0", dark="#3A2A1A", cell="#4A3826", ink="#3A2A1A"),
    "door_black": dict(body="#2A2D33", rim="#111316", plate="#3A3E46", dark="#0B0C0E", cell="#1E2026", ink="#C9CED4"),
    "door_rose": dict(body="#C98A7A", rim="#8A5A4E", plate="#F6DCD8", dark="#4A2420", cell="#5E322C", ink="#4A2420"),
}


def door(name, c):
    button = Layer()
    button.rect(93, 24, 107, 46, c["dark"])
    button.rr(80, 12, 120, 30, 7, c["rim"])
    button.rr(80, 12, 120, 26, 7, c["plate"])

    body = Layer()
    body.ring(100, 168, 13.5, 7, c["rim"])          # parmak halkası
    body.rr(156, 88, 172, 108, 4, c["rim"])          # sıfırlama düğmesi
    body.rr(160, 92, 176, 104, 3, c["dark"])
    body.el(100, 98, 63, fill=c["rim"])
    body.el(100, 96, 60, fill=c["body"])
    body.el(100, 98, 49, fill=c["plate"])
    body.glaze(lambda g: g.el(78, 70, 22, 12, fill=(255, 255, 255, 70)))
    body.rr(59, 83, 141, 115, 6, c["dark"])         # rakam penceresi
    for i in range(4):
        x = 63 + i * 19
        body.rr(x, 87, x + 17, 111, 2, c["cell"])
    body.text(100, 127, "VISITORS", 7.5, c["ink"], spacing=1.4)
    save(button.final(), name + "_button")
    save(body.final(), name + "_body")

    prev = Layer()
    for i, ch in enumerate("0128"):
        x = 63 + i * 19
        number(prev, x, 87, x + 17, 111, ch, 15, "#F2F4F6", FONT_MONO)
    save(stack(button, body, prev), name + "_preview")


# ---------------------------------------------------------------------------------- Rüzgârgülü: elektrik sayacı
WIND = {
    "wind_classic": dict(body="#2E3238", shade="#1E2125", plate="#EEF3EA", ink="#2E4A22", cell="#1A1C20", metal="#AEB4BA"),
    "wind_timber": dict(body="#8A5A32", shade="#6E4626", plate="#F3E7CF", ink="#3B2A1A", cell="#2A1E14", metal="#C9A06A"),
    "wind_midnight": dict(body="#16181B", shade="#0B0C0E", plate="#2A2D33", ink="#7FE0FF", cell="#0B0C0E", metal="#3A3E46"),
    "wind_gold": dict(body="#C9A24A", shade="#9C7A2E", plate="#FFF0C0", ink="#3A2A08", cell="#2A2008", metal="#E6C46E"),
}


def wind(name, c):
    body = Layer()
    body.rr(32, 12, 168, 192, 20, c["shade"])
    body.rr(34, 12, 166, 188, 20, c["body"])
    body.rr(46, 24, 154, 146, 12, c["plate"])
    body.text(100, 40, "kWh", 10, c["ink"])
    body.rr(52, 54, 148, 84, 4, c["cell"])           # sayaç penceresi
    for i in range(6):
        x = 57.25 + i * 14.5
        body.rr(x, 59, x + 13, 79, 1.5, "#C8323C" if i == 5 else "#3A3E46")
    body.rr(56, 96, 144, 118, 4, c["cell"])          # disk yarığı
    body.rr(60, 103, 140, 111, 1.5, "#C9CED4")       # alüminyum diskin kenarı
    body.rect(60, 109, 140, 111, "#9AA0A8")
    body.text(100, 131, "375 REV / kWh", 6.2, c["ink"], spacing=0.6)
    body.rr(48, 156, 152, 182, 7, c["shade"])        # klemens kapağı
    for x in (64, 136):
        body.el(x, 169, 4.5, fill=c["metal"])
        body.line([(x - 2.5, 169), (x + 2.5, 169)], 1.2, c["shade"])
    body.glaze(lambda g: g.poly([(52, 30), (72, 30), (54, 140), (46, 140), (46, 52)], (255, 255, 255, 46)))
    save(body.final(), name + "_body")

    prev = Layer()
    for i, ch in enumerate("004127"):
        x = 57.25 + i * 14.5
        number(prev, x, 59, x + 13, 79, ch, 12, "#F2F4F6", FONT_MONO)
    prev.rect(98, 103, 104, 111, "#16181B")
    save(stack(body, prev), name + "_preview")


# ------------------------------------------------------------------------------ Plak: kapağın arkası (parça listesi)
VINYL = {
    "vinyl_classic": dict(sleeve="#EADFC8", band="#6E4B2A", ink="#3A2A1A", label="#C8323C", track="#D2C4A6"),
    "vinyl_white": dict(sleeve="#F2F4F6", band="#2A3A4A", ink="#2A3A4A", label="#3E8FD1", track="#D5DADF"),
    "vinyl_dj": dict(sleeve="#2A2D33", band="#E23B3B", ink="#E6E8EA", label="#E23B3B", track="#3E424A"),
    "vinyl_gold": dict(sleeve="#FFF0C0", band="#9C7A2E", ink="#3A2A08", label="#E6C46E", track="#E8D59A"),
}
VINYL_ROWS = (72, 94, 116, 138)   # parça satırlarının orta çizgisi
VINYL_BAR = (46, 120)             # parça çubuğu x aralığı (yükseklik 6)
VINYL_CHECK = 131                 # bitti işaretinin merkezi


def vinyl(name, c):
    body = Layer()
    body.el(146, 98, 54, fill="#16181B")             # kapaktan çıkan plak
    for r in (48, 41, 34, 27):
        body.ring(146, 98, r, 0.7, "#2E3138")
    body.el(146, 98, 15, fill=c["label"])
    body.el(146, 98, 2.5, fill="#16181B")
    body.glaze(lambda g: g.rr(18, 30, 148, 164, 3, (0, 0, 0, 60)))
    body.rr(14, 26, 144, 160, 3, c["sleeve"])
    body.rr(14, 26, 144, 54, 3, c["band"])
    body.rect(14, 46, 144, 54, c["band"])
    body.text(22, 40.5, "TRACKS", 8.5, c["sleeve"], anchor="lm", spacing=1.2)
    for row, label in zip(VINYL_ROWS, ("A1", "A2", "B1", "B2")):
        body.text(24, row + 0.5, label, 8, c["ink"], anchor="lm")
        body.rr(VINYL_BAR[0], row - 3, VINYL_BAR[1], row + 3, 3, c["track"])
        body.ring(VINYL_CHECK, row, 4.5, 1.4, c["track"])
    save(body.final(), name + "_body")

    prev = Layer()
    number(prev, 80, 26, 140, 54, "37", 15, c["sleeve"])
    for i, row in enumerate(VINYL_ROWS[:2]):
        prev.rr(VINYL_BAR[0], row - 3, VINYL_BAR[1] if i == 0 else 84, row + 3, 3, c["band"])
    prev.el(VINYL_CHECK, VINYL_ROWS[0], 5, fill=c["band"])
    save(stack(body, prev), name + "_preview")


# -------------------------------------------------------------------------------------- Dünya: yaprak takvim
GLOBE = {
    "globe_classic": dict(board="#6E4B2A", board2="#5A3A1F", band="#C8323C", metal="#2F3640"),
    "globe_red": dict(board="#5A1F12", board2="#46170D", band="#A9502E", metal="#2A1A14"),
    "globe_ice": dict(board="#123A4A", board2="#0C2A36", band="#4AA3C4", metal="#0C2A36"),
    "globe_gold": dict(board="#3A2A08", board2="#2A1E06", band="#C9A24A", metal="#9C7A2E"),
}
PAGE = (50, 44, 150, 176)


def page(layer, c):
    x0, y0, x1, y1 = PAGE
    layer.rect(x0, y0, x1, y1, "#FBF8F0")
    layer.rect(x0, y0 + 6, x1, y0 + 24, c["band"])
    layer.text(100, y0 + 15.5, "YEARS", 9, "#FBF8F0", spacing=1.6)
    layer.text(100, y1 - 12, "THIS MONTH", 6.5, "#9A968C", spacing=1.0)


def globe(name, c):
    body = Layer()
    body.rr(36, 10, 164, 190, 10, c["board"])
    body.rr(36, 182, 164, 190, 4, c["board2"])
    body.el(100, 20, 4, fill=c["board2"])
    body.rect(PAGE[0], PAGE[1] + 6, PAGE[2], PAGE[3] + 6, "#CFCABD")
    body.rect(PAGE[0], PAGE[1] + 3, PAGE[2], PAGE[3] + 3, "#E6E1D4")
    page(body, c)
    save(body.final(), name + "_body")

    binding = Layer()
    binding.rr(44, 34, 156, 50, 4, c["metal"])
    for x in (72, 128):
        binding.rr(x - 4, 26, x + 4, 46, 3, "#AEB4BA")
    save(binding.final(), name + "_binding")

    loose = Layer()
    page(loose, c)
    save(loose.final(), name + "_page")

    curl = Layer()                                   # köşe kıvrımı: pivot (150,176), kesirle büyür
    curl.poly([(126, 176), (150, 152), (126, 152)], "#E2DDCF")
    curl.glaze(lambda g: g.poly([(126, 176), (150, 152), (152, 156), (130, 178)], (0, 0, 0, 40)))
    save(curl.final(), name + "_curl")

    prev = Layer()
    number(prev, 50, 72, 150, 150, "37", 44, "#2A2A2A")
    save(stack(body, prev, binding), name + "_preview")


# ------------------------------------------------------------------------------------ Bumerang: çetele tahtası
BOOMERANG = {
    "boomerang_classic": dict(wood="#C9A06A", wood2="#A07A4A", grain="#B58C58", ink="#4A2A18", cord="#3B2A1A"),
    "boomerang_blue": dict(wood="#3E8FD1", wood2="#2E6FA8", grain="#4A9ADB", ink="#F2F8FC", cord="#123A5A"),
    "boomerang_carbon": dict(wood="#2A2D33", wood2="#16181B", grain="#33373E", ink="#FF5A48", cord="#0B0C0E"),
    "boomerang_gold": dict(wood="#E2B647", wood2="#B8892A", grain="#D6A93C", ink="#3A2A08", cord="#3A2A08"),
}
TALLY_GROUP_X = 32       # ilk grubun sol kenarı
TALLY_GROUP_W = 27.5     # grup genişliği (4 çizik + boşluk)
TALLY_Y = (104, 156)     # çiziklerin üst/alt ucu


def tally_marks(layer, count, ink):
    for i in range(count):
        g, k = divmod(i, 5)
        x0 = TALLY_GROUP_X + g * TALLY_GROUP_W
        if k < 4:
            x = x0 + 3 + k * 5.5
            layer.line([(x, TALLY_Y[0]), (x, TALLY_Y[1])], 3, ink)
        else:
            layer.line([(x0, TALLY_Y[1] - 8), (x0 + 24, TALLY_Y[0] + 8)], 3, ink)


def boomerang(name, c):
    body = Layer()
    body.line([(56, 42), (100, 14), (144, 42)], 2, c["cord"])
    body.el(100, 14, 3.5, fill=c["cord"])
    body.rr(22, 40, 178, 176, 10, c["wood2"])
    body.rr(22, 36, 178, 170, 10, c["wood"])
    for y, a in ((60, 0), (88, 1.2), (128, 0.6), (160, 1.6)):
        body.line([(30 + 12 * math.sin(i + a) * 0 + i * 14, y + 3 * math.sin(i * 0.9 + a)) for i in range(11)], 1.2, c["grain"])
    body.text(32, 52, "TARGETS", 8, c["ink"], anchor="lm", spacing=1.3)
    save(body.final(), name + "_body")

    prev = Layer()
    prev.text(31, 76, "37", 26, c["ink"], anchor="lm")
    tally_marks(prev, 12, c["ink"])
    save(stack(body, prev), name + "_preview")


# ---------------------------------------------------------------------------------------- Çömlek: kil çuvalı
POTTERY = {
    "pottery_classic": dict(bag="#B9733E", bag2="#94592C", tie="#2F3640", paper="#F4EEDF", ink="#3D2A1C"),
    "pottery_wood": dict(bag="#A88B62", bag2="#866C48", tie="#5A3A1F", paper="#F3E7CF", ink="#3B2A1A"),
    "pottery_porcelain": dict(bag="#DAD5CA", bag2="#A8A296", tie="#5E646C", paper="#FFFFFF", ink="#2A3A4A"),
    "pottery_gold": dict(bag="#E6C46E", bag2="#C9A24A", tie="#3A2F1C", paper="#FFF0C0", ink="#3A2A08"),
}


def pottery(name, c):
    bag = Layer()                                    # pivot alt orta (100,186): kesirle söner
    bag.glaze(lambda g: g.el(100, 186, 52, 6, fill=(0, 0, 0, 50)))
    bag.poly([(88, 44), (76, 22), (96, 36)], c["bag2"])
    bag.poly([(112, 44), (124, 22), (104, 36)], c["bag2"])
    bag.poly([(78, 66), (122, 66), (110, 42), (90, 42)], c["bag2"])
    bag.rr(39.5, 57.5, 160.5, 188.5, 38, c["bag2"])  # kenar: açık renk çuval açık arka planda kaybolmasın
    bag.rr(42, 60, 158, 186, 36, c["bag"])
    bag.rr(84, 38, 116, 47, 4, c["tie"])
    bag.glaze(lambda g: g.el(64, 108, 10, 30, fill=(255, 255, 255, 56)))
    bag.glaze(lambda g: g.rr(42, 150, 158, 186, 30, (0, 0, 0, 30)))
    bag.line([(100, 47), (100, 106)], 1.2, c["tie"])
    bag.rr(68, 106, 132, 154, 5, c["paper"])
    bag.el(100, 112, 2.2, fill=c["bag2"])
    bag.text(100, 121, "POTS", 7, c["ink"], spacing=1.4)
    save(bag.final(), name + "_bag")

    prev = Layer()
    number(prev, 68, 124, 132, 152, "37", 19, c["ink"])
    save(stack(bag, prev), name + "_preview")


# ---------------------------------------------------------------------------------------- Soyacak: yazar kasa
PEELER = {
    "peeler_classic": dict(body="#E8E0CC", body2="#C9BFA6", keys="#C8323C", window="#1A1C20", metal="#AEB4BA", ink="#F4EEDF"),
    "peeler_brass": dict(body="#A07A34", body2="#7A5A22", keys="#F2E2B0", window="#1E1A12", metal="#E6C46E", ink="#F6E7C4"),
    "peeler_chrome": dict(body="#B8BEC6", body2="#8C939B", keys="#1A1C20", window="#0B0C0E", metal="#F2F4F6", ink="#E6E8EA"),
    "peeler_mint": dict(body="#6FBFA6", body2="#4E9A84", keys="#F2EDE0", window="#1E4A3E", metal="#DDE2E6", ink="#E8F6F0"),
}
PEELER_WINDOW = (72, 34, 128, 64)


def peeler(name, c):
    inside = Layer()                                 # açılınca görünen çekmece içi (paralar)
    inside.rr(30, 150, 170, 178, 3, "#16181B")
    for x in (52, 70, 88, 112, 128, 146):
        inside.el(x, 164, 6, 3, fill="#E6C46E")
    save(inside.final(), name + "_inside")

    body = Layer()
    body.line([(172, 118), (188, 96)], 4, c["metal"])  # kol
    body.el(188, 94, 6, fill=c["keys"])
    body.rr(64, 24, 136, 96, 8, c["body2"])
    body.rr(PEELER_WINDOW[0], PEELER_WINDOW[1], PEELER_WINDOW[2], PEELER_WINDOW[3], 4, c["window"])
    body.text(100, 77, "FRUIT", 7, c["window"], spacing=1.6)
    body.poly([(28, 87.5), (172, 87.5), (180.5, 158), (19.5, 158)], c["body2"])  # kenar
    body.poly([(30, 90), (170, 90), (178, 156), (22, 156)], c["body"])
    for r, y in enumerate((100, 116, 132)):
        inset = 6 - r * 2.5
        for i in range(6):
            x = 38 + inset + i * (124 - 2 * inset - 10) / 5
            body.rr(x, y, x + 13, y + 10, 3, c["keys"])
    save(body.final(), name + "_body")

    drawer = Layer()                                 # açılınca aşağı kayar
    drawer.rr(24, 150, 176, 184, 4, c["body2"])
    drawer.rr(86, 162, 114, 170, 3, c["metal"])
    save(drawer.final(), name + "_drawer")

    prev = Layer()
    number(prev, *PEELER_WINDOW, "037", 19, c["ink"], FONT_MONO)
    save(stack(inside, body, drawer, prev), name + "_preview")


# ------------------------------------------------------------------------------------ Kebap: sıra numaratörü
KEBAB = {
    "kebab_classic": dict(box="#2F3640", box2="#1E232A", strip="#C8323C", led="#FF3B30"),
    "kebab_copper": dict(box="#8A4A2A", box2="#6A3820", strip="#C77B4A", led="#FFB020"),
    "kebab_blue": dict(box="#1E3A5A", box2="#122A44", strip="#3E8FD1", led="#5CC8FF"),
    "kebab_gold": dict(box="#3A2A08", box2="#2A1E06", strip="#C9A24A", led="#FFD866"),
}
KEBAB_DIGITS = [(40 + i * 42, 82, 40 + i * 42 + 36, 142) for i in range(3)]

# Yedi segment: a üst, b sağ üst, c sağ alt, d alt, e sol alt, f sol üst, g orta.
SEGMENTS = {0: "abcdef", 1: "bc", 2: "abged", 3: "abgcd", 4: "fgbc", 5: "afgcd", 6: "afgedc", 7: "abc", 8: "abcdefg", 9: "abcdfg"}


def led_digit(n):
    w, h = 36, 60                                    # birim; 4 px/birim
    layer = Layer(w * 4, h * 4)
    k = 4 * SS
    t, g, slant = 5.5, 0.9, 2.5

    def seg(x0, y0, x1, y1, horizontal):
        if horizontal:
            pts = [(x0 + t / 2, y0), (x1 - t / 2, y0), (x1, (y0 + y1) / 2), (x1 - t / 2, y1), (x0 + t / 2, y1), (x0, (y0 + y1) / 2)]
        else:
            pts = [(x0, y0 + t / 2), ((x0 + x1) / 2, y0), (x1, y0 + t / 2), (x1, y1 - t / 2), ((x0 + x1) / 2, y1), (x0, y1 - t / 2)]
        sk = [(x + slant * (1 - y / h), y) for x, y in pts]
        layer.d.polygon([(x * k, y * k) for x, y in sk], fill=(255, 255, 255, 255))

    L, R, T, M, B = 3, w - 6, 2, h / 2, h - 2
    shapes = {
        "a": (L + g, T, R - g, T + t, True), "d": (L + g, B - t, R - g, B, True), "g": (L + g, M - t / 2, R - g, M + t / 2, True),
        "f": (L, T + g, L + t, M - g, False), "b": (R - t, T + g, R, M - g, False),
        "e": (L, M + g, L + t, B - g, False), "c": (R - t, M + g, R, B - g, False),
    }
    for s in SEGMENTS[n]:
        seg(*shapes[s])
    return layer.im.resize((w * 4, h * 4), Image.LANCZOS)


def kebab(name, c):
    body = Layer()
    for x in (52, 148):
        body.line([(x, 6), (x, 40)], 2, "#6B6F76")
    body.rr(14, 36, 186, 162, 12, c["box2"])
    body.rr(14, 36, 186, 156, 12, c["box"])
    body.rr(22, 44, 178, 68, 5, c["strip"])
    body.text(100, 56.5, "NOW SERVING", 9.5, "#FFFFFF", spacing=1.5)
    body.rr(30, 76, 170, 148, 7, "#0B0C0E")
    save(body.final(), name + "_body")

    prev = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    px = SIZE / 200.0
    for (x0, y0, x1, y1), ch in zip(KEBAB_DIGITS, "047"):
        for n, alpha in ((8, 34), (int(ch), 255)):
            im = led_digit(n).resize((round((x1 - x0) * px), round((y1 - y0) * px)), Image.LANCZOS)
            tint = Image.new("RGBA", im.size, rgba(c["led"]))
            tint.putalpha(im.getchannel("A").point(lambda a: a * alpha // 255))
            prev.alpha_composite(tint, (round(x0 * px), round(y0 * px)))
    save(stack(body, prev), name + "_preview")


# --------------------------------------------------------------------------------------- Olta: balık kovası
BUCKET = {
    "bucket_classic": dict(body="#AEB4BA", body2="#8C939B", rim="#D5DADF", inside="#4A5058", water="#3E7FA8", handle="#5E646C", ink="#FFFFFF", weave=False),
    "bucket_bamboo": dict(body="#C9A06A", body2="#A07A4A", rim="#8A6238", inside="#3B2A1A", water="#3B2A1A", handle="#8A6238", ink="#FFF6E2", weave=True),
    "bucket_carbon": dict(body="#2A2D33", body2="#1A1C20", rim="#E23B3B", inside="#0B0C0E", water="#1E4A6A", handle="#16181B", ink="#E6E8EA", weave=False),
    "bucket_gold": dict(body="#E6C46E", body2="#C9A24A", rim="#FFF0C0", inside="#7A5E22", water="#3E7FA8", handle="#9C7A2E", ink="#3A2A08", weave=False),
}
BUCKET_WATER = (50, 66, 150, 80)


def bucket(name, c):
    back = Layer()
    back.arc((40, 20, 160, 124), 180, 360, 3.5, c["handle"])
    back.el(100, 73, 54, 9, fill=c["inside"])
    save(back.final(), name + "_back")

    water = Layer()
    x0, y0, x1, y1 = BUCKET_WATER
    water.el((x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2, (y1 - y0) / 2, fill=c["water"])
    if not c["weave"]:
        water.glaze(lambda g: g.el(84, 72, 18, 2.5, fill=(255, 255, 255, 60)))
    save(water.final(), name + "_water")

    front = Layer()
    front.poly([(46, 74), (154, 74), (142, 186), (58, 186)], c["body"])
    if c["weave"]:
        for i, y in enumerate(range(80, 186, 9)):
            if i % 2:
                front.poly([(46 + (y - 74) * 0.111, y), (154 - (y - 74) * 0.111, y), (154 - (y + 4.5 - 74) * 0.111, y + 4.5),
                            (46 + (y + 4.5 - 74) * 0.111, y + 4.5)], c["body2"])
        for x in range(56, 150, 12):
            front.line([(x, 78), (x + (100 - x) * 0.11, 184)], 1.6, c["body2"])
    else:
        for y in (100, 162):
            w = (y - 74) * 0.111
            front.poly([(46 + w, y), (154 - w, y), (154 - w - 0.5, y + 4), (46 + w + 0.5, y + 4)], c["body2"])
        front.glaze(lambda g: g.poly([(56, 80), (68, 80), (72, 182), (64, 182)], (255, 255, 255, 50)))
    front.rr(58, 182, 142, 188, 3, c["body2"])
    front.rr(42, 68, 158, 79, 5, c["rim"])
    front.el(46, 73, 4, fill=c["handle"])
    front.el(154, 73, 4, fill=c["handle"])
    front.text(100, 170, "FISH", 7.5, c["ink"], spacing=1.6)
    save(front.final(), name + "_front")

    prev = Layer()
    number(prev, 60, 104, 140, 154, "12", 28, c["ink"])
    tail_im = tail().resize((int(P(30) / SS), int(P(30) / SS)), Image.LANCZOS)
    tails = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    for x, rot in ((78, 12), (112, -10)):
        t = tail_im.rotate(rot, resample=Image.BICUBIC, expand=False)
        tinted = Image.new("RGBA", t.size, rgba("#A8BAC4"))
        tinted.putalpha(t.getchannel("A"))
        tails.alpha_composite(tinted, (int(P(x - 15) / SS), int(P(48) / SS)))
    save(stack(back, water, tails, front, prev), name + "_preview")


def tail():
    """Balık kuyruğu (çatallı yüzgeç + sap), 40x40 birim tuval, sap alt ortada (20,40)."""
    layer = Layer(160, 160)
    k = 4 * SS
    pts = [(20, 40), (15, 26), (4, 6), (13, 9), (20, 18), (27, 9), (36, 6), (25, 26)]
    layer.d.polygon([(x * k, y * k) for x, y in pts], fill=(255, 255, 255, 255))
    over = Layer(160, 160)
    over.d.polygon([(x * k, y * k) for x, y in [(20, 40), (17, 28), (20, 22), (23, 28)]], fill=(0, 0, 0, 40))
    layer.im = Image.alpha_composite(layer.im, over.im)
    return layer.im.resize((160, 160), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    for table, fn in ((DOOR, door), (WIND, wind), (VINYL, vinyl), (GLOBE, globe), (BOOMERANG, boomerang),
                      (POTTERY, pottery), (PEELER, peeler), (KEBAB, kebab), (BUCKET, bucket)):
        for name, colors in table.items():
            fn(name, colors)
    for n in range(10):
        led_digit(n).save(os.path.join(OUT, f"led_{n}.png"))
    tail().save(os.path.join(OUT, "bucket_tail.png"))
    bar = Layer(296, 24)                             # Plak çubuğu: 74x6 birim, 4 px/birim
    bar.d.rounded_rectangle([0, 0, 296 * SS - 1, 24 * SS - 1], radius=12 * SS, fill=(255, 255, 255, 255))
    bar.final().save(os.path.join(OUT, "bar.png"))
    dot = Layer(64, 64)
    dot.d.ellipse([0, 0, 64 * SS - 1, 64 * SS - 1], fill=(255, 255, 255, 255))
    dot.final().save(os.path.join(OUT, "dot.png"))
    print("Tallies ->", OUT, len([f for f in os.listdir(OUT) if f.endswith(".png")]), "png")


if __name__ == "__main__":
    main()
