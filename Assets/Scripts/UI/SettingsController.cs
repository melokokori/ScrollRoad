using ScrollRoad.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ayarlar paneli (ana menü ve duraklatma menüsünden açılır): ana ses, açılışta masaüstü
    // arkadaşı modu, kapatınca tepside kalma, Windows ile başlama. Hepsi cihaza özel — yerel
    // PlayerPrefs (Windows ile başlama ise doğrudan kayıt defteri), cloud'a gitmez.
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Text volumeValueText;
        [SerializeField] Toggle startInCompanionToggle;
        [SerializeField] Toggle closeToTrayToggle;
        [SerializeField] Toggle runAtStartupToggle;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        public GameObject Panel => settingsPanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            startInCompanionToggle.onValueChanged.AddListener(SaveService.SaveStartInCompanion);
            closeToTrayToggle.onValueChanged.AddListener(SaveService.SaveCloseToTray);
            runAtStartupToggle.onValueChanged.AddListener(OnRunAtStartupChanged);
            runAtStartupToggle.interactable = WindowsStartup.IsSupported; // Editor'de kayıt defterine yazılmaz
            settingsPanel.SetActive(false);
        }

        void Open()
        {
            float volume = SaveService.LoadMasterVolume();
            volumeSlider.SetValueWithoutNotify(volume);
            volumeValueText.text = $"%{Mathf.RoundToInt(volume * 100)}";
            startInCompanionToggle.SetIsOnWithoutNotify(SaveService.LoadStartInCompanion());
            closeToTrayToggle.SetIsOnWithoutNotify(SaveService.LoadCloseToTray());
            runAtStartupToggle.SetIsOnWithoutNotify(WindowsStartup.IsEnabled());
            settingsPanel.SetActive(true);
        }

        void OnRunAtStartupChanged(bool enabled)
        {
            // Yazılamadıysa anahtarı gerçek duruma geri çek.
            if (!WindowsStartup.SetEnabled(enabled)) runAtStartupToggle.SetIsOnWithoutNotify(WindowsStartup.IsEnabled());
        }

        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            volumeValueText.text = $"%{Mathf.RoundToInt(value * 100)}";
            SaveService.SaveMasterVolume(value);
        }
    }
}
