using UnityEngine;

namespace ScrollRoad
{
    // Bumerang sahnesi (2026-10-14): yandan kesit, Avustralya çölü (kızıl toprak, uzakta yassı bir kaya tepesi,
    // birkaç çalı). Solda atıcı (tek karakter, Gemini); bumerang kodla çizilir. Sağda hedef, mesafesine göre
    // uzakta durur. Atış evreleri BoomerangEnvironment'ta: dönüş → elde hız biriktirme (atıcı geriye yaslanır,
    // bumerang gittikçe hızlı döner) → fırlatma (öne eğilir) → yay çizerek uçuş → vuruş (hedef devrilir,
    // kıvılcım) → aşağıdan kıvrılarak ele dönüş.
    //
    // Sahne konumun SAF fonksiyonu: geri sarınca bumerang aynı yoldan tersine gider, devrilen hedef kalkar.
    // Görev çubuğu şeridi: aynı sahne yatay ve alçak (atıcı solda yazının yanında, hedef sağda).
    public class BoomerangSceneView : IntervalSceneView
    {
        [SerializeField] BoomerangEnvironment env;
        [SerializeField] Sprite throwerSprite;
        [SerializeField] Sprite[] commonSprites;
        [SerializeField] Sprite[] specialSprites;
        // Atıcının elinin sprite içindeki yeri (0..1, soldan / alttan): bumerang burada döner.
        [SerializeField] Vector2 handPoint = new Vector2(0.07f, 0.89f);

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "Outback";

        const int SparkCount = 6;
        const int BushCount = 5;
        // Bir atışta bumerangın toplam tur sayısı ölçeği (dönüş hızı evrelere göre değişir).
        const float TurnsPerThrow = 30f;

        static readonly Color Sky = new Color(0.80f, 0.89f, 0.95f);
        static readonly Color Earth = new Color(0.80f, 0.46f, 0.28f);
        static readonly Color EarthDark = new Color(0.66f, 0.35f, 0.22f);
        static readonly Color Mesa = new Color(0.78f, 0.48f, 0.36f);
        static readonly Color Bush = new Color(0.55f, 0.60f, 0.36f);
        static readonly Color Wood = new Color(0.74f, 0.47f, 0.24f);
        static readonly Color Spark = new Color(1f, 0.85f, 0.35f);

        Sprite boomerangSprite;
        SpriteRenderer mesa, mesaTop, ground, groundEdge, thrower, boomerang, nextTarget, lastTarget;
        SpriteRenderer[] bushes, sparks;

        struct Layout
        {
            public float GroundY, ThrowerX, ThrowerH, NearX, FarX, BoomerangSize, ApexMax;
        }

        protected override void Build()
        {
            boomerangSprite = CreateBoomerangSprite(64);
            mesa = Create("Mesa", whiteSprite, 2);
            mesaTop = Create("MesaTop", circleSprite, 2);
            ground = Create("Ground", whiteSprite, 4);
            groundEdge = Create("GroundEdge", whiteSprite, 5);
            bushes = CreatePool("Bush", circleSprite, 6, BushCount);
            lastTarget = Create("LastTarget", whiteSprite, 10);
            nextTarget = Create("NextTarget", whiteSprite, 11);
            thrower = Create("Thrower", throwerSprite != null ? throwerSprite : whiteSprite, 20);
            boomerang = Create("Boomerang", boomerangSprite, 25);
            sparks = CreatePool("Spark", circleSprite, 30, SparkCount);
        }

        protected override void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX)
        {
            bottom = -0.2f;
            top = 2.6f;
            anchorWorldX = 0f;   // atıcı
            anchorScreenX = 0.36f;
        }

        Layout LayoutFor(bool strip) => strip
            ? new Layout { GroundY = 0f, ThrowerX = 0f, ThrowerH = 1.95f, NearX = 2.6f, FarX = 13.0f, BoomerangSize = 0.42f, ApexMax = 2.45f }
            : new Layout { GroundY = -2.7f, ThrowerX = -5.0f, ThrowerH = 2.3f, NearX = -2.2f, FarX = 6.2f, BoomerangSize = 0.5f, ApexMax = 1.6f };

        // Mesafe (m) → hedefin x'i.
        static float TargetX(Layout l, float meters) => Mathf.Lerp(l.NearX, l.FarX, Mathf.InverseLerp(20f, 92f, meters));

