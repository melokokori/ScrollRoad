using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Siparişler (Ortam Standardı v2 · Dayanak 4): günlük 3 + haftalık 1 siparişin üretimi, ilerlemesi, ödülü ve
    // paneli (≡ çekmecesinin başlığındaki "Orders" satırı açar). Gün yerel gece yarısı, hafta pazartesi yenilenir
    // İlerleme sipariş üretildiği andaki tabana göre sayılır; bir ortam yalnız seçiliyken ilerlediği için başka
    // ortamın siparişi oraya geçmeyi gerektirir. Seri (2026-10-25, kaldırılan GoalTracker'ın yerine): günün 3
    // siparişinin hepsi biterse günlük, haftalık sipariş biterse haftalık seri uzar. Kayıt yerel PlayerPrefs JSON.
    public class OrdersController : MonoBehaviour
    {
        [SerializeField] NoticeController notice;
        [SerializeField] GameObject panel;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text countText;
        [SerializeField] Image[] cardFrames;
        [SerializeField] Image[] cardThumbs;
        [SerializeField] TMP_Text[] cardTitles;
        [SerializeField] TMP_Text[] cardSubtitles;
        [SerializeField] RectTransform[] cardFills;
        [SerializeField] TMP_Text[] cardProgress;
        [SerializeField] Image weeklyFrame;
        [SerializeField] TMP_Text weeklyTitle;
        [SerializeField] RectTransform weeklyFill;
        [SerializeField] TMP_Text weeklyProgress;
        [SerializeField] Sprite[] activityPictures; // EnvironmentCatalog.All sırasıyla

        const string SaveKey = "ScrollRoad_Orders";
        static readonly Color Done = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.07f);

        OrdersState state;
        float checkTimer;

        public static OrdersController Instance { get; private set; }
        public GameObject Panel => panel;

        public int DailyDone => state == null ? 0 : state.daily.FindAll(o => o.done).Count;
        public int DailyCount => state == null ? 0 : state.daily.Count;
        public int WeeklyValue => state?.weekly == null ? 0 : Math.Min(state.weekly.target, Progress(state.weekly));
        public int WeeklyTarget => state?.weekly == null ? 0 : state.weekly.target;

        // Seri yalnız bugün/dün (bu hafta/geçen hafta) uzadıysa canlıdır.
        public int DayStreak => state == null ? 0
            : state.lastStreakDay == DayKey(DateTime.Today) || state.lastStreakDay == DayKey(DateTime.Today.AddDays(-1)) ? state.dayStreak : 0;
        public int WeekStreak => state == null ? 0
            : state.lastStreakWeek == WeekKey(DateTime.Today) || state.lastStreakWeek == WeekKey(DateTime.Today.AddDays(-7)) ? state.weekStreak : 0;

        void Awake()
        {
            Instance = this;
            state = SaveService.LoadJson<OrdersState>(SaveKey);
            if (state.daily == null) state.daily = new System.Collections.Generic.List<Order>();
            if (!state.streakMigrated)
            {
                // Kaldırılan günlük/haftalık hedeflerin serisi bir kez taşınır.
                var goals = SaveService.LoadGoals();
                state.dayStreak = goals.dayStreak;
                state.lastStreakDay = goals.lastDoneDay ?? "";
                state.weekStreak = goals.weekStreak;
                state.lastStreakWeek = goals.lastDoneWeek ?? "";
                state.streakMigrated = true;
                Save();
            }
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static string DayKey(DateTime date) => date.ToString("yyyy-MM-dd");
        static string WeekKey(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd");

        void Update()
        {
            checkTimer -= Time.unscaledDeltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 1f;
            // Ortam denetleyicileri Awake'te kaydolur; tabanlar ilk karelerde değil, kayıttan sonra alınır.
            if (Time.frameCount < 3) return;

            Renew();
            foreach (var o in state.daily) Check(o);
            if (state.weekly != null) Check(state.weekly);
            if (panel.activeSelf) Refresh();
        }

        void Renew()
        {
            string day = DayKey(DateTime.Today), week = WeekKey(DateTime.Today);
            bool dirty = false;
            if (state.day != day)
            {
                state.day = day;
                var rng = new System.Random(day.GetHashCode() ^ SystemInfo.deviceUniqueIdentifier.GetHashCode());
                var first = EnvironmentManager.Current.Id;
                state.daily.Clear();
                state.daily.Add(OrderCatalog.MakeDaily(first, rng));
                var others = new System.Collections.Generic.List<EnvironmentId>();
                foreach (var def in EnvironmentCatalog.All) if (def.Id != first) others.Add(def.Id);
                for (int i = 0; i < 2 && others.Count > 0; i++)
                {
                    int pick = rng.Next(others.Count);
                    state.daily.Add(OrderCatalog.MakeDaily(others[pick], rng));
                    others.RemoveAt(pick);
                }
                foreach (var o in state.daily) o.baseline = Measure(o);
                dirty = true;
            }
            if (state.week != week || state.weekly == null)
            {
                state.week = week;
                state.weekly = OrderCatalog.MakeWeekly();
                state.weekly.baseline = Measure(state.weekly);
                dirty = true;
            }
            if (dirty) Save();
        }

        void Save() => SaveService.SaveJson(SaveKey, state);

        // Siparişin ölçtüğü değerin şimdiki hâli (taban bununla alınır, ilerleme = şimdi − taban).
        static double Measure(Order o)
        {
            var env = (EnvironmentId)o.env;
            switch (o.kind)
            {
                case OrderKind.Units:
                    if (env == EnvironmentId.Road) return CarCatalog.CurrentProgress.lifetimeKm / EnvironmentCatalog.Road.UnitsPerPoint;
                    return EnvironmentContent.Get(env)?.LifetimePoints ?? 0.0;
                case OrderKind.Rares:
                {
                    var c = EnvironmentContent.Get(env);
                    if (c == null) return 0;
                    int n = 0;
                    foreach (var r in c.RaresAt(o.venue)) n += c.TimesFound(r.Id);
                    return n;
                }
                case OrderKind.Species:
                    return EnvironmentContent.Get(env)?.TimesFound(o.rare) ?? 0;
                default:
                {
                    int n = 0;
                    foreach (var def in EnvironmentCatalog.All)
                    {
                        var c = EnvironmentContent.Get(def.Id);
                        if (c == null) continue;
                        foreach (var v in c.Venues) foreach (var r in c.RaresAt(v.Id)) n += c.TimesFound(r.Id);
                    }
                    return n;
                }
            }
        }

        static int Progress(Order o)
        {
            double delta = Math.Max(0.0, Measure(o) - o.baseline);
            if (o.kind == OrderKind.Units) delta *= EnvironmentCatalog.Get((EnvironmentId)o.env).UnitsPerPoint;
            return (int)Math.Floor(delta + 1e-9);
        }

        void Check(Order o)
        {
            if (o.done || Progress(o) < o.target) return;
            o.done = true;
            state.completed++;

            // Ödül: siparişin mekânının (yoksa seçili ortamın bulunulan mekânının) eksik bir nadiri.
            var env = o.kind == OrderKind.AnyRares ? EnvironmentManager.Current.Id : (EnvironmentId)o.env;
            var c = EnvironmentContent.Get(env);
            string venue = c == null ? null : !string.IsNullOrEmpty(o.venue) ? o.venue : c.VenueId;
            var rng = new System.Random(Environment.TickCount);
            if (c != null && EnvironmentContent.GrantReward(c, venue, rng, out string rareName))
            {
                var v = Array.Find(c.Venues, x => x.Id == venue);
                o.result = $"{rareName} is on the way to {v?.Name ?? venue}";
            }
            else
            {
                o.result = "Stamped in your order book";
            }

            string streak = o == state.weekly ? ExtendWeekStreak() : ExtendDayStreak();
            Save();
            if (notice != null) notice.Announce($"Order done: {OrderCatalog.Title(o)}", streak ?? o.result, "Order done");
        }

        // Günün son siparişi bittiyse seri uzar; duyurunun alt satırı (yoksa null).
        string ExtendDayStreak()
        {
            string today = DayKey(DateTime.Today);
            if (state.lastStreakDay == today || state.daily.Exists(d => !d.done)) return null;
            state.dayStreak = state.lastStreakDay == DayKey(DateTime.Today.AddDays(-1)) ? state.dayStreak + 1 : 1;
            state.lastStreakDay = today;
            return $"All of today's orders done · streak: {Fmt.Count(state.dayStreak, "day")}";
        }

        string ExtendWeekStreak()
        {
            string week = WeekKey(DateTime.Today);
            if (state.lastStreakWeek == week) return null;
            state.weekStreak = state.lastStreakWeek == WeekKey(DateTime.Today.AddDays(-7)) ? state.weekStreak + 1 : 1;
            state.lastStreakWeek = week;
            return $"{state.weekly.result} · streak: {Fmt.Count(state.weekStreak, "week")}";
        }

        public void Open()
        {
            Renew();
            Refresh();
            panel.SetActive(true);
        }

        void Refresh()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            double hoursLeft = (tomorrow - DateTime.Now).TotalHours;
            countText.text = $"New orders in {UnlockLadder.Remaining(hoursLeft * UnlockLadder.PointsPerHour)}  ·  {Fmt.Int(state.completed)} done in all";

            for (int i = 0; i < cardFrames.Length; i++)
            {
                bool exists = i < state.daily.Count;
                cardFrames[i].gameObject.SetActive(exists);
                if (!exists) continue;
                var o = state.daily[i];
                int value = Math.Min(o.target, Progress(o));
                cardFrames[i].color = o.done ? Done : Idle;
                int envIndex = Array.FindIndex(EnvironmentCatalog.All, d => (int)d.Id == o.env);
                cardThumbs[i].sprite = envIndex >= 0 && envIndex < activityPictures.Length ? activityPictures[envIndex] : null;
                cardTitles[i].text = OrderCatalog.Title(o);
                cardSubtitles[i].text = o.done ? $"<color=#FFFFFF>DONE</color>  ·  {o.result}" : OrderCatalog.Subtitle(o);
                cardFills[i].anchorMax = new Vector2(o.target <= 0 ? 1f : (float)value / o.target, 1f);
                cardProgress[i].text = $"{Fmt.Int(value)} / {Fmt.Int(o.target)}";
            }

            var w = state.weekly;
            int wv = Math.Min(w.target, Progress(w));
            weeklyFrame.color = w.done ? Done : Idle;
            weeklyTitle.text = w.done ? $"{OrderCatalog.Title(w)}  ·  <alpha=#AA>{w.result}" : $"WEEKLY  ·  {OrderCatalog.Title(w)}";
            weeklyFill.anchorMax = new Vector2(w.target <= 0 ? 1f : (float)wv / w.target, 1f);
            weeklyProgress.text = $"{wv} / {w.target}";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: siparişleri hemen yenile.
        public void DebugRenew()
        {
            state.day = "";
            state.week = "";
            Renew();
        }

        public OrdersState DebugState => state;
#endif
    }
}
