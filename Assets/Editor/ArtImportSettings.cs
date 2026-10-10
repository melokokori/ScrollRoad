using UnityEditor;

namespace ScrollRoad.EditorTools
{
    // Assets/Art altındaki görseller düz vektör tarzında (pixel art değil): Sprite, bilinear,
    // mipmapsiz, yüksek kalite sıkıştırma. Elle import ayarı yapmaya gerek kalmasın diye zorlanır.
    public class ArtImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Art/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
        }
    }
}