        // Dönüş hızının birikimi: 0–0,15 yavaşlar (ele dönüş), 0,15–0,70 hızlanır (elde), 0,70–1 sabit (uçuş).
        static float SpinIntegral(float f)
        {
            if (f < 0.15f) return 1.6f * f - 4f * f * f;
            if (f < 0.70f)
            {
                float x = f - 0.15f;
                return 0.15f + 0.4f * x + 1.2f / (2f * 0.55f) * x * x;
            }
            return 0.70f + 1.6f * (f - 0.70f);
        }

        const float SpinPerThrow = 1.18f; // SpinIntegral(1)

        protected override void Draw(bool strip, bool night, float left, float right, float top, float bottom)
        {
            Background(strip, Sky);
            var l = LayoutFor(strip);
            long done = env.Done;
            float f = env.Phase;

            // Zemin, uzak kaya, çalılar.
            mesa.enabled = mesaTop.enabled = !strip;
            foreach (var b in bushes) b.enabled = !strip;
            if (!strip)
            {
                var mesaC = Opaque(WorldTheme.Land(Mesa));
                PlaceRect(mesa, 2.2f, l.GroundY + 0.55f, 4.6f, 1.1f, mesaC);
                PlaceRect(mesaTop, 2.2f, l.GroundY + 1.05f, 4.4f, 0.5f, mesaC);
                PlaceSpan(ground, left - 0.2f, right + 0.2f, bottom - 0.2f, l.GroundY, Opaque(WorldTheme.Land(Earth)));
                PlaceSpan(groundEdge, left - 0.2f, right + 0.2f, l.GroundY - 0.08f, l.GroundY, Opaque(WorldTheme.Land(EarthDark)));
                for (int i = 0; i < BushCount; i++)
                {
                    float bx = -6.8f + i * 3.3f + (i % 2) * 0.7f;
                    float bs = 0.32f + (i % 3) * 0.1f;
                    PlaceRect(bushes[i], bx, l.GroundY + bs * 0.3f, bs * 1.6f, bs, Opaque(WorldTheme.Land(Bush)));
                }
            }
            else
            {
                ground.enabled = false;
                var c = WorldTheme.Surface(Earth);
                c.a = 0.95f;
                PlaceSpan(groundEdge, left + 3.0f, right + 0.2f, l.GroundY - 0.12f, l.GroundY, c);
            }

            // Atıcı: elde hız biriktirirken geriye yaslanır, fırlatırken öne eğilir.
            float lean;
            if (f < BoomerangEnvironment.ReturnEnd) lean = Mathf.Lerp(-4f, 0f, f / BoomerangEnvironment.ReturnEnd);
            else if (f < BoomerangEnvironment.ThrowAt) lean = 9f * Smooth(BoomerangEnvironment.ReturnEnd, BoomerangEnvironment.ThrowAt - 0.04f, f);
            else lean = Mathf.Lerp(9f, -8f, Smooth(BoomerangEnvironment.ThrowAt - 0.04f, BoomerangEnvironment.ThrowAt + 0.06f, f))
                        + 4f * Smooth(BoomerangEnvironment.ThrowAt + 0.06f, 1f, f);
            var tint = Opaque(WorldTheme.Surface(Color.white) * (night ? 0.82f : 1f));
            PlaceSprite(thrower, throwerSprite, l.ThrowerX, l.GroundY, l.ThrowerH, tint, lean);
            Vector2 hand = HandPosition(l, lean);

            // Hedefler: sıradaki (n+1) belirir ve vurulana dek durur; vurulan (n) devrilip söner.
            var nextItem = env.Identify(done + 1);
            float nextX = TargetX(l, BoomerangEnvironment.Distance(nextItem));
            float appear = Smooth(0.12f, 0.30f, f);
            DrawTarget(nextTarget, nextItem, nextX, l, strip, night, appear, 0f);

            Vector2 lastHit = hand;
            if (done >= 1)
            {
                var lastItem = env.Identify(done);
                float lastX = TargetX(l, BoomerangEnvironment.Distance(lastItem));
                float fall = Smooth(0f, 0.16f, f);
                float fade = 1f - Smooth(0.16f, 0.36f, f);
                DrawTarget(lastTarget, lastItem, lastX, l, strip, night, fade, -80f * fall);
                lastHit = HitPoint(l, lastItem, lastX, strip);
            }
            else
            {
                lastTarget.enabled = false;
            }
            Vector2 nextHit = HitPoint(l, nextItem, nextX, strip);

            // Bumerang.
            Vector2 pos;
            if (f < BoomerangEnvironment.ReturnEnd && done >= 1)
            {
                float t = f / BoomerangEnvironment.ReturnEnd;
                // Dönüş: aşağıdan kıvrılan yay.
                var mid = (lastHit + hand) / 2f;
                var ctrl = new Vector2(mid.x, l.GroundY + 0.25f);
                pos = Bezier(lastHit, ctrl, hand, t);
            }
            else if (f >= BoomerangEnvironment.ThrowAt)
            {
                float t = (f - BoomerangEnvironment.ThrowAt) / (1f - BoomerangEnvironment.ThrowAt);
                var mid = (hand + nextHit) / 2f;
                float apex = Mathf.Min(l.ApexMax, Mathf.Max(hand.y, nextHit.y) + 0.6f + 0.06f * Mathf.Abs(nextHit.x - hand.x));
                pos = Bezier(hand, new Vector2(mid.x, apex * 2f - mid.y), nextHit, t);
            }
            else
            {
                pos = hand;
            }
            float spin = (float)((done * SpinPerThrow + SpinIntegral(f)) * TurnsPerThrow * 360.0 % 360.0);
            var wood = Opaque(WorldTheme.Surface(Wood) * (night ? 0.85f : 1f));
            PlaceRect(boomerang, pos.x, pos.y, l.BoomerangSize, l.BoomerangSize, wood, -spin);

            // Vuruş anı: hedefin üstünde dışa saçılan kıvılcımlar (geri sararken yok).
            bool burst = done >= 1 && f < 0.12f && env.Direction >= 0f;
            for (int k = 0; k < SparkCount; k++)
            {
                sparks[k].enabled = burst;
                if (!burst) continue;
                float t = f / 0.12f;
                float ang = (k / (float)SparkCount) * Mathf.PI * 2f + 0.3f;
                float r = 0.12f + t * (strip ? 0.5f : 0.7f);
                var c = WorldTheme.Surface(Spark);
                c.a = 1f - t;
                float d = (strip ? 0.12f : 0.1f) * (1f - t * 0.5f);
                PlaceRect(sparks[k], lastHit.x + Mathf.Cos(ang) * r, lastHit.y + Mathf.Sin(ang) * r * 0.8f, d, d, c);
            }
        }

