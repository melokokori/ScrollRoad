using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ekran üstü bilgi katmanı (normal pencere modu): hız, mesafe, ay rekoru; altta şehre ilerleme
    // çizgisi (dolgu = anlık konum, işaret = bu ayki en uzak nokta / skor), bölge + sonraki durak;
    // sağ üstte günlük/haftalık hedefler ve seri; gizli bölge fısıltısı.
    // Masaüstü arkadaşı modunda tamamen gizlenir.
    // Yazı rengi gökyüzünün parlaklığına göre koyu/açık seçilir (gizli bölgede gökyüzü kararıyor).
    public class HudController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] RoadSceneView view;
        [SerializeField] SecretZoneView secretZone;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject hudRoot;
        [SerializeField] Text speedText;
        [SerializeField] Text distanceText;
        [SerializeField] Text recordText;
        [SerializeField] Text cityText;
        [SerializeField] Text nextStopText;
        [SerializeField] GoalTracker goals;
        [SerializeField] Text goalsText;
        [SerializeField] Text whisperText;
        [SerializeField] Image progressTrack;
        [SerializeField] RectTransform progressFill;
        [SerializeField] RectTransform maxMarker;
        [SerializeField] Graphic[] inkGraphics; // gökyüzüne göre renk değiştiren ikincil öğeler

        static readonly Color DarkInk = new Color(0.16f, 0.19f, 0.24f);
        static readonly Color LightInk = new Color(0.93f, 0.91f, 0.97f);

        void Update()
        {
            bool companion = companionWindow != null && companionWindow.IsCompanionMode;
            if (hudRoot.activeSelf == companion) hudRoot.SetActive(!companion);
            if (companion) return;

            var sky = view != null ? view.CurrentSkyColor : Color.white;
            float lum = sky.r * 0.3f + sky.g * 0.59f + sky.b * 0.11f;
            var ink = lum > 0.5f ? DarkInk : LightInk;

            speedText.text = $"{drive.CurrentSpeedKmh:0} km/h";
            distanceText.text = Fmt.Km(drive.CurrentDistanceKmPrecise);
            recordText.text = "Month best: " + Fmt.Km(drive.MaxDistanceKmPrecise);

            // Şehrin ötesi UI'da asla anılmaz: ulaşıldıktan sonra yalnız "ulaşıldı" yazar.
            double remaining = drive.CityDistanceKm - drive.CurrentDistanceKmPrecise;
            cityText.text = drive.ReachedCity ? "City reached" : Fmt.Km(System.Math.Max(0, remaining), 0) + " to the city";

            nextStopText.text = NextStopLine();
            goalsText.text = GoalsLines();

            speedText.color = distanceText.color = recordText.color = cityText.color = ink;
            nextStopText.color = goalsText.color = ink;
            if (inkGraphics != null)
            {
                foreach (var g in inkGraphics) if (g != null) g.color = new Color(ink.r, ink.g, ink.b, g.color.a);
            }
            progressTrack.color = new Color(ink.r, ink.g, ink.b, 0.18f);

            float progress = Mathf.Clamp01((float)(drive.CurrentDistanceKmPrecise / drive.CityDistanceKm));
            progressFill.anchorMax = new Vector2(progress, 1f);
            float maxProgress = Mathf.Clamp01((float)(drive.MaxDistanceKmPrecise / drive.CityDistanceKm));
            maxMarker.anchorMin = new Vector2(maxProgress, 0f);
            maxMarker.anchorMax = new Vector2(maxProgress, 1f);

            if (secretZone != null)
            {
                whisperText.text = secretZone.CurrentText;
                var w = SecretZoneView.WhisperColor;
                whisperText.color = new Color(w.r, w.g, w.b, secretZone.CurrentAlpha);
            }
        }

        // "Pine Forest  ·  next: Pinecrest 12.4 km". Şehrin ötesi asla anılmaz (satır boş kalır).
        string NextStopLine()
        {
            double d = drive.CurrentDistanceKmPrecise;
            if (drive.InSecretZone) return "";
            string region = RouteMap.Regions[RouteMap.RegionIndexAt(d)].Name;
            int next = RouteMap.NextIndexAfter(d);
            if (next < 0 || RouteMap.Stops[next].Kind == RouteMap.StopKind.City) return region;
            var stop = RouteMap.Stops[next];
            return $"{region}  ·  next: {stop.Name} {Fmt.Km(stop.Km - d)}";
        }

        string GoalsLines()
        {
            if (goals == null) return "";
            string dayKm = goals.IsDone(GoalTracker.Goal.DailyDistance)
                ? $"<b>{goals.DailyKmGoal:0} km done</b>" : $"{Fmt.Int((long)goals.DayKm)} / {goals.DailyKmGoal:0} km";
            string dayTime = goals.IsDone(GoalTracker.Goal.DailyTime)
                ? $"<b>{goals.DailyMinutesGoal:0} min done</b>" : $"{goals.DayMinutes:0} / {goals.DailyMinutesGoal:0} min";
            string weekKm = goals.IsDone(GoalTracker.Goal.WeeklyDistance)
                ? $"<b>{goals.WeeklyKmGoal:0} km done</b>" : $"{Fmt.Int((long)goals.WeekKm)} / {goals.WeeklyKmGoal:0} km";
            string weekTime = goals.IsDone(GoalTracker.Goal.WeeklyTime)
                ? $"<b>{goals.WeeklyHoursGoal:0} h done</b>" : $"{goals.WeekHours:0.0} / {goals.WeeklyHoursGoal:0} h";

            return $"Today   {dayKm}  ·  {dayTime}\n" +
                   $"This week   {weekKm}  ·  {weekTime}\n" +
                   $"Streak: {Fmt.Count(goals.DayStreak, "day")}  ·  {Fmt.Count(goals.WeekStreak, "week")}";
        }
    }
}
