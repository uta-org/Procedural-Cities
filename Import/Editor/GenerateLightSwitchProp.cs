using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the low-poly wall light switch that stands beside a room's door
/// and switches that room's ceiling fixtures: a wall plate, a darker bezel
/// and a toggle lever on a pivot. The pivot ("LeverPivot") is what the
/// runtime tilts up (on) or down (off) — see LightSwitch.cs in the main
/// project. Purely visual plus one collider for the E-prompt raycast.
/// The prefab's pivot is the BACK of the plate (z = 0 is the wall face,
/// geometry extends towards +Z, into the room), same convention as the
/// wall sconces of GenerateLampProps.cs. Deployed to
/// Resources/Prefabs/AssetContents like the other Generate*Prop.cs scripts.
/// Menu: Tools / Procedural Cities / Generate Light Switch Prop
/// </summary>
public static class GenerateLightSwitchProp
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";

    /// <summary>Tilt of the lever pivot, degrees about local X: positive points the lever down (off).</summary>
    private const float LeverRestTilt = 30f;

    [MenuItem("Tools/Procedural Cities/Generate Light Switch Prop")]
    public static void Generate()
    {
        EnsureMatFolder();

        var matPlate = GetMat("LP_Switch_Plate", new Color(0.93f, 0.92f, 0.88f), 0f, 0.35f);
        var matBezel = GetMat("LP_Switch_Bezel", new Color(0.55f, 0.55f, 0.56f), 0.1f, 0.4f);
        var matLever = GetMat("LP_Switch_Lever", new Color(0.16f, 0.16f, 0.17f), 0.2f, 0.5f);

        var root = new GameObject("LightSwitch");

        // 0.08 wide x 0.12 tall plate, 1 cm proud of the wall.
        AddBox("Plate", root.transform, matPlate, new Vector3(0f, 0f, 0.005f), new Vector3(0.04f, 0.06f, 0.005f));
        AddBox("Bezel", root.transform, matBezel, new Vector3(0f, 0f, 0.0115f), new Vector3(0.016f, 0.032f, 0.0015f));

        var pivot = new GameObject("LeverPivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0f, 0.012f);
        pivot.transform.localRotation = Quaternion.Euler(LeverRestTilt, 0f, 0f);
        AddBox("Lever", pivot.transform, matLever, new Vector3(0f, 0f, 0.014f), new Vector3(0.007f, 0.007f, 0.014f));

        // Larger than the plate so the E-prompt does not demand a pixel-perfect aim.
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, 0.02f);
        collider.size = new Vector3(0.12f, 0.16f, 0.04f);

        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/LightSwitch.prefab");
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateLightSwitchProp] Saved LightSwitch prefab under {PrefabDir}");
    }

    private static void EnsureMatFolder()
    {
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        if (!AssetDatabase.IsValidFolder(MatDir))
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
