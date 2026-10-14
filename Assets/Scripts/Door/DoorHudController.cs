using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Döner kapının kart bilgi katmanı (Olta HUD'unun yerleşimiyle): sol üstte koyu plakada içerideki
    // ziyaretçi sayısı (bu ay) + kapının durumu, sağ üstte yeşil tabelada mekân + sıradaki ziyaretçi
    // (kaldırımda nadir bir misafir yaklaşıyorsa onun adı), sol altta misafir defteri özeti. Bugünkü
    // hedef plakası (sağ alt) Yol ile ortak (HudController). Görev çubuğu şeridinde ve diğer ortamlarda gizli.
    public class DoorHudController : MonoBehaviour
    {
        [SerializeField] DoorController door;
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text totalText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text placeText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text guestText;
        [SerializeField] TMP_Text logText;

        // Tabelada "geliyor" denecek kadar yakın misafir: kaldırımda kaç kişi geriden.
        const int LookAhead = 5;

        static readonly Color Idle = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color Normal = new Color(0.957f, 0.945f, 0.902f, 0.75f);

        void Update()
        {
            bool show = EnvironmentManager.Current.Id == EnvironmentId.Door && door != null;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            totalText.SetText(EnvironmentCatalog.Door.Format(door.VisitorsInside * EnvironmentCatalog.VisitorIntervalPoints));

            var engine = ScoreEngine.Instance;
            bool active = engine != null && engine.IsUserActive;
            if (door.TurnDirection < 0f)
            {
                statusText.text = "BACKING OUT";
                statusText.color = Normal;
            }
            else if (!active)
            {
                statusText.text = "IDLE · SLOW TURN";
                statusText.color = Idle;
            }
            else
            {
                statusText.text = "TURNING";
                statusText.color = Normal;
            }

            placeText.SetText(door.Venue.Name.ToUpperInvariant());
            var coming = UpcomingGuest();
            guestText.gameObject.SetActive(coming != null);
            if (coming != null)
            {
                phaseText.SetText("Here comes");
                guestText.SetText(coming.Name);
            }
            else
            {
                phaseText.SetText("Next: #" + Fmt.Int(door.VisitorsInside + 1));
            }

            var last = door.LastGuest;
            string lastLine = last != null ? $"  ·  <alpha=#99>LAST<alpha=#FF>  {last.Name}" : "";
            logText.text = $"<alpha=#99>GUEST BOOK<alpha=#FF>  {door.GuestsLoggedAt(door.Venue)} / {door.Venue.Guests.Length}{lastLine}";
        }

        // Kaldırımda kapıya yaklaşan ilk nadir misafir (yoksa null).
        VisitorType UpcomingGuest()
        {
            long next = door.VisitorsInside + 1;
            for (long i = next; i < next + LookAhead; i++)
            {
                var v = door.Identify(i);
                if (v.IsGuest) return v.Type;
            }
            return null;
        }
    }
}
