using UnityEngine;

namespace ScrollRoad
{
    // Kebap sahnesi (2026-10-25 yeniden: dikey döner büfesi; ilk hâli 2026-10-14 yatay çağ kebabıydı). Kart: döner
    // tezgâhı (açık duvar, alt yarıda fayans, önde tezgâh). Dikey şişte koni biçimli et döner; solunda (arkasında)
    // kızaran ızgara paneli, altında damlama tepsisi. Et kodla, her karede bir dokuya çizilir: silindire sarılmış
    // kat çizgileri ve kızarık lekeler dönüşle yatay kayar, soldan ızgaranın turuncu ışığı vurur. Usta (Gemini)
    // sağda durur; ön kolu ayrı görsel (Tools/gen_kebab.py --split), dirsekten döner: kesimde birkaç kez yukarıdan
    // aşağı bıçak çeker. Bıçak etin ARKASINDA çizilir, görünen kısmı yumruktan etin kenarına kadardır. Etin bıçağın
    // değdiği kuşağı (sağ kenardan dönüşle görünür yüze akarak) kızarık kabuğunu kaybedip açık renge döner, kesilen
    // dilimler tepsiye düşer. Porsiyon tezgâhtaki tabakta belirir, sonra sağdaki pass'e sürülür (son 3 porsiyon).
    //
    // Görev çubuğu şeridi: küçük dikey döner yazının yanında, sağında son porsiyonlar; usta ve tezgâh yok.
    //
    // Sahne konumun SAF fonksiyonu (evreler KebabEnvironment'ta): geri sarınca et ters döner, kesim geri alınır.
    public class KebabSceneView : IntervalSceneView
    {
        [SerializeField] KebabEnvironment env;
        [SerializeField] Sprite chefBodySprite;
        [SerializeField] Sprite chefArmSprite;

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "KebabHouse";

        const float Ppu = 64f;               // et dokusunda birim başına piksel
        const float DegreesPerPoint = 9f;    // şiş: seyirde ~0,4 tur/sn
        const int CoilCount = 7;
        const int SliceCount = 10;
        const int PassSlots = 3;
        const int TileRows = 4, TileCols = 18;
        const int Strokes = 5;               // kesimde bıçak çekişi
        const float BandLow = 0.06f, BandHigh = 0.62f; // bıçağın eriştiği kuşak (etin altından oran)

        // Usta görselinde (264x512, sol üstten piksel) dirsek, bıçak sapının alt ve üst ucu.
        static readonly Vector2 ArmPivotPx = new Vector2(84f, 188f);
        static readonly Vector2 HandleBottomPx = new Vector2(18f, 170f);
        static readonly Vector2 HandleTopPx = new Vector2(21f, 121f);
        const float ChefHeight = 3.6f;
        const float BladeTilt = 35f;         // bıçak sapın ekseninden ete doğru (saat yönünde) bu kadar açılır
        const float ArmRaised = -28f, ArmLowered = 12f, ArmRest = -8f;

        // Ortam Standardı v2: mekân duvar/ızgara gövdesi/tezgâhı (ItemSite.Palette 0-3), skin takımı şişi ve
        // ızgarayı boyar (SkinInfo.Palette 0-3: şiş çeliği, ızgara ışığı 1, ızgara ışığı 2, rezistans). Varsayılanlar
        // Kebap Evi ve Klasik.
        static readonly Color WallDefault = new Color(0.95f, 0.91f, 0.84f);
        static readonly Color GrillBodyDefault = new Color(0.66f, 0.34f, 0.24f);
        Color Wall => Look(0, WallDefault);
        Color GrillBody => Look(1, GrillBodyDefault);
        static readonly Color EmberDefault = new Color(1f, 0.48f, 0.16f);
        static readonly Color FlameADefault = new Color(1f, 0.62f, 0.15f);
        static readonly Color FlameBDefault = new Color(1f, 0.86f, 0.35f);
        Color Ember => Tint(3, EmberDefault);
        Color FlameA => Tint(1, FlameADefault);
        Color FlameB => Tint(2, FlameBDefault);
        static readonly Color Cooked = new Color(0.52f, 0.29f, 0.15f);
        static readonly Color Crisp = new Color(0.36f, 0.19f, 0.09f);
        static readonly Color Fresh = new Color(0.80f, 0.55f, 0.40f);
        static readonly Color SteelDefault = new Color(0.78f, 0.80f, 0.83f);
        static readonly Color CounterDefault = new Color(0.45f, 0.47f, 0.50f);
        static readonly Color CounterTopDefault = new Color(0.72f, 0.74f, 0.76f);
        Color Steel => Tint(0, SteelDefault);
        Color Counter => Look(2, CounterDefault);
        Color CounterTop => Look(3, CounterTopDefault);

