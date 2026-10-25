using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Dünya: yaprak takvim. Sayfada bu ayın yılı; sıradaki yıla yaklaştıkça sayfanın köşesi kıvrılır, yıl
    // dolunca eski yaprak koparılıp savrulur.
    public class GlobeTallyView : TallyView
    {
        [SerializeField] Image body;
        [SerializeField] Image binding;
        [SerializeField] TMP_Text count;
        [SerializeField] Image curl;            // pivot sayfanın sağ alt köşesi; kesirle büyür
        [SerializeField] RectTransform flyer;   // koparılan yaprak (Image + sayı)
        [SerializeField] Image flyerPage;
        [SerializeField] TMP_Text flyerCount;
        [SerializeField] CanvasGroup flyerGroup;

        const float FlyTime = 0.75f;

        long shownCount = -1;
        float flyAge = 99f;

        protected override void ApplySet(string set, SkinInfo skin)
        {
            body.sprite = Load(set, "body");
            binding.sprite = Load(set, "binding");
            curl.sprite = Load(set, "curl");
            flyerPage.sprite = Load(set, "page");
            count.color = flyerCount.color = skin.Ink;
        }

        protected override void OnUnit(long whole)
        {
            flyerCount.SetText("{0}", whole - 1);
            flyAge = 0f;
        }

        protected override void Tick(double units, float dt)
        {
            long whole = (long)System.Math.Floor(units);
            if (whole != shownCount)
            {
                shownCount = whole;
                count.SetText("{0}", whole);
            }

            flyAge += dt;
            bool flying = flyAge < FlyTime;
            flyer.gameObject.SetActive(flying);
            if (flying)
            {
                float t = flyAge / FlyTime;
                float e = t * t;
                flyer.anchoredPosition = new Vector2(36f * e, 22f * t - 70f * e) * UnitPx;
                flyer.localRotation = Quaternion.Euler(0f, 0f, -38f * e);
                flyerGroup.alpha = 1f - Mathf.Clamp01((t - 0.45f) / 0.55f);
            }

            float fraction = flying ? 0f : (float)(units - whole);
            // Kıvrım son üçte birde belirir (yeni sayfa hemen kıvrılmasın).
            float s = Mathf.Clamp01((fraction - 0.35f) / 0.65f);
            curl.enabled = s > 0.01f;
            curl.rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }
}
