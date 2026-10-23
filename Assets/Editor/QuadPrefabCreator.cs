using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Right-click one or more textures in the Project window to create ready-to-use
/// Quad prefabs. Each gets an Unlit, Opaque, Alpha Clipping material.
/// Keep this file inside a folder named Editor.
/// </summary>
public static class QuadPrefabCreator
{
    private enum Mode
    {
        BottomPivot,
        CenterPivot,
        BottomPivotBillboard,
        FallingItem
    }

    private const string OutputFolder = "Assets/Prefabs/Quads";

    // 100 pixels = 1 meter. A 512 pixel tall tree becomes 5.12 meters tall.
    private const float DefaultPixelsPerUnit = 100f;

    // Falling items are small, so 1000 pixels = 1 meter. A 300 pixel leaf becomes 0.3 meters.
    private const float ItemPixelsPerUnit = 1000f;

    private const float AlphaCutoff = 0.3f;

    [MenuItem("Assets/Create/2D Quad Prefabs/Standing (bottom pivot)")]
    private static void CreateStanding()
    {
        Create(Mode.BottomPivot);
    }

    [MenuItem("Assets/Create/2D Quad Prefabs/Centered (center pivot)")]
    private static void CreateCentered()
    {
        Create(Mode.CenterPivot);
    }

    [MenuItem("Assets/Create/2D Quad Prefabs/Standing + camera-facing")]
    private static void CreateStandingBillboard()
    {
        Create(Mode.BottomPivotBillboard);
    }

    [MenuItem("Assets/Create/2D Quad Prefabs/Falling item")]
    private static void CreateFallingItem()
    {
        Create(Mode.FallingItem);
    }

    private static void Create(Mode mode)
    {
        Texture2D[] textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        if (textures.Length == 0)
        {
            Debug.LogWarning("Select one or more textures in the Project window first.");
            return;
        }

        EnsureFolder(OutputFolder);

        GameObject lastPrefab = null;

        foreach (Texture2D texture in textures)
        {
            lastPrefab = CreateOne(texture, mode);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (lastPrefab != null)
        {
            EditorGUIUtility.PingObject(lastPrefab);
        }

        Debug.Log("Created " + textures.Length + " prefab(s) in " + OutputFolder);
    }

    private static GameObject CreateOne(Texture2D texture, Mode mode)
    {
        string texturePath = AssetDatabase.GetAssetPath(texture);
        float pixelsPerUnit = PrepareTexture(texturePath);

        if (mode == Mode.FallingItem)
        {
            pixelsPerUnit = ItemPixelsPerUnit;
        }

        Material material = GetOrCreateMaterial(texture, texturePath);

        float width = texture.width / pixelsPerUnit;
        float height = texture.height / pixelsPerUnit;

        GameObject root = new GameObject(texture.name);
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";

        // Colliders are not needed and only add cost.
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = new Vector3(width, height, 1f);
        visual.GetComponent<MeshRenderer>().sharedMaterial = material;

        // Bottom pivot: the root sits at the feet and the art stands on top of it.
        bool bottomPivot = mode == Mode.BottomPivot || mode == Mode.BottomPivotBillboard;
        visual.transform.localPosition = bottomPivot
            ? new Vector3(0f, height * 0.5f, 0f)
            : Vector3.zero;

        if (mode == Mode.BottomPivotBillboard)
        {
            root.AddComponent<BillboardToCamera>();
        }

        if (mode == Mode.FallingItem)
        {
            FallingItem item = root.AddComponent<FallingItem>();

            // The Visual field is private, so set it through the serialized object.
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("visual").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
            OutputFolder + "/" + texture.name + ".prefab");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return prefab;
    }

    // Sets the import options a cutout texture needs. Returns the pixels per unit to use.
    private static float PrepareTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return DefaultPixelsPerUnit;
        }

        bool changed = false;

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

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

        // Textures already set up as sprites keep their own pixels per unit.
        return importer.textureType == TextureImporterType.Sprite
            ? importer.spritePixelsPerUnit
            : DefaultPixelsPerUnit;
    }

    private static Material GetOrCreateMaterial(Texture2D texture, string texturePath)
    {
        string folder = Path.GetDirectoryName(texturePath).Replace('\\', '/');
        string materialPath = folder + "/Mat_" + texture.name + ".mat";

        // Reuse the material if this texture was processed before.
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (existing != null)
        {
            return existing;
        }

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

        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}