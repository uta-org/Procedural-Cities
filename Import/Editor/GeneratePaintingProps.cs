using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds five low-poly wall paintings for hallway decoration. Each painting
/// is a wooden frame (backing box) + a Quad textured with a real, public-
/// domain artwork cropped to a 128x128 square (Van Gogh, Hokusai, Monet,
/// Hiroshige, Turner — all authors died 70-100+ years ago, safely public
/// domain worldwide; see Import/Models/LowPoly/Textures/PaintingsOriginal
/// for the un-cropped source downloads and their Wikimedia Commons origin).
/// The 128x128 crops live in Import/Models/LowPoly/Textures/Paintings128 and
/// are picked up by Unity's normal PNG importer — this script only sets their
/// import settings and builds/deploys the frame prefabs to
/// Resources/Prefabs/AssetContents (same convention as the other
/// Generate*Prop.cs scripts in this folder).
/// Menu: Tools / Procedural Cities / Generate Painting Props
/// </summary>
public static class GeneratePaintingProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";
    private const string TexDir = PkgRoot + "/Models/LowPoly/Textures/Paintings128";

    private const float PaintingSize = 0.42f;
    private const float FrameSize = 0.48f;

    private static readonly (string prefab, string texFile)[] Paintings =
    {
        ("Painting1", "painting1_starry_night_128"),
        ("Painting2", "painting2_great_wave_128"),
        ("Painting3", "painting3_impression_sunrise_128"),
        ("Painting4", "painting4_hiroshige_128"),
        ("Painting5", "painting5_fighting_temeraire_128"),
    };

    [MenuItem("Tools/Procedural Cities/Generate Painting Props")]
    public static void Generate()
    {
        RemoveStaleProceduralAssets();

        var matFrame = GetSharedMat("LP_Painting_Frame", new Color(0.24f, 0.15f, 0.08f), 0.05f, 0.25f);

        foreach (var (prefabName, texFile) in Paintings)
            SaveOne(prefabName, texFile, matFrame);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GeneratePaintingProps] Saved {Paintings.Length} painting prefabs under {PrefabDir}");
    }

    private static void SaveOne(string prefabName, string texFile, Material matFrame)
    {
        var texPath = $"{TexDir}/{texFile}.png";
        ConfigureImport(texPath);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            Debug.LogError($"[GeneratePaintingProps] Missing texture at {texPath} — run the Wikimedia "
                + "Commons download + resize step first (see PaintingsOriginal/Paintings128 folders).");
            return;
        }

        var matPath = $"{MatDir}/{texFile}_Mat.mat";
        var matPicture = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (matPicture == null)
        {
            matPicture = new Material(
                Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"))
            {
                name = texFile + "_Mat",
            };
            AssetDatabase.CreateAsset(matPicture, matPath);
        }
        matPicture.mainTexture = tex;
        EditorUtility.SetDirty(matPicture);

        var root = BuildFrame(prefabName, matFrame, matPicture);
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{prefabName}.prefab");
        Object.DestroyImmediate(root);
    }

    private static void ConfigureImport(string texPath)
    {
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 128;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static GameObject BuildFrame(string rootName, Material matFrame, Material matPicture)
    {
        var root = new GameObject(rootName);

        var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(backing.GetComponent<Collider>());
        backing.name = "Frame";
        backing.transform.SetParent(root.transform, false);
        backing.transform.localScale = new Vector3(FrameSize, FrameSize, 0.03f);
        backing.GetComponent<MeshRenderer>().sharedMaterial = matFrame;

        var picture = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(picture.GetComponent<Collider>());
        picture.name = "Picture";
        picture.transform.SetParent(root.transform, false);
        picture.transform.localPosition = new Vector3(0, 0, -0.016f);
        picture.transform.localScale = new Vector3(PaintingSize, PaintingSize, 1f);
        picture.GetComponent<MeshRenderer>().sharedMaterial = matPicture;

        var collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(FrameSize, FrameSize, 0.05f);

        return root;
    }

    /// <summary>
    /// Removes the Texture2D .asset files and materials from the earlier,
    /// procedurally-painted version of this prop (superseded by the real
    /// public-domain artwork above) so they don't linger as orphaned assets.
    /// </summary>
    private static void RemoveStaleProceduralAssets()
    {
        var staleDir = PkgRoot + "/Models/LowPoly/Textures";
        for (var i = 1; i <= 5; i++)
        {
            var stalePath = $"{staleDir}/LP_Painting_Tex{i}.asset";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(stalePath) != null)
                AssetDatabase.DeleteAsset(stalePath);
        }
    }

    private static Material GetSharedMat(string name, Color color, float metallic, float smoothness)
    {
        var path = $"{MatDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name, color = color };
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
