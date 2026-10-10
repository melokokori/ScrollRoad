using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Masaüstü arkadaşı modunun küçük arayüzü (sabit piksel boyutlu ayrı canvas): sol üstte km,
    // sağ üstte ☰ menüsü (Tam pencere / Çıkış), altta gizli bölge fısıltısı. Normal modda gizli.
    public class CompanionHudController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] SecretZoneView secretZone;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject root;
        [SerializeField] Text kmText;
        [SerializeField] Text whisperText;
        [SerializeField] Button menuButton;
        [SerializeField] GameObject menuPanel;
        [SerializeField] Button exitCompanionButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            menuButton.onClick.AddListener(() => menuPanel.SetActive(!menuPanel.activeSelf));
            exitCompanionButton.onClick.AddListener(() =>
            {
                menuPanel.SetActive(false);
                companionWindow.SetCompanionMode(false);
            });
            quitButton.onClick.AddListener(BackgroundLifecycle.QuitApp);
            menuPanel.SetActive(false);
            root.SetActive(false);
        }

        void Update()
        {
            bool show = companionWindow.IsCompanionMode;
            if (root.activeSelf != show)
            {
                root.SetActive(show);
                if (!show) menuPanel.SetActive(false);
            }
            if (!show) return;

            kmText.text = Fmt.Km(drive.CurrentDistanceKmPrecise);
            if (secretZone != null)
            {
                whisperText.text = secretZone.CurrentText;
                var w = SecretZoneView.WhisperColor;
                whisperText.color = new Color(w.r, w.g, w.b, secretZone.CurrentAlpha);
            }
        }
    }
}
