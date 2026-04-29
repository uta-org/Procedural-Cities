using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class RebuildPan1
{
    const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    const string LowPolyDir = "Assets/LowPoly";
    const string CombinedDir = PkgRoot + "/Models/LowPoly";
    const string PkgMatDir = PkgRoot + "/Models/LowPoly/Materials";
    const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";

    // [MenuItem("Procedural Cities/Rebuild Pan1")]
    static void Rebuild()
    {
        Debug.Log("[RebuildPan1] Step 1: Regenerating LowPoly_Pan1...");
        var genType = typeof(GenerateLowPolyModels);

        var ensureFolders = genType.GetMethod("EnsureFolders",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        ensureFolders?.Invoke(null, null);

        var matField = genType.GetField("materials",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (matField != null)
            ((Dictionary<string, Material>)matField.GetValue(null)).Clear();

        // Delete old source materials so GetMat recreates them with current colors
        var oldMatGuids = AssetDatabase.FindAssets("LP_Pan1_", new[] { "Assets/LowPoly/Materials" });
        foreach (var guid in oldMatGuids)
        {
            var matPath = AssetDatabase.GUIDToAssetPath(guid);
            if (matPath.Contains("LP_Pan1_"))
            {
                AssetDatabase.DeleteAsset(matPath);
                Debug.Log($"[RebuildPan1] Deleted old material: {matPath}");
            }
        }

        var method = genType.GetMethod("GeneratePan1",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
        {
            Debug.LogError("[RebuildPan1] GeneratePan1 method not found!");
            return;
        }
        method.Invoke(null, null);

        string lowPolyPath = $"{LowPolyDir}/LowPoly_Pan1.prefab";
        string combinedPath = $"{CombinedDir}/Pan1_Combined.asset";
        string prefabPath = $"{PrefabDir}/Pan1.prefab";

        var lowPoly = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
        if (lowPoly == null)
        {
            Debug.LogError("[RebuildPan1] LowPoly_Pan1.prefab not found at " + lowPolyPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(lowPoly);
        instance.hideFlags = HideFlags.HideAndDontSave;

        var parts = new List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(instance.transform, Matrix4x4.identity, parts);
        Object.DestroyImmediate(instance);

        Debug.Log($"[RebuildPan1] Collected {parts.Count} mesh parts");

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
                    Debug.Log($"[RebuildPan1] Copied material {srcMat.name} to package");
                }
            }
            if (pkgMat != null)
            {
                var lpShader = Shader.Find("Procedural Cities/LowPoly PBR");
                if (lpShader != null)
                {
                    pkgMat.shader = lpShader;
                    pkgMat.color = srcMat.color;
                    pkgMat.SetFloat("_Metallic", srcMat.GetFloat("_Metallic"));
                    pkgMat.SetFloat("_Glossiness", srcMat.HasProperty("_Glossiness") ? srcMat.GetFloat("_Glossiness") : 0.3f);
                    EditorUtility.SetDirty(pkgMat);
                    Debug.Log($"[RebuildPan1] Updated material {pkgMat.name}");
                }
            }
            finalMaterials.Add(pkgMat != null ? pkgMat : srcMat);
        }

        var combinedMesh = new Mesh();
        combinedMesh.name = "Pan1_Combined";
        combinedMesh.CombineMeshes(finalCombines.ToArray(), false, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateTangents();
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
            var label = root.transform.Find("Label_Pan1");
            if (label != null)
                label.localPosition = new Vector3(0, labelHeight, 0);
        }

        AssetDatabase.SaveAssets();

        var staleGuids = AssetDatabase.FindAssets("LowPoly_Pan1", new[] { LowPolyDir });
        int deleted = 0;
        foreach (var guid in staleGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("LowPoly_Pan1"))
            {
                AssetDatabase.DeleteAsset(p);
                deleted++;
            }
        }

        AssetDatabase.ImportAsset(combinedPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        Debug.Log($"[RebuildPan1] Done! Bounds: {combinedMesh.bounds.size}, verts={combinedMesh.vertexCount}, mats={finalMaterials.Count}, cleaned {deleted} intermediate assets");
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
