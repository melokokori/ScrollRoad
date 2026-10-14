using UnityEngine;

namespace ScrollRoad
{
    // Olta ortamının sahnesi (2026-10-13 prototip): solda iskelede balıkçı, sağda göl ve suyun altının
    // kesiti; yem ve balık su içinde izlenir. Düz renk, Yol'un görsel diliyle (gökyüzü/tepeler
    // Lakeshore bölgesinin renkleri, dünya teması uygulanır). Balıkçı + iskele tek görsel (Gemini),
    // balıklar ayrı görseller; misina, yem, su ve sazlar kodla çizilir.
    // Görev çubuğu şeridinde: arka plan saydam, su çizgisi yatay; balıkçı, misina, yem ve balık.
    // Mekâna göre (2026-10-14): Göl = iskele + sazlar, durgun su; Nehir = kayalık kıyıda iskelesiz
    // balıkçı (göğüs çizmeli), akıntı çizgileri, dipte taşlar, çam ormanı tonları. Kıyı taşları ve akıntı
    // kodla çizilir; balıkçı her mekânda tek görsel. Kıyı = beton mendirekte balıkçı, deniz mavisi su,
    // hafif dalga (yüzey ve akıntı çizgileri ağır ağır salınır), dipte kaya blokları, uzakta burun.
    // Açık deniz = kayıkta balıkçı (kayık + adam tek görsel), dalgayla inip kalkar; kara ve dip yok.
    // Buz deliği = buz tabakası (kodla) üstünde taburede balıkçı; yem delikten DİKEY iner ve dikey sarılır.
    // Yalnız Olta seçiliyken çizer ve kamerayı yönetir (Yol'da RoadSceneView yönetir).
    public class FishingSceneView : MonoBehaviour
    {
        [SerializeField] FishingController fishing;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Camera viewCamera;
        [SerializeField] Sprite[] fishermanSprites; // FishCatalog.Spots sırasıyla
        [SerializeField] Sprite[] fishSprites;      // FishCatalog.All sırasıyla
        [SerializeField] Sprite hillsSprite;
        [SerializeField] Material spriteMaterial;

        [Header("Kart")]
        [SerializeField] float cardViewTop = 2.4f;
        [SerializeField] float cardViewBottom = -4.1f;
        [SerializeField] float waterY = -0.5f;
        [SerializeField] float bedY = -3.7f;
        [SerializeField] float fishermanHeight = 2.9f;
        [SerializeField] float cardPierEndX = -3.1f; // iskele ucu (sol üstteki HUD plakasının sağında)

        [Header("Görev çubuğu")]
        [SerializeField] float stripFishermanHeight = 2.4f;
        [SerializeField] float stripWaterDepth = 0.95f;
        [SerializeField] float stripFishermanScreenX = 0.30f; // solda kg yazısına yer kalsın
        [SerializeField] float stripFishScale = 0.8f;

        // Mekânın görünümü. Balıkçı görselinin ölçüleri normalize (sol-alt köken): kamış ucu, ayak/iskele
        // üstü, iskele direkleri, suyun başladığı yer (iskele ucu ya da öndeki ayak).
        sealed class Look
        {
            public Vector2 RodTip;
            public float DeckTop;
            public Vector2[] Posts;
            public float PierEnd;
            public float DeckAbove = 0.3f;  // ayak/iskele üstünün sudan yüksekliği (kart)
            public float HeightScale = 1f;
            public float CardShiftX;        // kartta balıkçıyı sağa kaydır (sol üst HUD plakasının altında kalmasın) // görsel boyu / göldeki balıkçı+iskele boyu
            public bool Reeds, Flow, Stones, Breakwater, Swell, Boat, Ice, NoBed;
            public float FarHillH, NearHillH;
            public Color Sky, FarLand, NearLand, WaterTop, WaterDeep, SurfaceLine, Bed, Reed, Stone;
        }

        // Göl: Lakeshore (RouteMap) tonları; fisherman.png.
        static readonly Look LakeLook = new Look
        {
            RodTip = new Vector2(0.994f, 0.970f),
            DeckTop = 0.28f,
            Posts = new[] { new Vector2(0.045f, 0.092f), new Vector2(0.531f, 0.578f) },
            PierEnd = 0.62f,
            Reeds = true,
            FarHillH = 1.6f, NearHillH = 1.0f,
            Sky = new Color(0.80f, 0.93f, 0.94f),
            FarLand = new Color(0.60f, 0.78f, 0.84f),
            NearLand = new Color(0.54f, 0.72f, 0.60f),
            WaterTop = new Color(0.36f, 0.64f, 0.74f),
            WaterDeep = new Color(0.20f, 0.42f, 0.54f),
            SurfaceLine = new Color(0.78f, 0.91f, 0.93f),
            Bed = new Color(0.62f, 0.56f, 0.42f),
            Reed = new Color(0.32f, 0.50f, 0.34f),
            Stone = new Color(0.58f, 0.58f, 0.54f),
        };

