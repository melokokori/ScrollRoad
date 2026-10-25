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

            titleText.text = "Fish log · " + spot.Name;
            int logged = 0;
            foreach (var s in spot.Species) if (p.Find(s.Id) != null) logged++;
            countText.text = unlocked
                ? $"{logged} / {spot.Species.Length} species  ·  {Fmt.Int(p.totalFish)} fish  ·  {Fmt.Amount(p.totalKg, 1, "kg")} caught"
                : $"LOCKED  ·  {UnlockLadder.Remaining(UnlockLadder.VenuePoints(System.Array.IndexOf(FishCatalog.Spots, spot)) - FishingController.LifetimePointsOf(p))} of fishing to go";

            for (int i = 0; i < tabTexts.Length && i < FishCatalog.Spots.Length; i++)
            {
                var s = FishCatalog.Spots[i];
                // 5 sekme dar: kilitli mekân yalnız soluk ad (kalan balık sayısı sağ üstte yazar).
                tabTexts[i].text = s.Name;
                tabTexts[i].color = s == spot ? TabOn : FishingController.IsUnlocked(s, p) ? TabOff : TabLocked;
            }

            fishHereButton.gameObject.SetActive(unlocked);
            fishHereButton.interactable = !current;
            fishHereText.text = current ? "FISHING HERE" : "FISH HERE";

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
                    nameTexts[i].text = $"LEGENDARY  ·  {species.Name}";
                    detailTexts[i].text = $"Best {record.bestKg:0.00} kg  ·  ×{record.count}  ·  <alpha=#99>First caught {record.firstCaught}";
                    continue;
                }
                fishImages[i].color = caught ? Color.white : Silhouette;
                nameTexts[i].text = caught ? species.Name : "???";
                detailTexts[i].text = caught
                    ? $"Best {record.bestKg:0.00} kg  ·  ×{record.count}\n<alpha=#88>First caught {record.firstCaught}"
                    : $"<alpha=#88>{species.MinKg:0.#}–{species.MaxKg:0.#} kg";
            }
        }
    }
}