        Vector2 HandPosition(Layout l, float lean)
        {
            float h = l.ThrowerH;
            float w = WidthAt(throwerSprite, h);
            // Sprite alt ortasından el noktasına (yaslanma açısıyla döndürülmüş).
            var local = new Vector2((handPoint.x - 0.5f) * w, handPoint.y * h);
            float r = lean * Mathf.Deg2Rad;
            var rotated = new Vector2(local.x * Mathf.Cos(r) - local.y * Mathf.Sin(r), local.x * Mathf.Sin(r) + local.y * Mathf.Cos(r));
            return new Vector2(l.ThrowerX, l.GroundY) + rotated;
        }

        // Hedef boyu: kartta atıcıyla orantılı görünsün diye büyütülür.
        static float TargetHeight(EnvItem item, bool strip) => item.Type.Size * (strip ? 1.05f : 1.3f);

        static Vector2 HitPoint(Layout l, EnvItem item, float x, bool strip) => new Vector2(x, l.GroundY + TargetHeight(item, strip) * 0.62f);

        void DrawTarget(SpriteRenderer sr, EnvItem item, float x, Layout l, bool strip, bool night, float alpha, float angle)
        {
            if (alpha <= 0.01f)
            {
                sr.enabled = false;
                return;
            }
            var sprite = SpriteFor(item);
            float h = TargetHeight(item, strip);
            var c = WorldTheme.Surface(Color.white) * (night ? 0.82f : 1f);
            c.a = alpha;
            // Devrilme: hedef arkaya (sağa) yatar, dibi yerinde kalır.
            PlaceSprite(sr, sprite, x, l.GroundY, h, c, angle);
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

        static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        // Bumerang: ortada ~100° açılı iki kol (yuvarlak uçlu), sprite merkezi kolların birleştiği yerin biraz
        // dışında (dönüş merkezi). Beyaz; renk oyunda verilir. Üstte ince açık bir şerit (boyalı desen).
        static Sprite CreateBoomerangSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var elbow = new Vector2(size * 0.5f, size * 0.62f);
            var armA = elbow + new Vector2(-0.42f, -0.30f) * size;
            var armB = elbow + new Vector2(0.42f, -0.30f) * size;
            float radius = size * 0.085f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Mathf.Min(SegDist(p, elbow, armA), SegDist(p, elbow, armB));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                // Kolların ortasında açık bir şerit.
                float stripe = Mathf.Abs(d - radius * 0.1f) < radius * 0.28f ? 1f : 0.82f;
                byte v = (byte)(255 * stripe);
                px[y * size + x] = new Color32(v, v, v, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
