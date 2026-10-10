using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ScrollRoad
{
    // Aylık skorun Steam'e periyodik gönderimi. Sadece ay rekoru arttıysa gönderir
    // (gereksiz istek yok). Steam yoksa sessizce hiçbir şey yapmaz.
    public class LeaderboardSync : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] float syncIntervalSeconds = 60f;

        float timer;
        double lastSubmittedKm = -1;
        bool submitting;

        public string LastStatus { get; private set; } = "—";

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < syncIntervalSeconds) return;
            timer = 0f;
            _ = SyncNow();
        }

        void OnApplicationQuit() => _ = SyncNow();

        public async Task SyncNow()
        {
            if (drive == null || submitting) return;
            if (!SteamManager.IsInitialized)
            {
                LastStatus = "Steam bağlı değil";
                return;
            }

            double km = drive.MaxDistanceKmPrecise;
            if (Math.Abs(km - lastSubmittedKm) < 0.001) return;

            submitting = true;
            try
            {
                bool ok = await ScrollRoadLeaderboard.SubmitDistanceKmAsync(km);
                if (ok) lastSubmittedKm = km;
                LastStatus = ok ? $"Gönderildi: {km:0.00} km" : "Gönderim başarısız";
            }
            catch (Exception e)
            {
                LastStatus = "Gönderim başarısız";
                Debug.LogWarning($"Leaderboard gönderimi başarısız: {e.Message}");
            }
            finally
            {
                submitting = false;
            }
        }
    }
}
