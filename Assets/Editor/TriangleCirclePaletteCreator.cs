using System.IO;
using UnityEditor;
using UnityEngine;

public static class TriangleCirclePaletteCreator
{
    private const string OutputDirectory = "Assets/Sprites/UI/Palette";
    private const string OutputPath = OutputDirectory + "/TriangleCirclePalette_8.png";

    [MenuItem("Tools/Tech2D/Create Triangle Circle Palette (1x8)")]
    public static void CreatePaletteTexture()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        // 8 базовых синих оттенков. Потом можешь заменить вручную на свои.
        Color32[] shades =
        {
            new Color32(7, 18, 51, 255),
            new Color32(11, 30, 77, 255),
            new Color32(16, 45, 102, 255),
            new Color32(21, 61, 128, 255),
            new Color32(28, 80, 153, 255),
            new Color32(36, 102, 179, 255),
            new Color32(48, 128, 204, 255),
            new Color32(66, 158, 230, 255)
        };

        var texture = new Texture2D(8, 1, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int i = 0; i < shades.Length; i++)
        {
            texture.SetPixel(i, 0, shades[i]);
        }

        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(OutputPath, png);
        Object.DestroyImmediate(texture);

        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        var created = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputPath);
        if (created != null)
        {
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        EditorUtility.DisplayDialog(
            "Palette Created",
            "Создана палитра 1x8: " + OutputPath + "\n\nНазначь её в _PaletteTex и поставь _PaletteCount = 8.",
            "OK");
    }
}
