using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Plak tablasının kart bilgi katmanı (Kapı/Rüzgârgülü HUD'unun yerleşimiyle): sol üstte koyu plakada bu ay
    // çalınan parça + durum, sağ üstte yeşil tabelada mekân + çalan plak ve parça sırası, sol altta plak
    // sandığı özeti. Bugünkü hedef plakası (sağ alt) ortak (HudController).
    public class VinylHudController : MonoBehaviour
    {
        [SerializeField] VinylController vinyl;
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text placeText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text nowText;
        [SerializeField] TMP_Text logText;

        static readonly Color Idle = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color Normal = new Color(0.957f, 0.945f, 0.902f, 0.75f);

        void Update()
        {
            bool show = EnvironmentManager.Current.Id == EnvironmentId.Vinyl && vinyl != null;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            if (totalText != null) totalText.SetText(EnvironmentCatalog.Vinyl.Format(vinyl.TracksDone * EnvironmentCatalog.TrackIntervalPoints));

            var engine = ScoreEngine.Instance;
            bool active = engine != null && engine.IsUserActive;
            if (vinyl.SpinDirection < 0f)
            {
                statusText.text = L.T("REWINDING");
                statusText.color = Normal;
            }
            else if (!active)
            {
                statusText.text = L.T("IDLE · SLOW SPIN");
                statusText.color = Idle;
            }
            else
            {
                statusText.text = L.T("PLAYING");
                statusText.color = Normal;
            }

            placeText.SetText(L.Upper(L.T(vinyl.Shop.Name)));
            var rec = vinyl.Current;
            phaseText.SetText(L.F("Track {0}/{1}", vinyl.TrackInRecord, RecordCatalog.TracksPerRecord));
            nowText.SetText(rec.DisplayName);

            var last = vinyl.LastSpecial;
            string lastLine = last != null ? $"  ·  <alpha=#99>{L.T("LAST")}<alpha=#FF>  {L.T(last.Name)}" : "";
            logText.text = $"<alpha=#99>{L.T("RECORD CRATE")}<alpha=#FF>  {vinyl.LoggedAt(vinyl.Shop)} / {vinyl.Shop.Specials.Length}{lastLine}";
        }
    }
}
