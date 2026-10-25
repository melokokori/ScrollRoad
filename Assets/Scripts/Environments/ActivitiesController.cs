using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ortam seçici (≡ çekmecesindeki "Activities"): her ortam bir kart. Karta tıklayınca o ortam seçilir
    // (yalnız seçili ortam ilerler). Tüm ortamlar baştan açık (2026-10-15 kullanıcı kararı). Kartın köşesindeki
    // SIDE düğmesi ortamı yan yuvaya koyar (SideActivity, 2026-10-25; 5 saatlik oyundan sonra açılır).
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
        [SerializeField] Button[] sideButtons;
        [SerializeField] Image[] sideFrames;
        [SerializeField] TMP_Text[] sideTexts;
        [SerializeField] TMP_Text hintText;

        static readonly Color ActiveFrame = new Color(0.055f, 0.42f, 0.243f);   // tabela yeşili
        static readonly Color IdleFrame = new Color(0.133f, 0.145f, 0.172f);
        static readonly Color Muted = new Color(0.957f, 0.945f, 0.902f, 0.6f);
        static readonly Color Amber = new Color(0.949f, 0.702f, 0.239f);

        public GameObject Panel => panel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;
                cardButtons[i].onClick.AddListener(() => Pick(index));
                if (i < sideButtons.Length) sideButtons[i].onClick.AddListener(() => PickSide(index));
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

        void PickSide(int index)
        {
            var all = EnvironmentCatalog.All;
            if (index < 0 || index >= all.Length) return;
            SideActivity.Toggle(all[index].Id);
            Refresh();
        }

        void Refresh()
        {
            var all = EnvironmentCatalog.All;
            var current = EnvironmentManager.Current;
            if (hintText != null)
            {
                var sideDef = SideActivity.Current?.Def;
                string rate = $"{SideActivity.Rate * 100:0}%";
                hintText.text = !SideActivity.IsUnlocked
                    ? $"Only the activity you pick moves forward. A side activity slot opens in {UnlockLadder.Remaining(SideActivity.PointsToUnlock)} of play."
                    : sideDef != null && sideDef != current
                        ? $"Side activity: {sideDef.Name} moves toward its unlocks at {rate} while you're elsewhere."
                        : $"Tap SIDE on a card: that activity moves toward its unlocks at {rate} while you play another.";
            }
            for (int i = 0; i < all.Length && i < cardButtons.Length; i++)
            {
                bool active = all[i] == current;
                cardFrames[i].color = active ? ActiveFrame : IdleFrame;
                thumbs[i].color = Color.white;
                cardButtons[i].interactable = !active;
                bool isNew = !active && ActivityGuide.IsNew(all[i].Id); // hiç girilmemiş (ActivityGuide)
                bool side = !active && SideActivity.Has(all[i].Id);
                statusTexts[i].text = active ? "PLAYING" : side ? $"SIDE · {SideActivity.Rate * 100:0}% SPEED" : isNew ? "NEW · CLICK TO TRY" : "CLICK TO SWITCH";
                statusTexts[i].color = active ? Color.white : side || isNew ? Amber : Muted;

                if (i >= sideButtons.Length) continue;
                bool canSide = SideActivity.IsUnlocked && !active && all[i].Id != EnvironmentId.Road;
                sideButtons[i].gameObject.SetActive(canSide);
                sideFrames[i].color = side ? Amber : new Color(1f, 1f, 1f, 0.12f);
                sideTexts[i].color = side ? new Color(0.12f, 0.1f, 0.06f) : Color.white;
            }
        }
    }
}
