using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Olta'nın makara kadranı (2026-10-15): Yol'daki hız göstergesinin (GaugeView) karşılığı, kartın sol
    // üstünde onun yerinde. İbre makara kolunun devrini (RPM) gösterir: ortak puanın hızı Yol'daki km/h'nin
    // yarısı (seyirde 30, rölantide 5, boost'ta 110'a kadar); misina boşalırken eksi bölgeye ("OUT") iner,
    // atışta/tutuşta kol durur, ibre sıfıra düşer. Kadran içindeki sayaç bu ayın balığını yuvarlar; ondalık
    // hane sıradaki balığa kadar sarılan kısım (balık tutulunca tam haneye geçer). Yüz ve ibre seçili takımın
    // (FishingGear.Dial) — kadran ölçeği GaugeSkins'in geometrisi (RPM = km/h / 2).
    public class ReelGaugeView : MonoBehaviour
    {
        [SerializeField] FishingController fishing;
        [SerializeField] Image face;
        [SerializeField] Image needle;
        [SerializeField] TMP_Text idleLabel;
        [SerializeField] OdometerView odometer;

        [Header("İbre")]
        [SerializeField] float stiffness = 140f;
        [SerializeField] float damping = 15f;

        public const float RpmPerPace = 0.5f;

        FishingGear.Gear shownGear;
        float needleRpm, needleVelocity;

        // Makara kolunun devri: yalnız sarılırken/misina boşalırken; ortak puan hızından.
        public static float Rpm()
        {
            var engine = ScoreEngine.Instance;
            return engine != null ? engine.Pace * RpmPerPace : 0f;
        }

        void OnEnable()
        {
            FishingGear.Changed += Apply;
            Apply();
        }

        void OnDisable() => FishingGear.Changed -= Apply;

        void Apply()
        {
            var gear = FishingGear.Selected;
            shownGear = gear;
            face.sprite = Load(gear.Dial, "face");
            var needleSprite = Load(gear.Dial, "needle");
            needle.sprite = needleSprite;
            needle.enabled = needleSprite != null;
            if (odometer != null)
            {
                odometer.PlaceInDial(100f - (6 * 12.5f + 5 * 1.5f) / 2f, 132f, 12.5f, 17f, 1.5f);
                var ink = DialInk(gear);
                odometer.SetColors(ink.digits, ink.last);
            }
        }

        static (Color digits, Color last) DialInk(FishingGear.Gear gear)
        {
            switch (gear.Id)
            {
                case "bamboo": return (new Color(0.94f, 0.89f, 0.78f), new Color(0.94f, 0.89f, 0.78f));
                case "carbon": return (new Color(0.91f, 0.90f, 0.86f), new Color(0.08f, 0.09f, 0.11f));
                case "gold": return (new Color(0.91f, 0.78f, 0.45f), new Color(0.11f, 0.09f, 0.07f));
                default: return (new Color(0.91f, 0.93f, 0.95f), new Color(0.12f, 0.16f, 0.27f));
            }
        }

        static Sprite Load(string dial, string part)
        {
            var sprite = Resources.Load<Sprite>($"Gauges/{dial}_{part}");
            if (sprite == null) Debug.LogWarning($"[ReelGaugeView] Görsel yok: Gauges/{dial}_{part}");
            return sprite;
        }

        void Update()
        {
            if (fishing == null) return;
            if (shownGear != FishingGear.Selected) Apply(); // kilit açılınca/kayıt değişince

            var phase = fishing.CurrentPhase;
            bool turning = phase == FishingController.Phase.Retrieving || phase == FishingController.Phase.Hooked;
            float rpm = turning ? Rpm() : 0f;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            needleVelocity += ((rpm - needleRpm) * stiffness - needleVelocity * damping) * dt;
            float min = GaugeSkins.MinKmh * RpmPerPace, max = GaugeSkins.MaxKmh * RpmPerPace;
            needleRpm = Mathf.Clamp(needleRpm + needleVelocity * dt, min - 2.5f, max + 2.5f);
            if (needle.enabled) needle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GaugeSkins.NeedleZ(needleRpm / RpmPerPace));

            if (idleLabel != null)
            {
                var engine = ScoreEngine.Instance;
                bool idle = engine != null && !engine.IsUserActive;
                if (idleLabel.gameObject.activeSelf != idle) idleLabel.gameObject.SetActive(idle);
            }
            if (odometer != null)
            {
                float partial = phase == FishingController.Phase.Landing || phase == FishingController.Phase.Resting ? 0f : fishing.Reeled;
                odometer.Show(fishing.MonthFish + Mathf.Min(partial, 0.999f));
            }
        }
    }
}