        Texture2D meatTex;
        Sprite meatSprite;
        Color32[] meatPixels;
        int texW, texH;
        bool texStrip;

        SpriteRenderer chefBody, chefArm, rod, cap, tray, trayLip, panel, panelInner, meat, blade, counter, counterTop, serving, sliding;
        SpriteRenderer[] coils, slices, pass, tilesH, tilesV;

        struct Layout
        {
            public float MeatX, MeatY0, MeatY1, RTop, RBottom, TrayY, CounterY, PlateX, PassX0, PassGap, ServeScale;
        }

        protected override void Build()
        {
            tilesH = CreatePool("TileRow", whiteSprite, 1, TileRows);
            tilesV = CreatePool("TileCol", whiteSprite, 1, TileCols);
            panel = Create("GrillPanel", whiteSprite, 3);
            panelInner = Create("GrillInner", whiteSprite, 4);
            coils = CreatePool("Coil", whiteSprite, 5, CoilCount);
            rod = Create("Spit", whiteSprite, 6);
            blade = Create("Blade", whiteSprite, 7);
            meat = Create("Meat", whiteSprite, 8);
            cap = Create("SpitCap", whiteSprite, 9);
            tray = Create("Tray", whiteSprite, 10);
            trayLip = Create("TrayLip", whiteSprite, 10);
            slices = CreatePool("Slice", whiteSprite, 11, SliceCount);
            chefArm = Create("ChefArm", chefArmSprite != null ? chefArmSprite : whiteSprite, 12);
            chefBody = Create("ChefBody", chefBodySprite != null ? chefBodySprite : whiteSprite, 13);
            counter = Create("Counter", whiteSprite, 20);
            counterTop = Create("CounterTop", whiteSprite, 21);
            pass = CreatePool("Pass", whiteSprite, 23, PassSlots);
            serving = Create("Serving", whiteSprite, 23);
            sliding = Create("Sliding", whiteSprite, 24);
        }

        protected override void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX)
        {
            bottom = -0.2f;
            top = 2.6f;
            anchorWorldX = 0f;      // şiş
            anchorScreenX = 0.36f;
        }

        static Layout LayoutFor(bool strip) => strip
            ? new Layout { MeatX = 0f, MeatY0 = 0.42f, MeatY1 = 2.15f, RTop = 0.55f, RBottom = 0.34f, TrayY = 0.18f, CounterY = 0f, PlateX = 1.7f, PassX0 = 3.2f, PassGap = 1.5f, ServeScale = 1.6f }
            : new Layout { MeatX = -3.7f, MeatY0 = -1.45f, MeatY1 = 1.05f, RTop = 0.86f, RBottom = 0.5f, TrayY = -1.78f, CounterY = -2.0f, PlateX = 0.5f, PassX0 = 2.1f, PassGap = 1.5f, ServeScale = 1.8f };

        // Etin y'deki yarıçapı (v: etin altından 0..1): üstte geniş koni, hafif yumrular, yuvarlak üst ve alt.
        static float RadiusAt(Layout l, float v)
        {
            if (v < 0f || v > 1f) return 0f;
            float r = Mathf.Lerp(l.RBottom, l.RTop, Mathf.Pow(v, 0.85f)) * (1f + 0.035f * Mathf.Sin(v * 31f) + 0.02f * Mathf.Sin(v * 57f + 1f));
            const float dome = 0.16f, foot = 0.04f;
            if (v > 1f - dome) r *= Mathf.Sqrt(Mathf.Max(0f, 1f - 0.9f * Mathf.Pow((v - (1f - dome)) / dome, 2f)));
            if (v < foot) r *= 0.75f + 0.25f * v / foot;
            return r;
        }

