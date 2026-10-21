using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the low-poly wall paintings: a wooden frame (backing box) + a Quad
/// textured with a real, public-domain artwork (every author died 100+ years
/// ago; the files are marked public domain on Wikimedia Commons). The
/// un-cropped downloads are kept in
/// Import/Models/LowPoly/Textures/PaintingsOriginal~ (out of Unity's import by
/// the trailing ~); the crops, cut to each painting's format, live in
/// Import/Models/LowPoly/Textures/Paintings and are picked up by Unity's normal
/// PNG importer — this script only sets their import settings and
/// builds/deploys the frame prefabs to Resources/Prefabs/AssetContents (same
/// convention as the other Generate*Prop.cs scripts in this folder).
/// Formats are in units of the original painting (a 0.48 m frame, a 128 px
/// texture): 1x1 ("64x64"), 2x2, 4x4, 1x2 and 2x4 (portrait), 2x1 and 4x2
/// (landscape), at least three artworks each; the runtime's PaintingCatalog
/// lists the same ids and formats. New prefabs also need an entry (address =
/// prefab name) in the main project's Remote Addressables group.
/// Menu: Tools / Procedural Cities / Generate Painting Props
/// </summary>
public static class GeneratePaintingProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";
    private const string TexDir = PkgRoot + "/Models/LowPoly/Textures/Paintings";

    /// <summary>Frame side of a 1-unit painting (m).</summary>
    private const float UnitFrameSize = 0.48f;

    /// <summary>Wooden border around the picture, each side (m).</summary>
    private const float FrameBorder = 0.03f;

    /// <summary>Frame depth (m).</summary>
    private const float FrameDepth = 0.03f;

    /// <summary>How far in front of the frame's centre the picture sits (m): just past its face.</summary>
    private const float PictureOffset = 0.016f;

    /// <summary>Texture pixels per unit of painting (the original 128 px crops).</summary>
    private const int PixelsPerUnit = 128;

    private static readonly (string prefab, string texFile, int width, int height)[] Paintings =
    {
        // 1x1 ("64x64"), the original five.
        ("Painting1", "painting1_starry_night", 1, 1),
        ("Painting2", "painting2_great_wave", 1, 1),
        ("Painting3", "painting3_impression_sunrise", 1, 1),
        ("Painting4", "painting4_hiroshige", 1, 1),
        ("Painting5", "painting5_fighting_temeraire", 1, 1),
        // 2x2 ("128x128").
        ("Painting6", "painting6_klimt_the_kiss", 2, 2),
        ("Painting7", "painting7_vermeer_pearl_earring", 2, 2),
        ("Painting8", "painting8_van_gogh_sunflowers", 2, 2),
        // 4x4 ("256x256").
        ("Painting9", "painting9_monet_water_lilies", 4, 4),
        ("Painting10", "painting10_hokusai_red_fuji", 4, 4),
        ("Painting11", "painting11_klimt_farm_garden", 4, 4),
        // 1x2 ("64x128").
        ("Painting12", "painting12_mucha_spring", 1, 2),
        ("Painting13", "painting13_mucha_summer", 1, 2),
        ("Painting14", "painting14_mucha_autumn", 1, 2),
        // 2x4 ("128x256").
        ("Painting15", "painting15_mucha_winter", 2, 4),
        ("Painting16", "painting16_hiroshige_plum_garden", 2, 4),
        ("Painting17", "painting17_hokusai_kirifuri_waterfall", 2, 4),
        // 2x1 ("128x64").
        ("Painting18", "painting18_korin_irises_left", 2, 1),
        ("Painting19", "painting19_korin_irises_right", 2, 1),
        ("Painting20", "painting20_friedrich_monk_by_the_sea", 2, 1),
        // 4x2 ("256x128").
        ("Painting21", "painting21_bierstadt_sierra_nevada", 4, 2),
        ("Painting22", "painting22_bruegel_hunters_in_the_snow", 4, 2),
        ("Painting23", "painting23_moran_grand_canyon_yellowstone", 4, 2),
    };

    [MenuItem("Tools/Procedural Cities/Generate Painting Props")]
    public static void Generate()
    {
        RemoveStaleProceduralAssets();

        var matFrame = GetSharedMat("LP_Painting_Frame", new Color(0.24f, 0.15f, 0.08f), 0.05f, 0.25f);

        foreach (var (prefabName, texFile, width, height) in Paintings)
            SaveOne(prefabName, texFile, width, height, matFrame);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GeneratePaintingProps] Saved {Paintings.Length} painting prefabs under {PrefabDir}");
    }

    private static void SaveOne(string prefabName, string texFile, int width, int height, Material matFrame)
    {
        var texPath = $"{TexDir}/{texFile}.png";
        ConfigureImport(texPath, PixelsPerUnit * Mathf.Max(width, height));
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            Debug.LogError($"[GeneratePaintingProps] Missing texture at {texPath} — crop it from the Wikimedia "
                + "Commons download first (see the PaintingsOriginal~ and Paintings folders).");
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

        var frame = new Vector2(UnitFrameSize * width, UnitFrameSize * height);
        var root = BuildFrame(prefabName, frame, matFrame, matPicture);
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{prefabName}.prefab");
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// Import settings by size: the original 128 px crops stay uncompressed and
    /// without mipmaps; larger ones are compressed and mipmapped (seen from
    /// across a room, they would shimmer).
    /// </summary>
    private static void ConfigureImport(string texPath, int maxSize)
    {
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null)
            return;

        var large = maxSize > PixelsPerUnit;
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = large;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = large ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static GameObject BuildFrame(string rootName, Vector2 frame, Material matFrame, Material matPicture)
    {
        var root = new GameObject(rootName);

        var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(backing.GetComponent<Collider>());
        backing.name = "Frame";
        backing.transform.SetParent(root.transform, false);
        backing.transform.localScale = new Vector3(frame.x, frame.y, FrameDepth);
        backing.GetComponent<MeshRenderer>().sharedMaterial = matFrame;

        var picture = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(picture.GetComponent<Collider>());
        picture.name = "Picture";
        picture.transform.SetParent(root.transform, false);
        // +Z (not -Z): the frame is mounted with its local +Z axis facing
        // outward into the room (matches AttemptPlace's onWall rotation,
        // which points the whole prop's +Z/forward away from the wall). A
        // default Quad's face normal is -Z (Unity's CreatePrimitive
        // convention), so it also needs the 180° yaw below — without it,
        // the quad ends up sandwiched behind the frame's solid Cube AND
        // facing away from the viewer, making the artwork invisible from
        // every angle in the room. Reported live: paintings placed and
        // correctly textured (confirmed via reflection) but rendering as a
        // flat, featureless dark rectangle — the frame occluding a
        // backwards, backward-facing canvas.
        picture.transform.localPosition = new Vector3(0, 0, PictureOffset);
        picture.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        picture.transform.localScale = new Vector3(frame.x - FrameBorder * 2f, frame.y - FrameBorder * 2f, 1f);
        picture.GetComponent<MeshRenderer>().sharedMaterial = matPicture;

        var collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(frame.x, frame.y, 0.05f);

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
