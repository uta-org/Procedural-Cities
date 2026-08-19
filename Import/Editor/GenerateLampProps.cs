using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds seven low-poly light fixtures for interior decoration: two short
/// table lamps, two wall sconces, and three ceiling fixtures. Purely visual
/// meshes — the two table lamps get their toggleable focal Light component
/// separately, from AddLampFocalLights.cs. Deployed to
/// Resources/Prefabs/AssetContents, same convention as the other
/// Generate*Prop.cs scripts in this folder.
/// Menu: Tools / Procedural Cities / Generate Lamp Props
/// </summary>
public static class GenerateLampProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";

    [MenuItem("Tools/Procedural Cities/Generate Lamp Props")]
    public static void Generate()
    {
        EnsureMatFolder();

        var matBase = GetMat("LP_Lamp_Base", new Color(0.15f, 0.15f, 0.16f), 0.6f, 0.5f);
        var matShadeWarm = GetMat("LP_Lamp_ShadeWarm", new Color(0.98f, 0.86f, 0.55f), 0f, 0.3f);
        var matShadeCool = GetMat("LP_Lamp_ShadeCool", new Color(0.95f, 0.95f, 0.90f), 0f, 0.3f);
        var matMount = GetMat("LP_Lamp_Mount", new Color(0.20f, 0.20f, 0.22f), 0.5f, 0.4f);

        SavePrefab(BuildTableLampRound(matBase, matShadeWarm), "TableLampRound");
        SavePrefab(BuildTableLampCone(matBase, matShadeCool), "TableLampCone");
        SavePrefab(BuildWallSconceDrum(matMount, matShadeWarm), "WallSconceDrum");
        SavePrefab(BuildWallSconceTorch(matMount, matShadeCool), "WallSconceTorch");
        SavePrefab(BuildCeilingFlushDisc(matMount, matShadeWarm), "CeilingFlushDisc");
        SavePrefab(BuildCeilingPendant(matMount, matShadeCool), "CeilingPendant");
        SavePrefab(BuildCeilingPanel(matMount, matShadeWarm), "CeilingPanel");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateLampProps] Saved 7 lamp prefabs under {PrefabDir}");
    }

    private static void SavePrefab(GameObject root, string name)
    {
        root.name = name;
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Table lamps (short, sit on a table/desk) ────────────────────────

    private static GameObject BuildTableLampRound(Material matBase, Material matShade)
    {
        var root = new GameObject("TableLampRound");
        AddCylinder("Base", root.transform, matBase, new Vector3(0, 0.012f, 0), 0.075f, 0.012f);
        AddCylinder("Pole", root.transform, matBase, new Vector3(0, 0.10f, 0), 0.012f, 0.088f);
        AddCylinder("Shade", root.transform, matShade, new Vector3(0, 0.245f, 0), 0.09f, 0.06f);
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, 0.15f, 0);
        collider.height = 0.30f;
        collider.radius = 0.09f;
        return root;
    }

    private static GameObject BuildTableLampCone(Material matBase, Material matShade)
    {
        var root = new GameObject("TableLampCone");
        AddCylinder("Base", root.transform, matBase, new Vector3(0, 0.02f, 0), 0.06f, 0.02f);
        AddCylinder("Pole", root.transform, matBase, new Vector3(0, 0.10f, 0), 0.010f, 0.06f);
        // Tapered look: a wide flat disc under a narrower one approximates a cone shade.
        AddCylinder("ShadeLower", root.transform, matShade, new Vector3(0, 0.185f, 0), 0.085f, 0.02f);
        AddCylinder("ShadeUpper", root.transform, matShade, new Vector3(0, 0.225f, 0), 0.045f, 0.02f);
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, 0.13f, 0);
        collider.height = 0.26f;
        collider.radius = 0.085f;
        return root;
    }

    // ── Wall sconces (mounted flush against a wall) ─────────────────────

    private static GameObject BuildWallSconceDrum(Material matMount, Material matShade)
    {
        var root = new GameObject("WallSconceDrum");
        AddBox("Plate", root.transform, matMount, new Vector3(0, 0, -0.01f), new Vector3(0.05f, 0.07f, 0.01f));
        AddCylinder("Arm", root.transform, matMount, new Vector3(0, 0, 0.05f), 0.012f, 0.05f,
            Quaternion.Euler(90f, 0f, 0f));
        AddCylinder("Shade", root.transform, matShade, new Vector3(0, 0, 0.13f), 0.07f, 0.05f,
            Quaternion.Euler(90f, 0f, 0f));
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0, 0.08f);
        collider.size = new Vector3(0.15f, 0.15f, 0.20f);
        return root;
    }

    private static GameObject BuildWallSconceTorch(Material matMount, Material matShade)
    {
        var root = new GameObject("WallSconceTorch");
        AddBox("Plate", root.transform, matMount, new Vector3(0, 0, -0.01f), new Vector3(0.045f, 0.10f, 0.01f));
        AddCylinder("Arm", root.transform, matMount, new Vector3(0, -0.02f, 0.045f), 0.010f, 0.045f,
            Quaternion.Euler(90f, 0f, 0f));
        // Upward-opening torch-style shade: narrow at the arm, wide at the open top.
        AddCylinder("ShadeLower", root.transform, matShade, new Vector3(0, 0.03f, 0.09f), 0.035f, 0.05f);
        AddCylinder("ShadeUpper", root.transform, matShade, new Vector3(0, 0.10f, 0.09f), 0.06f, 0.015f);
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.04f, 0.09f);
        collider.size = new Vector3(0.13f, 0.22f, 0.19f);
        return root;
    }

    // ── Ceiling fixtures (mounted flush or hanging from the ceiling) ────

    private static GameObject BuildCeilingFlushDisc(Material matMount, Material matShade)
    {
        var root = new GameObject("CeilingFlushDisc");
        AddCylinder("Mount", root.transform, matMount, new Vector3(0, -0.01f, 0), 0.22f, 0.01f);
        AddCylinder("Diffuser", root.transform, matShade, new Vector3(0, -0.035f, 0), 0.19f, 0.015f);
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, -0.02f, 0);
        collider.height = 0.05f;
        collider.radius = 0.22f;
        return root;
    }

    private static GameObject BuildCeilingPendant(Material matMount, Material matShade)
    {
        var root = new GameObject("CeilingPendant");
        AddCylinder("Canopy", root.transform, matMount, new Vector3(0, -0.01f, 0), 0.05f, 0.01f);
        AddCylinder("Cord", root.transform, matMount, new Vector3(0, -0.22f, 0), 0.006f, 0.20f);
        AddCylinder("Shade", root.transform, matShade, new Vector3(0, -0.45f, 0), 0.10f, 0.06f);
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, -0.20f, 0);
        collider.height = 0.50f;
        collider.radius = 0.10f;
        return root;
    }

    private static GameObject BuildCeilingPanel(Material matMount, Material matShade)
    {
        var root = new GameObject("CeilingPanel");
        AddBox("Frame", root.transform, matMount, new Vector3(0, -0.008f, 0), new Vector3(0.20f, 0.008f, 0.20f));
        AddBox("Diffuser", root.transform, matShade, new Vector3(0, -0.02f, 0), new Vector3(0.18f, 0.006f, 0.18f));
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, -0.015f, 0);
        collider.size = new Vector3(0.40f, 0.03f, 0.40f);
        return root;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void EnsureMatFolder()
    {
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder(PkgRoot + "/Models/LowPoly", "Materials");
    }

    private static GameObject AddCylinder(
        string name, Transform parent, Material mat, Vector3 localPos, float radius, float halfHeight,
        Quaternion? localRot = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot ?? Quaternion.identity;
        go.transform.localScale = new Vector3(radius * 2f, halfHeight, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject AddBox(
        string name, Transform parent, Material mat, Vector3 localPos, Vector3 halfExtents,
        Quaternion? localRot = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot ?? Quaternion.identity;
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
