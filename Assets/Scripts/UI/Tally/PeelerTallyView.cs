using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Soyacak: eski tuşlu yazar kasa. Pencerede bu ayın meyvesi (üç hane); her soyulan meyvede çekmece "çın" diye
    // açılır, içindeki paralar görünür, sonra kapanır.
    public class PeelerTallyView : TallyView
    {
        [SerializeField] Image inside;
        [SerializeField] Image body;
        [SerializeField] Image drawer;
        [SerializeField] TMP_Text count;

        const float OpenDepth = 16f;   // birim
        const float OpenTime = 0.07f, HoldTime = 0.45f, CloseTime = 0.35f;

        long shownCount = -1;
        float openAge = 99f;

        protected override string UnitSound => "register_ding";

        protected override void ApplySet(string set, SkinInfo skin)
        {
            inside.sprite = Load(set, "inside");
            body.sprite = Load(set, "body");
            drawer.sprite = Load(set, "drawer");
            count.color = skin.Ink;
        }

        protected override void OnUnit(long whole) => openAge = 0f;

        protected override void Tick(double units, float dt)
        {
            long whole = (long)System.Math.Floor(units);
            if (whole != shownCount)
            {
                shownCount = whole;
                count.SetText("{0:000}", whole % 1000);
            }
            openAge += dt;
            float open = openAge < OpenTime ? openAge / OpenTime
                : openAge < OpenTime + HoldTime ? 1f
                : Mathf.Clamp01(1f - (openAge - OpenTime - HoldTime) / CloseTime);
            open = Mathf.SmoothStep(0f, 1f, open);
            drawer.rectTransform.anchoredPosition = new Vector2(0f, -open * OpenDepth * UnitPx);
            drawer.rectTransform.localScale = Vector3.one * (1f + 0.04f * open);
        }
    }
}
