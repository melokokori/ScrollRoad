using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Meyve soyacağının sahnesi (2026-10-25 yeniden: elle soyan manav; ilk hâli 2026-10-14 kollu soyma makinesiydi).
    // Kart: manav dükkânı (açık duvar, ahşap zemin). Solda üst üste meyve kasaları (kodla; içlerinde mekânın sıradan
    // meyveleri). Ortada taburede oturan manav (Gemini, Tools/gen_peeler.py --split ile iki katman): meyve avucunda,
    // bıçağı tutan eli meyvenin ÖNÜNDE. Meyve kasadan avucuna gelir, avuçta döner (kabuktaki benekler kayar); bıçak
    // (kodla) çimdikleyen elden meyvenin sağ kenarına uzanır ve yukarıdan aşağı iner. Bıçağın üstünde kalan kısım
    // soyulmuş (meyve eti) görünür — et görseli aynı meyveden türetilmiş "<id>_flesh.png", bıçak çizgisinden yukarısı
    // kırpılarak çizilir. Kabuk bıçaktan tek parça, kıvrılarak yere sarkar ve yerde sağa uzar (uzunsa katlanarak
    // yığılır). Soyulan meyve sağdaki sandık üstündeki kâseye atılır (kâsede son 3 meyve).
    //
    // Görev çubuğu şeridi (yatay yorum): meyve ve bıçak yazının yanında, kabuk sağa doğru uzun bir şerit gibi
    // dalgalanarak uzar — "kopmayan kabuk" şeridin boyunca okunur; manav, kasa ve kâse yok.
    //
    // Sahne konumun SAF fonksiyonu (evreler PeelerEnvironment'ta); geri sarınca kabuk meyveye geri sarılır.
    public class PeelerSceneView : IntervalSceneView
    {
        [SerializeField] PeelerEnvironment env;
        [SerializeField] Sprite grocerBodySprite;
        [SerializeField] Sprite grocerHandSprite;

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "FruitStall";

        const float DegreesPerPoint = 30f; // meyvenin avuçta dönüşü: seyirde ~1,4 tur/sn
        const int CropSteps = 32;
        const int RibbonMax = 110;
        const int SpeckCount = 3;
        const int BowlSlots = 3;
        const int Crates = 3, CrateFruit = 3, CrateSlats = 2;

        // Manav görselinde (336x512, sol üstten piksel) meyvenin oturduğu avuç ve bıçağı tutan parmak ucu.
        static readonly Vector2 PalmPx = new Vector2(308f, 214f);
        static readonly Vector2 PinchPx = new Vector2(262f, 153f);
        const float GrocerHeight = 3.2f;

        // Ortam Standardı v2: mekân duvar/zemin üstü/zemin/kâseyi (ItemSite.Palette 0-3), skin takımı bıçağı boyar
        // (SkinInfo.Palette 0-2: bilezik, sap, ağız). Varsayılanlar Meyve Tezgâhı ve Klasik.
        static readonly Color WallDefault = new Color(0.95f, 0.92f, 0.84f);
        static readonly Color FloorTopDefault = new Color(0.66f, 0.46f, 0.30f);
        static readonly Color FloorDefault = new Color(0.52f, 0.35f, 0.22f);
        static readonly Color BowlWoodDefault = new Color(0.60f, 0.40f, 0.25f);
        static readonly Color BolsterDefault = new Color(0.27f, 0.29f, 0.33f);
        static readonly Color HandleDefault = new Color(0.72f, 0.26f, 0.20f);
        static readonly Color SteelDefault = new Color(0.80f, 0.82f, 0.85f);
        static readonly Color CrateWood = new Color(0.80f, 0.62f, 0.40f);
        Color Wall => Look(0, WallDefault);
        Color FloorTop => Look(1, FloorTopDefault);
        Color Floor => Look(2, FloorDefault);
        Color BowlWood => Look(3, BowlWoodDefault);
        Color Bolster => Tint(0, BolsterDefault);
        Color Handle => Tint(1, HandleDefault);
        Color Steel => Tint(2, SteelDefault);

        readonly Dictionary<long, Sprite> cropCache = new Dictionary<long, Sprite>();
        Sprite halfDiscSprite;

        SpriteRenderer floorTop, floor, grocerBody, grocerHand, knifeHandle, knifeBolster, blade, fruit, flesh, dropping, bowlBack, bowlFront;
        SpriteRenderer[] specks, ribbon, bowlFruit, crateFronts, crateSlats, crateFruit, standSlats;
        SpriteRenderer stand;

        struct Layout
        {
            public float FloorY, FruitX, FruitBottom, FruitScale, PinchX, PinchY, BowlX, BowlY, BowlScale, MetersToUnits, Thick, RunEnd;
        }

        // Kasaların yeri (kart): x0, x1, alt y; kasa yüksekliği CrateH.
        static readonly Vector3[] CrateBoxes = { new Vector3(-7.15f, -5.4f, 0f), new Vector3(-5.25f, -3.5f, 0f), new Vector3(-6.25f, -4.45f, 1f) };
        const float CrateH = 0.85f;

        protected override void Build()
        {
            halfDiscSprite = CreateHalfDisc(128);
            floorTop = Create("FloorTop", whiteSprite, 2);
            floor = Create("Floor", whiteSprite, 3);
            crateFruit = CreatePool("CrateFruit", whiteSprite, 4, Crates * CrateFruit);
            crateFronts = CreatePool("Crate", whiteSprite, 5, Crates);
            crateSlats = CreatePool("CrateSlat", whiteSprite, 6, Crates * CrateSlats);
            stand = Create("Stand", whiteSprite, 5);
            standSlats = CreatePool("StandSlat", whiteSprite, 6, CrateSlats);
            bowlBack = Create("BowlBack", circleSprite, 7);
            bowlFruit = CreatePool("BowlFruit", whiteSprite, 8, BowlSlots);
            bowlFront = Create("BowlFront", halfDiscSprite, 9);
            ribbon = CreatePool("Peel", whiteSprite, 10, RibbonMax);
            grocerBody = Create("GrocerBody", grocerBodySprite != null ? grocerBodySprite : whiteSprite, 12);
            fruit = Create("Fruit", whiteSprite, 14);
            flesh = Create("Flesh", whiteSprite, 15);
            specks = CreatePool("Speck", circleSprite, 16, SpeckCount);
            blade = Create("Blade", whiteSprite, 17);
            knifeBolster = Create("KnifeBolster", whiteSprite, 17);
            knifeHandle = Create("KnifeHandle", whiteSprite, 18);
            grocerHand = Create("GrocerHand", grocerHandSprite != null ? grocerHandSprite : whiteSprite, 19);
            dropping = Create("Dropping", whiteSprite, 20);
        }

        protected override void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX)
        {
            bottom = -0.2f;
            top = 2.6f;
            anchorWorldX = 0f;      // meyve
            anchorScreenX = 0.38f;
        }

        Layout LayoutFor(bool strip)
        {
            if (strip)
                return new Layout { FloorY = 0f, FruitX = 0f, FruitBottom = 0.45f, FruitScale = 1.5f, PinchX = -0.75f, PinchY = 1.25f, BowlX = 99f, BowlScale = 0f, MetersToUnits = 3.2f, Thick = 0.08f, RunEnd = 99f };
            // Manav: ayakları zeminde, avucu meyvenin altı.
            const float floorY = -2.6f, fruitX = -1.0f;
            float ppx = grocerBodySprite != null ? grocerBodySprite.rect.height / GrocerHeight : 160f;
            float w = grocerBodySprite != null ? grocerBodySprite.rect.width : 336f, h = grocerBodySprite != null ? grocerBodySprite.rect.height : 512f;
            float footX = fruitX - (PalmPx.x - w / 2f) / ppx;
            return new Layout
            {
                FloorY = floorY, FruitX = fruitX, FruitBottom = floorY + (h - PalmPx.y) / ppx - 0.06f, FruitScale = 0.85f,
                PinchX = footX + (PinchPx.x - w / 2f) / ppx, PinchY = floorY + (h - PinchPx.y) / ppx,
                BowlX = 3.7f, BowlY = floorY + CrateH, BowlScale = 0.95f, MetersToUnits = 2.4f, Thick = 0.1f, RunEnd = 2.45f,
            };
        }

        protected override void Draw(bool strip, bool night, float left, float right, float top, float bottom)
        {
            Background(strip, Wall);
            var l = LayoutFor(strip);
            long done = env.Done;
            float f = env.Phase;
            float dim = night ? 0.8f : 1f;
            var tint = Opaque(WorldTheme.Surface(Color.white) * dim);
            float spinDeg = (float)(env.Position * DegreesPerPoint % 360.0);
            bool card = !strip;

            // Zemin.
            floor.enabled = card;
            if (card)
            {
                PlaceSpan(floorTop, left - 0.2f, right + 0.2f, l.FloorY - 0.22f, l.FloorY, Opaque(WorldTheme.Surface(FloorTop) * dim));
                PlaceSpan(floor, left - 0.2f, right + 0.2f, bottom - 0.2f, l.FloorY - 0.22f, Opaque(WorldTheme.Surface(Floor) * dim));
            }
            else
            {
                var c = WorldTheme.Surface(FloorTop);
                c.a = 0.95f;
                PlaceSpan(floorTop, left + 3.2f, right + 0.2f, l.FloorY - 0.1f, l.FloorY, c);
            }

            // Kasalar (yalnız kart): her kasada mekânın bir sıradan meyvesi; üstteki kasa sıradaki meyvenin geldiği yer.
            var site = env.Site;
            var wood = Opaque(WorldTheme.Surface(CrateWood) * dim);
            for (int k = 0; k < Crates; k++)
            {
                var b = CrateBoxes[k];
                float y0 = l.FloorY + b.z * CrateH;
                crateFronts[k].enabled = card;
                for (int s = 0; s < CrateSlats; s++) crateSlats[k * CrateSlats + s].enabled = card;
                var type = site.Commons != null && site.Commons.Length > 0 ? site.Commons[k % site.Commons.Length] : null;
                for (int j = 0; j < CrateFruit; j++)
                {
                    var sr = crateFruit[k * CrateFruit + j];
                    sr.enabled = card && type != null;
                    if (!sr.enabled) continue;
                    float fx = Mathf.Lerp(b.x + 0.35f, b.y - 0.35f, j / (CrateFruit - 1f));
                    PlaceSprite(sr, ItemSprite(type.Id), fx, y0 + CrateH - 0.3f, Mathf.Min(0.6f, type.Size * 0.6f), tint);
                }
                if (!card) continue;
                PlaceSpan(crateFronts[k], b.x, b.y, y0, y0 + CrateH - 0.05f, wood);
                DrawSlats(crateSlats, k * CrateSlats, b.x, b.y, y0, y0 + CrateH - 0.05f, dim);
            }
            stand.enabled = card;
            for (int s = 0; s < CrateSlats; s++) standSlats[s].enabled = card;
            if (card)
            {
                PlaceSpan(stand, l.BowlX - 1.05f, l.BowlX + 1.05f, l.FloorY, l.BowlY, wood);
                DrawSlats(standSlats, 0, l.BowlX - 1.05f, l.BowlX + 1.05f, l.FloorY, l.BowlY, dim);
            }

            // Manav (yalnız kart): gövde meyvenin arkasında, bıçak tutan el önünde.
            bool grocerOn = card && grocerBodySprite != null && grocerHandSprite != null;
            grocerBody.enabled = grocerHand.enabled = grocerOn;
            if (grocerOn)
            {
                float ppx = grocerBodySprite.rect.height / GrocerHeight;
                float footX = l.FruitX - (PalmPx.x - grocerBodySprite.rect.width / 2f) / ppx;
                PlaceSprite(grocerBody, grocerBodySprite, footX, l.FloorY, GrocerHeight, tint);
                PlaceSprite(grocerHand, grocerHandSprite, footX, l.FloorY, GrocerHeight, tint);
            }

            // Bu meyve.
            var item = env.Identify(done + 1);
            var whole = SpriteFor(item, false);
            var fleshSprite = SpriteFor(item, true);
            float h = item.Type.Size * l.FruitScale;
            float w = WidthAt(whole, h);
            float halfW = w / 2f;
            float mount = Smooth(0f, PeelerEnvironment.MountEnd, f);
            float p = Mathf.Clamp01((f - PeelerEnvironment.MountEnd) / (PeelerEnvironment.PeelEnd - PeelerEnvironment.MountEnd));
            bool dropPhase = f >= PeelerEnvironment.PeelEnd;
            float topKeep = item.Type.Shape != null && item.Type.Shape.Length > 0 ? item.Type.Shape[0] : 0f;
            float bodyTop = 1f - topKeep;              // görselin altından oran
            float bladeFrac = bodyTop * (1f - p);      // bıçağın görseldeki yüksekliği (alttan)
            float bladeY = l.FruitBottom + bladeFrac * h;

            // Takılma: kartta üstteki kasadan avuca yay çizerek gelir, şeritte yukarıdan iner.
            Vector2 seat = new Vector2(l.FruitX, l.FruitBottom);
            Vector2 from;
            if (card)
            {
                var c = CrateBoxes[2];
                from = new Vector2((c.x + c.y) / 2f, l.FloorY + c.z * CrateH + CrateH - 0.3f);
            }
            else from = seat + new Vector2(0f, 0.8f);
            Vector2 at = mount >= 1f ? seat : Arc(from, seat, card ? 1.2f : 0.2f, mount);

            if (!dropPhase)
            {
                dropping.enabled = false;
                PlaceSprite(fruit, whole, at.x, at.y, h, WithAlpha(tint, card ? 1f : mount));
                DrawFleshCrop(fleshSprite, at.x, at.y, h, bladeFrac, WithAlpha(tint, card ? 1f : mount), p > 0f);
            }
            else
            {
                fruit.enabled = false;
                flesh.enabled = false;
                float t = Smooth(PeelerEnvironment.PeelEnd, 1f, f);
                var b = card ? new Vector2(l.BowlX - 0.75f, l.BowlY + 0.22f) : new Vector2(l.FruitX + 1.2f, l.FloorY - 1.5f);
                var pos = Arc(seat, b, card ? 1.4f : 0.4f, t);
                float hh = Mathf.Lerp(h, item.Type.Size * (card ? l.BowlScale : 1f), t);
                PlaceSprite(dropping, fleshSprite, pos.x, pos.y, hh, WithAlpha(tint, card ? 1f : 1f - t));
            }

            // Bıçak: çimdikleyen elden meyvenin sağ kenarındaki değme noktasına. Kart dışında el yok: kısa sap.
            bool knifeOn = mount >= 1f && !dropPhase;
            float contactY = bladeY;
            float rel = (contactY - (l.FruitBottom + bodyTop * h * 0.5f)) / Mathf.Max(0.1f, bodyTop * h * 0.5f);
            float contactHalf = halfW * Mathf.Sqrt(Mathf.Max(0.05f, 1f - rel * rel));
            var contact = new Vector2(l.FruitX + contactHalf * 0.9f, contactY);
            var pinch = new Vector2(l.PinchX, l.PinchY);
            knifeHandle.enabled = knifeBolster.enabled = blade.enabled = knifeOn;
            if (knifeOn)
            {
                var dir = (contact - pinch).normalized;
                PlaceLine(blade, pinch, contact + dir * 0.06f, strip ? 0.06f : 0.07f, Opaque(WorldTheme.Surface(Steel) * dim));
                PlaceLine(knifeBolster, pinch - dir * 0.03f, pinch + dir * 0.05f, strip ? 0.08f : 0.09f, Opaque(WorldTheme.Surface(Bolster) * dim));
                PlaceLine(knifeHandle, pinch - dir * (strip ? 0.4f : 0.42f), pinch - dir * 0.02f, strip ? 0.09f : 0.11f, Opaque(WorldTheme.Surface(Handle) * dim));
            }

            // Dönüş: kabuktaki birkaç benek yatay kayar (soyulmamış kısımda).
            for (int k = 0; k < SpeckCount; k++)
            {
                float a = (spinDeg * 1.3f + k * 120f) * Mathf.Deg2Rad;
                float depth = Mathf.Cos(a);
                float sy = l.FruitBottom + bladeFrac * h * (0.25f + 0.25f * k);
                bool on = !dropPhase && depth > 0.1f && sy < bladeY - 0.05f && mount >= 1f;
                specks[k].enabled = on;
                if (!on) continue;
                float sx = l.FruitX + Mathf.Sin(a) * halfW * 0.7f;
                var c = Color.Lerp(item.Type.Color, Color.black, 0.35f);
                c.a = 0.55f * depth;
                PlaceRect(specks[k], sx, sy, (strip ? 0.09f : 0.07f) * (0.5f + 0.5f * depth), strip ? 0.09f : 0.07f, c);
            }

            // Kabuk.
            float meters = PeelerEnvironment.PeelLength(item) / 100f;
            float length = meters * l.MetersToUnits * p;
            float ribbonAlpha = dropPhase ? 1f - Smooth(PeelerEnvironment.PeelEnd, 1f, f) : 1f;
            float fallY = dropPhase ? Smooth(PeelerEnvironment.PeelEnd, 1f, f) * 0.3f : 0f;
            DrawRibbon(strip, l, item, new Vector2(contact.x, contact.y - fallY), length, spinDeg * Mathf.Deg2Rad, ribbonAlpha, dim, left);

            // Kâse (yalnız kart): sandığın üstünde, son 3 meyve (yeni atılan önde); atış sırasında bir yuva kayar.
            bowlBack.enabled = bowlFront.enabled = card;
            if (card)
            {
                var bw = Opaque(WorldTheme.Surface(BowlWood) * dim);
                PlaceRect(bowlBack, l.BowlX, l.BowlY + 0.55f, 2.5f, 0.42f, Opaque(bw * 0.7f));
                PlaceRect(bowlFront, l.BowlX, l.BowlY + 0.55f, 2.6f, 1.5f, bw);
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
                    float x = l.BowlX - 0.75f + slot * 0.75f;
                    float alpha = k == BowlSlots - 1 ? 1f - shift : 1f;
                    PlaceSprite(bowlFruit[k], SpriteFor(it, true), x, l.BowlY + 0.22f, Mathf.Min(1.1f, it.Type.Size * l.BowlScale), WithAlpha(tint, alpha));
                }
            }
            else
            {
                foreach (var b in bowlFruit) b.enabled = false;
            }
        }

        // Kasa ön yüzüne iki koyu çıta aralığı.
        void DrawSlats(SpriteRenderer[] pool, int start, float x0, float x1, float y0, float y1, float dim)
        {
            var gap = Opaque(WorldTheme.Surface(CrateWood * 0.72f) * dim);
            for (int s = 0; s < CrateSlats; s++)
            {
                float y = Mathf.Lerp(y0, y1, (s + 1f) / (CrateSlats + 1f));
                PlaceSpan(pool[start + s], x0, x1, y - 0.025f, y + 0.025f, gap);
            }
        }

        static Vector2 Arc(Vector2 a, Vector2 b, float lift, float t)
        {
            var ctrl = new Vector2((a.x + b.x) / 2f, Mathf.Max(a.y, b.y) + lift);
            float u = 1f - t;
            return u * u * a + 2f * u * t * ctrl + t * t * b;
        }

        // Et görseli: bıçak çizgisinden görselin üstüne kadar kırpılmış sprite (önbellekte, CropSteps adım).
        void DrawFleshCrop(Sprite source, float x, float bottomY, float h, float bladeFrac, Color color, bool any)
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

        // Kabuk şeridi: kartta bıçaktan yere kıvrılarak sarkar, sonra yerde sağa uzar; sandığa (RunEnd) varınca geri
        // katlanıp bir sıra yukarıda sürer (yığın). Şeritte sağa doğru dalgalanan uzun bir şerit. Dış yüz kabuk rengi,
        // iç yüz açık — sarmalın dönüşü renk değişiminden okunur.
        void DrawRibbon(bool strip, Layout l, EnvItem item, Vector2 start, float length, float twist, float alpha, float dim, float left)
        {
            int used = 0;
            if (length > 0.02f && alpha > 0.01f)
            {
                float step = Mathf.Max(strip ? 0.09f : 0.06f, length / RibbonMax);
                var outer = WorldTheme.Surface(item.Type.Color) * dim;
                var inner = WorldTheme.Surface(Color.Lerp(item.Type.Color, item.Type.Color2, 0.65f)) * dim;
                Vector2 prev = start;
                float groundY = l.FloorY + 0.04f;
                float hang = start.y - groundY;
                float x0 = start.x + 0.14f, span = Mathf.Max(0.5f, l.RunEnd - x0);
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
                        float run = (sArc - hang) * 0.92f;
                        int row = Mathf.FloorToInt(run / span);
                        float along = run - row * span;
                        float x = row % 2 == 0 ? x0 + along : x0 + span - along;
                        pnt = new Vector2(x, groundY + row * 0.07f + 0.05f * Mathf.Abs(Mathf.Sin(run * 5f)));
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
