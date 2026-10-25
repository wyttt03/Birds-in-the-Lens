using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Right-click one or more textures (or a folder of textures) in the Project window
/// to create a ready-to-use material for each one: URP Unlit, Opaque, Alpha Clipping.
/// It also sets the import options a cutout texture needs.
/// Keep this file inside a folder named Editor.
/// </summary>
public static class TextureMaterialCreator
{
    private const string MenuPath = "Assets/Create/2D Materials/From Selected Textures";

    // Pixels with alpha below this value are cut away.
    private const float AlphaCutoff = 0.3f;

    // The menu item is only enabled while at least one texture is selected.
    [MenuItem(MenuPath, true)]
    private static bool ValidateCreateMaterials()
    {
        return Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets).Length > 0;
    }

    [MenuItem(MenuPath)]
    private static void CreateMaterials()
    {
        Texture2D[] textures = Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets);

        int created = 0;
        int skipped = 0;
        Material lastCreated = null;

        foreach (Texture2D selected in textures)
        {
            string texturePath = AssetDatabase.GetAssetPath(selected);

            // Ignore textures that live inside packages.
            if (!texturePath.StartsWith("Assets/"))
            {
                continue;
            }

            PrepareTexture(texturePath);

            // Reload, because the texture is reimported when its settings change.
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            string folder = Path.GetDirectoryName(texturePath).Replace('\\', '/');
            string materialPath = folder + "/Mat_" + texture.name + ".mat";

            // Never overwrite a material that already exists.
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
            {
                skipped++;
                continue;
            }

            Material material = BuildMaterial(texture);

            if (material == null)
            {
                return;
            }

            AssetDatabase.CreateAsset(material, materialPath);
            lastCreated = material;
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (lastCreated != null)
        {
            EditorGUIUtility.PingObject(lastCreated);
        }

        Debug.Log(
            "Materials created: " + created +
            ". Already existed and skipped: " + skipped + ".");
    }

    // Sets the import options a cutout texture needs.
    private static void PrepareTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        bool changed = false;

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        // Mip maps blend neighboring pixels and cause colored edges on cutouts.
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static Material BuildMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            Debug.LogError("The URP Unlit shader was not found. Is this a URP project?");
            return null;
        }

        Material material = new Material(shader);
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);

        // Opaque with alpha clipping, so the art writes depth and works with Depth Of Field.
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", AlphaCutoff);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = (int)RenderQueue.AlphaTest;

        return material;
    }
}