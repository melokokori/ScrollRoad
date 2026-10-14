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
    // Mekânlar (FishingController'daki gibi): AVM → Otel → Havalimanı → Müze → Gökdelen; her biri ömür boyu
    // ziyaretçi sayısıyla açılır, açıldığında duyurulur ve kapı oraya geçer; oyuncu defterden açık bir mekâna
    // dönebilir. AVM'nin gizli efsanesi (Koca Ayak) gelince ayrıca duyurulur.
    //
    // Yalnız Döner kapı seçiliyken ilerler. Görünüm DoorSceneView'da, HUD DoorHudController'da.
    public class DoorController : MonoBehaviour
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

        public VisitorCatalog.Visitor Identify(long index) => VisitorCatalog.Identify(state.month, Venue, index);

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
            venue.UnlockTotalVisitors <= 0 || CarCatalog.ReviewUnlockAll || progress.totalVisitors >= venue.UnlockTotalVisitors;

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
            if (CheckMonth()) Save();
        }

        void OnDestroy()
        {
            Save();
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
            CheckVenueUnlocks();

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

        void CheckVenueUnlocks()
        {
            if (Time.realtimeSinceStartup < 4f || Time.frameCount % 30 != 0) return;
            foreach (var venue in VisitorCatalog.Venues)
            {
                if (venue.UnlockTotalVisitors <= 0 || state.announcedVenues.Contains(venue.Id)) continue;
                if (state.totalVisitors < venue.UnlockTotalVisitors) continue;
                state.announcedVenues.Add(venue.Id);
                SelectVenue(venue);
                Save();
                if (notice != null)
                {
                    notice.Announce($"New venue: {venue.Name}", $"{venue.Guests.Length} new guests to meet", $"{venue.Name} unlocked");
                }
            }
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
