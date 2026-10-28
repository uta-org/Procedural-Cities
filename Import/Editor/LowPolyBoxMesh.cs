using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared helpers of the box-built low-poly props (filing cabinet, office bench desk,
/// laptop): boxes combined into one mesh with a submesh per material, the mesh written over
/// its asset so its GUID stays, and the props' own URP Lit materials.
/// </summary>
public static class LowPolyBoxMesh
{
    public const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    public const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    public const string ModelDir = PkgRoot + "/Models/LowPoly";
    public const string MatDir = ModelDir + "/Materials";

    /// <summary>One box of a prop: its material, centre, size and rotation (about its centre).</summary>
    public readonly struct Part
    {
        public readonly Material Material;
        public readonly Vector3 Center;
        public readonly Vector3 Size;
        public readonly Quaternion Rotation;

        public Part(Material material, Vector3 center, Vector3 size, Quaternion rotation)
        {
            Material = material;
            Center = center;
            Size = size;
            Rotation = rotation;
        }

        /// <summary>An axis-aligned box from <paramref name="min"/> to <paramref name="max"/>.</summary>
        public static Part FromMinMax(Material material, Vector3 min, Vector3 max) =>
            new(material, (min + max) * 0.5f, max - min, Quaternion.identity);
    }

    /// <summary>The parts as one mesh named <paramref name="name"/>, one submesh per material.</summary>
    public static Mesh Combine(string name, List<Part> parts, out Material[] materials)
    {
        var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var byMaterial = new List<Material>();
        var instances = new List<List<CombineInstance>>();
        foreach (var part in parts)
        {
            var index = byMaterial.IndexOf(part.Material);
            if (index < 0)
            {
                byMaterial.Add(part.Material);
                instances.Add(new List<CombineInstance>());
                index = byMaterial.Count - 1;
            }

            instances[index].Add(new CombineInstance
            {
                mesh = cube,
                transform = Matrix4x4.TRS(part.Center, part.Rotation, part.Size),
            });
        }

        var subMeshes = new CombineInstance[byMaterial.Count];
        for (var i = 0; i < byMaterial.Count; i++)
        {
            var sub = new Mesh();
            sub.CombineMeshes(instances[i].ToArray(), true, true);
            subMeshes[i] = new CombineInstance { mesh = sub, transform = Matrix4x4.identity };
        }

        var mesh = new Mesh { name = name };
        mesh.CombineMeshes(subMeshes, false, false);
        mesh.RecalculateBounds();
        foreach (var sub in subMeshes)
            Object.DestroyImmediate(sub.mesh);
        materials = byMaterial.ToArray();
        return mesh;
    }

    /// <summary>Writes <paramref name="mesh"/> to Models/LowPoly/&lt;mesh name&gt;.asset, over the existing asset (GUID kept).</summary>
    public static Mesh SaveMesh(Mesh mesh)
    {
        var meshPath = $"{ModelDir}/{mesh.name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, meshPath);
            return mesh;
        }

        existing.Clear();
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    /// <summary>
    /// Saves a prefab <paramref name="prefabName"/> whose child carries the mesh and materials,
    /// with a box collider of <paramref name="colliderCenter"/> / <paramref name="colliderSize"/>.
    /// </summary>
    public static void SavePrefab(string prefabName, string childName, Mesh mesh, Material[] materials,
        Vector3 colliderCenter, Vector3 colliderSize)
    {
        var root = new GameObject(prefabName);
        try
        {
            var child = new GameObject(childName);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = colliderCenter;
            collider.size = colliderSize;
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{prefabName}.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log($"[LowPolyBoxMesh] Saved {prefabName} ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }

    /// <summary>The URP Lit material <paramref name="name"/> under Models/LowPoly/Materials, created when missing.</summary>
    public static Material GetMat(string name, Color color, float metallic, float smoothness)
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

    /// <summary>An existing material under Models/LowPoly/Materials, or null.</summary>
    public static Material LoadMat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
}
