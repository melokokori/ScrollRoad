using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Kapı: güvenlikçinin el sayacı. Her ziyaretçide düğmeye basılır (tık), son hane bir yukarı yuvarlanır.
    // Sayaç tam sayı tutar; geri sararken çıkan ziyaretçiyle birlikte geri düşer.
    public class DoorTallyView : TallyView
    {
        [SerializeField] Image body;
        [SerializeField] Image button;
        [SerializeField] OdometerView odometer;

        const float PressDepth = 5f;    // birim
        const float PressTime = 0.07f, ReleaseTime = 0.16f, RollTime = 0.18f;

        float pressAge = 99f;
        float shown = -1f;

        protected override string UnitSound => "tally_click";

        protected override void ApplySet(string set, SkinInfo skin)
        {
            body.sprite = Load(set, "body");
            button.sprite = Load(set, "button");
            odometer.SetColors(skin.Ink, skin.Ink);
        }

        protected override void OnUnit(long whole) => pressAge = 0f;

        protected override void Tick(double units, float dt)
        {
            pressAge += dt;
            float press = pressAge < PressTime ? pressAge / PressTime
                : Mathf.Clamp01(1f - (pressAge - PressTime) / ReleaseTime);
            button.rectTransform.anchoredPosition = new Vector2(0f, -press * PressDepth * UnitPx);

            // Gösterilen sayı hedefe kısa bir yuvarlanmayla yetişir (Show onda biri yuvarladığı için /10).
            float target = (float)System.Math.Floor(units);
            shown = shown < 0f || target < shown || target - shown > 3f ? target : Mathf.MoveTowards(shown, target, dt / RollTime);
            odometer.Show(shown / 10.0);
        }
    }
}
