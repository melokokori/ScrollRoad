using System;
using UnityEngine;

namespace ScrollRoad
{
    // Ayın en uzak noktası bir durağı geçince StopReached tetiklenir (her durak ayda bir kez).
    // Kayıttan yüklenen ilerlemedeki duraklar için tetiklenmez. Şehir için de tetiklenmez —
    // onun kendi kutlaması var (RoadDrive.OnReachedCity); yine de pasaportta damga sayılır.
    public class RouteProgress : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;

        int passedCount = -1; // -1 = henüz baz alınmadı (RoadDrive kaydı Awake'te yükler)

        public event Action<RouteMap.Stop> StopReached;

        public int PassedCount => Math.Max(0, passedCount);

        void Update()
        {
            double max = drive.MaxDistanceKmPrecise;
            int passed = RouteMap.CountPassed(max);

            // İlk kare ve ay sıfırlaması (debug dahil): sessizce yeniden baz al.
            if (passedCount < 0 || passed < passedCount)
            {
                passedCount = passed;
                return;
            }

            while (passedCount < passed)
            {
                var stop = RouteMap.Stops[passedCount++];
                if (stop.Kind != RouteMap.StopKind.City) StopReached?.Invoke(stop);
            }
        }
    }
}
