using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Masaüstü modunun (görev çubuğu şeridi) arayüzü — sabit piksel boyutlu ayrı canvas, görev
    // çubuğu ölçeğiyle büyür: solda büyük km, altında hız (gizli bölgede fısıltı), sağda ≡ menüsü
    // (Tam pencere / Çıkış, şeride sığsın diye yan yana). Normal modda gizli.
    public class CompanionHudController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] SecretZoneView secretZone;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject root;
        [SerializeField] CanvasScaler scaler;
        [SerializeField] Text kmText;
        [SerializeField] Text subText;
        [SerializeField] Button menuButton;
        [SerializeField] GameObject menuPanel;
        [SerializeField] Button exitCompanionButton;
        [SerializeField] Button quitButton;

        static readonly Color SubTextColor = new Color(0.85f, 0.87f, 0.90f, 0.85f);

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

            scaler.scaleFactor = companionWindow.StripScale;
            kmText.text = Fmt.Km(drive.CurrentDistanceKmPrecise);

            // Alt satır: gizli bölge fısıltısı görünürken o, değilse hız.
            if (secretZone != null && secretZone.CurrentAlpha > 0.05f && !string.IsNullOrEmpty(secretZone.CurrentText))
            {
                subText.text = secretZone.CurrentText;
                var w = SecretZoneView.WhisperColor;
                subText.color = new Color(w.r, w.g, w.b, secretZone.CurrentAlpha);
                subText.fontStyle = FontStyle.Italic;
            }
            else
            {
                int speed = Mathf.RoundToInt(drive.CurrentSpeedKmh);
                subText.text = drive.IsUserActive ? $"{speed} km/sa" : $"rölanti · {speed} km/sa";
                subText.color = SubTextColor;
                subText.fontStyle = FontStyle.Normal;
            }
        }
    }
}
