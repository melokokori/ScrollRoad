using UnityEngine;

namespace ScrollRoad
{
    // Dünyayı döndüren elin sahnesi (2026-10-14). Kart: koyu mavi uzayda küçük bir gezegen (okyanus + kodla
    // üretilen kıtalar) döner; solda Güneş, gezegenin sağ yarısı gölgede — yüzeydeki yapılar dönerken
    // gündüzden geceye geçer. Gezegenin kenarında 12 yuva: her yıl bir yuvada yeni bir yapı yükselir (12 yıl
    // önceki o yuvadaki yapı toprağa iner). Sağda bir el, işaret parmağıyla gezegenin kenarını tur başına bir
    // kez yukarı iter (gezegen saat yönünün tersine döner); geri sarınca aşağı iter.
    //
    // Görev çubuğu şeridi (yatay yorum): gezegenin ekvatoru açılmış bir yer çizgisi olarak; yapılar elin
    // olduğu sağ uçta gecede doğar, sola akarken gündüze çıkar. El sağ uçta parmağıyla zemini sola iter.
    //
    // Sahne konumun SAF fonksiyonu: gezegen açısı = konum × DegreesPerPoint; ayın i. yılının yapısı
    // (i mod 12). yuvada, yılın ilerlemesiyle yükselir.
    public class GlobeSceneView : IntervalSceneView
    {
        [SerializeField] GlobeEnvironment env;
        [SerializeField] Sprite handSprite;
        // Parmak ucunun sprite içindeki yeri (0..1, soldan / alttan). Tools/gen_globe.py ölçüp yazdı.
        [SerializeField] Vector2 handTip = new Vector2(0.0f, 0.645f);

        protected override IntervalEnvironment Env => env;
        protected override string RootName => "Globe";

        const int Slots = 12;
        const int StarCount = 30;
        const int ItemPool = 14;
        const int GroundSegments = 32;
        const float StripLoop = 24f; // şeritte ekvatorun açılmış uzunluğu (dünya birimi)

        static readonly Vector2 Center = new Vector2(-0.4f, -1.3f);
        const float Radius = 2.0f;
        static readonly Vector2 SunPos = new Vector2(-5.7f, -1.4f);

        static readonly Color Space = new Color(0.13f, 0.17f, 0.31f);
        static readonly Color Ocean = new Color(0.26f, 0.53f, 0.78f);
        static readonly Color LandGreen = new Color(0.50f, 0.72f, 0.42f);
        static readonly Color LandTan = new Color(0.82f, 0.72f, 0.50f);
        static readonly Color Atmosphere = new Color(0.62f, 0.82f, 1f, 0.22f);
        static readonly Color Shadow = new Color(0.04f, 0.06f, 0.16f);
        static readonly Color SunColor = new Color(1f, 0.85f, 0.42f);
        static readonly Color NightTint = new Color(0.34f, 0.38f, 0.56f);
        static readonly Color StripGround = new Color(0.50f, 0.68f, 0.40f);

        Sprite planetSprite, shadowSprite;
        string planetSkin; // gezegen dokusu bu takımın renkleriyle üretildi
        SpriteRenderer sunGlow, sun, atmosphere, planet, shade, hand;
        SpriteRenderer[] stars, items, ground;

        protected override void Build()
        {
            // Kıta dokusu (256², gürültü) ilk görünüşte üretilir: ortamı hiç açmayan oyuncu açılışta beklemesin.
            shadowSprite = CreateShadowSprite(128);
            var bigCircle = CreateCircleSprite(128);

            stars = CreatePool("Star", circleSprite, 1, StarCount);
            sunGlow = Create("SunGlow", bigCircle, 2);
            sun = Create("Sun", bigCircle, 3);
            atmosphere = Create("Atmosphere", bigCircle, 4);
            planet = Create("Planet", whiteSprite, 5);
            shade = Create("NightSide", shadowSprite, 7);
            ground = CreatePool("Ground", whiteSprite, 5, GroundSegments);
            items = CreatePool("Item", whiteSprite, 10, ItemPool);
            hand = Create("Hand", handSprite != null ? handSprite : whiteSprite, 30);
        }

        protected override void StripBand(out float bottom, out float top, out float anchorWorldX, out float anchorScreenX)
        {
            bottom = -0.25f;
            top = 2.55f;
            anchorWorldX = 0f;      // elin parmağı
            anchorScreenX = 0.82f; // sağ uçta şeridin ≡ düğmesi var
        }

        // Güneş solda: yüzey noktasının dışa bakan açısına göre aydınlık 0..1.
        static float Light(float thetaDeg)
        {
            float c = -Mathf.Cos(thetaDeg * Mathf.Deg2Rad);
            return Smooth(-0.35f, 0.3f, c);
        }

