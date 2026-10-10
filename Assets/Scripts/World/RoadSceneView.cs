using System;
using UnityEngine;

namespace ScrollRoad
{
    // Oyun dünyasının görsel katmanı — minimalist, düz renkler:
    //   gökyüzü (kamera arka planı) → uzak tepeler → şehir silueti → yakın tepeler + zemin → yol → araba.
    // Parallax: yol çizgileri hızlı, yakın tepeler yavaş, uzak tepeler çok yavaş kayar; şehir ise
    // kaydırmayla değil İLERLEMEYLE büyür (yaklaştıkça). Şehrin ötesinde şehir sola akıp kaybolur.
    // Yol düz (2026-10-10, kullanıcı: kavis "hayal ettiğim gibi olmadı").
    // Masaüstü modunda (görev çubuğu şeridi) arka plan katmanları gizlenir, yol ince bir banda
    // iner, kamera araba + yolu şeride sığdırır (araba yüksekliğin ~%70'i) ve arka plan alfa-0
    // temizlenir (pencere saydamlığı ezilmesin).
    [DefaultExecutionOrder(50)]
    public class RoadSceneView : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Camera viewCamera;

        [Header("Görseller")]
        [SerializeField] Sprite carSprite;          // katalog yoksa yedek
        [SerializeField] CarCatalog carCatalog;     // garaj seçimi (Awake'te ve değişince uygulanır)
        [SerializeField] Sprite citySprite;
        [SerializeField] Sprite hillsSprite;
        [SerializeField] Material spriteMaterial; // URP Sprite-Unlit-Default (2D ışık gerektirmesin)

        [Header("Yerleşim (dünya birimi; normal kamera dikey yüksekliği 10 birim)")]
        [SerializeField] float cameraSize = 5f;
        [SerializeField] float roadTopY = -2.6f;
        [SerializeField] float roadHeight = 0.7f;
        [SerializeField] float carWidth = 1.7f;
        [SerializeField] float roadSegmentWidth = 0.35f;
        [SerializeField] float dashSpacing = 1.2f;

        [Header("Parallax (dünya birimi / km)")]
        [SerializeField] float roadUnitsPerKm = 60f;   // seyirde (60 km/sa) ≈ 1 birim/sn
        [SerializeField] float nearHillsUnitsPerKm = 8f;
        [SerializeField] float farHillsUnitsPerKm = 2.5f;

        [Header("Şehir")]
        [SerializeField] float cityMinHeight = 1.3f;
        [SerializeField] float cityMaxHeight = 4.4f;
        [SerializeField] float cityPassKm = 40f; // şehrin ötesinde bu kadar km'de ekrandan çıkar

        [Header("Masaüstü modu (görev çubuğu şeridi)")]
        [SerializeField] float companionRoadHeight = 0.14f;
        [SerializeField] float companionTopMargin = 0.16f;    // zıplama payı
        [SerializeField] float companionCarScreenX = 0.62f;   // araba şerit genişliğinin bu oranında (solda km yazısı)

        [Header("His")]
        [SerializeField] float stepBobImpulse = 0.9f;
        [SerializeField] float bobStiffness = 70f;
        [SerializeField] float bobDamping = 9f;
        [SerializeField] float uTurnFlipPerSec = 4f;
        [SerializeField] float biomeLengthKm = 1500f;

        // Gökyüzü biyom renkleri: mesafeye göre döngüsel (bandın son %15'inde yumuşak geçiş).
        static readonly Color[] BiomeColors =
        {
            new Color(0.84f, 0.93f, 0.95f), // açık gök mavisi
            new Color(0.95f, 0.93f, 0.84f), // buğday
            new Color(0.80f, 0.93f, 0.92f), // sahil turkuazı
            new Color(0.84f, 0.92f, 0.85f), // orman yeşilimsi
            new Color(0.89f, 0.87f, 0.95f), // alacakaranlık lila
            new Color(0.92f, 0.95f, 0.98f), // kar beyazı
        };

