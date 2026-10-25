using UnityEngine;

namespace ScrollRoad
{
    // İlerlemeyi (ömür boyu mesafe, bir ayda ulaşılan en uzak mesafe) garaja aktarır ve bir model
    // açılınca duyuru gösterir. Kilit durumu ayrıca kaydedilmez: ilerlemeden türetilir (o da cloud'da
    // birleştiriliyor), böylece başka cihazda da açık gelir.
    public class CarUnlockTracker : MonoBehaviour
    {
        [SerializeField] StatsTracker stats;
        [SerializeField] RoadDrive drive;
        [SerializeField] CarCatalog catalog;
        [SerializeField] NoticeController notice;

        bool[] unlocked;

        // Yol sahnesi aracı "sevinçle" sıçratır (RoadSceneView).
        public static event System.Action VehicleUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => VehicleUnlocked = null;

        void Update()
        {
            CarCatalog.CurrentProgress = new CarCatalog.Progress
            {
                lifetimeKm = stats.LifetimeKm,
                monthBestKm = System.Math.Max(stats.BestMonthKm, drive != null ? drive.MaxDistanceKmPrecise : 0),
            };

            // İlk kare: yüklemede zaten açık olanlar için duyuru yok.
            if (unlocked == null)
            {
                unlocked = new bool[catalog.models.Length];
                for (int i = 0; i < unlocked.Length; i++) unlocked[i] = CarCatalog.IsUnlocked(catalog.models[i]);
                return;
            }

            for (int i = 0; i < unlocked.Length; i++)
            {
                if (unlocked[i] || !CarCatalog.IsUnlocked(catalog.models[i])) continue;
                unlocked[i] = true;
                string name = catalog.models[i].displayName;
                notice.Announce($"New vehicle: {name}", "It's waiting in your garage", $"{name} unlocked");
                VehicleUnlocked?.Invoke();
            }
        }
    }
}
