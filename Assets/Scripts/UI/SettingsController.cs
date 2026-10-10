using ScrollRoad.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ayarlar paneli (≡ çekmecesinden açılır): ana ses, kart boyutu, görev çubuğundaki hız
    // göstergesinin görünümü, kart her zaman üstte, açılışta görev çubuğu şeridi modu, kapatınca
    // tepside kalma, Windows ile başlama, tekne/uçakta kendi göstergesi. Hepsi cihaza özel —
    // yerel PlayerPrefs (Windows ile başlama ise doğrudan kayıt defteri), cloud'a gitmez.
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Slider volumeSlider;
        [SerializeField] TMP_Text volumeValueText;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Button[] cardSizeButtons; // CompanionWindowController.CardSizePresets sırasıyla
        [SerializeField] Toggle cardOnTopToggle;
        [SerializeField] Toggle startInCompanionToggle;
        [SerializeField] Toggle closeToTrayToggle;
        [SerializeField] Toggle runAtStartupToggle;
        [SerializeField] Button[] stripGaugeButtons; // GaugeSkins.StripStyle sırasıyla
        [SerializeField] Toggle vehicleGaugeToggle;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        public GameObject Panel => settingsPanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            for (int i = 0; i < cardSizeButtons.Length; i++)
            {
                int preset = i;
                cardSizeButtons[i].onClick.AddListener(() =>
                {
                    companionWindow.SetCardSize(preset);
                    HighlightCardSize();
                });
            }
            cardOnTopToggle.onValueChanged.AddListener(on => companionWindow.SetCardOnTop(on));
            startInCompanionToggle.onValueChanged.AddListener(SaveService.SaveStartInCompanion);
            closeToTrayToggle.onValueChanged.AddListener(SaveService.SaveCloseToTray);
            runAtStartupToggle.onValueChanged.AddListener(OnRunAtStartupChanged);
            for (int i = 0; i < stripGaugeButtons.Length; i++)
            {
                var style = (GaugeSkins.StripStyle)i;
                stripGaugeButtons[i].onClick.AddListener(() =>
                {
                    GaugeSkins.Strip = style;
                    HighlightStripGauge();
                });
            }
            vehicleGaugeToggle.onValueChanged.AddListener(on => GaugeSkins.VehicleGauges = on);
            runAtStartupToggle.interactable = WindowsStartup.IsSupported; // Editor'de kayıt defterine yazılmaz
            settingsPanel.SetActive(false);
        }

        void Open()
        {
            float volume = SaveService.LoadMasterVolume();
            volumeSlider.SetValueWithoutNotify(volume);
            volumeValueText.text = $"{Mathf.RoundToInt(volume * 100)}%";
            HighlightCardSize();
            cardOnTopToggle.SetIsOnWithoutNotify(SaveService.LoadCardOnTop());
            startInCompanionToggle.SetIsOnWithoutNotify(SaveService.LoadStartInCompanion());
            closeToTrayToggle.SetIsOnWithoutNotify(SaveService.LoadCloseToTray());
            runAtStartupToggle.SetIsOnWithoutNotify(WindowsStartup.IsEnabled());
            HighlightStripGauge();
            vehicleGaugeToggle.SetIsOnWithoutNotify(GaugeSkins.VehicleGauges);
            settingsPanel.SetActive(true);
        }

        void HighlightCardSize()
        {
            int selected = SaveService.LoadCardSize();
            for (int i = 0; i < cardSizeButtons.Length; i++)
            {
                cardSizeButtons[i].GetComponent<Image>().color = i == selected ? SelectedColor : IdleColor;
            }
        }

        void HighlightStripGauge()
        {
            int selected = (int)GaugeSkins.Strip;
            for (int i = 0; i < stripGaugeButtons.Length; i++)
            {
                stripGaugeButtons[i].GetComponent<Image>().color = i == selected ? SelectedColor : IdleColor;
            }
        }

        static readonly Color SelectedColor = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly Color IdleColor = new Color(0.165f, 0.18f, 0.215f);

        void OnRunAtStartupChanged(bool enabled)
        {
            // Yazılamadıysa anahtarı gerçek duruma geri çek.
            if (!WindowsStartup.SetEnabled(enabled)) runAtStartupToggle.SetIsOnWithoutNotify(WindowsStartup.IsEnabled());
        }

        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            SaveService.SaveMasterVolume(value);
        }
    }
}
