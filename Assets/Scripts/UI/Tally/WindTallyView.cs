using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Rüzgârgülü: eski tip (Ferraris) elektrik sayacı. Sayaç bu ayın kWh'ını yuvarlar (kırmızı hane onda bir);
    // yarıktan görünen alüminyum diskin siyah işareti üretim hızıyla döner — seyirde ~0,75 tur/sn, rölantide yavaş,
    // geri sararken ters.
    public class WindTallyView : TallyView
    {
        [SerializeField] Image body;
        [SerializeField] OdometerView odometer;
        [SerializeField] RectTransform diskMark; // disk yarığının içinde (maskeli), yuvanın 200 birimlik uzayında

        const float DiskLeft = 60f, DiskRight = 140f, MarkWidth = 6f;
        const float RevPerPace = 0.75f / 60f;

        float angle;

        protected override void ApplySet(string set, SkinInfo skin)
        {
            body.sprite = Load(set, "body");
            odometer.SetColors(skin.Ink, skin.Accent);
        }

        protected override void Tick(double units, float dt)
        {
            angle = Mathf.Repeat(angle + Pace * RevPerPace * dt * Mathf.PI * 2f, Mathf.PI * 2f);
            // Disk yandan görülür: işaret ön yüzdeyken soldan sağa kayar, kenarlarda incelir; arkadayken görünmez.
            float front = Mathf.Sin(angle);
            float center = (DiskLeft + DiskRight) / 2f - Mathf.Cos(angle) * ((DiskRight - DiskLeft) / 2f - MarkWidth / 2f);
            float width = MarkWidth * Mathf.Max(0f, front);
            diskMark.gameObject.SetActive(front > 0.05f);
            TallyLayout.Place(diskMark, center - width / 2f, 103f, center + width / 2f, 111f);
            odometer.Show(units);
        }
    }
}
