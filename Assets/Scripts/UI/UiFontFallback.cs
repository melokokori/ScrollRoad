using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ScrollRoad
{
    // Arayüz yazı tiplerinin çalışma zamanı yedeği (2026-10-25): Overpass SDF varlıkları sabit karakter setiyle
    // üretilir (Editor/UiFonts) ve diskte değişmez. Sette olmayan karakterler (sıralamadaki oyuncu adları gibi)
    // için açılışta aynı TTF'lerden bellekte dinamik bir font varlığı kurulur ve sabit varlığın yedek listesine
    // eklenir; kapanışta çıkarılır (editörde asset dosyasına yazılmasın). Diziler sahne kurucusunda eşleşik sırayla.
    // 2026-10-26 (diller): Overpass Latin + Kiril kapsar; Çince/Japonca/Korece için Windows'un kendi yazı tipleri
    // (Microsoft YaHei, Yu Gothic, Malgun Gothic — her Windows 10/11'de var) dinamik yedek olur, build büyümez.
    // Han karakterleri dile göre farklı çizildiği için sıra seçili dile göre: Japoncada önce Yu Gothic, Korecede
    // önce Malgun, diğerlerinde önce YaHei (dil seçicideki "简体中文/日本語/한국어" her dilde görünür).
    [DefaultExecutionOrder(-300)]
    public class UiFontFallback : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset[] fonts;
        [SerializeField] Font[] sources;

        // fonts dizisiyle aynı sıra: SemiBold, ExtraBold, Mono. Kalın olan yalnız ExtraBold.
        static readonly bool[] Bold = { false, true, false };

        enum Cjk { Chinese, Japanese, Korean }

        static string FontFile(Cjk script, bool bold) => script switch
        {
            Cjk.Chinese => bold ? "msyhbd.ttc" : "msyh.ttc",
            Cjk.Japanese => bold ? "YuGothB.ttc" : "YuGothM.ttc",
            _ => bold ? "malgunbd.ttf" : "malgun.ttf",
        };

        TMP_FontAsset[] fallbacks;
        readonly Dictionary<(Cjk, bool), TMP_FontAsset> cjk = new Dictionary<(Cjk, bool), TMP_FontAsset>();

        void Awake()
        {
            fallbacks = new TMP_FontAsset[fonts.Length];
            for (int i = 0; i < fonts.Length && i < sources.Length; i++)
            {
                if (fonts[i] == null || sources[i] == null) continue;
                var dynamic = TMP_FontAsset.CreateFontAsset(sources[i]);
                if (dynamic == null) continue;
                dynamic.name = fonts[i].name + " (runtime fallback)";
                fonts[i].fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                fonts[i].fallbackFontAssetTable.Add(dynamic);
                fallbacks[i] = dynamic;
            }
            ApplyCjkOrder();
            L.Changed += ApplyCjkOrder;
        }

        // Her Overpass varlığının yedek listesi: [Overpass dinamik] + CJK yazı tipleri seçili dilin sırasıyla.
        void ApplyCjkOrder()
        {
            Cjk[] order = L.Code switch
            {
                "ja" => new[] { Cjk.Japanese, Cjk.Chinese, Cjk.Korean },
                "ko" => new[] { Cjk.Korean, Cjk.Chinese, Cjk.Japanese },
                _ => new[] { Cjk.Chinese, Cjk.Japanese, Cjk.Korean },
            };
            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] == null) continue;
                var table = fonts[i].fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                foreach (var asset in cjk.Values) table.Remove(asset);
                bool bold = i < Bold.Length && Bold[i];
                foreach (var script in order)
                {
                    var asset = CjkAsset(script, bold);
                    if (asset != null) table.Add(asset);
                }
            }
        }

        TMP_FontAsset CjkAsset(Cjk script, bool bold)
        {
            if (cjk.TryGetValue((script, bold), out var asset)) return asset;
            string path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts), FontFile(script, bold));
            if (!File.Exists(path) && bold) return CjkAsset(script, false); // kalını yoksa normali
            if (File.Exists(path))
            {
                asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (asset != null)
                {
                    asset.name = $"{Path.GetFileNameWithoutExtension(path)} (system fallback)";
                    asset.isMultiAtlasTexturesEnabled = true;
                }
            }
            if (asset == null) Debug.LogWarning($"[UiFontFallback] Sistem yazı tipi yok: {path}");
            cjk[(script, bold)] = asset;
            return asset;
        }

        void OnDestroy()
        {
            L.Changed -= ApplyCjkOrder;
            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] == null) continue;
                foreach (var asset in cjk.Values) if (asset != null) fonts[i].fallbackFontAssetTable?.Remove(asset);
            }
            foreach (var asset in cjk.Values) if (asset != null) Destroy(asset);
            cjk.Clear();
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
