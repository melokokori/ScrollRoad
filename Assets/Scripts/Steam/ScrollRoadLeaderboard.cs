using System;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;

namespace ScrollRoad
{
    // Aylık leaderboard: her takvim ayı (UTC) yeni bir tablo — "ScrollRoad_Global_yyyy-MM" ve
    // ülke tablosu. Skor = o ay ulaşılan en uzak mesafe (metre, KeepBest).
    public static class ScrollRoadLeaderboard
    {
        public static string GlobalLeaderboardName(DateTime? utcMonth = null)
        {
            return $"ScrollRoad_Global_{(utcMonth ?? DateTime.UtcNow):yyyy-MM}";
        }

        public static string CountryLeaderboardName(string countryCode, DateTime? utcMonth = null)
        {
            return $"ScrollRoad_{countryCode}_{(utcMonth ?? DateTime.UtcNow):yyyy-MM}";
        }

        // Skor "en uzak mesafe" (high-water mark) — anlık konum değil, RoadDrive.MaxDistanceKm.
        // Facepunch SubmitScoreAsync Steam tarafında KeepBest ile yükler (düşük skor eskisini ezmez).
        public static async Task<bool> SubmitDistanceKmAsync(double maxDistanceKm)
        {
            if (!SteamManager.IsInitialized) return false;

            int scoreMeters = (int)Math.Min(int.MaxValue, maxDistanceKm * 1000.0);

            bool globalOk = await SubmitToLeaderboardAsync(GlobalLeaderboardName(), scoreMeters);

            string country = SteamUtils.IpCountry;
            bool countryOk = true;
            if (!string.IsNullOrEmpty(country))
            {
                countryOk = await SubmitToLeaderboardAsync(CountryLeaderboardName(country), scoreMeters);
            }

            return globalOk && countryOk;
        }

        static async Task<bool> SubmitToLeaderboardAsync(string name, int scoreMeters)
        {
            var board = await SteamUserStats.FindOrCreateLeaderboardAsync(name, LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
            if (board == null) return false;

            var result = await board.Value.SubmitScoreAsync(scoreMeters);
            return result != null;
        }

        public static async Task<LeaderboardEntry[]> GetTopGlobalAsync(int count = 10, DateTime? utcMonth = null)
        {
            if (!SteamManager.IsInitialized) return null;
            var board = await SteamUserStats.FindOrCreateLeaderboardAsync(GlobalLeaderboardName(utcMonth), LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
            if (board == null) return null;

            return await board.Value.GetScoresAsync(count);
        }

        // Ay bitince o ayın tablosu artık değişmez — "geçen ayın açık sonuçları" (reveal)
        // için ek kilit gerekmez, bir önceki ayın adıyla sorgulamak yeterli.
        public static Task<LeaderboardEntry[]> GetPreviousMonthTopGlobalAsync(int count = 10)
        {
            return GetTopGlobalAsync(count, DateTime.UtcNow.AddMonths(-1));
        }
    }
}
