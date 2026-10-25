using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Misafir defteri (balık defterinin Döner kapı karşılığı): üstte mekân sekmeleri, altta seçili mekânın
    // 6 nadir misafirinin yuvası. Gelmemiş misafir koyu silüet + "???"; gelince renkli görsel, ad, kaç kez
    // geldiği ve ilk geliş tarihi. Açık mekânın sekmesinde "Go here" kapıyı oraya geçirir; kilitli mekânda
    // yuvalar silüet ve kalan ziyaretçi sayısı yazar. Çekmecedeki koleksiyon satırı Döner kapı'dayken bunu
    // açar (PassportController). Mekânın gizli efsanesi (AVM: Koca Ayak) gelene kadar görünmez; gelince
    // ızgaranın altında altın şerit olarak çıkar ve "x / 6" sayımına girmez.
    public class GuestBookController : MonoBehaviour
    {
        [SerializeField] DoorController door;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Button[] tabButtons;   // VisitorCatalog.Venues sırasıyla
        [SerializeField] TMP_Text[] tabTexts;
        [SerializeField] Button hereButton;
        [SerializeField] TMP_Text hereText;
        [SerializeField] Image[] guestImages;   // VisitorCatalog.AllGuests sırasıyla
        [SerializeField] GameObject[] slotRoots;
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;

        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);
        static readonly Color TabOn = Color.white;
        static readonly Color TabOff = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color TabLocked = new Color(1f, 1f, 1f, 0.22f);

        DoorVenue shown;

        public GameObject Panel => panel;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < tabButtons.Length && i < VisitorCatalog.Venues.Length; i++)
            {
                var venue = VisitorCatalog.Venues[i];
                tabButtons[i].onClick.AddListener(() => Show(venue));
            }
            hereButton.onClick.AddListener(() =>
            {
                if (door != null && door.SelectVenue(shown)) Show(shown);
            });
            panel.SetActive(false);
        }

        public void Open()
        {
            Show(door != null ? door.Venue : VisitorCatalog.Mall);
            panel.SetActive(true);
        }

        void Show(DoorVenue venue)
        {
            shown = venue;
            var p = door != null ? door.Progress : SaveService.LoadDoor();
            bool unlocked = DoorController.IsUnlocked(venue, p);
            bool current = door != null && door.Venue == venue;

            titleText.text = "Guest book · " + venue.Name;
            int logged = 0;
            foreach (var g in venue.Guests) if (p.Find(g.Id) != null) logged++;
            countText.text = unlocked
                ? $"{logged} / {venue.Guests.Length} guests  ·  {Fmt.Int(p.totalVisitors)} visitors in all"
                : $"LOCKED  ·  {UnlockLadder.Remaining(UnlockLadder.VenuePoints(System.Array.IndexOf(VisitorCatalog.Venues, venue)) - DoorController.LifetimePointsOf(p))} at the door to go";

            for (int i = 0; i < tabTexts.Length && i < VisitorCatalog.Venues.Length; i++)
            {
                var v = VisitorCatalog.Venues[i];
                tabTexts[i].text = v.Name;
                tabTexts[i].color = v == venue ? TabOn : DoorController.IsUnlocked(v, p) ? TabOff : TabLocked;
            }

            hereButton.gameObject.SetActive(unlocked);
            hereButton.interactable = !current;
            hereText.text = current ? "YOU ARE HERE" : "GO HERE";

            for (int i = 0; i < VisitorCatalog.AllGuests.Length && i < slotRoots.Length; i++)
            {
                var guest = VisitorCatalog.AllGuests[i];
                bool inVenue = venue.Contains(guest.Id);
                slotRoots[i].SetActive(inVenue);
                if (!inVenue) continue;

                var record = p.Find(guest.Id);
                bool met = record != null;
                if (guest == venue.Legendary)
                {
                    slotRoots[i].SetActive(met);
                    if (!met) continue;
                    nameTexts[i].text = $"LEGENDARY  ·  {guest.Name}";
                    detailTexts[i].text = $"Visited ×{record.count}  ·  <alpha=#99>First visit {record.firstSeen}";
                    continue;
                }
                guestImages[i].color = met ? Color.white : Silhouette;
                nameTexts[i].text = met ? guest.Name : "???";
                detailTexts[i].text = met
                    ? $"Visited ×{record.count}\n<alpha=#88>First visit {record.firstSeen}"
                    : "<alpha=#88>Not seen yet";
            }
        }
    }
}
