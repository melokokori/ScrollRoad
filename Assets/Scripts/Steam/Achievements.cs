using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Başarımlar (2026-10-25): koşullar birkaç saniyede bir oyunun kendi kaydından değerlendirilir; açılan başarım
    // yerelde tutulur (PlayerPrefs JSON) ve Steam bağlıyken Steam'e de işlenir — Steam sonradan bağlanırsa bekleyenler
    // o zaman gönderilir. Oyunda kısa bir duyuru çıkar (Steam'in kendi bildirimi de gelir). API adları ve metinler
    // Docs/basarimlar.md'de; Steamworks'te aynı adlarla tanımlanmalı. Test app'inde (480, Spacewar) bu adlar yok,
    // gönderilmez. İnceleme modunda (her şey açık) değerlendirilmez.
    public class Achievements : MonoBehaviour
    {
        [SerializeField] NoticeController notice;
        [SerializeField] CarCatalog carCatalog;
        [SerializeField] float checkSeconds = 5f;

        public sealed class Def
        {
            public string Id;
            public string Name;
            public string Description;
        }

        // Sıra Docs/basarimlar.md ile aynı.
        public static readonly Def[] All =
        {
            new Def { Id = "FIRST_KM", Name = L.N("Ignition"), Description = L.N("Drive your first kilometre.") },
            new Def { Id = "KM_100", Name = L.N("Road Tripper"), Description = L.N("Drive 100 km in total.") },
            new Def { Id = "KM_1000", Name = L.N("Long Haul"), Description = L.N("Drive 1,000 km in total.") },
            new Def { Id = "KM_10000", Name = L.N("Million Scrolls"), Description = L.N("Drive 10,000 km in total.") },
            new Def { Id = "GARAGE_10", Name = L.N("Garage Sale"), Description = L.N("Unlock 10 vehicles.") },
            new Def { Id = "ALL_ACTIVITIES", Name = L.N("Jack of All Trades"), Description = L.N("Try every activity.") },
            new Def { Id = "FIRST_RARE", Name = L.N("Beginner's Luck"), Description = L.N("Find your first rare.") },
            new Def { Id = "RARES_50", Name = L.N("Collector"), Description = L.N("Find 50 different rares.") },
            new Def { Id = "RARES_150", Name = L.N("Curator"), Description = L.N("Find 150 different rares.") },
            new Def { Id = "RARES_ALL", Name = L.N("The Whole Album"), Description = L.N("Find every rare in every place.") },
            new Def { Id = "PLACE_COMPLETE", Name = L.N("Full Shelf"), Description = L.N("Find all six rares of one place.") },
            new Def { Id = "ACTIVITY_COMPLETE", Name = L.N("Master of One"), Description = L.N("Complete every place of one activity.") },
            new Def { Id = "NIGHT_OWL", Name = L.N("Night Owl"), Description = L.N("Find a rare that only comes out after dark.") },
            new Def { Id = "LUCKY_TIMING", Name = L.N("Right Place, Right Time"), Description = L.N("Find a rare during a happening.") },
            new Def { Id = "FOCUSED", Name = L.N("Eyes on the Prize"), Description = L.N("Find the rare you were focusing on.") },
            new Def { Id = "FIRST_ORDER", Name = L.N("Order Up"), Description = L.N("Complete an order.") },
            new Def { Id = "ORDERS_50", Name = L.N("Regular"), Description = L.N("Complete 50 orders.") },
            new Def { Id = "STREAK_7", Name = L.N("Seven Days"), Description = L.N("Finish all of the day's orders seven days in a row.") },
            new Def { Id = "NEW_LOOK", Name = L.N("New Look"), Description = L.N("Unlock a new look in any activity.") },
            new Def { Id = "SIDE_GIG", Name = L.N("Side Gig"), Description = L.N("Put an activity in the side slot.") },
        };

        [Serializable]
        class State
        {
            public List<string> unlocked = new List<string>();
            public List<string> sent = new List<string>(); // Steam'e işlenenler
        }

        const string SaveKey = "ScrollRoad_Achievements";

        State state;
        float timer;
        int lastFound = -1;
        string focusRare = "";
        int focusCount = -1;

        public static Achievements Instance { get; private set; }

        public bool IsUnlocked(string id) => state != null && state.unlocked.Contains(id);

        void Awake()
        {
            Instance = this;
            state = SaveService.LoadJson<State>(SaveKey);
            if (state.unlocked == null) state.unlocked = new List<string>();
            if (state.sent == null) state.sent = new List<string>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f || Time.frameCount < 10) return;
            timer = checkSeconds;
            if (!CarCatalog.ReviewUnlockAll) Evaluate();
            SendPending();
        }

        void Evaluate()
        {
            double km = CarCatalog.CurrentProgress.lifetimeKm;
            Check("FIRST_KM", km >= 1);
            Check("KM_100", km >= 100);
            Check("KM_1000", km >= 1000);
            Check("KM_10000", km >= 10000);
            if (carCatalog != null)
            {
                int cars = 0;
                foreach (var m in carCatalog.models) if (CarCatalog.IsUnlocked(m)) cars++;
                Check("GARAGE_10", cars >= 10);
            }
            Check("ALL_ACTIVITIES", ActivityGuide.NewCount == 0);

            int found = 0, total = 0;
            bool place = false, activity = false, night = false, look = false;
            foreach (var def in EnvironmentCatalog.All)
            {
                var c = EnvironmentContent.Get(def.Id);
                if (c == null) continue;
                bool allPlaces = c.Venues.Length > 0;
                foreach (var venue in c.Venues)
                {
                    var rares = c.RaresAt(venue.Id);
                    int n = 0;
                    for (int i = 0; i < rares.Length; i++)
                    {
                        if (!c.IsLogged(rares[i].Id)) continue;
                        n++;
                        if (EnvironmentContent.IsNightRare(def.Id, i)) night = true;
                    }
                    found += n;
                    total += rares.Length;
                    bool complete = rares.Length > 0 && n == rares.Length;
                    place |= complete;
                    allPlaces &= complete;
                }
                activity |= allPlaces;
                look |= c.Skins.Length > 1 && EnvironmentContent.IsSkinUnlocked(c, 1);
            }
            Check("FIRST_RARE", found >= 1);
            Check("RARES_50", found >= 50);
            Check("RARES_150", found >= 150);
            Check("RARES_ALL", total > 0 && found >= total);
            Check("PLACE_COMPLETE", place);
            Check("ACTIVITY_COMPLETE", activity);
            Check("NIGHT_OWL", night);
            Check("NEW_LOOK", look);

            // Etkinlik sürerken yeni bir nadir defterde.
            var current = EnvironmentManager.Current.Id;
            if (lastFound >= 0 && found > lastFound && Happenings.IsActive(current)) Check("LUCKY_TIMING", true);
            lastFound = found;

            // Odak'taki nadir geldi (ilk kez ya da yeniden).
            var content = EnvironmentContent.Current;
            string focus = content != null ? EnvironmentContent.FocusOf(current) : "";
            int times = content != null && focus != "" ? content.TimesFound(focus) : -1;
            if (focus != "" && focus == focusRare && focusCount >= 0 && times > focusCount) Check("FOCUSED", true);
            focusRare = focus;
            focusCount = times;

            var orders = OrdersController.Instance;
            if (orders != null)
            {
                Check("FIRST_ORDER", orders.CompletedTotal >= 1);
                Check("ORDERS_50", orders.CompletedTotal >= 50);
                Check("STREAK_7", orders.DayStreak >= 7);
            }
            Check("SIDE_GIG", SideActivity.Current != null);
        }

        void Check(string id, bool condition)
        {
            if (!condition || state.unlocked.Contains(id)) return;
            state.unlocked.Add(id);
            SaveService.SaveJson(SaveKey, state);
            var def = Array.Find(All, d => d.Id == id);
            if (notice != null && def != null) notice.Announce(L.F("Achievement: {0}", L.T(def.Name)), L.T(def.Description), L.F("Achievement · {0}", L.T(def.Name)));
        }

        // Steam'e henüz işlenmemiş açılmalar (gerçek App ID'de).
        void SendPending()
        {
            if (!SteamManager.IsInitialized || SteamManager.TestAppId == 480 || state.sent.Count >= state.unlocked.Count) return;
            bool any = false;
            foreach (var id in state.unlocked)
            {
                if (state.sent.Contains(id)) continue;
                try
                {
                    if (new Steamworks.Data.Achievement(id).Trigger(apply: false)) { state.sent.Add(id); any = true; }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Başarım gönderilemedi ({id}): {e.Message}");
                }
            }
            if (!any) return;
            try { Steamworks.SteamUserStats.StoreStats(); } catch (Exception) { }
            SaveService.SaveJson(SaveKey, state);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: inceleme modunda da bir kez değerlendir.
        public void DebugEvaluate() => Evaluate();
        public void DebugReset()
        {
            state = new State();
            SaveService.SaveJson(SaveKey, state);
        }
        public IReadOnlyList<string> DebugUnlocked => state.unlocked;
#endif
    }
}
