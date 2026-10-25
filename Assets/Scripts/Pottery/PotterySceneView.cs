using UnityEngine;

namespace ScrollRoad
{
    // Çömlekçi çarkının sahnesi (2026-10-14). Kart: köy atölyesi (sıcak bej duvar, ahşap zemin). Solda çömlekçi
    // (tek karakter, Gemini) taburede, elleri çarktaki çamurda; çark kodla (ayak, çamur teknesi, dönen tabla —
    // tabladaki işaretler yatay kayar). Çamur kodla, her karede yeniden çizilen küçük bir dokuya: topak, çömleğin
    // profiline doğru yükselip biçimlenir; dönüş, yüzeyde kayan dikey parıltı bantlarından okunur. Biten
    // çömlek pişmiş/sırlı görseline (Gemini) geçer ve sağdaki rafa taşınır; raftaki çömlekler birer yuva kayar.
    //
    // Görev çubuğu şeridi (yatay yorum): çark solda yazının yanında, sağına uzanan uzun bir raf — bu ay biten
    // son çömlekler yan yana dizilir.
    //
    // Sahne konumun SAF fonksiyonu (Phase evreleri PotteryEnvironment'ta); geri sarınca çömlek raftan çarka
    // döner ve topağa çöker.
    public class PotterySceneView : IntervalSceneView
    {
        [SerializeField] PotteryEnvironment env;
        [SerializeField] Sprite potterSprite;
        // Çömlekçinin alt ortasının çarka göre x'i (kart, dünya birimi) ve boyu.
        [SerializeField] float potterOffset = -1.25f;
        [SerializeField] float potterHeight = 2.7f;

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "Workshop";

        const int TexW = 96, TexH = 116;
        const float Ppu = 80f;           // dokuda çömlek birimi başına piksel
        const float HalfWidth = 0.6f;    // dokunun yarı genişliği (çömlek birimi)
        const float DegreesPerPoint = 40f; // tabla: seyirde ~2 tur/sn
        const int Marks = 4;
        const int CardSlots = 5, StripSlots = 9;

        // Ortam Standardı v2: mekân duvar/zemin/rafı (ItemSite.Palette 0-2), skin takımı çarkı ve çamuru boyar
        // (SkinInfo.Palette 0-3). Varsayılanlar Köy Atölyesi ve Klasik.
        static readonly Color WallDefault = new Color(0.93f, 0.86f, 0.74f);
        static readonly Color FloorDefault = new Color(0.60f, 0.44f, 0.31f);
        static readonly Color ShelfWoodDefault = new Color(0.56f, 0.38f, 0.24f);
        static readonly Color WheelDarkDefault = new Color(0.30f, 0.30f, 0.32f);
        static readonly Color WheelHeadDefault = new Color(0.55f, 0.56f, 0.58f);
        static readonly Color PanDefault = new Color(0.42f, 0.43f, 0.46f);
        static readonly Color ClayDefault = new Color(0.72f, 0.47f, 0.35f);
        Color Wall => Look(0, WallDefault);
        Color Floor => Look(1, FloorDefault);
        Color ShelfWood => Look(2, ShelfWoodDefault);
        Color WheelDark => Tint(0, WheelDarkDefault);
        Color WheelHead => Tint(1, WheelHeadDefault);
        Color Pan => Tint(2, PanDefault);
        Color Clay => Tint(3, ClayDefault);

        Texture2D clayTex;
        Sprite claySprite;
        Color32[] clayPixels;
        readonly float[] rows = new float[TexH];

        SpriteRenderer floor, potter, pedestal, basePlate, pan, panRim, head, clay, wheelPot, carried, shelf, shelfEdge, windowFrame, windowPane;
        SpriteRenderer[] windowBars;
        SpriteRenderer[] marks, shelfPots, brackets;

        struct Layout
        {
            public float FloorY, WheelX, HeadY, Scale, ShelfY, ShelfX0, ShelfX1, SlotX0, SlotGap, ShelfScale, HeadW;
            public int Slots;
        }