        // Nehir: Pine Forest (RouteMap) tonları, daha serin ve yeşil su; fisherman_river.png.
        static readonly Look RiverLook = new Look
        {
            RodTip = new Vector2(0.995f, 0.977f),
            DeckTop = 0f,
            HeightScale = 0.72f, // iskelesiz boydan figür: göldeki adamla aynı boyda görünsün
            Posts = new Vector2[0],
            PierEnd = 0.34f,
            Flow = true,
            Stones = true,
            FarHillH = 2.1f, NearHillH = 1.5f,
            Sky = new Color(0.84f, 0.92f, 0.88f),
            FarLand = new Color(0.47f, 0.62f, 0.52f),
            NearLand = new Color(0.31f, 0.48f, 0.38f),
            WaterTop = new Color(0.33f, 0.58f, 0.60f),
            WaterDeep = new Color(0.17f, 0.36f, 0.42f),
            SurfaceLine = new Color(0.80f, 0.92f, 0.90f),
            Bed = new Color(0.50f, 0.49f, 0.44f),
            Reed = new Color(0.30f, 0.46f, 0.34f),
            Stone = new Color(0.60f, 0.60f, 0.57f),
        };

        // Kıyı: sıcak deniz ışığı, turkuaz-lacivert su, beton mendirek; fisherman_coast.png.
        static readonly Look CoastLook = new Look
        {
            RodTip = new Vector2(0.992f, 0.996f),
            DeckTop = 0f,
            HeightScale = 0.72f,
            Posts = new Vector2[0],
            PierEnd = 0.36f,
            Flow = true,
            Swell = true,
            Breakwater = true,
            DeckAbove = 0.6f, // mendirek sudan belirgin yüksek
            FarHillH = 1.1f, NearHillH = 0.55f,
            Sky = new Color(0.86f, 0.93f, 0.97f),
            FarLand = new Color(0.66f, 0.74f, 0.80f),
            NearLand = new Color(0.82f, 0.76f, 0.60f),
            WaterTop = new Color(0.22f, 0.62f, 0.72f),
            WaterDeep = new Color(0.10f, 0.30f, 0.48f),
            SurfaceLine = new Color(0.86f, 0.95f, 0.97f),
            Bed = new Color(0.78f, 0.72f, 0.56f),
            Reed = new Color(0.30f, 0.46f, 0.34f),
            Stone = new Color(0.74f, 0.73f, 0.70f), // mendirek betonu
        };

        // Açık deniz: derin lacivert, ufukta silik ada; fisherman_boat.png (DeckTop = kayığın su çizgisi).
        static readonly Look OpenSeaLook = new Look
        {
            RodTip = new Vector2(0.902f, 0.99f),
            DeckTop = 0.08f,
            DeckAbove = 0f,
            HeightScale = 0.92f,
            Posts = new Vector2[0],
            PierEnd = 0.95f,
            Flow = true,
            Swell = true,
            Boat = true,
            CardShiftX = 1.5f, // geniş kayık: adam plakanın sağında dursun
            NoBed = true,
            FarHillH = 0.3f, NearHillH = 0f,
            Sky = new Color(0.83f, 0.91f, 0.97f),
            FarLand = new Color(0.64f, 0.74f, 0.82f),
            NearLand = new Color(0.64f, 0.74f, 0.82f),
            WaterTop = new Color(0.12f, 0.44f, 0.64f),
            WaterDeep = new Color(0.04f, 0.15f, 0.33f),
            SurfaceLine = new Color(0.84f, 0.93f, 0.98f),
            Bed = new Color(0.04f, 0.15f, 0.33f),
            Reed = new Color(0.30f, 0.46f, 0.34f),
            Stone = new Color(0.60f, 0.60f, 0.57f),
        };

        // Buz deliği: kış ışığı, karlı tepeler, buzun altında koyu su; fisherman_ice.png (taburede oturur).
        // DeckAbove = buz kalınlığı (balıkçı buzun üstünde), Stone = buz, NearLand = kar.
        static readonly Look IceLook = new Look
        {
            RodTip = new Vector2(0.995f, 0.6f),
            DeckTop = 0f,
            DeckAbove = 0.5f,
            HeightScale = 0.55f,
            Posts = new Vector2[0],
            PierEnd = 0.995f,
            Ice = true,
            FarHillH = 1.4f, NearHillH = 0.8f,
            Sky = new Color(0.88f, 0.92f, 0.96f),
            FarLand = new Color(0.80f, 0.86f, 0.92f),
            NearLand = new Color(0.95f, 0.97f, 0.99f),
            WaterTop = new Color(0.18f, 0.32f, 0.42f),
            WaterDeep = new Color(0.07f, 0.14f, 0.22f),
            SurfaceLine = new Color(0.66f, 0.80f, 0.88f),
            Bed = new Color(0.22f, 0.24f, 0.26f),
            Reed = new Color(0.30f, 0.46f, 0.34f),
            Stone = new Color(0.78f, 0.88f, 0.95f),
        };

        static Look LookFor(FishingSpot spot) =>
            spot == FishCatalog.River ? RiverLook
            : spot == FishCatalog.Coast ? CoastLook
            : spot == FishCatalog.OpenSea ? OpenSeaLook
            : spot == FishCatalog.IceHole ? IceLook
            : LakeLook;

        static readonly Color PostColor = new Color(0.70f, 0.50f, 0.32f);
        static readonly Color LineColor = new Color(0.13f, 0.15f, 0.18f, 0.85f);
        static readonly Color LureColor = new Color(0.86f, 0.88f, 0.90f);
        static readonly Color LureTail = new Color(0.86f, 0.30f, 0.26f);

