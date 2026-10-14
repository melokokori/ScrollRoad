using UnityEngine;

namespace ScrollRoad
{
    // Döner kapı ortamının sahnesi (2026-10-14): mekâna göre (AVM/Otel/Havalimanı/Müze/Gökdelen) renkler ve
    // içerideki bölmeler değişir (VenueLook), yerleşim aynı. Yandan kesit. Solda dışarısı (gökyüzü, uzakta şehir,
    // kaldırım, ağaçlar), ortada AVM'nin cephe duvarı ve duvarın içine oturan cam döner kapı, sağda AVM'nin
    // içi (kesit: zemin kat vitrinleri, asma kat, sarkan lambalar). İnsanlar soldan yürür, kapının bir
    // bölmesine girip dönerek içeri geçer, içeride sağa yürüyüp kaybolur. Düz renk, Yol'un görsel diliyle;
    // insanlar ayrı görseller (Gemini), geri kalan her şey kodla çizilir.
    //
    // Sahne kapının konumunun SAF fonksiyonudur (DoorController.Turns, ziyaretçi biriminde): ayın i.
    // ziyaretçisinin bağıl konumu u = Turns − i; u = 0'da kapının ortasındadır (o an sayılır), |u| ≤ ½
    // kapının içi, u < −½ kaldırım, u > ½ AVM'nin içi. Kapı 4 kanatlı: bir ziyaretçi = yarım tur (bölmelerin
    // biri dolu biri boş; çeyrek turda kapının içinde iki kişi üst üste biniyordu); kanatlar ziyaretçinin
    // açısının ±45°'sinde. Geri sarınca herkes aynı yoldan geri yürür (yüzü sola döner).
    //
    // Görev çubuğu şeridinde: arka plan saydam; kaldırım/zemin şeridi, kapı, AVM'nin içi ve insanlar.
    // Yalnız Döner kapı seçiliyken çizer ve kamerayı yönetir.
    public class DoorSceneView : MonoBehaviour
    {
        [SerializeField] DoorController door;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Camera viewCamera;
        [SerializeField] Sprite[] walkerSprites; // VisitorCatalog.Walkers sırasıyla
        [SerializeField] Sprite[] guestSprites;  // VisitorCatalog.AllGuests sırasıyla
        [SerializeField] Sprite citySprite;
        [SerializeField] Material spriteMaterial;

        [Header("Kart")]
        [SerializeField] float cardViewTop = 2.4f;
        [SerializeField] float cardViewBottom = -4.1f;
        [SerializeField] float groundY = -2.95f;
        [SerializeField] float doorX = 1.0f;
        [SerializeField] float personHeight = 1.75f;

        [Header("Görev çubuğu")]
        [SerializeField] float stripDoorScreenX = 0.62f; // solda ziyaretçi sayısına yer kalsın

        // Kapı ve yerleşim (dünya birimi).
        const float DoorRadius = 1.0f;
        const float DoorHeight = 2.3f;
        const float PathRadius = 0.6f;    // kapının içinde insanın yolu (eksenden)
        const float OutsideGap = 1.9f;    // kaldırımda iki ziyaretçi arası
        const float InsideGap = 1.6f;     // AVM'nin içinde
        const float WallThickness = 0.35f;
        const float FloorDepth = 0.35f;
        const float MezzanineY = 3.6f;    // zemin kattan asma katın altına
        const float Stride = 0.95f;       // bir adımın boyu (zıplama/yalpa ritmi)

        const int PoolSize = 20;
        const int ShopCount = 3;
        const int LampCount = 3;
        const int TreeCount = 2;

