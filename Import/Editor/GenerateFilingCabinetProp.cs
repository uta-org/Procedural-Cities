using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds FilingCabinet (P33): a low-poly four-drawer office filing cabinet, 0.47 m wide,
/// 0.62 m deep and 1.32 m tall, pivot on the floor at its centre, front +Z (its back goes
/// against a wall, as the kitchen modules). Light-grey steel body, four drawer fronts
/// standing 0.012 m proud with a gap between them, a chrome pull on each.
/// Output: Models/LowPoly/FilingCabinet_Combined.asset (one submesh per material) and
/// Resources/Prefabs/AssetContents/FilingCabinet.prefab, rewritten in place so their GUIDs
/// stay. Menu: Tools / Procedural Cities / Generate Filing Cabinet Prop
/// </summary>
public static class GenerateFilingCabinetProp
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string ModelDir = PkgRoot + "/Models/LowPoly";
    private const string MatDir = ModelDir + "/Materials";

    private const float Width = 0.47f;
    private const float Depth = 0.62f;
    private const float Height = 1.32f;
    private const float Plinth = 0.04f;
    private const int Drawers = 4;
    private const float DrawerGap = 0.012f;
    private const float DrawerRelief = 0.012f;

    [MenuItem("Tools/Procedural Cities/Generate Filing Cabinet Prop")]
    public static void Generate()
    {
        var steel = GetMat("LP_FilingCabinet_Steel", new Color(0.66f, 0.68f, 0.70f), 0.35f, 0.45f);
        var chrome = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Chrome.mat");
        var dark = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Metal_DarkGray.mat");
        if (!chrome || !dark)
        {
            Debug.LogError("[GenerateFilingCabinetProp] Missing LP_Chrome / LP_Metal_DarkGray.");
            return;
        }

        var parts = new List<(Material Material, Vector3 Min, Vector3 Max)>();
        var hw = Width * 0.5f;
        var hd = Depth * 0.5f;

        // Body on a dark plinth set back from the front.
        parts.Add((dark, new Vector3(-hw + 0.02f, 0f, -hd + 0.02f), new Vector3(hw - 0.02f, Plinth, hd - 0.04f)));
        parts.Add((steel, new Vector3(-hw, Plinth, -hd), new Vector3(hw, Height, hd)));

        // Drawer fronts and their pulls.
        var drawerHeight = (Height - Plinth - DrawerGap * (Drawers + 1)) / Drawers;
        for (var i = 0; i < Drawers; i++)
        {
            var bottom = Plinth + DrawerGap + i * (drawerHeight + DrawerGap);
            parts.Add((steel, new Vector3(-hw + 0.015f, bottom, hd), new Vector3(hw - 0.015f, bottom + drawerHeight, hd + DrawerRelief)));
            var pullY = bottom + drawerHeight * 0.62f;
            parts.Add((chrome, new Vector3(-0.07f, pullY, hd + DrawerRelief), new Vector3(0.07f, pullY + 0.02f, hd + DrawerRelief + 0.025f)));
            // Label holder above the pull.
            parts.Add((dark, new Vector3(-0.045f, pullY + 0.05f, hd + DrawerRelief), new Vector3(0.045f, pullY + 0.085f, hd + DrawerRelief + 0.004f)));
        }

        var mesh = Combine(parts, out var materials);
        SaveMesh(ref mesh);

        var root = new GameObject("FilingCabinet");
        try
        {
            var child = new GameObject("filingcabinet");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, Height * 0.5f, DrawerRelief * 0.5f);
            collider.size = new Vector3(Width, Height, Depth + DrawerRelief);
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/FilingCabinet.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateFilingCabinetProp] Saved FilingCabinet ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }

    /// <summary>One mesh of axis-aligned boxes, one submesh per material.</summary>
    private static Mesh Combine(List<(Material Material, Vector3 Min, Vector3 Max)> parts, out Material[] materials)
    {
        var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var byMaterial = new List<Material>();
        var instances = new List<List<CombineInstance>>();
        foreach (var (material, min, max) in parts)
        {
            var index = byMaterial.IndexOf(material);
            if (index < 0)
            {
                byMaterial.Add(material);
                instances.Add(new List<CombineInstance>());
                index = byMaterial.Count - 1;
            }

            instances[index].Add(new CombineInstance
            {
                mesh = cube,
                transform = Matrix4x4.TRS((min + max) * 0.5f, Quaternion.identity, max - min),
            });
        }

        var subMeshes = new CombineInstance[byMaterial.Count];
        for (var i = 0; i < byMaterial.Count; i++)
        {
            var sub = new Mesh();
            sub.CombineMeshes(instances[i].ToArray(), true, true);
            subMeshes[i] = new CombineInstance { mesh = sub, transform = Matrix4x4.identity };
        }

        var mesh = new Mesh { name = "FilingCabinet_Combined" };
        mesh.CombineMeshes(subMeshes, false, false);
        mesh.RecalculateBounds();
        foreach (var sub in subMeshes)
            Object.DestroyImmediate(sub.mesh);
        materials = byMaterial.ToArray();
        return mesh;
    }

    /// <summary>Writes the mesh over its existing asset (GUID kept), or creates it.</summary>
    private static void SaveMesh(ref Mesh mesh)
    {
        var meshPath = $"{ModelDir}/FilingCabinet_Combined.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, meshPath);
            return;
        }

        existing.Clear();
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        mesh = existing;
        EditorUtility.SetDirty(mesh);
    }

    private static Material GetMat(string name, Color color, float metallic, float smoothness)
    {
        var path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
