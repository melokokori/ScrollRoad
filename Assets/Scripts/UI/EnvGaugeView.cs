using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Ortam kadranı (Ortam Standardı v2 · kadran, 2026-10-25): Olta'nın makara kadranının (ReelGaugeView)
    // genellemesi, Yol ve Olta dışındaki ortamların kartında sol üstte. İbre ortamın dönen nesnesinin devri:
    // RPM = ortak tempo / 2 (seyirde 30, rölantide 5, boost'ta 110'a kadar, geri sararken eksi bölge). Kadran
    // içindeki sayaç bu ayın birimi (ondalık hane sıradaki öğeye kadar olan kısım). Yüz, ibre ve sayaç
    // mürekkebi seçili skin takımının (SkinInfo.Dial / DialInk) — skin değişince kendiliğinden yenilenir.
    public class EnvGaugeView : MonoBehaviour
    {
        [SerializeField] EnvironmentId environment;
        [SerializeField] Image face;
        [SerializeField] Image needle;
        [SerializeField] TMP_Text idleLabel;
        [SerializeField] OdometerView odometer;

        [Header("İbre")]
        [SerializeField] float stiffness = 140f;
        [SerializeField] float damping = 15f;

        string shownDial;
        float needleRpm, needleVelocity;

        void Apply(SkinInfo skin)
        {
            shownDial = skin.Dial;
            face.sprite = string.IsNullOrEmpty(skin.Dial) ? null : Resources.Load<Sprite>($"Gauges/{skin.Dial}_face");
            face.enabled = face.sprite != null;
            needle.sprite = string.IsNullOrEmpty(skin.Dial) ? null : Resources.Load<Sprite>($"Gauges/{skin.Dial}_needle");
            needle.enabled = needle.sprite != null;
            if (odometer != null)
            {
                odometer.PlaceInDial(100f - (6 * 12.5f + 5 * 1.5f) / 2f, 132f, 12.5f, 17f, 1.5f);
                odometer.SetColors(skin.DialInk, skin.DialInkLast);
            }
        }

        void OnEnable()
        {
            // İlk karede yüz boşken beyaz kare görünmesin.
            if (shownDial == null) face.enabled = needle.enabled = false;
            Refresh();
        }

        bool Refresh()
        {
            var c = EnvironmentContent.Get(environment);
            if (c == null) return false;
            var skin = c.Skins[Mathf.Max(0, EnvironmentContent.SkinIndex(c, c.SkinId))];
            if (skin.Dial != shownDial) Apply(skin);
            return true;
        }

        void Update()
        {
            if (!Refresh()) return;
            var c = EnvironmentContent.Get(environment);

            var engine = ScoreEngine.Instance;
            bool active = EnvironmentManager.Current.Id == environment;
            float rpm = active && engine != null ? engine.Pace * ReelGaugeView.RpmPerPace : 0f;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            needleVelocity += ((rpm - needleRpm) * stiffness - needleVelocity * damping) * dt;
            float min = GaugeSkins.MinKmh * ReelGaugeView.RpmPerPace, max = GaugeSkins.MaxKmh * ReelGaugeView.RpmPerPace;
            needleRpm = Mathf.Clamp(needleRpm + needleVelocity * dt, min - 2.5f, max + 2.5f);
            if (needle.enabled) needle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GaugeSkins.NeedleZ(needleRpm / ReelGaugeView.RpmPerPace));

            if (idleLabel != null)
            {
                bool idle = engine != null && !engine.IsUserActive;
                if (idleLabel.gameObject.activeSelf != idle) idleLabel.gameObject.SetActive(idle);
            }
            if (odometer != null) odometer.Show(c.MonthUnits);
        }
    }
}
