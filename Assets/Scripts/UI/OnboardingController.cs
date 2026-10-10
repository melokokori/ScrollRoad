using ScrollRoad.InputCapture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // İlk açılış rehberi: Game sahnesine ilk girişte oyunu DURDURMADAN ekranda küçük bir kart,
    // 4 adım. "Scroll = gas" adımı oyuncu gerçekten yukarı scroll yapınca kendiliğinden ilerler
    // (araba tepki verirken görsün diye arka plan karartılmaz). Bitince ya da "Atla" ile bir daha
    // gösterilmez (cihaza özel, yerel). Şerit modunda, tepsideyken ve menü açıkken bekler.
    public class OnboardingController : MonoBehaviour
    {
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] CardMenuController menu;
        [SerializeField] GameObject card;
        [SerializeField] TMP_Text stepText;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text hintText;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text nextLabel;
        [SerializeField] Button skipButton;
        [SerializeField] float scrollNotchesToAdvance = 4f;
        [SerializeField] float autoAdvanceDelay = 1.2f;

        struct Step
        {
            public string Title, Body, Hint;
            public bool WaitsForScroll;
        }

        static readonly Step[] Steps =
        {
            new Step
            {
                Title = "Goal: the city",
                Body = "A single road leads to a distant city. A new race starts every month, and whoever gets furthest by the end of the month wins.",
            },
            new Step
            {
                Title = "Scroll = gas",
                Body = "Scroll the mouse wheel up and the car speeds up. Scroll down to slow down; keep going and you'll reverse.",
                Hint = "Try it: scroll up a little.",
                WaitsForScroll = true,
            },
            new Step
            {
                Title = "It counts everywhere",
                Body = "In your browser, in Word, wherever you scroll, the car moves, even while the game is in the background. Macros and auto-scroll don't count.",
            },
            new Step
            {
                Title = "Stop and it idles",
                Body = "After 3 minutes without a scroll, the car drops to idle. Closing the card keeps the game running in the tray next to the clock. Press F9 to shrink it into the taskbar.",
            },
        };

        int stepIndex;
        float scrolledUp;
        float autoAdvanceTimer = -1f;
        bool active;

        void Awake()
        {
            nextButton.onClick.AddListener(Next);
            skipButton.onClick.AddListener(Finish);
            card.SetActive(false);
            active = !SaveService.LoadOnboardingDone();
            if (active) ShowStep(0);
        }

        void Update()
        {
            if (!active) return;

            bool blocked = (companionWindow != null && companionWindow.IsCompanionMode)
                           || BackgroundLifecycle.IsHiddenToTray
                           || (menu != null && menu.IsOpen);
            if (card.activeSelf == blocked) card.SetActive(!blocked);
            if (blocked) return;

            if (!Steps[stepIndex].WaitsForScroll) return;

            if (autoAdvanceTimer >= 0f)
            {
                autoAdvanceTimer -= Time.unscaledDeltaTime;
                if (autoAdvanceTimer < 0f) Next();
                return;
            }

            var source = GlobalScrollSource.Instance;
            if (source != null && source.FrameNotchesSigned > 0f) scrolledUp += source.FrameNotchesSigned;
            if (scrolledUp >= scrollNotchesToAdvance)
            {
                hintText.text = "That's it!";
                autoAdvanceTimer = autoAdvanceDelay;
            }
        }

        void ShowStep(int index)
        {
            stepIndex = index;
            scrolledUp = 0f;
            autoAdvanceTimer = -1f;

            var step = Steps[index];
            bool last = index == Steps.Length - 1;
            stepText.text = $"{index + 1} / {Steps.Length}";
            titleText.text = step.Title;
            bodyText.text = step.Body;
            hintText.text = step.Hint ?? "";
            nextLabel.text = last ? "Start" : "Next";
            skipButton.gameObject.SetActive(!last);
        }

        void Next()
        {
            if (stepIndex + 1 < Steps.Length) ShowStep(stepIndex + 1);
            else Finish();
        }

        void Finish()
        {
            active = false;
            card.SetActive(false);
            SaveService.MarkOnboardingDone();
        }
    }
}
