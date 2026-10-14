using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ortam seçici (≡ çekmecesindeki "Activities"): her ortam bir kart. Açık ortama tıklayınca o ortam
    // seçilir (yalnız seçili ortam ilerler). Kilitli kartta ilerleme çubuğu ve kalan miktar — gizli puan
    // görünmez, kalan miktar o an açık olan ortamın biriminde yazılır ("120 km to go").
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
        [SerializeField] GameObject[] progressTracks;
        [SerializeField] RectTransform[] progressFills;

        static readonly Color ActiveFrame = new Color(0.055f, 0.42f, 0.243f);   // tabela yeşili
        static readonly Color IdleFrame = new Color(0.133f, 0.145f, 0.172f);
        static readonly Color LockedFrame = new Color(0.10f, 0.11f, 0.13f);
        static readonly Color Amber = new Color(0.949f, 0.702f, 0.239f);
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

        void Update()
        {
            if (panel.activeSelf) Refresh(); // kilit ilerlemesi canlı
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
                var def = all[i];
                bool unlocked = EnvironmentManager.IsUnlocked(def);
                bool active = def == current;
                cardFrames[i].color = active ? ActiveFrame : unlocked ? IdleFrame : LockedFrame;
                thumbs[i].color = unlocked ? Color.white : new Color(0.02f, 0.03f, 0.05f, 0.6f);
                cardButtons[i].interactable = unlocked && !active;
                progressTracks[i].SetActive(!unlocked);

                if (active)
                {
                    statusTexts[i].text = "PLAYING";
                    statusTexts[i].color = Color.white;
                }
                else if (unlocked)
                {
                    statusTexts[i].text = "CLICK TO SWITCH";
                    statusTexts[i].color = Muted;
                }
                else
                {
                    string left = current.Format(EnvironmentManager.PointsToUnlock(def), 0);
                    statusTexts[i].text = $"LOCKED  ·  {left} to go";
                    statusTexts[i].color = Amber;
                    progressFills[i].anchorMax = new Vector2(EnvironmentManager.UnlockProgress(def), 1f);
                }
            }
        }
    }
}