        protected override void Draw(bool strip, bool night, float left, float right, float top, float bottom)
        {
            // Ortam Standardı v2: mekân (çağ) uzayın ve güneşin rengini, skin takımı gezegeni boyar.
            Background(strip, Look(0, Space));
            double position = env.Position;
            float globeAngle = (float)(position * GlobeEnvironment.DegreesPerPoint % 360.0);

            // Kart öğeleri yalnız kartta.
            foreach (var s in stars) s.enabled = !strip;
            sunGlow.enabled = sun.enabled = atmosphere.enabled = planet.enabled = shade.enabled = !strip;
            foreach (var g in ground) g.enabled = strip;

            if (!strip)
            {
                if (planetSprite == null || planetSkin != env.SkinId)
                {
                    planetSkin = env.SkinId;
                    planetSprite = CreatePlanetSprite(256, Tint(0, Ocean), Tint(1, LandGreen), Tint(2, LandTan));
                    planet.sprite = planetSprite;
                }
                for (int i = 0; i < StarCount; i++)
                {
                    float hx = Hash(i, 1), hy = Hash(i, 2), hs = Hash(i, 3);
                    float x = Mathf.Lerp(left + 0.2f, right - 0.2f, hx);
                    float y = Mathf.Lerp(bottom + 0.2f, top - 0.2f, hy);
                    float d = 0.03f + hs * 0.04f;
                    PlaceRect(stars[i], x, y, d, d, WithAlpha(Color.white, night ? 0.75f : 0.55f + hs * 0.3f));
                }
                var sunC = WorldTheme.Surface(Look(1, SunColor));
                PlaceRect(sunGlow, SunPos.x, SunPos.y, 1.5f, 1.5f, WithAlpha(sunC, 0.07f));
                PlaceRect(sun, SunPos.x, SunPos.y, 1.0f, 1.0f, Opaque(sunC));
                PlaceRect(atmosphere, Center.x, Center.y, Radius * 2.16f, Radius * 2.16f, WorldTheme.Surface(Tint(3, Atmosphere)));
                PlaceRect(planet, Center.x, Center.y, Radius * 2f, Radius * 2f, Opaque(WorldTheme.Surface(Color.white) * (night ? 0.85f : 1f)), globeAngle);
                PlaceRect(shade, Center.x, Center.y, Radius * 2.01f, Radius * 2.01f, Opaque(Shadow));
            }
            else
            {
                // Açılmış ekvator: her parça kendi açısının aydınlığıyla.
                float x0 = left - 0.5f, x1 = 0.6f;
                float segW = (x1 - x0) / GroundSegments;
                for (int k = 0; k < GroundSegments; k++)
                {
                    float x = x0 + (k + 0.5f) * segW;
                    float theta = StripTheta(x);
                    var groundColor = Tint(1, StripGround);
                    var c = Color.Lerp(groundColor * 0.45f, groundColor, Light(theta));
                    c = WorldTheme.Surface(c);
                    c.a = 0.95f;
                    PlaceRect(ground[k], x, -0.08f, segW + 0.02f, 0.16f, c);
                }
            }

            DrawItems(strip, night, globeAngle, left);
            DrawHand(strip, night, globeAngle);
        }

        // Şeritte bir x'in ekvatordaki açısı (gezegenin dönüşünden bağımsız: sabit gün/gece bandı).
        static float StripTheta(float x) => (-(x + 0.9f) / StripLoop) * 360f;

        void DrawItems(bool strip, bool night, float globeAngle, float left)
        {
            long done = env.Done;
            float f = env.Phase;
            int used = 0;
            for (long i = done + 1 - Slots; i <= done + 1 && used < ItemPool; i++)
            {
                if (i < 1) continue;
                float g = 1f;
                if (i == done + 1) g = Smooth(0.12f, 0.95f, f);            // bu yıl yükselen
                else if (i + Slots == done + 1) g = 1f - Smooth(0f, 0.45f, f); // yuvasını bırakan
                if (g <= 0.01f) continue;

                var item = env.Identify(i);
                var sprite = SpriteFor(item);
                var sr = items[used++];
                float theta = (int)(i % Slots) * (360f / Slots) + globeAngle;
                float light = Light(theta);
                var color = Color.Lerp(NightTint * (night ? 0.75f : 1f), Color.white, light);
                color = WorldTheme.Surface(color);
                color.a = 1f;

                if (!strip)
                {
                    float h = item.Type.Size * g;
                    if (WidthAt(sprite, h) > 1.3f) h *= 1.3f / WidthAt(sprite, h);
                    float rad = theta * Mathf.Deg2Rad;
                    var p = Center + (Radius - 0.04f) * new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    PlaceSprite(sr, sprite, p.x, p.y, h, color, theta - 90f);
                    sr.sortingOrder = 10;
                }
                else
                {
                    // Elin olduğu sağ uçtan (açı 0) sola akar; tur uzunluğu StripLoop.
                    float wrap = Mathf.Repeat(theta / 360f, 1f);
                    float x = -0.9f - wrap * StripLoop;
                    float h = item.Type.Size * 1.55f * g;
                    if (WidthAt(sprite, h) > 1.9f) h *= 1.9f / WidthAt(sprite, h);
                    // Sol uçta (yazının altında) söner.
                    color.a = Mathf.Clamp01((x - (left + 3.2f)) / 1.5f);
                    PlaceSprite(sr, sprite, x, -0.03f, h, color);
                    sr.sortingOrder = 10;
                }
            }
            for (int k = used; k < ItemPool; k++) items[k].enabled = false;
        }