        const int WaterBands = 4;
        const int ReedCount = 7;
        const int DropCount = 14;
        const int MaxPosts = 2;
        const int BankStoneCount = 4; // balıkçının altındaki ve önündeki kayalar
        const int BedStoneCount = 6;
        const int StreakCount = 9;
        const float FlowSpeed = 0.9f; // akıntı çizgileri, dünya birimi/sn (sağdan sola)

        Transform root;
        Sprite whiteSprite, circleSprite;
        SpriteRenderer farHills, nearHills, surface, bed, fisherman, line, lure, lureTail, fish, bankTop, bankUnder;
        SpriteRenderer[] bands, posts, reeds, drops, bankStones, bedStones, streaks;
        float[] streakX;
        SpriteRenderer iceLeft, iceRight, snowLeft, snowRight, holeWater;
        float holeX, holeSurf; // Buz deliği: deliğin ortası ve içindeki su yüzeyi
        Look look = LakeLook;
        Sprite fishermanSprite;
        float[] reedPhase;
        Vector2[] dropVel;
        float[] dropLife;
        bool lastSplashCast, pendingLandSplash;
        FishingController.Phase lastPhase;
        float sink;     // 0..1 yem suya düştükten sonra batar
        float wiggle;   // takılı balığın kıvranması

        bool IsCompanion => companionWindow != null && companionWindow.IsCompanionMode;

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circleSprite = CreateCircleSprite();
            Build();
        }