        static readonly Color SkyNearCityColor = new Color(0.99f, 0.86f, 0.70f);   // şehre yaklaştıkça gün batımı
        static readonly Color SecretSkyColor = new Color(0.17f, 0.11f, 0.25f);     // şehrin ötesi — belgelenmemiş
        static readonly Color CitySilhouetteColor = new Color(0.18f, 0.23f, 0.31f);
        static readonly Color RoadColor = new Color(0.22f, 0.23f, 0.27f);
        static readonly Color SecretRoadColor = new Color(0.34f, 0.26f, 0.45f); // koyu zeminde okunur kalsın
        static readonly Color DashColor = new Color(0.97f, 0.95f, 0.86f);
        static readonly Color DustColor = new Color(0.62f, 0.60f, 0.56f);

        static readonly Color[] ConfettiColors =
        {
            new Color(0.95f, 0.38f, 0.30f),
            new Color(0.98f, 0.80f, 0.25f),
            new Color(0.35f, 0.70f, 0.92f),
            new Color(0.45f, 0.80f, 0.50f),
            new Color(0.98f, 0.97f, 0.93f),
        };

        const int DustPoolSize = 32;
        const int ConfettiPoolSize = 48;
        const int HillTiles = 3;

        struct Particle
        {
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public float spin;
            public Color color;
        }

        Sprite whiteSprite;
        Sprite circleSprite;
        Transform worldRoot;
        Transform backdropRoot;
        SpriteRenderer groundFill;
        SpriteRenderer[] farHills;
        SpriteRenderer[] nearHills;
        SpriteRenderer cityRenderer;
        Transform roadRoot;
        SpriteRenderer[] roadSegments = Array.Empty<SpriteRenderer>();
        SpriteRenderer[] dashes = Array.Empty<SpriteRenderer>();
        SpriteRenderer carRenderer;
        Particle[] dust;
        Particle[] confetti;
        int nextDust;

        bool facingReverse;
        float visualFacing = 1f; // +1 sağa (ileri), -1 sola (geri); U-dönüşünde yumuşakça geçer
        float bobOffset;
        float bobVelocity;
        float dustTimer;
        float secretBlend;

        bool IsCompanion => companionWindow != null && companionWindow.IsCompanionMode;
        float RoadH => IsCompanion ? companionRoadHeight : roadHeight;
        float RoadCenterY => roadTopY - RoadH / 2f;
        float CarHeight => carSprite != null ? carWidth * carSprite.bounds.size.y / carSprite.bounds.size.x : carWidth * 0.45f;
        // Şeritte yol ince: araba yola gömülmeden, tekerlekleri yolun üstünde durur.
        float CarCenterY => IsCompanion ? roadTopY + CarHeight * 0.5f - 0.03f : RoadCenterY + CarHeight * 0.5f - 0.12f;
        float DashHeight => IsCompanion ? 0.035f : 0.07f;

        // Test/doğrulama için okunabilir durum.
        public float CityHeightUnits { get; private set; }
        public Color CurrentSkyColor { get; private set; }

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = cameraSize;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.transform.position = new Vector3(0f, 0f, -10f);

            // Texture2D.whiteTexture 4x4 — pixelsPerUnit=4 ile 1x1 dünya birimlik beyaz kare.
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circleSprite = CreateCircleSprite();
            if (carCatalog != null && carCatalog.Selected != null) carSprite = carCatalog.Selected.sprite;

            Build();
            CarCatalog.SelectionChanged += HandleCarSelectionChanged;

