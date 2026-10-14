using UnityEngine;

namespace ScrollRoad
{
    // Kebap sahnesi (2026-10-14): çağ kebabı. Kart: kebapçı mutfağı (açık duvar, önde tezgâh). Yatay şiş odun
    // ateşinin (tuğla ocak, kor, kodla titreyen alevler) üstünde döner; şişin sağ ucunda dönen çark. Et kodla, her
    // karede küçük bir dokuya çizilir: koni biçimli silindir, kat çizgileri, dönüş dikey kayan bantlardan okunur;
    // piştikçe pembeden kahverengiye kızarır. Kesimde bıçak şiş boyunca soldan sağa gezer, arkasında kızarık
    // katman alınmış (açık) et kalır, dilimler düşer. Usta (tek karakter, Gemini) şişin arkasında bıçağı izler.
    // Porsiyon tezgâhtaki tabakta belirir, sonra sağdaki pass'e sürülür (son 3 porsiyon).
    //
    // Görev çubuğu şeridi: çağ kebabı şişi yatay — birebir: uzun şiş şeridin ortasında döner, altında kor çizgisi,
    // sağında son porsiyonlar.
    //
    // Sahne konumun SAF fonksiyonu (evreler KebabEnvironment'ta): geri sarınca et soğur, kesim geri alınır.
    public class KebabSceneView : IntervalSceneView
    {
        [SerializeField] KebabEnvironment env;
        [SerializeField] Sprite chefSprite;
        [SerializeField] Sprite[] commonSprites;
        [SerializeField] Sprite[] specialSprites;

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "KebabHouse";

        const int TexW = 192, TexH = 48;
        const float DegreesPerPoint = 9f; // şiş: seyirde ~0,4 tur/sn (çağ kebabı yavaş döner)
        const int FlameCount = 7;
        const int SliceCount = 8;
        const int PassSlots = 3;

        static readonly Color Wall = new Color(0.95f, 0.91f, 0.84f);
        static readonly Color Brick = new Color(0.66f, 0.34f, 0.24f);
        static readonly Color Ember = new Color(1f, 0.48f, 0.16f);
        static readonly Color FlameA = new Color(1f, 0.62f, 0.15f);
        static readonly Color FlameB = new Color(1f, 0.86f, 0.35f);
        static readonly Color Raw = new Color(0.84f, 0.50f, 0.46f);
        static readonly Color Cooked = new Color(0.55f, 0.30f, 0.16f);
        static readonly Color Fresh = new Color(0.76f, 0.52f, 0.36f);
        static readonly Color Steel = new Color(0.78f, 0.80f, 0.83f);
        static readonly Color Counter = new Color(0.45f, 0.47f, 0.50f);
        static readonly Color CounterTop = new Color(0.72f, 0.74f, 0.76f);

        Texture2D meatTex;
        Sprite meatSprite;
        Color32[] meatPixels;

        SpriteRenderer firebox, embers, chef, rod, meat, gear, gearHub, knifeBlade, knifeHandle, counter, counterTop, serving, sliding;
        SpriteRenderer[] flames, spokes, slices, pass;

        struct Layout
        {
            public float SpitY, MeatX0, MeatX1, Radius0, Radius1, CounterY, PlateX, PassX0, PassGap, ServeScale;
        }

        protected override void Build()
        {
            meatTex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            meatPixels = new Color32[TexW * TexH];
            meatSprite = Sprite.Create(meatTex, new Rect(0, 0, TexW, TexH), new Vector2(0f, 0.5f), TexH);

            chef = Create("Chef", chefSprite != null ? chefSprite : whiteSprite, 3);
            firebox = Create("Firebox", whiteSprite, 4);
            embers = Create("Embers", whiteSprite, 5);
            flames = CreatePool("Flame", circleSprite, 6, FlameCount);
            rod = Create("Rod", whiteSprite, 8);
            meat = Create("Meat", meatSprite, 9);
            gear = Create("Gear", circleSprite, 9);
            spokes = CreatePool("GearSpoke", whiteSprite, 10, 2);
            gearHub = Create("GearHub", circleSprite, 11);
            slices = CreatePool("Slice", whiteSprite, 12, SliceCount);
            knifeBlade = Create("KnifeBlade", whiteSprite, 13);
            knifeHandle = Create("KnifeHandle", whiteSprite, 13);
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
            anchorWorldX = 0f;      // şişin sol ucu
            anchorScreenX = 0.35f;
        }

        static Layout LayoutFor(bool strip) => strip
            ? new Layout { SpitY = 1.15f, MeatX0 = 0f, MeatX1 = 8.5f, Radius0 = 0.62f, Radius1 = 0.48f, CounterY = 0f, PlateX = 10.0f, PassX0 = 11.6f, PassGap = 1.6f, ServeScale = 1.6f }
            : new Layout { SpitY = -0.1f, MeatX0 = -5.0f, MeatX1 = -0.4f, Radius0 = 0.82f, Radius1 = 0.64f, CounterY = -2.45f, PlateX = 1.8f, PassX0 = 3.5f, PassGap = 1.5f, ServeScale = 1.8f };

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
            float slicing = Mathf.Clamp01((f - KebabEnvironment.CookEnd) / (KebabEnvironment.SliceEnd - KebabEnvironment.CookEnd));
            bool cutting = f >= KebabEnvironment.CookEnd && f < KebabEnvironment.SliceEnd;
            float knifeX = Mathf.Lerp(l.MeatX0 + 0.15f, l.MeatX1 - 0.1f, slicing);

