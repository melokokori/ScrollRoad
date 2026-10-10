using ScrollRoad.InputCapture;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // İlk açılış rehberi: Game sahnesine ilk girişte oyunu DURDURMADAN ekranda küçük bir kart,
    // 4 adım. "Scroll = gaz" adımı oyuncu gerçekten yukarı scroll yapınca kendiliğinden ilerler
    // (araba tepki verirken görsün diye arka plan karartılmaz). Bitince ya da "Atla" ile bir daha
    // gösterilmez (cihaza özel, yerel). Masaüstü modunda, tepsideyken ve duraklatmada bekler.
    public class OnboardingController : MonoBehaviour
    {
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] PauseController pause;
        [SerializeField] GameObject card;
        [SerializeField] Text stepText;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Text hintText;
        [SerializeField] Button nextButton;
        [SerializeField] Text nextLabel;
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
                Title = "Hedef: şehir",
                Body = "Uzaktaki şehre giden tek bir yol var. Her ay yeni bir yarış başlar; ay sonunda en uzağa giden kazanır.",
            },
            new Step
            {
                Title = "Scroll = gaz",
                Body = "Fare tekerleğini yukarı çevir, araba hızlansın. Aşağı çevirirsen yavaşlar, fazlası geri vites.",
                Hint = "Dene: biraz yukarı scroll yap.",
                WaitsForScroll = true,
            },
            new Step
            {
                Title = "Her yerde sayılır",
                Body = "Tarayıcıda, Word'de, nerede scroll yaparsan araba ilerler; oyun arka planda olsa bile. Makrolar ve otomatik scroll sayılmaz.",
            },
            new Step
            {
                Title = "Durursan rölanti",
                Body = "3 dakika scroll gelmezse araba rölantiye düşer. Pencereyi kapatınca saatin yanındaki simgede çalışmaya devam eder; F9 ile görev çubuğuna küçülür.",
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
                           || (pause != null && pause.IsPaused);
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
                hintText.text = "İşte bu!";
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
            nextLabel.text = last ? "Başla" : "İleri";
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
