using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Olta: balık kovası (Bambu takımında hasır sepet). Üstüne tebeşirle bu ayın balığı yazılı; tutulan balıkların
    // kuyrukları sudan çıkar (en çok üç), su ve kuyruklar sarma hızıyla çalkalanır. Balık tutulunca su sıçrar,
    // yeni kuyruk belirir.
    public class FishingTallyView : TallyView
    {
        [SerializeField] Image back;
        [SerializeField] Image water;     // pivot su yüzeyinin ortası
        [SerializeField] Image front;
        [SerializeField] Image[] tails;   // 3; pivot kuyruk sapı
        [SerializeField] Image[] drops;   // sıçrama damlaları
        [SerializeField] TMP_Text count;

        static readonly Color TailColor = new Color(0.66f, 0.73f, 0.77f);
        static readonly Color DropColor = new Color(0.78f, 0.9f, 0.97f);
        const float PopTime = 0.4f, SplashTime = 0.55f;

        long shownCount = -1;
        float popAge = 99f, slosh;

        protected override void ApplySet(string set, SkinInfo skin)
        {
            back.sprite = Load(set, "back");
            water.sprite = Load(set, "water");
            front.sprite = Load(set, "front");
            count.color = skin.Ink;
            var tail = Shared("bucket_tail");
            foreach (var t in tails)
            {
                t.sprite = tail;
                t.color = TailColor;
            }
            foreach (var d in drops) d.color = DropColor;
        }

        protected override void OnUnit(long whole) => popAge = 0f;

        protected override void Tick(double units, float dt)
        {
            long whole = (long)System.Math.Floor(units);
            if (whole != shownCount)
            {
                shownCount = whole;
                count.SetText("{0}", whole);
            }
            popAge += dt;

            // Çalkalanma sarma hızıyla: rölantide neredeyse durgun, boost'ta belirgin.
            float energy = 0.15f + Pace01 * 2.2f;
            slosh += dt * (1.5f + 6f * Pace01);
            water.rectTransform.localScale = new Vector3(1f, 1f + 0.12f * energy * Mathf.Sin(slosh * 2.1f), 1f);

            int visible = (int)System.Math.Min(whole, tails.Length);
            for (int i = 0; i < tails.Length; i++)
            {
                tails[i].enabled = i < visible;
                bool newest = i == visible - 1 && popAge < PopTime;
                float pop = newest ? Mathf.Sin(popAge / PopTime * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) : 1f;
                tails[i].rectTransform.localScale = new Vector3(pop, pop, 1f);
                float wag = Mathf.Sin(slosh * (2.6f + i * 0.7f) + i * 1.9f) * (4f + 9f * energy);
                tails[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * -12f + wag);
            }

            bool splashing = popAge < SplashTime;
            float t = popAge / SplashTime;
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i].enabled = splashing;
                if (!splashing) continue;
                float dir = (i - (drops.Length - 1) / 2f) * 14f;
                drops[i].rectTransform.anchoredPosition = new Vector2(dir * t, 34f * t - 40f * t * t) * UnitPx;
                drops[i].color = new Color(DropColor.r, DropColor.g, DropColor.b, 1f - t);
            }
        }
    }
}
