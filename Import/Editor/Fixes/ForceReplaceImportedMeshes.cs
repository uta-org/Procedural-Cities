using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Forces replacement of prefabs that still use imported 3D model meshes (3DS/OBJ)
/// with their LowPoly procedural versions. Unlike FixAllBrokenPrefabs, this does NOT
/// skip prefabs with valid meshes — it overwrites them.
/// Menu: Tools / Procedural Cities / Force Replace Imported Meshes
/// </summary>
public static class ForceReplaceImportedMeshes
{
    const string PkgRoot    = "Packages/dev.z3nth10n.proceduralcities.import";
    const string LowPolyDir = PkgRoot + "/Models/LowPoly";
    const string PrefabDir  = PkgRoot + "/Resources/Prefabs/AssetContents";

    // These prefabs still reference imported 3DS/OBJ meshes and need replacement
    static readonly string[] TargetPrefabs = new[]
    {
        "Lamp0", "Lamp1", "Lamp2", "Lamp3", "Lamp4",
        "Cup", "ChoppingBoard", "Sofa"
    };

    // [MenuItem("Tools/uzProceduralCities/Force Replace Imported Meshes")]
    static void Execute()
    {
        int replaced = 0;
        int failed = 0;
        var failedNames = new List<string>();

        for (int i = 0; i < TargetPrefabs.Length; i++)
        {
            string name = TargetPrefabs[i];
            EditorUtility.DisplayProgressBar("Replacing Imported Meshes",
                $"Processing {name} ({i + 1}/{TargetPrefabs.Length})",
                (float)i / TargetPrefabs.Length);

            string prefabPath = $"{PrefabDir}/{name}.prefab";
            string lowPolyPath = $"{LowPolyDir}/LowPoly_{name}.prefab";

            var lowPolyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
            if (lowPolyPrefab == null)
            {
                Debug.LogError($"[ForceReplace] No LowPoly prefab for '{name}' at {lowPolyPath}");
                failed++;
                failedNames.Add(name);
                continue;
            }

            if (RebuildFromLowPoly(name, prefabPath, lowPolyPrefab))
                replaced++;
            else
            {
                failed++;
                failedNames.Add(name);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        string msg = $"Replaced: {replaced}, Failed: {failed}";
        if (failedNames.Count > 0)
            msg += $"\nFailed: {string.Join(", ", failedNames)}";

        Debug.Log($"[ForceReplace] {msg}");
        Debug.Log($"[ForceReplace] DONE - {msg}");
    }

    static bool RebuildFromLowPoly(string name, string prefabPath, GameObject lowPolyPrefab)
    {
        var lowPolyInstance = (GameObject)PrefabUtility.InstantiatePrefab(lowPolyPrefab);
        if (lowPolyInstance == null)
        {
            Debug.LogError($"[ForceReplace] Failed to instantiate LowPoly_{name}");
            return false;
        }
        lowPolyInstance.hideFlags = HideFlags.HideAndDontSave;

        var parts = new List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(lowPolyInstance.transform, Matrix4x4.identity, parts);
        Object.DestroyImmediate(lowPolyInstance);

        if (parts.Count == 0)
        {
            Debug.LogError($"[ForceReplace] No mesh parts found in LowPoly_{name}");
            return false;
        }

        // Group by material for submesh creation
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
        combinedMesh.name = name;
        combinedMesh.CombineMeshes(finalCombines.ToArray(), false, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        // Save combined mesh
        string meshPath = $"{LowPolyDir}/{name}_Combined.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(combinedMesh, meshPath);

        float labelHeight = combinedMesh.bounds.max.y + 0.1f;

        // Build prefab
        var root = new GameObject(name);

        var modelGo = new GameObject(name.ToLower());
        modelGo.transform.SetParent(root.transform);
        modelGo.transform.localPosition = Vector3.zero;
        modelGo.transform.localRotation = Quaternion.identity;
        modelGo.transform.localScale = Vector3.one;

        var mf = modelGo.AddComponent<MeshFilter>();
        mf.sharedMesh = combinedMesh;

        var mr = modelGo.AddComponent<MeshRenderer>();
        mr.sharedMaterials = finalMaterials.ToArray();

        // Label child
        var labelGo = new GameObject($"Label_{name}");
        labelGo.transform.SetParent(root.transform);
        labelGo.transform.localPosition = new Vector3(0, labelHeight, 0);
        labelGo.transform.localRotation = Quaternion.identity;
        labelGo.transform.localScale = Vector3.one;

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = name;
        tm.characterSize = 0.06f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[ForceReplace] Rebuilt {name}: {combinedMesh.vertexCount} verts, " +
                  $"{finalMaterials.Count} materials, {parts.Count} parts");
        return true;
    }

    static void CollectMeshParts(Transform parent, Matrix4x4 parentMatrix,
        List<(Mesh mesh, Matrix4x4 transform, Material[] materials)> parts)
    {
        foreach (Transform child in parent)
        {
            var localMatrix = parentMatrix * Matrix4x4.TRS(
                child.localPosition, child.localRotation, child.localScale);

            var mf = child.GetComponent<MeshFilter>();
            var mr = child.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null && mr != null)
            {
                parts.Add((mf.sharedMesh, localMatrix, mr.sharedMaterials));
            }

            if (child.childCount > 0)
                CollectMeshParts(child, localMatrix, parts);
        }
    }
}