            drive.OnDirectionChanged += HandleDirectionChanged;
            drive.OnStep += HandleStep;
            drive.OnReachedCity += HandleCityReached;
        }

        void OnDestroy()
        {
            CarCatalog.SelectionChanged -= HandleCarSelectionChanged;
            if (drive == null) return;
            drive.OnDirectionChanged -= HandleDirectionChanged;
            drive.OnStep -= HandleStep;
            drive.OnReachedCity -= HandleCityReached;
        }

        void HandleStep() => bobVelocity += stepBobImpulse;

        void HandleCarSelectionChanged()
        {
            var entry = carCatalog != null ? carCatalog.Selected : null;
            if (entry == null || entry.sprite == null) return;
            carSprite = entry.sprite;
            carRenderer.sprite = carSprite;
            bobVelocity += stepBobImpulse * 1.5f; // yeni araç "yerine oturur"
        }

        void HandleDirectionChanged()
        {
            bobVelocity += stepBobImpulse * 2f;
            for (int i = 0; i < 6; i++) SpawnDust();
        }

        void HandleCityReached()
        {
            bobVelocity += stepBobImpulse * 3f;
            SpawnConfettiBurst();
        }

        // ---------------------------------------------------------------- kurulum

        void Build()
        {
            worldRoot = new GameObject("World").transform;
            worldRoot.SetParent(transform, false);

            backdropRoot = new GameObject("Backdrop").transform;
            backdropRoot.SetParent(worldRoot, false);

            farHills = CreateHillLayer("FarHills", 1);
            cityRenderer = CreateRenderer("City", citySprite != null ? citySprite : whiteSprite, 2, backdropRoot);
            nearHills = CreateHillLayer("NearHills", 3);
            groundFill = CreateRenderer("Ground", whiteSprite, 4, backdropRoot);

            roadRoot = new GameObject("Road").transform;
            roadRoot.SetParent(worldRoot, false);

            carRenderer = CreateRenderer("Car", carSprite != null ? carSprite : whiteSprite, 40, worldRoot);

            dust = CreatePool("Dust", DustPoolSize, circleSprite, 35);
            confetti = CreatePool("Confetti", ConfettiPoolSize, whiteSprite, 45);
        }

        SpriteRenderer[] CreateHillLayer(string name, int order)
        {
            var layer = new SpriteRenderer[HillTiles];
            for (int i = 0; i < HillTiles; i++)
            {
                layer[i] = CreateRenderer($"{name}{i}", hillsSprite != null ? hillsSprite : whiteSprite, order, backdropRoot);
            }
            return layer;
        }

        Particle[] CreatePool(string name, int size, Sprite sprite, int order)
        {
            var root = new GameObject(name).transform;
            root.SetParent(worldRoot, false);
            var pool = new Particle[size];
            for (int i = 0; i < size; i++)
            {
                var sr = CreateRenderer($"{name}{i}", sprite, order, root);
                sr.enabled = false;
                pool[i].renderer = sr;
            }
            return pool;
        }

        SpriteRenderer CreateRenderer(string name, Sprite sprite, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
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

        // ---------------------------------------------------------------- kare döngüsü

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool paused = Time.timeScale <= 0f;

            if (drive.State != RoadDrive.DriveState.Idle) facingReverse = drive.State == RoadDrive.DriveState.Reverse;
            visualFacing = Mathf.MoveTowards(visualFacing, facingReverse ? -1f : 1f, uTurnFlipPerSec * dt);

            if (!paused)
            {
                bobVelocity += (-bobOffset * bobStiffness - bobVelocity * bobDamping) * dt;
                bobOffset = Mathf.Clamp(bobOffset + bobVelocity * dt, -0.12f, 0.25f);

                // Toz hıza bağlı sürekli emisyon: seyirde seyrek, boost'ta yoğun.
                float speed = Mathf.Abs(drive.CurrentSpeedKmh);
                if (speed > 5f)
                {
                    dustTimer += dt;
                    float interval = Mathf.Clamp(14f / speed, 0.05f, 1.2f);
                    while (dustTimer >= interval)
                    {
                        dustTimer -= interval;
                        SpawnDust();
                    }
                }
            }
        }

        void LateUpdate()
        {
            bool companion = IsCompanion;

            // Kamera: normalde sabit geniş çerçeve; şeritte yolun altından arabanın üstüne kadar,
            // araba yatayda companionCarScreenX'te (solda km yazısına yer kalsın).
            float size = cameraSize, camX = 0f, camY = 0f;
            if (companion)
            {
                float bottom = roadTopY - RoadH - 0.03f;
                float top = CarCenterY + CarHeight * 0.5f + companionTopMargin;
                size = (top - bottom) / 2f;
                camY = (top + bottom) / 2f;
                camX = -(companionCarScreenX - 0.5f) * 2f * size * viewCamera.aspect;
            }
            viewCamera.orthographicSize = size;
            viewCamera.transform.position = new Vector3(camX, camY, -10f);

            float halfW = size * viewCamera.aspect;

            var sky = EvaluateSky();
            CurrentSkyColor = sky;
            viewCamera.backgroundColor = companion ? new Color(0f, 0f, 0f, 0f) : sky;

            backdropRoot.gameObject.SetActive(!companion);
            if (!companion) LayoutBackdrop(sky, cameraSize * viewCamera.aspect);

            LayoutRoad(camX, halfW);
            LayoutDashes(camX, halfW);
            LayoutCar();
            UpdateParticles(dust, Time.deltaTime, 4f, 0f, isConfetti: false);
            UpdateParticles(confetti, Time.deltaTime, 3.5f, 0.6f, isConfetti: true);
        }

        // Renk katmanları: biyom (mesafeye göre döngüsel) → şehir yaklaşımı gün batımı (progress²)
        // → dünya teması (WorldTheme) → gizli bölge morunun yumuşak karışımı.
        Color EvaluateSky()
        {
            double d = Math.Max(0.0, drive.CurrentDistanceKmPrecise);
            double band = d / Math.Max(1f, biomeLengthKm);
            int index = (int)Math.Floor(band);
            float t = Mathf.InverseLerp(0.85f, 1f, (float)(band - index));
            var color = Color.Lerp(BiomeColors[index % BiomeColors.Length], BiomeColors[(index + 1) % BiomeColors.Length], t);

            float progress = Mathf.Clamp01((float)(d / drive.CityDistanceKm));
            // Gizli bölge tonu mesafeyle gelir (şehrin 3-8 km ötesi): varış anı gün batımında kalsın.
            float beyond = (float)(d - drive.CityDistanceKm);
            secretBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 8f, beyond));
            color = Color.Lerp(color, SkyNearCityColor, progress * progress * (1f - secretBlend));
            // Tema yalnız normal dünyayı boyar; gizli bölgenin moru her temada aynı (kural dışı yer).
            return Color.Lerp(WorldTheme.Sky(color), SecretSkyColor, secretBlend);
        }

        void LayoutBackdrop(Color sky, float halfW)
        {
            double d = drive.CurrentDistanceKmPrecise;
            var farColor = Shade(sky, 0.90f);
            var nearColor = Shade(sky, 0.78f);

            // Uzak tepeler: geniş, alçak, açık ton.
            LayoutHills(farHills, d * farHillsUnitsPerKm, halfW, bottomY: -2.45f, height: 2.5f, width: 26f, farColor, phase: 0.37f);
            // Yakın tepeler + altında aynı renkte zemin dolgusu (yol bu zeminin üstünde).
            LayoutHills(nearHills, d * nearHillsUnitsPerKm, halfW, bottomY: -2.35f, height: 1.35f, width: 21f, nearColor, phase: 0f);

            groundFill.color = nearColor;
            float groundTop = -2.3f;
            float groundBottom = -cameraSize - 0.5f;
            groundFill.transform.localPosition = new Vector3(0f, (groundTop + groundBottom) / 2f, 0f);
            groundFill.transform.localScale = new Vector3(halfW * 2f + 2f, groundTop - groundBottom, 1f);

            LayoutCity(sky, halfW);
        }

        void LayoutHills(SpriteRenderer[] tiles, double scroll, float halfW, float bottomY, float height, float width, Color color, float phase)
        {
            float offset = (float)((scroll + phase * width) % width);
            if (offset < 0f) offset += width;
            for (int i = 0; i < tiles.Length; i++)
            {
                var sr = tiles[i];
                var b = sr.sprite.bounds.size;
                // Döşeme i, [-halfW - offset + i·w, ... + w] aralığını kaplar (offset ∈ [0, w)).
                float x = -halfW - offset + (i + 0.5f) * width;
                sr.transform.localPosition = new Vector3(x, bottomY + height / 2f, 0f);
                sr.transform.localScale = new Vector3(width / b.x * 1.002f, height / b.y, 1f); // %0.2 bindirme: ek yeri çizgisi olmasın
                sr.color = color;
            }
        }

        void LayoutCity(Color sky, float halfW)
        {
            if (citySprite == null) { cityRenderer.enabled = false; return; }

            double d = drive.CurrentDistanceKmPrecise;
            float progress = Mathf.Clamp01((float)(d / drive.CityDistanceKm));
            float beyond = (float)Math.Max(0.0, d - drive.CityDistanceKm);

            float height = Mathf.Lerp(cityMinHeight, cityMaxHeight, Mathf.Pow(progress, 1.4f));
            var b = citySprite.bounds.size;
            float width = height * b.x / b.y;
            CityHeightUnits = height;

            // Yaklaşırken sağdan merkeze doğru hafifçe kayar; ötesinde sola akıp ekrandan çıkar.
            float x = Mathf.Lerp(halfW * 0.5f, halfW * 0.15f, progress);
            float pass = Mathf.Clamp01(beyond / Mathf.Max(1f, cityPassKm));
            x = Mathf.Lerp(x, -halfW - width * 0.6f, pass * pass * (3f - 2f * pass));
            cityRenderer.enabled = pass < 1f;

            float bottomY = -2.3f; // taban yakın tepelerin/zeminin arkasında kalsın
            cityRenderer.transform.localPosition = new Vector3(x, bottomY + height / 2f, 0f);
            cityRenderer.transform.localScale = new Vector3(height / b.y, height / b.y, 1f);

            // Atmosfer: uzaktayken gökyüzüne karışık soluk, yaklaştıkça koyu siluet.
            cityRenderer.color = Color.Lerp(sky, WorldTheme.CitySilhouette(CitySilhouetteColor), Mathf.Lerp(0.4f, 1f, progress));
        }

        void LayoutRoad(float camX, float halfW)
        {
            float seg = Mathf.Max(0.1f, roadSegmentWidth);
            int needed = Mathf.CeilToInt(halfW * 2f / seg) + 3;
            if (roadSegments.Length < needed) roadSegments = Grow(roadSegments, needed, "RoadSeg", 20, roadRoot);

            var color = WorldTheme.Surface(Color.Lerp(RoadColor, SecretRoadColor, secretBlend));
            float startX = camX - halfW - seg;
            for (int i = 0; i < roadSegments.Length; i++)
            {
                var sr = roadSegments[i];
                sr.enabled = i < needed;
                if (!sr.enabled) continue;
                float x = startX + i * seg;
                sr.color = color;
                sr.transform.localPosition = new Vector3(x, RoadCenterY, 0f);
                sr.transform.localScale = new Vector3(seg * 1.02f, RoadH, 1f); // hafif bindirme: ek yeri çizgisi olmasın
            }
        }

        void LayoutDashes(float camX, float halfW)
        {
            int needed = Mathf.CeilToInt(halfW * 2f / dashSpacing) + 3;
            var dashColor = WorldTheme.Surface(DashColor);
            if (dashes.Length < needed) dashes = Grow(dashes, needed, "Dash", 30, roadRoot);

            // double ile mod: büyük mesafelerde float çözünürlüğü kaymayı titretmesin.
            float offset = (float)((drive.CurrentDistanceKmPrecise * roadUnitsPerKm) % dashSpacing);
            float startX = camX - halfW - dashSpacing;
            for (int i = 0; i < dashes.Length; i++)
            {
                var sr = dashes[i];
                sr.enabled = i < needed;
                if (!sr.enabled) continue;
                float x = startX + i * dashSpacing - offset;
                sr.color = dashColor;
                sr.transform.localPosition = new Vector3(x, RoadCenterY, 0f);
                sr.transform.localScale = new Vector3(dashSpacing * 0.45f, DashHeight, 1f);
            }
        }

        SpriteRenderer[] Grow(SpriteRenderer[] pool, int size, string name, int order, Transform parent)
        {
            var grown = new SpriteRenderer[size];
            for (int i = 0; i < size; i++)
            {
                grown[i] = i < pool.Length ? pool[i] : CreateRenderer($"{name}{i}", whiteSprite, order, parent);
            }
            return grown;
        }

        void LayoutCar()
        {
            var t = carRenderer.transform;
            t.localPosition = new Vector3(0f, CarCenterY + bobOffset, 0f);
            float scale = carWidth / carRenderer.sprite.bounds.size.x;
            if (carSprite == null)
            {
                t.localScale = new Vector3(carWidth * visualFacing, carWidth * 0.45f, 1f);
                carRenderer.color = new Color(0.93f, 0.42f, 0.33f);
                return;
            }
            // Eksi X ölçek = yatay flip; ortadan geçerken araç bir an "kenardan" görünür (U-dönüşü hissi).
            t.localScale = new Vector3(scale * visualFacing, scale, 1f);
            carRenderer.color = Color.white;
        }

        // ---------------------------------------------------------------- parçacıklar

        void SpawnDust()
        {
            float dir = facingReverse ? -1f : 1f;
            ref var p = ref dust[nextDust];
            nextDust = (nextDust + 1) % dust.Length;

            p.maxLife = UnityEngine.Random.Range(0.35f, 0.6f);
            p.life = p.maxLife;
            p.velocity = new Vector2(-dir * UnityEngine.Random.Range(0.6f, 1.4f), UnityEngine.Random.Range(0.4f, 1.0f));
            p.color = WorldTheme.Surface(DustColor);
            p.renderer.enabled = true;
            float x = -dir * carWidth * 0.42f + UnityEngine.Random.Range(-0.05f, 0.05f);
            p.renderer.transform.localPosition = new Vector3(x, RoadCenterY - 0.02f, 0f);
            p.renderer.transform.localRotation = Quaternion.identity;
        }

        void SpawnConfettiBurst()
        {
            for (int i = 0; i < confetti.Length; i++)
            {
                ref var p = ref confetti[i];
                p.maxLife = UnityEngine.Random.Range(1.4f, 2.4f);
                p.life = p.maxLife;
                p.velocity = new Vector2(UnityEngine.Random.Range(-2.5f, 2.5f), UnityEngine.Random.Range(2.5f, 5.5f));
                p.spin = UnityEngine.Random.Range(-360f, 360f);
                p.color = ConfettiColors[UnityEngine.Random.Range(0, ConfettiColors.Length)];
                p.renderer.enabled = true;
                var t = p.renderer.transform;
                t.localPosition = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), CarCenterY + 0.3f, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                float s = UnityEngine.Random.Range(0.07f, 0.13f);
                t.localScale = new Vector3(s, s * 0.6f, 1f);
            }
        }

        static void UpdateParticles(Particle[] pool, float dt, float gravity, float drag, bool isConfetti)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                ref var p = ref pool[i];
                if (!p.renderer.enabled) continue;

                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.renderer.enabled = false;
                    continue;
                }

                p.velocity.y -= gravity * dt;
                p.velocity.x *= 1f - drag * dt;
                var t = p.renderer.transform;
                t.localPosition += (Vector3)(p.velocity * dt);
                float frac = p.life / p.maxLife;

                if (isConfetti)
                {
                    t.Rotate(0f, 0f, p.spin * dt);
                    p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, Mathf.Clamp01(frac * 3f));
                }
                else
                {
                    p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, frac * 0.7f);
                    float s = Mathf.Lerp(0.05f, 0.17f, frac);
                    t.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}
