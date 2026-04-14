using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fixes 6 broken Prefab Variants whose FBX parents were deleted.
/// Rebuilds them as standalone prefabs using existing LowPoly meshes.
/// Menu: Procedural Cities / Fix Broken Prefabs
/// </summary>
public static class FixBrokenPrefabs
{
    const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    const string MeshDir = PkgRoot + "/Models/LowPoly";
    const string MatDir  = MeshDir + "/Materials";
    const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    const string TreeModelPath = PkgRoot + "/Models/Tree/Tree N190616.3DS";

    struct MeshPart
    {
        public string meshAssetName; // e.g. "LowPoly_Glass_Glass"
        public string materialName;  // e.g. "LP_Glass_Clear"
    }

    [MenuItem("Procedural Cities/Fix Broken Prefabs")]
    static void Fix()
    {
        int fixed_ = 0;
        try
        {
            fixed_ += RebuildPrefab("Glass", GetGlassParts(), 0.12f);
            fixed_ += RebuildPrefab("Chair", GetChairParts(), 0.87f);
            fixed_ += RebuildPrefab("Door", GetDoorParts(), 2.0f);
            fixed_ += RebuildPrefab("Clock", GetClockParts(), 0.30f);
            fixed_ += RebuildPrefab("Oven", GetOvenParts(), 0.87f);
            fixed_ += RebuildTree();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FixBrokenPrefabs] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[FixBrokenPrefabs] Fixed {fixed_}/6 prefabs.");
        EditorUtility.DisplayDialog("Fix Broken Prefabs",
            $"Fixed {fixed_}/6 prefabs.\nCheck console for details.", "OK");
    }

    static int RebuildPrefab(string name, List<MeshPart> parts, float labelHeight)
    {
        string prefabPath = $"{PrefabDir}/{name}.prefab";

        // Load and combine meshes
        var combineInstances = new List<CombineInstance>();
        var materialList = new List<Material>();
        var materialMap = new Dictionary<string, int>(); // matName -> submesh index

        foreach (var part in parts)
        {
            string meshPath = $"{MeshDir}/{part.meshAssetName}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                Debug.LogError($"[FixBrokenPrefabs] Mesh not found: {meshPath}");
                return 0;
            }

            string matPath = $"{MatDir}/{part.materialName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Debug.LogError($"[FixBrokenPrefabs] Material not found: {matPath}");
                return 0;
            }

            // Group by material: same material -> merge into one submesh
            if (!materialMap.ContainsKey(part.materialName))
            {
                materialMap[part.materialName] = materialList.Count;
                materialList.Add(mat);
            }

