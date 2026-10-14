using UnityEngine;

namespace ScrollRoad
{
    // Rüzgârgülü ortamının sahnesi (2026-10-14): yandan kesit. Arkada bir tepenin üstünde büyük beyaz türbin
    // (kule + gövde + 3 pervane, kodla çizilir, pervane döner), uzak sırtta iki küçük türbin; türbinin
    // dibinden sağa enerji hattı iner, ucunda bağlantı direği. Önde vadinin düzlüğünde yapılar sağdan sola
    // dizili: direğin sağındakiler elektriksiz (koyu silüet), direğin önünden geçen bağlanır (kıvılcım,
    // renkleri açılır), solda ışıklı köy olarak kaybolur. Hattın üstünde sarı enerji darbeleri akar,
    // gökte rüzgâr çizgileri esintiyle sürüklenir. Düz renk, Yol'un görsel diliyle; yapılar ayrı görseller
    // (Gemini), geri kalan her şey kodla.
    //
    // Sahne bu ayın konumunun SAF fonksiyonudur (WindController.Kwh / Position): ayın i. yapısının bağıl
    // konumu u = Kwh − i; u = 0'da direğin önündedir (o an bağlanır ve sayılır), u < 0 elektriksiz (sağda),
    // u > 0 bağlı (solda). Pervane açısı konumla orantılı: hız = tempo, geri sarınca ters döner.
    //
    // Görev çubuğu şeridinde: saydam arka plan; ince çim şeridi, küçük türbin, hat, direk ve yapılar.
    // Yalnız Rüzgârgülü seçiliyken çizer ve kamerayı yönetir.
    public class WindSceneView : MonoBehaviour
    {
        [SerializeField] WindController wind;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Camera viewCamera;
        [SerializeField] Sprite[] homeSprites;    // GridCatalog.Homes sırasıyla
        [SerializeField] Sprite[] specialSprites; // GridCatalog.AllSpecials sırasıyla
        [SerializeField] Sprite hillsSprite;
        [SerializeField] Material spriteMaterial;

        [Header("Kart")]
        [SerializeField] float cardViewTop = 2.4f;
        [SerializeField] float cardViewBottom = -4.1f;
        [SerializeField] float groundY = -2.95f;
        [SerializeField] float poleX = 0.9f;

        [Header("Görev çubuğu")]
        [SerializeField] float stripPoleScreenX = 0.5f; // solda kWh yazısına yer kalsın

        // Pervane: puan başına derece (seyirde ~0,45 tur/sn, tam boost'ta ~1,7 tur/sn, rölantide yavaş).
        const double DegreesPerPoint = 10.0;
        const float Gap = 2.5f;            // iki yapı arası (dünya birimi)
        const int PoolSize = 12;
        const int PulseCount = 7;
        const int SparkCount = 5;
        const int StreakCount = 6;

        static readonly Color SkyDay = new Color(0.80f, 0.90f, 0.96f);
        static readonly Color FarRidge = new Color(0.62f, 0.75f, 0.66f);
        static readonly Color HillGreen = new Color(0.55f, 0.72f, 0.45f);
        static readonly Color Grass = new Color(0.60f, 0.76f, 0.45f);
        static readonly Color Soil = new Color(0.50f, 0.40f, 0.30f);
        static readonly Color Turbine = new Color(0.96f, 0.96f, 0.95f);
        static readonly Color TurbineShade = new Color(0.84f, 0.86f, 0.88f);
        static readonly Color PoleWood = new Color(0.45f, 0.34f, 0.25f);
        static readonly Color Wire = new Color(0.22f, 0.23f, 0.26f);
        static readonly Color Energy = new Color(1.0f, 0.86f, 0.30f);
        static readonly Color UnlitDay = new Color(0.24f, 0.26f, 0.33f);
        static readonly Color UnlitNight = new Color(0.10f, 0.11f, 0.16f);
        static readonly Color Glow = new Color(1.0f, 0.82f, 0.45f);