        protected override void Draw(bool strip, bool night, float left, float right, float top, float bottom)
        {
            Background(strip, Wall);
            var l = LayoutFor(strip);
            long done = env.Done;
            float f = env.Phase;
            float dim = night ? 0.8f : 1f;
            var tint = Opaque(WorldTheme.Surface(Color.white) * dim);
            double position = env.Position;
            float spin = (float)(position * DegreesPerPoint % 360.0);
            bool cutting = f >= KebabEnvironment.CookEnd && f < KebabEnvironment.SliceEnd;
            float slicing = Mathf.Clamp01((f - KebabEnvironment.CookEnd) / (KebabEnvironment.SliceEnd - KebabEnvironment.CookEnd));
            bool active = env.Direction != 0f || ScoreEngine.Instance == null;
            float height = l.MeatY1 - l.MeatY0;

            // Fayans (yalnız kart): tezgâhtan duvarın ortasına ince derz çizgileri.
            bool card = !strip;
            var grout = Opaque(WorldTheme.Sky(Wall) * 0.9f); // duvarın (gök rengiyle aynı) tonu: Gece'de parlamasın
            float tileTop = l.CounterY + 1.9f, tileGap = (tileTop - l.CounterY) / TileRows;
            for (int i = 0; i < TileRows; i++)
            {
                tilesH[i].enabled = card;
                if (card) PlaceSpan(tilesH[i], left - 0.2f, right + 0.2f, l.CounterY + (i + 1) * tileGap - 0.015f, l.CounterY + (i + 1) * tileGap + 0.015f, grout);
            }
            for (int i = 0; i < TileCols; i++)
            {
                float x = left + (i + 0.5f) * (right - left) / TileCols;
                tilesV[i].enabled = card;
                if (card) PlaceSpan(tilesV[i], x - 0.015f, x + 0.015f, l.CounterY, tileTop, grout);
            }

            // Izgara paneli: etin solunda (arkasında), kızaran yatay rezistanslar; ışık konumdan türeyen hafif
            // titreşimle nefes alır.
            float glowBreath = 0.5f + 0.5f * Mathf.Sin((float)(position * 0.08 % 6283.0));
            float glow = (active ? 1f : 0.65f) * (0.85f + 0.15f * glowBreath);
            float pw = strip ? 0.9f : 1.45f;
            float px0 = l.MeatX - pw, px1 = l.MeatX + 0.15f;
            float py0 = l.MeatY0 - (strip ? 0.1f : 0.15f), py1 = l.MeatY1 + (strip ? 0.12f : 0.2f);
            PlaceSpan(panel, px0, px1, py0, py1, Opaque(WorldTheme.Surface(GrillBody) * dim));
            float inset = strip ? 0.07f : 0.1f;
            PlaceSpan(panelInner, px0 + inset, px1, py0 + inset, py1 - inset, Opaque(WorldTheme.Surface(GrillBody * 0.35f) * dim));
            var flameA = WorldTheme.Surface(FlameA);
            var flameB = WorldTheme.Surface(FlameB);
            var ember = WorldTheme.Surface(Ember);
            for (int i = 0; i < CoilCount; i++)
            {
                float cy = Mathf.Lerp(py0 + inset * 2.2f, py1 - inset * 2.2f, (i + 0.5f) / CoilCount);
                float flick = 0.5f + 0.5f * Mathf.Sin((float)(position * 0.21 % 6283.0) + i * 1.9f);
                var c = Color.Lerp(Color.Lerp(ember, flameA, glow), flameB, flick * 0.5f * glow);
                c.a = 1f;
                PlaceSpan(coils[i], px0 + inset * 1.8f, px1, cy - (strip ? 0.045f : 0.07f), cy + (strip ? 0.045f : 0.07f), Opaque(c * dim));
            }

            // Şiş, üst başlık ve damlama tepsisi.
            var steel = Opaque(WorldTheme.Surface(Steel) * dim);
            PlaceSpan(rod, l.MeatX - 0.04f, l.MeatX + 0.04f, l.TrayY, l.MeatY1 + (strip ? 0.25f : 0.4f), steel);
            PlaceRect(cap, l.MeatX, l.MeatY1 + (strip ? 0.16f : 0.24f), strip ? 0.42f : 0.62f, strip ? 0.06f : 0.09f, steel);
            float trayHalf = l.RTop + (strip ? 0.3f : 0.55f);
            PlaceSpan(tray, l.MeatX - trayHalf, l.MeatX + trayHalf, strip ? l.CounterY : l.CounterY, l.TrayY, Opaque(steel * 0.82f));
            PlaceSpan(trayLip, l.MeatX - trayHalf - 0.04f, l.MeatX + trayHalf + 0.04f, l.TrayY - 0.05f, l.TrayY + 0.02f, steel);

            // Usta ve bıçak (yalnız kart). Kolun açısı: kesimde Strokes kez aşağı çekiş (çekişin ilk %75'i iniş,
            // kalanı dönüş), diğer evrelerde dinlenme.
            float armAngle = ArmRest;
            if (cutting)
            {
                float s = slicing * Strokes % 1f;
                float down = s < 0.75f ? Smooth(0f, 0.75f, s) : 1f - Smooth(0.75f, 1f, s);
                armAngle = Mathf.Lerp(ArmRaised, ArmLowered, down);
            }
            else if (f < KebabEnvironment.CookEnd)
            {
                // Pişerken kol yavaşça dinlenmeye gelir (bir önceki kesimin sonundan).
                armAngle = Mathf.Lerp(ArmRaised, ArmRest, Smooth(0f, 0.08f, f));
            }
            else
            {
                armAngle = Mathf.Lerp(ArmRaised, ArmRest, Smooth(KebabEnvironment.SliceEnd, 1f, f));
            }
            bool chefOn = card && chefBodySprite != null && chefArmSprite != null;
            chefBody.enabled = chefArm.enabled = chefOn;
            float contactY = float.NaN;
            blade.enabled = false;
            if (chefOn)
            {
                // Yumruk etin kenarının biraz sağında: ustanın alt ortası buna göre (sap alt ucu dinlenmede y ≈ -0,1).
                float ppx = chefBodySprite.rect.height / ChefHeight;
                float edge = l.MeatX + RadiusAt(l, (-0.1f - l.MeatY0) / height);
                var handleRest = new Vector2(edge + 0.48f, -0.1f);
                var foot = new Vector2(handleRest.x - (HandleBottomPx.x - chefBodySprite.rect.width / 2f) / ppx,
                    handleRest.y - (chefBodySprite.rect.height - HandleBottomPx.y) / ppx);
                PlaceSprite(chefBody, chefBodySprite, foot.x, foot.y, ChefHeight, tint);
                var pivot = PixelToWorld(chefBodySprite, foot, ppx, ArmPivotPx);
                PlaceRotated(chefArm, chefArmSprite, foot, ChefHeight, pivot, armAngle, tint);

                // Bıçak: sap ekseninin uzantısı, ete doğru BladeTilt açılı; görünen kısmı etin kenarında biter (fazlası
                // etin arkasında kalır, uç etin dışına taşmaz).
                var hb = Rotate(PixelToWorld(chefBodySprite, foot, ppx, HandleBottomPx) - pivot, armAngle) + pivot;
                var ht = Rotate(PixelToWorld(chefBodySprite, foot, ppx, HandleTopPx) - pivot, armAngle) + pivot;
                var dir = Rotate((hb - ht).normalized, -BladeTilt);
                float t = BladeReach(l, hb, dir, out contactY);
                PlaceLine(blade, hb, hb + dir * (t + 0.3f), 0.12f, steel);
            }

            // Et.
            float cook = Smooth(0f, KebabEnvironment.CookEnd, f);
            double cutStart = position - (f - KebabEnvironment.CookEnd) * env.Interval;
            float shaved = f >= KebabEnvironment.CookEnd ? (float)((position - cutStart) * DegreesPerPoint) : -1f;
            RasterMeat(l, strip, cook, shaved, (float)(cutStart * DegreesPerPoint % 360.0), spin, glow, night);

            // Dilimler: kesimde çekiş başına iki dilim bıçağın değdiği yerden tepsiye düşer.
            for (int k = 0; k < SliceCount; k++)
            {
                float born = (k + 0.5f) / SliceCount;
                float age = (slicing - born) / 0.12f;
                bool on = cutting && age >= 0f && age < 1f;
                slices[k].enabled = on;
                if (!on) continue;
                float v = Mathf.Lerp(BandHigh, BandLow, (born * Strokes % 1f));
                float sy0 = l.MeatY0 + v * height;
                float sx = l.MeatX + RadiusAt(l, v) * 0.85f;
                float sy = Mathf.Lerp(sy0, l.TrayY + 0.05f, age * age);
                var c = WorldTheme.Surface(Color.Lerp(Cooked, Crisp, 0.4f)) * dim;
                c.a = 1f - Smooth(0.85f, 1f, age);
                PlaceRect(slices[k], sx - age * 0.25f, sy, strip ? 0.14f : 0.24f, strip ? 0.05f : 0.07f, c, 20f + k * 37f + age * 90f);
            }

            // Tezgâh (yalnız kart), porsiyon tabağı, pass.
            counter.enabled = counterTop.enabled = card;
            if (card)
            {
                PlaceSpan(counter, left - 0.2f, right + 0.2f, bottom - 0.2f, l.CounterY - 0.12f, Opaque(WorldTheme.Surface(Counter) * dim));
                PlaceSpan(counterTop, left - 0.2f, right + 0.2f, l.CounterY - 0.12f, l.CounterY, Opaque(WorldTheme.Surface(CounterTop) * dim));
            }
            var item = env.Identify(done + 1);
            float fill = Smooth(KebabEnvironment.CookEnd + 0.04f, KebabEnvironment.SliceEnd, f);
            float slide = Smooth(KebabEnvironment.SliceEnd, 1f, f);
            float serveH = item.Type.Size * l.ServeScale;
            float plateY = l.CounterY + 0.04f;
            float plx = Mathf.Lerp(l.PlateX, l.PassX0, slide);
            if (f < KebabEnvironment.SliceEnd)
            {
                sliding.enabled = false;
                PlaceSprite(serving, SpriteFor(item), l.PlateX, plateY - 0.02f, serveH, WithAlpha(tint, fill));
            }
            else
            {
                serving.enabled = false;
                PlaceSprite(sliding, SpriteFor(item), plx, plateY - 0.02f, serveH, tint);
            }
            for (int k = 0; k < PassSlots; k++)
            {
                long index = done - k;
                bool on = index >= 1;
                pass[k].enabled = on;
                if (!on) continue;
                var it = env.Identify(index);
                float x = l.PassX0 + (k + slide) * l.PassGap;
                float alpha = k == PassSlots - 1 ? 1f - slide : 1f;
                PlaceSprite(pass[k], SpriteFor(it), x, plateY - 0.02f, it.Type.Size * l.ServeScale, WithAlpha(tint, alpha));
            }
        }