        void Build()
        {
            root = new GameObject("Lake").transform;
            root.SetParent(transform, false);

            farHills = Create("FarHills", hillsSprite != null ? hillsSprite : whiteSprite, 1);
            nearHills = Create("NearHills", hillsSprite != null ? hillsSprite : whiteSprite, 2);
            bands = new SpriteRenderer[WaterBands];
            for (int i = 0; i < WaterBands; i++) bands[i] = Create($"Water{i}", whiteSprite, 10 + i);
            bed = Create("Bed", whiteSprite, 15);
            bankUnder = Create("BankUnder", whiteSprite, 17);
            bankTop = Create("BankTop", whiteSprite, 17);
            reeds = new SpriteRenderer[ReedCount];
            reedPhase = new float[ReedCount];
            for (int i = 0; i < ReedCount; i++)
            {
                reeds[i] = Create($"Reed{i}", whiteSprite, 16);
                reedPhase[i] = i * 1.7f;
            }
            bedStones = new SpriteRenderer[BedStoneCount];
            for (int i = 0; i < BedStoneCount; i++) bedStones[i] = Create($"BedStone{i}", circleSprite, 16);
            streaks = new SpriteRenderer[StreakCount];
            streakX = new float[StreakCount];
            for (int i = 0; i < StreakCount; i++)
            {
                streaks[i] = Create($"Streak{i}", whiteSprite, 14);
                streakX[i] = i / (float)StreakCount;
            }
            surface = Create("Surface", whiteSprite, 18);
            posts = new SpriteRenderer[MaxPosts];
            for (int i = 0; i < posts.Length; i++) posts[i] = Create($"Post{i}", whiteSprite, 19);
            bankStones = new SpriteRenderer[BankStoneCount];
            for (int i = 0; i < BankStoneCount; i++) bankStones[i] = Create($"BankStone{i}", circleSprite, 19);
            iceLeft = Create("IceLeft", whiteSprite, 19);
            iceRight = Create("IceRight", whiteSprite, 19);
            snowLeft = Create("SnowLeft", whiteSprite, 20);
            snowRight = Create("SnowRight", whiteSprite, 20);
            holeWater = Create("HoleWater", whiteSprite, 18);
            fish = Create("Fish", whiteSprite, 22);
            line = Create("Line", whiteSprite, 24);
            lure = Create("Lure", circleSprite, 25);
            lureTail = Create("LureTail", circleSprite, 26);
            fisherman = Create("Fisherman", whiteSprite, 30);
            drops = new SpriteRenderer[DropCount];
            dropVel = new Vector2[DropCount];
            dropLife = new float[DropCount];
            for (int i = 0; i < DropCount; i++)
            {
                drops[i] = Create($"Drop{i}", circleSprite, 27);
                drops[i].enabled = false;
            }
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

        bool Show()
        {
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Fishing;
            if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
            return active;
        }

        // ---------------------------------------------------------------- kare

        void LateUpdate()
        {
            if (!Show() || fishing == null) return;
            bool strip = IsCompanion;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            var spot = fishing.Spot;
            look = LookFor(spot);
            int spotIndex = FishCatalog.SpotIndex(spot);
            fishermanSprite = fishermanSprites != null && spotIndex >= 0 && spotIndex < fishermanSprites.Length && fishermanSprites[spotIndex] != null
                ? fishermanSprites[spotIndex] : whiteSprite;
            fisherman.sprite = fishermanSprite;

            // Yerleşim: su çizgisi, dip, balıkçı boyu; kamera kartta sabit bant, şeritte balıkçının
            // tepesinden suyun dibine.
            float wY = strip ? 0f : waterY;
            float bY = strip ? -stripWaterDepth : bedY;
            float manH = (strip ? stripFishermanHeight : fishermanHeight) * look.HeightScale;
            float size, camY, camX;
            float aspect = Mathf.Max(0.1f, viewCamera.aspect);
            float manW = manH * (fishermanSprite != null ? fishermanSprite.bounds.size.x / fishermanSprite.bounds.size.y : 1f);
            float manLeft;
            if (strip)
            {
                float bottom = bY, top = wY + StripDeck(look) + manH * (1f - look.DeckTop) + 0.25f;
                float band = 1f - (companionWindow != null ? companionWindow.StripOverflowFraction : 0f);
                size = (top - bottom) / 2f / Mathf.Max(0.2f, band);
                camY = bottom + size;
                float halfW0 = size * aspect;
                // Balıkçının iskele ucu ekranın stripFishermanScreenX'inde.
                camX = 0f;
                manLeft = -halfW0 + stripFishermanScreenX * 2f * halfW0 - manW * look.PierEnd;
            }
            else
            {
                size = (cardViewTop - cardViewBottom) / 2f;
                camY = (cardViewTop + cardViewBottom) / 2f;
                camX = 0f;
                manLeft = cardPierEndX - manW * look.PierEnd + look.CardShiftX;
            }
            viewCamera.orthographicSize = size;
            viewCamera.transform.position = new Vector3(camX, camY, -10f);
            float halfW = size * aspect;
            float left = camX - halfW, right = camX + halfW;

            var sky = WorldTheme.Sky(look.Sky);
            sky.a = 1f;
            viewCamera.backgroundColor = strip ? new Color(0f, 0f, 0f, 0f) : sky;

            // Tepeler (yalnız kart).
            farHills.enabled = !strip && look.FarHillH > 0f;
            nearHills.enabled = !strip && look.NearHillH > 0f;
            if (!strip)
            {
                PlaceStretched(farHills, camX, wY + 0.55f, halfW * 2.2f, look.FarHillH, Opaque(WorldTheme.Land(look.FarLand)));
                PlaceStretched(nearHills, camX + 2.5f, wY + 0.35f, halfW * 2.6f, look.NearHillH, Opaque(WorldTheme.Land(look.NearLand)));
            }

            // Su: yüzeyden dibe açıktan koyuya düz bantlar; yüzeyde ince açık çizgi.
            float waterH = wY - bY;
            for (int i = 0; i < WaterBands; i++)
            {
                float t = i / (float)(WaterBands - 1);
                var c = Opaque(WorldTheme.Surface(ThemeWater(Color.Lerp(look.WaterTop, look.WaterDeep, t), sky)));
                if (strip) c.a = 0.92f;
                float top = wY - waterH * i / WaterBands;
                PlaceRect(bands[i], camX, top - waterH / WaterBands / 2f, halfW * 2f + 0.2f, waterH / WaterBands + 0.01f, c);
            }
            float swell = look.Swell ? Mathf.Sin(Time.time * 1.1f) * (strip ? 0.02f : 0.04f) : 0f;
            PlaceRect(surface, camX, wY + swell, halfW * 2f + 0.2f, strip ? 0.07f : 0.05f, Opaque(WorldTheme.Surface(ThemeWater(look.SurfaceLine, sky))));
            bed.enabled = !strip && !look.NoBed;
            if (bed.enabled) PlaceRect(bed, camX, bY - 0.25f, halfW * 2f + 0.2f, 0.5f, Opaque(WorldTheme.Land(look.Bed)));

            // Akıntı (Nehir): suyun üst yarısında sağdan sola kayan ince açık çizgiler.
            for (int i = 0; i < StreakCount; i++)
            {
                streaks[i].enabled = look.Flow;
                if (!look.Flow) continue;
                // Kıyıda akıntı yok: dalga çizgileri yavaşça kıyıya (sola) sürüklenir.
                float speed = look.Swell ? 0.25f : FlowSpeed;
                streakX[i] = Mathf.Repeat(streakX[i] - dt * speed * (1f + (i % 3) * 0.25f) / (halfW * 2f), 1f);
                float depth01 = (i * 0.37f) % 1f;
                float sy = wY - 0.12f - depth01 * waterH * (strip ? 0.6f : 0.55f) + swell * (1f - depth01);
                float len = (strip ? 0.5f : 0.7f) + (i % 4) * 0.18f;
                var sc = Opaque(WorldTheme.Surface(ThemeWater(look.SurfaceLine, sky)));
                // Kenarlarda yavaşça belirip kaybolsun (başa sararken sıçrama görünmesin).
                sc.a = (strip ? 0.35f : 0.28f) * Mathf.Sin(streakX[i] * Mathf.PI) * (1f - depth01 * 0.5f);
                PlaceRect(streaks[i], left + streakX[i] * halfW * 2f, sy, len, strip ? 0.04f : 0.025f, sc);
            }

            // Dipte taşlar (Nehir, yalnız kart).
            for (int i = 0; i < BedStoneCount; i++)
            {
                bedStones[i].enabled = (look.Stones || look.Breakwater) && !strip;
                if (!bedStones[i].enabled) continue;
                float x = left + (0.32f + i * 0.115f + (i % 2) * 0.03f) * halfW * 2f;
                float w = 0.45f + (i * 0.29f % 0.35f);
                var stone = Color.Lerp(WorldTheme.Land(look.Stone), ThemeWater(look.WaterDeep, sky), 0.35f + (i % 3) * 0.1f);
                PlaceRect(bedStones[i], x, bY + w * 0.12f, w, w * 0.5f, Opaque(stone));
            }

            // Sazlar: dipte hafifçe sallanan ince şeritler (yalnız kart).
            for (int i = 0; i < ReedCount; i++)
            {
                reeds[i].enabled = !strip && look.Reeds;
                if (!reeds[i].enabled) continue;
                float x = left + (i * 0.137f + 0.08f + (i % 2) * 0.05f) * halfW * 2f;
                if (i < 3) x = manLeft + manW * (0.68f + i * 0.08f); // iskelenin dibinde küme
                float h = 0.6f + (i * 0.37f % 0.5f);
                reedPhase[i] += dt * 0.9f;
                PlaceRect(reeds[i], x, bY + h / 2f, 0.07f, h, Opaque(WorldTheme.Land(look.Reed)));
                reeds[i].transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(reedPhase[i]) * 4f);
            }

            // Balıkçı + iskele; direkler suyun dibine kadar uzar.
            float manBottom = wY + look.DeckAbove * (manH / (fishermanHeight * look.HeightScale)) - manH * look.DeckTop;
            if (strip) manBottom = wY + StripDeck(look) - manH * look.DeckTop;
            // Kayık dalgayla inip kalkar ve hafifçe yalpalar.
            if (look.Boat) manBottom += swell * 1.2f;
            fisherman.transform.localRotation = Quaternion.Euler(0f, 0f, look.Boat ? Mathf.Sin(Time.time * 1.1f + 0.6f) * 1.5f : 0f);
            fisherman.transform.localPosition = new Vector3(manLeft + manW / 2f, manBottom + manH / 2f, 0f);
            var fb = fisherman.sprite.bounds.size;
            fisherman.transform.localScale = new Vector3(manW / fb.x, manH / fb.y, 1f);
            fisherman.color = WorldTheme.Current == WorldTheme.Kind.Night ? new Color(0.62f, 0.66f, 0.78f) : WorldTheme.Surface(Color.white);
            for (int i = 0; i < posts.Length; i++)
            {
                posts[i].enabled = i < look.Posts.Length;
                if (!posts[i].enabled) continue;
                float px0 = manLeft + manW * look.Posts[i].x, px1 = manLeft + manW * look.Posts[i].y;
                float postTop = manBottom + 0.02f, postBottom = strip ? bY : bY - 0.1f;
                PlaceRect(posts[i], (px0 + px1) / 2f, (postTop + postBottom) / 2f, px1 - px0, postTop - postBottom, fisherman.color * PostColor);
            }

            // Kıyı: kartın solundan iskelenin başına kadar düz toprak; su altında kumlu şev.
            bankTop.enabled = bankUnder.enabled = !strip && !look.Boat && !look.Ice;
            if (bankTop.enabled)
            {
                float deckY = manBottom + manH * look.DeckTop;
                // Göl: kıyı iskelenin başında biter. Nehir: balıkçı kıyının üstünde durur, kıyı ayaklarının altına kadar.
                float bankRight = manLeft + manW * (look.Posts.Length > 0 ? 0.06f : look.Breakwater ? look.PierEnd + 0.08f : look.PierEnd * 0.6f);
                if (look.Breakwater)
                {
                    // Mendirek: balıkçının ayağına kadar beton üst yüzey, sudan dibe dik duvar (suyun içinde koyulaşır).
                    var concrete = WorldTheme.Land(look.Stone);
                    if (WorldTheme.Current == WorldTheme.Kind.Night) concrete = Color.Lerp(concrete, sky, 0.35f);
                    PlaceRect(bankTop, (left + bankRight) / 2f, (deckY + wY) / 2f, bankRight - left, deckY - wY, Opaque(concrete));
                    var wall = Color.Lerp(concrete, ThemeWater(look.WaterDeep, sky), 0.5f);
                    PlaceRect(bankUnder, (left + bankRight) / 2f, (wY + bY) / 2f, bankRight - left, wY - bY, Opaque(wall));
                }
                else
                {
                    var bankColor = look.Stones ? Color.Lerp(look.NearLand, look.Stone, 0.55f) : look.NearLand;
                    PlaceRect(bankTop, (left + bankRight) / 2f, (deckY - 0.05f + wY) / 2f, bankRight - left, deckY - 0.05f - wY, Opaque(WorldTheme.Land(bankColor)));
                    // Su altında kalan şev: dip rengi suyla karışık (suyun içinde olduğu okunsun).
                    var under = Color.Lerp(WorldTheme.Land(look.Bed), ThemeWater(look.WaterTop, sky), 0.45f);
                    PlaceRect(bankUnder, (left + bankRight + 0.4f) / 2f, (wY + bY) / 2f, bankRight + 0.4f - left, wY - bY, Opaque(under));
                }
            }

            // Kıyı kayaları (Nehir): 0-1 balıkçının ayaklarının altında, 2-3 önünde suya inen yuvarlak taşlar.
            // Şeritte kıyı çizilmez; kayalar balıkçıyı suyun üstünde taşır.
            float feetY = manBottom + manH * look.DeckTop;
            for (int i = 0; i < BankStoneCount; i++)
            {
                // Mendirekte yalnız şeritte, ayağın altında tek beton blok (kartta duvar zaten var).
                bankStones[i].enabled = look.Stones || (look.Breakwater && strip && i == 0);
                if (!bankStones[i].enabled) continue;
                if (look.Breakwater)
                {
                    var block = Opaque(WorldTheme.Land(look.Stone));
                    if (WorldTheme.Current == WorldTheme.Kind.Night) block = Opaque(Color.Lerp(block, sky, 0.35f));
                    bankStones[i].sprite = whiteSprite;
                    PlaceRect(bankStones[i], manLeft + manW * 0.2f, (feetY + bY) / 2f, manW * 0.55f, feetY - bY, block);
                    continue;
                }
                bankStones[i].sprite = circleSprite;
                float k = strip ? 0.8f : 1f;
                bool under = i < 2;
                float w = (under ? 1.0f : 0.75f - (i - 2) * 0.2f) * k;
                float h = (under ? 0.62f : 0.5f - (i - 2) * 0.12f) * k;
                float cx = under ? manLeft + manW * (0.08f + i * 0.2f) : manLeft + manW * look.PierEnd + (i - 2) * 0.55f * k;
                float cy = under ? feetY - h * 0.42f : wY + h * 0.08f - (i - 2) * 0.12f;
                var stone = Opaque(WorldTheme.Land(Color.Lerp(look.Stone, look.NearLand, i * 0.08f)));
                if (WorldTheme.Current == WorldTheme.Kind.Night) stone = Opaque(Color.Lerp(stone, sky, 0.35f));
                PlaceRect(bankStones[i], cx, cy, w, h, stone);
            }

            Vector2 tip = new Vector2(manLeft + manW * look.RodTip.x, manBottom + manH * look.RodTip.y);

            // Buz: balıkçının altından kartın iki kenarına buz tabakası (üstünde ince kar), kamış ucunun
            // altında delik; delikte su buzun üstüne yakın durur. Şeritte de çizilir (balıkçıyı taşır).
            iceLeft.enabled = iceRight.enabled = snowLeft.enabled = snowRight.enabled = holeWater.enabled = look.Ice;
            if (look.Ice)
            {
                float iceTop = manBottom, half = strip ? 0.2f : 0.3f, snowH = strip ? 0.05f : 0.07f;
                holeX = tip.x;
                holeSurf = iceTop - (strip ? 0.05f : 0.09f);
                // Buz bir zemin değil yüzey: gecede siluete dönmesin, balıkçı gibi soluk maviye çekilir.
                var nightTint = WorldTheme.Current == WorldTheme.Kind.Night ? new Color(0.42f, 0.48f, 0.62f) : Color.white;
                var ice = WorldTheme.Surface(look.Stone) * nightTint;
                var snow = WorldTheme.Surface(look.NearLand) * nightTint;
                float lx = left - 0.2f, l1 = holeX - half, r0 = holeX + half, rx = right + 0.2f;
                PlaceRect(iceLeft, (lx + l1) / 2f, (iceTop + wY) / 2f, l1 - lx, iceTop - wY, Opaque(ice));
                PlaceRect(iceRight, (r0 + rx) / 2f, (iceTop + wY) / 2f, rx - r0, iceTop - wY, Opaque(ice));
                PlaceRect(snowLeft, (lx + l1) / 2f, iceTop - snowH / 2f, l1 - lx, snowH, Opaque(snow));
                PlaceRect(snowRight, (r0 + rx) / 2f, iceTop - snowH / 2f, rx - r0, snowH, Opaque(snow));
                var hole = Opaque(WorldTheme.Surface(ThemeWater(look.WaterTop, sky)));
                if (strip) hole.a = 0.92f;
                PlaceRect(holeWater, holeX, (holeSurf + wY) / 2f, half * 2f, holeSurf - wY + 0.01f, hole);
                // Yüzey çizgisi yalnız delikte.
                PlaceRect(surface, holeX, holeSurf, half * 2f, strip ? 0.05f : 0.04f, Opaque(WorldTheme.Surface(ThemeWater(look.SurfaceLine, sky))));
            }
            float shoreX = manLeft + manW * look.PierEnd + (strip ? 0.4f : 0.5f) + (look.Stones ? (strip ? 0.3f : 0.45f) : look.Breakwater ? 0.2f : 0f);
            float farX = right - (strip ? 0.8f : 0.9f);
            float maxDepth = (wY - bY) * (strip ? 0.75f : 0.8f);

            UpdateLureAndFish(strip, dt, tip, shoreX, farX, wY, maxDepth);
            UpdateDrops(dt, wY);
        }

