using System;
using UnityEngine;

namespace ScrollRoad
{
    // Oyun dünyasının görsel katmanı — minimalist, düz renkler:
    //   gökyüzü (kamera arka planı) → uzak tepeler → şehir silueti → yakın tepeler + zemin → yol → araba.
    // Parallax: yol çizgileri hızlı, yakın tepeler yavaş, uzak tepeler çok yavaş kayar; şehir ise
    // kaydırmayla değil İLERLEMEYLE büyür (yaklaştıkça). Şehrin ötesinde şehir sola akıp kaybolur.
    // Yol düz (2026-10-10, kullanıcı: kavis "hayal ettiğim gibi olmadı").
    // Manzara RouteMap bölgelerine göre (gökyüzü, tepe rengi ve yüksekliği; sınırdan önce yumuşak
    // geçiş); her durak yol kenarında bir tabelayla geçilir. Gökyüzünde yıldızlar (gece/gizli bölge),
    // bulutlar ve ara sıra kuşlar. Araç hıza tepki verir (burun açısı, zıplama, yol titreşimi, jant
    // işaretleri dönen tekerlekler; tekne yalpalar ve köpük izi bırakır) — hepsi kodla, ek doku yok.
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
        // Kart görünümü: yalnız yolun altından şehir siluetinin tepesine kadar olan bant (kart geniş ve
        // alçak; boş gökyüzü yok). Yatayda kartın en-boy oranı kadar görünür.
        [SerializeField] float cardViewTop = 2.4f;
        [SerializeField] float cardViewBottom = -4.1f;
        [SerializeField] float roadTopY = -2.6f;
        [SerializeField] float roadHeight = 0.7f;
        [SerializeField] float carWidth = 1.7f;
        float carWidthScale = 1f;

        // Aracın gittiği zemin: şehir araçları asfaltta, OFF-ROAD sekmesi toprak patikada, küçük
        // tekneler nehirde, büyük gemiler açık denizde, hava araçları gökte.
        enum Ground { Asphalt, Dirt, River, Sea, Air }
        Ground ground = Ground.Asphalt;
        bool OnLand => ground == Ground.Asphalt || ground == Ground.Dirt;
        bool OnWater => ground == Ground.River || ground == Ground.Sea;
        [SerializeField] float airAltitude = -0.3f; // hava araçlarının kartta süzüldüğü yükseklik
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

        // Araç gövdesi hıza tepki verir (tek görselle, ek çizim yok): hızlanırken burun kalkar,
        // yavaşlarken öne eğilir (hız ile gecikmeli hızın farkı, yaylı); zıplarken hafifçe uzar;
        // kara araçları hızla artan yol titreşimi alır (rölantide neredeyse durgun).
        [Header("Araç hareketi")]
        [SerializeField] float pitchPerKmh = 0.08f;      // (hız - gecikmeli hız) başına burun açısı (derece)
        [SerializeField] float maxPitchDeg = 3f;
        [SerializeField] float motorcyclePitchScale = 1.6f;
        [SerializeField] float speedLagSeconds = 0.45f;
        [SerializeField] float pitchStiffness = 55f;
        [SerializeField] float pitchDamping = 7f;
        [SerializeField] float squashPerBobVelocity = 0.03f;
        [SerializeField] float maxVibration = 0.028f;     // dünya birimi (~1,5 px) — en yüksek hızda
        [SerializeField] float boatRockDeg = 1.6f;
        [SerializeField] float dirtVibrationScale = 1.8f;
        [SerializeField] Vector2 dirtBumpSeconds = new Vector2(1.2f, 3.5f); // patikada tümsekler arası
        [SerializeField] float airCameraLift = 1.3f;       // hava aracında kart kamerası bu kadar yükselir (yer aşağıda kalır)
        [SerializeField] float brakeHoldSeconds = 0.6f;    // yukarı scroll'dan sonra stop lambası açık kalır

        // Anlara özel basılıp esneme (2026-10-26): araç önce basılır, sonra uzar, yaylı söner; tabanı
        // yerde kalır. Ek çizim yok, aracın kendi ölçeği. Değerler yayın başlangıç hızı.
        [Header("Araç pop'u")]
        [SerializeField] float popStiffness = 200f;
        [SerializeField] float popDamping = 8f;
        [SerializeField] float unlockPop = 1.6f;          // yeni araç açıldı
        [SerializeField] float stopPop = 1.0f;            // durağa varış (bölge durağında 1,4 kat)
        [SerializeField] float boostPop = 0.8f;           // boost'a giriş
        [SerializeField] float boostPopKmh = 160f;        // bu hızı geçince...
        [SerializeField] float boostResetKmh = 90f;       // ...ve bunun altına inmeden tekrar etmez
        [SerializeField] float boostPopCooldown = 8f;

        [Header("Ortam")]
        [SerializeField] float cloudUnitsPerKm = 1.2f;
        [SerializeField] float cloudWind = 0.04f;         // birim/sn, araçtan bağımsız sürüklenme
        [SerializeField] Vector2 birdIntervalSeconds = new Vector2(40f, 110f);

        [Header("Tabela")]
        [SerializeField] float signPoleHeight = 1.25f;
        [SerializeField] float signBoardHeight = 0.5f;

        static readonly Color SecretSkyColor = new Color(0.17f, 0.11f, 0.25f);     // şehrin ötesi — belgelenmemiş
        static readonly Color CitySilhouetteColor = new Color(0.18f, 0.23f, 0.31f);
        static readonly Color RoadColor = new Color(0.22f, 0.23f, 0.27f);
        static readonly Color SecretRoadColor = new Color(0.34f, 0.26f, 0.45f); // koyu zeminde okunur kalsın
        static readonly Color DashColor = new Color(0.97f, 0.95f, 0.86f);
        static readonly Color DeepSeaColor = new Color(0.14f, 0.34f, 0.54f);
        static readonly Color WaveColor = new Color(0.78f, 0.90f, 0.96f);
        static readonly Color DustColor = new Color(0.62f, 0.60f, 0.56f);
        static readonly Color SignPoleColor = new Color(0.30f, 0.30f, 0.33f);
        static readonly Color TownBoardColor = new Color(0.97f, 0.95f, 0.88f);
        static readonly Color TownTextColor = new Color(0.16f, 0.19f, 0.24f);
        static readonly Color RegionBoardColor = new Color(0.20f, 0.38f, 0.30f); // karayolu yeşili
        static readonly Color RegionTextColor = new Color(0.97f, 0.97f, 0.94f);

        static readonly Color[] ConfettiColors =
        {
            new Color(0.95f, 0.38f, 0.30f),
            new Color(0.98f, 0.80f, 0.25f),
            new Color(0.35f, 0.70f, 0.92f),
            new Color(0.45f, 0.80f, 0.50f),
            new Color(0.98f, 0.97f, 0.93f),
        };

        static readonly Color FoamColor = new Color(0.93f, 0.97f, 0.99f);
        static readonly Color BirdColor = new Color(0.16f, 0.18f, 0.22f);

        const int DustPoolSize = 32;
        const int ConfettiPoolSize = 48;
        const int FoamPoolSize = 24;
        const int ContrailPoolSize = 56;
        const int HillTiles = 3;
        const int MaxWheels = 4;
        const int CloudCount = 5;
        const int StarCount = 28;
        const int BirdCount = 3;

        enum ParticleKind { Dust, Confetti, Foam, Contrail }

        struct Particle
        {
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public float spin;
            public float size; // tozun boy çarpanı (patikada iri)
            public Color color;
        }

        struct Bird
        {
            public Transform root;
            public Transform leftWing;
            public Transform rightWing;
            public SpriteRenderer leftRenderer;
            public SpriteRenderer rightRenderer;
            public Vector2 offset;
            public float phase;
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
        Particle[] foam;
        Particle[] contrail;
        int nextDust;
        int nextFoam;
        int nextContrail;
        float contrailTimer;
        bool hasContrail;
        float airLift;        // kart kamerasının o anki yükselmesi (hava aracına geçişte yumuşak)
        SpriteRenderer[] airClouds = Array.Empty<SpriteRenderer>();

        bool facingReverse;
        float visualFacing = 1f; // +1 sağa (ileri), -1 sola (geri); U-dönüşünde yumuşakça geçer
        float bobOffset;
        float bobVelocity;
        float dustTimer;
        float foamTimer;
        float secretBlend;

        CarCatalog.Kind carKind = CarCatalog.Kind.Car;
        float laggedSpeed;
        float pitch;          // derece, + = burun yukarı (aracın baktığı yöne göre)
        float pitchVelocity;
        float vibrationTime;

        SpriteRenderer[] wheelRenderers;
        VehicleArt.Wheel[] wheels = Array.Empty<VehicleArt.Wheel>();
        float[] wheelAngles = new float[MaxWheels];

