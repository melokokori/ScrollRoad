using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ScrollRoad.EditorTools
{
    // Kart arayüzünün yazı tipleri: Overpass (karayolu tabelası yazısı Highway Gothic'ten türetilmiş,
    // SIL OFL — Assets/Fonts/OFL.txt). TTF'lerden TextMeshPro SDF font varlıkları bir kez üretilir.
    // 2026-10-25: SABİT atlas (Static) — eskiden dinamikti ve editörde her yeni karakter çizildiğinde .asset dosyası
    // kendiliğinden değişiyordu. Set: ASCII + oyunun metinlerinde geçen işaretler + Türkçe ve yaygın Latin harfleri.
    // Sette olmayan karakterler (oyuncu adları) çalışma zamanında bellekte kurulan dinamik yedekten çizilir
    // (UiFontFallback; diske yazılmaz). Metinlere yeni bir özel karakter eklenirse buraya eklenip menüden
    // "ScrollRoad/Yazı Tiplerini Yeniden Üret" + "Sahneleri Kur" çalıştırılmalı.
    public static class UiFonts
    {
        const string Folder = "Assets/Fonts";
        const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        const string Symbols = "\u00A0·×–—→←…’‘“”›‹°•"; // ≡ ve ✓ Overpass'ta yok: oyuncu metinlerinde kullanma
        const string Latin = "çğıöşüÇĞİÖŞÜéèêëáàâäíìîïóòôúùûñßÉÈÁÀÂÄÍÓÔÚÑ";
        public const string Characters = Ascii + Symbols + Latin;

        const int AtlasSize = 2048;

        public static readonly string[] Names = { "Overpass-SemiBold", "Overpass-ExtraBold", "OverpassMono-SemiBold" };

        public static TMP_FontAsset SemiBold => Ensure("Overpass-SemiBold");
        public static TMP_FontAsset ExtraBold => Ensure("Overpass-ExtraBold");
        public static TMP_FontAsset Mono => Ensure("OverpassMono-SemiBold");

        public static Font Ttf(string name) => AssetDatabase.LoadAssetAtPath<Font>($"{Folder}/{name}.ttf");

        [MenuItem("ScrollRoad/Yazı Tiplerini Yeniden Üret")]
        public static void Regenerate()
        {
            foreach (var name in Names) AssetDatabase.DeleteAsset($"{Folder}/{name} SDF.asset");
            foreach (var name in Names) Ensure(name);
        }

        static TMP_FontAsset Ensure(string name)
        {
            string assetPath = $"{Folder}/{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = Ttf(name);
            if (font == null)
            {
                Debug.LogError($"Yazı tipi bulunamadı: {Folder}/{name}.ttf");
                return null;
            }

            // Karakterler dinamik modda eklenir, sonra atlas sabitlenir. Tek atlas: ek atlas dokuları alt varlık
            // olarak kaydedilmezse kaybolur.
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
            asset.name = $"{name} SDF";
            AssetDatabase.CreateAsset(asset, assetPath);

            // Atlas ve materyal font varlığının alt varlıkları olarak kaydedilmeli, yoksa sahne yeniden
            // açılınca kaybolurlar.
            asset.atlasTextures[0].name = $"{name} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = $"{name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            if (!asset.TryAddCharacters(Characters, out string missing) && !string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[UiFonts] {name}: yazı tipinde olmayan karakterler (yedekten çizilir): {missing}");
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