            // Ocak: tuğla gövde + kor çizgisi + alevler (alevin boyu konumdan türeyen titreşimle).
            float fireY0 = strip ? 0f : l.CounterY + 0.55f, fireY1 = strip ? 0.22f : l.SpitY - 1.2f;
            firebox.enabled = !strip;
            if (!strip) PlaceSpan(firebox, l.MeatX0 - 0.4f, l.MeatX1 + 0.4f, fireY0, fireY1, Opaque(WorldTheme.Surface(Brick) * dim));
            var emberC = WorldTheme.Surface(Ember);
            emberC.a = strip ? 0.9f : 1f;
            PlaceSpan(embers, l.MeatX0 - 0.3f, l.MeatX1 + 0.3f, fireY1 - (strip ? 0.12f : 0.1f), fireY1, emberC);
            bool active = env.Direction != 0f || ScoreEngine.Instance == null;
            for (int i = 0; i < FlameCount; i++)
            {
                float fx = Mathf.Lerp(l.MeatX0 + 0.1f, l.MeatX1 - 0.1f, (i + 0.5f) / FlameCount);
                float flick = 0.5f + 0.5f * Mathf.Sin((float)(position * 0.35 % 6283.0) + i * 1.7f);
                float fh = (strip ? 0.32f : 0.5f) * (0.6f + 0.4f * flick) * (active ? 1f : 0.7f);
                var c = Color.Lerp(WorldTheme.Surface(FlameA), WorldTheme.Surface(FlameB), flick);
                c.a = 0.85f;
                PlaceRect(flames[i], fx, fireY1 + fh * 0.4f, fh * 0.55f, fh, c);
            }

            // Usta (yalnız kart): şişin arkasında, kesimde bıçağı izler.
            chef.enabled = !strip && chefSprite != null;
            if (chef.enabled)
            {
                float restX = l.MeatX1 + 1.3f;
                float cx = cutting ? Mathf.Lerp(restX, knifeX + 1.0f, Smooth(0f, 0.15f, slicing) * (1f - Smooth(0.85f, 1f, slicing))) : restX;
                PlaceSprite(chef, chefSprite, cx, l.CounterY - 0.75f, 3.6f, tint);
            }

            // Şiş çubuğu + çark.
            var steel = Opaque(WorldTheme.Surface(Steel) * dim);
            PlaceSpan(rod, l.MeatX0 - 0.5f, l.MeatX1 + 0.75f, l.SpitY - 0.04f, l.SpitY + 0.04f, steel);
            float gx = l.MeatX1 + 0.75f, gr = strip ? 0.3f : 0.36f;
            PlaceRect(gear, gx, l.SpitY, gr * 2f, gr * 2f, Opaque(WorldTheme.Surface(new Color(0.30f, 0.31f, 0.34f)) * dim));
            for (int k = 0; k < spokes.Length; k++)
                PlaceRect(spokes[k], gx, l.SpitY, gr * 1.7f, 0.06f, steel, -spin + k * 90f);
            PlaceRect(gearHub, gx, l.SpitY, gr * 0.4f, gr * 0.4f, steel);

            // Et.
            float cook = Smooth(0f, KebabEnvironment.CookEnd, f);
            RasterMeat(l, cook, f >= KebabEnvironment.CookEnd, knifeX, spin, night);
            float length = l.MeatX1 - l.MeatX0;
            float maxR = Mathf.Max(l.Radius0, l.Radius1);
            meat.transform.localPosition = new Vector3(l.MeatX0, l.SpitY, 0f);
            meat.transform.localRotation = Quaternion.identity;
            meat.transform.localScale = new Vector3(length / (TexW / (float)TexH), maxR * 2f, 1f);
            meat.color = Color.white;

            // Bıçak ve dilimler (yalnız kesimde).
            knifeBlade.enabled = knifeHandle.enabled = cutting;
            if (cutting)
            {
                float r = Mathf.Lerp(l.Radius0, l.Radius1, Mathf.InverseLerp(l.MeatX0, l.MeatX1, knifeX));
                PlaceRect(knifeBlade, knifeX + 0.08f, l.SpitY + 0.05f, 0.07f, r * 2.4f, steel, 12f);
                PlaceRect(knifeHandle, knifeX + 0.08f - 0.2f, l.SpitY + r * 1.2f + 0.25f, 0.1f, 0.42f, Opaque(WorldTheme.Surface(new Color(0.25f, 0.17f, 0.12f)) * dim), 12f);
            }
            for (int k = 0; k < SliceCount; k++)
            {
                float born = (k + 0.5f) / SliceCount;
                float age = (slicing - born) / 0.25f;
                bool on = cutting && age >= 0f && age < 1f;
                slices[k].enabled = on;
                if (!on) continue;
                float sx = Mathf.Lerp(l.MeatX0 + 0.15f, l.MeatX1 - 0.1f, born) + 0.12f;
                float r = Mathf.Lerp(l.Radius0, l.Radius1, born);
                float sy = l.SpitY - r - age * (strip ? 0.6f : 0.85f);
                var c = WorldTheme.Surface(Cooked) * dim;
                c.a = 1f - age;
                PlaceRect(slices[k], sx, sy, strip ? 0.16f : 0.22f, 0.06f, c, 20f + k * 23f);
            }

