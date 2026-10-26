using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Olta'nın kart bilgi katmanı (Yol HUD'unun "tabela" diliyle): sol üstte balık kovası (FishingTallyView;
    // bu ayın balığı kovanın üstünde, 2026-10-15'ten önce burada koyu plaka vardı — alanlar artık
    // isteğe bağlı), sağ üstte yeşil tabelada mekân + atışın durumu + yemin kıyıya uzaklığı,
    // sol altta balık defteri özeti. Bugünkü hedef plakası (sağ alt) Yol ile ortak (HudController).
    // Görev çubuğu şeridinde ve Yol'da gizli.
    public class FishingHudController : MonoBehaviour
    {
        [SerializeField] FishingController fishing;
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text placeText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text distanceText;
        [SerializeField] TMP_Text logText;

        // Ekrandaki en uzak atış kaç metre sayılsın (yalnız görsel ölçek).
        const float FarCastMeters = 30f;

        static readonly Color Idle = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color Normal = new Color(0.957f, 0.945f, 0.902f, 0.75f);

        void Update()
        {
            bool show = EnvironmentManager.Current.Id == EnvironmentId.Fishing && fishing != null;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            var p = fishing.Progress;
            if (totalText != null) totalText.SetText(EnvironmentCatalog.Fishing.Format(p.monthFish * EnvironmentCatalog.FishIntervalPoints));

            var engine = ScoreEngine.Instance;
            bool active = engine != null && engine.IsUserActive;
            if (statusText != null)
            {
                if (fishing.ReelDirection < 0f)
                {
                    statusText.text = L.T("LETTING LINE OUT");
                    statusText.color = Normal;
                }
                else if (!active)
                {
                    statusText.text = L.T("IDLE · SLOW REEL");
                    statusText.color = Idle;
                }
                else
                {
                    statusText.text = L.T("REELING");
                    statusText.color = Normal;
                }
            }

            placeText.SetText(L.Upper(L.T(fishing.Spot.Name)));
            bool showDistance = false;
            switch (fishing.CurrentPhase)
            {
                case FishingController.Phase.Casting:
                    phaseText.SetText(L.T("Casting..."));
                    break;
                case FishingController.Phase.Retrieving:
                    phaseText.SetText(L.T("Retrieving"));
                    showDistance = true;
                    break;
                case FishingController.Phase.Hooked:
                    phaseText.SetText(L.T("Fish on!"));
                    showDistance = true;
                    break;
                default:
                    var last = fishing.LastFish;
                    phaseText.SetText(last != null ? L.T(last.Name) + "  " + L.F("{0:0.00} kg", fishing.LastKg) : L.T("Landed"));
                    break;
            }
            distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                float meters = fishing.CastDistance * (1f - fishing.Reeled) * FarCastMeters;
                distanceText.SetText("{0} m", Mathf.Max(1, Mathf.RoundToInt(meters)));
            }

            var lastFish = fishing.LastFish;
            string lastLine = lastFish != null ? $"  ·  <alpha=#99>{L.T("LAST")}<alpha=#FF>  {L.T(lastFish.Name)} " + L.F("{0:0.00} kg", fishing.LastKg) : "";
            logText.text = $"<alpha=#99>{L.T("FISH LOG")}<alpha=#FF>  {fishing.SpeciesLoggedAt(fishing.Spot)} / {fishing.Spot.Species.Length}{lastLine}";
        }
    }
}