        protected override void Build()
        {
            clayTex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            clayPixels = new Color32[TexW * TexH];
            claySprite = Sprite.Create(clayTex, new Rect(0, 0, TexW, TexH), new Vector2(0.5f, 0f), Ppu);

            floor = Create("Floor", whiteSprite, 2);
            windowFrame = Create("WindowFrame", whiteSprite, 1);
            windowPane = Create("WindowPane", whiteSprite, 1);
            windowBars = CreatePool("WindowBar", whiteSprite, 1, 2);
            basePlate = Create("BasePlate", whiteSprite, 9);
            panRim = Create("PanRim", whiteSprite, 10);
            shelf = Create("Shelf", whiteSprite, 4);
            shelfEdge = Create("ShelfEdge", whiteSprite, 5);
            brackets = CreatePool("Bracket", whiteSprite, 3, 3);
            shelfPots = CreatePool("ShelfPot", whiteSprite, 6, StripSlots + 1);
            potter = Create("Potter", potterSprite != null ? potterSprite : whiteSprite, 8);
            pedestal = Create("Pedestal", whiteSprite, 9);
            pan = Create("Pan", whiteSprite, 10);
            head = Create("WheelHead", whiteSprite, 11);
            marks = CreatePool("Mark", whiteSprite, 12, Marks);
            clay = Create("Clay", claySprite, 13);
            wheelPot = Create("WheelPot", whiteSprite, 14);
            carried = Create("CarriedPot", whiteSprite, 15);
        }

        protected override void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX)
        {
            bottom = -0.2f;
            top = 2.6f;
            anchorWorldX = 0f;      // çark
            anchorScreenX = 0.37f;
        }

        static Layout LayoutFor(bool strip) => strip
            ? new Layout { FloorY = 0f, WheelX = 0f, HeadY = 0.45f, Scale = 1.2f, ShelfY = 0.06f, ShelfX0 = 1.2f, ShelfX1 = 40f, SlotX0 = 2.1f, SlotGap = 1.65f, ShelfScale = 1.1f, HeadW = 1.3f, Slots = StripSlots }
            : new Layout { FloorY = -2.95f, WheelX = -2.2f, HeadY = -1.55f, Scale = 1.25f, ShelfY = -0.75f, ShelfX0 = 1.0f, ShelfX1 = 7.8f, SlotX0 = 1.8f, SlotGap = 1.25f, ShelfScale = 0.85f, HeadW = 1.5f, Slots = CardSlots };

