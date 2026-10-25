using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Kebap: büfenin "NOW SERVING" sıra numaratörü. Kırmızı (takıma göre) yedi segmentli üç hane bu ayın porsiyonu;
    // her porsiyonda numara bir atlar, hane iki kez yanıp söner ve "dıt" der. Sönük "888" arka planda hep görünür.
    public class KebabTallyView : TallyView
    {
        [SerializeField] Image body;
        [SerializeField] Image[] digits;  // 3; soldan sağa
        [SerializeField] Image[] ghosts;  // 3; sönük 8

        const float BlinkTime = 0.42f;

        static Sprite[] ledSprites;
        long shown = -1;
        float blinkAge = 99f;
        Color led;

        protected override string UnitSound => "ticket_ding";

        protected override void ApplySet(string set, SkinInfo skin)
        {
            body.sprite = Load(set, "body");
            led = skin.Ink;
            foreach (var g in ghosts) g.color = new Color(led.r, led.g, led.b, 0.07f);
        }

        protected override void OnUnit(long whole) => blinkAge = 0f;

        protected override void Tick(double units, float dt)
        {
            ledSprites ??= LoadDigits();
            long n = (long)System.Math.Floor(units) % 1000;
            if (n != shown)
            {
                shown = n;
                for (int i = 0; i < digits.Length; i++)
                {
                    long d = n / (long)Mathf.Pow(10, digits.Length - 1 - i) % 10;
                    digits[i].sprite = ledSprites[d];
                }
            }
            blinkAge += dt;
            // İki kısa sönme: 0–0,08 ve 0,16–0,24 sn.
            bool off = blinkAge < BlinkTime && (blinkAge < 0.08f || (blinkAge > 0.16f && blinkAge < 0.24f));
            foreach (var d in digits) d.color = off ? Color.clear : led;
        }

        static Sprite[] LoadDigits()
        {
            var sprites = new Sprite[10];
            for (int i = 0; i < 10; i++) sprites[i] = Shared($"led_{i}");
            return sprites;
        }
    }
}