        // Dönen parçalar (VehicleArt.Props): aracın çocuğu, aracın yön/eğimini izler.
        Transform propRoot;
        VehicleArt.Prop[] props = Array.Empty<VehicleArt.Prop>();
        SpriteRenderer[] propBlades = Array.Empty<SpriteRenderer>();
        SpriteRenderer[] propBlurs = Array.Empty<SpriteRenderer>();
        float[] propAngles = Array.Empty<float>();

        SpriteRenderer brakeCore;
        SpriteRenderer brakeHalo;
        Vector2? brakeLightPoint;
        float brakeHold;
        float brakeLevel;
        float bumpTimer = 2f;

        // Zemin işaretleri (yuvarlak sprite): patikada teker izi + çakıl, denizde dalga sıraları.
        SpriteRenderer[] marks = Array.Empty<SpriteRenderer>();
        SpriteRenderer bankTop;     // nehir kıyıları
        SpriteRenderer bankBottom;
        Color surfaceColor;         // bu karenin zemin rengi (toz/köpük de buradan)

        Sprite cloudSprite;
        SpriteRenderer[] clouds;
        SpriteRenderer[] stars;
        Vector3[] starData;   // x (-1..1 ekran oranı), y, boyut
        float starVisibility;
        Bird[] birds;
        bool birdsFlying;
        float birdTimer = 15f;
        float birdX;
        float birdY;

        Transform signRoot;
        SpriteRenderer signPoleLeft;
        SpriteRenderer signPoleRight;
        SpriteRenderer signBoard;
        TextMesh signText;
        MeshRenderer signTextRenderer;
        int signStopIndex = -1;

        bool IsCompanion => companionWindow != null && companionWindow.IsCompanionMode;
        float RoadH => IsCompanion ? companionRoadHeight : roadHeight;
        float RoadCenterY => roadTopY - RoadH / 2f;
        float CarWidth => carWidth * carWidthScale;
        float CarHeight => carSprite != null ? CarWidth * carSprite.bounds.size.y / carSprite.bounds.size.x : CarWidth * 0.45f;
        // Şeritte yol ince: araba yola gömülmeden, tekerlekleri yolun üstünde durur.
        // Şerit: kara araçları yolda, tekne suya biraz batık, hava aracı yolun üstünde süzülür
        // (şeritte yer yok: orada hava aracı da yol çizgisinde durur).
        float CarCenterY
        {
            get
            {
                if (IsCompanion) return CompanionCarBaseY + (ground == Ground.Air ? Mathf.Sin(Time.time * 1.3f) * 0.02f : 0f);
                return ground switch
                {
                    Ground.River or Ground.Sea => roadTopY - RoadH * 0.45f + CarHeight * 0.5f,
                    Ground.Air => airAltitude + airLift + Mathf.Sin(Time.time * 1.3f) * 0.06f,
                    _ => RoadCenterY + CarHeight * 0.5f - 0.12f,
                };
            }
        }
        // Şeritte aracın süzülmesiz merkez yüksekliği (kamera bunu izler, süzülmeyi değil).
        float CompanionCarBaseY => roadTopY + CarHeight * 0.5f - (OnWater ? RoadH * 0.6f : 0.03f) + (ground == Ground.Air ? 0.04f : 0f);

        // Tekerleklerin yere değdiği yükseklik (araç görselinin alt kenarı, zıplamasız).
        float ContactY => IsCompanion ? roadTopY - 0.03f : RoadCenterY - 0.12f;
        float DashHeight => (IsCompanion ? 0.035f : 0.07f) * (OnWater ? 0.6f : 1f);
        // Zemin bu karede kaç birim/sn akıyor (ileri giderken sola).
        float GroundUnitsPerSecond => drive.CurrentSpeedKmh / 3600f * roadUnitsPerKm;

        // Test/doğrulama için okunabilir durum.
        public float CityHeightUnits { get; private set; }
        public Color CurrentSkyColor { get; private set; }

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = (cardViewTop - cardViewBottom) / 2f;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.transform.position = new Vector3(0f, 0f, -10f);

            // Texture2D.whiteTexture 4x4 — pixelsPerUnit=4 ile 1x1 dünya birimlik beyaz kare.
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            circleSprite = CreateCircleSprite();
            cloudSprite = CreateCloudSprite();
            ApplySelectedCar();

            Build();
            RefreshWheels();
            laggedSpeed = drive.CurrentSpeedKmh;
            CarCatalog.SelectionChanged += HandleCarSelectionChanged;

            drive.OnDirectionChanged += HandleDirectionChanged;
            drive.OnStep += HandleStep;
            drive.OnReachedCity += HandleCityReached;
            CarUnlockTracker.VehicleUnlocked += HandleVehicleUnlocked;
            route = FindAnyObjectByType<RouteProgress>();
            if (route != null) route.StopReached += HandleStopReached;
        }

        void OnDestroy()
        {
            CarCatalog.SelectionChanged -= HandleCarSelectionChanged;
            CarUnlockTracker.VehicleUnlocked -= HandleVehicleUnlocked;
            if (route != null) route.StopReached -= HandleStopReached;
            if (drive == null) return;
            drive.OnDirectionChanged -= HandleDirectionChanged;
            drive.OnStep -= HandleStep;
            drive.OnReachedCity -= HandleCityReached;
        }

        void HandleStep() => bobVelocity += stepBobImpulse;

        void HandleCarSelectionChanged()
        {
            if (!ApplySelectedCar()) return;
            carRenderer.sprite = carSprite;
            RefreshWheels();
            bobVelocity += stepBobImpulse * 1.5f; // yeni araç "yerine oturur"
        }

        // Jant işaretleri yalnız kara araçlarında (wheels.txt'te konumu olanlar); arabanın çocukları
        // oldukları için yön çevirme, eğim ve zıplamayı kendiliğinden izlerler.
        void RefreshWheels()
        {
            var model = carCatalog != null ? carCatalog.SelectedModel : null;
            bool fromCatalog = model != null && carSprite == carCatalog.SelectedSprite;
            wheels = fromCatalog && OnLand ? VehicleArt.Wheels(model.id) : Array.Empty<VehicleArt.Wheel>();
            brakeLightPoint = fromCatalog && OnLand ? VehicleArt.RearLight(model.id) : null;
            for (int i = 0; i < wheelRenderers.Length; i++)
            {
                var sr = wheelRenderers[i];
                sr.enabled = i < wheels.Length && wheels[i].mark != null;
                if (!sr.enabled) continue;
                sr.sprite = wheels[i].mark;
                sr.transform.localPosition = new Vector3(wheels[i].center.x, wheels[i].center.y, 0f);
                wheelAngles[i] = UnityEngine.Random.Range(0f, 120f); // tekerlekler aynı açıda donmuş görünmesin
            }
            RefreshProps(fromCatalog ? model.id : null);
        }

