using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Çömlek: kil çuvalı. Çarktaki çömlek büyüdükçe çuval söner (kesirle); çömlek bitince yeni kil gelir, çuval
    // yaylanarak dolar. Etikette bu ayın çömlek sayısı.
    public class PotteryTallyView : TallyView
    {
        [SerializeField] Image bag;       // pivot alt orta; sayı etiketiyle birlikte ölçeklenir
        [SerializeField] TMP_Text count;

        const float Sag = 0.26f, Spread = 0.07f;
        const float Stiffness = 170f, Damping = 9f;

        long shownCount = -1;
        float fill = 1f, fillVelocity;

        protected override void ApplySet(string set, SkinInfo skin)
        {
            bag.sprite = Load(set, "bag");
            count.color = skin.Ink;
        }

        protected override void Tick(double units, float dt)
        {
            long whole = (long)System.Math.Floor(units);
            if (whole != shownCount)
            {
                shownCount = whole;
                count.SetText("{0}", whole);
            }
            // Hedef doluluk kesirle iner; birim gelince 1'e sıçrar, yay sönümlü aşarak oturur.
            float target = 1f - (float)(units - whole);
            fillVelocity += ((target - fill) * Stiffness - fillVelocity * Damping) * dt;
            fill += fillVelocity * dt;
            float empty = 1f - fill;
            bag.rectTransform.localScale = new Vector3(1f + Spread * empty, 1f - Sag * empty, 1f);
        }
    }
}
