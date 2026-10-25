using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Plak: kapağın arkasındaki parça listesi (A1 A2 B1 B2 — plak 4 parça). Çalan parçanın çubuğu dolar, biten
    // parçanın yanına işaret düşer; dört parça bitince yeni plak gelir, liste boşalır. Bantta bu ayın parça sayısı.
    public class VinylTallyView : TallyView
    {
        [SerializeField] Image body;
        [SerializeField] TMP_Text count;
        [SerializeField] Image[] bars;    // 4; Filled/Horizontal
        [SerializeField] Image[] checks;  // 4

        const int TracksPerRecord = 4;
        const float PopTime = 0.35f, HoldFull = 0.8f;

        long shownCount = -1;
        float popAge = 99f;
        int popRow = -1;

        protected override void ApplySet(string set, SkinInfo skin)
        {
            body.sprite = Load(set, "body");
            count.color = skin.Ink;
            foreach (var b in bars) b.color = skin.Accent;
            foreach (var c in checks) c.color = skin.Accent;
        }

        protected override void OnUnit(long whole)
        {
            popRow = (int)((whole - 1) % TracksPerRecord);
            popAge = 0f;
        }

        protected override void Tick(double units, float dt)
        {
            long whole = (long)System.Math.Floor(units);
            if (whole != shownCount)
            {
                shownCount = whole;
                count.SetText("{0}", whole);
            }
            popAge += dt;

            int row = (int)(whole % TracksPerRecord);
            float fraction = (float)(units - whole);
            // Plak bitince son işaret bir an görünsün, sonra liste boşalsın.
            bool holding = row == 0 && popRow == TracksPerRecord - 1 && popAge < HoldFull;
            for (int i = 0; i < TracksPerRecord; i++)
            {
                bool done = holding || i < row;
                bars[i].fillAmount = done ? 1f : i == row ? fraction : 0f;
                checks[i].enabled = done;
                float pop = i == popRow && popAge < PopTime ? 1f + 0.5f * Mathf.Sin(popAge / PopTime * Mathf.PI) : 1f;
                checks[i].rectTransform.localScale = Vector3.one * pop;
            }
        }
    }
}