        // Pervane/rotor/fan nesneleri araç değişince yeniden kurulur (seyrek; havuz gerekmez).
        void RefreshProps(string id)
        {
            if (propRoot != null) Destroy(propRoot.gameObject);
            props = id != null ? VehicleArt.Props(id) : Array.Empty<VehicleArt.Prop>();
            propBlades = new SpriteRenderer[props.Length];
            propBlurs = new SpriteRenderer[props.Length];
            propAngles = new float[props.Length];
            if (props.Length == 0) return;

            propRoot = new GameObject("Props").transform;
            propRoot.SetParent(carRenderer.transform, false);
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                propAngles[i] = UnityEngine.Random.Range(0f, Mathf.PI);
                if (p.kind == VehicleArt.PropKind.Fan)
                {
                    // Dönen fan yalnız kendi açıklığında görünür (önündeki kabinin üstüne taşmaz).
                    var maskGo = new GameObject($"FanMask{i}", typeof(SpriteMask));
                    maskGo.transform.SetParent(propRoot, false);
                    maskGo.transform.localPosition = p.center;
                    maskGo.GetComponent<SpriteMask>().sprite = p.fanMask;
                    var fan = CreateRenderer($"Fan{i}", p.fan, 41, propRoot);
                    fan.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    fan.transform.localPosition = p.center;
                    propBlades[i] = fan;
                    continue;
                }
                // Hız bulanıklığı (yarı saydam elips) + kanat.
                propBlurs[i] = CreateRenderer($"PropBlur{i}", circleSprite, 43, propRoot);
                propBlades[i] = CreateRenderer($"PropBlade{i}", circleSprite, 44, propRoot);
                propBlurs[i].transform.localPosition = p.center;
                propBlades[i].transform.localPosition = p.center;
                propBlades[i].color = p.color;
            }
        }

        // Yandan görülen pervane/rotor dönerken kanat boyu cos(açı) ile salınır (kenardan bakış);
        // bize dönük kuyruk rotoru ve fan gerçekten döner. Hızla dönüş ve bulanıklık artar; rölantide
        // de durmazlar.
        void UpdateProps(float speed, float dt)
        {
            float blurAlpha = 0.12f + 0.22f * Mathf.Clamp01(speed / 120f);
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                var blade = propBlades[i];
                var blur = propBlurs[i];
                switch (p.kind)
                {
                    case VehicleArt.PropKind.Nose:
                    {
                        propAngles[i] += (24f + speed * 0.25f) * dt;
                        float thick = p.size * 0.11f;
                        blade.transform.localScale = new Vector3(thick, 2f * p.size * Mathf.Abs(Mathf.Cos(propAngles[i])) + thick, 1f);
                        blur.transform.localScale = new Vector3(thick * 1.6f, 2f * p.size, 1f);
                        blur.color = new Color(p.color.r, p.color.g, p.color.b, blurAlpha);
                        break;
                    }
                    case VehicleArt.PropKind.Rotor:
                    {
                        propAngles[i] += (16f + speed * 0.12f) * dt;
                        float thick = p.size * 0.035f + 0.03f;
                        blade.transform.localScale = new Vector3(2f * p.size * Mathf.Abs(Mathf.Cos(propAngles[i])) + thick, thick, 1f);
                        blur.transform.localScale = new Vector3(2f * p.size, thick * 1.8f, 1f);
                        blur.color = new Color(p.color.r, p.color.g, p.color.b, blurAlpha);
                        break;
                    }
                    case VehicleArt.PropKind.Tail:
                    {
                        propAngles[i] += (30f + speed * 0.2f) * dt;
                        float thick = p.size * 0.25f;
                        blade.transform.localScale = new Vector3(thick, 2f * p.size, 1f);
                        blade.transform.localRotation = Quaternion.Euler(0f, 0f, propAngles[i] * Mathf.Rad2Deg);
                        blur.transform.localScale = new Vector3(2f * p.size, 2f * p.size, 1f);
                        blur.color = new Color(p.color.r, p.color.g, p.color.b, blurAlpha * 0.8f);
                        break;
                    }
                    default: // Fan
                        propAngles[i] -= (4f + speed * 0.04f) * dt;
                        blade.transform.localRotation = Quaternion.Euler(0f, 0f, propAngles[i] * Mathf.Rad2Deg);
                        break;
                }
            }
        }

        // Garajdaki model + renk; model genişlik çarpanı tekerleri aynı boyda tutar.
        bool ApplySelectedCar()
        {
            if (carCatalog == null) return false;
            var sprite = carCatalog.SelectedSprite;
            if (sprite == null) return false;
            carSprite = sprite;
            var model = carCatalog.SelectedModel;
            carWidthScale = model != null && model.widthScale > 0f ? model.widthScale : 1f;
            carKind = model != null ? model.kind : CarCatalog.Kind.Car;
            hasContrail = model != null && model.contrail;
            ground = (model != null ? model.Lane : CarCatalog.Lane.Road) switch
            {
                CarCatalog.Lane.Air => Ground.Air,
                CarCatalog.Lane.Water => model.openSea ? Ground.Sea : Ground.River,
                _ => CarCatalog.TabOf(carKind) == CarCatalog.Tab.OffRoad ? Ground.Dirt : Ground.Asphalt,
            };
            if (OnLand) facingReverse = false; // kara araçları geri viteste döner değil, geri gider
            return true;
        }

        // Kara araçları geri vitese geçer (burun ileri bakar, geri gider); tekne ve hava araçları döner.
        bool TurnsAround => !OnLand;

        void HandleDirectionChanged()
        {
            bobVelocity += stepBobImpulse * (TurnsAround ? 2f : 1f);
            if (OnLand) for (int i = 0; i < 6; i++) SpawnDust();
        }

        RouteProgress route;
        float popOffset, popVelocity;
        bool boostPopped;
        float boostPopTimer;

        void Pop(float strength) => popVelocity -= strength; // eksi = önce basılır

        void HandleVehicleUnlocked()
        {
            Pop(unlockPop);
            bobVelocity += stepBobImpulse * 3f;
        }

        void HandleStopReached(RouteMap.Stop stop)
        {
            bool region = stop.Kind == RouteMap.StopKind.Region;
            Pop(stopPop * (region ? 1.4f : 1f));
            bobVelocity += stepBobImpulse * (region ? 2.5f : 1.5f);
        }

        // Boost'a giriş: hız eşiği geçince bir kez; eşiğin altına inip bekleme bitmeden tekrar etmez.
        void UpdateBoostPop(float dt)
        {
            float speed = drive.CurrentSpeedKmh;
            boostPopTimer -= dt;
            if (!boostPopped && speed > boostPopKmh && boostPopTimer <= 0f)
            {
                boostPopped = true;
                boostPopTimer = boostPopCooldown;
                Pop(boostPop);
            }
            else if (boostPopped && speed < boostResetKmh) boostPopped = false;
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

            BuildSkyLife();
            farHills = CreateHillLayer("FarHills", 1);
            cityRenderer = CreateRenderer("City", citySprite != null ? citySprite : whiteSprite, 2, backdropRoot);
            nearHills = CreateHillLayer("NearHills", 3);
            groundFill = CreateRenderer("Ground", whiteSprite, 4, backdropRoot);
            BuildSign();

            roadRoot = new GameObject("Road").transform;
            roadRoot.SetParent(worldRoot, false);

            carRenderer = CreateRenderer("Car", carSprite != null ? carSprite : whiteSprite, 40, worldRoot);
            wheelRenderers = new SpriteRenderer[MaxWheels];
            for (int i = 0; i < MaxWheels; i++)
            {
                wheelRenderers[i] = CreateRenderer($"Wheel{i}", whiteSprite, 41, carRenderer.transform);
                wheelRenderers[i].enabled = false;
            }
            brakeHalo = CreateRenderer("BrakeHalo", circleSprite, 42, carRenderer.transform);
            brakeCore = CreateRenderer("BrakeCore", circleSprite, 43, carRenderer.transform);
            brakeHalo.enabled = brakeCore.enabled = false;

            bankTop = CreateRenderer("BankTop", whiteSprite, 21, roadRoot);
            bankBottom = CreateRenderer("BankBottom", whiteSprite, 21, roadRoot);

            dust = CreatePool("Dust", DustPoolSize, circleSprite, 35);
            foam = CreatePool("Foam", FoamPoolSize, circleSprite, 39);
            contrail = CreatePool("Contrail", ContrailPoolSize, circleSprite, 38);
            confetti = CreatePool("Confetti", ConfettiPoolSize, whiteSprite, 45);
        }

        // Gökyüzü canlılığı (arka planla birlikte şeritte gizli): yıldızlar (gece teması ve gizli
        // bölge), yavaş sürüklenen bulutlar ve ara sıra geçen küçük bir kuş sürüsü. Hepsi düz renk.
        void BuildSkyLife()
        {
            var starRoot = new GameObject("Stars").transform;
            starRoot.SetParent(backdropRoot, false);
            stars = new SpriteRenderer[StarCount];
            starData = new Vector3[StarCount];
            var rng = new System.Random(7);
            for (int i = 0; i < StarCount; i++)
            {
                stars[i] = CreateRenderer($"Star{i}", circleSprite, -2, starRoot);
                starData[i] = new Vector3((float)rng.NextDouble() * 2f - 1f, -0.3f + (float)rng.NextDouble() * 2.6f, 0.02f + (float)rng.NextDouble() * 0.03f);
            }

            var cloudRoot = new GameObject("Clouds").transform;
            cloudRoot.SetParent(backdropRoot, false);
            clouds = new SpriteRenderer[CloudCount];
            for (int i = 0; i < CloudCount; i++) clouds[i] = CreateRenderer($"Cloud{i}", cloudSprite, -1, cloudRoot);

            var birdRoot = new GameObject("Birds").transform;
            birdRoot.SetParent(backdropRoot, false);
            birds = new Bird[BirdCount];
            for (int i = 0; i < BirdCount; i++)
            {
                var root = new GameObject($"Bird{i}").transform;
                root.SetParent(birdRoot, false);
                birds[i].root = root;
                birds[i].leftWing = CreateWing(root, -1f, out birds[i].leftRenderer);
                birds[i].rightWing = CreateWing(root, 1f, out birds[i].rightRenderer);
                birds[i].offset = new Vector2(-i * 0.32f, (i % 2 == 0 ? 0.1f : -0.08f) * (i == 0 ? 0f : 1f));
                birds[i].phase = i * 1.9f;
                root.gameObject.SetActive(false);
            }
        }

        // Kanat: kökü kuşun merkezinde dönen pivot + yarı uzunlukta kaydırılmış ince dikdörtgen.
        Transform CreateWing(Transform bird, float side, out SpriteRenderer renderer)
        {
            var pivot = new GameObject(side < 0 ? "WingL" : "WingR").transform;
            pivot.SetParent(bird, false);
            renderer = CreateRenderer("Shape", whiteSprite, 5, pivot);
            const float length = 0.12f, thickness = 0.022f;
            renderer.transform.localPosition = new Vector3(side * length / 2f, 0f, 0f);
            renderer.transform.localScale = new Vector3(length, thickness, 1f);
            return pivot;
        }

        // Tabela arka planla birlikte gizlenir (görev çubuğu şeridinde yer yok).
        void BuildSign()
        {
            signRoot = new GameObject("Sign").transform;
            signRoot.SetParent(backdropRoot, false);
            signPoleLeft = CreateRenderer("PoleL", whiteSprite, 6, signRoot);
            signPoleRight = CreateRenderer("PoleR", whiteSprite, 6, signRoot);
            signBoard = CreateRenderer("Board", whiteSprite, 7, signRoot);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(signRoot, false);
            signText = textGo.AddComponent<TextMesh>();
            signText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            signText.fontSize = 64;
            signText.characterSize = 0.045f;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.alignment = TextAlignment.Center;
            signTextRenderer = textGo.GetComponent<MeshRenderer>();
            signTextRenderer.sharedMaterial = signText.font.material;
            signTextRenderer.sortingOrder = 8;
            signRoot.gameObject.SetActive(false);
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

        // Yol yalnız seçili ortamken çizilir; başka ortamda dünya gizli, kamerayı o ortamın görünümü yönetir.
        bool ShowRoad()
        {
            bool active = RoadDrive.IsActiveEnvironment;
            if (worldRoot.gameObject.activeSelf != active) worldRoot.gameObject.SetActive(active);
            return active;
        }

        void Update()
        {
            if (!ShowRoad()) return;
            // Yaylar açık Euler: takılan uzun bir karede (ör. tepsiden dönüş) patlamasınlar.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            bool paused = Time.timeScale <= 0f;

            if (drive.State != RoadDrive.DriveState.Idle) facingReverse = TurnsAround && drive.State == RoadDrive.DriveState.Reverse;
            visualFacing = Mathf.MoveTowards(visualFacing, facingReverse ? -1f : 1f, uTurnFlipPerSec * dt);

            if (!paused)
            {
                bobVelocity += (-bobOffset * bobStiffness - bobVelocity * bobDamping) * dt;
                bobOffset = Mathf.Clamp(bobOffset + bobVelocity * dt, -0.12f, 0.25f);
                popVelocity += (-popOffset * popStiffness - popVelocity * popDamping) * dt;
                popOffset = Mathf.Clamp(popOffset + popVelocity * dt, -0.15f, 0.15f);
                UpdateBoostPop(dt);

                UpdatePitch(dt);
                UpdateBrake(dt);

                // Toz hıza bağlı sürekli emisyon: seyirde seyrek, boost'ta yoğun; patikada daha bol.
                float speed = Mathf.Abs(drive.CurrentSpeedKmh);
                if (speed > 5f && OnLand)
                {
                    dustTimer += dt;
                    float interval = Mathf.Clamp((ground == Ground.Dirt ? 9f : 14f) / speed, 0.04f, 1.2f);
                    while (dustTimer >= interval)
                    {
                        dustTimer -= interval;
                        SpawnDust();
                    }
                }

                // Patika tümsekleri: hızla sıklaşmaz ama sertleşir.
                if (ground == Ground.Dirt && speed > 15f)
                {
                    bumpTimer -= dt;
                    if (bumpTimer <= 0f)
                    {
                        bumpTimer = UnityEngine.Random.Range(dirtBumpSeconds.x, dirtBumpSeconds.y);
                        bobVelocity += stepBobImpulse * Mathf.Lerp(0.35f, 1.3f, Mathf.Clamp01(speed / 160f));
                    }
                }

                // Jetin arkasında uçuş izi: sık, küçük, birbirine değen parçacıklar bir çizgi oluşturur.
                if (hasContrail && ground == Ground.Air && speed > 5f)
                {
                    contrailTimer += dt;
                    float interval = Mathf.Clamp(2.2f / speed, 0.012f, 0.08f);
                    while (contrailTimer >= interval)
                    {
                        contrailTimer -= interval;
                        SpawnContrail();
                    }
                }

                // Teknenin arkasında köpük izi: aynı mantık, suda.
                if (speed > 5f && OnWater)
                {
                    foamTimer += dt;
                    float interval = Mathf.Clamp(10f / speed, 0.06f, 0.8f);
                    while (foamTimer >= interval)
                    {
                        foamTimer -= interval;
                        SpawnFoam();
                    }
                }
            }
        }

        // Stop lambası: yukarı scroll (fren) ya da geri vites. Yalnız stop lambası konumu bulunan
        // kara araçlarında.
        void UpdateBrake(float dt)
        {
            var scroll = ScrollRoad.InputCapture.GlobalScrollSource.Instance;
            if (scroll != null && scroll.FrameNotchesForward < -0.0001f) brakeHold = brakeHoldSeconds;
            brakeHold -= dt;
            bool on = brakeLightPoint.HasValue && (brakeHold > 0f || drive.CurrentSpeedKmh < -1f);
            brakeLevel = Mathf.MoveTowards(brakeLevel, on ? 1f : 0f, dt * (on ? 14f : 5f));
        }

        // Burun açısı: anlık hız gecikmeli hızın önündeyse (hızlanma) burun kalkar, gerisindeyse
        // (seyire dönüş, fren) iner. Aracın baktığı yöne göre: geri giderken hızlanmak da burnu kaldırır.
        void UpdatePitch(float dt)
        {
            float speed = drive.CurrentSpeedKmh;
            laggedSpeed = Mathf.Lerp(laggedSpeed, speed, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, speedLagSeconds)));
            float facing = facingReverse ? -1f : 1f;
            float scale = carKind == CarCatalog.Kind.Motorcycle ? motorcyclePitchScale : 1f;
            float max = maxPitchDeg * scale;
            float target = Mathf.Clamp((speed - laggedSpeed) * facing * pitchPerKmh * scale, -max * 0.6f, max);
            pitchVelocity += ((target - pitch) * pitchStiffness - pitchVelocity * pitchDamping) * dt;
            pitch = Mathf.Clamp(pitch + pitchVelocity * dt, -max, max * 1.3f);

            // Tekerlekler yol hızında döner (yarıçapa göre); aracın yerel ekseninde hep "ileri" yönde,
            // ayna çevrilmiş araçta bu kendiliğinden ters yön olur.
            if (wheels.Length == 0) return;
            float units = speed / 3600f * roadUnitsPerKm * dt * facing;
            float carScale = CarWidth / Mathf.Max(0.001f, carRenderer.sprite.bounds.size.x);
            for (int i = 0; i < wheels.Length && i < MaxWheels; i++)
            {
                float r = Mathf.Max(0.01f, wheels[i].tireRadius * carScale);
                wheelAngles[i] = Mathf.Repeat(wheelAngles[i] - units / r * Mathf.Rad2Deg, 360f);
            }
        }

        void LateUpdate()
        {
            if (!ShowRoad()) return;
            bool companion = IsCompanion;

            // Kamera: kartta yol + tepeler + şehir bandı; şeritte yolun altından arabanın üstüne kadar,
            // araba yatayda companionCarScreenX'te (solda km yazısına yer kalsın).
            airLift = Mathf.MoveTowards(airLift, ground == Ground.Air ? airCameraLift : 0f, Time.unscaledDeltaTime * 2.5f);
            float size = (cardViewTop - cardViewBottom) / 2f, camX = 0f, camY = (cardViewTop + cardViewBottom) / 2f + airLift;
            if (companion)
            {
                float bottom = roadTopY - RoadH - 0.03f;
                float top = CompanionCarBaseY + CarHeight * 0.5f + companionTopMargin;
                // Taşan kadranda pencere yukarı uzar: şerit sahnesi pencerenin alt bandında kalır,
                // üstü saydam (arka plan alfa 0, sahnede orada bir şey yok).
                float band = 1f - (companionWindow != null ? companionWindow.StripOverflowFraction : 0f);
                size = (top - bottom) / 2f / Mathf.Max(0.2f, band);
                camY = bottom + size;
                camX = -(companionCarScreenX - 0.5f) * 2f * size * viewCamera.aspect;
            }
            viewCamera.orthographicSize = size;
            viewCamera.transform.position = new Vector3(camX, camY, -10f);

            float halfW = size * viewCamera.aspect;

            var sky = EvaluateSky();
            CurrentSkyColor = sky;
            viewCamera.backgroundColor = companion ? new Color(0f, 0f, 0f, 0f) : sky;

            surfaceColor = EvaluateSurface(sky);
            backdropRoot.gameObject.SetActive(!companion);
            if (!companion) LayoutBackdrop(sky, halfW, camY - size);

            LayoutRoad(camX, halfW);
            LayoutDashes(camX, halfW);
            LayoutMarks(camX, halfW);
            LayoutBanks(camX, halfW);
            LayoutAirClouds(camX, halfW, sky);
            LayoutCar();

            // Toz ve köpük yere bırakılır: zeminle birlikte akar (hızlıyken uzun, rölantide kısa iz).
            float groundShift = -GroundUnitsPerSecond * Time.deltaTime;
            UpdateParticles(dust, Time.deltaTime, 2.5f, 0f, ParticleKind.Dust, groundShift);
            UpdateParticles(foam, Time.deltaTime, 0f, 1.6f, ParticleKind.Foam, groundShift);
            UpdateParticles(contrail, Time.deltaTime, 0f, 0f, ParticleKind.Contrail, groundShift);
            UpdateParticles(confetti, Time.deltaTime, 3.5f, 0.6f, ParticleKind.Confetti, 0f);
        }

        // Yol/su yüzeyinin rengi: asfalt sabit, patika ve su bölgeye göre (sınırda yumuşak geçiş).
        Color EvaluateSurface(Color sky)
        {
            RouteMap.Blend(Math.Max(0.0, drive.CurrentDistanceKmPrecise), out var a, out var b, out float t);
            Color c = ground switch
            {
                Ground.Dirt => Color.Lerp(a.Trail, b.Trail, t),
                Ground.River => Color.Lerp(a.Water, b.Water, t),
                Ground.Sea => Color.Lerp(Color.Lerp(a.Water, b.Water, t), DeepSeaColor, 0.35f),
                _ => RoadColor,
            };
            // Gece temasında su ve toprak da kararır (asfalt zaten koyu).
            if (ground != Ground.Asphalt && WorldTheme.Current == WorldTheme.Kind.Night) c = Color.Lerp(c, sky, 0.55f) * 0.85f;
            c = WorldTheme.Surface(Color.Lerp(c, SecretRoadColor, secretBlend));
            c.a = 1f;
            return c;
        }

        // Renk katmanları: bölge (RouteMap, sınırdan önce yumuşak geçiş) → dünya teması (WorldTheme)
        // → gizli bölge morunun yumuşak karışımı. Son bölge "Sunset Road" şehre varışı karşılar.
        Color EvaluateSky()
        {
            double d = Math.Max(0.0, drive.CurrentDistanceKmPrecise);
            RouteMap.Blend(d, out var a, out var b, out float t);
            var color = Color.Lerp(a.Sky, b.Sky, t);

            // Gizli bölge tonu mesafeyle gelir (şehrin 3-8 km ötesi): varış anı gün batımında kalsın.
            float beyond = (float)(d - drive.CityDistanceKm);
            secretBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 8f, beyond));
            // Tema yalnız normal dünyayı boyar; gizli bölgenin moru her temada aynı (kural dışı yer).
            return Color.Lerp(WorldTheme.Sky(color), SecretSkyColor, secretBlend);
        }

        void LayoutBackdrop(Color sky, float halfW, float viewBottom)
        {
            double d = drive.CurrentDistanceKmPrecise;
            RouteMap.Blend(Math.Max(0.0, d), out var a, out var b, out float t);
            // Gizli bölgede tepeler gökyüzünden türetilir (eski davranış): mor dünya bütün kalsın.
            var farColor = Color.Lerp(WorldTheme.Land(Color.Lerp(a.FarLand, b.FarLand, t)), Shade(sky, 0.90f), secretBlend);
            var nearColor = Color.Lerp(WorldTheme.Land(Color.Lerp(a.NearLand, b.NearLand, t)), Shade(sky, 0.78f), secretBlend);
            float farHeight = 2.5f * Mathf.Lerp(a.FarHeight, b.FarHeight, t);
            float nearHeight = 1.35f * Mathf.Lerp(a.NearHeight, b.NearHeight, t);

            // Açık deniz: ufka kadar su; uzak tepeler alçak, puslu adalara döner, yakın tepeler yok.
            bool sea = ground == Ground.Sea;
            if (sea)
            {
                farHeight *= 0.55f;
                farColor = Color.Lerp(sky, farColor, 0.6f);
                farColor.a = 1f;
            }

            // Uzak tepeler: geniş, açık ton.
            LayoutHills(farHills, d * farHillsUnitsPerKm, halfW, bottomY: -2.45f, height: farHeight, width: 26f, farColor, phase: 0.37f);
            // Yakın tepeler + altında aynı renkte zemin dolgusu (yol bu zeminin üstünde).
            LayoutHills(nearHills, d * nearHillsUnitsPerKm, halfW, bottomY: -2.35f, height: nearHeight, width: 21f, nearColor, phase: 0f);
            foreach (var hill in nearHills) hill.enabled = !sea;

            groundFill.color = sea ? surfaceColor : nearColor;
            float groundTop = -2.3f;
            float groundBottom = viewBottom - 0.5f;
            groundFill.transform.localPosition = new Vector3(0f, (groundTop + groundBottom) / 2f, 0f);
            groundFill.transform.localScale = new Vector3(halfW * 2f + 2f, groundTop - groundBottom, 1f);

            LayoutCity(sky, halfW);
            LayoutSign(halfW);
            LayoutSkyLife(sky, halfW, d);
        }

        void LayoutSkyLife(Color sky, float halfW, double d)
        {
            float lum = sky.r * 0.3f + sky.g * 0.59f + sky.b * 0.11f;
            bool dark = lum < 0.35f;

            // Yıldızlar: gece temasında ya da gizli bölgede belirir, yavaşça yanıp söner.
            float starTarget = Mathf.Max(WorldTheme.Current == WorldTheme.Kind.Night ? 1f : 0f, secretBlend);
            starVisibility = Mathf.MoveTowards(starVisibility, starTarget, Time.unscaledDeltaTime * 0.5f);
            float starDrift = (float)((d * 0.15) % (halfW * 2f));
            for (int i = 0; i < stars.Length; i++)
            {
                var sr = stars[i];
                sr.enabled = starVisibility > 0.01f;
                if (!sr.enabled) continue;
                var s = starData[i];
                float x = Mathf.Repeat(s.x * halfW - starDrift + halfW, halfW * 2f) - halfW;
                sr.transform.localPosition = new Vector3(x, s.y, 0f);
                sr.transform.localScale = new Vector3(s.z, s.z, 1f);
                float twinkle = 0.55f + 0.45f * Mathf.Sin(Time.time * (0.8f + i % 5 * 0.35f) + i * 2.3f);
                sr.color = new Color(1f, 0.98f, 0.92f, starVisibility * twinkle * 0.9f);
            }

            // Bulutlar: uzak tepelerden de yavaş kayar + rüzgârla kendi kendine sürüklenir.
            var cloudColor = Color.Lerp(sky, WorldTheme.Surface(new Color(1f, 1f, 0.98f)), dark ? 0.13f : 0.55f);
            cloudColor.a = 1f; // gece gökyüzü rengi tam opak değil; bulut yıldızları örtsün
            float span = halfW * 2f + 8f;
            double drift = d * cloudUnitsPerKm + Time.time * cloudWind;
            for (int i = 0; i < clouds.Length; i++)
            {
                var sr = clouds[i];
                float width = 1.5f + (i * 0.618f % 1f) * 1.4f;
                float x = (float)((i * span / clouds.Length + i * 1.7 - drift) % span);
                if (x < 0f) x += span;
                x -= halfW + 4f;
                float y = 0.9f + (i * 0.37f % 1f) * 1.15f;
                var b = sr.sprite.bounds.size;
                sr.transform.localPosition = new Vector3(x, y, 0f);
                sr.transform.localScale = new Vector3(width / b.x, width / b.x, 1f);
                sr.color = cloudColor;
            }

            UpdateBirds(sky, halfW, dark);
        }

        // Ara sıra soldan sağa geçen 3 kuşluk sürü (gece ve gizli bölgede yok). Kanat çırpma ile
        // süzülme dönüşümlü; sahne hızlandıkça geride kalır gibi yavaşlar.
        void UpdateBirds(Color sky, float halfW, bool dark)
        {
            float dt = Time.deltaTime;
            if (!birdsFlying)
            {
                birdTimer -= dt;
                if (birdTimer > 0f || dark || secretBlend > 0.1f) return;
                birdsFlying = true;
                birdX = -halfW - 0.6f;
                birdY = UnityEngine.Random.Range(0.2f, 1.0f); // üst kenardaki HUD'un altında, açık gökte
                for (int i = 0; i < birds.Length; i++) birds[i].root.gameObject.SetActive(true);
            }

            float roadSpeed = Mathf.Abs(drive.CurrentSpeedKmh) / 3600f * roadUnitsPerKm;
            birdX += Mathf.Max(0.25f, 0.75f - roadSpeed * 0.12f) * dt;
            if (birdX > halfW + 1.2f)
            {
                birdsFlying = false;
                birdTimer = UnityEngine.Random.Range(birdIntervalSeconds.x, birdIntervalSeconds.y);
                for (int i = 0; i < birds.Length; i++) birds[i].root.gameObject.SetActive(false);
                return;
            }

            var color = Color.Lerp(sky, WorldTheme.Surface(BirdColor), 0.7f);
            color.a = 1f;
            for (int i = 0; i < birds.Length; i++)
            {
                ref var bird = ref birds[i];
                float t = Time.time + bird.phase;
                bool gliding = Mathf.Sin(t * 0.9f) > 0.45f;
                float angle = gliding ? 10f : 18f + 26f * Mathf.Sin(t * 11f);
                bird.root.localPosition = new Vector3(birdX + bird.offset.x, birdY + bird.offset.y + Mathf.Sin(t * 1.7f) * 0.04f, 0f);
                bird.leftWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
                bird.rightWing.localRotation = Quaternion.Euler(0f, 0f, angle);
                bird.leftRenderer.color = color;
                bird.rightRenderer.color = color;
            }
        }

        // En yakın durağın tabelası yol hızında kayar (seyirde ~10 sn görünür). Şehrin tabelası yok,
        // şehir kendisi görünüyor.
        void LayoutSign(float halfW)
        {
            double d = drive.CurrentDistanceKmPrecise;
            int index = -1;
            float x = 0f;
            for (int i = 0; i < RouteMap.Stops.Count; i++)
            {
                var stop = RouteMap.Stops[i];
                if (stop.Kind == RouteMap.StopKind.City || stop.Km <= 0) continue;
                float sx = (float)((stop.Km - d) * roadUnitsPerKm);
                if (sx > -halfW - 4f && sx < halfW + 4f) { index = i; x = sx; break; }
            }

            // Açık denizde yol kenarı yok: tabela yalnız HUD'da.
            signRoot.gameObject.SetActive(index >= 0 && ground != Ground.Sea);
            if (index < 0 || ground == Ground.Sea) return;

            var s = RouteMap.Stops[index];
            bool region = s.Kind == RouteMap.StopKind.Region;
            if (index != signStopIndex)
            {
                signStopIndex = index;
                signText.text = s.Name;
                signText.fontStyle = region ? FontStyle.Bold : FontStyle.Normal;
            }

            // Metin ölçüsü her kare okunur: TextMesh ağı ilk karede henüz kurulmamış olabilir.
            float boardW = signTextRenderer.bounds.size.x + 0.5f;
            float baseY = -2.4f;
            float boardY = baseY + signPoleHeight + signBoardHeight / 2f;
            signRoot.localPosition = new Vector3(x, 0f, 0f);

            var poleColor = WorldTheme.Surface(SignPoleColor);
            LayoutPole(signPoleLeft, -boardW * 0.32f, baseY, poleColor);
            LayoutPole(signPoleRight, boardW * 0.32f, baseY, poleColor);

            signBoard.color = WorldTheme.Surface(region ? RegionBoardColor : TownBoardColor);
            signBoard.transform.localPosition = new Vector3(0f, boardY, 0f);
            signBoard.transform.localScale = new Vector3(boardW, signBoardHeight, 1f);
            signText.color = WorldTheme.Surface(region ? RegionTextColor : TownTextColor);
            signText.transform.localPosition = new Vector3(0f, boardY, -0.01f);
        }

        void LayoutPole(SpriteRenderer pole, float x, float baseY, Color color)
        {
            pole.color = color;
            pole.transform.localPosition = new Vector3(x, baseY + signPoleHeight / 2f, 0f);
            pole.transform.localScale = new Vector3(0.07f, signPoleHeight, 1f);
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
            // Açık denizde önünde tepe yok: şehir ufukta tamamen göründüğü için alçak bir kıyı silueti olsun.
            if (ground == Ground.Sea) height *= 0.55f;
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

            var color = surfaceColor;
            bool hidden = ground == Ground.Air && IsCompanion; // görev çubuğunda hava aracının altında yer yok
            float startX = camX - halfW - seg;
            for (int i = 0; i < roadSegments.Length; i++)
            {
                var sr = roadSegments[i];
                sr.enabled = i < needed && !hidden;
                if (!sr.enabled) continue;
                float x = startX + i * seg;
                sr.color = color;
                sr.transform.localPosition = new Vector3(x, RoadCenterY, 0f);
                sr.transform.localScale = new Vector3(seg * 1.02f, RoadH, 1f); // hafif bindirme: ek yeri çizgisi olmasın
            }
        }

        void LayoutDashes(float camX, float halfW)
        {
            // Şerit çizgisi asfaltta; nehirde (ve şeritteki denizde) kısa dalga çizgileri. Patikada ve
            // kartta açık denizde yok (orada LayoutMarks çizer).
            bool water = OnWater;
            bool show = ground == Ground.Asphalt || (ground == Ground.Air && !IsCompanion) || ground == Ground.River || (ground == Ground.Sea && IsCompanion);
            int needed = show ? Mathf.CeilToInt(halfW * 2f / dashSpacing) + 3 : 0;
            var dashColor = water ? Color.Lerp(surfaceColor, WorldTheme.Surface(WaveColor), 0.75f) : WorldTheme.Surface(DashColor);
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
                sr.transform.localScale = new Vector3(dashSpacing * (water ? 0.22f : 0.45f), DashHeight, 1f);
            }
        }

        SpriteRenderer[] Grow(SpriteRenderer[] pool, int size, string name, int order, Transform parent, Sprite sprite = null)
        {
            var grown = new SpriteRenderer[size];
            for (int i = 0; i < size; i++)
            {
                grown[i] = i < pool.Length ? pool[i] : CreateRenderer($"{name}{i}", sprite != null ? sprite : whiteSprite, order, parent);
            }
            return grown;
        }

        // Zemin işaretleri. Her işaret mutlak sırasından türetilen sabit rastgelelikle yerleşir: kaydıkça
        // desen değişmez, ızgara gibi de görünmez.
        //  - Patika: iki sıra teker izi + dağınık çakıl (açık/koyu).
        //  - Açık deniz (kart): ufka doğru küçülen, yavaşlayan dalga sıraları (derinlik hissi).
        int markCount;

        void LayoutMarks(float camX, float halfW)
        {
            markCount = 0;
            double scroll = drive.CurrentDistanceKmPrecise * roadUnitsPerKm;
            bool companion = IsCompanion;
            var c = surfaceColor;

            if (ground == Ground.Dirt)
            {
                var rut = Shade(c, 0.78f);
                var dark = Shade(c, 0.66f);
                var light = Color.Lerp(c, Color.white, 0.3f);
                if (companion)
                {
                    MarkRow(scroll, camX, halfW, 1.1f, RoadCenterY, 0f, new Vector2(0.5f, 0.022f), new Vector2(0.9f, 0.03f), rut, rut, 1, 0.3f);
                    MarkRow(scroll, camX, halfW, 0.6f, RoadCenterY, RoadH * 0.3f, new Vector2(0.02f, 0.02f), new Vector2(0.035f, 0.035f), dark, light, 5, 0.9f);
                }
                else
                {
                    MarkRow(scroll, camX, halfW, 1.1f, RoadCenterY + 0.14f, 0f, new Vector2(0.55f, 0.035f), new Vector2(1.0f, 0.05f), rut, rut, 1, 0.3f);
                    MarkRow(scroll, camX, halfW, 1.1f, RoadCenterY - 0.12f, 0f, new Vector2(0.55f, 0.035f), new Vector2(1.0f, 0.05f), rut, rut, 2, 0.3f);
                    MarkRow(scroll, camX, halfW, 0.42f, RoadCenterY, RoadH * 0.42f, new Vector2(0.025f, 0.022f), new Vector2(0.055f, 0.045f), dark, light, 5, 0.9f);
                }
            }
            else if (ground == Ground.Sea && !companion)
            {
                var wave = WorldTheme.Surface(WaveColor);
                // (y, hız çarpanı, aralık, en küçük boy, en büyük boy, beyazlık)
                MarkRow(scroll * 0.3, camX, halfW, 1.5f, -2.42f, 0.03f, new Vector2(0.12f, 0.018f), new Vector2(0.22f, 0.024f), Color.Lerp(c, wave, 0.3f), Color.Lerp(c, wave, 0.22f), 11, 0.6f);
                MarkRow(scroll * 0.55, camX, halfW, 1.35f, -2.68f, 0.05f, new Vector2(0.2f, 0.028f), new Vector2(0.34f, 0.036f), Color.Lerp(c, wave, 0.38f), Color.Lerp(c, wave, 0.3f), 12, 0.6f);
                MarkRow(scroll, camX, halfW, 1.25f, -3.12f, 0.06f, new Vector2(0.3f, 0.04f), new Vector2(0.5f, 0.05f), Color.Lerp(c, wave, 0.5f), Color.Lerp(c, wave, 0.4f), 13, 0.6f);
                MarkRow(scroll * 1.35, camX, halfW, 1.45f, -3.55f, 0.07f, new Vector2(0.45f, 0.05f), new Vector2(0.7f, 0.065f), Color.Lerp(c, wave, 0.55f), Color.Lerp(c, wave, 0.45f), 14, 0.6f);
            }

            for (int i = markCount; i < marks.Length; i++) marks[i].enabled = false;
        }

        void MarkRow(double scroll, float camX, float halfW, float spacing, float y, float yJitter,
            Vector2 sizeMin, Vector2 sizeMax, Color colorA, Color colorB, int seed, float xJitter)
        {
            long first = (long)Math.Floor((scroll + camX - halfW) / spacing) - 1;
            int count = Mathf.CeilToInt(halfW * 2f / spacing) + 3;
            if (marks.Length < markCount + count) marks = Grow(marks, markCount + count + 16, "Mark", 31, roadRoot, circleSprite);
            for (long j = first; j < first + count; j++)
            {
                float h1 = Hash(j, seed), h2 = Hash(j, seed + 101), h3 = Hash(j, seed + 202);
                var sr = marks[markCount++];
                sr.enabled = true;
                float x = (float)(j * spacing - scroll) + h1 * spacing * xJitter;
                sr.transform.localPosition = new Vector3(x, y + (h2 - 0.5f) * 2f * yJitter, 0f);
                sr.transform.localScale = new Vector3(Mathf.Lerp(sizeMin.x, sizeMax.x, h3), Mathf.Lerp(sizeMin.y, sizeMax.y, h3), 1f);
                sr.color = h3 > 0.5f ? colorA : colorB;
            }
        }

        static float Hash(long j, int seed)
        {
            unchecked
            {
                ulong x = (ulong)(j * 374761393L + seed * 668265263L);
                x = (x ^ (x >> 13)) * 1274126177UL;
                x ^= x >> 16;
                return (x & 0xFFFF) / 65535f;
            }
        }

        // Nehir kıyıları: suyun üstünde ve altında ince kum/çamur şeritleri (kartta).
        void LayoutBanks(float camX, float halfW)
        {
            bool show = ground == Ground.River && !IsCompanion;
            bankTop.enabled = bankBottom.enabled = show;
            if (!show) return;
            RouteMap.Blend(Math.Max(0.0, drive.CurrentDistanceKmPrecise), out var a, out var b, out float t);
            var bank = WorldTheme.Surface(Color.Lerp(Color.Lerp(a.Trail, b.Trail, t), Color.white, 0.12f));
            if (WorldTheme.Current == WorldTheme.Kind.Night) bank = Shade(bank, 0.45f);
            bank = Color.Lerp(bank, SecretRoadColor, secretBlend);
            bank.a = 1f;
            const float h = 0.07f;
            PlaceStrip(bankTop, camX, roadTopY + h / 2f - 0.01f, halfW * 2f + 2f, h, bank);
            PlaceStrip(bankBottom, camX, roadTopY - RoadH - h / 2f + 0.01f, halfW * 2f + 2f, h, bank);
        }

        static void PlaceStrip(SpriteRenderer sr, float x, float y, float width, float height, Color color)
        {
            sr.color = color;
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localScale = new Vector3(width, height, 1f);
        }

        void LayoutCar()
        {
            var t = carRenderer.transform;
            float speed = Mathf.Abs(drive.CurrentSpeedKmh);
            float facing = visualFacing >= 0f ? 1f : -1f;
            var basePos = new Vector2(0f, CarCenterY + bobOffset);
            float noseUp = pitch;
            bool pivotAtWheels = false;

            switch (ground)
            {
                case Ground.Asphalt:
                case Ground.Dirt:
                    // Yol titreşimi: rölantide neredeyse yok, hızlandıkça sıklaşır ve büyür (≤ ~1,5 px;
                    // patikada daha sert).
                    float amp = maxVibration * Mathf.Pow(Mathf.Clamp01((speed - 12f) / 150f), 0.7f);
                    if (ground == Ground.Dirt) amp *= dirtVibrationScale;
                    if (Time.timeScale > 0f) vibrationTime += Time.unscaledDeltaTime * (14f + speed * 0.12f);
                    basePos.y += (Mathf.PerlinNoise(vibrationTime, 0.37f) - 0.5f) * 2f * amp;
                    pivotAtWheels = true;
                    break;
                case Ground.River:
                case Ground.Sea:
                    // Tekne dalgada yavaşça yalpalar ve iner kalkar; büyük gemi ağır, daha az ve yavaş.
                    float tw = Time.time;
                    float heavy = Mathf.Clamp(1.4f / carWidthScale, 0.3f, 1f);
                    float slow = Mathf.Lerp(0.6f, 1f, heavy);
                    noseUp = noseUp * heavy + (Mathf.Sin(tw * 1.5f * slow) + Mathf.Sin(tw * 0.9f * slow + 1f) * 0.5f) * boatRockDeg * heavy;
                    basePos.y += Mathf.Sin(tw * 1.2f * slow) * 0.02f;
                    break;
            }

            // Kara araçları tekerleğin üstünde döner: burun kalkarken arka, inerken ön teker yerde kalır.
            var pos = basePos;
            float z = noseUp * facing;
            if (pivotAtWheels && Mathf.Abs(noseUp) > 0.0001f)
            {
                var pivotOffset = new Vector2(-Mathf.Sign(noseUp) * facing * CarWidth * 0.33f, -CarHeight * 0.5f);
                pos = basePos + pivotOffset + (Vector2)(Quaternion.Euler(0f, 0f, z) * -pivotOffset);
            }
            t.localPosition = new Vector3(pos.x, pos.y, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, z);

            float scale = CarWidth / carRenderer.sprite.bounds.size.x;
            if (carSprite == null)
            {
                t.localScale = new Vector3(CarWidth * visualFacing, CarWidth * 0.45f, 1f);
                carRenderer.color = new Color(0.93f, 0.42f, 0.33f);
                return;
            }
            // Zıplarken hafif uzama/basılma (hacim korunur gibi).
            float squash = Mathf.Clamp(bobVelocity * squashPerBobVelocity, -0.05f, 0.05f) + popOffset;
            // Eksi X ölçek = yatay flip; ortadan geçerken araç bir an "kenardan" görünür (U-dönüşü hissi).
            t.localScale = new Vector3(scale * visualFacing * (1f - squash * 0.5f), scale * (1f + squash), 1f);
            // Pop'ta taban yerde kalsın (basılınca araç havada küçülmesin).
            t.localPosition += new Vector3(0f, CarHeight * 0.5f * popOffset, 0f);
            carRenderer.color = Color.white;

            for (int i = 0; i < wheels.Length && i < MaxWheels; i++)
            {
                wheelRenderers[i].transform.localRotation = Quaternion.Euler(0f, 0f, wheelAngles[i]);
            }
            UpdateProps(speed, Time.deltaTime);

            // Stop lambası: küçük kırmızı çekirdek + yumuşak hale; boyutlar dünya biriminde sabit.
            bool lit = brakeLightPoint.HasValue && brakeLevel > 0.01f;
            brakeCore.enabled = brakeHalo.enabled = lit;
            if (lit)
            {
                var p = brakeLightPoint.Value;
                float inv = 1f / Mathf.Max(0.0001f, scale);
                // Mercan gövde de kırmızımsı: çekirdek açık-parlak, hale koyu kırmızı ki seçilsin.
                float core = (IsCompanion ? 0.06f : 0.08f) * inv, halo = (IsCompanion ? 0.18f : 0.26f) * inv;
                brakeCore.transform.localPosition = new Vector3(p.x, p.y, 0f);
                brakeHalo.transform.localPosition = new Vector3(p.x, p.y, 0f);
                brakeCore.transform.localScale = new Vector3(core, core, 1f);
                brakeHalo.transform.localScale = new Vector3(halo, halo, 1f);
                brakeCore.color = new Color(1f, 0.55f, 0.5f, brakeLevel);
                brakeHalo.color = new Color(0.95f, 0.05f, 0.08f, brakeLevel * 0.55f);
            }
        }

        // Toz/köpük çıkış noktası: hareket yönünün gerisinde kalan tekerleğin yere değdiği yer (geri
        // viteste ön teker). Tekerlek bilgisi yoksa aracın gerisi.
        Vector2 TrailPoint(float motion, float rearFraction)
        {
            var t = carRenderer.transform;
            if (wheels.Length > 0)
            {
                Vector3 best = Vector3.zero;
                float bestScore = float.NegativeInfinity;
                for (int i = 0; i < wheels.Length && i < MaxWheels; i++)
                {
                    var w = wheels[i];
                    var p = t.TransformPoint(new Vector3(w.center.x, w.center.y - w.tireRadius, 0f));
                    float score = -p.x * motion;
                    if (score > bestScore) { bestScore = score; best = p; }
                }
                var local = worldRoot.InverseTransformPoint(best);
                return new Vector2(local.x, ContactY);
            }
            return new Vector2(t.localPosition.x - motion * CarWidth * rearFraction, ContactY);
        }

        float MotionSign => drive.CurrentSpeedKmh < -1f ? -1f : 1f;

        // ---------------------------------------------------------------- parçacıklar

        // Toz: geride kalan tekerleğin dibinden kalkar, zeminle birlikte akar (UpdateParticles),
        // kendi küçük savrulması hareketin tersine. Patikada toprağın renginde ve iri.
        void SpawnDust()
        {
            float motion = MotionSign;
            ref var p = ref dust[nextDust];
            nextDust = (nextDust + 1) % dust.Length;

            bool dirt = ground == Ground.Dirt;
            p.maxLife = UnityEngine.Random.Range(0.35f, 0.6f) * (dirt ? 1.25f : 1f);
            p.life = p.maxLife;
            p.velocity = new Vector2(-motion * UnityEngine.Random.Range(0.1f, 0.4f), UnityEngine.Random.Range(0.35f, 0.8f));
            p.size = dirt ? 1.35f : 1f;
            var dustColor = dirt ? Color.Lerp(surfaceColor, Color.white, 0.22f) : WorldTheme.Surface(DustColor);
            dustColor.a = 1f;
            p.color = dustColor;
            p.renderer.enabled = true;
            var at = TrailPoint(motion, 0.42f);
            p.renderer.transform.localPosition = new Vector3(at.x + UnityEngine.Random.Range(-0.04f, 0.04f), at.y + 0.02f, 0f);
            p.renderer.transform.localRotation = Quaternion.identity;
        }

        // Köpük: kıçtan su çizgisinde doğar, suyla birlikte akar, yassı ve sönerek büyür.
        void SpawnFoam()
        {
            float motion = MotionSign;
            ref var p = ref foam[nextFoam];
            nextFoam = (nextFoam + 1) % foam.Length;

            p.maxLife = UnityEngine.Random.Range(0.6f, 1.0f);
            p.life = p.maxLife;
            p.velocity = new Vector2(-motion * UnityEngine.Random.Range(0.1f, 0.25f), UnityEngine.Random.Range(-0.03f, 0.03f));
            p.color = WorldTheme.Surface(FoamColor);
            p.renderer.enabled = true;
            float x = carRenderer.transform.localPosition.x - motion * CarWidth * 0.44f + UnityEngine.Random.Range(-0.04f, 0.04f);
            // Su çizgisi teknenin alt kenarının biraz üstü (gövdenin ortasından köpük çıkmasın).
            float waterLine = IsCompanion ? roadTopY - RoadH * 0.3f : roadTopY - RoadH * 0.45f + CarHeight * 0.1f;
            p.renderer.transform.localPosition = new Vector3(x, waterLine + UnityEngine.Random.Range(-0.03f, 0.02f), 0f);
            p.renderer.transform.localRotation = Quaternion.identity;
        }

        // Uçuş izi: kuyruktan çıkar, havayla (zemin hızında) geride kalır, genişleyip söner.
        void SpawnContrail()
        {
            float motion = MotionSign;
            ref var p = ref contrail[nextContrail];
            nextContrail = (nextContrail + 1) % contrail.Length;

            p.maxLife = UnityEngine.Random.Range(0.9f, 1.3f);
            p.life = p.maxLife;
            p.velocity = new Vector2(-motion * 0.05f, UnityEngine.Random.Range(-0.01f, 0.01f));
            p.size = IsCompanion ? 0.6f : 1f;
            var c = WorldTheme.Surface(new Color(0.98f, 0.98f, 1f));
            c.a = 1f;
            p.color = c;
            p.renderer.enabled = true;
            var t = carRenderer.transform;
            p.renderer.transform.localPosition = new Vector3(t.localPosition.x - motion * CarWidth * 0.47f, t.localPosition.y + CarHeight * 0.05f, 0f);
            p.renderer.transform.localRotation = Quaternion.identity;
        }

        // Hava aracında bulutlar uçağın önünden ve arkasından hıza göre akar (zemin olmayınca hız
        // hissini bunlar verir). İki sıra: arkada yavaş ve soluk, önde hızlı ve büyük. Görev
        // çubuğunda yalnız uçağın altından akan küçük bulutlar.
        void LayoutAirClouds(float camX, float halfW, Color sky)
        {
            int k = 0;
            if (ground == Ground.Air)
            {
                double scroll = drive.CurrentDistanceKmPrecise * roadUnitsPerKm;
                float lum = sky.r * 0.3f + sky.g * 0.59f + sky.b * 0.11f;
                var white = WorldTheme.Surface(new Color(1f, 1f, 0.98f));
                var back = Color.Lerp(sky, white, lum < 0.35f ? 0.12f : 0.45f);
                var front = Color.Lerp(sky, white, lum < 0.35f ? 0.2f : 0.85f);
                back.a = 1f;
                front.a = 0.85f;
                if (IsCompanion)
                {
                    // Görev çubuğu koyu: bulutlar tam beyaz değil, soluk.
                    var strip = WorldTheme.Surface(new Color(0.92f, 0.94f, 0.97f));
                    strip.a = 0.85f;
                    k = CloudRow(k, scroll * 0.9, camX, halfW, 2.2f, roadTopY - 0.02f, 0.03f, 0.35f, 0.6f, strip, 36, 21);
                }
                else
                {
                    float planeY = airAltitude + airLift; // süzülmeyi izlemesin
                    k = CloudRow(k, scroll * 0.55, camX, halfW, 3.2f, planeY - 0.15f, 0.75f, 0.7f, 1.3f, back, 36, 22);
                    k = CloudRow(k, scroll * 1.25, camX, halfW, 5.5f, planeY - 0.35f, 0.65f, 1.2f, 2.0f, front, 46, 23);
                }
            }
            for (int i = k; i < airClouds.Length; i++) airClouds[i].enabled = false;
        }

        int CloudRow(int k, double scroll, float camX, float halfW, float spacing, float y, float yJitter,
            float minW, float maxW, Color color, int order, int seed)
        {
            long first = (long)Math.Floor((scroll + camX - halfW - maxW) / spacing) - 1;
            int count = Mathf.CeilToInt((halfW * 2f + maxW * 2f) / spacing) + 3;
            if (airClouds.Length < k + count) airClouds = Grow(airClouds, k + count + 4, "AirCloud", 36, worldRoot, cloudSprite);
            var b = cloudSprite.bounds.size;
            for (long j = first; j < first + count; j++)
            {
                float h1 = Hash(j, seed), h2 = Hash(j, seed + 101), h3 = Hash(j, seed + 202);
                var sr = airClouds[k++];
                // Bazı yuvalar boş kalır: bulutlar düzenli aralıkla dizilmesin.
                sr.enabled = h3 > 0.25f;
                if (!sr.enabled) continue;
                float w = Mathf.Lerp(minW, maxW, h3);
                sr.sortingOrder = order;
                sr.color = color;
                sr.transform.localPosition = new Vector3((float)(j * spacing - scroll) + h1 * spacing * 0.5f, y + (h2 - 0.5f) * 2f * yJitter, 0f);
                sr.transform.localScale = new Vector3(w / b.x, w / b.x, 1f);
            }
            return k;
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

        // groundShift: zeminin bu karede kaydığı miktar — yere bırakılan izler onunla birlikte akar.
        static void UpdateParticles(Particle[] pool, float dt, float gravity, float drag, ParticleKind kind, float groundShift)
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
                t.localPosition += (Vector3)(p.velocity * dt) + new Vector3(groundShift, 0f, 0f);
                float frac = p.life / p.maxLife;

                switch (kind)
                {
                    case ParticleKind.Confetti:
                        t.Rotate(0f, 0f, p.spin * dt);
                        p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, Mathf.Clamp01(frac * 3f));
                        break;
                    case ParticleKind.Contrail:
                        // Doğarken ince ve belirgin, geride genişleyip söner.
                        p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, Mathf.Clamp01(frac * 1.4f) * 0.9f);
                        float c = Mathf.Lerp(0.2f, 0.07f, frac) * p.size;
                        t.localScale = new Vector3(c, c, 1f);
                        break;
                    case ParticleKind.Foam:
                        p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, frac * 0.75f);
                        float f = Mathf.Lerp(0.2f, 0.07f, frac);
                        t.localScale = new Vector3(f, f * 0.4f, 1f);
                        break;
                    default:
                        p.renderer.color = new Color(p.color.r, p.color.g, p.color.b, frac * 0.7f);
                        float s = Mathf.Lerp(0.05f, 0.17f, frac) * p.size;
                        t.localScale = new Vector3(s, s, 1f);
                        break;
                }
            }
        }

        // Düz renkli bulut: düz tabanlı, birkaç dairenin birleşimi (yumuşak kenar).
        static Sprite CreateCloudSprite()
        {
            const int w = 128, h = 56;
            var bumps = new[] { new Vector3(26, 20, 16), new Vector3(50, 28, 22), new Vector3(78, 26, 19), new Vector3(100, 18, 13) };
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            const float floor = 9f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = 0f;
                foreach (var b in bumps)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(b.x, b.y));
                    a = Mathf.Max(a, Mathf.Clamp01(b.z - d + 0.5f));
                }
                // Taban: tümseklerin altı düz bir şeritle doldurulur, altı kesilir.
                if (y + 0.5f >= floor && y + 0.5f <= 20f && x > 14 && x < 110) a = 1f;
                a = Mathf.Min(a, Mathf.Clamp01(y + 0.5f - floor + 0.5f));
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        }

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}
