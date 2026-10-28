#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Vesperhold.EditorTools
{
    // Keeps phone memory and download size down: textures under Resources/Art get sensible sizes and, on Android and
    // iOS, ASTC compression. Pixel-art characters stay uncompressed with point filtering so they stay crisp.
    public class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.mipmapEnabled = false; ti.isReadable = false; ti.alphaIsTransparency = true;
            bool pixel = assetPath.Contains("/Characters/warrior") || assetPath.Contains("/Characters/mage");
            int max = assetPath.Contains("/Backdrops/") || assetPath.Contains("/UI/title_art") ? 1024
                : assetPath.Contains("/Portraits/") ? 512 : assetPath.Contains("/VFX/") ? 192 : assetPath.Contains("/Monsters/") ? 512 : 1024;
            ti.maxTextureSize = max;
            ti.filterMode = pixel ? FilterMode.Point : FilterMode.Bilinear;
            if (pixel) { ti.textureCompression = TextureImporterCompression.Uncompressed; return; }
            ti.textureCompression = TextureImporterCompression.Compressed;
            foreach (var platform in new[] { "Android", "iOS" })
            {
                var ps = ti.GetPlatformTextureSettings(platform);
                ps.overridden = true; ps.maxTextureSize = max; ps.format = TextureImporterFormat.ASTC_6x6;
                ti.SetPlatformTextureSettings(ps);
            }
        }
    }
}
#endif
