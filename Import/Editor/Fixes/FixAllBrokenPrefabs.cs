using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Rebuilds ALL AssetContents prefabs whose meshes are missing (FBX deleted).
/// Uses the corresponding LowPoly_*.prefab as source: instantiates it, extracts
/// all MeshFilter+MeshRenderer children with their local transforms, combines
/// meshes per-material group, saves as _Combined.asset, and rebuilds the prefab.
/// Menu: Tools / Procedural Cities / Fix All Broken Prefabs
/// </summary>
public static class FixAllBrokenPrefabs
{
    const string PkgRoot   = "Packages/dev.z3nth10n.proceduralcities.import";
    const string LowPolyDir = PkgRoot + "/Models/LowPoly";
    const string PrefabDir  = PkgRoot + "/Resources/Prefabs/AssetContents";
    const string TreeModelPath = PkgRoot + "/Models/Tree/Tree N190616.3DS";

    // [MenuItem("Tools/uzProceduralCities/Fix All Broken Prefabs")]
    static void FixAll()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
        int fixedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;
        var failedNames = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            EditorUtility.DisplayProgressBar("Fixing Prefabs",
                $"Processing {prefabName} ({i + 1}/{guids.Length})", (float)i / guids.Length);

            // Load prefab and check if it has valid meshes
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            if (HasValidMeshes(prefab))
            {
                skippedCount++;
                continue;
            }

            // Special case: Tree uses 3DS model, not LowPoly prefab
            if (prefabName == "Tree")
            {
                if (RebuildTree(prefabPath))
                    fixedCount++;
                else
                {
                    failedCount++;
                    failedNames.Add(prefabName);
                }
                continue;
            }

            // Find corresponding LowPoly prefab
            string lowPolyPath = $"{LowPolyDir}/LowPoly_{prefabName}.prefab";
            var lowPolyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lowPolyPath);
            if (lowPolyPrefab == null)
            {
                Debug.LogWarning($"[FixAll] No LowPoly prefab for '{prefabName}' at {lowPolyPath}");
                failedCount++;
                failedNames.Add(prefabName);
                continue;
            }

            if (RebuildFromLowPoly(prefabName, prefabPath, lowPolyPrefab))
                fixedCount++;
            else
            {
                failedCount++;
                failedNames.Add(prefabName);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        string msg = $"Fixed: {fixedCount}, Skipped (OK): {skippedCount}, Failed: {failedCount}";
        if (failedNames.Count > 0)
            msg += $"\nFailed: {string.Join(", ", failedNames)}";

        Debug.Log($"[FixAll] {msg}");
        // EditorUtility.DisplayDialog("Fix All Broken Prefabs", msg, "OK");
    }

    static bool HasValidMeshes(GameObject prefab)
    {
        var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters.Length == 0) return false;

        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh != null)
                return true;
        }

        // All MeshFilters have null mesh → broken
        return false;
    }

    static bool RebuildFromLowPoly(string name, string prefabPath, GameObject lowPolyPrefab)
    {
        // Instantiate the LowPoly prefab to read its structure
        var lowPolyInstance = (GameObject)PrefabUtility.InstantiatePrefab(lowPolyPrefab);
        if (lowPolyInstance == null)
        {
            Debug.LogError($"[FixAll] Failed to instantiate LowPoly_{name}");
            return false;
        }
        lowPolyInstance.hideFlags = HideFlags.HideAndDontSave;

        // Collect all mesh parts with their local transforms and materials
        var parts = new List<(Mesh mesh, Matrix4x4 transform, Material[] materials)>();
        CollectMeshParts(lowPolyInstance.transform, Matrix4x4.identity, parts);

        Object.DestroyImmediate(lowPolyInstance);

        if (parts.Count == 0)
        {
            Debug.LogError($"[FixAll] No mesh parts found in LowPoly_{name}");
            return false;
        }

        // Group by material signature for submesh creation
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

        // Combine per material group, then merge groups as submeshes
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

            // Use first material from the group
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

        // Compute bounds for label height
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

        // Label child (TextMesh auto-creates MeshRenderer with font material)
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

        Debug.Log($"[FixAll] Rebuilt {name}: {combinedMesh.vertexCount} verts, " +
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

            // Recurse into children
            if (child.childCount > 0)
                CollectMeshParts(child, localMatrix, parts);
        }
    }

    static bool RebuildTree(string prefabPath)
    {
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(TreeModelPath);
        var meshes = allAssets.OfType<Mesh>().ToArray();
        var mats = allAssets.OfType<Material>().ToArray();

        if (meshes.Length == 0)
        {
            Debug.LogError($"[FixAll] No meshes in {TreeModelPath}");
            return false;
        }

        var root = new GameObject("Tree");

        var modelGo = new GameObject("tree");
        modelGo.transform.SetParent(root.transform);

        var mf = modelGo.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes[0];

        var mr = modelGo.AddComponent<MeshRenderer>();
        if (mats.Length > 0)
            mr.sharedMaterials = mats;

        for (int i = 1; i < meshes.Length; i++)
        {
            var partGo = new GameObject($"tree_part{i}");
            partGo.transform.SetParent(root.transform);

            var partMf = partGo.AddComponent<MeshFilter>();
            partMf.sharedMesh = meshes[i];

            var partMr = partGo.AddComponent<MeshRenderer>();
            partMr.sharedMaterial = i < mats.Length ? mats[i] : mats[0];
        }

        float maxY = 4.5f;
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            maxY = bounds.max.y;
        }

        var labelGo = new GameObject("Label_Tree");
        labelGo.transform.SetParent(root.transform);
        labelGo.transform.localPosition = new Vector3(0, maxY + 0.3f, 0);

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "Tree";
        tm.characterSize = 0.06f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[FixAll] Rebuilt Tree from 3DS ({meshes.Length} meshes)");
        return true;
    }

    // ─────────────────────────────────────────────────────
    //  FIX LABELS (repair existing prefabs with wrong material)
    // ─────────────────────────────────────────────────────
    // [MenuItem("Tools/uzProceduralCities/Fix Prefab Labels")]
    static void FixLabels()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
        int fixedCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var textMeshes = prefab.GetComponentsInChildren<TextMesh>(true);
            if (textMeshes.Length == 0) continue;

            bool needsFix = false;
            foreach (var textMesh in textMeshes)
            {
                var meshRenderer = textMesh.GetComponent<MeshRenderer>();
                if (meshRenderer != null && meshRenderer.sharedMaterial != null
                    && meshRenderer.sharedMaterial.name == "Default-Diffuse")
                {
                    needsFix = true;
                    break;
                }
            }

            if (!needsFix) continue;

            // Load prefab contents for editing
            string prefabContentsPath = path;
            var root = PrefabUtility.LoadPrefabContents(prefabContentsPath);

            foreach (var textMesh in root.GetComponentsInChildren<TextMesh>(true))
            {
                var meshRenderer = textMesh.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    // Reset to font material (TextMesh default)
                    meshRenderer.sharedMaterial = textMesh.font != null
                        ? textMesh.font.material
                        : null;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabContentsPath);
            PrefabUtility.UnloadPrefabContents(root);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FixAll] Fixed labels on {fixedCount} prefabs.");
    }
}