        sealed class TurbineParts
        {
            public SpriteRenderer Tower, Nacelle, Hub;
            public SpriteRenderer[] Blades = new SpriteRenderer[3];
        }

        Transform root;
        Sprite whiteSprite, circleSprite, bladeSprite, towerSprite;
        SpriteRenderer farRidge, hill, soil, grass, wire, poleTop, pole, poleBox;
        TurbineParts main;
        TurbineParts[] far;
        SpriteRenderer[] pulses, sparks, streaks, buildings, glows;

        bool IsCompanion => companionWindow != null && companionWindow.IsCompanionMode;

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circleSprite = CreateCircleSprite();
            bladeSprite = CreateBladeSprite();
            towerSprite = CreateTowerSprite();
            Build();
        }

        void Build()
        {
            root = new GameObject("WindFarm").transform;
            root.SetParent(transform, false);

            streaks = new SpriteRenderer[StreakCount];
            for (int i = 0; i < StreakCount; i++) streaks[i] = Create($"Streak{i}", whiteSprite, 1);
            farRidge = Create("FarRidge", hillsSprite != null ? hillsSprite : whiteSprite, 2);
            far = new TurbineParts[2];
            for (int i = 0; i < far.Length; i++) far[i] = CreateTurbine($"FarTurbine{i}", 3);
            hill = Create("Hill", circleSprite, 6);
            main = CreateTurbine("Turbine", 8);
            wire = Create("Wire", whiteSprite, 12);
            pulses = new SpriteRenderer[PulseCount];
            for (int i = 0; i < PulseCount; i++) pulses[i] = Create($"Pulse{i}", circleSprite, 13);
            pole = Create("Pole", whiteSprite, 14);
            poleTop = Create("PoleArm", whiteSprite, 14);
            poleBox = Create("Transformer", whiteSprite, 15);
            soil = Create("Soil", whiteSprite, 16);
            grass = Create("Grass", whiteSprite, 17);
            glows = new SpriteRenderer[PoolSize];
            buildings = new SpriteRenderer[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                glows[i] = Create($"Glow{i}", circleSprite, 18);
                buildings[i] = Create($"Building{i}", whiteSprite, 20);
            }
            sparks = new SpriteRenderer[SparkCount];
            for (int i = 0; i < SparkCount; i++) sparks[i] = Create($"Spark{i}", circleSprite, 60);
        }

        TurbineParts CreateTurbine(string name, int order)
        {
            var t = new TurbineParts
            {
                Tower = Create(name + "Tower", towerSprite, order),
                Nacelle = Create(name + "Nacelle", whiteSprite, order + 1),
                Hub = Create(name + "Hub", circleSprite, order + 3),
            };
            for (int k = 0; k < 3; k++) t.Blades[k] = Create($"{name}Blade{k}", bladeSprite, order + 2);
            return t;
        }

