using UnityEngine;
using UnityEditor;
using System.Linq;

public static class FixAwningScale
{
    // [MenuItem("Tools/uzProceduralCities/Fix Awning Scale")]
    static void Fix()
    {
        // The Awning_Combined.asset was generated from the LowPoly_Awning prefab
        // but somehow has inflated vertices (~12.8m wide instead of ~3.8m).
        // Rebuild it from the LowPoly source with correct dimensions.

        const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
        string lowPolyPath = PkgRoot + "/Models/LowPoly/LowPoly_Awning.prefab";
        string combinedPath = PkgRoot + "/Models/LowPoly/Awning_Combined.asset";
        string prefabPath = PkgRoot + "/Resources/Prefabs/AssetContents/Awning.prefab";

        var lowPoly = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
        if (lowPoly == null)
        {
            Debug.LogError("[FixAwning] LowPoly_Awning.prefab not found at " + lowPolyPath);
            return;
        }

        // Instantiate to read mesh parts
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(lowPoly);
        instance.hideFlags = HideFlags.HideAndDontSave;

        var parts = new System.Collections.Generic.List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(instance.transform, Matrix4x4.identity, parts);
        Object.DestroyImmediate(instance);

        if (parts.Count == 0)
        {
            Debug.LogError("[FixAwning] No mesh parts found");
            return;
        }

        // Group by material
        var materialGroups = new System.Collections.Generic.Dictionary<string,
            (System.Collections.Generic.List<CombineInstance> combines, Material[] mats)>();

        foreach (var part in parts)
        {
            string matKey = string.Join("|", part.materials.Select(m => m != null ? m.name : "null"));
            if (!materialGroups.ContainsKey(matKey))
                materialGroups[matKey] = (new System.Collections.Generic.List<CombineInstance>(), part.materials);

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

        var finalCombines = new System.Collections.Generic.List<CombineInstance>();
        var finalMaterials = new System.Collections.Generic.List<Material>();

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
        combinedMesh.name = "Awning_Combined";
        combinedMesh.CombineMeshes(finalCombines.ToArray(), false, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        Debug.Log($"[FixAwning] New mesh bounds: {combinedMesh.bounds} (verts={combinedMesh.vertexCount})");

        // Save combined mesh (overwrite)
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(combinedPath);
        AssetDatabase.CreateAsset(combinedMesh, combinedPath);

        // Update prefab label height
        float labelHeight = combinedMesh.bounds.max.y + 0.1f;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            string tempPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Update mesh reference on child
                var mf = root.GetComponentInChildren<MeshFilter>();
                if (mf != null)
                {
                    var freshMesh = AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
                    mf.sharedMesh = freshMesh;
                }

                // Update materials
                var mr = root.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterials = finalMaterials.ToArray();

                // Update label position
                var label = root.transform.Find("Label_Awning");
                if (label != null)
                    label.localPosition = new Vector3(0, labelHeight, 0);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FixAwning] Done. Bounds: center={combinedMesh.bounds.center}, size={combinedMesh.bounds.size}");
    }

    static void CollectMeshParts(Transform parent, Matrix4x4 parentMatrix,
        System.Collections.Generic.List<(Mesh mesh, Matrix4x4 transform, Material[] materials)> parts)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var localMatrix = parentMatrix * Matrix4x4.TRS(
                child.localPosition, child.localRotation, child.localScale);

            var mf = child.GetComponent<MeshFilter>();
            var mr = child.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null && mr != null)
            {
                parts.Add((mf.sharedMesh, localMatrix, mr.sharedMaterials));
            }

            CollectMeshParts(child, localMatrix, parts);
        }
    }
}