        protected override void Draw(bool strip, bool night, float left, float right, float top, float bottom)
        {
            Background(strip, Wall);
            var l = LayoutFor(strip);
            long done = env.Done;
            float f = env.Phase;
            var tint = Opaque(WorldTheme.Surface(Color.white) * (night ? 0.8f : 1f));

            // Zemin ve raf.
            floor.enabled = !strip;
            if (!strip) PlaceSpan(floor, left - 0.2f, right + 0.2f, bottom - 0.2f, l.FloorY, Opaque(WorldTheme.Land(Floor)));
            var wood = Opaque(WorldTheme.Surface(ShelfWood) * (night ? 0.75f : 1f));
            float shelfLeft = strip ? l.ShelfX0 : l.ShelfX0;
            float shelfRight = strip ? right + 0.5f : l.ShelfX1;
            PlaceSpan(shelf, shelfLeft, shelfRight, l.ShelfY - (strip ? 0.1f : 0.12f), l.ShelfY, wood);
            PlaceSpan(shelfEdge, shelfLeft, shelfRight, l.ShelfY - (strip ? 0.1f : 0.12f), l.ShelfY - (strip ? 0.07f : 0.08f), Opaque(wood * 0.8f));
            for (int i = 0; i < brackets.Length; i++)
            {
                brackets[i].enabled = !strip;
                if (!strip) PlaceRect(brackets[i], l.ShelfX0 + 0.6f + i * 2.9f, l.ShelfY - 0.32f, 0.1f, 0.42f, Opaque(wood * 0.85f));
            }

            // Duvarda pencere (yalnız kart): dört camlı, ahşap çerçeve; camda gökyüzü (temaya göre).
            windowFrame.enabled = windowPane.enabled = !strip;
            foreach (var bar in windowBars) bar.enabled = !strip;
            if (!strip)
            {
                PlaceRect(windowFrame, -0.55f, 0.65f, 1.9f, 1.55f, wood);
                PlaceRect(windowPane, -0.55f, 0.65f, 1.66f, 1.31f, Opaque(WorldTheme.Sky(new Color(0.74f, 0.87f, 0.96f))));
                PlaceRect(windowBars[0], -0.55f, 0.65f, 0.08f, 1.31f, wood);
                PlaceRect(windowBars[1], -0.55f, 0.65f, 1.66f, 0.08f, wood);
            }

            // Çömlekçi (yalnız kart).
            potter.enabled = !strip && potterSprite != null;
            if (potter.enabled) PlaceSprite(potter, potterSprite, l.WheelX + potterOffset, l.FloorY, potterHeight, tint);

            // Çark: ayak, çamur teknesi, tabla + kayan işaretler.
            float s = l.Scale;
            var dark = Opaque(WorldTheme.Surface(WheelDark) * (night ? 0.8f : 1f));
            PlaceSpan(basePlate, l.WheelX - 0.45f * s, l.WheelX + 0.45f * s, l.FloorY, l.FloorY + 0.07f * s, dark);
            PlaceSpan(pedestal, l.WheelX - 0.13f * s, l.WheelX + 0.13f * s, l.FloorY, l.HeadY - 0.3f * s, dark);
            var panC = Opaque(WorldTheme.Surface(Pan) * (night ? 0.8f : 1f));
            // Çamur teknesi: alta doğru daralan gövde + açık kenarlı ağız (yandan yuvarlak tekne gibi okunur).
            PlaceSpan(pan, l.WheelX - 0.6f * s, l.WheelX + 0.6f * s, l.HeadY - 0.32f * s, l.HeadY - 0.1f * s, panC);
            PlaceSpan(panRim, l.WheelX - 0.72f * s, l.WheelX + 0.72f * s, l.HeadY - 0.13f * s, l.HeadY - 0.06f * s, Opaque(panC * 1.18f));
            var headC = Opaque(WorldTheme.Surface(WheelHead) * (night ? 0.85f : 1f));
            PlaceSpan(head, l.WheelX - l.HeadW / 2f, l.WheelX + l.HeadW / 2f, l.HeadY - 0.08f * s, l.HeadY, headC);
            float spin = (float)(env.Position * DegreesPerPoint % 360.0);
            for (int k = 0; k < Marks; k++)
            {
                float a = (spin + k * 360f / Marks) * Mathf.Deg2Rad;
                float depth = Mathf.Cos(a);
                marks[k].enabled = depth > 0f;
                if (!marks[k].enabled) continue;
                float x = l.WheelX + Mathf.Sin(a) * l.HeadW / 2f * 0.92f;
                PlaceRect(marks[k], x, l.HeadY - 0.04f * s, 0.06f * s * (0.4f + 0.6f * depth), 0.06f * s, WithAlpha(Color.white, 0.55f * depth));
            }

            DrawClay(l, f, done, spin, night);
            DrawShelf(l, f, done, night, tint, strip, left);
        }

        void DrawClay(Layout l, float f, long done, float spin, bool night)
        {
            var item = env.Identify(done + 1);
            float landing = Smooth(0f, PotteryEnvironment.ShapeStart * 0.75f, f);
            float shape = Smooth(PotteryEnvironment.ShapeStart, PotteryEnvironment.ShapeEnd, f);
            float rawAlpha = (1f - Smooth(PotteryEnvironment.ShapeEnd, PotteryEnvironment.FireEnd, f)) * landing;
            float potAlpha = Smooth(PotteryEnvironment.ShapeEnd, PotteryEnvironment.FireEnd, f);
            bool carrying = f >= PotteryEnvironment.FireEnd;

            clay.enabled = rawAlpha > 0.01f;
            if (clay.enabled)
            {
                RasterClay(item.Type, shape, landing, spin, night);
                clay.transform.localPosition = new Vector3(l.WheelX, l.HeadY - 0.01f, 0f);
                clay.transform.localRotation = Quaternion.identity;
                clay.transform.localScale = new Vector3(l.Scale, l.Scale, 1f);
                clay.color = WithAlpha(Color.white, rawAlpha);
            }

            var sprite = SpriteFor(item);
            var tint = WorldTheme.Surface(Color.white) * (night ? 0.82f : 1f);
            float h = item.Type.Size * l.Scale;
            if (!carrying)
            {
                carried.enabled = false;
                PlaceSprite(wheelPot, sprite, l.WheelX, l.HeadY - 0.02f, h, WithAlpha(tint, potAlpha));
            }
            else
            {
                wheelPot.enabled = false;
                float t = Smooth(PotteryEnvironment.FireEnd, 1f, f);
                var a = new Vector2(l.WheelX, l.HeadY);
                var b = new Vector2(l.SlotX0, l.ShelfY);
                var ctrl = new Vector2((a.x + b.x) / 2f, Mathf.Max(a.y, b.y) + (l.FloorY == 0f ? 0.5f : 1.2f));
                float u = 1f - t;
                var p = u * u * a + 2f * u * t * ctrl + t * t * b;
                float scale = Mathf.Lerp(l.Scale, l.ShelfScale, t);
                PlaceSprite(carried, sprite, p.x, p.y, item.Type.Size * scale, WithAlpha(tint, 1f));
            }
        }