        static readonly Color SkyDay = new Color(0.84f, 0.92f, 0.95f);
        static readonly Color CityTint = new Color(0.70f, 0.78f, 0.84f);
        static readonly Color Sidewalk = new Color(0.78f, 0.77f, 0.74f);
        static readonly Color Curb = new Color(0.62f, 0.61f, 0.58f);
        static readonly Color Asphalt = new Color(0.30f, 0.31f, 0.34f);
        static readonly Color TreeCrown = new Color(0.47f, 0.66f, 0.47f);
        static readonly Color TreeTrunk = new Color(0.50f, 0.38f, 0.28f);
        static readonly Color Facade = new Color(0.88f, 0.84f, 0.78f);
        static readonly Color Accent = new Color(0.80f, 0.33f, 0.30f);       // kapı saçağı (AVM kırmızısı)
        static readonly Color InteriorWall = new Color(0.97f, 0.93f, 0.85f);
        static readonly Color InteriorFloor = new Color(0.86f, 0.80f, 0.70f);
        static readonly Color Foundation = new Color(0.42f, 0.40f, 0.38f);
        static readonly Color Slab = new Color(0.80f, 0.77f, 0.72f);
        static readonly Color Metal = new Color(0.36f, 0.39f, 0.43f);
        static readonly Color Glass = new Color(0.72f, 0.88f, 0.94f);
        static readonly Color ShopFrame = new Color(0.30f, 0.32f, 0.36f);
        static readonly Color LampLight = new Color(1.0f, 0.86f, 0.55f);
        static readonly Color[] ShopColors =
        {
            new Color(0.40f, 0.62f, 0.66f), new Color(0.90f, 0.66f, 0.36f), new Color(0.62f, 0.52f, 0.72f),
        };

        // Mekânın görünümü (2026-10-14): cephe/saçak/iç renkleri ve iç düzen. Yerleşim (kapı, zemin, asma kat,
        // 3 bölme, lambalar) bütün mekânlarda aynı; bölmelere ne çizileceği Decor'a göre değişir.
        enum Decor { Shops, Hotel, Airport, Museum, Tower }

        enum Lamp { None, Pendant, Chandelier, Spot }

        sealed class VenueLook
        {
            public Decor Decor;
            public Color Facade, Accent, Wall, Floor;
            public Color[] Bays;
            public Lamp Lamps;
        }

        static readonly VenueLook MallLook = new VenueLook
        {
            Decor = Decor.Shops, Facade = Facade, Accent = Accent, Wall = InteriorWall, Floor = InteriorFloor,
            Bays = ShopColors, Lamps = Lamp.Pendant,
        };

        static readonly VenueLook HotelLook = new VenueLook
        {
            Decor = Decor.Hotel,
            Facade = new Color(0.86f, 0.78f, 0.66f), Accent = new Color(0.52f, 0.17f, 0.22f),
            Wall = new Color(0.95f, 0.89f, 0.78f), Floor = new Color(0.62f, 0.27f, 0.27f), // kırmızı halı
            Bays = new[] { new Color(0.45f, 0.30f, 0.20f), new Color(0.55f, 0.38f, 0.25f), new Color(0.55f, 0.38f, 0.25f) },
            Lamps = Lamp.Chandelier,
        };

        static readonly VenueLook AirportLook = new VenueLook
        {
            Decor = Decor.Airport,
            Facade = new Color(0.80f, 0.83f, 0.86f), Accent = new Color(0.22f, 0.45f, 0.74f),
            Wall = new Color(0.93f, 0.95f, 0.96f), Floor = new Color(0.78f, 0.80f, 0.82f),
            Bays = new[] { new Color(0.22f, 0.45f, 0.74f), new Color(0.22f, 0.45f, 0.74f), new Color(0.95f, 0.74f, 0.20f) },
            Lamps = Lamp.None,
        };

        static readonly VenueLook MuseumLook = new VenueLook
        {
            Decor = Decor.Museum,
            Facade = new Color(0.93f, 0.91f, 0.87f), Accent = new Color(0.22f, 0.38f, 0.34f),
            Wall = new Color(0.30f, 0.42f, 0.43f), Floor = new Color(0.62f, 0.46f, 0.31f), // koyu duvar, parke
            Bays = new[] { new Color(0.85f, 0.62f, 0.30f), new Color(0.35f, 0.50f, 0.70f), new Color(0.72f, 0.36f, 0.32f) },
            Lamps = Lamp.Spot,
        };

        static readonly VenueLook TowerLook = new VenueLook
        {
            Decor = Decor.Tower,
            Facade = new Color(0.40f, 0.48f, 0.56f), Accent = new Color(0.18f, 0.20f, 0.23f),
            Wall = new Color(0.84f, 0.84f, 0.82f), Floor = new Color(0.36f, 0.37f, 0.40f), // granit
            Bays = new[] { new Color(0.70f, 0.73f, 0.76f), new Color(0.70f, 0.73f, 0.76f), new Color(0.70f, 0.73f, 0.76f) },
            Lamps = Lamp.None,
        };

