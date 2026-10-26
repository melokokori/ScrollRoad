using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ScrollRoad
{
    // Aylık skorun (gizli ortak puanın ay rekoru) Steam'e periyodik gönderimi. Sadece ay rekoru
    // arttıysa gönderir (gereksiz istek yok). Steam yoksa sessizce hiçbir şey yapmaz.
    public class LeaderboardSync : MonoBehaviour
    {
        [SerializeField] float syncIntervalSeconds = 60f;

        float timer;
        double lastSubmitted = -1;
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
            var engine = ScoreEngine.Instance;
            if (engine == null || submitting) return;
            if (!SteamManager.IsInitialized)
            {
                LastStatus = L.T("Steam not connected");
                return;
            }

            double points = engine.MonthBestPoints;
            if (Math.Abs(points - lastSubmitted) < 1) return;

            submitting = true;
            try
            {
                bool ok = await ScrollRoadLeaderboard.SubmitPointsAsync(points);
                if (ok) lastSubmitted = points;
                LastStatus = ok ? L.F("Submitted: {0}", EnvironmentManager.Current.Format(points)) : L.T("Submission failed");
            }
            catch (Exception e)
            {
                LastStatus = L.T("Submission failed");
                Debug.LogWarning($"Leaderboard gönderimi başarısız: {e.Message}");
            }
            finally
            {
                submitting = false;
            }
        }
    }
}
