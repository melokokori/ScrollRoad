using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Dolap (Ortam Standardı v2, 2026-10-25; 2026-10-15'teki Olta takım kutusunun genellemesi): garajın Yol
    // dışındaki karşılığı. ≡ çekmecesindeki "Garage" satırı Yol dışında ortamın dolabının adını alır
    // ("Tackle box"…) ve bunu açar (GarageController yönlendirir). Skin takımları yan yana kart: önizleme
    // takımın kadranı (varsa), ad, kısa tarif; kilitliyse koyu silüet + kalan seyir süresi. Kartlar takım
    // sayısına göre ortalanır. Seçim ortamın kendi kaydına (IEnvironmentContent.SetSkin).
    public class SkinBoxController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Button[] cardButtons;
        [SerializeField] Image[] cardFrames;
        [SerializeField] Image[] dialImages;
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;
        [SerializeField] float cardSpacing = 188f;

        static readonly Color Selected = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Locked = new Color(0.02f, 0.03f, 0.05f, 0.6f);

        public GameObject Panel => panel;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;
                cardButtons[i].onClick.AddListener(() => Pick(index));
            }
            panel.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            panel.SetActive(true);
        }

        void Update()
        {
            if (panel.activeSelf && Time.frameCount % 30 == 0) Refresh(); // kalan süreler canlı
        }

        void Pick(int index)
        {
            var c = EnvironmentContent.Current;
            if (c == null || index >= c.Skins.Length) return;
            EnvironmentContent.SelectSkin(c, c.Skins[index].Id);
            Refresh();
        }

        void Refresh()
        {
            var c = EnvironmentContent.Current;
            if (c == null)
            {
                panel.SetActive(false);
                return;
            }
            var skins = c.Skins;
            double points = EnvironmentContent.LadderPoints(c);
            titleText.text = L.T(c.SkinBoxName);
            int open = 0;
            for (int i = 0; i < cardButtons.Length; i++)
            {
                bool exists = i < skins.Length;
                cardButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                var skin = skins[i];
                var rect = (RectTransform)cardButtons[i].transform;
                rect.anchoredPosition = new Vector2((i - (skins.Length - 1) / 2f) * cardSpacing, rect.anchoredPosition.y);

                bool unlocked = EnvironmentContent.IsSkinUnlocked(c, i);
                if (unlocked) open++;
                bool current = skin.Id == c.SkinId;
                cardFrames[i].color = current ? Selected : Idle;
                var dial = string.IsNullOrEmpty(skin.Tally) ? null : Resources.Load<Sprite>($"Tallies/{skin.Tally}_preview");
                dialImages[i].sprite = dial;
                dialImages[i].enabled = dial != null;
                dialImages[i].color = unlocked ? Color.white : Locked;
                nameTexts[i].text = L.T(skin.Name);
                detailTexts[i].text = !unlocked
                    ? "<alpha=#88>" + L.T("Unlocks with more play here") + "\n" + L.F("{0} to go", UnlockLadder.Remaining(UnlockLadder.SkinPoints(i) - points))
                    : current ? L.T("IN USE") + "\n<alpha=#88>" + L.T(skin.Blurb) : "<alpha=#88>" + L.T(skin.Blurb);
                cardButtons[i].interactable = unlocked;
            }
            countText.text = L.F("{0} / {1} unlocked  ·  {2} played here", open, skins.Length, UnlockLadder.Remaining(c.LifetimePoints));
        }
    }
}