        static VenueLook LookFor(DoorVenue venue)
        {
            if (venue == VisitorCatalog.Hotel) return HotelLook;
            if (venue == VisitorCatalog.Airport) return AirportLook;
            if (venue == VisitorCatalog.Museum) return MuseumLook;
            if (venue == VisitorCatalog.Skyscraper) return TowerLook;
            return MallLook;
        }

        Transform root;
        Sprite whiteSprite, circleSprite;
        SpriteRenderer city, sidewalk, curb, road, floorIn, foundation, interiorWall, wallUpper, canopy, canopyLip;
        SpriteRenderer slab, railing, drumCap, drumBase, centerPost, backGlass, frontGlass, frontEdgeL, frontEdgeR;
        SpriteRenderer[] treeCrowns, treeTrunks, shopFrames, shopGlass, shopAwnings, upperShops, lampCords, lamps;
        SpriteRenderer[] wingGlass, wingEdge, wingBar;
        SpriteRenderer[] people;

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
            root = new GameObject("Mall").transform;
            root.SetParent(transform, false);

            city = Create("City", citySprite != null ? citySprite : whiteSprite, 1);
            treeCrowns = new SpriteRenderer[TreeCount];
            treeTrunks = new SpriteRenderer[TreeCount];
            for (int i = 0; i < TreeCount; i++)
            {
                treeTrunks[i] = Create($"TreeTrunk{i}", whiteSprite, 3);
                treeCrowns[i] = Create($"TreeCrown{i}", circleSprite, 4);
            }
            road = Create("Road", whiteSprite, 5);
            curb = Create("Curb", whiteSprite, 6);
            sidewalk = Create("Sidewalk", whiteSprite, 6);

            interiorWall = Create("InteriorWall", whiteSprite, 8);
            shopFrames = new SpriteRenderer[ShopCount];
            shopGlass = new SpriteRenderer[ShopCount];
            shopAwnings = new SpriteRenderer[ShopCount];
            upperShops = new SpriteRenderer[ShopCount];
            for (int i = 0; i < ShopCount; i++)
            {
                shopFrames[i] = Create($"ShopFrame{i}", whiteSprite, 9);
                shopGlass[i] = Create($"ShopGlass{i}", whiteSprite, 10);
                shopAwnings[i] = Create($"ShopAwning{i}", whiteSprite, 11);
                upperShops[i] = Create($"UpperShop{i}", whiteSprite, 10);
            }
            slab = Create("Mezzanine", whiteSprite, 12);
            railing = Create("Railing", whiteSprite, 12);
            lampCords = new SpriteRenderer[LampCount];
            lamps = new SpriteRenderer[LampCount];
            for (int i = 0; i < LampCount; i++)
            {
                lampCords[i] = Create($"LampCord{i}", whiteSprite, 12);
                lamps[i] = Create($"Lamp{i}", circleSprite, 13);
            }
            floorIn = Create("Floor", whiteSprite, 14);
            foundation = Create("Foundation", whiteSprite, 14);
            wallUpper = Create("FacadeWall", whiteSprite, 15);

            backGlass = Create("DrumBack", whiteSprite, 20);
            centerPost = Create("CenterPost", whiteSprite, 21);
            wingGlass = new SpriteRenderer[4];
            wingEdge = new SpriteRenderer[4];
            wingBar = new SpriteRenderer[4];
            for (int k = 0; k < 4; k++)
            {
                wingGlass[k] = Create($"Wing{k}", whiteSprite, 22);
                wingEdge[k] = Create($"WingEdge{k}", whiteSprite, 23);
                wingBar[k] = Create($"WingBar{k}", whiteSprite, 23);
            }
            people = new SpriteRenderer[PoolSize];
            for (int i = 0; i < PoolSize; i++) people[i] = Create($"Person{i}", whiteSprite, 30);
            frontGlass = Create("DrumFront", whiteSprite, 60);
            frontEdgeL = Create("DrumEdgeL", whiteSprite, 61);
            frontEdgeR = Create("DrumEdgeR", whiteSprite, 61);
            drumBase = Create("DrumBase", whiteSprite, 62);
            drumCap = Create("DrumCap", whiteSprite, 62);
            canopy = Create("Canopy", whiteSprite, 63);
            canopyLip = Create("CanopyLip", whiteSprite, 64);
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
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Door;
            if (root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
            return active;
        }

        // ---------------------------------------------------------------- kare

