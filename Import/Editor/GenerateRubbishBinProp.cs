using UnityEditor;
using UnityEngine;

/// <summary>
/// Rebuilds the interior "rubbish_bin" prop with more visual detail (base ring,
/// ribbed body, rim, lid, handle knob instead of a plain octagon + flat lid).
/// CityManager's "_furniturePrefabs" array (JobScene.unity) already references
/// Import/Models/LowPoly/LowPoly_RubbishBin.prefab by GUID, so this overwrites
/// that prefab's contents IN PLACE (same path, same .meta/GUID) — the existing
/// scene reference picks up the new look with no scene edit needed. Also
/// deploys a copy to Resources/Prefabs/AssetContents so the
/// DecorationSpawner.LoadPrefabFromResources fallback path (used if the
/// Inspector-array reference is ever broken) resolves to the same look rather
/// than InteriorStreamingManager's placeholder Cube.
/// Menu: Tools / Procedural Cities / Generate RubbishBin Prop
/// </summary>
public static class GenerateRubbishBinProp
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string LivePrefabPath = PkgRoot + "/Models/LowPoly/LowPoly_RubbishBin.prefab";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";

    [MenuItem("Tools/Procedural Cities/Generate RubbishBin Prop")]
    public static void Generate()
    {
        EnsureMatFolder();

        var matBody = GetMat("LP_Bin_Body", new Color(0.16f, 0.38f, 0.18f), 0.05f, 0.35f);
        var matTrim = GetMat("LP_Bin_Trim", new Color(0.20f, 0.20f, 0.22f), 0.5f, 0.5f);

        var root = BuildHierarchy("LowPoly_RubbishBin", matBody, matTrim);
        var oldGuid = AssetDatabase.AssetPathToGUID(LivePrefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, LivePrefabPath, out var liveSuccess);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var newGuid = AssetDatabase.AssetPathToGUID(LivePrefabPath);

        var resourcesRoot = BuildHierarchy("RubbishBin", matBody, matTrim);
        PrefabUtility.SaveAsPrefabAsset(resourcesRoot, $"{PrefabDir}/RubbishBin.prefab", out var resSuccess);
        Object.DestroyImmediate(resourcesRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[GenerateRubbishBinProp] live save={liveSuccess} guid {oldGuid}->{newGuid} " +
            $"resources save={resSuccess} paths: {LivePrefabPath} , {PrefabDir}/RubbishBin.prefab");
    }

    private static GameObject BuildHierarchy(string rootName, Material matBody, Material matTrim)
    {
        var root = new GameObject(rootName);

        // Foot ring
        AddCylinder("Base", root.transform, matTrim, new Vector3(0, 0.03f, 0), 0.34f, 0.03f);

        // Tapered body (slightly narrower than the rim, ribbed for silhouette)
        AddCylinder("rubbishbin", root.transform, matBody, new Vector3(0, 0.395f, 0), 0.31f, 0.335f);

        // Vertical ribs break up the flat cylinder wall.
        const int ribCount = 6;
        for (var i = 0; i < ribCount; i++)
        {
            var angleDeg = i * (360f / ribCount);
            var rad = angleDeg * Mathf.Deg2Rad;
            var rib = AddBox($"Rib{i}", root.transform, matTrim,
                new Vector3(Mathf.Sin(rad) * 0.305f, 0.395f, Mathf.Cos(rad) * 0.305f),
                new Vector3(0.02f, 0.335f, 0.045f));
            rib.transform.localRotation = Quaternion.Euler(0, angleDeg, 0);
        }

        // Rim lip, flat lid disc, and a small handle knob on top.
        AddCylinder("Rim", root.transform, matTrim, new Vector3(0, 0.75f, 0), 0.335f, 0.02f);
        AddCylinder("Lid", root.transform, matTrim, new Vector3(0, 0.80f, 0), 0.30f, 0.03f);
        AddSphere("Knob", root.transform, matTrim, new Vector3(0, 0.875f, 0), 0.045f);

        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, 0.46f, 0);
        collider.height = 0.92f;
        collider.radius = 0.34f;

        return root;
    }

    private static void EnsureMatFolder()
    {
        if (AssetDatabase.IsValidFolder(MatDir))
            return;
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        AssetDatabase.CreateFolder(PkgRoot + "/Models/LowPoly", "Materials");
    }

    private static GameObject AddCylinder(
        string name, Transform parent, Material mat, Vector3 localPos, float radius, float halfHeight)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        // Native primitive cylinder: radius 0.5, half-height 1.
        go.transform.localScale = new Vector3(radius * 2f, halfHeight, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject AddBox(
        string name, Transform parent, Material mat, Vector3 localPos, Vector3 halfExtents)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        // Native primitive cube: half-extent 0.5 on every axis.
        go.transform.localScale = halfExtents * 2f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject AddSphere(
        string name, Transform parent, Material mat, Vector3 localPos, float radius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * (radius * 2f);
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
