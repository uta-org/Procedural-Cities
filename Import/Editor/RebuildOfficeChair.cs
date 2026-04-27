using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class RebuildOfficeChair
{
    const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    const string LowPolyDir = "Assets/LowPoly";                       // where GenerateLowPolyModels writes
    const string CombinedDir = PkgRoot + "/Models/LowPoly";           // where _Combined lives
    const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";

    [MenuItem("Procedural Cities/Rebuild Office Chair")]
    static void Rebuild()
    {
        // Step 1: Regenerate the LowPoly model via reflection (private method)
        Debug.Log("[RebuildChair] Step 1: Regenerating LowPoly_OfficeChair...");
        var genType = typeof(GenerateLowPolyModels);
        // Call EnsureFolders first
        var ensureFolders = genType.GetMethod("EnsureFolders",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        ensureFolders?.Invoke(null, null);
        // Clear materials cache
        var matField = genType.GetField("materials",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (matField != null)
            ((Dictionary<string, Material>)matField.GetValue(null)).Clear();
        // Call GenerateOfficeChair
        var method = genType.GetMethod("GenerateOfficeChair",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
        {
            Debug.LogError("[RebuildChair] GenerateOfficeChair method not found!");
            return;
        }
        method.Invoke(null, null);

        // Step 2: Load the fresh LowPoly prefab and rebuild _Combined.asset
        string lowPolyPath = $"{LowPolyDir}/LowPoly_OfficeChair.prefab";
        string combinedPath = $"{CombinedDir}/OfficeChair_Combined.asset";
        string prefabPath = $"{PrefabDir}/OfficeChair.prefab";

        var lowPoly = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
        if (lowPoly == null)
        {
            Debug.LogError("[RebuildChair] LowPoly_OfficeChair.prefab not found at " + lowPolyPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(lowPoly);
        instance.hideFlags = HideFlags.HideAndDontSave;

        var parts = new List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(instance.transform, Matrix4x4.identity, parts);
        Object.DestroyImmediate(instance);

        Debug.Log($"[RebuildChair] Collected {parts.Count} mesh parts");

        // Group by material
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
                    transform = part.transform,
                    subMeshIndex = sub
                });
            }
        }

        var finalCombines = new List<CombineInstance>();
        var finalMaterials = new List<Material>();

        foreach (var kvp in materialGroups)
        {
            var groupMesh = new Mesh();
            groupMesh.CombineMeshes(kvp.Value.combines.ToArray(), true, true);
            finalCombines.Add(new CombineInstance
            {
                mesh = groupMesh,
                transform = Matrix4x4.identity,
                subMeshIndex = 0
            });
            finalMaterials.Add(kvp.Value.mats[0]);
        }

        var combinedMesh = new Mesh();
        combinedMesh.name = "OfficeChair_Combined";
        combinedMesh.CombineMeshes(finalCombines.ToArray(), false, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        // Save combined mesh
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(combinedPath);
        AssetDatabase.CreateAsset(combinedMesh, combinedPath);

        float labelHeight = combinedMesh.bounds.max.y + 0.1f;

        // Rebuild prefab
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;
            var mf = root.GetComponentInChildren<MeshFilter>();
            if (mf != null)
            {
                var freshMesh = AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
                mf.sharedMesh = freshMesh;
            }
            var mr = root.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
                mr.sharedMaterials = finalMaterials.ToArray();
            var label = root.transform.Find("Label_OfficeChair");
            if (label != null)
                label.localPosition = new Vector3(0, labelHeight, 0);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RebuildChair] Done! Bounds: {combinedMesh.bounds.size}, verts={combinedMesh.vertexCount}, mats={finalMaterials.Count}");
    }

    static void CollectMeshParts(Transform parent, Matrix4x4 parentMatrix,
        List<(Mesh mesh, Matrix4x4 transform, Material[] materials)> parts)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var localMatrix = parentMatrix * Matrix4x4.TRS(
                child.localPosition, child.localRotation, child.localScale);

            var mf = child.GetComponent<MeshFilter>();
            var mr = child.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null && mr != null)
                parts.Add((mf.sharedMesh, localMatrix, mr.sharedMaterials));

            CollectMeshParts(child, localMatrix, parts);
        }
    }
}
