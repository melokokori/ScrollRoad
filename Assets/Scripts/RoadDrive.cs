using System;
using ScrollRoad.InputCapture;
using UnityEngine;

namespace ScrollRoad
{
    public class RoadDrive : MonoBehaviour
    {
        public enum DriveState { Forward, Idle, Reverse }

        // Ana menü gibi RoadDrive olmayan yerlerin de kullandığı varsayılan şehir mesafesi.
        public const float DefaultCityDistanceKm = 18000f;

        [Header("Yol")]
        [SerializeField] float cityDistanceKm = DefaultCityDistanceKm;

        // Hız modeli (seyir + boost, aktiflik penceresi, rölanti) 2026-10-13'te ortak puan motoruna
        // (ScoreEngine) taşındı; Yol yalnız aktif ortamken ilerler ve km = puan × Yol oranı. Yol'da
        // tempo birimi km/sa'e birebir eşit olduğu için görünüş ve davranış değişmedi.

        [Header("Kalıcılık")]
        [SerializeField] float autosaveIntervalSeconds = 10f;

        float autosaveTimer;
        DriveState lastMovingDirection = DriveState.Forward;
        bool hasEnteredSecretZone;
        bool hasNotifiedCity;

        // Mesafe double tutulur: float'ın 18.000 km civarındaki çözünürlüğü (~2 m) kare başına
        // seyir ilerlemesinden (~0,3 m) büyük olduğu için araç float'ta fiilen dururdu.
        double distanceKm;
        double maxDistanceKm;

        public double CurrentDistanceKmPrecise => distanceKm;
        public double MaxDistanceKmPrecise => maxDistanceKm;
        public float CurrentDistanceKm => (float)distanceKm;
        public float MaxDistanceKm => (float)maxDistanceKm;
        public float CurrentSpeedKmh { get; private set; }
        public DriveState State { get; private set; } = DriveState.Idle;
        public float CityDistanceKm => cityDistanceKm;
        public bool ReachedCity => maxDistanceKm >= cityDistanceKm;
        static ScoreEngine Engine => ScoreEngine.Instance;
        static double KmPerPoint => EnvironmentCatalog.Road.UnitsPerPoint;
        static float KmhPerPace => (float)(ScoreEngine.PointsPerPaceHour * KmPerPoint);

        public float CruiseSpeedKmh => Engine != null ? Engine.CruisePace * KmhPerPace : 60f;

        // Son gerçek scroll'dan bu yana 3 dk geçmediyse seyir hızında, yoksa rölantide (ScoreEngine).
        public bool IsUserActive => Engine != null && Engine.IsUserActive;
        public float TargetCruiseSpeedKmh => Engine != null ? Engine.TargetPace * KmhPerPace : 0f;

        // Yol yalnız seçili ortamken ilerler (diğer ortamlarda araç bekler, ses/olay yok).
        public static bool IsActiveEnvironment => EnvironmentManager.Current.Id == EnvironmentId.Road;

        // Şehrin ötesi UI'da hiç belirtilmez, kasıtlı olarak belgelenmemiş/organik keşif.
        public bool InSecretZone => distanceKm > cityDistanceKm;

        public event Action OnDirectionChanged;
        public event Action OnEnteredSecretZone;
        public event Action OnStep; // scroll olan her karede (görsel/ses geri bildirimi için)
        public event Action OnReachedCity; // oyun sırasında şehre İLK ulaşmada (yükleme anında zaten ulaşılmışsa tetiklenmez)

        void Awake()
        {
            // Kayıt Awake'te yüklenir ki diğer bileşenler Start'ta doğru mesafeyi görsün.
            // (SteamManager BeforeSceneLoad'da kurulduğu için cloud birleştirmesi de burada olur.)
            // Oyun kapalıyken ilerleme yok (2026-10-10 kararı).
            LoadPersistedProgress();
            CurrentSpeedKmh = 0f;
        }

        void OnApplicationQuit() => SaveAll();

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveAll();
        }

        void OnDestroy() => SaveAll();

        void SaveAll()
        {
            SaveService.SaveCloseTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            SaveProgress();
        }

        // Skor "o ay içinde başlangıçtan ulaşılan en uzak mesafe" olduğu için yarış her takvim
        // ayı başında sıfırdan başlıyor — Steam leaderboard'un aylık sıfırlanmasıyla tutarlı.
        void LoadPersistedProgress()
        {
            var data = SaveService.Load();
            string currentMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");

            if (data.raceMonth == currentMonth)
            {
                distanceKm = data.currentDistanceKm;
                maxDistanceKm = data.maxDistanceKm;
            }
            else
            {
                distanceKm = 0;
                maxDistanceKm = 0;
            }

            hasNotifiedCity = ReachedCity; // kayıtta zaten ulaşılmışsa kutlama yeniden çalmasın
            hasEnteredSecretZone = InSecretZone;
        }

        void SaveProgress()
        {
            string currentMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            SaveService.SaveProgress(currentMonth, distanceKm, maxDistanceKm);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var engine = Engine;
            if (engine == null || dt <= 0f) return;

            if (IsActiveEnvironment)
            {
                // Scroll = anlık hız boost'u (aşağı +, yukarı -; eksiye inerse geri gidilir); tempo ve
                // seyre dönüş ScoreEngine'de. Kaynak sistem geneli (GlobalScrollSource).
                if (Mathf.Abs(engine.FrameNotches) > 0.0001f) OnStep?.Invoke();
                CurrentSpeedKmh = engine.Pace * KmhPerPace;
                distanceKm = Math.Max(0.0, distanceKm + engine.FrameRawPoints * KmPerPoint);
                maxDistanceKm = Math.Max(maxDistanceKm, distanceKm);
            }
            else
            {
                CurrentSpeedKmh = 0f;
            }

            // ±1 km/h bandı: seyire dönüş sırasında işaret değişimi anındaki titremeyi önler.
            var direction = CurrentSpeedKmh > 1f ? DriveState.Forward
                : CurrentSpeedKmh < -1f ? DriveState.Reverse
                : DriveState.Idle;
            State = direction;
            if (direction != DriveState.Idle && direction != lastMovingDirection)
            {
                lastMovingDirection = direction;
                OnDirectionChanged?.Invoke();
            }

            CheckThresholds();

            autosaveTimer += dt;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                SaveProgress();
            }
        }

        // Araç scroll olmadan da ilerlediği için eşik kontrolleri her kare yapılır.
        void CheckThresholds()
        {
            if (!hasEnteredSecretZone && InSecretZone)
            {
                hasEnteredSecretZone = true;
                OnEnteredSecretZone?.Invoke();
            }

            if (!hasNotifiedCity && ReachedCity)
            {
                hasNotifiedCity = true;
                OnReachedCity?.Invoke();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ geliştirme/test: şehir ve gizli bölge olaylarını uzun sürüş beklemeden denemek için.
        // Release build'de derlenmez.
        public void DebugSetDistanceKm(double km)
        {
            distanceKm = Math.Max(0.0, km);
            maxDistanceKm = Math.Max(maxDistanceKm, distanceKm);
        }

        public void DebugResetMonth()
        {
            distanceKm = 0;
            maxDistanceKm = 0;
            hasNotifiedCity = false;
            hasEnteredSecretZone = false;
        }
#endif
    }
}
