using System;
using UnityEngine;

namespace ScrollRoad
{
    // Ömürlük istatistikler (aylık yarışla sıfırlanmaz): toplam yol, scroll hamlesi, U-dönüşü,
    // şehre ulaşma sayısı, en iyi ay skoru. PlayerPrefs'e doğrudan dokunmaz, her şey SaveService üzerinden.
    public class StatsTracker : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] float autosaveIntervalSeconds = 30f;

        // Kayıttan yüklenen mesafe ömürlük yola sayılmasın diye baz ilk karede alınır.
        bool baselineSet;
        double lastDistanceKm;
        float autosaveTimer;

        public double LifetimeKm { get; private set; }
        public int LifetimeSteps { get; private set; }
        public int LifetimeTurns { get; private set; }
        public int CityReachedCount { get; private set; }
        public double BestMonthKm { get; private set; }
        public string BestMonth { get; private set; } = "";

        void Awake()
        {
            drive.OnStep += HandleStep;
            drive.OnDirectionChanged += HandleTurn;
            drive.OnReachedCity += HandleCityReached;
        }

        void Start()
        {
            var data = SaveService.Load();
            LifetimeKm = data.lifetimeKm;
            LifetimeSteps = data.lifetimeSteps;
            LifetimeTurns = data.lifetimeTurns;
            CityReachedCount = data.cityReachedCount;
            BestMonthKm = data.bestMonthKm;
            BestMonth = data.bestMonth;
        }

        void OnDestroy()
        {
            if (drive == null) return;
            drive.OnStep -= HandleStep;
            drive.OnDirectionChanged -= HandleTurn;
            drive.OnReachedCity -= HandleCityReached;
            SaveStats();
        }

        void OnApplicationQuit() => SaveStats();

        void HandleStep() => LifetimeSteps++;
        void HandleTurn() => LifetimeTurns++;
        void HandleCityReached() => CityReachedCount++;

        void Update()
        {
            if (!baselineSet)
            {
                baselineSet = true;
                lastDistanceKm = drive.CurrentDistanceKmPrecise;
                return;
            }

            // Sadece canlı ileri gidiş ömürlük yola sayılır (geri gidiş saymaz).
            double delta = drive.CurrentDistanceKmPrecise - lastDistanceKm;
            if (delta > 0 && delta < 50) LifetimeKm += delta; // büyük sıçramalar (test/ışınlanma) sayılmaz
            lastDistanceKm = drive.CurrentDistanceKmPrecise;

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                SaveStats();
            }
        }

        void SaveStats()
        {
            if (drive != null && drive.MaxDistanceKmPrecise > BestMonthKm)
            {
                BestMonthKm = drive.MaxDistanceKmPrecise;
                BestMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            }

            SaveService.SaveStats(LifetimeKm, LifetimeSteps, LifetimeTurns, CityReachedCount, BestMonthKm, BestMonth);
        }
    }
}
