using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Masaüstü modunun (görev çubuğu şeridi) arayüzü — sabit piksel boyutlu ayrı canvas, görev
    // çubuğu ölçeğiyle büyür. Hız göstergesinin üç görünümü (Ayarlar → GaugeSkins.Strip):
    //  - Sade plaka: skinin renkli plakası; mini kadran + yuvarlanan sayaç + hız (rölantide IDLE).
    //    Durak/hedef duyurusu ve gizli bölge fısıltısı plakada sayacın yerine geçer.
    //  - Kartın aynısı: kart kadranı 44 px; yanında büyük km ve alt satır (hız/duyuru/fısıltı).
    //  - Taşan kadran: kart kadranı 88 px, yarısı görev çubuğunun üstüne taşar (pencere o kadar
    //    yukarı uzar, saydam kısım tıklamayı geçirir).
    // Sağda ≡ menüsü (Kart / Çıkış). Normal modda gizli.
    public class CompanionHudController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] SecretZoneView secretZone;
        [SerializeField] NoticeController notice;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject root;
        [SerializeField] CanvasScaler scaler;
        [SerializeField] Text kmText;
        [SerializeField] Text subText;
        [SerializeField] Button menuButton;
        [SerializeField] GameObject menuPanel;
        [SerializeField] Button exitCompanionButton;
        [SerializeField] Button quitButton;

        [Header("Sade plaka")]
        [SerializeField] GameObject plateRoot;
        [SerializeField] Image plateImage;
        [SerializeField] GaugeView miniGauge;
        [SerializeField] OdometerView plateOdometer;
        [SerializeField] TMP_Text plateSpeed;
        [SerializeField] TMP_Text plateUnit;
        [SerializeField] TMP_Text plateNotice;

        [Header("Kart kadranı")]
        [SerializeField] GaugeView dialGauge;
        [SerializeField] float cardDialSize = 44f;
        [SerializeField] float peekDialSize = 88f;

        static readonly Color SubTextColor = new Color(0.85f, 0.87f, 0.90f, 0.85f);
        static readonly Color IdleColor = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color NoticeColor = new Color(1f, 0.86f, 0.45f);

        GaugeSkins.Skin plateSkin;

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

            // Taşan kadranda pencere yukarı uzuyor: şerit öğeleri alttaki görev çubuğu bandında kalsın.
            var rootRect = (RectTransform)root.transform;
            rootRect.offsetMax = new Vector2(0f, -companionWindow.StripOverflowUnits);

            var style = GaugeSkins.Strip;
            bool plate = style == GaugeSkins.StripStyle.Plate;
            if (plateRoot.activeSelf != plate) plateRoot.SetActive(plate);
            if (dialGauge.gameObject.activeSelf == plate) dialGauge.gameObject.SetActive(!plate);
            kmText.gameObject.SetActive(!plate);
            subText.gameObject.SetActive(!plate);

            if (plate) UpdatePlate();
            else UpdateDial(style == GaugeSkins.StripStyle.Peek ? peekDialSize : cardDialSize);
        }

        void UpdatePlate()
        {
            if (plateSkin != miniGauge.Skin && miniGauge.Skin != null)
            {
                plateSkin = miniGauge.Skin;
                plateImage.sprite = GaugeSkins.Plate(plateSkin);
                plateOdometer.SetColors(plateSkin.odoText, plateSkin.odoLastText);
                plateSpeed.color = plateSkin.stripText;
            }
            if (plateSkin == null) return;

            string message = CurrentMessage(out var messageColor, out bool italic);
            bool hasMessage = message != null;
            plateNotice.gameObject.SetActive(hasMessage);
            plateOdometer.gameObject.SetActive(!hasMessage);
            plateSpeed.gameObject.SetActive(!hasMessage);
            plateUnit.gameObject.SetActive(!hasMessage);
            if (hasMessage)
            {
                plateNotice.text = message;
                plateNotice.color = messageColor;
                plateNotice.fontStyle = italic ? FontStyles.Italic : FontStyles.Normal;
                return;
            }

            plateOdometer.Show(drive.CurrentDistanceKmPrecise);
            float speed = drive.CurrentSpeedKmh;
            float shown = plateSkin.knots ? speed / 1.852f : speed;
            int v = Mathf.RoundToInt(Mathf.Abs(shown));
            plateSpeed.SetText(speed < -1f ? "R {0}" : "{0}", v);
            bool idle = !drive.IsUserActive;
            plateUnit.text = idle ? "IDLE" : plateSkin.knots ? "KN" : "KM/H";
            var unitColor = idle ? IdleColor : plateSkin.stripText;
            plateUnit.color = new Color(unitColor.r, unitColor.g, unitColor.b, idle ? 1f : 0.65f);
        }

        void UpdateDial(float size)
        {
            var rt = (RectTransform)dialGauge.transform;
            if (!Mathf.Approximately(rt.sizeDelta.x, size)) dialGauge.SetSize(size);

            // Km ve alt satır kadranın sağında.
            float x = rt.anchoredPosition.x + size + 10f;
            kmText.rectTransform.anchoredPosition = new Vector2(x, kmText.rectTransform.anchoredPosition.y);
            subText.rectTransform.anchoredPosition = new Vector2(x, subText.rectTransform.anchoredPosition.y);
            kmText.text = Fmt.Km(drive.CurrentDistanceKmPrecise);

            string message = CurrentMessage(out var messageColor, out bool italic);
            if (message != null)
            {
                subText.text = message;
                subText.color = messageColor;
                subText.fontStyle = italic ? FontStyle.Italic : FontStyle.Bold;
            }
            else
            {
                var skin = dialGauge.Skin;
                bool knots = skin != null && skin.knots;
                float speed = drive.CurrentSpeedKmh;
                int v = Mathf.RoundToInt(knots ? speed / 1.852f : speed);
                string unit = knots ? "kn" : "km/h";
                subText.text = drive.IsUserActive ? $"{v} {unit}" : $"idle · {v} {unit}";
                subText.color = SubTextColor;
                subText.fontStyle = FontStyle.Normal;
            }
        }

        // Gizli bölge fısıltısı görünürken o, sonra damga/hedef duyurusu; yoksa null.
        string CurrentMessage(out Color color, out bool italic)
        {
            if (secretZone != null && secretZone.CurrentAlpha > 0.05f && !string.IsNullOrEmpty(secretZone.CurrentText))
            {
                var w = SecretZoneView.WhisperColor;
                color = new Color(w.r, w.g, w.b, secretZone.CurrentAlpha);
                italic = true;
                return secretZone.CurrentText;
            }
            if (notice != null && notice.CompanionAlpha > 0.05f && !string.IsNullOrEmpty(notice.CompanionText))
            {
                color = new Color(NoticeColor.r, NoticeColor.g, NoticeColor.b, notice.CompanionAlpha);
                italic = false;
                return notice.CompanionText;
            }
            color = Color.white;
            italic = false;
            return null;
        }
    }
}
