using System;
using UnityEngine;

namespace ScrollRoad
{
    // Plak tablası döngüsü (2026-10-14 prototip; Docs/ortamlar.md "Plak tablası"): yukarı scroll plağı
    // döndürür, müzik scroll hızında çalar (VinylMusic), her EnvironmentCatalog.TrackIntervalPoints'te bir
    // parça biter; RecordCatalog.TracksPerRecord parçada plak biter ve sandığa girer. Aşağı scroll plağı
    // ters çevirir: iğne geri gider, parça sayacı düşer (ay rekoru aşılmadan iki kez sayılmaz). Rölantide plak
    // yavaş döner. Başarısızlık yok. Kapı/Rüzgârgülü ile aynı model: sahne bu ayın konumunun saf fonksiyonu.
    public class VinylController : MonoBehaviour, IEnvironmentContent
    {
        [SerializeField] NoticeController notice;
        [SerializeField] float autosaveIntervalSeconds = 10f;
        [SerializeField] int maxPerFrame = 50;

        VinylProgress state;
        float autosaveTimer;

        public static VinylController Instance { get; private set; }

        public VinylProgress Progress => state;
        public double Interval => EnvironmentCatalog.TrackIntervalPoints;
        public double Position => state.position;
        // Bu ay çalınan parça (kesirli).
        public double Tracks => state.position / Interval;
        public long TracksDone => (long)Math.Floor(Tracks + 1e-9);
        // Şu an tabladaki plak (1'den) ve plaktaki parça (1..TracksPerRecord), parçanın ilerlemesi 0..1.
        public long RecordIndex => TracksDone / RecordCatalog.TracksPerRecord + 1;
        public int TrackInRecord => (int)(TracksDone % RecordCatalog.TracksPerRecord) + 1;
        public float TrackProgress => (float)(Tracks - Math.Floor(Tracks));
        // Plağın tamamındaki ilerleme 0..1 (iğnenin yeri).
        public float RecordProgress => (float)((Tracks - (RecordIndex - 1) * (double)RecordCatalog.TracksPerRecord) / RecordCatalog.TracksPerRecord);
        public float SpinDirection { get; private set; }

        public RecordShop Shop => RecordCatalog.GetShop(state.shop);
        public RecordType LastSpecial => RecordCatalog.GetSpecial(state.lastSpecialId);
        public string Month => state.month;

        public RecordCatalog.Record Identify(long recordIndex) => RecordCatalog.Identify(state.month, Shop, recordIndex, EnvironmentContent.FocusOf(EnvironmentId.Vinyl));
        public RecordCatalog.Record Current => Identify(RecordIndex);
        public ulong CurrentTrackSeed => RecordCatalog.TrackSeed(state.month, Shop, TracksDone);

        public int LoggedAt(RecordShop shop)
        {
            int n = 0;
            foreach (var r in shop.Specials) if (state.Find(r.Id) != null) n++;
            return n;
        }


        // ---- IEnvironmentContent (Ortam Standardı v2)
        static VenueInfo[] venueInfos;

        public EnvironmentDef Def => EnvironmentCatalog.Vinyl;
        public double LifetimePoints => state.totalTracks * Interval;
        public string VenueNoun => "shop";
        public VenueInfo[] Venues => venueInfos ??= Array.ConvertAll(RecordCatalog.Shops, v => new VenueInfo { Id = v.Id, Name = v.Name });
        public string VenueId => Shop.Id;
        public void SetVenue(string id)
        {
            if (Array.FindIndex(RecordCatalog.Shops, s => s.Id == id) < 0 || state.shop == id) return;
            state.shop = id;
            Save();
        }
        public RareInfo[] RaresAt(string venueId) =>
            Array.ConvertAll(RecordCatalog.GetShop(venueId).Specials, r => new RareInfo { Id = r.Id, Name = r.Name, Frequency = r.Frequency });
        public bool IsLogged(string rareId) => state.Find(rareId) != null;
        public string SkinNoun => "look";
        public SkinInfo[] Skins => EnvironmentContent.ClassicOnly;
        public string SkinId => "classic";
        public void SetSkin(string id) { }
        public System.Collections.Generic.IEnumerable<string> LegacyAnnounced => Array.Empty<string>();

        public event Action<RecordType, bool> SpecialCrated; // plak, yeni mi
        public event Action TrackFinished;

        void Awake()
        {
            Instance = this;
            state = SaveService.LoadVinyl();
            if (state.log == null) state.log = new System.Collections.Generic.List<CrateRecord>();
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
            if (state != null) SaveService.SaveVinyl(state);
        }

        void Update()
        {
            var engine = ScoreEngine.Instance;
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Vinyl;
            if (engine == null || !active || Time.deltaTime <= 0f)
            {
                SpinDirection = 0f;
                return;
            }

            if (CheckMonth()) Save();

            SpinDirection = engine.Pace > 1f ? 1f : engine.Pace < -1f ? -1f : 0f;
            state.position = Math.Max(0.0, state.position + engine.FrameRawPoints);

            long done = TracksDone;
            for (int n = 0; state.monthBest < done && n < maxPerFrame; n++)
            {
                state.monthBest++;
                Finish(state.monthBest);
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

        // Ayın track. parçası ilk kez bitti; plağın son parçasıysa plak sandığa girer.
        void Finish(long track)
        {
            state.totalTracks++;
            TrackFinished?.Invoke();
            if (track % RecordCatalog.TracksPerRecord != 0) return;

            state.totalRecords++;
            var record = Identify(track / RecordCatalog.TracksPerRecord);
            if (!record.IsSpecial) return;

            var type = record.Type;
            var entry = state.Find(type.Id);
            bool isNew = entry == null;
            if (isNew)
            {
                entry = new CrateRecord { id = type.Id, firstSeen = DateTime.Now.ToString("yyyy-MM-dd") };
                state.log.Add(entry);
            }
            entry.count++;
            state.lastSpecialId = type.Id;
            Save();

            SpecialCrated?.Invoke(type, isNew);
            if (isNew && notice != null)
            {
                notice.Announce($"New record: {type.Name}", "Filed in your record crate", $"{type.Name} · into the crate");
            }
        }
    }
}
