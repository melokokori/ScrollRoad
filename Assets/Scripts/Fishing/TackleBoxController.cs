using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Takım kutusu (2026-10-15): garajın Olta karşılığı. ≡ çekmecesindeki "Garage" satırı Olta'dayken
    // "Tackle box" olur ve bunu açar (GarageController yönlendirir). Dört takım yan yana kart: önizleme
    // takımın makara kadranı, ad, kısa tarif; kilitliyse koyu silüet + kaç balık kaldığı. Seçim olta kaydına.
    public class TackleBoxController : MonoBehaviour
    {
        [SerializeField] FishingController fishing;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text countText;
        [SerializeField] Button[] cardButtons;   // FishingGear.All sırasıyla
        [SerializeField] Image[] cardFrames;
        [SerializeField] Image[] dialImages;
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;

        static readonly Color Selected = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Locked = new Color(0.02f, 0.03f, 0.05f, 0.6f);

        public GameObject Panel => panel;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < cardButtons.Length && i < FishingGear.All.Length; i++)
            {
                var gear = FishingGear.All[i];
                cardButtons[i].onClick.AddListener(() =>
                {
                    FishingGear.Select(gear);
                    Refresh();
                });
            }
            panel.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            panel.SetActive(true);
        }

        void Refresh()
        {
            var p = fishing != null ? fishing.Progress : SaveService.LoadFishing();
            var selected = FishingGear.SelectedIn(p);
            int open = 0;
            for (int i = 0; i < FishingGear.All.Length && i < cardButtons.Length; i++)
            {
                var gear = FishingGear.All[i];
                bool unlocked = FishingGear.IsUnlocked(gear, p);
                if (unlocked) open++;
                bool current = gear == selected;
                cardFrames[i].color = current ? Selected : Idle;
                if (dialImages[i].sprite == null) dialImages[i].sprite = Resources.Load<Sprite>($"Gauges/{gear.Dial}_face");
                dialImages[i].color = unlocked ? Color.white : Locked;
                nameTexts[i].text = gear.Name;
                double left = UnlockLadder.SkinPoints(i) - FishingController.LifetimePointsOf(p);
                detailTexts[i].text = !unlocked
                    ? $"<alpha=#88>Unlocks with more fishing\n{UnlockLadder.Remaining(left)} to go"
                    : current ? $"IN USE\n<alpha=#88>{gear.Blurb}" : $"<alpha=#88>{gear.Blurb}";
                cardButtons[i].interactable = unlocked;
            }
            countText.text = $"{open} / {FishingGear.All.Length} tackle  ·  {Fmt.Int(p != null ? p.totalFish : 0)} fish caught";
        }
    }
}
