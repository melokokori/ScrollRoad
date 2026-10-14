using System;
using UnityEngine;

namespace ScrollRoad
{
    // Günlük ve haftalık hedefler (2026-10-10, kullanıcı kararı): miktar ve aktif süre AYRI ölçülür.
    // 2026-10-13: miktar gizli ortak puanla tutulur (ScoreEngine) ve aktif ortamın biriminde yazılır
    // (Yol'da km, Olta'da kg); puan hiç görünmez. Eski km kaydı bir kez puana taşınır (1 km = 1000).
    // Ödül yalnız tik + seri; sıralamayı etkilemez. Bir gün/hafta, iki hedefi de tutunca "tamam"
    // sayılır ve seriyi uzatır. Gün yerel gece yarısında, hafta pazartesi başlar.
    // Aktif süre = RoadDrive.IsUserActive (son gerçek scroll'dan 3 dk içinde) olduğu süre;
    // yalnız açık durmak saymaz — tepside/autostart ile hedef kendiliğinden dolmasın.
    // Kayıt şimdilik yerel (Steam Cloud'a dahil değil).
    public class GoalTracker : MonoBehaviour
    {
        public enum Goal { DailyDistance, DailyTime, WeeklyDistance, WeeklyTime }

        // Değerler "sonra belirleriz": demo verisine göre ayarlanacak. Puan: 80.000 = Yol'da 80 km.
        [Header("Günlük")]
        [SerializeField] double dailyPointsGoal = 80000;
        [SerializeField] float dailyMinutesGoal = 60f;
        [Header("Haftalık")]
        [SerializeField] double weeklyPointsGoal = 500000;
        [SerializeField] float weeklyHoursGoal = 6f;

        [SerializeField] float autosaveIntervalSeconds = 15f;

        GoalProgress state;
        float autosaveTimer;
        float dateCheckTimer;

        // goal, gün/hafta da tamamlandı mı (iki hedef birden)
        public event Action<Goal, bool> GoalCompleted;

        public double DailyPointsGoal => dailyPointsGoal;
        public float DailyMinutesGoal => dailyMinutesGoal;
        public double WeeklyPointsGoal => weeklyPointsGoal;
        public float WeeklyHoursGoal => weeklyHoursGoal;

        public double DayPoints => state.dayPoints;
        public double DayMinutes => state.daySeconds / 60.0;
        public double WeekPoints => state.weekPoints;
        public double WeekHours => state.weekSeconds / 3600.0;

        // "4 / 80 km" ya da "80 km done" — aktif ortamın biriminde, tam sayı.
        public string AmountLine(bool weekly)
        {
            var env = EnvironmentManager.Current;
            double value = weekly ? state.weekPoints : state.dayPoints;
            double goal = weekly ? weeklyPointsGoal : dailyPointsGoal;
            string goalText = env.Format(goal, 0);
            return value >= goal ? $"{goalText} done" : $"{Fmt.Int((long)env.ToUnits(value))} / {goalText}";
        }

        public bool IsDone(Goal goal)
        {
            switch (goal)
            {
                case Goal.DailyDistance: return state.dayPoints >= dailyPointsGoal;
                case Goal.DailyTime: return state.daySeconds >= dailyMinutesGoal * 60.0;
                case Goal.WeeklyDistance: return state.weekPoints >= weeklyPointsGoal;
                default: return state.weekSeconds >= weeklyHoursGoal * 3600.0;
            }
        }

        // Seri yalnız dün/bugün (geçen hafta/bu hafta) tamamlandıysa canlıdır.
        public int DayStreak =>
            state.lastDoneDay == DayKey(DateTime.Today) || state.lastDoneDay == DayKey(DateTime.Today.AddDays(-1)) ? state.dayStreak : 0;

        public int WeekStreak =>
            state.lastDoneWeek == WeekKey(DateTime.Today) || state.lastDoneWeek == WeekKey(DateTime.Today.AddDays(-7)) ? state.weekStreak : 0;

        void Awake()
        {
            state = SaveService.LoadGoals();
            if (state.pointsVersion < 1)
            {
                // Puan katmanından önceki kayıt: bugünkü/bu haftaki km puana (idempotent: sürüm yazılır).
                double ppk = 1.0 / EnvironmentCatalog.Road.UnitsPerPoint;
                state.dayPoints = state.dayKm * ppk;
                state.weekPoints = state.weekKm * ppk;
                state.pointsVersion = 1;
                Save();
            }
            RollOver();
        }

