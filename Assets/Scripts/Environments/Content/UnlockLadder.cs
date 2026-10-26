using UnityEngine;

namespace ScrollRoad
{
    // Açılma merdiveni (Ortam Standardı v2, 2026-10-25): her ortamda mekânlar ve skin takımları o ortamın
    // KENDİ seyir saatiyle açılır (yalnız seçili ortam ilerler). Seyir temposu 60 = saatte 60.000 puan.
    // Merdivenler iç içe: skin 2 (1 sa) → mekân 2 (3 sa) → skin 3 (8 sa) → mekân 3 (12 sa) → mekân 4 (25 sa)
    // → skin 4 (40 sa) → mekân 5 (60 sa). Merdivenden uzun listede son basamak tekrarlanır.
    public static class UnlockLadder
    {
        public const double CruisePace = 60.0;
        public const double PointsPerHour = ScoreEngine.PointsPerPaceHour * CruisePace;

        public static readonly float[] VenueHours = { 0f, 3f, 12f, 25f, 60f };
        public static readonly float[] SkinHours = { 0f, 1f, 8f, 40f };

        static double At(float[] ladder, int index) =>
            index <= 0 ? 0.0 : ladder[Mathf.Min(index, ladder.Length - 1)] * PointsPerHour;

        public static double VenuePoints(int index) => At(VenueHours, index);
        public static double SkinPoints(int index) => At(SkinHours, index);

        public static bool IsVenueUnlocked(double lifetimePoints, int index) =>
            index <= 0 || CarCatalog.ReviewUnlockAll || lifetimePoints >= VenuePoints(index);

        public static bool IsSkinUnlocked(double lifetimePoints, int index) =>
            index <= 0 || CarCatalog.ReviewUnlockAll || lifetimePoints >= SkinPoints(index);

        // Kalan puanı seyir süresi olarak yazar: "2 h 15 m", "40 m", "under a minute".
        public static string Remaining(double points)
        {
            double minutes = System.Math.Ceiling(System.Math.Max(0.0, points) / PointsPerHour * 60.0);
            if (minutes < 1) return L.T("under a minute");
            long h = (long)(minutes / 60);
            long m = (long)(minutes % 60);
            if (h == 0) return L.F("{0} m", m);
            return m == 0 ? L.F("{0} h", Fmt.Int(h)) : L.F("{0} h {1} m", Fmt.Int(h), m);
        }
    }
}
