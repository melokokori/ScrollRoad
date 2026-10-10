using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Yuvarlanan kilometre sayacı: 6 hücre (5 tam + 1 ondalık). Hücre arka planları skin görselinde
    // çizili (kadran yüzü ya da görev çubuğu plakası); burada yalnız maskeli rakamlar var.
    // Ondalık hane sürekli döner; üstteki bir hane yalnız altındaki tüm haneler 9'dayken, ondalık
    // haneyle birlikte döner (elde taşıması). Geri giderken aynı hesap ters yuvarlar.
    public class OdometerView : MonoBehaviour
    {
        [SerializeField] RectTransform[] cells;   // soldan sağa; her biri RectMask2D
        [SerializeField] TMP_Text[] current;
        [SerializeField] TMP_Text[] next;         // hücrenin altından gelen bir sonraki rakam

        static readonly string[] Digits = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        readonly int[] shown = { -1, -1, -1, -1, -1, -1 };

        public int CellCount => cells.Length;

        // Kadran içinde kullanılırken hücreler kadranın 200 birimlik uzayına göre yerleşir.
        public void PlaceInDial(float ox, float oy, float cw, float ch, float gap)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                float x = ox + i * (cw + gap);
                cells[i].anchorMin = new Vector2(x / 200f, 1f - (oy + ch) / 200f);
                cells[i].anchorMax = new Vector2((x + cw) / 200f, 1f - oy / 200f);
                cells[i].offsetMin = cells[i].offsetMax = Vector2.zero;
            }
        }

        public void SetColors(Color digits, Color lastDigit)
        {
            for (int i = 0; i < current.Length; i++)
            {
                var c = i == current.Length - 1 ? lastDigit : digits;
                current[i].color = c;
                next[i].color = c;
            }
        }

        public void SetFontSize(float size)
        {
            for (int i = 0; i < current.Length; i++)
            {
                current[i].fontSize = size;
                next[i].fontSize = size;
            }
        }

        public void Show(double km)
        {
            int n = cells.Length;
            double tenths = System.Math.Max(0.0, km) * 10.0;
            float roll = (float)(tenths - System.Math.Floor(tenths));
            long whole = (long)System.Math.Floor(tenths);
            long below = 1; // 10^j: hanenin altındaki haneler bu sayıyı oluşturur
            for (int j = 0; j < n; j++)
            {
                int i = n - 1 - j;
                int digit = (int)(whole / below % 10);
                bool rolling = j == 0 || whole % below == below - 1;
                if (shown[i] != digit)
                {
                    shown[i] = digit;
                    current[i].SetText(Digits[digit]);
                    next[i].SetText(Digits[(digit + 1) % 10]);
                }
                float h = cells[i].rect.height;
                float shift = Mathf.Round((rolling ? roll : 0f) * h * 2f) / 2f; // yarım piksele yuvarla: metin titremesin
                Offset(current[i].rectTransform, shift);
                Offset(next[i].rectTransform, shift - h);
                below *= 10;
            }
        }

        static void Offset(RectTransform rect, float y)
        {
            rect.offsetMin = new Vector2(0f, y);
            rect.offsetMax = new Vector2(0f, y);
        }
    }
}