        void OnDestroy() => Save();
        void OnApplicationQuit() => Save();

        void Update()
        {
            dateCheckTimer += Time.unscaledDeltaTime;
            if (dateCheckTimer >= 1f)
            {
                dateCheckTimer = 0f;
                RollOver();
            }

            var engine = ScoreEngine.Instance;
            if (engine == null) return;

            bool dayKm = IsDone(Goal.DailyDistance), dayTime = IsDone(Goal.DailyTime);
            bool weekKm = IsDone(Goal.WeeklyDistance), weekTime = IsDone(Goal.WeeklyTime);

            // Yalnız ileri ilerleme (ay konumunun artışı) sayılır; hangi ortamda olursa olsun.
            double gain = engine.FrameGainPoints;
            if (gain > 0)
            {
                state.dayPoints += gain;
                state.weekPoints += gain;
            }

            if (Time.timeScale > 0f && engine.IsUserActive)
            {
                state.daySeconds += Time.unscaledDeltaTime;
                state.weekSeconds += Time.unscaledDeltaTime;
            }

            if (!dayKm && IsDone(Goal.DailyDistance)) Complete(Goal.DailyDistance);
            if (!dayTime && IsDone(Goal.DailyTime)) Complete(Goal.DailyTime);
            if (!weekKm && IsDone(Goal.WeeklyDistance)) Complete(Goal.WeeklyDistance);
            if (!weekTime && IsDone(Goal.WeeklyTime)) Complete(Goal.WeeklyTime);

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        void Complete(Goal goal)
        {
            bool daily = goal == Goal.DailyDistance || goal == Goal.DailyTime;
            bool periodDone = daily
                ? IsDone(Goal.DailyDistance) && IsDone(Goal.DailyTime)
                : IsDone(Goal.WeeklyDistance) && IsDone(Goal.WeeklyTime);

            // İki hedef aynı karede dolarsa seri bir kez sayılsın.
            string today = DayKey(DateTime.Today), thisWeek = WeekKey(DateTime.Today);
            if (periodDone && daily && state.lastDoneDay != today)
            {
                state.dayStreak = state.lastDoneDay == DayKey(DateTime.Today.AddDays(-1)) ? state.dayStreak + 1 : 1;
                state.lastDoneDay = today;
            }
            else if (periodDone && !daily && state.lastDoneWeek != thisWeek)
            {
                state.weekStreak = state.lastDoneWeek == WeekKey(DateTime.Today.AddDays(-7)) ? state.weekStreak + 1 : 1;
                state.lastDoneWeek = thisWeek;
            }

            Save();
            GoalCompleted?.Invoke(goal, periodDone);
        }

        // Gün/hafta değiştiyse sayaçları sıfırla (seri bilgisi korunur).
        void RollOver()
        {
            string day = DayKey(DateTime.Today);
            string week = WeekKey(DateTime.Today);
            bool changed = false;
            if (state.dayKey != day)
            {
                state.dayKey = day;
                state.dayKm = 0;
                state.dayPoints = 0;
                state.daySeconds = 0;
                changed = true;
            }
            if (state.weekKey != week)
            {
                state.weekKey = week;
                state.weekKm = 0;
                state.weekPoints = 0;
                state.weekSeconds = 0;
                changed = true;
            }
            if (changed) Save();
        }

        void Save()
        {
            if (state != null) SaveService.SaveGoals(state);
        }

        static string DayKey(DateTime date) => date.ToString("yyyy-MM-dd");

        // Haftanın anahtarı = o haftanın pazartesisi.
        static string WeekKey(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: hedef tamamlama/seri akışını beklemeden denemek için.
        public void DebugAdd(double points, double seconds)
        {
            state.dayPoints += points;
            state.weekPoints += points;
            state.daySeconds += seconds;
            state.weekSeconds += seconds;
        }

        public void DebugResetAll()
        {
            state = new GoalProgress();
            RollOver();
        }
#endif
    }

    [Serializable]
    public class GoalProgress
    {
        public string dayKey = "";
        public int pointsVersion;   // 0 = puan katmanından önceki kayıt (dayKm/weekKm geçerli)
        public double dayPoints;
        public double weekPoints;
        public double dayKm;        // eski alan, yalnız taşıma için okunur
        public double daySeconds;
        public string weekKey = "";
        public double weekKm;
        public double weekSeconds;
        public int dayStreak;
        public string lastDoneDay = "";
        public int weekStreak;
        public string lastDoneWeek = "";
    }
}