            // Tezgâh (yalnız kart), porsiyon tabağı, pass.
            counter.enabled = counterTop.enabled = !strip;
            if (!strip)
            {
                PlaceSpan(counter, left - 0.2f, right + 0.2f, bottom - 0.2f, l.CounterY - 0.12f, Opaque(WorldTheme.Surface(Counter) * dim));
                PlaceSpan(counterTop, left - 0.2f, right + 0.2f, l.CounterY - 0.12f, l.CounterY, Opaque(WorldTheme.Surface(CounterTop) * dim));
            }
            var item = env.Identify(done + 1);
            float fill = Smooth(KebabEnvironment.CookEnd + 0.04f, KebabEnvironment.SliceEnd, f);
            float slide = Smooth(KebabEnvironment.SliceEnd, 1f, f);
            float serveH = item.Type.Size * l.ServeScale;
            float plateY = l.CounterY + 0.04f;
            float px = Mathf.Lerp(l.PlateX, l.PassX0, slide);
            if (f < KebabEnvironment.SliceEnd)
            {
                sliding.enabled = false;
                PlaceSprite(serving, SpriteFor(item), l.PlateX, plateY - 0.02f, serveH, WithAlpha(tint, fill));
            }
            else
            {
                serving.enabled = false;
                PlaceSprite(sliding, SpriteFor(item), px, plateY - 0.02f, serveH, tint);
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

        // Et dokusu: koni (solda kalın), soldan sağa x; y kesitinde silindir gölgesi (üst açık), dönen dikey bantlar,
        // eksen boyunca kat çizgileri. Kızarıklık: bıçağın solunda (kesilmiş) taze kat, sağında pişmiş renk.
        void RasterMeat(Layout l, float cook, bool afterCook, float knifeX, float spin, bool night)
        {
            float length = l.MeatX1 - l.MeatX0;
            float maxR = Mathf.Max(l.Radius0, l.Radius1);
            float spinRad = spin * Mathf.Deg2Rad;
            float dim = night ? 0.82f : 1f;
            var raw = WorldTheme.Surface(Raw);
            var cooked = WorldTheme.Surface(Cooked);
            var fresh = WorldTheme.Surface(Fresh);
            for (int x = 0; x < TexW; x++)
            {
                float u = (x + 0.5f) / TexW;
                float wx = l.MeatX0 + u * length;
                float r = Mathf.Lerp(l.Radius0, l.Radius1, u) * (0.94f + 0.06f * Mathf.Sin(u * 23f)); // hafif yumru
                // Uçlar yuvarlatılır.
                float end = Mathf.Min(u, 1f - u) * length;
                if (end < r * 0.35f) r *= Mathf.Sqrt(Mathf.Clamp01(end / (r * 0.35f)));
                float rn = r / maxR;
                // Bu x'in kızarıklığı: kesimde bıçağın solu taze, sonrası (f ≥ 0,9) tümü taze.
                Color surface;
                if (afterCook && wx < knifeX) surface = fresh;
                else surface = Color.Lerp(Color.Lerp(raw, fresh, 0.5f), cooked, cook);
                for (int y = 0; y < TexH; y++)
                {
                    int i = y * TexW + x;
                    float yn = ((y + 0.5f) / TexH * 2f - 1f) / Mathf.Max(0.01f, rn); // −1..1 kesit içinde
                    float a = Mathf.Clamp01((1f - Mathf.Abs(yn)) * rn * TexH * 0.5f + 0.5f);
                    if (a <= 0f)
                    {
                        meatPixels[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float yc = Mathf.Clamp(yn, -1f, 1f);
                    float light = 0.8f + 0.25f * yc;                                    // üst açık
                    light += 0.07f * Mathf.Sin(Mathf.Asin(yc) * 6f + spinRad);          // dönen bantlar
                    light += 0.04f * Mathf.Sin(u * length * 26f + yc * 2f);            // kat çizgileri
                    var c = surface * light * dim;
                    meatPixels[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), a);
                }
            }
            meatTex.SetPixels32(meatPixels);
            meatTex.Apply(false);
        }

        Sprite SpriteFor(EnvItem item)
        {
            var site = env.Site;
            if (item.IsSpecial)
            {
                int si = site.SpecialIndex(item.Type.Id);
                return specialSprites != null && si >= 0 && si < specialSprites.Length ? specialSprites[si] : null;
            }
            int ci = site.CommonIndex(item.Type);
            return commonSprites != null && ci >= 0 && ci < commonSprites.Length ? commonSprites[ci] : null;
        }
    }
}
