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

        // 3. iterasyon hız modeli (2026-07-10): araç her zaman seyir hızında kendi kendine ilerler,
        // scroll yukarı/aşağı hızı anlık artırır/azaltır, bırakınca üstel olarak seyire döner.
        // (1. iterasyon: sıfıra sönümlenen momentum — reddedildi; 2. iterasyon: sabit 100m adım —
        // kullanıcı isteğiyle bu modele çevrildi. Eksi hız = geri gitme, minSpeedKmh ile sınırlı.)
        [Header("Hız Modeli")]
        [SerializeField] float cruiseSpeedKmh = 60f;
        [SerializeField] float boostPerNotchKmh = 30f;
        [SerializeField] float maxSpeedKmh = 220f;
        [SerializeField] float minSpeedKmh = -60f;
        [SerializeField] float returnToCruiseSeconds = 2f; // seyire dönüş zaman sabiti (üstel)

        [Header("Offline İlerleme")]
        [SerializeField, Range(0f, 1f)] float idleSpeedFraction = 0.15f;
        [SerializeField] float maxOfflineHours = 8f;
        [SerializeField] float maxOfflineKmPerDay = 50f;

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
        public float CruiseSpeedKmh => cruiseSpeedKmh;

        // Şehrin ötesi UI'da hiç belirtilmez, kasıtlı olarak belgelenmemiş/organik keşif.
        public bool InSecretZone => distanceKm > cityDistanceKm;

        // Yükleme anında uygulanan offline ilerleme (karşılama bildirimi için; 0 = yok).
        public double OfflineGrantedKm { get; private set; }

        public event Action OnDirectionChanged;
        public event Action OnEnteredSecretZone;
        public event Action OnStep; // scroll olan her karede (görsel/ses geri bildirimi için)
        public event Action OnReachedCity; // oyun sırasında şehre İLK ulaşmada (yükleme anında zaten ulaşılmışsa tetiklenmez)

        void Awake()
        {
            // Kayıt Awake'te yüklenir ki diğer bileşenler Start'ta doğru mesafeyi görsün.
            // (SteamManager BeforeSceneLoad'da kurulduğu için cloud birleştirmesi de burada olur.)
            LoadPersistedProgress();
            ApplyPendingOfflineProgress();
            CurrentSpeedKmh = cruiseSpeedKmh;
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

        void ApplyPendingOfflineProgress()
        {
            var data = SaveService.Load();
            if (!data.hasLastClose) return;

            double elapsedSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.lastCloseUnixSeconds;
            double cappedSeconds = Math.Clamp(elapsedSeconds, 0, maxOfflineHours * 3600.0);
            if (cappedSeconds > 0)
            {
                ApplyOfflineProgress(cappedSeconds);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Scroll = anlık hız boost'u (yukarı +, aşağı -; eksiye inerse geri gidilir).
            // Kaynak sistem geneli: oyun odakta değilken yapılan scroll da sayılır (GlobalScrollSource).
            // Duraklatılmışken (timeScale=0) scroll hıza yazılmaz — devam edince sıçrama olmasın.
            var scrollSource = GlobalScrollSource.Instance;
            float scrollTicks = scrollSource != null ? scrollSource.FrameNotchesSigned : 0f;
            if (dt <= 0f) return;

            if (Mathf.Abs(scrollTicks) > 0.0001f)
            {
                CurrentSpeedKmh = Mathf.Clamp(CurrentSpeedKmh + scrollTicks * boostPerNotchKmh, minSpeedKmh, maxSpeedKmh);
                OnStep?.Invoke();
            }

            // Scroll bırakılınca hız üstel olarak seyir hızına döner (idle çekirdeği: araç hep akar).
            float returnLerp = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, returnToCruiseSeconds));
            CurrentSpeedKmh = Mathf.Lerp(CurrentSpeedKmh, cruiseSpeedKmh, returnLerp);

            distanceKm = Math.Max(0.0, distanceKm + CurrentSpeedKmh * (double)dt / 3600.0);
            maxDistanceKm = Math.Max(maxDistanceKm, distanceKm);

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

        public void ApplyOfflineProgress(double offlineSeconds)
        {
            // Ne kadar sık aç/kapa yapılırsa yapılsın günlük toplam idle kazancı sabit kalsın diye
            // takvim günü başına kredilenen km ayrıca takip ediliyor (gap-başına tavan bunu tek başına önlemiyordu).
            var data = SaveService.Load();
            string todayKey = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
            float creditedToday = data.offlineCreditDate == todayKey ? data.offlineCreditKmToday : 0f;

            float rawIdleKm = (float)(cruiseSpeedKmh * idleSpeedFraction * (offlineSeconds / 3600.0));
            float grantedKm = Mathf.Min(rawIdleKm, Mathf.Max(0f, maxOfflineKmPerDay - creditedToday));

            distanceKm += grantedKm;
            maxDistanceKm = Math.Max(maxDistanceKm, distanceKm);
            OfflineGrantedKm = grantedKm;

            SaveService.SaveOfflineCredit(todayKey, creditedToday + grantedKm);
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
