using System;
using UnityEngine;

namespace ScrollRoad
{
    // Döner kapı döngüsü (2026-10-14 prototip; Docs/ortamlar.md "Döner kapı"): yukarı scroll kapıyı
    // döndürür, kaldırımdaki insanlar sırayla bölmelere girip AVM'ye geçer; aşağı scroll kapıyı ters
    // çevirir, son girenler geri çıkar (sayaç da düşer). Rölantide kapı kendiliğinden yavaş döner
    // (ScoreEngine temposu). Başarısızlık yok.
    //
    // Birim: ziyaretçi sayısı. Kapının bu ayki konumu (puan) her EnvironmentCatalog.VisitorIntervalPoints'te
    // bir ziyaretçi içeri alır: konum / aralık = içerideki ziyaretçi. Sahne konumun saf fonksiyonudur
    // (DoorSceneView): kapı açısı ve herkesin yeri konumdan hesaplanır, geri sarınca aynı insanlar geri
    // yürür. Ayın i. ziyaretçisinin kimliği sabittir (VisitorCatalog.Identify); yalnız ayın rekorunu (monthBest)
    // aşan ziyaretçi ömür boyu sayıya ve deftere yazılır — geri çıkıp yeniden giren iki kez sayılmaz.
    // Sayaç aylık: ay değişince konum ve rekor sıfırlanır, misafir defteri kalıcıdır.
    //
    // Mekânlar (FishingController'daki gibi): AVM → Otel → Havalimanı → Müze → Gökdelen; Ortam Standardı v2
    // (2026-10-25) ile Kapı'da geçen seyir saatiyle açılır (UnlockLadder), duyuruyu ContentUnlockWatcher yapar;
    // oyuncu açık bir mekâna geçebilir. AVM'nin gizli efsanesi (Koca Ayak) gelince ayrıca duyurulur.
    //
    // Yalnız Döner kapı seçiliyken ilerler. Görünüm DoorSceneView'da, HUD DoorHudController'da.
    public class DoorController : MonoBehaviour, IEnvironmentContent
    {
        [SerializeField] NoticeController notice;
        [SerializeField] float autosaveIntervalSeconds = 10f;
        [SerializeField] int maxVisitsPerFrame = 50; // yalnız hata/test sıçramalarına karşı

        DoorProgress state;
        float autosaveTimer;

        public static DoorController Instance { get; private set; }

        public DoorProgress Progress => state;
        public double Interval => EnvironmentCatalog.VisitorIntervalPoints;
        // Kapının konumu ziyaretçi biriminde (kesirli): sahne bununla çizilir.
        public double Turns => state.position / Interval;
        public long VisitorsInside => (long)Math.Floor(Turns + 1e-9);
        // Dönüş yönü: + içeri, - geri çıkış, 0 duruyor (görünüm/HUD için).
        public float TurnDirection { get; private set; }

        public DoorVenue Venue => VisitorCatalog.GetVenue(state.venue);
        public VisitorType LastGuest => VisitorCatalog.GetGuest(state.lastGuestId);
        public bool LastWasNew { get; private set; }

        public VisitorCatalog.Visitor Identify(long index) => VisitorCatalog.Identify(state.month, Venue, index, EnvironmentContent.FocusOf(EnvironmentId.Door),
            EnvironmentContent.GuaranteeFor(EnvironmentId.Door, Venue.Id, Interval, state.log.Count == 0, state.guaranteeMonth, state.month));

        public int GuestsLoggedAt(DoorVenue venue)
        {
            int n = 0;
            foreach (var g in venue.Guests) if (state.Find(g.Id) != null) n++;
            return n;
        }

        public event Action<VisitorType, bool> GuestArrived; // tür, yeni mi
        public event Action<DoorVenue> VenueChanged;

        public bool IsUnlocked(DoorVenue venue) => IsUnlocked(venue, state);

        public static bool IsUnlocked(DoorVenue venue, DoorProgress progress) =>
            UnlockLadder.IsVenueUnlocked(LifetimePointsOf(progress), Array.IndexOf(VisitorCatalog.Venues, venue));

        public static double LifetimePointsOf(DoorProgress progress) =>
            progress == null ? 0.0 : progress.totalVisitors * EnvironmentCatalog.VisitorIntervalPoints;

        // ---- IEnvironmentContent (Ortam Standardı v2)
        static VenueInfo[] venueInfos;

