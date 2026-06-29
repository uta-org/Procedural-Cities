using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Rebuilds all 5 Tree variants from LowPoly generators.
/// Menu: Tools / Procedural Cities / Rebuild Trees
/// </summary>
public static class RebuildTrees
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string LowPolyDir = "Assets/LowPoly";
    private const string CombinedDir = PkgRoot + "/Models/LowPoly";
    private const string PkgMatDir = PkgRoot + "/Models/LowPoly/Materials";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";

    private static readonly string[] TreeNames = { "Tree", "Tree1", "Tree2", "Tree3", "Tree4", "Tree5", "Tree6" };

    // ReSharper disable once UnusedMember.Local
    // [MenuItem("Tools/uzProceduralCities/Rebuild Trees")]
    private static void Rebuild()
    {
        Debug.Log("[RebuildTrees] Regenerating all 7 Tree variants...");
        var genType = typeof(GenerateLowPolyModels);

        var ensureFolders = genType.GetMethod("EnsureFolders",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        ensureFolders?.Invoke(null, null);

        var matField = genType.GetField("materials",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (matField != null)
            ((Dictionary<string, Material>)matField.GetValue(null)).Clear();

        // Generate all 5 tree variants
        foreach (var treeName in TreeNames)
        {
            var method = genType.GetMethod($"Generate{treeName}",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                Debug.LogError($"[RebuildTrees] Generate{treeName} method not found!");
                continue;
            }
            method.Invoke(null, null);
            Debug.Log($"[RebuildTrees] Generated LowPoly_{treeName}");

            // Build combined asset + prefab
            BuildCombinedPrefab(treeName);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RebuildTrees] Done rebuilding all 5 Tree variants.");
    }

    private static void BuildCombinedPrefab(string treeName)
    {
        string lowPolyPath = $"{LowPolyDir}/LowPoly_{treeName}.prefab";
        string combinedPath = $"{CombinedDir}/{treeName}_Combined.asset";
        string prefabPath = $"{PrefabDir}/{treeName}.prefab";

        var lowPoly = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
        if (lowPoly == null)
        {
            Debug.LogError($"[RebuildTrees] LowPoly_{treeName}.prefab not found at {lowPolyPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(lowPoly);
        instance.hideFlags = HideFlags.HideAndDontSave;

        var parts = new List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(instance.transform, Matrix4x4.identity, parts);
        Object.DestroyImmediate(instance);

        Debug.Log($"[RebuildTrees] {treeName}: Collected {parts.Count} mesh parts");

        var materialGroups = new Dictionary<string, (List<CombineInstance> combines, Material[] mats)>();
        foreach (var part in parts)
        {
            string matKey = string.Join("|", part.materials.Select(m => m != null ? m.name : "null"));
            if (!materialGroups.ContainsKey(matKey))
                materialGroups[matKey] = (new List<CombineInstance>(), part.materials);

            for (int sub = 0; sub < part.mesh.subMeshCount; sub++)
            {
                materialGroups[matKey].combines.Add(new CombineInstance
                {
                    mesh = part.mesh,
                    subMeshIndex = sub,
                    transform = part.transform
                });
            }
        }

        var combinedMesh = new Mesh();
        combinedMesh.name = $"{treeName}_Combined";
        var allCombines = new List<CombineInstance>();
        var allMaterials = new List<Material>();

        foreach (var group in materialGroups.Values)
        {
            var groupMesh = new Mesh();
            groupMesh.CombineMeshes(group.combines.ToArray(), true, true);
            allCombines.Add(new CombineInstance { mesh = groupMesh, transform = Matrix4x4.identity });
            allMaterials.Add(group.mats[0]);
        }

        combinedMesh.CombineMeshes(allCombines.ToArray(), false, true);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        // Save combined mesh
        if (AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath) != null)
            AssetDatabase.DeleteAsset(combinedPath);
        AssetDatabase.CreateAsset(combinedMesh, combinedPath);

        // Copy materials to package
        var pkgMats = new Material[allMaterials.Count];
        for (int i = 0; i < allMaterials.Count; i++)
        {
            string srcPath = AssetDatabase.GetAssetPath(allMaterials[i]);
            string dstPath = $"{PkgMatDir}/{allMaterials[i].name}.mat";
            if (!AssetDatabase.LoadAssetAtPath<Material>(dstPath))
                AssetDatabase.CopyAsset(srcPath, dstPath);
            pkgMats[i] = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
        }

        // Build prefab
        var root = new GameObject(treeName);
        var meshChild = new GameObject("mesh");
        meshChild.transform.SetParent(root.transform);
        meshChild.transform.localPosition = Vector3.zero;
        meshChild.transform.localRotation = Quaternion.identity;
        meshChild.transform.localScale = Vector3.one;

        var mf = meshChild.AddComponent<MeshFilter>();
        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
        var mr = meshChild.AddComponent<MeshRenderer>();
        mr.sharedMaterials = pkgMats;

        // Compute bounds for label
        float maxY = 1.5f;
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            maxY = bounds.max.y;
        }

        var labelGo = new GameObject($"Label_{treeName}");
        labelGo.transform.SetParent(root.transform);
        labelGo.transform.localPosition = new Vector3(0, maxY + 0.3f, 0);
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = treeName;
        tm.characterSize = 0.06f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        int totalVerts = combinedMesh.vertexCount;
        Debug.Log($"[RebuildTrees] {treeName}: saved prefab ({totalVerts} verts)");
    }

    private static void CollectMeshParts(Transform t, Matrix4x4 parentMatrix, List<(Mesh, Matrix4x4, Material[])> parts)
    {
        var localMatrix = parentMatrix * Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
        var mf = t.GetComponent<MeshFilter>();
        var mr = t.GetComponent<MeshRenderer>();
        if (mf != null && mf.sharedMesh != null && mr != null)
            parts.Add((mf.sharedMesh, localMatrix, mr.sharedMaterials));

        for (int i = 0; i < t.childCount; i++)
            CollectMeshParts(t.GetChild(i), localMatrix, parts);
    }
}