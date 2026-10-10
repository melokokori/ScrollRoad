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
