using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ortam seçici (≡ çekmecesindeki "Activities"): her ortam bir kart. Karta tıklayınca o ortam seçilir
    // (yalnız seçili ortam ilerler). Tüm ortamlar baştan açık (2026-10-15 kullanıcı kararı).
    // Kart dizileri EnvironmentCatalog.All sırasıyla (sahne kurucu öyle dizer).
    public class ActivitiesController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button[] cardButtons;
        [SerializeField] Image[] cardFrames;
        [SerializeField] Image[] thumbs;
        [SerializeField] TMP_Text[] statusTexts;

        static readonly Color ActiveFrame = new Color(0.055f, 0.42f, 0.243f);   // tabela yeşili
        static readonly Color IdleFrame = new Color(0.133f, 0.145f, 0.172f);
        static readonly Color Muted = new Color(0.957f, 0.945f, 0.902f, 0.6f);

        public GameObject Panel => panel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;
                cardButtons[i].onClick.AddListener(() => Pick(index));
            }
            panel.SetActive(false);
        }

        void Open()
        {
            Refresh();
            panel.SetActive(true);
        }

        void Pick(int index)
        {
            var all = EnvironmentCatalog.All;
            if (index < 0 || index >= all.Length) return;
            if (!EnvironmentManager.Select(all[index])) return;
            Refresh();
            panel.SetActive(false); // seçilen ortam hemen görünsün
        }

        void Refresh()
        {
            var all = EnvironmentCatalog.All;
            var current = EnvironmentManager.Current;
            for (int i = 0; i < all.Length && i < cardButtons.Length; i++)
            {
                bool active = all[i] == current;
                cardFrames[i].color = active ? ActiveFrame : IdleFrame;
                thumbs[i].color = Color.white;
                cardButtons[i].interactable = !active;
                statusTexts[i].text = active ? "PLAYING" : "CLICK TO SWITCH";
                statusTexts[i].color = active ? Color.white : Muted;
            }
        }
    }
}
