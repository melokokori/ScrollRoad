using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Rüzgârgülünün kart bilgi katmanı (Kapı HUD'unun yerleşimiyle): sol üstte koyu plakada bu ay üretilen
    // kWh + türbinin durumu, sağ üstte yeşil tabelada mekân + sıradaki yapı (5 kWh içinde nadir bir yapı
    // varsa onun adı), sol altta şebeke defteri özeti. Bugünkü hedef plakası (sağ alt) ortak (HudController).
    public class WindHudController : MonoBehaviour
    {
        [SerializeField] WindController wind;
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text placeText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text nextText;
        [SerializeField] TMP_Text logText;

        const int LookAhead = 5;

        static readonly Color Idle = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color Normal = new Color(0.957f, 0.945f, 0.902f, 0.75f);

        void Update()
        {
            bool show = EnvironmentManager.Current.Id == EnvironmentId.Wind && wind != null;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            if (totalText != null) totalText.SetText(EnvironmentCatalog.Wind.Format(wind.Connected * EnvironmentCatalog.KwhIntervalPoints));

            var engine = ScoreEngine.Instance;
            bool active = engine != null && engine.IsUserActive;
            if (wind.SpinDirection < 0f)
            {
                statusText.text = L.T("BRAKING");
                statusText.color = Normal;
            }
            else if (!active)
            {
                statusText.text = L.T("IDLE · LIGHT BREEZE");
                statusText.color = Idle;
            }
            else
            {
                statusText.text = L.T("GENERATING");
                statusText.color = Normal;
            }

            placeText.SetText(L.Upper(L.T(wind.Site.Name)));
            var coming = Upcoming();
            nextText.gameObject.SetActive(coming != null);
            if (coming != null)
            {
                phaseText.SetText(L.T("Powering"));
                nextText.SetText(L.T(coming.Name));
            }
            else
            {
                phaseText.SetText(L.F("Next: home #{0}", Fmt.Int(wind.Connected + 1)));
            }

            var last = wind.LastSpecial;
            string lastLine = last != null ? $"  ·  <alpha=#99>{L.T("LAST")}<alpha=#FF>  {L.T(last.Name)}" : "";
            logText.text = $"<alpha=#99>{L.T("GRID LOG")}<alpha=#FF>  {wind.LoggedAt(wind.Site)} / {wind.Site.Specials.Length}{lastLine}";
        }

        // Direğe yaklaşan ilk nadir yapı (yoksa null).
        BuildingType Upcoming()
        {
            long next = wind.Connected + 1;
            for (long i = next; i < next + LookAhead; i++)
            {
                var b = wind.Identify(i);
                if (b.IsSpecial) return b.Type;
            }
            return null;
        }
    }
}
