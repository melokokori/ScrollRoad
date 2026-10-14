using System;
using ScrollRoad.InputCapture;
using UnityEngine;

namespace ScrollRoad
{
    // Gizli ortak puan motoru (2026-10-13; Docs/ortamlar.md "Ortak skor = arka planda bir puan").
    // Scroll + seyir modeli (seyir + boost, aktiflik penceresi, rölanti) RoadDrive'dan buraya taşındı
    // ve AYNEN korunuyor; çıktısı ortamdan bağımsız bir "tempo" ve puandır. Her ortam kendi birimini
    // bu puandan oranla türetir (EnvironmentCatalog). Puan oyuncuya hiç gösterilmez.
    //
    // Tempo birimi: 1 tempo = saatte 1000 puan. Yol'da 1 puan = 1 m olduğu için Yol'un km/sa hızı
    // tempoya birebir eşittir (60 seyir, 10 rölanti, ±30 çentik başına, 220 / -60 sınırlar).
    //
    // Aylık skor Yol'daki "ayın en uzak noktası" kuralının genellemesi: ay içi konum (eksiye inmez),
    // ay rekoru = o konumun en yükseği (geri gidip tekrar ilerlemek skoru iki kez saymaz). Ömür boyu
    // puan = konumdaki her ileri adımın toplamı (ortam kilitleri bundan açılır).
    [DefaultExecutionOrder(-200)]
    public class ScoreEngine : MonoBehaviour
    {
        public const double PointsPerPaceHour = 1000.0;

        // 3. iterasyon hız modeli (2026-07-10) + 2026-10-10 aktiflik kararı; değerler RoadDrive'dan.
        [Header("Tempo modeli")]
        [SerializeField] float cruisePace = 60f;
        [SerializeField] float idleCruisePace = 10f;
        [SerializeField] float activeWindowSeconds = 180f;
        [SerializeField] float boostPerNotch = 30f;
        [SerializeField] float maxPace = 220f;
        [SerializeField] float minPace = -60f;
        [SerializeField] float returnToCruiseSeconds = 2f;

        [Header("Kalıcılık")]
        [SerializeField] float autosaveIntervalSeconds = 10f;

        double lastScrollRealtime = double.NegativeInfinity; // açılışta rölantide başlar
        float autosaveTimer;
        string raceMonth = "";

        public static ScoreEngine Instance { get; private set; }

        public float Pace { get; private set; }
        public float CruisePace => cruisePace;
        public bool IsUserActive => Time.realtimeSinceStartupAsDouble - lastScrollRealtime < activeWindowSeconds;
        public float TargetPace => IsUserActive ? cruisePace : idleCruisePace;

        // Bu karenin scroll çentikleri (işaretli; enjekte olaylar GlobalScrollSource'ta zaten ayrılır).
        public float FrameNotches { get; private set; }
        // Bu karede tempodan doğan işaretli puan (kırpılmamış). Ortamlar kendi konumlarını bununla sürer.
        public double FrameRawPoints { get; private set; }
        // Ay konumunun bu karedeki gerçek artışı (≥0): ömür boyu puan ve günlük/haftalık hedefler.
        public double FrameGainPoints { get; private set; }

        public double MonthPoints { get; private set; }
        public double MonthBestPoints { get; private set; }
        public double LifetimePoints { get; private set; }
        public double BestMonthPoints { get; private set; }
        public string BestMonth { get; private set; } = "";

        public event Action OnStep; // scroll olan her karede

        void Awake()
        {
            Instance = this;
            var data = SaveService.Load(); // eski km kaydı burada (bir kez) puana taşınır
            string month = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            raceMonth = month;
            if (data.pointsRaceMonth == month)
            {
                MonthPoints = data.monthPoints;
                MonthBestPoints = Math.Max(data.monthBestPoints, data.monthPoints);
            }
            LifetimePoints = data.lifetimePoints;
            BestMonthPoints = data.bestMonthPoints;
            BestMonth = data.bestPointsMonth ?? "";
            Pace = idleCruisePace;
        }

        void OnDestroy()
        {
            Save();
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() => Save();

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var scrollSource = GlobalScrollSource.Instance;
            float notches = scrollSource != null ? scrollSource.FrameNotchesSigned : 0f;
            FrameNotches = 0f;
            FrameRawPoints = 0;
            FrameGainPoints = 0;
            // Duraklatılmışken (timeScale=0) scroll tempoya yazılmaz — devam edince sıçrama olmasın.
            if (dt <= 0f) return;
            FrameNotches = notches;

            // Yön fark etmez: aşağı scroll da kullanıcının başında olduğunu gösterir.
            if (Mathf.Abs(notches) > 0.0001f)
            {
                lastScrollRealtime = Time.realtimeSinceStartupAsDouble;
                Pace = Mathf.Clamp(Pace + notches * boostPerNotch, minPace, maxPace);
                OnStep?.Invoke();
            }

            // Scroll bırakılınca tempo üstel olarak hedef seyre döner (aktifken seyir, değilse rölanti).
            float returnLerp = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, returnToCruiseSeconds));
            Pace = Mathf.Lerp(Pace, TargetPace, returnLerp);

            FrameRawPoints = Pace * (double)dt / 3600.0 * PointsPerPaceHour;
            double next = Math.Max(0.0, MonthPoints + FrameRawPoints);
            FrameGainPoints = Math.Max(0.0, next - MonthPoints);
            MonthPoints = next;
            MonthBestPoints = Math.Max(MonthBestPoints, MonthPoints);
            LifetimePoints += FrameGainPoints;

            autosaveTimer += dt;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        void Save()
        {
            if (MonthBestPoints > BestMonthPoints)
            {
                BestMonthPoints = MonthBestPoints;
                BestMonth = raceMonth;
            }
            SaveService.SavePoints(raceMonth, MonthPoints, MonthBestPoints, LifetimePoints, BestMonthPoints, BestMonth);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: puan eklemek (ör. ortam kilidi, Olta döngüsü). Release build'de derlenmez.
        public void DebugAddPoints(double points)
        {
            MonthPoints = Math.Max(0.0, MonthPoints + points);
            MonthBestPoints = Math.Max(MonthBestPoints, MonthPoints);
            if (points > 0) LifetimePoints += points;
        }

        // YALNIZ test: tempoyu bir süre sabitle (scroll'suz Play testi). <0 = kapat.
        [NonSerialized] public float DebugHoldPace = -999f;

        void LateUpdate()
        {
            if (DebugHoldPace > -999f)
            {
                Pace = DebugHoldPace;
                lastScrollRealtime = Time.realtimeSinceStartupAsDouble;
            }
        }
#endif
    }
}
