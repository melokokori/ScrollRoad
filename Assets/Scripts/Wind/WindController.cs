using System;
using UnityEngine;

namespace ScrollRoad
{
    // Rüzgârgülü döngüsü (2026-10-14 prototip; Docs/ortamlar.md "Rüzgârgülü"): yukarı scroll pervaneyi
    // hızlandırır, üretilen her kWh vadide bir yapıyı şebekeye bağlar; aşağı scroll pervaneyi yavaşlatır /
    // ters çevirir, son bağlananlar yeniden kararır (sayaç da düşer). Rölantide hafif esinti (ScoreEngine
    // temposu). Başarısızlık yok.
    //
    // Birim: kWh. Bu ayki konum (puan) her EnvironmentCatalog.KwhIntervalPoints'te bir kWh; konum / aralık =
    // bağlı yapı sayısı. Sahne konumun saf fonksiyonu (WindSceneView). Ayın i. yapısının kimliği sabittir
    // (GridCatalog.Identify); yalnız ayın rekorunu (monthBest) aşan yapı ömür boyu sayıya ve deftere yazılır.
    // Sayaç aylık; şebeke defteri kalıcı. Kapı ile birebir aynı model (DoorController).
    //
    // Yalnız Rüzgârgülü seçiliyken ilerler. Görünüm WindSceneView'da, HUD WindHudController'da.
    public class WindController : MonoBehaviour, IEnvironmentContent
    {
        [SerializeField] NoticeController notice;
        [SerializeField] float autosaveIntervalSeconds = 10f;
        [SerializeField] int maxPerFrame = 50; // yalnız hata/test sıçramalarına karşı

        WindProgress state;
        float autosaveTimer;

        public static WindController Instance { get; private set; }

        public WindProgress Progress => state;
        public double Interval => EnvironmentCatalog.KwhIntervalPoints;
        // Bu ay üretilen kWh (kesirli): sahne bununla çizilir.
        public double Kwh => state.position / Interval;
        public long Connected => (long)Math.Floor(Kwh + 1e-9);
        // Bu ayın puan konumu (pervane açısı bundan türer).
        public double Position => state.position;
        // + üretim, - ters dönüş, 0 duruyor.
        public float SpinDirection { get; private set; }

        public WindSite Site => GridCatalog.GetSite(state.site);
        public BuildingType LastSpecial => GridCatalog.GetSpecial(state.lastSpecialId);

        public GridCatalog.Building Identify(long index) => GridCatalog.Identify(state.month, Site, index, EnvironmentContent.FocusOf(EnvironmentId.Wind),
            EnvironmentContent.GuaranteeFor(EnvironmentId.Wind, Site.Id, Interval, state.log.Count == 0, state.guaranteeMonth, state.month));

        public int LoggedAt(WindSite site)
        {
            int n = 0;
            foreach (var b in site.Specials) if (state.Find(b.Id) != null) n++;
            return n;
        }


        // ---- IEnvironmentContent (Ortam Standardı v2)
        static VenueInfo[] venueInfos;

        public EnvironmentDef Def => EnvironmentCatalog.Wind;
        public double LifetimePoints => state.totalKwh * Interval;
        public string VenueNoun => "site";
        public VenueInfo[] Venues => venueInfos ??= Array.ConvertAll(GridCatalog.Sites, v => new VenueInfo { Id = v.Id, Name = v.Name });
        public string VenueId => Site.Id;
        public void SetVenue(string id)
        {
            if (Array.FindIndex(GridCatalog.Sites, s => s.Id == id) < 0 || state.site == id) return;
            state.site = id;
            Save();
        }
        public RareInfo[] RaresAt(string venueId) =>
            Array.ConvertAll(GridCatalog.GetSite(venueId).Specials, r => new RareInfo { Id = r.Id, Name = r.Name, Frequency = r.Frequency });
        public bool IsLogged(string rareId) => state.Find(rareId) != null;
        public int TimesFound(string rareId) => state.Find(rareId)?.count ?? 0;
        public long NextItemIndex => state.monthBest + 1;
        public double MonthUnits => Kwh;
        public string SkinNoun => "look";
        public string SkinBoxName => "Looks";
        public SkinInfo[] Skins => SkinCatalog.Wind;
        public string SkinId
        {
            get
            {
                int i = EnvironmentContent.SkinIndex(this, state.skin);
                return i > 0 && EnvironmentContent.IsSkinUnlocked(this, i) ? state.skin : Skins[0].Id;
            }
        }
        public SkinInfo Skin => Skins[Math.Max(0, EnvironmentContent.SkinIndex(this, SkinId))];
        public void SetSkin(string id)
        {
            state.skin = id;
            Save();
        }
        public System.Collections.Generic.IEnumerable<string> LegacyAnnounced => Array.Empty<string>();

        public event Action<BuildingType, bool> SpecialConnected; // yapı, yeni mi
        public event Action BuildingConnected;                     // her bağlanan yapı (ses/kıvılcım için)

        void Awake()
        {
            Instance = this;
            state = SaveService.LoadWind();
            if (state.log == null) state.log = new System.Collections.Generic.List<GridRecord>();
            if (CheckMonth()) Save();
            EnvironmentContent.Register(this);
        }

        void OnDestroy()
        {
            Save();
            EnvironmentContent.Unregister(this);
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() => Save();

        void Save()
        {
            if (state != null) SaveService.SaveWind(state);
        }

        void Update()
        {
            var engine = ScoreEngine.Instance;
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Wind;
            if (engine == null || !active || Time.deltaTime <= 0f)
            {
                SpinDirection = 0f;
                return;
            }

            if (CheckMonth()) Save();

            SpinDirection = engine.Pace > 1f ? 1f : engine.Pace < -1f ? -1f : 0f;
            state.position = Math.Max(0.0, state.position + engine.FrameRawPoints);

            long connected = Connected;
            for (int n = 0; state.monthBest < connected && n < maxPerFrame; n++)
            {
                state.monthBest++;
                Connect(state.monthBest);
            }

            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        bool CheckMonth()
        {
            string month = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            if (state.month == month) return false;
            state.month = month;
            state.position = 0;
            state.monthBest = 0;
            return true;
        }

        // Ayın index. yapısı ilk kez şebekeye bağlandı.
        void Connect(long index)
        {
            state.totalKwh++;
            BuildingConnected?.Invoke();
            var building = Identify(index);
            if (!building.IsSpecial) return;

            var type = building.Type;
            var record = state.Find(type.Id);
            bool isNew = record == null;
            if (isNew)
            {
                record = new GridRecord { id = type.Id, firstSeen = DateTime.Now.ToString("yyyy-MM-dd") };
                state.log.Add(record);
                if (string.IsNullOrEmpty(state.guaranteeMonth)) state.guaranteeMonth = state.month;
            }
            record.count++;
            state.lastSpecialId = type.Id;
            Save();

            SpecialConnected?.Invoke(type, isNew);
            if (isNew && notice != null)
            {
                notice.Announce($"Powered up: {type.Name}", "Stamped in your grid log", $"{type.Name} · now on the grid");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: şebeke defterini ve ayın sayacını sıfırla.
        public void DebugReset()
        {
            state = new WindProgress();
            CheckMonth();
            Save();
        }
#endif
    }
}
