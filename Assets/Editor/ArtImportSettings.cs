using System.Text.RegularExpressions;
using UnityEditor;

namespace ScrollRoad.EditorTools
{
    // Assets/Art altındaki görseller düz vektör tarzında (pixel art değil): Sprite, bilinear,
    // mipmapsiz, yüksek kalite sıkıştırma. Elle import ayarı yapmaya gerek kalmasın diye zorlanır.
    // Assets/Art/UI: sıkıştırmasız (küçük köşeler ve ikonlar bulanıklaşmasın); adı "_b12" gibi
    // bitenler 9 parçalı (sliced) sprite, sayı her kenarın piksel payı.
    public class ArtImportSettings : AssetPostprocessor
    {
        static readonly Regex BorderSuffix = new Regex(@"_b(\d+)\.png$");

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');

            // İbre skinleri (Tools/make_gauges.py, Resources'tan yüklenir): kartta ~130 px, görev
            // çubuğunda ~40 px çizildikleri için mipmap şart; dokular 2'nin kuvveti, sıkıştırılabilir.
            // Ortam nesneleri (Tools/make_tallies.py, 2026-10-25) aynı yuvada aynı boyutta çizilir.
            if (path.StartsWith("Assets/Resources/Gauges/") || path.StartsWith("Assets/Resources/Tallies/"))
            {
                var gauge = (TextureImporter)assetImporter;
                gauge.textureType = TextureImporterType.Sprite;
                gauge.spriteImportMode = SpriteImportMode.Single;
                gauge.mipmapEnabled = true;
                // Kaiser + hafif negatif bias: Box mipleri ve trilinear karışım 512 px'lik kadranı
                // görev çubuğunda (44-88 px) bulanıklaştırıyordu.
                gauge.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                gauge.mipMapBias = -0.5f;
                gauge.alphaIsTransparency = true;
                gauge.filterMode = UnityEngine.FilterMode.Trilinear;
                gauge.textureCompression = TextureImporterCompression.CompressedHQ;
                gauge.wrapMode = UnityEngine.TextureWrapMode.Clamp;
                gauge.spritePixelsPerUnit = 100f;
                return;
            }

            if (!path.StartsWith("Assets/Art/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;

            // Araç görselleri burada değil: StreamingAssets/Vehicles, VehicleArt çalışma zamanında yükler.

            if (!path.StartsWith("Assets/Art/UI/")) return;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var match = BorderSuffix.Match(path);
            if (match.Success)
            {
                float b = float.Parse(match.Groups[1].Value);
                importer.spriteBorder = new UnityEngine.Vector4(b, b, b, b);
            }
        }
    }
}
