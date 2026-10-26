using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Balık defteri (pasaportun Olta karşılığı): üstte mekân sekmeleri, altta seçili sekmenin 6 türünün
    // yuvası. Tutulmamış tür koyu silüet + "???"; tutulunca renkli görsel, ad, en büyük kg rekoru, kaç
    // kez tutulduğu ve ilk tutuluş tarihi (damga). Açık mekânın sekmesinde "Fish here" oltayı oraya
    // geçirir; kilitli mekânda yuvalar silüet ve kalan balık sayısı yazar. Çekmecedeki koleksiyon satırı
    // Olta'dayken bunu açar (PassportController). Mekânın gizli efsanesi (Göl: Altın Sazan) tutulana kadar
    // görünmez; tutulunca ızgaranın altında altın şerit olarak çıkar ve "x / 6" sayımına girmez.
    public class FishLogController : MonoBehaviour
    {
        [SerializeField] FishingController fishing;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Button[] tabButtons;   // FishCatalog.Spots sırasıyla
        [SerializeField] TMP_Text[] tabTexts;
        [SerializeField] Button fishHereButton;
        [SerializeField] TMP_Text fishHereText;
        [SerializeField] Image[] fishImages;    // FishCatalog.All sırasıyla (mekân başına 6 yuva art arda)
        [SerializeField] GameObject[] slotRoots; // FishCatalog.All sırasıyla
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;

        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);
        static readonly Color TabOn = Color.white;
        static readonly Color TabOff = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color TabLocked = new Color(1f, 1f, 1f, 0.22f);

        FishingSpot shown;

        public GameObject Panel => panel;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < tabButtons.Length && i < FishCatalog.Spots.Length; i++)
            {
                var spot = FishCatalog.Spots[i];
                tabButtons[i].onClick.AddListener(() => Show(spot));
            }
            fishHereButton.onClick.AddListener(() =>
            {
                if (fishing != null && fishing.SelectSpot(shown)) Show(shown);
            });
            panel.SetActive(false);
        }

        public void Open()
        {
            Show(fishing != null ? fishing.Spot : FishCatalog.Lake);
            panel.SetActive(true);
        }

        void Show(FishingSpot spot)
        {
            shown = spot;
            var p = fishing != null ? fishing.Progress : SaveService.LoadFishing();
            bool unlocked = FishingController.IsUnlocked(spot, p);
            bool current = fishing != null && fishing.Spot == spot;

            titleText.text = L.T("Fish log") + " · " + L.T(spot.Name);
            int logged = 0;
            foreach (var s in spot.Species) if (p.Find(s.Id) != null) logged++;
            countText.text = unlocked
                ? L.F("{0} / {1} species  ·  {2}  ·  {3} caught", logged, spot.Species.Length, L.P(p.totalFish, "{0} fish", "{0} fish"), Fmt.Amount(p.totalKg, 1, L.N("kg")))
                : L.T("LOCKED") + "  ·  " + L.F("{0} of fishing to go", UnlockLadder.Remaining(UnlockLadder.VenuePoints(System.Array.IndexOf(FishCatalog.Spots, spot)) - FishingController.LadderPointsOf(p)));

            for (int i = 0; i < tabTexts.Length && i < FishCatalog.Spots.Length; i++)
            {
                var s = FishCatalog.Spots[i];
                // 5 sekme dar: kilitli mekân yalnız soluk ad (kalan balık sayısı sağ üstte yazar).
                tabTexts[i].text = L.T(s.Name);
                tabTexts[i].color = s == spot ? TabOn : FishingController.IsUnlocked(s, p) ? TabOff : TabLocked;
            }

            fishHereButton.gameObject.SetActive(unlocked);
            fishHereButton.interactable = !current;
            fishHereText.text = current ? L.T("FISHING HERE") : L.T("FISH HERE");

            for (int i = 0; i < FishCatalog.All.Length && i < slotRoots.Length; i++)
            {
                var species = FishCatalog.All[i];
                bool inSpot = spot.Contains(species.Id);
                slotRoots[i].SetActive(inSpot);
                if (!inSpot) continue;

                var record = p.Find(species.Id);
                bool caught = record != null;
                RareNoteTarget.Attach(slotRoots[i], EnvironmentId.Fishing, species.Id, caught);
                if (species == spot.Legendary)
                {
                    slotRoots[i].SetActive(caught);
                    if (!caught) continue;
                    nameTexts[i].text = L.T("LEGENDARY") + "  ·  " + L.T(species.Name);
                    detailTexts[i].text = L.F("Best {0:0.00} kg  ·  ×{1}", record.bestKg, record.count) + "  ·  <alpha=#99>" + L.F("First caught {0}", record.firstCaught);
                    continue;
                }
                fishImages[i].color = caught ? Color.white : Silhouette;
                nameTexts[i].text = caught ? L.T(species.Name) : "???";
                detailTexts[i].text = caught
                    ? L.F("Best {0:0.00} kg  ·  ×{1}", record.bestKg, record.count) + "\n<alpha=#88>" + L.F("First caught {0}", record.firstCaught)
                    : "<alpha=#88>" + L.F("{0:0.#}–{1:0.#} kg", species.MinKg, species.MaxKg);
            }
        }
    }
}
