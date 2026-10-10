using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ayarlar paneli (ana menü ve duraklatma menüsünden açılır): ana ses + açılışta masaüstü
    // arkadaşı modu. İkisi de cihaza özel tercih — yerel PlayerPrefs, cloud'a gitmez.
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Text volumeValueText;
        [SerializeField] Toggle startInCompanionToggle;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        public GameObject Panel => settingsPanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            startInCompanionToggle.onValueChanged.AddListener(SaveService.SaveStartInCompanion);
            settingsPanel.SetActive(false);
        }

        void Open()
        {
            float volume = SaveService.LoadMasterVolume();
            volumeSlider.SetValueWithoutNotify(volume);
            volumeValueText.text = $"%{Mathf.RoundToInt(volume * 100)}";
            startInCompanionToggle.SetIsOnWithoutNotify(SaveService.LoadStartInCompanion());
            settingsPanel.SetActive(true);
        }

        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            volumeValueText.text = $"%{Mathf.RoundToInt(value * 100)}";
            SaveService.SaveMasterVolume(value);
        }
    }
}
