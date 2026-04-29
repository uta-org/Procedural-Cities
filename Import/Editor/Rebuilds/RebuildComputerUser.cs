using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class RebuildComputerUser
{
    const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    const string LowPolyDir = "Assets/LowPoly";
    const string CombinedDir = PkgRoot + "/Models/LowPoly";
    const string PkgMatDir = PkgRoot + "/Models/LowPoly/Materials";
    const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";

    // [MenuItem("Procedural Cities/Rebuild ComputerUser")]
    static void Rebuild()
    {
        Debug.Log("[RebuildComputerUser] Step 1: Regenerating LowPoly_ComputerUser...");
        var genType = typeof(GenerateLowPolyModels);

        var ensureFolders = genType.GetMethod("EnsureFolders",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        ensureFolders?.Invoke(null, null);

        var matField = genType.GetField("materials",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (matField != null)
            ((Dictionary<string, Material>)matField.GetValue(null)).Clear();

        var method = genType.GetMethod("GenerateComputerUser",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
        {
            Debug.LogError("[RebuildComputerUser] GenerateComputerUser method not found!");
            return;
        }
        method.Invoke(null, null);

        string lowPolyPath = $"{LowPolyDir}/LowPoly_ComputerUser.prefab";
        string combinedPath = $"{CombinedDir}/ComputerUser_Combined.asset";
        string prefabPath = $"{PrefabDir}/ComputerUser.prefab";

        var lowPoly = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
        if (lowPoly == null)
        {
            Debug.LogError("[RebuildComputerUser] LowPoly_ComputerUser.prefab not found at " + lowPolyPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(lowPoly);
        instance.hideFlags = HideFlags.HideAndDontSave;

        var parts = new List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(instance.transform, Matrix4x4.identity, parts);
        Object.DestroyImmediate(instance);

        Debug.Log($"[RebuildComputerUser] Collected {parts.Count} mesh parts");

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
            var srcMat = kvp.Value.mats[0];
            var pkgMat = AssetDatabase.LoadAssetAtPath<Material>($"{PkgMatDir}/{srcMat.name}.mat");
            if (pkgMat == null)
            {
                string srcPath = AssetDatabase.GetAssetPath(srcMat);
                string dstPath = $"{PkgMatDir}/{srcMat.name}.mat";
                if (!string.IsNullOrEmpty(srcPath))
                {
                    AssetDatabase.CopyAsset(srcPath, dstPath);
                    pkgMat = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
                    Debug.Log($"[RebuildComputerUser] Copied material {srcMat.name} to package");
                }
            }
            finalMaterials.Add(pkgMat != null ? pkgMat : srcMat);
        }

        var combinedMesh = new Mesh();
        combinedMesh.name = "ComputerUser_Combined";
        combinedMesh.CombineMeshes(finalCombines.ToArray(), false, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(combinedPath);
        AssetDatabase.CreateAsset(combinedMesh, combinedPath);

        float labelHeight = combinedMesh.bounds.max.y + 0.1f;

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
            var label = root.transform.Find("Label_ComputerUser");
            if (label != null)
                label.localPosition = new Vector3(0, labelHeight, 0);
        }

        AssetDatabase.SaveAssets();

        // Clean up intermediates
        var staleGuids = AssetDatabase.FindAssets("LowPoly_ComputerUser", new[] { LowPolyDir });
        int deleted = 0;
        foreach (var guid in staleGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("LowPoly_ComputerUser"))
            {
                AssetDatabase.DeleteAsset(p);
                deleted++;
            }
        }

        // Force reimport package assets
        AssetDatabase.ImportAsset(combinedPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        Debug.Log($"[RebuildComputerUser] Done! Bounds: {combinedMesh.bounds.size}, verts={combinedMesh.vertexCount}, mats={finalMaterials.Count}, cleaned {deleted} intermediate assets");
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