            combineInstances.Add(new CombineInstance
            {
                mesh = mesh,
                transform = Matrix4x4.identity,
                subMeshIndex = 0
            });
        }

        // If all parts use same material, combine into one submesh
        // If different materials, combine per-material group
        Mesh combinedMesh;
        Material[] finalMaterials;

        if (materialMap.Count == 1)
        {
            // All same material - merge everything
            combinedMesh = new Mesh();
            combinedMesh.name = name;
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, false);
            finalMaterials = new Material[] { materialList[0] };
        }
        else
        {
            // Group parts by material, combine each group, then merge groups as submeshes
            var groupedMeshes = new List<CombineInstance>();
            var orderedMaterials = new List<Material>();

            foreach (var kvp in materialMap)
            {
                string matName = kvp.Key;
                var mat = materialList[kvp.Value];
                orderedMaterials.Add(mat);

                // Find all parts with this material
                var group = new List<CombineInstance>();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].materialName == matName)
                        group.Add(combineInstances[i]);
                }

                // Combine this material group into one mesh
                var groupMesh = new Mesh();
                groupMesh.CombineMeshes(group.ToArray(), true, false);

                groupedMeshes.Add(new CombineInstance
                {
                    mesh = groupMesh,
                    transform = Matrix4x4.identity,
                    subMeshIndex = 0
                });
            }

            // Combine all groups as separate submeshes
            combinedMesh = new Mesh();
            combinedMesh.name = name;
            combinedMesh.CombineMeshes(groupedMeshes.ToArray(), false, false);
            finalMaterials = orderedMaterials.ToArray();
        }

        // Save combined mesh as asset
        string combinedMeshPath = $"{MeshDir}/{name}_Combined.asset";
        var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(combinedMeshPath);
        if (existingMesh != null)
            AssetDatabase.DeleteAsset(combinedMeshPath);
        AssetDatabase.CreateAsset(combinedMesh, combinedMeshPath);

        // Build prefab hierarchy: Root -> ModelChild + LabelChild
        var root = new GameObject(name);

        // Model child
        var modelGo = new GameObject(name.ToLower());
        modelGo.transform.SetParent(root.transform);
        modelGo.transform.localPosition = Vector3.zero;
        modelGo.transform.localRotation = Quaternion.identity;
        modelGo.transform.localScale = Vector3.one;

        var mf = modelGo.AddComponent<MeshFilter>();
        mf.sharedMesh = combinedMesh;

        var mr = modelGo.AddComponent<MeshRenderer>();
        mr.sharedMaterials = finalMaterials;

        // Label child
        var labelGo = new GameObject($"Label_{name}");
        labelGo.transform.SetParent(root.transform);
        labelGo.transform.localPosition = new Vector3(0, labelHeight + 0.1f, 0);
        labelGo.transform.localRotation = Quaternion.identity;
        labelGo.transform.localScale = Vector3.one;

        var labelMr = labelGo.AddComponent<MeshRenderer>();
        labelMr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = name;
        tm.characterSize = 0.06f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;

        // Save prefab (overwrites broken file, .meta preserved)
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[FixBrokenPrefabs] Rebuilt {name} ({combinedMesh.vertexCount} verts, {finalMaterials.Length} mats)");
        return 1;
    }

    static int RebuildTree()
    {
        string prefabPath = $"{PrefabDir}/Tree.prefab";

        // Load meshes from the 3DS model
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(TreeModelPath);
        var meshes = allAssets.OfType<Mesh>().ToArray();
        var mats = allAssets.OfType<Material>().ToArray();

        if (meshes.Length == 0)
        {
            Debug.LogError($"[FixBrokenPrefabs] No meshes found in {TreeModelPath}");
            return 0;
        }

        var root = new GameObject("Tree");

        // Model child
        var modelGo = new GameObject("tree");
        modelGo.transform.SetParent(root.transform);
        modelGo.transform.localPosition = Vector3.zero;
        modelGo.transform.localRotation = Quaternion.identity;
        modelGo.transform.localScale = Vector3.one;

        // Use the first (largest) mesh
        var mf = modelGo.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes[0];

        var mr = modelGo.AddComponent<MeshRenderer>();
        if (mats.Length > 0)
            mr.sharedMaterials = mats;

        // If there are additional meshes, add them as children
        for (int i = 1; i < meshes.Length; i++)
        {
            var partGo = new GameObject($"tree_part{i}");
            partGo.transform.SetParent(root.transform);
            partGo.transform.localPosition = Vector3.zero;
            partGo.transform.localRotation = Quaternion.identity;
            partGo.transform.localScale = Vector3.one;

            var partMf = partGo.AddComponent<MeshFilter>();
            partMf.sharedMesh = meshes[i];

            var partMr = partGo.AddComponent<MeshRenderer>();
            if (i < mats.Length)
                partMr.sharedMaterial = mats[i];
            else if (mats.Length > 0)
                partMr.sharedMaterial = mats[0];
        }

        // Compute bounds for label positioning
        float maxY = 4.5f;
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            maxY = bounds.max.y;
        }

        // Label child
        var labelGo = new GameObject("Label_Tree");
        labelGo.transform.SetParent(root.transform);
        labelGo.transform.localPosition = new Vector3(0, maxY + 0.3f, 0);
        labelGo.transform.localRotation = Quaternion.identity;
        labelGo.transform.localScale = Vector3.one;

        var labelMr = labelGo.AddComponent<MeshRenderer>();
        labelMr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "Tree";
        tm.characterSize = 0.06f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[FixBrokenPrefabs] Rebuilt Tree from 3DS model ({meshes.Length} meshes, {mats.Length} mats)");
        return 1;
    }

    // ====== Part definitions ======

    static List<MeshPart> GetGlassParts() => new List<MeshPart>
    {
        new MeshPart { meshAssetName = "LowPoly_Glass_Glass", materialName = "LP_Glass_Clear" },
    };

    static List<MeshPart> GetChairParts() => new List<MeshPart>
    {
        new MeshPart { meshAssetName = "LowPoly_Chair_Seat",  materialName = "LP_Wood_Chair" },
        new MeshPart { meshAssetName = "LowPoly_Chair_Back",  materialName = "LP_Wood_Chair" },
        new MeshPart { meshAssetName = "LowPoly_Chair_Leg0",  materialName = "LP_Wood_Chair" },
        new MeshPart { meshAssetName = "LowPoly_Chair_Leg1",  materialName = "LP_Wood_Chair" },
        new MeshPart { meshAssetName = "LowPoly_Chair_Leg2",  materialName = "LP_Wood_Chair" },
        new MeshPart { meshAssetName = "LowPoly_Chair_Leg3",  materialName = "LP_Wood_Chair" },
    };

    static List<MeshPart> GetDoorParts() => new List<MeshPart>
    {
        new MeshPart { meshAssetName = "LowPoly_Door_Panel",      materialName = "LP_Wood_Door" },
        new MeshPart { meshAssetName = "LowPoly_Door_UpperPanel",  materialName = "LP_Wood_Door" },
        new MeshPart { meshAssetName = "LowPoly_Door_LowerPanel",  materialName = "LP_Wood_Door" },
        new MeshPart { meshAssetName = "LowPoly_Door_Handle",      materialName = "LP_Metal_Handle" },
    };

    static List<MeshPart> GetClockParts() => new List<MeshPart>
    {
        new MeshPart { meshAssetName = "LowPoly_Clock_Body",       materialName = "LP_Plastic_White" },
        new MeshPart { meshAssetName = "LowPoly_Clock_Face",       materialName = "LP_Clock_Face" },
        new MeshPart { meshAssetName = "LowPoly_Clock_HourHand",   materialName = "LP_Metal_Black" },
        new MeshPart { meshAssetName = "LowPoly_Clock_MinuteHand", materialName = "LP_Metal_Black" },
    };

    static List<MeshPart> GetOvenParts() => new List<MeshPart>
    {
        new MeshPart { meshAssetName = "LowPoly_Oven_Body",      materialName = "LP_Appliance_White" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Stovetop",  materialName = "LP_Appliance_White" },
        new MeshPart { meshAssetName = "LowPoly_Oven_DoorWindow", materialName = "LP_Oven_Glass" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Burner0",   materialName = "LP_Metal_Black" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Burner1",   materialName = "LP_Metal_Black" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Burner2",   materialName = "LP_Metal_Black" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Burner3",   materialName = "LP_Metal_Black" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Knob0",     materialName = "LP_Appliance_Silver" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Knob1",     materialName = "LP_Appliance_Silver" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Knob2",     materialName = "LP_Appliance_Silver" },
        new MeshPart { meshAssetName = "LowPoly_Oven_Knob3",     materialName = "LP_Appliance_Silver" },
    };
}
