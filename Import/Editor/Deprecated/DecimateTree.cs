#if HAS_UNITY_MESH_SIMPLIFIER
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;
using System.Linq;

/// <summary>
/// Decimates the Tree 3DS model using UnityMeshSimplifier.
/// Saves simplified meshes as .asset files and rebuilds the Tree.prefab.
/// Menu: Procedural Cities / Decimate Tree
/// </summary>
public static class DecimateTree
{
    const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    const string TreeModelPath = PkgRoot + "/Models/Tree/Tree N190616.3DS";
    const string OutputDir = PkgRoot + "/Models/Tree/Simplified";
    const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";

    // Target quality: 0.01 = keep 1% of triangles (301k -> ~3k)
    const float Quality = 0.01f;

    // [MenuItem("Procedural Cities/Decimate Tree")]
    static void Decimate()
    {
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(TreeModelPath);
        var meshes = allAssets.OfType<Mesh>().ToArray();
        var mats = allAssets.OfType<Material>().ToArray();

        if (meshes.Length == 0)
        {
            Debug.LogError($"[DecimateTree] No meshes found in {TreeModelPath}. Make sure Unity has imported the 3DS file.");
            return;
        }

        // Ensure output directory
        if (!AssetDatabase.IsValidFolder(OutputDir))
        {
            // Create intermediate folders
            string parent = PkgRoot + "/Models/Tree";
            AssetDatabase.CreateFolder(parent, "Simplified");
        }

        int totalOrigVerts = 0, totalOrigTris = 0;
        int totalSimplVerts = 0, totalSimplTris = 0;

        var simplifiedMeshes = new Mesh[meshes.Length];

        for (int i = 0; i < meshes.Length; i++)
        {
            var srcMesh = meshes[i];
            int origVerts = srcMesh.vertexCount;
            int origTris = srcMesh.triangles.Length / 3;
            totalOrigVerts += origVerts;
            totalOrigTris += origTris;

            var simplifier = new MeshSimplifier(srcMesh);
            simplifier.SimplifyMesh(Quality);
            var simplified = simplifier.ToMesh();
            simplified.name = $"Tree_Simplified_{i}";

            int simplVerts = simplified.vertexCount;
            int simplTris = simplified.triangles.Length / 3;
            totalSimplVerts += simplVerts;
            totalSimplTris += simplTris;

            Debug.Log($"[DecimateTree] Mesh {i} '{srcMesh.name}': {origVerts} -> {simplVerts} verts, {origTris} -> {simplTris} tris");

            // Save as persistent asset
            string assetPath = $"{OutputDir}/Tree_Simplified_{i}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(simplified, assetPath);
            simplifiedMeshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DecimateTree] TOTAL: {totalOrigVerts} -> {totalSimplVerts} verts ({(float)totalSimplVerts / totalOrigVerts:P1}), " +
                  $"{totalOrigTris} -> {totalSimplTris} tris ({(float)totalSimplTris / totalOrigTris:P1})");

        // Rebuild Tree.prefab with simplified meshes
        RebuildPrefab(simplifiedMeshes, mats);
    }

    static void RebuildPrefab(Mesh[] meshes, Material[] mats)
    {
        string prefabPath = $"{PrefabDir}/Tree.prefab";

        var root = new GameObject("Tree");

        // Main mesh child
        var modelGo = new GameObject("tree");
        modelGo.transform.SetParent(root.transform);
        modelGo.transform.localPosition = Vector3.zero;
        modelGo.transform.localRotation = Quaternion.identity;
        modelGo.transform.localScale = Vector3.one;

        var mf = modelGo.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes[0];

        var mr = modelGo.AddComponent<MeshRenderer>();
        if (mats.Length > 0)
            mr.sharedMaterials = mats;

        // Additional mesh children
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
            partMr.sharedMaterial = i < mats.Length ? mats[i] : mats[0];
        }

        // Compute bounds for label
        float maxY = 4.5f;
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            maxY = bounds.max.y;
        }

        // Label
        var labelGo = new GameObject("Label_Tree");
        labelGo.transform.SetParent(root.transform);
        labelGo.transform.localPosition = new Vector3(0, maxY + 0.3f, 0);
        labelGo.transform.localRotation = Quaternion.identity;
        labelGo.transform.localScale = Vector3.one;

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "Tree";
        tm.characterSize = 0.06f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.black;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[DecimateTree] Rebuilt Tree.prefab with {meshes.Length} simplified meshes");
    }
}
#endif
