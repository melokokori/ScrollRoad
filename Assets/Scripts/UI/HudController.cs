using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Kartın bilgi katmanı ("tabela" stili): sol üstteki hız göstergesi ve kilometre sayacı ayrı
    // (GaugeView, skinli kadran); burada sağ üstte yeşil karayolu tabelası (bölge + sonraki
    // durak + kalan mesafe); altta koyu plakalarda şehre ilerleme ve bugünkü hedefler; gizli bölge
    // fısıltısı. Haftalık hedef, seri ve ay rekoru ≡ çekmecesinde. Görev çubuğu şeridi modunda gizli.
    public class HudController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] SecretZoneView secretZone;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject hudRoot;
        [SerializeField] GameObject signRoot;
        [SerializeField] TMP_Text regionText;
        [SerializeField] TMP_Text townText;
        [SerializeField] TMP_Text stopDistanceText;
        [SerializeField] TMP_Text cityText;
        [SerializeField] GoalTracker goals;
        [SerializeField] TMP_Text goalsText;
        [SerializeField] TMP_Text whisperText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] RectTransform maxMarker;

        void Update()
        {
            bool companion = companionWindow != null && companionWindow.IsCompanionMode;
            if (hudRoot.activeSelf == companion) hudRoot.SetActive(!companion);
            if (companion) return;

            UpdateSign();

            // Şehrin ötesi UI'da asla anılmaz: ulaşıldıktan sonra yalnız "ulaşıldı" yazar.
            double remaining = drive.CityDistanceKm - drive.CurrentDistanceKmPrecise;
            cityText.text = drive.ReachedCity
                ? "THE CITY  <alpha=#99>REACHED"
                : Fmt.Km(System.Math.Max(0, remaining), 0) + "  <alpha=#99>TO THE CITY";
            goalsText.text = TodayLine();

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

        // Tabela: üstte bölge, altta sonraki durak + kalan mesafe. Sonraki durak şehirse (ya da yoksa)
        // yalnız bölge adı büyük yazılır. Gizli bölgede tabela yok.
        void UpdateSign()
        {
            double d = drive.CurrentDistanceKmPrecise;
            bool show = !drive.InSecretZone;
            if (signRoot.activeSelf != show) signRoot.SetActive(show);
            if (!show) return;

            string region = RouteMap.Regions[RouteMap.RegionIndexAt(d)].Name;
            int next = RouteMap.NextIndexAfter(d);
            bool hasStop = next >= 0 && RouteMap.Stops[next].Kind != RouteMap.StopKind.City;
            regionText.gameObject.SetActive(hasStop);
            stopDistanceText.gameObject.SetActive(hasStop);
            if (hasStop)
            {
                var stop = RouteMap.Stops[next];
                regionText.SetText(region.ToUpperInvariant());
                townText.SetText(stop.Name);
                stopDistanceText.SetText(Fmt.Km(stop.Km - d));
            }
            else
            {
                townText.SetText(region);
            }
        }

        // "TODAY  4 / 80 km  ·  3 / 60 min" (tamamlananlar "done").
        string TodayLine()
        {
            if (goals == null) return "";
            string dayKm = goals.IsDone(GoalTracker.Goal.DailyDistance)
                ? $"{goals.DailyKmGoal:0} km done" : $"{Fmt.Int((long)goals.DayKm)} / {goals.DailyKmGoal:0} km";
            string dayTime = goals.IsDone(GoalTracker.Goal.DailyTime)
                ? $"{goals.DailyMinutesGoal:0} min done" : $"{goals.DayMinutes:0} / {goals.DailyMinutesGoal:0} min";
            return $"<alpha=#99>TODAY<alpha=#FF>  {dayKm}  ·  {dayTime}";
        }
    }
}
