using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a low-poly console/side table for hallway decoration — wider and
/// lower than the bedroom "SmallTable" nightstand prefab, with a lower shelf
/// for visual interest. Deployed to Resources/Prefabs/AssetContents (same
/// convention as GenerateRubbishBinProp.cs / GeneratePlanterProps.cs) so
/// DecorationSpawner's Resources.Load fallback resolves "console_table".
/// Menu: Tools / Procedural Cities / Generate Console Table Prop
/// </summary>
public static class GenerateConsoleTableProp
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";

    [MenuItem("Tools/Procedural Cities/Generate Console Table Prop")]
    public static void Generate()
    {
        EnsureMatFolder();

        var matWood = GetMat("LP_ConsoleTable_Wood", new Color(0.42f, 0.27f, 0.15f), 0.05f, 0.35f);
        var matWoodDark = GetMat("LP_ConsoleTable_WoodDark", new Color(0.28f, 0.17f, 0.09f), 0.05f, 0.3f);

        var root = new GameObject("ConsoleTable");

        // Tabletop: 0.90 wide (X) x 0.30 deep (Z), 0.04 thick, top at y=0.44.
        AddBox("Top", root.transform, matWood,
            new Vector3(0, 0.42f, 0), new Vector3(0.45f, 0.02f, 0.15f));

        // Lower shelf for silhouette interest.
        AddBox("Shelf", root.transform, matWoodDark,
            new Vector3(0, 0.14f, 0), new Vector3(0.40f, 0.012f, 0.13f));

        // Four tapered-looking legs (thin boxes) at the corners.
        var legHalfHeight = 0.20f;
        var legX = 0.40f;
        var legZ = 0.12f;
        foreach (var sx in new[] { -1f, 1f })
        {
            foreach (var sz in new[] { -1f, 1f })
            {
                AddBox($"Leg_{(sx < 0 ? "L" : "R")}{(sz < 0 ? "B" : "F")}", root.transform, matWoodDark,
                    new Vector3(sx * legX, legHalfHeight, sz * legZ),
                    new Vector3(0.025f, legHalfHeight, 0.025f));
            }
        }

        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.22f, 0);
        collider.size = new Vector3(0.92f, 0.44f, 0.32f);

        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/ConsoleTable.prefab");
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateConsoleTableProp] Saved {PrefabDir}/ConsoleTable.prefab");
    }

    private static void EnsureMatFolder()
    {
        if (AssetDatabase.IsValidFolder(MatDir))
            return;
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        AssetDatabase.CreateFolder(PkgRoot + "/Models/LowPoly", "Materials");
    }

    private static GameObject AddBox(
        string name, Transform parent, Material mat, Vector3 localPos, Vector3 halfExtents)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = halfExtents * 2f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static Material GetMat(string name, Color color, float metallic, float smoothness)
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