        SpriteRenderer Create(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            return sr;
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size / 2f - 1f;
            var c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), c) + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Pervane kanadı: kökte dar, ~%22'de en geniş, uca doğru incelir; bir kenarı düz (ön kenar), öbürü
        // kavisli. Pivot kökte (alt orta), boyu 1 birim.
        static Sprite CreateBladeSprite()
        {
            const int w = 24, h = 160;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                float width = t < 0.22f ? Mathf.Lerp(0.45f, 1f, t / 0.22f) : Mathf.Lerp(1f, 0.18f, Mathf.Pow((t - 0.22f) / 0.78f, 0.9f));
                float half = width * (w / 2f - 1f);
                for (int x = 0; x < w; x++)
                {
                    float dx = x - (w / 2f - 0.5f);
                    // Ön kenar düz (sol), arka kenar genişliğin tamamı (sağ).
                    float lo = -half * 0.45f, hi = half * 1.0f;
                    float a = Mathf.Clamp01(Mathf.Min(dx - lo + 0.5f, hi - dx + 0.5f));
                    if (t > 0.985f) a *= (1f - t) / 0.015f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
        }

        // Kule: altta geniş, üstte dar konik. Pivot alt orta, boyu 1 birim.
        static Sprite CreateTowerSprite()
        {
            const int w = 32, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                float half = Mathf.Lerp(1f, 0.55f, t) * (w / 2f - 1f);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - (w / 2f - 0.5f));
                    float a = Mathf.Clamp01(half - dx + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
        }

        bool Show()
        {
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Wind;
            if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
            return active;
        }

        // ---------------------------------------------------------------- kare

        void LateUpdate()
        {
            if (!Show() || wind == null) return;
            bool strip = IsCompanion;
            bool night = WorldTheme.Current == WorldTheme.Kind.Night;

            // Kamera: kartta sabit bant; şeritte zeminin altından en uzun yapının üstüne.
            float aspect = Mathf.Max(0.1f, viewCamera.aspect);
            float size, camX, camY;
            if (strip)
            {
                float bottom = groundY - 0.15f, top = groundY + 3.0f;
                float band = 1f - (companionWindow != null ? companionWindow.StripOverflowFraction : 0f);
                size = (top - bottom) / 2f / Mathf.Max(0.2f, band);
                camY = bottom + size;
                camX = poleX - (stripPoleScreenX * 2f - 1f) * size * aspect;
            }
            else
            {
                size = (cardViewTop - cardViewBottom) / 2f;
                camY = (cardViewTop + cardViewBottom) / 2f;
                camX = 0f;
            }
            viewCamera.orthographicSize = size;
            viewCamera.transform.position = new Vector3(camX, camY, -10f);
            float halfW = size * aspect;
            float left = camX - halfW, right = camX + halfW;
            float top0 = camY + size, bottom0 = camY - size;

            var sky = WorldTheme.Sky(SkyDay);
            sky.a = 1f;
            viewCamera.backgroundColor = strip ? new Color(0f, 0f, 0f, 0f) : sky;

            double position = wind.Position;
            float rotor = (float)(-(position * DegreesPerPoint % 360.0)); // saat yönünde

            // Türbin yerleşimi: kartta tepenin üstünde, şeritte zeminde ve küçük.
            float turbineX, baseY, hubY, blade;
            if (strip)
            {
                turbineX = poleX - 2.3f;
                baseY = groundY;
                hubY = groundY + 2.05f;
                blade = 0.95f;
            }
            else
            {
                turbineX = -3.0f;
                baseY = groundY + 1.35f;
                hubY = 0.55f;
                blade = 1.35f;
            }

            // Rüzgâr çizgileri (yalnız kart): soldan sağa, esintinin hızıyla sürüklenir.
            for (int i = 0; i < StreakCount; i++)
            {
                streaks[i].enabled = !strip;
                if (strip) continue;
                float span = (right - left) + 4f;
                float phase = (float)((position * 0.0016 + i * 0.37) % 1.0);
                if (phase < 0) phase += 1f;
                float x = left - 2f + phase * span;
                float y = groundY + 1.6f + (i * 0.73f % 1f) * (top0 - groundY - 2.2f);
                var c = night ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.45f);
                c.a *= Mathf.Sin(phase * Mathf.PI); // kenarlarda söner
                PlaceRect(streaks[i], x, y, 1.1f + (i % 3) * 0.35f, 0.035f, c);
            }

            // Uzak sırt + iki küçük türbin (yalnız kart).
            farRidge.enabled = !strip;
            if (!strip) PlaceRect(farRidge, camX, groundY + 0.55f, halfW * 2.3f, 1.5f, Opaque(WorldTheme.Land(FarRidge)));
            for (int i = 0; i < far.Length; i++)
            {
                SetTurbineEnabled(far[i], !strip);
                if (strip) continue;
                float fx = 2.4f + i * 2.9f;
                float fBase = groundY + 0.85f + i * 0.12f;
                var tint = Color.Lerp(WorldTheme.Surface(TurbineShade), WorldTheme.Land(FarRidge), 0.35f) * (night ? 0.55f : 1f);
                DrawTurbine(far[i], fx, fBase, fBase + 1.15f, 0.55f, rotor * 0.93f + i * 47f, Opaque(tint), Opaque(tint));
            }

            // Tepe (kart): türbinin altında geniş yuvarlak.
            hill.enabled = !strip;
            if (!strip) PlaceRect(hill, turbineX - 0.4f, baseY - 1.5f, 8.5f, 3.2f, Opaque(WorldTheme.Land(HillGreen)));

            // Ana türbin.
            var body = Opaque(WorldTheme.Surface(Turbine) * (night ? 0.72f : 1f));
            var shade = Opaque(WorldTheme.Surface(TurbineShade) * (night ? 0.72f : 1f));
            // Şeritte türbin yapıların önünde (kayan yapılar arkasında kalmasın); kartta tepede, arkada.
            SetOrder(main, strip ? 50 : 8);
            DrawTurbine(main, turbineX, baseY, hubY, blade, rotor, body, shade);

            // Enerji hattı: kulenin ortasından direğin tepesine, üstünde sarı darbeler akar.
            float poleH = strip ? 1.6f : 2.15f;
            var a = new Vector2(turbineX + 0.06f, baseY + (hubY - baseY) * (strip ? 0.45f : 0.35f));
            var b = new Vector2(poleX, groundY + poleH - 0.08f);
            PlaceLine(wire, a, b, strip ? 0.035f : 0.03f, Opaque(WorldTheme.Surface(Wire) * (night ? 1.6f : 1f)));
            for (int i = 0; i < PulseCount; i++)
            {
                float f = (float)((position * 0.004 + i / (double)PulseCount) % 1.0);
                if (f < 0) f += 1f;
                var p = Vector2.Lerp(a, b, f);
                var c = WorldTheme.Surface(Energy);
                c.a = Mathf.Sin(f * Mathf.PI) * (wind.SpinDirection != 0f || ScoreEngine.Instance == null ? 1f : 0.5f);
                PlaceRect(pulses[i], p.x, p.y, strip ? 0.13f : 0.11f, strip ? 0.13f : 0.11f, c);
            }

            // Bağlantı direği: ahşap direk + kol + trafo kutusu.
            var wood = Opaque(WorldTheme.Surface(PoleWood) * (night ? 0.7f : 1f));
            PlaceRect(pole, poleX, groundY + poleH / 2f, 0.09f, poleH, wood);
            PlaceRect(poleTop, poleX, groundY + poleH - 0.1f, 0.55f, 0.06f, wood);
            PlaceRect(poleBox, poleX + 0.09f, groundY + poleH * 0.62f, 0.26f, 0.34f, Opaque(WorldTheme.Surface(new Color(0.55f, 0.58f, 0.60f)) * (night ? 0.7f : 1f)));

            // Zemin: çim + toprak (şeritte ince çim şeridi).
            soil.enabled = !strip;
            if (!strip) PlaceSpan(soil, left - 0.2f, right + 0.2f, bottom0 - 0.2f, groundY - 0.3f, Opaque(WorldTheme.Land(Soil)));
            var grassC = Opaque(WorldTheme.Land(Grass));
            if (strip) grassC.a = 0.95f;
            PlaceSpan(grass, left - 0.2f, right + 0.2f, groundY - (strip ? 0.12f : 0.3f), groundY, grassC);

            UpdateBuildings(strip, night, left, right, turbineX);
        }

        static void SetOrder(TurbineParts t, int order)
        {
            t.Tower.sortingOrder = order;
            t.Nacelle.sortingOrder = order + 1;
            foreach (var b in t.Blades) b.sortingOrder = order + 2;
            t.Hub.sortingOrder = order + 3;
        }

        void SetTurbineEnabled(TurbineParts t, bool on)
        {
            t.Tower.enabled = t.Nacelle.enabled = t.Hub.enabled = on;
            foreach (var b in t.Blades) b.enabled = on;
        }

        void DrawTurbine(TurbineParts t, float x, float baseY, float hubY, float blade, float angle, Color body, Color shade)
        {
            float towerH = hubY - baseY;
            float towerW = blade * 0.16f;
            t.Tower.enabled = towerH > 0.05f;
            t.Tower.transform.localPosition = new Vector3(x, baseY, 0f);
            t.Tower.transform.localRotation = Quaternion.identity;
            t.Tower.transform.localScale = new Vector3(towerW / towerSprite.bounds.size.x, towerH / towerSprite.bounds.size.y, 1f);
            t.Tower.color = body;
            // Gövde (nacelle) kulenin tepesinde, rüzgâra (sola) bakar; göbek sol ucunda.
            float nw = blade * 0.38f, nh = blade * 0.13f;
            PlaceRect(t.Nacelle, x + nw * 0.18f, hubY, nw, nh, shade);
            float hubX = x - nw * 0.32f;
            float hr = blade * 0.11f;
            PlaceRect(t.Hub, hubX, hubY, hr * 1.4f, hr * 1.4f, body);
            float bw = blade * 0.085f;
            for (int k = 0; k < 3; k++)
            {
                var b = t.Blades[k];
                b.enabled = true;
                b.transform.localPosition = new Vector3(hubX, hubY, 0f);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, angle + k * 120f);
                b.transform.localScale = new Vector3(bw / bladeSprite.bounds.size.x, blade / bladeSprite.bounds.size.y, 1f);
                b.color = k == 0 ? body : Color.Lerp(body, shade, 0.25f * k);
            }
        }

        void UpdateBuildings(bool strip, bool night, float left, float right, float turbineX)
        {
            double kwh = wind.Kwh;
            // u = kwh − i; x = poleX − u·Gap.
            double uMin = (poleX - (right + 2f)) / Gap;
            double uMax = (poleX - (left - 2f)) / Gap;
            long first = System.Math.Max(1L, (long)System.Math.Ceiling(kwh - uMax));
            long last = (long)System.Math.Floor(kwh - uMin);
            float scaleAll = strip ? 0.95f : 1f;
            var unlit = night ? UnlitNight : UnlitDay;
            var lit = WorldTheme.Surface(Color.white);
            bool backing = wind.SpinDirection < 0f;

            int used = 0;
            float sparkU = -1f;
            float sparkX = 0f, sparkTop = 0f;
            for (long i = first; i <= last && used < PoolSize; i++)
            {
                double u = kwh - i;
                var bld = wind.Identify(i);
                float x = poleX - (float)u * Gap + bld.Jitter * 0.12f * Gap;
                if (x < left - 2f || x > right + 2f) continue;

                var sr = buildings[used];
                var glow = glows[used];
                used++;
                var sprite = SpriteFor(bld);
                sr.enabled = true;
                sr.sprite = sprite != null ? sprite : whiteSprite;
                var bs = sr.sprite.bounds.size;
                float h = bld.Type.Height * scaleAll;
                float w = h * bs.x / Mathf.Max(0.001f, bs.y);
                if (w > Gap * 0.9f)
                {
                    h *= Gap * 0.9f / w;
                    w = Gap * 0.9f;
                }
                float s = h / Mathf.Max(0.001f, bs.y);
                sr.transform.localScale = new Vector3(s, s, 1f);
                sr.transform.localRotation = Quaternion.identity;
                sr.transform.localPosition = new Vector3(x, groundY + h / 2f - 0.02f, 0f);
                // Yeni gelen (sağdaki) arkada.
                sr.sortingOrder = 40 - used;

                // Bağlanma: u 0'ı geçince renkler açılır; ilk anda hafif parlama.
                float on = Mathf.Clamp01((float)(u + 0.03) / 0.06f);
                var c = Color.Lerp(unlit, lit, on);
                if (u >= 0 && u < 0.18) c = Color.Lerp(c, Color.white * 1.15f, (float)(1.0 - u / 0.18) * 0.35f);
                // Şeritte solda yazının altında, kartta sol kenarda söner.
                // Şeritte türbine varmadan söner (türbin önde, yazı solda).
                float fadeEdge = strip ? turbineX + 0.3f : left - 0.3f;
                c.a = Mathf.Clamp01((x - fadeEdge) / 1.2f);
                sr.color = c;
                if (c.a <= 0.01f) sr.enabled = false;

                // Gecede bağlı yapıların arkasında sıcak ışık halesi.
                glow.enabled = night && on > 0.5f && sr.enabled;
                if (glow.enabled)
                {
                    var g = Glow;
                    g.a = 0.22f * on * c.a;
                    PlaceRect(glow, x, groundY + h * 0.45f, w * 1.35f, h * 1.15f, g);
                }

                if (u >= 0 && u < 0.25 && (sparkU < 0 || u < sparkU) && !backing)
                {
                    sparkU = (float)u;
                    sparkX = x;
                    sparkTop = groundY + h;
                }
            }
            for (int i = used; i < PoolSize; i++)
            {
                buildings[i].enabled = false;
                glows[i].enabled = false;
            }

            // Kıvılcım: yeni bağlanan yapının çatısında birkaç sarı nokta dışa saçılır ve söner.
            for (int k = 0; k < SparkCount; k++)
            {
                sparks[k].enabled = sparkU >= 0f;
                if (!sparks[k].enabled) continue;
                float t = sparkU / 0.25f;
                float ang = (k / (float)SparkCount) * Mathf.PI * 2f + 0.4f;
                float r = 0.15f + t * 0.6f;
                var c = WorldTheme.Surface(Energy);
                c.a = 1f - t;
                float d = (strip ? 0.11f : 0.09f) * (1f - t * 0.5f);
                PlaceRect(sparks[k], sparkX + Mathf.Cos(ang) * r, sparkTop + 0.1f + Mathf.Sin(ang) * r * 0.7f, d, d, c);
            }
        }

        Sprite SpriteFor(GridCatalog.Building b)
        {
            if (b.IsSpecial)
            {
                int si = GridCatalog.SpecialIndex(b.Type.Id);
                if (specialSprites != null && si >= 0 && si < specialSprites.Length) return specialSprites[si];
                return null;
            }
            int hi = System.Array.IndexOf(GridCatalog.Homes, b.Type);
            if (homeSprites != null && hi >= 0 && hi < homeSprites.Length) return homeSprites[hi];
            return null;
        }

        static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        void PlaceRect(SpriteRenderer sr, float x, float y, float w, float h, Color color)
        {
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localRotation = Quaternion.identity;
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / b.x, h / b.y, 1f);
            sr.color = color;
        }

        void PlaceSpan(SpriteRenderer sr, float x0, float x1, float y0, float y1, Color color)
        {
            sr.enabled = x1 > x0 && y1 > y0;
            if (!sr.enabled) return;
            PlaceRect(sr, (x0 + x1) / 2f, (y0 + y1) / 2f, x1 - x0, y1 - y0, color);
        }

        void PlaceLine(SpriteRenderer sr, Vector2 a, Vector2 b, float width, Color color)
        {
            var d = b - a;
            float len = d.magnitude;
            sr.enabled = len > 0.01f;
            if (!sr.enabled) return;
            sr.transform.localPosition = (Vector3)((a + b) / 2f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var bs = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(len / bs.x, width / bs.y, 1f);
            sr.color = color;
        }
    }
}
