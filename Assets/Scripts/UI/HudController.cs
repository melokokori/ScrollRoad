using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Kartın bilgi katmanı ("tabela" stili): sol üstteki hız göstergesi ve kilometre sayacı ayrı
    // (GaugeView, skinli kadran); burada sağ üstte yeşil karayolu tabelası (bölge + sonraki
    // durak + kalan mesafe); altta koyu plakalarda şehre ilerleme ve bugünkü siparişler + seri; gizli bölge
    // fısıltısı. Haftalık sipariş ve ay rekoru ≡ çekmecesinde. Görev çubuğu şeridi modunda gizli.
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
        [SerializeField] TMP_Text goalsText;
        [SerializeField] TMP_Text whisperText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] RectTransform maxMarker;
        [SerializeField] GameObject[] roadOnly; // kadran, tabela, şehir plakası, fısıltı: Olta'da gizli

        void Update()
        {
            bool companion = companionWindow != null && companionWindow.IsCompanionMode;
            if (hudRoot.activeSelf == companion) hudRoot.SetActive(!companion);
            if (companion) return;

            // Bugünkü sipariş plakası ortak; geri kalanı Yol'a özel.
            goalsText.text = TodayLine();
            bool road = RoadDrive.IsActiveEnvironment;
            foreach (var go in roadOnly)
            {
                if (go != null && go.activeSelf != road) go.SetActive(road);
            }
            if (!road) return;

            UpdateSign();

            // Şehrin ötesi UI'da asla anılmaz: ulaşıldıktan sonra yalnız "ulaşıldı" yazar.
            double remaining = drive.CityDistanceKm - drive.CurrentDistanceKmPrecise;
            cityText.text = drive.ReachedCity
                ? "THE CITY  <alpha=#99>REACHED"
                : Fmt.Km(System.Math.Max(0, remaining), 0) + "  <alpha=#99>TO THE CITY";

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

        // "TODAY  ORDERS 1 / 3  ·  STREAK 4 DAYS" (seri yoksa yalnız siparişler).
        static string TodayLine()
        {
            var orders = OrdersController.Instance;
            if (orders == null || orders.DailyCount == 0) return "";
            string line = orders.DailyDone >= orders.DailyCount
                ? "<alpha=#99>TODAY<alpha=#FF>  ALL ORDERS DONE"
                : $"<alpha=#99>TODAY<alpha=#FF>  ORDERS {orders.DailyDone} / {orders.DailyCount}";
            int streak = orders.DayStreak;
            return streak > 0 ? $"{line}  ·  STREAK {Fmt.Count(streak, "day").ToUpperInvariant()}" : line;
        }
    }
}
