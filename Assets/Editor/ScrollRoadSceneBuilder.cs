using Kirurobo;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ScrollRoad.EditorTools
{
    // Sahne tamamen koddan kurulur (elle sahne düzenlemesi yok → tekrar üretilebilir).
    // Menü: ScrollRoad → Sahneleri Kur. Ayrıca Player/URP ayarlarını saydam pencere için zorlar.
    //
    // Tek sahne (Game): oyun doğrudan kartta açılır, ayrı ana menü yok. Arayüz 800x340'lık karta
    // göre tasarlandı (CanvasScaler referansı); menüler kartın içinde: ≡ çekmecesi ve kartı kaplayan
    // paneller. Görev çubuğu şeridinin arayüzü ayrı, sabit piksel ölçekli canvas.
    public static class ScrollRoadSceneBuilder
    {
        public const string ScenesFolder = "Assets/Scenes";
        public const string GameScenePath = ScenesFolder + "/" + SceneNames.Game + ".unity";
        const string LegacyMainMenuScenePath = ScenesFolder + "/MainMenu.unity";

        const string CarCatalogPath = "Assets/Data/CarCatalog.asset";
        const string AppIconPath = "Assets/Art/Icon/app_car.png";

        // Garaj: modeller × renkler (ilk model ve ilk renk varsayılan). Her modelin tek taban görseli
        // Assets/StreamingAssets/Vehicles/<model>.png (boyanabilirse mercan gövdeli; Tools/gen_vehicle.py,
        // yeşil zemin); renkler ve silüetler oyunda VehicleArt ile türetilir.
        // Adlar gerçek modellerden esinlenen KURGUSAL markalar (gerçek isim/logo yok).
        // Eşikler geçici ("sonra ayarlanacak"). widthScale: yolda tekerler/oranlar tutarlı görünsün.
        struct ModelDef
        {
            public string id, name, label;
            public CarCatalog.Kind kind;
            public bool paintable;
            public CarCatalog.Unlock unlock;
            public double value;
            public float width;
        }

        static ModelDef Def(string id, string name, CarCatalog.Kind kind, CarCatalog.Unlock unlock, double value = 0,
            float width = 1f, bool paintable = true, string label = null) =>
            new ModelDef { id = id, name = name, kind = kind, unlock = unlock, value = value, width = width, paintable = paintable, label = label };

        static readonly ModelDef[] CarModels =
        {
            // Cars
            Def("car", "Hatchback", CarCatalog.Kind.Car, CarCatalog.Unlock.Free),
            Def("atmaca", "Tufan Atmaca", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 200, 1.05f),
            Def("zefir", "Anatol Zefir", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 400, 1.05f),
            Def("coralsedan", "Toyama Coral", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 500, 1.05f),
            Def("t10s", "TUĞ T10S", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 1200, 1.05f),
            Def("dreier", "Bavaro Dreier", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 1750, 1.05f),
            Def("van", "Camper", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 2500, 1.12f),
            Def("impulso", "Sorabu Impulso", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 3500, 1.05f),
            Def("type3", "Teslo Type 3", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 4500, 1.05f),
            Def("chaser", "Dodger Chaser", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 7000, 1.15f),
            Def("towne", "Linden Towne", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 8000, 1.2f),
            Def("panorama", "Pfalz Panorama", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 16000, 1.1f),
            Def("apparition", "Rollo Apparition", CarCatalog.Kind.Car, CarCatalog.Unlock.LifetimeKm, 40000, 1.2f),
            // Off-road, SUV, pickup
            Def("duner", "Dacio Duner", CarCatalog.Kind.Suv, CarCatalog.Unlock.LifetimeKm, 350, 1.05f),
            Def("taiga", "Ladoga Taiga", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.MonthBestKm, 500, 0.95f, true, "reach Pine Forest in one month"),
            Def("jinny", "Suzaku Jinny", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.LifetimeKm, 1000, 0.9f),
            Def("pickup", "Toyama Highlux", CarCatalog.Kind.Pickup, CarCatalog.Unlock.LifetimeKm, 1500, 1.1f),
            Def("atv", "Trail ATV", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.LifetimeKm, 2000, 0.7f),
            Def("t10x", "TUĞ T10X", CarCatalog.Kind.Suv, CarCatalog.Unlock.LifetimeKm, 2250, 1.05f),
            Def("landcrawler", "Toyama Land Crawler", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.MonthBestKm, 4500, 1.1f, true, "reach the Desert in one month"),
            Def("guardian", "Land Roamer Guardian", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.LifetimeKm, 6500, 1.0f),
            Def("wrangel", "Jiip Wrangel", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.MonthBestKm, 7500, 0.95f, true, "reach the Canyon in one month"),
            Def("mercatorg", "Mercator G", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.LifetimeKm, 12000, 1.0f),
            Def("ibex", "Dodger Ibex", CarCatalog.Kind.Pickup, CarCatalog.Unlock.LifetimeKm, 15000, 1.25f),
            Def("escapade", "Cadillon Escapade", CarCatalog.Kind.Suv, CarCatalog.Unlock.LifetimeKm, 21000, 1.2f),
            Def("hammerhx", "Hammer HX", CarCatalog.Kind.OffRoad, CarCatalog.Unlock.LifetimeKm, 29000, 1.15f),
            Def("toro", "Lamberti Toro", CarCatalog.Kind.Suv, CarCatalog.Unlock.LifetimeKm, 35000, 1.1f),
            // Two wheels and friends
            Def("mobilo", "Mobilo Super", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 100, 0.75f),
            Def("pcx", "Hondo PX", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 300, 0.75f),
            Def("vesper", "Vesper Primo", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 750, 0.7f),
            Def("javo", "Javo 350", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 850, 0.75f),
            Def("chimp", "Hondo Chimp", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 1100, 0.6f),
            Def("atlasgs", "Bavaro Atlas GS", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.MonthBestKm, 2500, 0.8f, true, "reach Mountain Pass in one month"),
            Def("anfield", "Royal Anfield Classic", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 3250, 0.75f),
            Def("bonnevale", "Tribune Bonnevale", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 6000, 0.75f),
            Def("baron", "KTX 390 Baron", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 10000, 0.75f),
            Def("snodoo", "Sno-Doo", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.MonthBestKm, 11000, 0.8f, true, "reach the Snowy Plateau in one month"),
            Def("heavyboy", "Hartley Heavy Boy", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.MonthBestKm, 15000, 0.85f, true, "reach Sunset Road in one month"),
            Def("tarantula", "Kanam Tarantula", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 17000, 0.8f),
            Def("hayate", "Suzaku Hayate", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 25000, 0.8f),
            Def("monza", "Duccio Monza", CarCatalog.Kind.Motorcycle, CarCatalog.Unlock.LifetimeKm, 33000, 0.8f),
            // Sea
            Def("swanboat", "Swan Pedalo", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 600, 0.9f, false),
            Def("rowboat", "Rowboat", CarCatalog.Kind.Boat, CarCatalog.Unlock.MonthBestKm, 1200, 1.2f, false, "reach Lakeshore in one month"),
            Def("sailboat", "Sailboat", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 3000, 1.1f, false),
            Def("ferry", "Bosphorus Ferry", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 4000, 3.0f, false),
            Def("tugboat", "Tugboat", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 5500, 1.4f, false),
            Def("watercraft", "Water Scooter", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 7500, 0.8f, false),
            Def("speedboat", "Speedboat", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 11000, 1.2f, false),
            Def("seiner", "Fishing Boat", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 14000, 1.8f, false),
            Def("yacht", "Yacht", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 19000, 2.0f, false),
            Def("hovercraft", "Hovercraft", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 23000, 1.4f, false),
            Def("seabus", "Sea Bus", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 27000, 2.2f, false),
            Def("container", "Container Ship", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 37500, 3.2f, false),
            Def("icebreaker", "Icebreaker", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 42500, 2.8f, false),
            Def("submarine", "Submarine", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 47500, 1.8f, false),
            Def("liner", "Ocean Liner", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 50000, 3.2f, false),
            Def("cruise", "Cruise Ship", CarCatalog.Kind.Boat, CarCatalog.Unlock.LifetimeKm, 52500, 3.2f, false),
            // Sky
            Def("cropduster", "Crop Duster", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.MonthBestKm, 150, 1.1f, false, "reach Wheat Fields in one month"),
            Def("helicopter", "Helicopter", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 5000, 1.1f, false),
            Def("seaplane", "Seaplane", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 9000, 1.2f, false),
            Def("glider", "Glider", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 13000, 1.4f, false),
            Def("balloon", "Hot Air Balloon", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.MonthBestKm, 18000, 1.0f, false, "reach The City in one month"),
            Def("bizjet", "Business Jet", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 31000, 1.5f, false),
            Def("airship", "Airship", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 45000, 2.2f, false),
            Def("airliner", "Airliner", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 55000, 2.2f, false),
            Def("fighter", "Jet Fighter", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 57500, 1.4f, false),
            Def("shuttle", "Space Shuttle", CarCatalog.Kind.Aircraft, CarCatalog.Unlock.LifetimeKm, 60000, 1.6f, false),
        };

        static readonly (string id, string name, string swatch, bool secret, string glass)[] CarPaints =
        {
            ("coral", "Coral", "#E8644A", false, null),
            ("cherry", "Cherry", "#C8323C", false, null),
            ("mustard", "Mustard", "#E8B23A", false, null),
            ("mint", "Mint", "#6CC9A1", false, null),
            ("forest", "Forest", "#2F7D5B", false, null),
            ("teal", "Teal", "#2BB3C0", false, null),
            ("navy", "Navy", "#2E4A8C", false, null),
            ("lavender", "Lavender", "#9B86D6", false, null),
            ("graphite", "Graphite", "#4A4F5A", false, null),
            ("whisper", "Whisper", "#3A2552", true, "#C9B4F0"), // gizli bölge ödülü (lavanta cam)
        };

        // Sürücülü araçlar: sürücünün teni mercana yakın, gövde eşiği yükseltilir ki yüz boyanmasın.
        static readonly string[] RiderModels =
        {
            "mobilo", "vesper", "atv", "pcx", "chimp", "javo", "anfield", "bonnevale", "baron", "hayate", "monza",
            "heavyboy", "atlasgs", "tarantula", "snodoo",
        };

        // Açık denizde giden büyük gemiler; kalan tekneler (sandal, kuğu, jet ski, sürat teknesi,
        // yelkenli, hovercraft) nehirde.
        static readonly string[] OpenSeaModels =
        {
            "ferry", "tugboat", "seiner", "yacht", "seabus", "container", "icebreaker", "submarine", "liner", "cruise",
        };

        // Uçuş izi bırakan jetler (balon, zeplin, planör, pervaneliler ve helikopter bırakmaz).
        static readonly string[] ContrailModels = { "bizjet", "airliner", "fighter", "shuttle" };

        static readonly (string id, CarCatalog.Kind kind)[] ComingSoon =
        {
            ("soon_sport", CarCatalog.Kind.Car),
        };

        const string SpriteUnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // Kart arayüzünün referans boyutu (CompanionWindowController.CardBaseSize ile aynı).
        static readonly Vector2 CardReference = new Vector2(800, 340);

        // "Tabela" paleti: karayolu tabelası yeşili, asfalt, koyu plaka, sayaç rakamı kremi, kehribar vurgu.
        static readonly Color SkyColor = new Color(0.84f, 0.93f, 0.95f);
        static readonly Color Ink = new Color(0.11f, 0.13f, 0.16f);
        static readonly Color SignGreen = new Color(0.055f, 0.42f, 0.243f);
        static readonly Color Asphalt = new Color(0.10f, 0.11f, 0.135f);
        static readonly Color PlateDark = new Color(0.082f, 0.09f, 0.11f, 0.86f);
        static readonly Color PlateText = new Color(0.957f, 0.945f, 0.902f);
        static readonly Color Cell = new Color(0.133f, 0.145f, 0.172f);
        static readonly Color ButtonIdle = new Color(0.165f, 0.18f, 0.215f);
        static readonly Color Amber = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color TextLight = new Color(0.95f, 0.95f, 0.93f);
        static readonly Color TextMuted = new Color(0.957f, 0.945f, 0.902f, 0.58f);

        [MenuItem("ScrollRoad/Sahneleri Kur")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");

            ConfigureProjectSettings();
            // UI sprite'larının 9 parça payları ArtImportSettings'te; her kurulumda güncel olsun.
            AssetDatabase.ImportAsset("Assets/Art/UI", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            BuildCarCatalog();
            BuildGameScene();

            // Eski ayrı ana menü sahnesi artık yok.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyMainMenuScenePath) != null) AssetDatabase.DeleteAsset(LegacyMainMenuScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScenePath, true) };

            EditorSceneManager.OpenScene(GameScenePath);
            Debug.Log("ScrollRoad sahnesi kuruldu: Game (kart arayüzü), Build Settings güncellendi.");
        }

        // UniWindowController saydamlığı D3D12'de hiç, D3D11'de de flip-model swapchain açıkken
        // çalışmıyor (UniWinC README, Limitations). URP'de ayrıca HDR kapalı olmalı (alfa kanalı korunsun).
        public static void ConfigureProjectSettings()
        {
            PlayerSettings.productName = "ScrollRoad";
            // Kart boyutu; gerçek boyut açılışta monitör ölçeği ve Ayarlar'daki ön ayarla hesaplanır.
            PlayerSettings.defaultScreenWidth = (int)CardReference.x;
            PlayerSettings.defaultScreenHeight = (int)CardReference.y;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true; // kenarlık kaldırıldığı için kullanıcı yine boyutlandıramaz
            PlayerSettings.allowFullscreenSwitch = false; // Alt+Enter ile tam ekran olmasın
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.useFlipModelSwapchain = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            foreach (var asset in new[] { GraphicsSettings.defaultRenderPipeline, QualitySettings.renderPipeline })
            {
                if (asset is UniversalRenderPipelineAsset urp && urp.supportsHDR)
                {
                    urp.supportsHDR = false;
                    EditorUtility.SetDirty(urp);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static CarCatalog BuildCarCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(CarCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CarCatalog>();
                AssetDatabase.CreateAsset(catalog, CarCatalogPath);
            }

            catalog.paints = new CarCatalog.Paint[CarPaints.Length];
            for (int i = 0; i < CarPaints.Length; i++)
            {
                ColorUtility.TryParseHtmlString(CarPaints[i].swatch, out var swatch);
                catalog.paints[i] = new CarCatalog.Paint { id = CarPaints[i].id, displayName = CarPaints[i].name, swatch = swatch, secret = CarPaints[i].secret };
                if (CarPaints[i].glass != null && ColorUtility.TryParseHtmlString(CarPaints[i].glass, out var glass))
                {
                    catalog.paints[i].tintGlass = true;
                    catalog.paints[i].glass = glass;
                }
            }

            catalog.models = new CarCatalog.Model[CarModels.Length];
            for (int m = 0; m < CarModels.Length; m++)
            {
                var def = CarModels[m];
                WarnIfVehicleArtMissing(def.id);
                catalog.models[m] = new CarCatalog.Model
                {
                    id = def.id,
                    displayName = def.name,
                    kind = def.kind,
                    paintable = def.paintable,
                    unlock = def.unlock,
                    unlockValue = def.value,
                    unlockLabel = def.label,
                    widthScale = def.width,
                    bodyMinSat = System.Array.IndexOf(RiderModels, def.id) >= 0 ? 0.45f : 0.3f,
                    openSea = System.Array.IndexOf(OpenSeaModels, def.id) >= 0,
                    contrail = System.Array.IndexOf(ContrailModels, def.id) >= 0,
                };
            }

            catalog.comingSoon = new CarCatalog.Teaser[ComingSoon.Length];
            for (int i = 0; i < ComingSoon.Length; i++)
            {
                catalog.comingSoon[i] = new CarCatalog.Teaser
                {
                    id = ComingSoon[i].id,
                    kind = ComingSoon[i].kind,
                };
                WarnIfVehicleArtMissing(ComingSoon[i].id);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static void WarnIfVehicleArtMissing(string id)
        {
            if (!System.IO.File.Exists(VehicleArt.BasePath(id))) Debug.LogWarning($"[ScrollRoad] Araç görseli yok: {VehicleArt.BasePath(id)}");
        }

        static CarCatalog LoadCarCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(CarCatalogPath);
            return catalog != null ? catalog : BuildCarCatalog();
        }

        // ------------------------------------------------------------------ Oyun

        static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(SkyColor);

            // Gizli ortak puan motoru + seçili ortam (2026-10-13): RoadDrive ve ortamlar bundan ilerler.
            var engineGo = new GameObject("ScoreEngine");
            engineGo.AddComponent<ScoreEngine>();
            var environments = engineGo.AddComponent<EnvironmentManager>();

            var roadGo = new GameObject("RoadDrive");
            var drive = roadGo.AddComponent<RoadDrive>();

            var windowGo = new GameObject("CompanionWindow");
            var uni = windowGo.AddComponent<UniWindowController>();
            uni.isHitTestEnabled = false;          // CompanionWindowController saydamlığa geçerken açar
            uni.autoSwitchCameraBackground = false; // arka planı RoadSceneView yönetiyor
            uni.currentCamera = cam;
            uni.hitTestType = UniWindowController.HitTestType.Opacity;
            var companion = windowGo.AddComponent<CompanionWindowController>();
            Bind(companion, "uniWindow", uni);

            var view = roadGo.AddComponent<RoadSceneView>();
            Bind(view, "drive", drive);
            Bind(view, "companionWindow", companion);
            Bind(view, "viewCamera", cam);
            Bind(view, "carSprite", LoadArtSprite(AppIconPath)); // katalog yüklenene kadarki yedek
            Bind(view, "carCatalog", LoadCarCatalog());
            Bind(view, "citySprite", LoadArtSprite("Assets/Art/City/city_skyline.png"));
            Bind(view, "hillsSprite", LoadArtSprite("Assets/Art/Background/hills_far.png"));
            Bind(view, "spriteMaterial", AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath));

            var secret = roadGo.AddComponent<SecretZoneView>();
            Bind(secret, "drive", drive);
            var stats = roadGo.AddComponent<StatsTracker>();
            Bind(stats, "drive", drive);
            roadGo.AddComponent<LeaderboardSync>();
            var route = roadGo.AddComponent<RouteProgress>();
            Bind(route, "drive", drive);
            var goals = engineGo.AddComponent<GoalTracker>();

            // Olta ortamı: mantık + sahne (yalnız seçiliyken çizer ve kamerayı yönetir).
            var fishingGo = new GameObject("Fishing");
            var fishing = fishingGo.AddComponent<FishingController>();
            var lake = fishingGo.AddComponent<FishingSceneView>();
            Bind(lake, "fishing", fishing);
            Bind(lake, "companionWindow", companion);
            Bind(lake, "viewCamera", cam);
            BindArray(lake, "fishermanSprites", new Object[] { LoadArtSprite(FishingArt + "fisherman.png"), LoadArtSprite(FishingArt + "fisherman_river.png"), LoadArtSprite(FishingArt + "fisherman_coast.png"),
                LoadArtSprite(FishingArt + "fisherman_boat.png"), LoadArtSprite(FishingArt + "fisherman_ice.png") });
            BindArray(lake, "fishSprites", FishSprites());
            Bind(lake, "hillsSprite", LoadArtSprite("Assets/Art/Background/hills_far.png"));
            Bind(lake, "spriteMaterial", AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath));

            // Döner kapı ortamı (2026-10-14): mantık + sahne (yalnız seçiliyken çizer ve kamerayı yönetir).
            var doorGo = new GameObject("RevolvingDoor");
            var door = doorGo.AddComponent<DoorController>();
            var mall = doorGo.AddComponent<DoorSceneView>();
            Bind(mall, "door", door);
            Bind(mall, "companionWindow", companion);
            Bind(mall, "viewCamera", cam);
            BindArray(mall, "walkerSprites", DoorSprites(VisitorCatalog.Walkers));
            BindArray(mall, "guestSprites", DoorSprites(VisitorCatalog.AllGuests));
            Bind(mall, "citySprite", LoadArtSprite("Assets/Art/City/city_skyline.png"));
            Bind(mall, "spriteMaterial", AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath));

            var sound = roadGo.AddComponent<SoundController>();
            Bind(sound, "drive", drive);
            var vehicleSound = roadGo.AddComponent<VehicleSound>();
            Bind(vehicleSound, "drive", drive);
            Bind(vehicleSound, "catalog", LoadCarCatalog());
            BindArray(sound, "stepClips", new Object[] { LoadClip("tick_001"), LoadClip("tick_002"), LoadClip("tick_004") });
            Bind(sound, "turnClip", LoadClip("scratch_001"));
            Bind(sound, "cityReachedClip", LoadClip("city_reached"));
            Bind(sound, "uiClickClip", LoadClip("click_001"));

            Bind(sound, "companionWindow", companion);
            CreateHud(drive, view, secret, companion, route, goals, fishing, door, out var notice, out var windowButtons);
            Bind(fishing, "notice", notice);
            Bind(door, "notice", notice);
            Bind(environments, "notice", notice);
            var unlocks = roadGo.AddComponent<CarUnlockTracker>();
            Bind(unlocks, "stats", stats);
            Bind(unlocks, "drive", drive);
            Bind(unlocks, "catalog", LoadCarCatalog());
            Bind(unlocks, "notice", notice);
            var menu = CreateMenu(drive, stats, goals, companion, windowButtons, fishing, door);
            CreateOnboarding(companion, menu);
            CreateCompanionHud(drive, secret, companion, notice);

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        // Kartın pencere düğmeleri (sağ üst): ≡ menü, – simge durumu, × kapat.
        struct WindowButtons
        {
            public Button Menu, Minimize, Close;
        }

        const string FishingArt = "Assets/Art/Fishing/";

        const string DoorArt = "Assets/Art/Door/";

        static Object[] DoorSprites(VisitorType[] types)
        {
            var sprites = new Object[types.Length];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = LoadArtSprite(DoorArt + types[i].Id + ".png");
            return sprites;
        }

        static Object[] FishSprites()
        {
            var sprites = new Object[FishCatalog.All.Length];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = LoadArtSprite(FishingArt + FishCatalog.All[i].Id + ".png");
            return sprites;
        }

        static HudController CreateHud(RoadDrive drive, RoadSceneView view, SecretZoneView secret,
            CompanionWindowController companion, RouteProgress route, GoalTracker goals, FishingController fishing,
            DoorController door, out NoticeController notice, out WindowButtons windowButtons)
        {
            var canvas = CreateCanvas("HudCanvas", 0);
            var root = new GameObject("HudRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            // Görünmez sürükleme yüzeyi: en arkada, kartın boş yerlerinden pencereyi taşır.
            var dragSurface = new GameObject("DragSurface", typeof(RectTransform), typeof(Image), typeof(CardDragHandle));
            dragSurface.transform.SetParent(root.transform, false);
            dragSurface.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            Stretch(dragSurface.GetComponent<RectTransform>());
            Bind(dragSurface.GetComponent<CardDragHandle>(), "window", companion);

            // Sol üst: hız göstergesi (skinli kadran, GaugeView; kilometre sayacı kadranın içinde).
            var gauge = CreateGauge(root.transform, "Gauge", 128f, false, drive);
            Anchor((RectTransform)gauge.transform, new Vector2(0, 1), new Vector2(12, -8), new Vector2(0, 1));

            // Sağ üst: yeşil karayolu tabelası (bölge / sonraki durak + mesafe).
            var sign = CreateSign(root.transform, "Sign", out var signInner, out _);
            Anchor((RectTransform)sign.transform, new Vector2(1, 1), new Vector2(-116, -10), new Vector2(1, 1));
            var region = Tmp(signInner, "Region", "", 8.5f, UiFonts.ExtraBold, new Color(1f, 1f, 1f, 0.82f), TextAlignmentOptions.Left);
            region.characterSpacing = 16;
            var stopRow = Row(signInner, "StopRow", 14, TextAnchor.LowerLeft);
            var town = Tmp(stopRow.transform, "Town", "", 17, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
            var stopDistance = Tmp(stopRow.transform, "Distance", "", 12, UiFonts.SemiBold, Color.white, TextAlignmentOptions.Left);
            Object.DestroyImmediate(stopRow.GetComponent<ContentSizeFitter>()); // iç satırı dış düzen boyutlandırıyor

            windowButtons = new WindowButtons
            {
                Close = CreateIconButton(root.transform, "CloseWindowButton", IconClose, new Vector2(-12, -12)),
                Minimize = CreateIconButton(root.transform, "MinimizeButton", IconMinimize, new Vector2(-44, -12)),
                Menu = CreateIconButton(root.transform, "MenuButton", IconMenu, new Vector2(-76, -12)),
            };

            // Alt plakalar: solda şehre ilerleme + kalan mesafe, sağda bugünkü hedefler.
            var cityPlate = PlateRow(root.transform, "CityPlate");
            Anchor((RectTransform)cityPlate.transform, new Vector2(0, 0), new Vector2(16, 9), new Vector2(0, 0));
            var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            track.transform.SetParent(cityPlate.transform, false);
            track.GetComponent<Image>().color = new Color(PlateText.r, PlateText.g, PlateText.b, 0.25f);
            track.GetComponent<Image>().raycastTarget = false;
            track.GetComponent<LayoutElement>().preferredWidth = 96;
            track.GetComponent<LayoutElement>().preferredHeight = 4;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            fill.GetComponent<Image>().color = Amber;
            fill.GetComponent<Image>().raycastTarget = false;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            var marker = new GameObject("MaxMarker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(track.transform, false);
            marker.GetComponent<Image>().color = PlateText;
            marker.GetComponent<Image>().raycastTarget = false;
            var markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = new Vector2(0, 1);
            markerRect.sizeDelta = new Vector2(2, 10);

            var city = Tmp(cityPlate.transform, "CityRemaining", "", 11, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            city.characterSpacing = 3;

            var goalsPlate = PlateRow(root.transform, "TodayPlate");
            Anchor((RectTransform)goalsPlate.transform, new Vector2(1, 0), new Vector2(-16, 9), new Vector2(1, 0));
            var goalsText = Tmp(goalsPlate.transform, "Today", "", 11, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            goalsText.characterSpacing = 3;

            var whisper = Tmp(root.transform, "Whisper", "", 17, UiFonts.SemiBold, SecretZoneView.WhisperColor);
            whisper.fontStyle = FontStyles.Italic;
            SetRect(whisper.rectTransform, new Vector2(0, 46), new Vector2(760, 30));

            var hud = new GameObject("HudController").AddComponent<HudController>();
            Bind(hud, "drive", drive);
            Bind(hud, "secretZone", secret);
            Bind(hud, "companionWindow", companion);
            Bind(hud, "hudRoot", root);
            Bind(hud, "signRoot", sign);
            Bind(hud, "regionText", region);
            Bind(hud, "townText", town);
            Bind(hud, "stopDistanceText", stopDistance);
            Bind(hud, "cityText", city);
            Bind(hud, "goals", goals);
            Bind(hud, "goalsText", goalsText);
            Bind(hud, "whisperText", whisper);
            Bind(hud, "progressFill", fillRect);
            Bind(hud, "maxMarker", markerRect);
            BindArray(hud, "roadOnly", new Object[] { gauge.gameObject, sign, cityPlate, whisper.gameObject });

            CreateFishingHud(root.transform, fishing);
            CreateDoorHud(root.transform, door);

            var celebration = CreateCelebrationBanner(canvas.transform, drive, companion);
            notice = CreateNotice(canvas.transform, route, goals, companion, celebration);
            return hud;
        }

        // Olta'nın kart HUD'u (Yol HUD'unun yerleşimiyle): sol üstte toplam kg plakası, sağ üstte göl
        // tabelası (durum + yemin uzaklığı), sol altta balık defteri özeti. Bugünkü hedef plakası ortak.
        static void CreateFishingHud(Transform hudRoot, FishingController fishing)
        {
            var root = new GameObject("FishingHud", typeof(RectTransform));
            root.transform.SetParent(hudRoot, false);
            Stretch((RectTransform)root.transform);
            root.transform.SetSiblingIndex(1); // sürükleme yüzeyinin hemen üstünde, pencere düğmelerinin altında

            var totalPlate = Plate(root.transform, "TotalPlate", PlateDark, RoundSprite);
            Anchor(totalPlate.rectTransform, new Vector2(0, 1), new Vector2(12, -10), new Vector2(0, 1));
            totalPlate.rectTransform.sizeDelta = new Vector2(176, 66);
            var label = Tmp(totalPlate.transform, "Label", "TOTAL CATCH", 8.5f, UiFonts.ExtraBold, TextMuted, TextAlignmentOptions.TopLeft);
            label.characterSpacing = 16;
            Anchor(label.rectTransform, new Vector2(0, 1), new Vector2(11, -8), new Vector2(0, 1));
            label.rectTransform.sizeDelta = new Vector2(150, 12);
            var total = Tmp(totalPlate.transform, "Total", "", 22, UiFonts.Mono, PlateText, TextAlignmentOptions.TopLeft);
            Anchor(total.rectTransform, new Vector2(0, 1), new Vector2(11, -20), new Vector2(0, 1));
            total.rectTransform.sizeDelta = new Vector2(160, 26);
            var status = Tmp(totalPlate.transform, "Status", "", 9.5f, UiFonts.ExtraBold, TextMuted, TextAlignmentOptions.TopLeft);
            status.characterSpacing = 10;
            Anchor(status.rectTransform, new Vector2(0, 1), new Vector2(11, -48), new Vector2(0, 1));
            status.rectTransform.sizeDelta = new Vector2(160, 12);

            var sign = CreateSign(root.transform, "LakeSign", out var signInner, out _);
            Anchor((RectTransform)sign.transform, new Vector2(1, 1), new Vector2(-116, -10), new Vector2(1, 1));
            var place = Tmp(signInner, "Place", "", 8.5f, UiFonts.ExtraBold, new Color(1f, 1f, 1f, 0.82f), TextAlignmentOptions.Left);
            place.characterSpacing = 16;
            var phaseRow = Row(signInner, "PhaseRow", 14, TextAnchor.LowerLeft);
            var phase = Tmp(phaseRow.transform, "Phase", "", 17, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
            var distance = Tmp(phaseRow.transform, "Distance", "", 12, UiFonts.SemiBold, Color.white, TextAlignmentOptions.Left);
            Object.DestroyImmediate(phaseRow.GetComponent<ContentSizeFitter>());

            var logPlate = PlateRow(root.transform, "FishLogPlate");
            Anchor((RectTransform)logPlate.transform, new Vector2(0, 0), new Vector2(16, 9), new Vector2(0, 0));
            var log = Tmp(logPlate.transform, "FishLog", "", 11, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            log.characterSpacing = 3;

            var controller = new GameObject("FishingHudController").AddComponent<FishingHudController>();
            Bind(controller, "fishing", fishing);
            Bind(controller, "root", root);
            Bind(controller, "totalText", total);
            Bind(controller, "statusText", status);
            Bind(controller, "placeText", place);
            Bind(controller, "phaseText", phase);
            Bind(controller, "distanceText", distance);
            Bind(controller, "logText", log);
            root.SetActive(false);
        }

        // Döner kapının kart HUD'u (Olta HUD'unun yerleşimiyle): sol üstte ziyaretçi plakası, sağ üstte
        // mekân tabelası (sıradaki ziyaretçi / yaklaşan misafir), sol altta misafir defteri özeti.
        static void CreateDoorHud(Transform hudRoot, DoorController door)
        {
            var root = new GameObject("DoorHud", typeof(RectTransform));
            root.transform.SetParent(hudRoot, false);
            Stretch((RectTransform)root.transform);
            root.transform.SetSiblingIndex(1);

            var totalPlate = Plate(root.transform, "TotalPlate", PlateDark, RoundSprite);
            Anchor(totalPlate.rectTransform, new Vector2(0, 1), new Vector2(12, -10), new Vector2(0, 1));
            totalPlate.rectTransform.sizeDelta = new Vector2(176, 66);
            var label = Tmp(totalPlate.transform, "Label", "VISITORS", 8.5f, UiFonts.ExtraBold, TextMuted, TextAlignmentOptions.TopLeft);
            label.characterSpacing = 16;
            Anchor(label.rectTransform, new Vector2(0, 1), new Vector2(11, -8), new Vector2(0, 1));
            label.rectTransform.sizeDelta = new Vector2(150, 12);
            var total = Tmp(totalPlate.transform, "Total", "", 22, UiFonts.Mono, PlateText, TextAlignmentOptions.TopLeft);
            Anchor(total.rectTransform, new Vector2(0, 1), new Vector2(11, -20), new Vector2(0, 1));
            total.rectTransform.sizeDelta = new Vector2(160, 26);
            var status = Tmp(totalPlate.transform, "Status", "", 9.5f, UiFonts.ExtraBold, TextMuted, TextAlignmentOptions.TopLeft);
            status.characterSpacing = 10;
            Anchor(status.rectTransform, new Vector2(0, 1), new Vector2(11, -48), new Vector2(0, 1));
            status.rectTransform.sizeDelta = new Vector2(160, 12);

            var sign = CreateSign(root.transform, "VenueSign", out var signInner, out _);
            Anchor((RectTransform)sign.transform, new Vector2(1, 1), new Vector2(-116, -10), new Vector2(1, 1));
            var place = Tmp(signInner, "Place", "", 8.5f, UiFonts.ExtraBold, new Color(1f, 1f, 1f, 0.82f), TextAlignmentOptions.Left);
            place.characterSpacing = 16;
            var phaseRow = Row(signInner, "PhaseRow", 14, TextAnchor.LowerLeft);
            var phase = Tmp(phaseRow.transform, "Phase", "", 12, UiFonts.SemiBold, Color.white, TextAlignmentOptions.Left);
            var guest = Tmp(phaseRow.transform, "Guest", "", 17, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
            Object.DestroyImmediate(phaseRow.GetComponent<ContentSizeFitter>());

            var logPlate = PlateRow(root.transform, "GuestBookPlate");
            Anchor((RectTransform)logPlate.transform, new Vector2(0, 0), new Vector2(16, 9), new Vector2(0, 0));
            var log = Tmp(logPlate.transform, "GuestBook", "", 11, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            log.characterSpacing = 3;

            var controller = new GameObject("DoorHudController").AddComponent<DoorHudController>();
            Bind(controller, "door", door);
            Bind(controller, "root", root);
            Bind(controller, "totalText", total);
            Bind(controller, "statusText", status);
            Bind(controller, "placeText", place);
            Bind(controller, "phaseText", phase);
            Bind(controller, "guestText", guest);
            Bind(controller, "logText", log);
            root.SetActive(false);
        }

        // Durak/hedef duyurusu: üst ortada küçük yeşil tabela.
        static NoticeController CreateNotice(Transform canvas, RouteProgress route, GoalTracker goals,
            CompanionWindowController companion, CelebrationController celebration)
        {
            var sign = CreateSign(canvas, "NoticeCard", out var inner, out _);
            sign.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Anchor((RectTransform)sign.transform, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(0.5f, 1));
            inner.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            var title = Tmp(inner, "Title", "", 15, UiFonts.ExtraBold, Color.white);
            var subtitle = Tmp(inner, "Subtitle", "", 10.5f, UiFonts.SemiBold, new Color(1f, 1f, 1f, 0.78f));

            // Durak duyurusunda tabelanın sağ üst köşesine basılan pasaport damgası (bölge renginde,
            // pasaport panelindeki damgalarla aynı dil). Düzen dışı: tabelanın boyutunu etkilemez.
            var stamp = Plate(sign.transform, "Stamp", Color.white, RoundSprite);
            stamp.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Anchor(stamp.rectTransform, new Vector2(1, 1), new Vector2(-6, -2), new Vector2(0.5f, 0.5f));
            stamp.rectTransform.sizeDelta = new Vector2(70, 21);
            var stampText = Tmp(stamp.transform, "Label", "STAMPED", 9.5f, UiFonts.ExtraBold, PlateText);
            stampText.characterSpacing = 8;
            Stretch(stampText.rectTransform);
            stampText.rectTransform.offsetMin = new Vector2(0, -1);
            stamp.gameObject.SetActive(false);

            sign.SetActive(false);
            var controller = new GameObject("NoticeController").AddComponent<NoticeController>();
            Bind(controller, "stamp", stamp);
            Bind(controller, "stampText", stampText);
            Bind(controller, "route", route);
            Bind(controller, "goals", goals);
            Bind(controller, "companionWindow", companion);
            Bind(controller, "celebration", celebration);
            Bind(controller, "cardGroup", sign.GetComponent<CanvasGroup>());
            Bind(controller, "titleText", title);
            Bind(controller, "subtitleText", subtitle);
            return controller;
        }

        // Şehre varış: büyük yeşil tabela.
        static CelebrationController CreateCelebrationBanner(Transform canvas, RoadDrive drive, CompanionWindowController companion)
        {
            var sign = CreateSign(canvas, "CityBanner", out var inner, out _);
            sign.AddComponent<CanvasGroup>().blocksRaycasts = false;
            SetRect((RectTransform)sign.transform, new Vector2(0, 40), Vector2.zero);
            var layout = inner.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(26, 26, 10, 9);

            var title = Tmp(inner, "Title", "YOU REACHED THE CITY!", 26, UiFonts.ExtraBold, Color.white);
            title.characterSpacing = 4;
            // Gizli reveal kancası: kim şehre ulaştı, ay sonuna kadar rakiplerden gizli.
            Tmp(inner, "Subtitle", "Your rivals can't see this. Wait for the end of the month.", 12, UiFonts.SemiBold, new Color(1f, 1f, 1f, 0.85f));

            sign.SetActive(false);
            var controller = new GameObject("CelebrationController").AddComponent<CelebrationController>();
            Bind(controller, "drive", drive);
            Bind(controller, "companionWindow", companion);
            Bind(controller, "bannerGroup", sign.GetComponent<CanvasGroup>());
            return controller;
        }

        // İlk açılış rehberi: oyunu durdurmayan koyu plaka; sol üstteki sayaç ve sağ üstteki pencere
        // düğmeleri açık kalır, araba kartın alt kısmında görünür ("scroll = gaz" adımında tepkisini görsün).
        static void CreateOnboarding(CompanionWindowController companion, CardMenuController menu)
        {
            var canvas = CreateCanvas("OnboardingCanvas", 5);
            var card = Plate(canvas.transform, "OnboardingCard", new Color(Asphalt.r, Asphalt.g, Asphalt.b, 0.96f), RoundMediumSprite).gameObject;
            card.GetComponent<Image>().raycastTarget = true;
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = new Vector2(500, 150);
            Anchor(rect, new Vector2(0.5f, 1), new Vector2(50, -62), new Vector2(0.5f, 1)); // tabelanın altında, arabanın üstünde

            var step = Tmp(card.transform, "Step", "", 10, UiFonts.Mono, TextMuted, TextAlignmentOptions.Left);
            SetRect(step.rectTransform, new Vector2(-205, 57), new Vector2(60, 16));
            var title = Tmp(card.transform, "Title", "", 18, UiFonts.ExtraBold, Color.white);
            SetRect(title.rectTransform, new Vector2(0, 46), new Vector2(400, 26));
            var body = Tmp(card.transform, "Body", "", 12.5f, UiFonts.SemiBold, PlateText);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 4;
            SetRect(body.rectTransform, new Vector2(0, 9), new Vector2(456, 48));
            var hint = Tmp(card.transform, "Hint", "", 12.5f, UiFonts.SemiBold, Amber);
            hint.fontStyle = FontStyles.Italic;
            SetRect(hint.rectTransform, new Vector2(0, -24), new Vector2(456, 20));

            var next = CreateButton(card.transform, "NextButton", "Next", new Vector2(186, -51), new Vector2(100, 26), SignGreen, 13, UiFonts.ExtraBold);
            var skip = CreateButton(card.transform, "SkipButton", "Skip", new Vector2(-206, -51), new Vector2(68, 22), new Color(1f, 1f, 1f, 0.06f), 11, UiFonts.SemiBold);

            var controller = new GameObject("OnboardingController").AddComponent<OnboardingController>();
            Bind(controller, "companionWindow", companion);
            Bind(controller, "menu", menu);
            Bind(controller, "card", card);
            Bind(controller, "stepText", step);
            Bind(controller, "titleText", title);
            Bind(controller, "bodyText", body);
            Bind(controller, "hintText", hint);
            Bind(controller, "nextButton", next);
            Bind(controller, "nextLabel", next.GetComponentInChildren<TMP_Text>());
            Bind(controller, "skipButton", skip);
        }

        // ≡ çekmecesi (yeşil çıkış tabelası) + kartı kaplayan paneller (Passport, Leaderboard, Garage, Stats, Settings).
        static CardMenuController CreateMenu(RoadDrive drive, StatsTracker stats, GoalTracker goals,
            CompanionWindowController companion, WindowButtons windowButtons, FishingController fishing, DoorController door)
        {
            var canvas = CreateCanvas("MenuCanvas", 10);

            // Çekmece açıkken kartın geri kalanı hafif kararır; tıklayınca çekmece kapanır.
            var blockerGo = new GameObject("DrawerBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
            blockerGo.transform.SetParent(canvas.transform, false);
            blockerGo.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.35f);
            Stretch(blockerGo.GetComponent<RectTransform>());
            var blocker = blockerGo.GetComponent<Button>();
            blocker.transition = Selectable.Transition.None;

            var drawerGo = new GameObject("Drawer", typeof(RectTransform), typeof(Image));
            drawerGo.transform.SetParent(canvas.transform, false);
            drawerGo.GetComponent<Image>().color = SignGreen;
            var drawer = drawerGo.GetComponent<RectTransform>();
            drawer.anchorMin = new Vector2(1, 0);
            drawer.anchorMax = new Vector2(1, 1);
            drawer.pivot = new Vector2(1, 0.5f);
            drawer.sizeDelta = new Vector2(248, 0);
            drawer.anchoredPosition = Vector2.zero;

            var ring = Plate(drawer, "Ring", Color.white, RingSprite);
            Stretch(ring.rectTransform);
            ring.rectTransform.offsetMin = new Vector2(7, 7);
            ring.rectTransform.offsetMax = new Vector2(-7, -7);

            var bestLabel = Tmp(drawer, "MonthBestLabel", "MONTH BEST", 8.5f, UiFonts.ExtraBold, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.TopLeft);
            bestLabel.characterSpacing = 16;
            Anchor(bestLabel.rectTransform, new Vector2(0, 1), new Vector2(20, -19), new Vector2(0, 1));
            bestLabel.rectTransform.sizeDelta = new Vector2(208, 12);
            var best = Tmp(drawer, "MonthBest", "", 21, UiFonts.Mono, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(best.rectTransform, new Vector2(0, 1), new Vector2(20, -31), new Vector2(0, 1));
            best.rectTransform.sizeDelta = new Vector2(208, 26);
            var sub = Tmp(drawer, "HeaderSub", "", 10.5f, UiFonts.SemiBold, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.TopLeft);
            Anchor(sub.rectTransform, new Vector2(0, 1), new Vector2(20, -58), new Vector2(0, 1));
            sub.rectTransform.sizeDelta = new Vector2(208, 30);
            sub.lineSpacing = 6;

            // Activities = ortam seçici (2026-10-13). "Passport" satırı Olta'da "Fish log" olur.
            string[] labels = { "Activities", "Passport", "Leaderboard", "Garage", "Stats", "Settings", "Taskbar mode" };
            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = CreateDrawerRow(drawer, labels[i], new Vector2(20, -96 - i * 29));
            }

            var quit = CreateTextButton(drawer, "QuitButton", "QUIT", 10.5f, Color.white, new Vector2(20, 15), new Vector2(46, 18));
            var steam = Tmp(drawer, "SteamStatus", "", 8.5f, UiFonts.SemiBold, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.BottomLeft);
            steam.characterSpacing = 8;
            Anchor(steam.rectTransform, new Vector2(0, 0), new Vector2(72, 18), new Vector2(0, 0));
            steam.rectTransform.sizeDelta = new Vector2(156, 14);

            var activities = CreateActivitiesPanel(canvas.transform, buttons[0]);
            var passport = CreatePassportPanel(canvas.transform, buttons[1], drive);
            var fishLog = CreateFishLogPanel(canvas.transform, fishing);
            Bind(passport, "fishLog", fishLog);
            var guestBook = CreateGuestBookPanel(canvas.transform, door);
            Bind(passport, "guestBook", guestBook);
            var garage = CreateGaragePanel(canvas.transform, buttons[3]);
            var statsCtrl = CreateStatsPanel(canvas.transform, buttons[4], drive, stats);
            var settings = CreateSettingsPanel(canvas.transform, buttons[5], companion);

            var lbPanel = CreatePanel(canvas.transform, "LeaderboardPanel", "Leaderboard", out var lbClose, out _);
            var thisMonth = CreateButton(lbPanel.transform, "ThisMonthTab", "This month", Vector2.zero, new Vector2(104, 24), ButtonIdle, 11.5f, UiFonts.SemiBold);
            Anchor((RectTransform)thisMonth.transform, new Vector2(0, 1), new Vector2(176, -14), new Vector2(0, 1));
            var lastMonth = CreateButton(lbPanel.transform, "LastMonthTab", "Last month", Vector2.zero, new Vector2(104, 24), ButtonIdle, 11.5f, UiFonts.SemiBold);
            Anchor((RectTransform)lastMonth.transform, new Vector2(0, 1), new Vector2(286, -14), new Vector2(0, 1));
            var lbText = Tmp(lbPanel.transform, "Entries", "", 13, UiFonts.Mono, PlateText, TextAlignmentOptions.TopLeft);
            lbText.lineSpacing = 8;
            Anchor(lbText.rectTransform, new Vector2(0, 1), new Vector2(26, -60), new Vector2(0, 1));
            lbText.rectTransform.sizeDelta = new Vector2(748, 266);
            lbPanel.SetActive(false);

            var menu = new GameObject("CardMenuController").AddComponent<CardMenuController>();
            Bind(menu, "drive", drive);
            Bind(menu, "goals", goals);
            Bind(menu, "companionWindow", companion);
            Bind(menu, "menuButton", windowButtons.Menu);
            Bind(menu, "minimizeButton", windowButtons.Minimize);
            Bind(menu, "closeWindowButton", windowButtons.Close);
            Bind(menu, "drawer", drawer);
            Bind(menu, "drawerBlocker", blocker);
            Bind(menu, "monthBestText", best);
            Bind(menu, "headerSubText", sub);
            Bind(menu, "steamText", steam);
            BindArray(menu, "panelButtons", new Object[] { buttons[0], buttons[1], buttons[3], buttons[4], buttons[5] });
            Bind(menu, "leaderboardButton", buttons[2]);
            Bind(menu, "taskbarButton", buttons[6]);
            Bind(menu, "collectionLabel", buttons[1].GetComponentInChildren<TMP_Text>());
            Bind(menu, "quitButton", quit);
            Bind(menu, "leaderboardPanel", lbPanel);
            Bind(menu, "leaderboardText", lbText);
            Bind(menu, "thisMonthTab", thisMonth);
            Bind(menu, "lastMonthTab", lastMonth);
            Bind(menu, "leaderboardCloseButton", lbClose);
            BindArray(menu, "subPanels", new Object[] { passport.Panel, garage.Panel, settings.Panel, statsCtrl.Panel, activities.Panel, fishLog.Panel, guestBook.Panel });

            blockerGo.SetActive(false);
            drawerGo.SetActive(false);
            return menu;
        }

        // Ortam seçici: her ortam bir kart (küçük görsel, ad, tarif, birim; durum satırı; kilitliyse
        // ilerleme çubuğu). Kartlar EnvironmentCatalog.All sırasıyla.
        static ActivitiesController CreateActivitiesPanel(Transform parent, Button openButton)
        {
            var panel = CreatePanel(parent, "ActivitiesPanel", "Activities", out var close, out _);
            var hint = Tmp(panel.transform, "Hint", "Only the activity you pick moves forward. Your monthly score counts them all.",
                10.5f, UiFonts.SemiBold, TextMuted, TextAlignmentOptions.Left);
            Anchor(hint.rectTransform, new Vector2(0, 1), new Vector2(26, -52), new Vector2(0, 1));
            hint.rectTransform.sizeDelta = new Vector2(700, 16);

            var all = EnvironmentCatalog.All;
            Sprite[] pictures = { LoadArtSprite(AppIconPath), LoadArtSprite(FishingArt + "fisherman.png"), LoadArtSprite(DoorArt + "walker_coat.png") };
            int n = all.Length;
            var buttons = new Object[n];
            var frames = new Object[n];
            var thumbs = new Object[n];
            var statuses = new Object[n];
            var tracks = new Object[n];
            var fills = new Object[n];
            const float cardH = 232f, gap = 18f;
            float cardW = Mathf.Min(360f, (748f - (n - 1) * gap) / n); // 3 ortamda ~237
            float startX = -(n * cardW + (n - 1) * gap) / 2f + cardW / 2f;
            for (int i = 0; i < n; i++)
            {
                var def = all[i];
                var frame = Plate(panel.transform, $"Card_{def.Id}", Cell, RoundSprite);
                frame.raycastTarget = true;
                frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                SetRect(frame.rectTransform, new Vector2(startX + i * (cardW + gap), -28f), new Vector2(cardW, cardH));
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                var colors = button.colors;
                colors.disabledColor = Color.white; // kilitli/aktif kartın rengini Image.color belirler
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
                button.colors = colors;

                var thumb = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
                thumb.transform.SetParent(frame.transform, false);
                var thumbImage = thumb.GetComponent<Image>();
                thumbImage.sprite = pictures[Mathf.Min(i, pictures.Length - 1)];
                thumbImage.preserveAspect = true;
                thumbImage.raycastTarget = false;
                var tr = (RectTransform)thumb.transform;
                tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
                SetRect(tr, new Vector2(0f, -62f), new Vector2(200f, 100f));

                var name = Tmp(frame.transform, "Name", def.Name, 18, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Center);
                name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                SetRect(name.rectTransform, new Vector2(0f, -130f), new Vector2(cardW - 20f, 24f));
                var tag = Tmp(frame.transform, "Tagline", $"{def.Tagline}  <alpha=#88>Counted in {def.Unit}.", 10.5f, UiFonts.SemiBold, TextMuted, TextAlignmentOptions.Center);
                tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                SetRect(tag.rectTransform, new Vector2(0f, -160f), new Vector2(cardW - 20f, 30f)); // dar kartta iki satır
                tag.textWrappingMode = TextWrappingModes.Normal;

                var status = Tmp(frame.transform, "Status", "", 10.5f, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Center);
                status.characterSpacing = 8;
                status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                SetRect(status.rectTransform, new Vector2(0f, 38f), new Vector2(cardW - 20f, 16f));

                var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
                track.transform.SetParent(frame.transform, false);
                track.GetComponent<Image>().color = new Color(PlateText.r, PlateText.g, PlateText.b, 0.2f);
                track.GetComponent<Image>().raycastTarget = false;
                var trackRect = (RectTransform)track.transform;
                trackRect.anchorMin = trackRect.anchorMax = new Vector2(0.5f, 0f);
                SetRect(trackRect, new Vector2(0f, 20f), new Vector2(cardW - 80f, 5f));
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(track.transform, false);
                fill.GetComponent<Image>().color = Amber;
                fill.GetComponent<Image>().raycastTarget = false;
                var fillRect = (RectTransform)fill.transform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

                buttons[i] = button;
                frames[i] = frame;
                thumbs[i] = thumbImage;
                statuses[i] = status;
                tracks[i] = track;
                fills[i] = fillRect;
            }

            panel.SetActive(false);
            var controller = new GameObject("ActivitiesController").AddComponent<ActivitiesController>();
            Bind(controller, "panel", panel);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            BindArray(controller, "cardButtons", buttons);
            BindArray(controller, "cardFrames", frames);
            BindArray(controller, "thumbs", thumbs);
            BindArray(controller, "statusTexts", statuses);
            BindArray(controller, "progressTracks", tracks);
            BindArray(controller, "progressFills", fills);
            return controller;
        }

        // Balık defteri: üstte mekân sekmeleri + "Fish here", altta seçili mekânın türleri 3x2 yuvada
        // (görsel, ad, en büyük kg, sayı, ilk tutuluş). Her mekânın yuvaları aynı yerde üst üste; denetleyici
        // yalnız seçili sekmeninkileri açar.
        static FishLogController CreateFishLogPanel(Transform parent, FishingController fishing)
        {
            var panel = CreatePanel(parent, "FishLogPanel", "Fish log · " + FishCatalog.Lake.Name, out var close, out var title);
            var count = Tmp(panel.transform, "Count", "", 10.5f, UiFonts.SemiBold, TextMuted, TextAlignmentOptions.Right);
            Anchor(count.rectTransform, new Vector2(1, 1), new Vector2(-50, -18), new Vector2(1, 1));
            count.rectTransform.sizeDelta = new Vector2(420, 16);
            count.characterSpacing = 2;

            // Sekmeler soldan (5 mekân), "Fish here" sağda; aynı satır.
            const float tabY = 112f, tabW = 118f, tabH = 24f, tabGap = 6f;
            var tabs = new Object[FishCatalog.Spots.Length];
            var tabTexts = new Object[FishCatalog.Spots.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = CreateButton(panel.transform, $"Tab_{FishCatalog.Spots[i].Id}", FishCatalog.Spots[i].Name,
                    new Vector2(-376 + tabW / 2f + i * (tabW + tabGap), tabY), new Vector2(tabW, tabH), ButtonIdle, 10.5f, UiFonts.ExtraBold);
                tabs[i] = tab;
                tabTexts[i] = tab.GetComponentInChildren<TMP_Text>();
            }
            var fishHere = CreateButton(panel.transform, "FishHereButton", "FISH HERE", new Vector2(376 - 58f, tabY),
                new Vector2(116f, tabH), SignGreen, 10.5f, UiFonts.ExtraBold);

            var sprites = FishSprites();
            int n = FishCatalog.All.Length;
            var images = new Object[n];
            var roots = new Object[n];
            var names = new Object[n];
            var details = new Object[n];
            const float cellW = 240f, cellH = 116f, gapX = 12f, gapY = 8f, firstRowY = 38f;
            foreach (var spot in FishCatalog.Spots)
            {
                // Gizli efsane: ızgaranın altında altın şerit (tutulunca görünür). Diziler FishCatalog.All sırasında.
                if (spot.Legendary != null)
                {
                    int li = FishCatalog.IndexOf(spot.Legendary.Id);
                    var band = Plate(panel.transform, $"Legend_{spot.Legendary.Id}", new Color(0.78f, 0.60f, 0.16f), RoundSprite);
                    band.rectTransform.anchorMin = band.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    SetRect(band.rectTransform, new Vector2(0f, -158f), new Vector2(3 * cellW + 2 * gapX, 22f));
                    var legendGo = new GameObject("Fish", typeof(RectTransform), typeof(Image));
                    legendGo.transform.SetParent(band.transform, false);
                    var legendImage = legendGo.GetComponent<Image>();
                    legendImage.sprite = (Sprite)sprites[li];
                    legendImage.preserveAspect = true;
                    legendImage.raycastTarget = false;
                    var lr = (RectTransform)legendGo.transform;
                    lr.anchorMin = lr.anchorMax = new Vector2(0f, 0.5f);
                    SetRect(lr, new Vector2(28f, 0f), new Vector2(44f, 20f));
                    var legendName = Tmp(band.transform, "Name", "", 11.5f, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
                    legendName.rectTransform.anchorMin = legendName.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    SetRect(legendName.rectTransform, new Vector2(60f + 130f, 0f), new Vector2(260f, 18f));
                    var legendDetail = Tmp(band.transform, "Detail", "", 10f, UiFonts.SemiBold, Color.white, TextAlignmentOptions.Right);
                    legendDetail.rectTransform.anchorMin = legendDetail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    SetRect(legendDetail.rectTransform, new Vector2(-10f - 200f, 0f), new Vector2(400f, 18f));
                    images[li] = legendImage;
                    roots[li] = band.gameObject;
                    names[li] = legendName;
                    details[li] = legendDetail;
                    band.gameObject.SetActive(false);
                }

                for (int j = 0; j < spot.Species.Length; j++)
                {
                    int slot = FishCatalog.IndexOf(spot.Species[j].Id);
                    int col = j % 3, row = j / 3;
                    var cell = Plate(panel.transform, $"Slot_{spot.Species[j].Id}", Cell, RoundSprite);
                    cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    float x = (col - 1) * (cellW + gapX);
                    float y = firstRowY - row * (cellH + gapY);
                    SetRect(cell.rectTransform, new Vector2(x, y), new Vector2(cellW, cellH));

                    var fishGo = new GameObject("Fish", typeof(RectTransform), typeof(Image));
                    fishGo.transform.SetParent(cell.transform, false);
                    var image = fishGo.GetComponent<Image>();
                    image.sprite = (Sprite)sprites[slot];
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    var fr = (RectTransform)fishGo.transform;
                    fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 1f);
                    SetRect(fr, new Vector2(0f, -32f), new Vector2(150f, 50f));

                    var name = Tmp(cell.transform, "Name", "", 13, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Center);
                    name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    SetRect(name.rectTransform, new Vector2(0f, 43f), new Vector2(cellW - 12f, 18f));
                    var detail = Tmp(cell.transform, "Detail", "", 9.5f, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Center);
                    detail.lineSpacing = 4;
                    detail.rectTransform.anchorMin = detail.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    SetRect(detail.rectTransform, new Vector2(0f, 18f), new Vector2(cellW - 12f, 28f));

                    images[slot] = image;
                    roots[slot] = cell.gameObject;
                    names[slot] = name;
                    details[slot] = detail;
                }
            }

            panel.SetActive(false);
            var controller = new GameObject("FishLogController").AddComponent<FishLogController>();
            Bind(controller, "fishing", fishing);
            Bind(controller, "panel", panel);
            Bind(controller, "titleText", title);
            Bind(controller, "countText", count);
            BindArray(controller, "tabButtons", tabs);
            BindArray(controller, "tabTexts", tabTexts);
            Bind(controller, "fishHereButton", fishHere);
            Bind(controller, "fishHereText", fishHere.GetComponentInChildren<TMP_Text>());
            BindArray(controller, "fishImages", images);
            BindArray(controller, "slotRoots", roots);
            BindArray(controller, "nameTexts", names);
            BindArray(controller, "detailTexts", details);
            Bind(controller, "closeButton", close);
            return controller;
        }

        // Misafir defteri: balık defterinin yerleşimi — üstte mekân sekmeleri + "Go here", altta seçili mekânın
        // 6 nadir misafiri 3x2 yuvada (görsel solda, ad ve geliş bilgisi sağda; insanlar dikey).
        static GuestBookController CreateGuestBookPanel(Transform parent, DoorController door)
        {
            var panel = CreatePanel(parent, "GuestBookPanel", "Guest book · " + VisitorCatalog.Mall.Name, out var close, out var title);
            var count = Tmp(panel.transform, "Count", "", 10.5f, UiFonts.SemiBold, TextMuted, TextAlignmentOptions.Right);
            Anchor(count.rectTransform, new Vector2(1, 1), new Vector2(-50, -18), new Vector2(1, 1));
            count.rectTransform.sizeDelta = new Vector2(420, 16);
            count.characterSpacing = 2;

            const float tabY = 112f, tabW = 118f, tabH = 24f, tabGap = 6f;
            var venues = VisitorCatalog.Venues;
            var tabs = new Object[venues.Length];
            var tabTexts = new Object[venues.Length];
            for (int i = 0; i < venues.Length; i++)
            {
                var tab = CreateButton(panel.transform, $"Tab_{venues[i].Id}", venues[i].Name,
                    new Vector2(-376 + tabW / 2f + i * (tabW + tabGap), tabY), new Vector2(tabW, tabH), ButtonIdle, 10.5f, UiFonts.ExtraBold);
                tabs[i] = tab;
                tabTexts[i] = tab.GetComponentInChildren<TMP_Text>();
            }
            var here = CreateButton(panel.transform, "HereButton", "YOU ARE HERE", new Vector2(376 - 58f, tabY),
                new Vector2(116f, tabH), SignGreen, 10.5f, UiFonts.ExtraBold);

            var sprites = DoorSprites(VisitorCatalog.AllGuests);
            int n = VisitorCatalog.AllGuests.Length;
            var images = new Object[n];
            var roots = new Object[n];
            var names = new Object[n];
            var details = new Object[n];
            const float cellW = 240f, cellH = 116f, gapX = 12f, gapY = 8f, firstRowY = 38f;
            foreach (var venue in venues)
            {
                for (int j = 0; j < venue.Guests.Length; j++)
                {
                    int slot = VisitorCatalog.GuestIndex(venue.Guests[j].Id);
                    int col = j % 3, row = j / 3;
                    var cell = Plate(panel.transform, $"Slot_{venue.Guests[j].Id}", Cell, RoundSprite);
                    cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    SetRect(cell.rectTransform, new Vector2((col - 1) * (cellW + gapX), firstRowY - row * (cellH + gapY)), new Vector2(cellW, cellH));

                    var guestGo = new GameObject("Guest", typeof(RectTransform), typeof(Image));
                    guestGo.transform.SetParent(cell.transform, false);
                    var image = guestGo.GetComponent<Image>();
                    image.sprite = (Sprite)sprites[slot];
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    var gr = (RectTransform)guestGo.transform;
                    gr.anchorMin = gr.anchorMax = new Vector2(0f, 0.5f);
                    SetRect(gr, new Vector2(46f, 0f), new Vector2(76f, 104f));

                    var name = Tmp(cell.transform, "Name", "", 13, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
                    name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    SetRect(name.rectTransform, new Vector2(90f + 70f, 16f), new Vector2(140f, 18f));
                    var detail = Tmp(cell.transform, "Detail", "", 9.5f, UiFonts.SemiBold, PlateText, TextAlignmentOptions.TopLeft);
                    detail.lineSpacing = 4;
                    detail.rectTransform.anchorMin = detail.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    SetRect(detail.rectTransform, new Vector2(90f + 70f, -12f), new Vector2(140f, 30f));

                    images[slot] = image;
                    roots[slot] = cell.gameObject;
                    names[slot] = name;
                    details[slot] = detail;
                }
            }

            panel.SetActive(false);
            var controller = new GameObject("GuestBookController").AddComponent<GuestBookController>();
            Bind(controller, "door", door);
            Bind(controller, "panel", panel);
            Bind(controller, "titleText", title);
            Bind(controller, "countText", count);
            BindArray(controller, "tabButtons", tabs);
            BindArray(controller, "tabTexts", tabTexts);
            Bind(controller, "hereButton", here);
            Bind(controller, "hereText", here.GetComponentInChildren<TMP_Text>());
            BindArray(controller, "guestImages", images);
            BindArray(controller, "slotRoots", roots);
            BindArray(controller, "nameTexts", names);
            BindArray(controller, "detailTexts", details);
            Bind(controller, "closeButton", close);
            return controller;
        }

        // Çekmece satırı: üstte ince ayraç, solda ad, sağda ok (çıkış tabelası listesi gibi).
        static Button CreateDrawerRow(Transform drawer, string label, Vector2 topLeft)
        {
            var go = new GameObject(label.Split(' ')[0] + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(drawer, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(208, 29);
            Anchor(rect, new Vector2(0, 1), topLeft, new Vector2(0, 1));
            go.GetComponent<Image>().color = Color.white;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(go.transform, false);
            divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.28f);
            divider.GetComponent<Image>().raycastTarget = false;
            var d = (RectTransform)divider.transform;
            d.anchorMin = new Vector2(0, 1);
            d.anchorMax = new Vector2(1, 1);
            d.pivot = new Vector2(0.5f, 1);
            d.sizeDelta = new Vector2(0, 1);
            d.anchoredPosition = Vector2.zero;

            var text = Tmp(go.transform, "Text", label, 14.5f, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(2, -2);

            var arrow = Icon(go.transform, "Arrow", IconArrow, 13, Color.white);
            Anchor(arrow.rectTransform, new Vector2(1, 0.5f), new Vector2(-2, 0), new Vector2(1, 0.5f));
            return button;
        }

        // Kartı tamamen kaplayan panel (koyu asfalt): sol üstte başlık tabelası, sağ üstte × kapat.
        static GameObject CreatePanel(Transform parent, string name, string title, out Button close, out TMP_Text titleText)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = Asphalt;
            Stretch(panel.GetComponent<RectTransform>());

            var sign = CreateSign(panel.transform, "TitleSign", out var inner, out _);
            Anchor((RectTransform)sign.transform, new Vector2(0, 1), new Vector2(14, -10), new Vector2(0, 1));
            inner.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(11, 11, 5, 3);
            titleText = Tmp(inner, "Title", title, 14, UiFonts.ExtraBold, Color.white, TextAlignmentOptions.Left);
            titleText.fontStyle = FontStyles.UpperCase;
            titleText.characterSpacing = 4;

            close = CreateIconButton(panel.transform, "CloseButton", IconClose, new Vector2(-12, -12));
            return panel;
        }

        // Pasaport: her satır bir bölge (bölge damgası + kasabaları), son satır şehir.
        // Damgalar RouteMap.Stops sırasıyla oluşturulur (PassportController bu sıraya güvenir).
        static PassportController CreatePassportPanel(Transform parent, Button openButton, RoadDrive drive)
        {
            var panel = CreatePanel(parent, "PassportPanel", "Passport", out var close, out var title);
            var count = Tmp(panel.transform, "Count", "", 10.5f, UiFonts.SemiBold, TextMuted, TextAlignmentOptions.Right);
            Anchor(count.rectTransform, new Vector2(1, 1), new Vector2(-50, -18), new Vector2(1, 1));
            count.rectTransform.sizeDelta = new Vector2(320, 16);
            count.characterSpacing = 2;

            const float rowStep = 27f, rowHeight = 23f, regionWidth = 160f, townWidth = 140f, gap = 8f, firstRowTop = -56f;
            var frames = new Object[RouteMap.Stops.Count];
            var texts = new Object[RouteMap.Stops.Count];
            int row = -1;
            float x = 0f;
            for (int i = 0; i < RouteMap.Stops.Count; i++)
            {
                var stop = RouteMap.Stops[i];
                bool big = stop.Kind != RouteMap.StopKind.Town;
                if (big)
                {
                    row++;
                    x = -(regionWidth + 3 * (townWidth + gap)) / 2f; // en uzun satır (bölge + 3 kasaba) ortalansın
                }
                float w = big ? regionWidth : townWidth;

                var frame = Plate(panel.transform, $"Stamp_{i}", Color.white, RoundSprite);
                var rect = frame.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
                SetRect(rect, new Vector2(x + w / 2f, firstRowTop - row * rowStep - rowHeight / 2f), new Vector2(w, rowHeight));
                x += w + gap;

                var label = Tmp(frame.transform, "Name", stop.Name, big ? 12 : 11.5f, big ? UiFonts.ExtraBold : UiFonts.SemiBold, PlateText);
                Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(0, -1);

                frames[i] = frame;
                texts[i] = label;
            }

            panel.SetActive(false);
            var controller = new GameObject("PassportController").AddComponent<PassportController>();
            Bind(controller, "drive", drive);
            Bind(controller, "passportPanel", panel);
            Bind(controller, "titleText", title);
            Bind(controller, "countText", count);
            BindArray(controller, "stampFrames", frames);
            BindArray(controller, "stampTexts", texts);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            return controller;
        }

        // Garaj: solda seçili aracın önizlemesi + "MODEL · RENK" + kilit bilgisi; sağda kategori sekmeleri,
        // sekmenin araçları 5'erli sayfalarda (‹ ›), RENK yuvarlakları ve dünya teması.
        static GarageController CreateGaragePanel(Transform parent, Button openButton)
        {
            var catalog = LoadCarCatalog();
            var panel = CreatePanel(parent, "GaragePanel", "Garage", out var close, out _);
            const float gridX = 120f, rowWidth = 470f;
            const int slotsPerPage = 5;

            var preview = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            preview.transform.SetParent(panel.transform, false);
            var previewImage = preview.GetComponent<Image>();
            previewImage.preserveAspect = true;
            previewImage.raycastTarget = false;
            // sprite çalışma zamanında (GarageController.Refresh, VehicleArt): sahneye kaydedilemez
            SetRect(preview.GetComponent<RectTransform>(), new Vector2(-250, 16), new Vector2(230, 110));

            var nameText = Tmp(panel.transform, "SelectedName", "", 14, UiFonts.ExtraBold, PlateText);
            nameText.fontStyle = FontStyles.UpperCase;
            nameText.characterSpacing = 6;
            SetRect(nameText.rectTransform, new Vector2(-250, -58), new Vector2(250, 22));

            var detail = Tmp(panel.transform, "Detail", "", 10.5f, UiFonts.SemiBold, Amber);
            detail.textWrappingMode = TextWrappingModes.Normal;
            SetRect(detail.rectTransform, new Vector2(-250, -86), new Vector2(260, 34));

            // Kategori sekmeleri (satırın sol kenarından) + sayfa göstergesi (sağ kenar)
            float[] tabWidths = { 54, 76, 92, 46, 46 };
            var tabs = new Object[CarCatalog.TabNames.Length];
            float tx = gridX - rowWidth / 2f;
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = CreateButton(panel.transform, $"Tab{i}Button", CarCatalog.TabNames[i], Vector2.zero, new Vector2(tabWidths[i], 20), ButtonIdle, 9, UiFonts.ExtraBold);
                tab.GetComponentInChildren<TMP_Text>().characterSpacing = 6;
                var rt = (RectTransform)tab.transform;
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(tx, 104);
                tx += tabWidths[i] + 6;
                tabs[i] = tab;
            }
            var pageText = Tmp(panel.transform, "PageText", "", 10, UiFonts.Mono, TextMuted, TextAlignmentOptions.Right);
            SetRect(pageText.rectTransform, new Vector2(gridX + rowWidth / 2f - 30, 104), new Vector2(60, 16));

            // Sayfa yuvaları (her sayfada aynı 5 yuva, içerik denetleyiciden gelir)
            var modelRow = CenteredRow(panel.transform, "ModelRow", new Vector2(gridX, 64), new Vector2(432, 50), 8);
            var slotButtons = new Object[slotsPerPage];
            var slotFrames = new Object[slotsPerPage];
            var slotImages = new Object[slotsPerPage];
            var slotPlates = new Object[slotsPerPage];
            var slotPlateTexts = new Object[slotsPerPage];
            for (int i = 0; i < slotsPerPage; i++)
            {
                GarageSlot(modelRow, $"Slot{i}", null, out var frame, out var car, out var plate, out var plateText, PlateDark, PlateText, "");
                frame.rectTransform.sizeDelta = new Vector2(80, 50);
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                slotButtons[i] = button;
                slotFrames[i] = frame;
                slotImages[i] = car;
                slotPlates[i] = plate;
                slotPlateTexts[i] = plateText;
            }
            var prev = CreateIconButton(panel.transform, "PrevPageButton", IconArrow, Vector2.zero);
            var prevRect = (RectTransform)prev.transform;
            prevRect.anchorMin = prevRect.anchorMax = prevRect.pivot = new Vector2(0.5f, 0.5f);
            SetRect(prevRect, new Vector2(gridX - 232, 64), new Vector2(18, 50));
            prev.transform.Find("Icon").localRotation = Quaternion.Euler(0, 0, 180);
            var next = CreateIconButton(panel.transform, "NextPageButton", IconArrow, Vector2.zero);
            var nextRect = (RectTransform)next.transform;
            nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = new Vector2(0.5f, 0.5f);
            SetRect(nextRect, new Vector2(gridX + 232, 64), new Vector2(18, 50));

            // RENK yuvarlakları (gizli renk kilitliyken pasif — satır boşluk bırakmadan dizilir)
            SectionLabel(panel.transform, "PaintLabel", "PAINT", new Vector2(gridX, 26), rowWidth);
            var paintRow = CenteredRow(panel.transform, "PaintRow", new Vector2(gridX, -2), new Vector2(rowWidth, 32), 12);
            paintRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            paintRow.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 0, 0, 0); // halka payı
            var paintButtons = new Object[catalog.paints.Length];
            var paintRings = new Object[catalog.paints.Length];
            for (int i = 0; i < catalog.paints.Length; i++)
            {
                var paint = catalog.paints[i];
                var dot = Icon(paintRow, $"Paint_{paint.id}", LoadUiSprite("dot"), 24, paint.swatch);
                dot.raycastTarget = true;
                var button = dot.gameObject.AddComponent<Button>();
                button.targetGraphic = dot;
                var colors = button.colors; // boya rengi birebir görünsün (ortak düğme ayarı 1,2 kat aydınlatıyor)
                colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
                colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
                colors.selectedColor = Color.white;
                colors.colorMultiplier = 1f;
                button.colors = colors;
                var ring = Icon(dot.transform, "Ring", LoadUiSprite("dot_ring"), 32, PlateText);
                ring.enabled = false;
                dot.gameObject.SetActive(!paint.secret);
                paintButtons[i] = button;
                paintRings[i] = ring;
            }
            var paintNote = Tmp(panel.transform, "PaintNote", "This one keeps its own colours.", 11, UiFonts.SemiBold, TextMuted, TextAlignmentOptions.Left);
            SetRect(paintNote.rectTransform, new Vector2(gridX, -2), new Vector2(rowWidth, 20));

            // Dünya teması
            SectionLabel(panel.transform, "ThemeLabel", "WORLD", new Vector2(gridX, -46), rowWidth);
            var themes = new Object[WorldTheme.DisplayNames.Length];
            for (int i = 0; i < themes.Length; i++)
            {
                float x = gridX - rowWidth / 2f + 55f + i * 118f;
                themes[i] = CreateButton(panel.transform, $"Theme{i}Button", WorldTheme.DisplayNames[i], new Vector2(x, -76), new Vector2(110, 26), ButtonIdle, 12.5f, UiFonts.SemiBold);
            }

            // Hız göstergesi skinleri: mini kadran + ad (kilitliyken km ya da TRIP).
            SectionLabel(panel.transform, "GaugeLabel", "GAUGE", new Vector2(gridX, -106), rowWidth);
            var gaugeSkins = new System.Collections.Generic.List<GaugeSkins.Skin>(GaugeSkins.Selectable());
            var gaugeButtons = new Object[gaugeSkins.Count];
            var gaugeIcons = new Object[gaugeSkins.Count];
            var gaugeLabels = new Object[gaugeSkins.Count];
            for (int i = 0; i < gaugeSkins.Count; i++)
            {
                float x = gridX - rowWidth / 2f + 44f + i * 95.5f;
                var button = CreateButton(panel.transform, $"Gauge_{gaugeSkins[i].id}", gaugeSkins[i].name, new Vector2(x, -134), new Vector2(88, 28), ButtonIdle, 10.5f, UiFonts.SemiBold);
                var label = button.GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.Left;
                label.rectTransform.offsetMin = new Vector2(30, -1);
                var icon = Icon(button.transform, "Icon", null, 22, Color.white);
                icon.preserveAspect = true;
                Anchor(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(0, 0.5f));
                gaugeButtons[i] = button;
                gaugeIcons[i] = icon;
                gaugeLabels[i] = label;
            }

            panel.SetActive(false);
            var controller = new GameObject("GarageController").AddComponent<GarageController>();
            Bind(controller, "catalog", catalog);
            Bind(controller, "garagePanel", panel);
            Bind(controller, "previewImage", previewImage);
            Bind(controller, "selectedNameText", nameText);
            Bind(controller, "detailText", detail);
            BindArray(controller, "tabButtons", tabs);
            BindArray(controller, "slotButtons", slotButtons);
            BindArray(controller, "slotFrames", slotFrames);
            BindArray(controller, "slotImages", slotImages);
            BindArray(controller, "slotPlates", slotPlates);
            BindArray(controller, "slotPlateTexts", slotPlateTexts);
            Bind(controller, "prevButton", prev);
            Bind(controller, "nextButton", next);
            Bind(controller, "pageText", pageText);
            Bind(controller, "paintRow", paintRow.gameObject);
            Bind(controller, "paintNote", paintNote);
            BindArray(controller, "paintButtons", paintButtons);
            BindArray(controller, "paintRings", paintRings);
            BindArray(controller, "themeButtons", themes);
            BindArray(controller, "gaugeButtons", gaugeButtons);
            BindArray(controller, "gaugeIcons", gaugeIcons);
            BindArray(controller, "gaugeLabels", gaugeLabels);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            return controller;
        }

        // Garaj bölüm başlığı: satırın sol kenarına hizalı küçük aralıklı büyük harf.
        static void SectionLabel(Transform parent, string name, string text, Vector2 center, float width)
        {
            var label = Tmp(parent, name, text, 9, UiFonts.ExtraBold, TextMuted, TextAlignmentOptions.Left);
            label.characterSpacing = 16;
            SetRect(label.rectTransform, center, new Vector2(width, 14));
        }

        // Çocuklarını ortalayan sabit boyutlu yatay satır (pasif çocuklar boşluk bırakmaz).
        static Transform CenteredRow(Transform parent, string name, Vector2 center, Vector2 size, float spacing)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            SetRect(row.GetComponent<RectTransform>(), center, size);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row.transform;
        }

        // Model yuvası: çerçeve + araç görseli + sağ altta küçük plaka (kilit km'si ya da "SOON").
        static Image GarageSlot(Transform row, string name, Sprite sprite, out Image frame, out Image car,
            out Image plate, out TMP_Text plateText, Color plateColor, Color plateInk, string plateLabel)
        {
            frame = Plate(row, name, new Color(1f, 1f, 1f, 0.07f), RoundMediumSprite);
            frame.raycastTarget = true;
            frame.rectTransform.sizeDelta = new Vector2(86, 52);

            var carGo = new GameObject("Car", typeof(RectTransform), typeof(Image));
            carGo.transform.SetParent(frame.transform, false);
            car = carGo.GetComponent<Image>();
            car.sprite = sprite;
            car.preserveAspect = true;
            car.raycastTarget = false;
            Stretch(car.rectTransform);
            car.rectTransform.offsetMin = new Vector2(7, 6);
            car.rectTransform.offsetMax = new Vector2(-7, -6);

            plate = Plate(frame.transform, "Plate", plateColor, RoundSprite);
            Anchor(plate.rectTransform, new Vector2(1, 0), new Vector2(-3, 3), new Vector2(1, 0));
            plate.rectTransform.sizeDelta = new Vector2(44, 14);
            plateText = Tmp(plate.transform, "Text", plateLabel, 7.5f, UiFonts.ExtraBold, plateInk);
            plateText.characterSpacing = 6;
            Stretch(plateText.rectTransform);
            plateText.rectTransform.offsetMin = new Vector2(0, -1);
            if (plateLabel == "") plate.gameObject.SetActive(false);
            return frame;
        }

        static SettingsController CreateSettingsPanel(Transform parent, Button openButton, CompanionWindowController companion)
        {
            var panel = CreatePanel(parent, "SettingsPanel", "Settings", out var close, out _);

            var volumeLabel = Tmp(panel.transform, "VolumeLabel", "Volume", 13, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            SetRect(volumeLabel.rectTransform, new Vector2(-290, 110), new Vector2(140, 26));
            var slider = CreateSlider(panel.transform, "VolumeSlider", new Vector2(-20, 110), new Vector2(360, 22));
            var volumeValue = Tmp(panel.transform, "VolumeValue", "", 12, UiFonts.Mono, PlateText);
            SetRect(volumeValue.rectTransform, new Vector2(200, 110), new Vector2(70, 26));

            var sizeLabel = Tmp(panel.transform, "CardSizeLabel", "Card size", 13, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            SetRect(sizeLabel.rectTransform, new Vector2(-290, 80), new Vector2(140, 26));
            var sizeButtons = new Object[CompanionWindowController.CardSizeNames.Length];
            for (int i = 0; i < sizeButtons.Length; i++)
            {
                sizeButtons[i] = CreateButton(panel.transform, $"CardSize{i}Button", CompanionWindowController.CardSizeNames[i],
                    new Vector2(-150 + i * 108, 80), new Vector2(100, 26), ButtonIdle, 12.5f, UiFonts.SemiBold);
            }

            // Görev çubuğundaki hız göstergesi: sade plaka / kartın aynısı / taşan kadran.
            var stripLabel = Tmp(panel.transform, "StripGaugeLabel", "Taskbar gauge", 13, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            SetRect(stripLabel.rectTransform, new Vector2(-290, 50), new Vector2(140, 26));
            var stripButtons = new Object[GaugeSkins.StripStyleNames.Length];
            for (int i = 0; i < stripButtons.Length; i++)
            {
                stripButtons[i] = CreateButton(panel.transform, $"StripGauge{i}Button", GaugeSkins.StripStyleNames[i],
                    new Vector2(-150 + i * 108, 50), new Vector2(100, 26), ButtonIdle, 12.5f, UiFonts.SemiBold);
            }

            var onTop = CreateToggle(panel.transform, "CardOnTopToggle", "Keep the card on top of other windows", new Vector2(-40, 20), new Vector2(640, 28));
            var toggle = CreateToggle(panel.transform, "StartInCompanionToggle", "Start in taskbar mode", new Vector2(-40, -10), new Vector2(640, 28));
            var trayToggle = CreateToggle(panel.transform, "CloseToTrayToggle", "Keep running in the tray when closed", new Vector2(-40, -40), new Vector2(640, 28));
            var startupToggle = CreateToggle(panel.transform, "RunAtStartupToggle", "Start with Windows", new Vector2(-40, -70), new Vector2(640, 28));
            var vehicleGauge = CreateToggle(panel.transform, "VehicleGaugeToggle", "Boats and aircraft use their own gauge", new Vector2(-40, -100), new Vector2(640, 28));
            var note = Tmp(panel.transform, "Note",
                "Taskbar mode: a small transparent strip on your taskbar (F9). Drag the card by any empty spot. In the tray, your scrolls keep counting; reopen it from the icon next to the clock.",
                10.5f, UiFonts.SemiBold, TextMuted);
            note.textWrappingMode = TextWrappingModes.Normal;
            SetRect(note.rectTransform, new Vector2(0, -134), new Vector2(720, 30));

            panel.SetActive(false); // Editor'de sahne temiz görünsün (runtime'da da kapalı başlar)
            var controller = new GameObject("SettingsController").AddComponent<SettingsController>();
            Bind(controller, "settingsPanel", panel);
            Bind(controller, "volumeSlider", slider);
            Bind(controller, "volumeValueText", volumeValue);
            Bind(controller, "companionWindow", companion);
            BindArray(controller, "cardSizeButtons", sizeButtons);
            Bind(controller, "cardOnTopToggle", onTop);
            Bind(controller, "startInCompanionToggle", toggle);
            Bind(controller, "closeToTrayToggle", trayToggle);
            Bind(controller, "runAtStartupToggle", startupToggle);
            BindArray(controller, "stripGaugeButtons", stripButtons);
            Bind(controller, "vehicleGaugeToggle", vehicleGauge);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            return controller;
        }

        static StatsController CreateStatsPanel(Transform parent, Button openButton, RoadDrive drive, StatsTracker stats)
        {
            var panel = CreatePanel(parent, "StatsPanel", "Stats", out var close, out _);
            var text = Tmp(panel.transform, "StatsText", "", 13, UiFonts.SemiBold, PlateText, TextAlignmentOptions.TopLeft);
            text.lineSpacing = 6;
            Anchor(text.rectTransform, new Vector2(0, 1), new Vector2(26, -58), new Vector2(0, 1));
            text.rectTransform.sizeDelta = new Vector2(748, 270);

            panel.SetActive(false);
            var controller = new GameObject("StatsController").AddComponent<StatsController>();
            Bind(controller, "drive", drive);
            Bind(controller, "stats", stats);
            Bind(controller, "statsPanel", panel);
            Bind(controller, "statsText", text);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            return controller;
        }

        // Görev çubuğu şeridi arayüzü: sabit piksel ölçekli (küçük pencerede okunur kalsın diye).
        static void CreateCompanionHud(RoadDrive drive, SecretZoneView secret, CompanionWindowController companion, NoticeController notice)
        {
            var canvasGo = new GameObject("CompanionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var root = new GameObject("CompanionRoot", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            // Şerit 48 px yüksek (%100 ölçek); CompanionHudController görev çubuğu ölçeğini uygular.
            var km = CreateText(root.transform, "Km", "", 17, Vector2.zero, new Vector2(170, 22), TextLight);
            km.fontStyle = FontStyle.Bold;
            km.alignment = TextAnchor.MiddleLeft;
            Anchor(km.rectTransform, new Vector2(0, 0.5f), new Vector2(10, 8), new Vector2(0, 0.5f));
            var outline = km.gameObject.AddComponent<Outline>(); // açık/koyu görev çubuğunda okunur kalsın
            outline.effectColor = new Color(0.08f, 0.09f, 0.12f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var sub = CreateText(root.transform, "Sub", "", 11, Vector2.zero, new Vector2(200, 16), TextLight);
            sub.alignment = TextAnchor.MiddleLeft;
            Anchor(sub.rectTransform, new Vector2(0, 0.5f), new Vector2(10, -10), new Vector2(0, 0.5f));
            var subOutline = sub.gameObject.AddComponent<Outline>();
            subOutline.effectColor = outline.effectColor;
            subOutline.effectDistance = new Vector2(1f, -1f);

            // Sade plaka: skinin plakası (206x42 tasarım; doku 512x128, dikeyde ortalı) + mini kadran
            // + sayaç + hız + birim; duyuru/fısıltı sayacın yerine geçer.
            const float plateTexH = 206f * 128f / 512f, plateTop = (plateTexH - 42f) / 2f;
            var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plateGo.transform.SetParent(root.transform, false);
            var plateImage = plateGo.GetComponent<Image>();
            plateImage.raycastTarget = false;
            var plateRect = (RectTransform)plateGo.transform;
            plateRect.sizeDelta = new Vector2(206f, plateTexH);
            Anchor(plateRect, new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(0, 0.5f));
            var miniGauge = CreateGauge(plateGo.transform, "MiniGauge", 40f, true, drive);
            Anchor((RectTransform)miniGauge.transform, new Vector2(0, 1), new Vector2(4, -(plateTop + 1f)), new Vector2(0, 1));
            var plateOdo = CreateOdometer(plateGo.transform, "Odometer", 12f, out var plateCells);
            Stretch((RectTransform)plateOdo.transform);
            for (int i = 0; i < plateCells.Length; i++)
            {
                var cell = plateCells[i];
                cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0, 1);
                cell.sizeDelta = new Vector2(10.5f, 17f);
                cell.anchoredPosition = new Vector2(53f + i * (10.5f + 1.26f), -(plateTop + 12.5f));
            }
            float speedX = 53f + 6 * 10.5f + 5 * 1.26f + 11f;
            var plateSpeed = Tmp(plateGo.transform, "Speed", "0", 18, UiFonts.ExtraBold, PlateText, TextAlignmentOptions.Left);
            Anchor(plateSpeed.rectTransform, new Vector2(0, 1), new Vector2(speedX, -(plateTop + 7f)), new Vector2(0, 1));
            plateSpeed.rectTransform.sizeDelta = new Vector2(206f - speedX - 4f, 20f);
            var plateUnit = Tmp(plateGo.transform, "Unit", "KM/H", 8, UiFonts.Mono, PlateText, TextAlignmentOptions.Left);
            plateUnit.characterSpacing = 8;
            Anchor(plateUnit.rectTransform, new Vector2(0, 1), new Vector2(speedX, -(plateTop + 27f)), new Vector2(0, 1));
            plateUnit.rectTransform.sizeDelta = new Vector2(206f - speedX - 4f, 10f);
            var plateNotice = Tmp(plateGo.transform, "Notice", "", 11, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            plateNotice.textWrappingMode = TextWrappingModes.Normal;
            plateNotice.enableAutoSizing = true;
            plateNotice.fontSizeMin = 7.5f;
            plateNotice.fontSizeMax = 12f;
            Anchor(plateNotice.rectTransform, new Vector2(0, 1), new Vector2(50f, -(plateTop + 4f)), new Vector2(0, 1));
            plateNotice.rectTransform.sizeDelta = new Vector2(150f, 34f);
            plateNotice.gameObject.SetActive(false);

            // Kartın aynısı / taşan kadran: tam kadran (boyutu çalışma zamanında), sol-altta.
            var dialGauge = CreateGauge(root.transform, "DialGauge", 44f, false, drive);
            Anchor((RectTransform)dialGauge.transform, new Vector2(0, 0), new Vector2(8, 2), new Vector2(0, 0));
            dialGauge.gameObject.SetActive(false);

            var menuButton = CreateIconButton(root.transform, "MenuButton", IconMenu, Vector2.zero);
            Anchor((RectTransform)menuButton.transform, new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(1, 0.5f));
            ((RectTransform)menuButton.transform).sizeDelta = new Vector2(24, 24);

            var panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            SetSliced(panel.GetComponent<Image>(), RoundSprite);
            panel.GetComponent<Image>().color = Asphalt;
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(196, 34);
            Anchor(panelRect, new Vector2(1, 0.5f), new Vector2(-36, 0), new Vector2(1, 0.5f));
            var exitButton = CreateButton(panel.transform, "ExitCompanionButton", "Card", new Vector2(-40, 0), new Vector2(108, 26), ButtonIdle, 12.5f);
            var quitButton = CreateButton(panel.transform, "QuitButton", "Quit", new Vector2(57, 0), new Vector2(74, 26), ButtonIdle, 12.5f);

            var controller = new GameObject("CompanionHudController").AddComponent<CompanionHudController>();
            Bind(controller, "drive", drive);
            Bind(controller, "secretZone", secret);
            Bind(controller, "notice", notice);
            Bind(controller, "companionWindow", companion);
            Bind(controller, "root", root);
            Bind(controller, "scaler", canvasGo.GetComponent<CanvasScaler>());
            Bind(controller, "kmText", km);
            Bind(controller, "subText", sub);
            Bind(controller, "menuButton", menuButton);
            Bind(controller, "menuPanel", panel);
            Bind(controller, "exitCompanionButton", exitButton);
            Bind(controller, "quitButton", quitButton);
            Bind(controller, "plateRoot", plateGo);
            Bind(controller, "plateImage", plateImage);
            Bind(controller, "miniGauge", miniGauge);
            Bind(controller, "plateOdometer", plateOdo);
            Bind(controller, "plateSpeed", plateSpeed);
            Bind(controller, "plateUnit", plateUnit);
            Bind(controller, "plateNotice", plateNotice);
            Bind(controller, "dialGauge", dialGauge);
        }

        // Hız göstergesi (GaugeView): yüz, Dijital segmentleri (radial fill), tam boyda kadran içi sayaç
        // + dijital hız + IDLE, en üstte ibre. Görseller çalışma zamanında skinle atanır (GaugeSkins).
        static GaugeView CreateGauge(Transform parent, string name, float size, bool mini, RoadDrive drive)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);

            var face = GaugeImage(go.transform, "Face");
            var lit = GaugeImage(go.transform, "Lit");
            lit.type = Image.Type.Filled;
            lit.fillMethod = Image.FillMethod.Radial360;
            lit.fillOrigin = (int)Image.Origin360.Bottom;
            lit.fillClockwise = true;

            OdometerView odometer = null;
            TMP_Text digital = null, idle = null;
            if (!mini)
            {
                odometer = CreateOdometer(go.transform, "Odometer", size * 13f / 200f, out _);
                Stretch((RectTransform)odometer.transform);
                digital = Tmp(go.transform, "Digital", "0", size * 38f / 200f, UiFonts.Mono, PlateText);
                var dr = digital.rectTransform;
                dr.anchorMin = new Vector2(0.2f, 1f - 100f / 200f);
                dr.anchorMax = new Vector2(0.8f, 1f - 62f / 200f);
                dr.offsetMin = dr.offsetMax = Vector2.zero;
                idle = Tmp(go.transform, "Idle", "IDLE", size * 9f / 200f, UiFonts.ExtraBold, Amber);
                idle.characterSpacing = 10;
            }
            var needle = GaugeImage(go.transform, "Needle");

            var view = go.AddComponent<GaugeView>();
            Bind(view, "drive", drive);
            Bind(view, "catalog", LoadCarCatalog());
            var so = new SerializedObject(view);
            so.FindProperty("mini").boolValue = mini;
            so.ApplyModifiedPropertiesWithoutUndo();
            Bind(view, "face", face);
            Bind(view, "needle", needle);
            Bind(view, "lit", lit);
            if (!mini)
            {
                Bind(view, "bigNumber", digital);
                Bind(view, "idleLabel", idle);
                Bind(view, "odometer", odometer);
            }
            return view;
        }

        static Image GaugeImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            Stretch(image.rectTransform);
            return image;
        }

        // Yuvarlanan sayaç: 6 maskeli hücre, her birinde şimdiki + bir sonraki rakam (OdometerView).
        // Hücre arka planları skin görselinde; konumları çağıran yerleştirir (ya da PlaceInDial).
        static OdometerView CreateOdometer(Transform parent, string name, float fontSize, out RectTransform[] cells)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            cells = new RectTransform[6];
            var current = new Object[6];
            var next = new Object[6];
            for (int i = 0; i < 6; i++)
            {
                var cell = new GameObject($"Cell{i}", typeof(RectTransform), typeof(RectMask2D));
                cell.transform.SetParent(go.transform, false);
                cells[i] = (RectTransform)cell.transform;
                var cur = Tmp(cell.transform, "Text", "0", fontSize, UiFonts.Mono, PlateText);
                Stretch(cur.rectTransform);
                var nxt = Tmp(cell.transform, "Next", "1", fontSize, UiFonts.Mono, PlateText);
                Stretch(nxt.rectTransform);
                current[i] = cur;
                next[i] = nxt;
            }
            var view = go.AddComponent<OdometerView>();
            BindArray(view, "cells", cells);
            BindArray(view, "current", current);
            BindArray(view, "next", next);
            return view;
        }

        // ------------------------------------------------------------------ tabela stili yardımcıları

        // UI sprite'ları 2x çözünürlükte (ArtImportSettings "_bN" ile 9 parçalı yapar); Image'larda
        // pixelsPerUnitMultiplier = 2 ile kullanılır, köşeler büyük kart boyutunda da keskin kalır.
        static Sprite RoundSprite => LoadUiSprite("slice_round_b10");
        static Sprite RoundMediumSprite => LoadUiSprite("slice_roundm_b16");
        static Sprite RingSprite => LoadUiSprite("slice_ring_b14");
        static Sprite IconMenu => LoadUiSprite("icon_menu");
        static Sprite IconMinimize => LoadUiSprite("icon_min");
        static Sprite IconClose => LoadUiSprite("icon_close");
        static Sprite IconArrow => LoadUiSprite("icon_arrow");

        static Sprite LoadUiSprite(string name)
        {
            string path = $"Assets/Art/UI/{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null ? sprite : LoadArtSprite(path);
        }

        static void SetSliced(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
        }

        static TMP_Text Tmp(Transform parent, string name, string content, float fontSize, TMP_FontAsset font, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Image Plate(Transform parent, string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            SetSliced(image, sprite);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Image Icon(Transform parent, string name, Sprite sprite, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        // İçeriğe göre büyüyen yatay satır.
        static GameObject Row(Transform parent, string name, float spacing, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go;
        }

        // Koyu plaka üstünde içeriğe göre büyüyen satır (alt bilgi plakaları).
        static GameObject PlateRow(Transform parent, string name)
        {
            var plate = Plate(parent, name, PlateDark, RoundSprite).gameObject;
            var layout = plate.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(9, 9, 5, 4);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = plate.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return plate;
        }

        // Yeşil karayolu tabelası: yeşil zemin + içte ince beyaz çerçeve; içeriğe göre büyür.
        // inner'a eklenen öğeler alt alta dizilir.
        static GameObject CreateSign(Transform parent, string name, out Transform inner, out Image outer)
        {
            outer = Plate(parent, name, SignGreen, RoundMediumSprite);
            var outerLayout = outer.gameObject.AddComponent<VerticalLayoutGroup>();
            outerLayout.padding = new RectOffset(3, 3, 3, 3);
            outerLayout.childControlWidth = outerLayout.childControlHeight = true;
            outerLayout.childForceExpandWidth = outerLayout.childForceExpandHeight = false;
            var fitter = outer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var ring = Plate(outer.transform, "Ring", Color.white, RingSprite);
            var layout = ring.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(11, 11, 5, 4);
            layout.spacing = -1;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            inner = ring.transform;
            return outer.gameObject;
        }

        // Kartın sağ üstündeki koyu plaka ikon düğmeleri (≡, –, ×) ve panellerin × kapatması.
        static Button CreateIconButton(Transform parent, string name, Sprite icon, Vector2 topRight)
        {
            var plate = Plate(parent, name, PlateDark, RoundSprite);
            plate.raycastTarget = true;
            Anchor(plate.rectTransform, new Vector2(1, 1), topRight, new Vector2(1, 1));
            plate.rectTransform.sizeDelta = new Vector2(27, 24);
            var button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            SetButtonColors(button);
            Icon(plate.transform, "Icon", icon, 13, PlateText);
            return button;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color,
            float fontSize, TMP_FontAsset font = null)
        {
            var plate = Plate(parent, name, color, RoundSprite);
            plate.raycastTarget = true;
            SetRect(plate.rectTransform, pos, size);
            var button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            SetButtonColors(button);
            var text = Tmp(plate.transform, "Text", label, fontSize, font != null ? font : UiFonts.SemiBold, PlateText);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(0, -1);
            return button;
        }

        // Arka plansız, yalnız yazıdan oluşan düğme (çekmecedeki QUIT). Sol-alt köşeye sabitlenir.
        static Button CreateTextButton(Transform parent, string name, string label, float fontSize, Color color, Vector2 bottomLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            Anchor(rect, Vector2.zero, bottomLeft, Vector2.zero);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var text = Tmp(go.transform, "Text", label, fontSize, UiFonts.ExtraBold, color, TextAlignmentOptions.BottomLeft);
            text.characterSpacing = 12;
            Stretch(text.rectTransform);
            var button = go.GetComponent<Button>();
            button.targetGraphic = text;
            return button;
        }

        static void SetButtonColors(Button button)
        {
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1.2f;
            button.colors = colors;
        }

        static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), pos, size);

            var bg = Plate(go.transform, "Background", ButtonIdle, RoundSprite);
            bg.rectTransform.anchorMin = new Vector2(0, 0.35f);
            bg.rectTransform.anchorMax = new Vector2(1, 0.65f);
            bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0, 0.35f);
            faRect.anchorMax = new Vector2(1, 0.65f);
            faRect.offsetMin = new Vector2(4, 0);
            faRect.offsetMax = new Vector2(-4, 0);
            var fill = Plate(fillArea.transform, "Fill", Amber, RoundSprite);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.offsetMin = new Vector2(6, 0);
            haRect.offsetMax = new Vector2(-6, 0);
            var handle = Plate(handleArea.transform, "Handle", PlateText, RoundSprite);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(12, 0);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), pos, size);

            var box = Plate(go.transform, "Background", ButtonIdle, RoundSprite);
            box.raycastTarget = true;
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0, 0.5f);
            box.rectTransform.pivot = new Vector2(0, 0.5f);
            box.rectTransform.anchoredPosition = Vector2.zero;
            box.rectTransform.sizeDelta = new Vector2(20, 20);

            var check = Plate(box.transform, "Checkmark", Amber, RoundSprite);
            check.rectTransform.anchorMin = Vector2.zero;
            check.rectTransform.anchorMax = Vector2.one;
            check.rectTransform.offsetMin = new Vector2(4, 4);
            check.rectTransform.offsetMax = new Vector2(-4, -4);

            var text = Tmp(go.transform, "Label", label, 13, UiFonts.SemiBold, PlateText, TextAlignmentOptions.Left);
            var textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0, 0.5f);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.anchoredPosition = new Vector2(30, -1);
            textRect.sizeDelta = new Vector2(size.x - 30, size.y);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }

        // ------------------------------------------------------------------ genel yardımcılar

        static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.allowHDR = false;   // saydam pencere: alfa kanalı korunmalı
            cam.allowMSAA = false;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            return cam;
        }

        static GameObject CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CardReference;
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        // Eski uGUI Text: yalnız görev çubuğu şeridinde (o arayüz ayrı, sabit piksel ölçekli).
        static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = GetFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(go.GetComponent<RectTransform>(), pos, size);
            return text;
        }

        static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Bind(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BindArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{field}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite LoadArtSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError($"Sprite yüklenemedi: {path}");
            return sprite;
        }

        static AudioClip LoadClip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{name}.ogg");
            if (clip == null) Debug.LogError($"Ses bulunamadı: {name}");
            return clip;
        }

        static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