        public EnvironmentDef Def => EnvironmentCatalog.Door;
        public double LifetimePoints => LifetimePointsOf(state);
        public string VenueNoun => "venue";
        public VenueInfo[] Venues => venueInfos ??= Array.ConvertAll(VisitorCatalog.Venues, v => new VenueInfo { Id = v.Id, Name = v.Name });
        public string VenueId => Venue.Id;
        public void SetVenue(string id) => SelectVenue(VisitorCatalog.GetVenue(id));
        public RareInfo[] RaresAt(string venueId) =>
            Array.ConvertAll(VisitorCatalog.GetVenue(venueId).Guests, r => new RareInfo { Id = r.Id, Name = r.Name, Frequency = r.Frequency });
        public bool IsLogged(string rareId) => state.Find(rareId) != null;
        public int TimesFound(string rareId) => state.Find(rareId)?.count ?? 0;
        public long NextItemIndex => state.monthBest + 1;
        public double MonthUnits => Turns;
        public string SkinNoun => "look";
        public string SkinBoxName => "Looks";
        public SkinInfo[] Skins => SkinCatalog.Door;
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
        public System.Collections.Generic.IEnumerable<string> LegacyAnnounced
        {
            get { foreach (var id in state.announcedVenues) yield return "venue:" + id; }
        }

        public bool SelectVenue(DoorVenue venue)
        {
            if (venue == null || !IsUnlocked(venue)) return false;
            if (venue == Venue) return true;
            state.venue = venue.Id;
            Save();
            VenueChanged?.Invoke(venue);
            return true;
        }

        void Awake()
        {
            Instance = this;
            state = SaveService.LoadDoor();
            if (state.announcedVenues == null) state.announcedVenues = new System.Collections.Generic.List<string>();
            if (state.log == null) state.log = new System.Collections.Generic.List<GuestRecord>();
            if (!IsUnlocked(Venue)) state.venue = VisitorCatalog.Mall.Id;
            EnvironmentContent.Register(this);
            if (CheckMonth()) Save();
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
            if (state != null) SaveService.SaveDoor(state);
        }

        void Update()
        {
            var engine = ScoreEngine.Instance;
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Door;
            if (engine == null || !active || Time.deltaTime <= 0f)
            {
                TurnDirection = 0f;
                return;
            }

            if (CheckMonth()) Save();

            TurnDirection = engine.Pace > 1f ? 1f : engine.Pace < -1f ? -1f : 0f;
            state.position = Math.Max(0.0, state.position + engine.FrameRawPoints);

            long inside = VisitorsInside;
            for (int n = 0; state.monthBest < inside && n < maxVisitsPerFrame; n++)
            {
                state.monthBest++;
                Visit(state.monthBest);
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

        // Ayın index. ziyaretçisi ilk kez içeri girdi.
        void Visit(long index)
        {
            state.totalVisitors++;
            var visitor = Identify(index);
            if (!visitor.IsGuest) return;

            var guest = visitor.Type;
            var record = state.Find(guest.Id);
            bool isNew = record == null;
            if (isNew)
            {
                record = new GuestRecord { id = guest.Id, firstSeen = DateTime.Now.ToString("yyyy-MM-dd") };
                state.log.Add(record);
                if (string.IsNullOrEmpty(state.guaranteeMonth)) state.guaranteeMonth = state.month;
            }
            record.count++;
            state.lastGuestId = guest.Id;
            LastWasNew = isNew;
            Save();

            GuestArrived?.Invoke(guest, isNew);
            if (visitor.IsLegendary && notice != null)
            {
                notice.Announce($"Legendary: {guest.Name}", $"The rarest guest of {Venue.Name} walked in", $"Legendary {guest.Name}!");
            }
            else if (isNew && notice != null)
            {
                notice.Announce($"Special guest: {guest.Name}", "Signed in your guest book", $"{guest.Name} · new guest");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: defteri ve ayın sayacını sıfırla.
        public void DebugReset()
        {
            state = new DoorProgress();
            CheckMonth();
            Save();
        }

        // YALNIZ test: kapıyı bu ayın verilen konumuna (ziyaretçi biriminde) koy; aradaki ziyaretçiler sayılır.
        public void DebugSetTurns(double turns)
        {
            state.position = Math.Max(0.0, turns) * Interval;
        }
#endif
    }
}