        // Bıçak hattının etin sağ kenarına değdiği uzaklık (hb'den dir yönünde); değmezse en yakın geçiş. Kesimde
        // kenar kuşağın dışına düşerse bıçak kuşağın sınırında durur.
        float BladeReach(Layout l, Vector2 hb, Vector2 dir, out float contactY)
        {
            float height = l.MeatY1 - l.MeatY0;
            float best = 1.2f;
            contactY = float.NaN;
            for (float t = 0.05f; t < 2.6f; t += 0.02f)
            {
                var p = hb + dir * t;
                float v = (p.y - l.MeatY0) / height;
                if (v < 0f) break;
                if (p.x <= l.MeatX + RadiusAt(l, v))
                {
                    contactY = p.y;
                    return t;
                }
                best = t;
            }
            return Mathf.Min(best, 1.6f);
        }

        // Görselin pikseli (sol üstten) → dünya; görsel alt ortası foot'ta, ppx piksel/birim.
        static Vector2 PixelToWorld(Sprite s, Vector2 foot, float ppx, Vector2 px) =>
            new Vector2(foot.x + (px.x - s.rect.width / 2f) / ppx, foot.y + (s.rect.height - px.y) / ppx);

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // Gövdeyle aynı tuvaldeki kol görselini, gövdenin yerinden pivot etrafında döndürerek yerleştirir.
        void PlaceRotated(SpriteRenderer sr, Sprite sprite, Vector2 foot, float h, Vector2 pivot, float angle, Color color)
        {
            sr.sprite = sprite;
            sr.enabled = true;
            float sc = h / sprite.bounds.size.y;
            var center = new Vector2(foot.x, foot.y + h / 2f);
            var c = Rotate(center - pivot, angle) + pivot;
            sr.transform.localScale = new Vector3(sc, sc, 1f);
            sr.transform.localPosition = new Vector3(c.x, c.y, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            sr.color = color;
        }

        // Et dokusu: her satırda koninin yarıçapı; her pikselde silindir açısı a, etin kendi açısı φ = a + dönüş.
        // Yüz: kat çizgileri + kızarık lekeler (φ'ye bağlı, dönüşle kayar), soldan ızgara ışığı, sağ kenar gölgeli.
        // Kesilmiş kısım: bıçak sağ kenarda (a = +90°); kuşaktaki bir nokta kesim başladığından beri bıçağın önünden
        // geçtiyse kabuğu alınmış (açık) görünür. Kuşak dışı hep pişmiş: çevrim başında kesilen kısım pembe değil,
        // yeniden kızarmaya başlayan taze ettir.
        void RasterMeat(Layout l, bool strip, float cook, float shavedDeg, float cutStartDeg, float spin, float glow, bool night)
        {
            float rMax = Mathf.Max(l.RTop, l.RBottom) * 1.08f;
            float height = l.MeatY1 - l.MeatY0;
            int w = Mathf.CeilToInt(rMax * 2f * Ppu), h = Mathf.CeilToInt(height * Ppu);
            if (meatTex == null || texW != w || texH != h || texStrip != strip)
            {
                if (meatTex != null) Destroy(meatTex);
                texW = w;
                texH = h;
                texStrip = strip;
                meatTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                meatPixels = new Color32[w * h];
                meatSprite = Sprite.Create(meatTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), Ppu);
                meat.sprite = meatSprite;
            }
            float dim = night ? 0.82f : 1f;
            var fresh = WorldTheme.Surface(Fresh);
            var cooked = WorldTheme.Surface(Cooked);
            var crisp = WorldTheme.Surface(Crisp);
            var flame = WorldTheme.Surface(FlameA);
            float spinRad = spin * Mathf.Deg2Rad;
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h;
                float r = RadiusAt(l, v);
                bool band = v >= BandLow && v <= BandHigh;
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float dx = (x + 0.5f) / Ppu - rMax;
                    float edge = (r - Mathf.Abs(dx)) * Ppu + 0.5f;
                    if (edge <= 0f || r <= 0f)
                    {
                        meatPixels[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float xn = Mathf.Clamp(dx / r, -1f, 1f);
                    float a = Mathf.Asin(xn);
                    float phi = a + spinRad;
                    // Bu nokta bıçağın önünden geçti mi? (a = 90°'de bıçak; φ − 90° − kesim başı açısı, 0..360)
                    bool isShaved = false;
                    if (band && shavedDeg >= 0f)
                    {
                        float since = Mathf.Repeat(phi * Mathf.Rad2Deg - 90f - cutStartDeg, 360f);
                        isShaved = shavedDeg >= 360f || since <= shavedDeg;
                    }
                    float c = band ? (isShaved ? 0f : (shavedDeg >= 0f ? 1f : cook)) : 1f;
                    var col = Color.Lerp(fresh, cooked, c);
                    // Kat çizgileri ve kızarık lekeler.
                    float layers = Mathf.Sin(v * height * 46f + 1.8f * Mathf.Sin(phi * 3f));
                    col *= 1f + 0.045f * layers;
                    float blot = Blot(phi, v * height * 2.2f);
                    col = Color.Lerp(col, crisp, c * Mathf.Clamp01((blot - 0.55f) * 3f) * 0.75f);
                    // Işık: önden-soldan dağınık + soldan ızgara kızıllığı, sağa doğru gölge.
                    float nz = Mathf.Sqrt(1f - xn * xn);
                    float diffuse = Mathf.Clamp01(-0.4f * xn + 0.92f * nz);
                    col *= 0.58f + 0.5f * diffuse;
                    float rim = Mathf.Pow(Mathf.Clamp01(-xn), 3f) * 0.35f * glow;
                    col += flame * rim;
                    col *= dim;
                    meatPixels[i] = new Color(Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b), Mathf.Clamp01(edge));
                }
            }
            meatTex.SetPixels32(meatPixels);
            meatTex.Apply(false);
            meat.enabled = true;
            meat.transform.localPosition = new Vector3(l.MeatX, l.MeatY0, 0f);
            meat.transform.localRotation = Quaternion.identity;
            meat.transform.localScale = Vector3.one;
            meat.color = Color.white;
        }

        // Etin çevresinde (φ, 12 hücre — dönüşte dikişsiz) ve boyunca yumuşak değer gürültüsü: kızarık lekeler.
        static float Blot(float phi, float y)
        {
            float u = Mathf.Repeat(phi / (2f * Mathf.PI), 1f) * 12f;
            int u0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(y);
            float fu = u - u0, fy = y - y0;
            fu = fu * fu * (3f - 2f * fu);
            fy = fy * fy * (3f - 2f * fy);
            float h00 = Hash(u0 % 12, y0), h10 = Hash((u0 + 1) % 12, y0), h01 = Hash(u0 % 12, y0 + 1), h11 = Hash((u0 + 1) % 12, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(h00, h10, fu), Mathf.Lerp(h01, h11, fu), fy);
        }

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        Sprite SpriteFor(EnvItem item) => ItemSprite(item.Type.Id);
    }
}
