using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Arayüz yazı tiplerinin çalışma zamanı yedeği (2026-10-25): Overpass SDF varlıkları sabit karakter setiyle
    // üretilir (Editor/UiFonts) ve diskte değişmez. Sette olmayan karakterler (sıralamadaki oyuncu adları gibi)
    // için açılışta aynı TTF'lerden bellekte dinamik bir font varlığı kurulur ve sabit varlığın yedek listesine
    // eklenir; kapanışta çıkarılır (editörde asset dosyasına yazılmasın). Diziler sahne kurucusunda eşleşik sırayla.
    [DefaultExecutionOrder(-300)]
    public class UiFontFallback : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset[] fonts;
        [SerializeField] Font[] sources;

        TMP_FontAsset[] fallbacks;

        void Awake()
        {
            fallbacks = new TMP_FontAsset[fonts.Length];
            for (int i = 0; i < fonts.Length && i < sources.Length; i++)
            {
                if (fonts[i] == null || sources[i] == null) continue;
                var dynamic = TMP_FontAsset.CreateFontAsset(sources[i]);
                if (dynamic == null) continue;
                dynamic.name = fonts[i].name + " (runtime fallback)";
                fonts[i].fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
                fonts[i].fallbackFontAssetTable.Add(dynamic);
                fallbacks[i] = dynamic;
            }
        }

        void OnDestroy()
        {
            if (fallbacks == null) return;
            for (int i = 0; i < fallbacks.Length; i++)
            {
                if (fallbacks[i] == null) continue;
                fonts[i].fallbackFontAssetTable?.Remove(fallbacks[i]);
                Destroy(fallbacks[i]);
            }
        }
    }
}