        // El: tur başına bir itiş — turun ilk %30'unda parmak kenara değip yüzeyle birlikte kayar, sonra
        // kalkıp geri döner. Ters dönüşte aynı yol tersinden (aşağı iter).
        void DrawHand(bool strip, bool night, float globeAngle)
        {
            if (handSprite == null)
            {
                hand.enabled = false;
                return;
            }
            float s = Mathf.Repeat(globeAngle / 360f, 1f);
            float stroke, lift;
            if (s < 0.3f)
            {
                stroke = s / 0.3f;
                lift = 0f;
            }
            else
            {
                float u = (s - 0.3f) / 0.7f;
                stroke = 1f - Smooth(0f, 1f, u);
                lift = Mathf.Sin(u * Mathf.PI);
            }

            Vector2 tip;
            float rot, h;
            if (!strip)
            {
                float a = Mathf.Lerp(-28f, 28f, stroke) * Mathf.Deg2Rad;
                tip = Center + (Radius + 0.02f + 0.45f * lift) * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                rot = a * Mathf.Rad2Deg * 0.6f;
                h = 1.45f;
            }
            else
            {
                tip = new Vector2(Mathf.Lerp(0.55f, -0.55f, stroke), 0.02f + 0.55f * lift);
                rot = 18f;
                h = 1.5f;
            }
            float w = WidthAt(handSprite, h);
            float r = rot * Mathf.Deg2Rad;
            var right = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            var up = new Vector2(-Mathf.Sin(r), Mathf.Cos(r));
            var center = tip + (0.5f - handTip.x) * w * right + (0.5f - handTip.y) * h * up;
            hand.sprite = handSprite;
            hand.enabled = true;
            float sc = h / handSprite.bounds.size.y;
            hand.transform.localScale = new Vector3(sc, sc, 1f);
            hand.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            hand.transform.localPosition = new Vector3(center.x, center.y, 0f);
            var c = WorldTheme.Surface(Color.white) * (night ? 0.85f : 1f);
            c.a = 1f;
            hand.color = c;
        }

        Sprite SpriteFor(EnvItem item) => ItemSprite(item.Type.Id);

        static float Hash(int i, int salt) => (float)VisitorCatalog.Hash01(0x5EEDUL, i, salt);

        // Okyanus + değer gürültüsüyle kıtalar (yeşil, yer yer kum rengi). Gezegenle birlikte döner.
        static Sprite CreatePlanetSprite(int size, Color ocean, Color landGreen, Color landTan)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size / 2f - 1f;
            float c = size / 2f - 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 0.5f);
                if (a <= 0f)
                {
                    px[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }
                float u = x / (float)size, v = y / (float)size;
                float n = Fbm(u * 5f, v * 5f, 11);
                float edge = d / r; // kıyıya yakın kıtalar biraz küçülsün (kenarda okyanus)
                Color col = ocean;
                if (n - edge * 0.12f > 0.56f)
                {
                    col = Fbm(u * 9f, v * 9f, 37) > 0.62f ? landTan : landGreen;
                }
                px[y * size + x] = new Color(col.r, col.g, col.b, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Gece yarısı: sağa doğru koyulaşan, sınırı yumuşak gölge (dönmez).
        static Sprite CreateShadowSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size / 2f - 1f;
            float c = size / 2f - 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float inside = Mathf.Clamp01(r - d + 0.5f);
                float shade = 0.62f * Smooth(-0.25f, 0.4f, dx / r);
                px[y * size + x] = new Color(1f, 1f, 1f, inside * shade);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static float Fbm(float x, float y, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < 4; o++)
            {
                sum += amp * ValueNoise(x, y, seed + o * 17);
                norm += amp;
                x *= 2.03f;
                y *= 2.03f;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        static float ValueNoise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Lattice(xi, yi, seed), b = Lattice(xi + 1, yi, seed);
            float c = Lattice(xi, yi + 1, seed), d = Lattice(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Lattice(int x, int y, int seed) =>
            (float)VisitorCatalog.Hash01((ulong)(uint)seed * 0x9E3779B1UL, x * 7919L + y * 104729L, 3);
    }
}