        // Raftaki çömlekler: yuva 0'da son biten (n); taşıma sırasında hepsi bir yuva sağa kayar, en sondaki söner.
        void DrawShelf(Layout l, float f, long done, bool night, Color tint, bool strip, float left)
        {
            float shift = f >= PotteryEnvironment.FireEnd ? Smooth(PotteryEnvironment.FireEnd, 1f, f) : 0f;
            int used = 0;
            for (int k = 0; k < l.Slots && used < shelfPots.Length; k++)
            {
                long index = done - k;
                if (index < 1) break;
                var item = env.Identify(index);
                float slot = k + shift;
                float x = l.SlotX0 + slot * l.SlotGap;
                float alpha = k == l.Slots - 1 ? 1f - shift : 1f;
                var sr = shelfPots[used++];
                var c = tint;
                c.a = alpha;
                PlaceSprite(sr, SpriteFor(item), x, l.ShelfY - 0.01f, item.Type.Size * l.ShelfScale, c);
            }
            for (int k = used; k < shelfPots.Length; k++) shelfPots[k].enabled = false;
        }

        // Çamur dokusu: profilin her satırı için yarıçap, sonra soldan ışık + dönen parıltı bantları + çark izleri.
        void RasterClay(ItemType type, float shape, float landing, float spin, bool night)
        {
            float lumpH = 0.42f;
            float height = Mathf.Lerp(lumpH, type.Size, shape) * Mathf.Lerp(0.6f, 1f, landing);
            for (int y = 0; y < TexH; y++)
            {
                float hy = (y + 0.5f) / Ppu;
                float t = hy / Mathf.Max(0.01f, height);
                if (t > 1f)
                {
                    rows[y] = -1f;
                    continue;
                }
                float lump = 0.45f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(t, 1.6f)));
                float target = PotteryEnvironment.Profile(type, t) * type.Size;
                rows[y] = Mathf.Min(HalfWidth - 1f / Ppu, Mathf.Lerp(lump, target, shape) * Mathf.Lerp(0.7f, 1f, landing));
            }

            var baseC = WorldTheme.Surface(Clay) * (night ? 0.82f : 1f);
            float spinRad = spin * Mathf.Deg2Rad;
            for (int y = 0; y < TexH; y++)
            {
                float r = rows[y];
                float hy = (y + 0.5f) / Ppu;
                float ring = 0.035f * Mathf.Sin(hy * 60f);
                bool rim = r > 0f && (y + 1 >= TexH || rows[y + 1] < 0f || y + 2 < TexH && rows[y + 2] < 0f);
                for (int x = 0; x < TexW; x++)
                {
                    int i = y * TexW + x;
                    if (r <= 0f)
                    {
                        clayPixels[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float dx = (x + 0.5f) / Ppu - HalfWidth;
                    float a = Mathf.Clamp01((r - Mathf.Abs(dx)) * Ppu + 0.5f);
                    if (a <= 0f)
                    {
                        clayPixels[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float xn = Mathf.Clamp(dx / r, -1f, 1f);
                    float light = 0.86f - 0.22f * xn;
                    light += 0.13f * Mathf.Exp(-Mathf.Pow((xn + 0.45f) / 0.18f, 2f)); // ıslak parıltı
                    light += 0.05f * Mathf.Sin(Mathf.Asin(xn) * 7f + spinRad);      // dönen bantlar
                    light += ring;
                    if (rim) light *= 0.72f; // ağız
                    var c = baseC * light;
                    clayPixels[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), a);
                }
            }
            clayTex.SetPixels32(clayPixels);
            clayTex.Apply(false);
        }

        Sprite SpriteFor(EnvItem item) => ItemSprite(item.Type.Id);
    }
}
