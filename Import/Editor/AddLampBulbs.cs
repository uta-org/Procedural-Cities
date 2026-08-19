using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 1 of the "make lamp light look like it comes from the lamp" fix
/// (reported live: "las luces no parecen salir de la lampara en si").
/// Adds a small bulb to each of the 8 lamp/fixture prefabs the lighting
/// system uses — two overlapping spheres, "BulbOn" (white, lightly emissive)
/// and "BulbOff" (dark grey), toggled by <c>LampFocalLight.SetOn</c> instead
/// of swapping a shared material, so no runtime material reference lookup is
/// needed. Positioned at the same anchor <c>AddLampFocalLights.cs</c> already
/// computed for the fixture's Light component, so bulb and light coincide.
/// Phase 2 (a later pass) will adjust the Light itself so it visually reads
/// as emanating from this bulb; this phase only adds the bulb + its two
/// on/off colours.
/// Menu: Tools / Procedural Cities / Add Lamp Bulbs
/// </summary>
public static class AddLampBulbs
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";
    private const float BulbRadius = 0.025f;

    private static readonly string[] TargetPrefabs =
    {
        "TableLampRound", "TableLampCone", "Lamp1",
        "WallSconceDrum", "WallSconceTorch",
        "CeilingFlushDisc", "CeilingPendant", "CeilingPanel",
    };

    [MenuItem("Tools/Procedural Cities/Add Lamp Bulbs")]
    public static void Generate()
    {
        EnsureMatFolder();
        var matOn = GetOrCreateMat("LP_Lamp_BulbOn", Color.white, emissive: true);
        var matOff = GetOrCreateMat("LP_Lamp_BulbOff", new Color(0.18f, 0.18f, 0.18f), emissive: false);

        var touched = 0;
        foreach (var prefabName in TargetPrefabs)
        {
            if (AddBulb(prefabName, matOn, matOff))
                touched++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AddLampBulbs] Added/verified a bulb on {touched}/{TargetPrefabs.Length} lamp prefabs.");
    }

    private static bool AddBulb(string prefabName, Material matOn, Material matOff)
    {
        var path = FindPrefabPath(prefabName);
        if (path == null)
        {
            Debug.LogWarning($"[AddLampBulbs] Prefab not found: {prefabName}");
            return false;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.transform.Find("BulbAnchor") != null)
                return true; // already added (re-running the menu item).

            var focalLight = root.GetComponentInChildren<Light>(true);
            var anchorLocalPos = focalLight != null
                ? focalLight.transform.localPosition
                : ComputeLocalBounds(root).center;

            var anchor = new GameObject("BulbAnchor");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = anchorLocalPos;

            var bulbOn = CreateBulbSphere("BulbOn", anchor.transform, matOn);
            var bulbOff = CreateBulbSphere("BulbOff", anchor.transform, matOff);
            bulbOn.SetActive(false);
            bulbOff.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreateBulbSphere(string name, Transform parent, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * (BulbRadius * 2f); // primitive sphere has diameter 1 at scale 1
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Same combined-local-bounds fallback as AddLampFocalLights.cs, for prefabs with no Light yet.</summary>
    private static Bounds ComputeLocalBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.one * 0.2f);

        var worldBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        var localCenter = root.transform.InverseTransformPoint(worldBounds.center);
        return new Bounds(localCenter, worldBounds.size);
    }

    private static void EnsureMatFolder()
    {
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder(PkgRoot + "/Models/LowPoly", "Materials");
    }

    private static Material GetOrCreateMat(string name, Color color, bool emissive)
    {
        var path = $"{MatDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name, color = color };
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
        else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.2f);

        if (emissive && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", Color.white);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static string FindPrefabPath(string prefabName)
    {
        var guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        foreach (var guid in guids)
        {
            var candidatePath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(candidatePath) == prefabName)
                return candidatePath;
        }
        return null;
    }
}