        void UpdateLureAndFish(bool strip, float dt, Vector2 tip, float shoreX, float farX, float wY, float maxDepth)
        {
            var phase = fishing.CurrentPhase;
            if (phase != lastPhase)
            {
                if (phase == FishingController.Phase.Retrieving && lastPhase == FishingController.Phase.Casting) sink = 0f;
                if (phase == FishingController.Phase.Landing) pendingLandSplash = true;
                lastPhase = phase;
            }

            float u = fishing.CastDistance * (1f - fishing.Reeled); // 1 = en uzak
            float castX = Mathf.Lerp(shoreX, farX, fishing.CastDistance);
            float x = Mathf.Lerp(shoreX, farX, u);
            float fullDepth = maxDepth * Mathf.Lerp(0.45f, 1f, fishing.CastDistance);
            Vector2 lurePos;
            bool lureVisible = true, fishVisible = false;
            Vector2 fishMouth = Vector2.zero;
            float fishAlpha = 1f, fishTilt = 0f;

            switch (phase)
            {
                case FishingController.Phase.Casting:
                {
                    // Kamış ucundan atılan yer üstüne yay çizerek uçar.
                    float t = Mathf.Clamp01(fishing.PhaseTime / Mathf.Max(0.01f, fishing.CastSeconds));
                    if (look.Ice)
                    {
                        // Buz: yem kamış ucundan deliğe düz iner.
                        lurePos = Vector2.Lerp(tip, new Vector2(holeX, holeSurf), t * t);
                        if (t >= 0.98f && !lastSplashCast)
                        {
                            lastSplashCast = true;
                            Splash(new Vector2(holeX, holeSurf), strip ? 0.3f : 0.5f, 4);
                        }
                        if (t < 0.5f) lastSplashCast = false;
                        break;
                    }
                    float arc = (strip ? 1.0f : 2.2f) * 4f * t * (1f - t);
                    lurePos = new Vector2(Mathf.Lerp(tip.x, castX, t), Mathf.Lerp(tip.y, wY, t) + arc);
                    if (t >= 0.98f && !lastSplashCast)
                    {
                        lastSplashCast = true;
                        Splash(new Vector2(castX, wY), strip ? 0.5f : 1f, 6);
                    }
                    if (t < 0.5f) lastSplashCast = false;
                    break;
                }
                case FishingController.Phase.Retrieving:
                case FishingController.Phase.Hooked:
                {
                    // Spinning: yem düşünce batar, ortada derinde gelir, kıyıya yaklaşınca yükselir.
                    sink = Mathf.MoveTowards(sink, 1f, dt / 1.2f);
                    if (look.Ice)
                    {
                        // Buz: yem delikten dibe doğru iner, sarıldıkça dikey yükselir.
                        float iceDepth = (holeSurf - (wY - maxDepth / (strip ? 0.75f : 0.8f)) - (strip ? 0.15f : 0.45f)) * fishing.CastDistance;
                        lurePos = new Vector2(holeX, holeSurf - 0.08f - Mathf.Max(0f, iceDepth * (1f - fishing.Reeled) * sink));
                    }
                    else
                    {
                        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.3f));
                        float depth = fullDepth * rise * sink;
                        lurePos = new Vector2(x, wY - Mathf.Max(0.06f, depth));
                    }

                    float strike = fishing.StrikeAt;
                    float r = fishing.Reeled;
                    if (phase == FishingController.Phase.Hooked)
                    {
                        fishVisible = true;
                        wiggle += dt * (fishing.ReelDirection != 0f ? 9f : 4f);
                        fishTilt = (look.Ice ? -70f : 0f) + Mathf.Sin(wiggle) * 9f; // buzda başı yukarı, dikey gelir
                        fishMouth = lurePos;
                    }
                    else if (r > strike - 0.15f)
                    {
                        // Balık derinden yeme doğru yaklaşır.
                        float t = Mathf.Clamp01((r - (strike - 0.15f)) / 0.15f);
                        fishVisible = true;
                        fishAlpha = Mathf.Clamp01(t * 2.5f);
                        var from = lurePos + new Vector2(strip ? 1.6f : 2.6f, -(strip ? 0.15f : 0.9f));
                        fishMouth = Vector2.Lerp(from, lurePos + new Vector2(0.15f, 0f), Mathf.SmoothStep(0f, 1f, t));
                        fishTilt = Mathf.Sin(Time.time * 3f) * 3f;
                    }
                    break;
                }
                case FishingController.Phase.Landing:
                {
                    // Balık sudan çıkar, balıkçının eline doğru yükselir ve kaybolur.
                    float t = Mathf.Clamp01(fishing.PhaseTime / Mathf.Max(0.01f, fishing.LandSeconds));
                    var start = new Vector2(shoreX, wY - 0.06f);
                    var hand = new Vector2(Mathf.Lerp(shoreX, tip.x, 0.5f) - (strip ? 0.6f : 1.2f), Mathf.Lerp(wY, tip.y, 0.55f));
                    if (look.Ice)
                    {
                        start = new Vector2(holeX, holeSurf);
                        hand = tip + new Vector2(-(strip ? 0.5f : 0.8f), strip ? 0.2f : 0.35f);
                    }
                    float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f));
                    lurePos = Vector2.Lerp(start, hand, e) + new Vector2(0f, Mathf.Sin(e * Mathf.PI) * (strip ? 0.3f : 0.6f));
                    if (pendingLandSplash)
                    {
                        pendingLandSplash = false;
                        Splash(start, strip ? 0.5f : 1f, 8);
                    }
                    fishVisible = true;
                    fishMouth = lurePos;
                    fishAlpha = 1f - Mathf.Clamp01((t - 0.75f) / 0.25f);
                    wiggle += dt * 12f;
                    fishTilt = -35f * e + Mathf.Sin(wiggle) * 10f;
                    break;
                }
                default: // Resting: yem kamış ucunda sallanır
                    lurePos = tip + new Vector2(0f, -(strip ? 0.25f : 0.45f)) + new Vector2(Mathf.Sin(Time.time * 2f) * 0.03f, 0f);
                    break;
            }

            // Misina: kamış ucundan yeme düz çizgi.
            float lineW = strip ? 0.05f : 0.022f;
            PlaceLine(line, tip, lurePos, lineW, LineColor);

            float lureScale = strip ? 1.4f : 1f;
            lure.enabled = lureTail.enabled = lureVisible && !(phase == FishingController.Phase.Landing);
            lure.transform.localPosition = new Vector3(lurePos.x, lurePos.y, 0f);
            lure.transform.localScale = new Vector3(0.17f * lureScale, 0.08f * lureScale, 1f);
            lure.color = Opaque(WorldTheme.Surface(LureColor));
            lureTail.transform.localPosition = new Vector3(lurePos.x + 0.09f * lureScale, lurePos.y, 0f);
            lureTail.transform.localScale = new Vector3(0.06f * lureScale, 0.06f * lureScale, 1f);
            lureTail.color = Opaque(WorldTheme.Surface(LureTail));

            // Balık: başı kıyıya (sola) bakar, ağzı yemde; boyu tür ve kg'a göre.
            fish.enabled = fishVisible;
            if (fishVisible)
            {
                var species = fishing.CastFish;
                int index = species != null ? FishCatalog.IndexOf(species.Id) : -1;
                if (index >= 0 && fishSprites != null && index < fishSprites.Length && fishSprites[index] != null)
                {
                    fish.sprite = fishSprites[index];
                }
                float w01 = species != null && species.MaxKg > species.MinKg
                    ? Mathf.Clamp01((fishing.CastKg - species.MinKg) / (species.MaxKg - species.MinKg)) : 0.5f;
                float length = (species != null ? species.LengthUnits : 1f) * Mathf.Lerp(0.55f, 1f, w01) * (strip ? stripFishScale : 1f);
                var b = fish.sprite.bounds.size;
                float scale = length / Mathf.Max(0.001f, b.x);
                fish.flipX = true; // görseller sağa bakar
                fish.transform.localScale = new Vector3(scale, scale, 1f);
                fish.transform.localRotation = Quaternion.Euler(0f, 0f, fishTilt);
                // Ağız sol uçta: merkez, ağızdan gövde yönünde yarım boy sağda.
                var offset = (Vector2)(fish.transform.localRotation * new Vector3(length / 2f - length * 0.04f, 0f, 0f));
                fish.transform.localPosition = new Vector3(fishMouth.x + offset.x, fishMouth.y + offset.y, 0f);
                var tint = WorldTheme.Current == WorldTheme.Kind.Night ? new Color(0.62f, 0.66f, 0.78f) : WorldTheme.Surface(Color.white);
                tint.a = fishAlpha;
                fish.color = tint;
            }
        }

        // Su sıçraması: yüzeyden yukarı fırlayıp düşen birkaç damla.
        void Splash(Vector2 at, float scale, int count)
        {
            for (int i = 0, n = 0; i < DropCount && n < count; i++)
            {
                if (drops[i].enabled) continue;
                n++;
                drops[i].enabled = true;
                dropLife[i] = 0.7f;
                dropVel[i] = new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(1.6f, 3.2f)) * scale;
                drops[i].transform.localPosition = new Vector3(at.x + Random.Range(-0.1f, 0.1f) * scale, at.y, 0f);
                float s = Random.Range(0.06f, 0.11f) * Mathf.Max(0.6f, scale);
                drops[i].transform.localScale = new Vector3(s, s, 1f);
            }
        }

        void UpdateDrops(float dt, float wY)
        {
            var c = Opaque(WorldTheme.Surface(look.SurfaceLine));
            for (int i = 0; i < DropCount; i++)
            {
                if (!drops[i].enabled) continue;
                dropLife[i] -= dt;
                dropVel[i] += new Vector2(0f, -9f) * dt;
                var p = drops[i].transform.localPosition + (Vector3)(dropVel[i] * dt);
                drops[i].transform.localPosition = p;
                c.a = Mathf.Clamp01(dropLife[i] / 0.3f);
                drops[i].color = c;
                if (dropLife[i] <= 0f || (p.y < wY && dropVel[i].y < 0f)) drops[i].enabled = false;
            }
        }

        // Şeritte iskele/ayak üstünün sudan yüksekliği: kartınkiyle orantılı.
        static float StripDeck(Look l) => 0.15f * l.DeckAbove / 0.3f;

        // Gecede su gökyüzüyle koyulaşır (Yol'daki nehir kuralı).
        static Color ThemeWater(Color c, Color sky)
        {
            if (WorldTheme.Current == WorldTheme.Kind.Night) c = Color.Lerp(c, sky, 0.55f) * 0.85f;
            return c;
        }

        static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        void PlaceRect(SpriteRenderer sr, float x, float y, float w, float h, Color color)
        {
            sr.transform.localPosition = new Vector3(x, y, 0f);
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / b.x, h / b.y, 1f);
            sr.color = color;
        }

        void PlaceStretched(SpriteRenderer sr, float x, float bottom, float w, float h, Color color)
        {
            PlaceRect(sr, x, bottom + h / 2f, w, h, color);
        }

        void PlaceLine(SpriteRenderer sr, Vector2 a, Vector2 b, float width, Color color)
        {
            var d = b - a;
            float len = d.magnitude;
            sr.enabled = len > 0.01f;
            if (!sr.enabled) return;
            sr.transform.localPosition = (Vector3)((a + b) / 2f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var size = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(len / size.x, width / size.y, 1f);
            sr.color = color;
        }
    }
}