        void LateUpdate()
        {
            if (!Show() || door == null) return;
            bool strip = IsCompanion;
            bool night = WorldTheme.Current == WorldTheme.Kind.Night;
            var look = LookFor(door.Venue);

            // Kamera: kartta sabit bant; şeritte zeminin altından kapı saçağının üstüne.
            float aspect = Mathf.Max(0.1f, viewCamera.aspect);
            float size, camX, camY;
            if (strip)
            {
                float bottom = groundY - 0.2f, top = groundY + DoorHeight + 0.3f;
                float band = 1f - (companionWindow != null ? companionWindow.StripOverflowFraction : 0f);
                size = (top - bottom) / 2f / Mathf.Max(0.2f, band);
                camY = bottom + size;
                camX = doorX - (stripDoorScreenX * 2f - 1f) * size * aspect;
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

            float gY = groundY;
            float inX = doorX + WallThickness / 2f; // AVM'nin içi buradan sağa
            float outX = doorX - DoorRadius;         // kaldırım buraya kadar

            // Dışarısı: uzakta şehir, ağaçlar, yol (yalnız kart).
            city.enabled = !strip && citySprite != null;
            if (city.enabled)
            {
                float cw = (outX - left) + 1.5f;
                float ch = cw * (citySprite.bounds.size.y / Mathf.Max(0.01f, citySprite.bounds.size.x));
                ch = Mathf.Min(ch, 2.6f);
                PlaceRect(city, left + cw / 2f - 0.2f, gY + ch / 2f - 0.05f, cw, ch, Opaque(WorldTheme.CitySilhouette(CityTint)));
            }
            for (int i = 0; i < TreeCount; i++)
            {
                treeCrowns[i].enabled = treeTrunks[i].enabled = !strip;
                if (strip) continue;
                float tx = left + (0.18f + i * 0.33f) * (outX - left);
                float th = 1.25f + i * 0.2f, cr = 0.62f + i * 0.08f;
                PlaceRect(treeTrunks[i], tx, gY + th / 2f, 0.12f, th, Opaque(WorldTheme.Land(TreeTrunk)));
                PlaceRect(treeCrowns[i], tx, gY + th + cr * 0.55f, cr * 2f, cr * 1.8f, Opaque(WorldTheme.Land(TreeCrown)));
            }
            road.enabled = !strip;
            if (!strip) PlaceSpan(road, left - 0.2f, outX + 0.01f, bottom0 - 0.2f, gY - FloorDepth - 0.06f, Opaque(WorldTheme.Surface(Asphalt) * (night ? 0.6f : 1f)));
            curb.enabled = !strip;
            if (!strip) PlaceSpan(curb, left - 0.2f, outX + 0.01f, gY - FloorDepth - 0.06f, gY - FloorDepth, Opaque(WorldTheme.Surface(Curb) * (night ? 0.6f : 1f)));
            var walk = Opaque(WorldTheme.Surface(Sidewalk) * (night ? 0.62f : 1f));
            if (strip) walk.a = 0.95f;
            PlaceSpan(sidewalk, left - 0.2f, outX + 0.01f, gY - (strip ? 0.12f : FloorDepth), gY, walk);

            // AVM'nin içi: gecede de ışıklı (sıcak), yalnız sepya tonlanır.
            var wallIn = Opaque(WorldTheme.Surface(look.Wall));
            if (strip) wallIn.a = 0.92f;
            float interiorTop = strip ? gY + DoorHeight + 0.12f : top0 + 0.2f;
            PlaceSpan(interiorWall, inX, right + 0.2f, gY, interiorTop, wallIn);
            var floorColor = Opaque(WorldTheme.Surface(look.Floor));
            if (strip) floorColor.a = 0.95f;
            PlaceSpan(floorIn, outX, right + 0.2f, gY - (strip ? 0.12f : FloorDepth), gY, floorColor);
            foundation.enabled = !strip;
            if (!strip) PlaceSpan(foundation, outX, right + 0.2f, bottom0 - 0.2f, gY - FloorDepth, Opaque(WorldTheme.Surface(Foundation) * (night ? 0.7f : 1f)));

            // Vitrinler (zemin kat) + asma kat + üst kat vitrinlerinin altı + sarkan lambalar (yalnız kart).
            float shopsLeft = doorX + DoorRadius + 1.0f, shopsRight = right - 0.35f;
            float shopW = (shopsRight - shopsLeft) / ShopCount;
            float mezY = gY + MezzanineY;
            for (int i = 0; i < ShopCount; i++)
            {
                bool on = !strip && shopW > 0.6f;
                shopFrames[i].enabled = shopGlass[i].enabled = shopAwnings[i].enabled = upperShops[i].enabled = on;
                if (!on) continue;
                DrawBay(look, i, shopsLeft + shopW * (i + 0.5f), shopW * 0.82f, gY, mezY, top0, night);
            }
            slab.enabled = railing.enabled = !strip;
            if (!strip)
            {
                PlaceSpan(slab, inX, right + 0.2f, mezY, mezY + 0.25f, Opaque(WorldTheme.Surface(Slab)));
                var rail = WorldTheme.Surface(Glass);
                rail.a = 0.45f;
                PlaceSpan(railing, inX + 0.6f, right + 0.2f, mezY + 0.25f, mezY + 0.75f, rail);
            }
            for (int i = 0; i < LampCount; i++)
            {
                lampCords[i].enabled = lamps[i].enabled = !strip && shopW > 0.6f && look.Lamps != Lamp.None;
                if (!lamps[i].enabled) continue;
                var lampC = WorldTheme.Surface(LampLight);
                lampC.a = 1f;
                if (look.Lamps == Lamp.Spot)
                {
                    // Müze: her tablonun üstünde asma katın altına asılı küçük spot.
                    float sx = shopsLeft + shopW * (i + 0.5f);
                    PlaceSpan(lampCords[i], sx - 0.012f, sx + 0.012f, mezY - 0.22f, mezY, Opaque(WorldTheme.Surface(Metal)));
                    PlaceRect(lamps[i], sx, mezY - 0.28f, 0.2f, 0.14f, lampC);
                    continue;
                }
                float lx = shopsLeft + shopW * (i + 0.5f) + shopW * 0.5f * (i == LampCount - 1 ? -1f : 1f) * 0.3f;
                float cordLen = 0.55f + (i % 2) * 0.25f;
                PlaceSpan(lampCords[i], lx - 0.012f, lx + 0.012f, mezY - cordLen, mezY, Opaque(WorldTheme.Surface(Metal)));
                if (look.Lamps == Lamp.Chandelier)
                    PlaceRect(lamps[i], lx, mezY - cordLen - 0.18f, 0.62f, 0.36f, lampC);
                else
                    PlaceRect(lamps[i], lx, mezY - cordLen - 0.12f, 0.3f, 0.26f, lampC);
            }

            // Cephe duvarı (kapının üstünden yukarı) + kapı saçağı (dışarıya taşar).
            float capTop = gY + DoorHeight + 0.18f;
            var facade = Opaque(WorldTheme.Surface(look.Facade) * (night ? 0.55f : 1f));
            wallUpper.enabled = !strip;
            if (!strip) PlaceSpan(wallUpper, doorX - WallThickness / 2f, doorX + WallThickness / 2f, capTop, top0 + 0.2f, facade);
            var accent = Opaque(WorldTheme.Surface(look.Accent) * (night ? 0.75f : 1f));
            PlaceSpan(canopy, doorX - DoorRadius - (strip ? 0.25f : 0.55f), doorX + WallThickness / 2f, capTop, capTop + (strip ? 0.14f : 0.22f), accent);
            canopyLip.enabled = !strip;
            if (!strip) PlaceSpan(canopyLip, doorX - DoorRadius - 0.55f, doorX - DoorRadius - 0.47f, capTop - 0.12f, capTop + 0.22f, accent * 0.85f);

            UpdateDoor(strip, gY);
            UpdatePeople(strip, gY, left, right);
        }

        // İçerideki bir bölme (zemin kat + asma katın üstü). Dört parça her mekânda başka bir şeye dönüşür:
        // AVM vitrin/cam/tente/üst vitrin, Otel resepsiyon ya da oda kapısı, Havalimanı check-in bankosu ve
        // asılı tabela, Müze çerçeveli tablo ve etiketi, Gökdelen asansör kapısı ve kat göstergesi.
        void DrawBay(VenueLook look, int i, float cx, float w, float gY, float mezY, float top0, bool night)
        {
            var bay = look.Bays[i % look.Bays.Length];
            var frameC = Opaque(WorldTheme.Surface(ShopFrame));
            float upperBottom = mezY + 0.45f;
            switch (look.Decor)
            {
                case Decor.Hotel:
                {
                    var wood = Opaque(WorldTheme.Surface(bay));
                    var woodLight = Opaque(WorldTheme.Surface(Color.Lerp(bay, Color.white, 0.25f)));
                    if (i == 0)
                    {
                        // Resepsiyon bankosu + arkasında anahtar panosu.
                        PlaceRect(shopFrames[i], cx, gY + 0.52f, w, 1.04f, wood);
                        PlaceRect(shopGlass[i], cx, gY + 1.08f, w + 0.12f, 0.1f, woodLight);
                        PlaceRect(shopAwnings[i], cx, gY + 2.05f, w * 0.6f, 0.62f, woodLight);
                    }
                    else
                    {
                        // Oda kapısı + altın numara plakası.
                        float dw = Mathf.Min(1.0f, w * 0.6f);
                        PlaceRect(shopFrames[i], cx, gY + 1.07f, dw, 2.14f, wood);
                        PlaceRect(shopGlass[i], cx, gY + 1.0f, dw - 0.2f, 1.75f, woodLight);
                        PlaceRect(shopAwnings[i], cx, gY + 2.36f, 0.3f, 0.14f, Opaque(WorldTheme.Surface(new Color(0.85f, 0.68f, 0.30f))));
                    }
                    // Üst kat: sıcak duvar kâğıdı panosu.
                    PlaceSpan(upperShops[i], cx - w / 2f, cx + w / 2f, upperBottom, top0 + 0.2f,
                        Opaque(WorldTheme.Surface(Color.Lerp(look.Wall, bay, 0.18f))));
                    break;
                }
                case Decor.Airport:
                {
                    // Check-in bankosu (bagaj bandı üstte), üstünde asılı tabela; üst katta büyük cam (gökyüzü).
                    PlaceRect(shopFrames[i], cx, gY + 0.5f, w * 0.85f, 1.0f, Opaque(WorldTheme.Surface(new Color(0.60f, 0.63f, 0.67f))));
                    PlaceRect(shopGlass[i], cx, gY + 1.04f, w * 0.9f, 0.08f, Opaque(WorldTheme.Surface(Metal)));
                    PlaceRect(shopAwnings[i], cx, gY + 2.55f, w * 0.7f, 0.42f, Opaque(WorldTheme.Surface(bay)));
                    var sky = WorldTheme.Sky(SkyDay);
                    PlaceSpan(upperShops[i], cx - w / 2f - 0.05f, cx + w / 2f + 0.05f, upperBottom, top0 + 0.2f,
                        Opaque(Color.Lerp(sky, Glass, 0.35f) * (night ? 0.55f : 1f)));
                    break;
                }
                case Decor.Museum:
                {
                    // Altın çerçeveli tablo, altında küçük etiket; üst katta sergi afişi.
                    float pw = Mathf.Min(1.6f, w * 0.62f), ph = 1.15f, py = gY + 1.8f;
                    PlaceRect(shopFrames[i], cx, py, pw, ph, Opaque(WorldTheme.Surface(new Color(0.80f, 0.64f, 0.28f))));
                    PlaceRect(shopGlass[i], cx, py, pw - 0.16f, ph - 0.16f, Opaque(WorldTheme.Surface(bay)));
                    PlaceRect(shopAwnings[i], cx, gY + 1.0f, 0.32f, 0.13f, Opaque(WorldTheme.Surface(new Color(0.95f, 0.93f, 0.88f))));
                    PlaceSpan(upperShops[i], cx - w * 0.18f, cx + w * 0.18f, upperBottom + 0.2f, top0 + 0.2f,
                        Opaque(WorldTheme.Surface(Color.Lerp(bay, look.Wall, 0.35f))));
                    break;
                }
                case Decor.Tower:
                {
                    // Asansör: çelik çerçeve + kapı, üstünde kehribar kat göstergesi; üst katta cam giydirme cephe.
                    float ew = Mathf.Min(1.2f, w * 0.62f);
                    PlaceRect(shopFrames[i], cx, gY + 1.12f, ew + 0.16f, 2.24f, frameC);
                    PlaceRect(shopGlass[i], cx, gY + 1.05f, ew, 2.1f, Opaque(WorldTheme.Surface(bay)));
                    PlaceRect(shopAwnings[i], cx, gY + 2.45f, 0.42f, 0.14f, Opaque(WorldTheme.Surface(new Color(0.95f, 0.66f, 0.22f))));
                    PlaceSpan(upperShops[i], cx - w / 2f - 0.05f, cx + w / 2f + 0.05f, upperBottom, top0 + 0.2f,
                        Opaque(WorldTheme.Surface(Color.Lerp(Glass, look.Facade, 0.45f)) * (night ? 0.6f : 1f)));
                    break;
                }
                default:
                {
                    // AVM: vitrin + cam + tente; asma katın üstünde üst kat vitrini.
                    float h = 2.1f;
                    PlaceRect(shopFrames[i], cx, gY + h / 2f, w, h, frameC);
                    var glassC = Color.Lerp(bay, Color.white, night ? 0.25f : 0.55f);
                    PlaceRect(shopGlass[i], cx, gY + (h - 0.12f) / 2f, w - 0.14f, h - 0.12f, Opaque(WorldTheme.Surface(glassC)));
                    PlaceRect(shopAwnings[i], cx, gY + h + 0.12f, w + 0.12f, 0.28f, Opaque(WorldTheme.Surface(bay)));
                    PlaceSpan(upperShops[i], cx - w / 2f, cx + w / 2f, upperBottom, top0 + 0.2f, Opaque(WorldTheme.Surface(Color.Lerp(glassC, ShopFrame, 0.2f))));
                    break;
                }
            }
        }

        void UpdateDoor(bool strip, float gY)
        {
            float h = DoorHeight;
            var metal = Opaque(WorldTheme.Surface(Metal));
            PlaceSpan(drumBase, doorX - DoorRadius - 0.06f, doorX + DoorRadius + 0.06f, gY - 0.04f, gY + 0.03f, metal);
            PlaceSpan(drumCap, doorX - DoorRadius - 0.06f, doorX + DoorRadius + 0.06f, gY + h, gY + h + 0.18f, metal);
            PlaceSpan(centerPost, doorX - 0.035f, doorX + 0.035f, gY, gY + h, metal);

            // Kapının yan camları (kesitte kameraya yakın ve uzak kavisli duvar): ince saydam yüzey.
            var back = WorldTheme.Surface(Glass);
            back.a = strip ? 0.3f : 0.22f;
            PlaceSpan(backGlass, doorX - DoorRadius * 0.87f, doorX + DoorRadius * 0.87f, gY, gY + h, back);
            var front = WorldTheme.Surface(Glass);
            front.a = 0.12f;
            PlaceSpan(frontGlass, doorX - DoorRadius * 0.87f, doorX + DoorRadius * 0.87f, gY, gY + h, front);
            PlaceSpan(frontEdgeL, doorX - DoorRadius * 0.87f - 0.025f, doorX - DoorRadius * 0.87f + 0.025f, gY, gY + h, metal);
            PlaceSpan(frontEdgeR, doorX + DoorRadius * 0.87f - 0.025f, doorX + DoorRadius * 0.87f + 0.025f, gY, gY + h, metal);

            // Kanatlar: θ = 45° + 180°·kesir(Turns) + k·90°; yandan bakınca eksenden r·sin θ'ya uzanan cam.
            // Kameraya dönük kanat (cos θ büyük) insanların önünde, diğerleri arkasında çizilir.
            double turns = door.Turns;
            float frac = (float)(turns - System.Math.Floor(turns));
            float theta0 = 45f + 180f * frac;
            for (int k = 0; k < 4; k++)
            {
                float th = (theta0 + 90f * k) * Mathf.Deg2Rad;
                float s = Mathf.Sin(th), c = Mathf.Cos(th);
                float edgeX = doorX + DoorRadius * s;
                bool frontSide = c > 0.75f;
                wingGlass[k].sortingOrder = frontSide ? 54 : 22;
                wingEdge[k].sortingOrder = wingBar[k].sortingOrder = frontSide ? 55 : 23;
                float w = Mathf.Abs(DoorRadius * s);
                wingGlass[k].enabled = wingBar[k].enabled = w > 0.03f;
                var g = WorldTheme.Surface(Glass);
                g.a = (strip ? 0.32f : 0.24f) * (0.55f + 0.45f * Mathf.Abs(s));
                if (wingGlass[k].enabled)
                {
                    PlaceSpan(wingGlass[k], Mathf.Min(doorX, edgeX), Mathf.Max(doorX, edgeX), gY + 0.06f, gY + h - 0.06f, g);
                    // İtme kolu: kanadın yarı yüksekliğinde yatay çubuk.
                    PlaceSpan(wingBar[k], Mathf.Min(doorX, edgeX), Mathf.Max(doorX, edgeX), gY + h * 0.45f - 0.025f, gY + h * 0.45f + 0.025f, metal);
                }
                PlaceSpan(wingEdge[k], edgeX - 0.028f, edgeX + 0.028f, gY + 0.06f, gY + h - 0.06f, metal);
            }
        }

        // Ayın i. ziyaretçisinin bağıl konumundan (u) yatay yeri.
        float PersonX(double u, float jitter)
        {
            if (u < -0.5)
            {
                float d = (float)(-0.5 - u);
                return doorX - PathRadius - d * OutsideGap + jitter * 0.35f * OutsideGap * Mathf.SmoothStep(0f, 1f, d);
            }
            if (u <= 0.5) return doorX + 2f * PathRadius * (float)u;
            float e = (float)(u - 0.5);
            return doorX + PathRadius + e * InsideGap + jitter * 0.3f * InsideGap * Mathf.SmoothStep(0f, 1f, e);
        }

        void UpdatePeople(bool strip, float gY, float left, float right)
        {
            double turns = door.Turns;
            double uMin = -0.5 - (doorX - PathRadius - left + 1.2f) / OutsideGap - 1.0;
            double uMax = 0.5 + (right - doorX - PathRadius + 1.2f) / InsideGap + 1.0;
            long first = System.Math.Max(1L, (long)System.Math.Floor(turns - uMax));
            long last = (long)System.Math.Ceiling(turns - uMin);
            bool backing = door.TurnDirection < 0f;
            var tint = WorldTheme.Current == WorldTheme.Kind.Night ? new Color(0.70f, 0.72f, 0.82f) : WorldTheme.Surface(Color.white);
            var tintIn = WorldTheme.Surface(Color.white); // AVM'nin içi ışıklı

            int used = 0;
            for (long i = last; i >= first && used < PoolSize; i--)
            {
                double u = turns - i;
                var v = door.Identify(i);
                float x = PersonX(u, v.Jitter);
                if (x < left - 1f || x > right + 1f) continue;

                var sr = people[used];
                // Yeni gelen (soldaki, i büyük) arkada; önce dizilen önde.
                sr.sortingOrder = 30 + used;
                used++;
                sr.enabled = true;
                var sprite = SpriteFor(v);
                sr.sprite = sprite != null ? sprite : whiteSprite;

                float h = personHeight * v.Type.Height * v.Scale;
                var b = sr.sprite.bounds.size;
                float scale = h / Mathf.Max(0.001f, b.y);
                sr.flipX = backing; // görseller sağa yürür; geri çıkarken yüzü sola döner
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                // Yürüyüş: adım başına hafif zıplama ve yalpa (yürünen yola bağlı, durunca durur).
                float phase = (x + v.Jitter * 3f) / Stride * Mathf.PI;
                float bob = Mathf.Abs(Mathf.Sin(phase)) * h * 0.025f;
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * 1.6f);
                sr.transform.localPosition = new Vector3(x, gY + h / 2f + bob, 0f);

                // İçeride sağ kenara yaklaşınca dükkânlara dağılır (solar).
                float a = 1f;
                if (u > 0.5) a = 1f - Mathf.Clamp01((x - (right - 2.2f)) / 1.6f);
                var c = u > 0.0 ? tintIn : tint;
                c.a = a;
                sr.color = c;
                if (a <= 0.01f) sr.enabled = false;
            }
            for (int i = used; i < PoolSize; i++) people[i].enabled = false;
        }

        Sprite SpriteFor(VisitorCatalog.Visitor v)
        {
            if (v.IsGuest)
            {
                int gi = VisitorCatalog.GuestIndex(v.Type.Id);
                if (guestSprites != null && gi >= 0 && gi < guestSprites.Length) return guestSprites[gi];
                return null;
            }
            int wi = System.Array.IndexOf(VisitorCatalog.Walkers, v.Type);
            if (walkerSprites != null && wi >= 0 && wi < walkerSprites.Length) return walkerSprites[wi];
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
    }
}
