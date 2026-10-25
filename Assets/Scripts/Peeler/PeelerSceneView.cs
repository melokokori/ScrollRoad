using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Meyve soyacağının sahnesi (2026-10-14). Kart: manav tezgâhı (açık duvar, ahşap masa). Masada dökme demir bir
    // soyma makinesi (kodla): taban, meyveyi tutan mil, solda kollu çark (kol döner), sağda dikey ray üstünde
    // aşağı inen bıçak. Meyve (Gemini) milde döner; bıçağın üstünde kalan kısım soyulmuş (meyve eti) görünür —
    // et görseli Tools/gen_peeler.py'nin aynı meyveden türettiği "<id>_flesh.png", bıçak çizgisinden yukarısı
    // kırpılarak çizilir. Kabuk bıçaktan tek parça, kıvrılarak masaya sarkar ve masada uzar. Soyulan meyve
    // sağdaki kâseye düşer (kâsede son 3 meyve).
    //
    // Görev çubuğu şeridi (yatay yorum): makine solda yazının yanında, kabuk sağa doğru uzun bir şerit gibi
    // dalgalanarak uzar — "kopmayan kabuk" şeridin boyunca okunur.
    //
    // Sahne konumun SAF fonksiyonu (evreler PeelerEnvironment'ta); geri sarınca kabuk meyveye geri sarılır.
    public class PeelerSceneView : IntervalSceneView
    {
        [SerializeField] PeelerEnvironment env;

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "FruitStall";

        const float DegreesPerPoint = 30f; // kol: seyirde ~1,4 tur/sn
        const int CropSteps = 32;
        const int RibbonMax = 90;
        const int SpeckCount = 3;
        const int BowlSlots = 3;

        static readonly Color Wall = new Color(0.95f, 0.92f, 0.84f);
        static readonly Color TableTop = new Color(0.66f, 0.46f, 0.30f);
        static readonly Color TableFront = new Color(0.52f, 0.35f, 0.22f);
        static readonly Color Iron = new Color(0.27f, 0.29f, 0.33f);
        static readonly Color Handle = new Color(0.72f, 0.26f, 0.20f);
        static readonly Color Steel = new Color(0.80f, 0.82f, 0.85f);
        static readonly Color BowlWood = new Color(0.60f, 0.40f, 0.25f);

        readonly Dictionary<long, Sprite> cropCache = new Dictionary<long, Sprite>();
        Sprite halfDiscSprite;

        SpriteRenderer tableTop, tableFront, machineBase, spindle, axle, crank, crankHub, knob, rail, carriage, blade,
            fruit, flesh, dropping, bowlBack, bowlFront;
        SpriteRenderer[] spokes, specks, ribbon, bowlFruit;

        struct Layout
        {
            public float TableY, FruitX, SpindleTop, FruitScale, CrankR, BowlX, BowlScale, MetersToUnits, Thick;
        }

        protected override void Build()
        {
            halfDiscSprite = CreateHalfDisc(128);
            tableTop = Create("TableTop", whiteSprite, 2);
            tableFront = Create("TableFront", whiteSprite, 3);
            bowlBack = Create("BowlBack", circleSprite, 4);
            bowlFruit = CreatePool("BowlFruit", whiteSprite, 5, BowlSlots);
            bowlFront = Create("BowlFront", halfDiscSprite, 6);
            ribbon = CreatePool("Peel", whiteSprite, 8, RibbonMax);
            axle = Create("Axle", whiteSprite, 9);
            machineBase = Create("Base", whiteSprite, 10);
            spindle = Create("Spindle", whiteSprite, 10);
            crank = Create("Crank", circleSprite, 11);
            spokes = CreatePool("Spoke", whiteSprite, 12, 2);
            crankHub = Create("Hub", circleSprite, 13);
            knob = Create("Knob", circleSprite, 13);
            fruit = Create("Fruit", whiteSprite, 14);
            flesh = Create("Flesh", whiteSprite, 15);
            specks = CreatePool("Speck", circleSprite, 16, SpeckCount);
            rail = Create("Rail", whiteSprite, 17);
            carriage = Create("Carriage", whiteSprite, 18);
            blade = Create("Blade", whiteSprite, 18);
            dropping = Create("Dropping", whiteSprite, 20);
        }

        protected override void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX)
        {
            bottom = -0.2f;
            top = 2.6f;
            anchorWorldX = 0f;      // meyve
            anchorScreenX = 0.38f;
        }

        static Layout LayoutFor(bool strip) => strip
            ? new Layout { TableY = 0f, FruitX = 0f, SpindleTop = 0.3f, FruitScale = 1.5f, CrankR = 0.34f, BowlX = 99f, BowlScale = 0f, MetersToUnits = 3.2f, Thick = 0.08f }
            : new Layout { TableY = -2.0f, FruitX = -1.8f, SpindleTop = -1.0f, FruitScale = 1.75f, CrankR = 0.55f, BowlX = 4.0f, BowlScale = 1.1f, MetersToUnits = 2.4f, Thick = 0.1f };

        protected override void Draw(bool strip, bool night, float left, float right, float top, float bottom)
        {
            Background(strip, Wall);
            var l = LayoutFor(strip);
            long done = env.Done;
            float f = env.Phase;
            float dim = night ? 0.8f : 1f;
            var tint = Opaque(WorldTheme.Surface(Color.white) * dim);
            float crankDeg = (float)(env.Position * DegreesPerPoint % 360.0);

            // Masa.
            tableFront.enabled = !strip;
            if (!strip)
            {
                PlaceSpan(tableTop, left - 0.2f, right + 0.2f, l.TableY - 0.35f, l.TableY, Opaque(WorldTheme.Surface(TableTop) * dim));
                PlaceSpan(tableFront, left - 0.2f, right + 0.2f, bottom - 0.2f, l.TableY - 0.35f, Opaque(WorldTheme.Surface(TableFront) * dim));
            }
            else
            {
                var c = WorldTheme.Surface(TableTop);
                c.a = 0.95f;
                PlaceSpan(tableTop, left + 3.2f, right + 0.2f, l.TableY - 0.1f, l.TableY, c);
            }

            // Bu meyve.
            var item = env.Identify(done + 1);
            var whole = SpriteFor(item, false);
            var fleshSprite = SpriteFor(item, true);
            float h = item.Type.Size * l.FruitScale;
            float w = WidthAt(whole, h);
            float mount = Smooth(0f, PeelerEnvironment.MountEnd, f);
            float p = Mathf.Clamp01((f - PeelerEnvironment.MountEnd) / (PeelerEnvironment.PeelEnd - PeelerEnvironment.MountEnd));
            bool dropPhase = f >= PeelerEnvironment.PeelEnd;
            float fruitBottom = l.SpindleTop + (1f - mount) * (strip ? 0.8f : 1.4f);
            float topKeep = item.Type.Shape != null && item.Type.Shape.Length > 0 ? item.Type.Shape[0] : 0f;
            float bodyTop = 1f - topKeep;              // görselin altından oran
            float bladeFrac = bodyTop * (1f - p);      // bıçağın görseldeki yüksekliği (alttan)
            float bladeY = fruitBottom + bladeFrac * h;

            // Makine (meyveden bağımsız boyutlar).
            float s = strip ? 0.85f : 1.3f;
            var iron = Opaque(WorldTheme.Surface(Iron) * dim);
            PlaceSpan(machineBase, l.FruitX - 0.5f * s, l.FruitX + 0.5f * s, l.TableY, l.TableY + 0.32f * s, iron);
            PlaceSpan(spindle, l.FruitX - 0.035f, l.FruitX + 0.035f, l.TableY + 0.3f * s, l.SpindleTop + 0.15f, Opaque(WorldTheme.Surface(Steel) * dim));
            var crankC = new Vector2(l.FruitX - 1.5f * s, l.TableY + 0.72f * s);
            PlaceSpan(axle, crankC.x, l.FruitX - 0.4f * s, crankC.y - 0.05f * s, crankC.y + 0.05f * s, iron);
            PlaceRect(crank, crankC.x, crankC.y, l.CrankR * 2f, l.CrankR * 2f, iron);
            for (int k = 0; k < spokes.Length; k++)
                PlaceRect(spokes[k], crankC.x, crankC.y, l.CrankR * 1.7f, 0.06f * s, Opaque(WorldTheme.Surface(Steel) * dim * 0.85f), crankDeg + k * 90f);
            PlaceRect(crankHub, crankC.x, crankC.y, 0.14f * s, 0.14f * s, Opaque(WorldTheme.Surface(Steel) * dim));
            float kr = crankDeg * Mathf.Deg2Rad;
            PlaceRect(knob, crankC.x + Mathf.Cos(kr) * l.CrankR * 0.82f, crankC.y + Mathf.Sin(kr) * l.CrankR * 0.82f, 0.17f * s, 0.17f * s, Opaque(WorldTheme.Surface(Handle) * dim));

            // Meyve: bütün hâli + bıçağın üstünde soyulmuş et; düşerken yalnız et.
            float halfW = w / 2f;
            float railX = l.FruitX + halfW + 0.3f * s;
            if (!dropPhase)
            {
                dropping.enabled = false;
                PlaceSprite(fruit, whole, l.FruitX, fruitBottom, h, WithAlpha(tint, mount));
                DrawFleshCrop(item, fleshSprite, l.FruitX, fruitBottom, h, bladeFrac, WithAlpha(tint, mount), p > 0f);
            }
            else
            {
                fruit.enabled = false;
                flesh.enabled = false;
                float t = Smooth(PeelerEnvironment.PeelEnd, 1f, f);
                var a = new Vector2(l.FruitX, fruitBottom);
                var b = strip ? new Vector2(l.FruitX + 1.2f, l.TableY - 1.5f) : new Vector2(l.BowlX - 0.9f, l.TableY + 0.22f);
                var ctrl = new Vector2((a.x + b.x) / 2f, a.y + (strip ? 0.4f : 1.0f));
                float u = 1f - t;
                var pos = u * u * a + 2f * u * t * ctrl + t * t * b;
                float hh = Mathf.Lerp(h, item.Type.Size * (strip ? 1f : l.BowlScale), t);
                PlaceSprite(dropping, fleshSprite, pos.x, pos.y, hh, WithAlpha(tint, strip ? 1f - t : 1f));
            }

            // Bıçak rayı + taşıyıcı + bıçak (yalnız soyarken meyveye değer).
            PlaceSpan(rail, railX - 0.03f, railX + 0.03f, l.TableY, l.SpindleTop + bodyTop * h + 0.2f, iron);
            float contactY = dropPhase ? l.SpindleTop + 0.05f : bladeY;
            float contactHalf = halfW * Mathf.Sqrt(Mathf.Max(0.05f, 1f - Mathf.Pow((contactY - (fruitBottom + bodyTop * h * 0.5f)) / Mathf.Max(0.1f, bodyTop * h * 0.5f), 2f)));
            float contactX = l.FruitX + contactHalf * 0.92f;
            PlaceRect(carriage, railX, contactY, 0.2f * s, 0.14f * s, iron);
            PlaceLine(blade, new Vector2(railX - 0.08f * s, contactY), new Vector2(contactX, contactY), 0.045f * s, Opaque(WorldTheme.Surface(Steel) * dim));

            // Dönüş: kabuktaki birkaç benek yatay kayar (soyulmamış kısımda).
            for (int k = 0; k < SpeckCount; k++)
            {
                float a = (crankDeg * 1.3f + k * 120f) * Mathf.Deg2Rad;
                float depth = Mathf.Cos(a);
                float sy = fruitBottom + bladeFrac * h * (0.25f + 0.25f * k);
                bool on = !dropPhase && depth > 0.1f && sy < bladeY - 0.05f && mount > 0.9f;
                specks[k].enabled = on;
                if (!on) continue;
                float sx = l.FruitX + Mathf.Sin(a) * halfW * 0.7f;
                var c = Color.Lerp(item.Type.Color, Color.black, 0.35f);
                c.a = 0.55f * depth;
                PlaceRect(specks[k], sx, sy, 0.07f * s * (0.5f + 0.5f * depth), 0.07f * s, c);
            }

            // Kabuk.
            float meters = PeelerEnvironment.PeelLength(item) / 100f;
            float length = meters * l.MetersToUnits * p;
            float ribbonAlpha = dropPhase ? 1f - Smooth(PeelerEnvironment.PeelEnd, 1f, f) : 1f;
            float fallY = dropPhase ? Smooth(PeelerEnvironment.PeelEnd, 1f, f) * 0.3f : 0f;
            DrawRibbon(strip, l, item, new Vector2(contactX, contactY - fallY), length, crankDeg * Mathf.Deg2Rad, ribbonAlpha, dim, left);

            // Kâse (yalnız kart): son 3 meyve (yeni düşen önde); düşüş sırasında bir yuva kayar.
            bowlBack.enabled = bowlFront.enabled = !strip;
            if (!strip)
            {
                var wood = Opaque(WorldTheme.Surface(BowlWood) * dim);
                PlaceRect(bowlBack, l.BowlX, l.TableY + 0.65f, 3.1f, 0.5f, Opaque(wood * 0.7f));
                PlaceRect(bowlFront, l.BowlX, l.TableY + 0.65f, 3.2f, 1.9f, wood);
                float shift = dropPhase ? Smooth(PeelerEnvironment.PeelEnd, 1f, f) : 0f;
                for (int k = 0; k < BowlSlots; k++)
                {
                    long index = done - k;
                    if (index < 1)
                    {
                        bowlFruit[k].enabled = false;
                        continue;
                    }
                    var it = env.Identify(index);
                    float slot = k + shift;
                    float x = l.BowlX - 0.9f + slot * 0.9f;
                    float alpha = k == BowlSlots - 1 ? 1f - shift : 1f;
                    PlaceSprite(bowlFruit[k], SpriteFor(it, true), x, l.TableY + 0.22f, it.Type.Size * l.BowlScale, WithAlpha(tint, alpha));
                    bowlFruit[k].sortingOrder = 5;
                }
            }
            else
            {
                foreach (var b in bowlFruit) b.enabled = false;
            }
        }

        // Et görseli: bıçak çizgisinden görselin üstüne kadar kırpılmış sprite (önbellekte, CropSteps adım).
        void DrawFleshCrop(EnvItem item, Sprite source, float x, float bottomY, float h, float bladeFrac, Color color, bool any)
        {
            if (source == null || !any)
            {
                flesh.enabled = false;
                return;
            }
            int step = Mathf.Clamp(Mathf.CeilToInt((1f - bladeFrac) * CropSteps), 1, CropSteps);
            float keepFrac = step / (float)CropSteps;
            long key = ((long)source.GetInstanceID() << 8) | (uint)step;
            if (!cropCache.TryGetValue(key, out var crop))
            {
                var r = source.rect;
                float ch = Mathf.Max(1f, Mathf.Round(r.height * keepFrac));
                crop = Sprite.Create(source.texture, new Rect(r.x, r.y + r.height - ch, r.width, ch), new Vector2(0.5f, 0f), source.pixelsPerUnit);
                cropCache[key] = crop;
            }
            flesh.sprite = crop;
            flesh.enabled = true;
            float sc = h / source.bounds.size.y;
            flesh.transform.localScale = new Vector3(sc, sc, 1f);
            flesh.transform.localRotation = Quaternion.identity;
            flesh.transform.localPosition = new Vector3(x, bottomY + (1f - keepFrac) * h, 0f);
            flesh.color = color;
        }

        // Kabuk şeridi: kartta bıçaktan masaya kıvrılarak sarkar, sonra masada sağa uzar; şeritte sağa doğru
        // dalgalanan uzun bir şerit. Dış yüz kabuk rengi, iç yüz açık — sarmalın dönüşü renk değişiminden okunur.
        void DrawRibbon(bool strip, Layout l, EnvItem item, Vector2 start, float length, float twist, float alpha, float dim, float left)
        {
            int used = 0;
            if (length > 0.02f && alpha > 0.01f)
            {
                float step = Mathf.Max(strip ? 0.09f : 0.06f, length / RibbonMax);
                var outer = WorldTheme.Surface(item.Type.Color) * dim;
                var inner = WorldTheme.Surface(Color.Lerp(item.Type.Color, item.Type.Color2, 0.65f)) * dim;
                Vector2 prev = start;
                float hang = start.y - (l.TableY + 0.04f);
                for (float sArc = step; sArc <= length + 1e-4f && used < RibbonMax; sArc += step)
                {
                    Vector2 pnt;
                    float wave = Mathf.Sin(sArc * (strip ? 2.6f : 7f) - twist);
                    if (strip)
                    {
                        float droop = Mathf.Min(start.y - 0.08f, 0.28f * sArc);
                        pnt = new Vector2(start.x + 0.08f + sArc, Mathf.Max(0.06f, start.y - droop + 0.13f * wave));
                    }
                    else if (sArc <= hang)
                    {
                        pnt = new Vector2(start.x + 0.14f + 0.13f * wave, start.y - sArc);
                    }
                    else
                    {
                        float run = sArc - hang;
                        pnt = new Vector2(start.x + 0.14f + run * 0.92f, l.TableY + 0.04f + 0.05f * Mathf.Abs(Mathf.Sin(run * 5f)));
                    }
                    var c = Color.Lerp(inner, outer, 0.5f + 0.5f * wave);
                    c.a = alpha;
                    if (strip) c.a *= Mathf.Clamp01((pnt.x - (left + 3.2f)) / 1f);
                    PlaceLine(ribbon[used++], prev, pnt, l.Thick * (0.75f + 0.25f * Mathf.Abs(wave)), c);
                    prev = pnt;
                }
            }
            for (int k = used; k < RibbonMax; k++) ribbon[k].enabled = false;
        }

        Sprite SpriteFor(EnvItem item, bool peeled) => ItemSprite(peeled ? item.Type.Id + "_flesh" : item.Type.Id);

        // Kâsenin ön yüzü: dairenin alt yarısı (pivot üst kenarın ortası değil, merkez — yerleşim ona göre).
        static Sprite CreateHalfDisc(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size / 2f - 1f;
            var c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = y > c.y ? 0f : Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), c) + 0.5f) * Mathf.Clamp01(c.y - y + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
