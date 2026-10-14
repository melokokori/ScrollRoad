using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Hız göstergesi: skin görselleri (GaugeSkins) + yaylı ibre + kadran içi kilometre sayacı.
    //  - Tam boy (kart, görev çubuğunda "kartın aynısı" / "taşan kadran"): rakamlı kadran, sayaç
    //    kadranın içinde, rölantide kehribar "IDLE".
    //  - Mini (görev çubuğu plakası): rakamsız sade kadran; sayaç ve hız plakada ayrı.
    // İbre gerçek hızı yayla izler: scroll çentiğinde hafifçe seker, yüksek hızda ufak titrer.
    // Dijital skinde ibre yok: segment yayı (radial fill) dolar, ortada büyük dijital hız.
    public class GaugeView : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] CarCatalog catalog;
        [SerializeField] bool mini;
        [SerializeField] Image face;
        [SerializeField] Image needle;
        [SerializeField] Image lit;            // Dijital segmentleri
        [SerializeField] TMP_Text bigNumber;   // Dijital hız (yalnız tam boy)
        [SerializeField] TMP_Text idleLabel;   // yalnız tam boy
        [SerializeField] OdometerView odometer; // yalnız tam boy

        [Header("İbre")]
        [SerializeField] float stiffness = 140f;
        [SerializeField] float damping = 15f;
        [SerializeField] float jitterFromKmh = 140f;
        [SerializeField] float jitterDeg = 1.2f;

        // Görev çubuğu kadranlarında (44-88 px) kadran içi yazılar okunmaz lekeye dönüyor: IDLE
        // 100 px'in, sayaç 60 px'in altında gizlenir (km ve rölanti şeritte kadranın yanında yazıyor).
        const float HideIdleBelowSize = 100f, HideOdometerBelowSize = 60f;
        bool hideIdle, hideOdometer;

        GaugeSkins.Skin skin;
        float needleKmh;
        float needleVelocity;
        float jitterTime;
        bool initialized;

        public GaugeSkins.Skin Skin => skin;

        void OnEnable()
        {
            GaugeSkins.Changed += Apply;
            CarCatalog.SelectionChanged += Apply;
            Apply();
        }

        void OnDisable()
        {
            GaugeSkins.Changed -= Apply;
            CarCatalog.SelectionChanged -= Apply;
        }

        public void Apply()
        {
            skin = GaugeSkins.For(catalog != null ? catalog.SelectedModel : null);
            face.sprite = GaugeSkins.Face(skin, mini);

            var needleSprite = GaugeSkins.Needle(skin, mini);
            needle.sprite = needleSprite;
            needle.enabled = needleSprite != null;

            var litSprite = GaugeSkins.Lit(skin, mini);
            lit.sprite = litSprite;
            lit.enabled = litSprite != null;

            if (bigNumber != null)
            {
                bigNumber.gameObject.SetActive(skin.lcd);
                bigNumber.color = skin.odoText;
            }
            if (idleLabel != null)
            {
                // Dijitalde KM/H yazısı alttaki sayaçla rakam arasında; IDLE rakamın üstüne.
                var rt = idleLabel.rectTransform;
                float y = skin.lcd ? 52f : 118f;
                rt.anchorMin = new Vector2(0.25f, 1f - (y + 7f) / 200f);
                rt.anchorMax = new Vector2(0.75f, 1f - (y - 7f) / 200f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            if (odometer != null)
            {
                odometer.PlaceInDial(100f - (6 * 12.5f + 5 * 1.5f) / 2f, skin.lcd ? 128f : 132f, 12.5f, 17f, 1.5f);
                odometer.SetColors(skin.odoText, skin.odoLastText);
            }
        }

        // Kadranı yeniden boyutlandırır; yazılar kadranın 200 birimlik uzayıyla orantılı büyür.
        public void SetSize(float size)
        {
            ((RectTransform)transform).sizeDelta = new Vector2(size, size);
            hideIdle = size < HideIdleBelowSize;
            hideOdometer = size < HideOdometerBelowSize;
            if (odometer != null)
            {
                odometer.gameObject.SetActive(!hideOdometer);
                odometer.SetFontSize(size * 13f / 200f);
            }
            if (idleLabel != null && hideIdle) idleLabel.gameObject.SetActive(false);
            if (bigNumber != null) bigNumber.fontSize = size * 38f / 200f;
            if (idleLabel != null) idleLabel.fontSize = size * 9f / 200f;
        }

        void Update()
        {
            if (drive == null || skin == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float speed = drive.CurrentSpeedKmh;
            if (!initialized)
            {
                needleKmh = speed;
                initialized = true;
            }

            jitterTime += dt;
            float jitter = Mathf.Max(0f, (Mathf.Abs(speed) - jitterFromKmh) / 80f) * Mathf.Sin(jitterTime * 37f) * jitterDeg;
            needleVelocity += ((speed + jitter - needleKmh) * stiffness - needleVelocity * damping) * dt;
            needleKmh = Mathf.Clamp(needleKmh + needleVelocity * dt, GaugeSkins.MinKmh - 5f, GaugeSkins.MaxKmh + 5f);

            if (needle.enabled) needle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GaugeSkins.NeedleZ(needleKmh));
            if (lit.enabled) lit.fillAmount = needleKmh <= 0f ? 0f : GaugeSkins.LcdFill(needleKmh);
            if (bigNumber != null && skin.lcd)
            {
                float shown = skin.knots ? speed / 1.852f : speed;
                int v = Mathf.RoundToInt(Mathf.Abs(shown));
                bigNumber.SetText(speed < -1f ? "R{0}" : "{0}", v);
            }
            if (idleLabel != null)
            {
                bool idle = !drive.IsUserActive && !hideIdle;
                if (idleLabel.gameObject.activeSelf != idle) idleLabel.gameObject.SetActive(idle);
            }
            if (odometer != null && !hideOdometer) odometer.Show(drive.CurrentDistanceKmPrecise);
        }
    }
}
