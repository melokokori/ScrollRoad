using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ScrollRoad.EditorTools
{
    // Kart arayüzünün yazı tipleri: Overpass (karayolu tabelası yazısı Highway Gothic'ten türetilmiş,
    // SIL OFL — Assets/Fonts/OFL.txt). TTF'lerden TextMeshPro SDF font varlıkları bir kez üretilir.
    // Dinamik atlas: oyuncu adları (sıralama) gibi önceden bilinmeyen karakterler de çizilebilsin.
    public static class UiFonts
    {
        const string Folder = "Assets/Fonts";
        const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·×–—→…’";

        public static TMP_FontAsset SemiBold => Ensure("Overpass-SemiBold");
        public static TMP_FontAsset ExtraBold => Ensure("Overpass-ExtraBold");
        public static TMP_FontAsset Mono => Ensure("OverpassMono-SemiBold");

        static TMP_FontAsset Ensure(string name)
        {
            string assetPath = $"{Folder}/{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{Folder}/{name}.ttf");
            if (font == null)
            {
                Debug.LogError($"Yazı tipi bulunamadı: {Folder}/{name}.ttf");
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = $"{name} SDF";
            AssetDatabase.CreateAsset(asset, assetPath);

            // Atlas ve materyal font varlığının alt varlıkları olarak kaydedilmeli, yoksa sahne yeniden
            // açılınca kaybolurlar.
            asset.atlasTextures[0].name = $"{name} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = $"{name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            asset.TryAddCharacters(Ascii);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
