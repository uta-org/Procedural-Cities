using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the shop counter module ("StoreCounter"): a waist-high wooden
/// service counter, 1.08 m wide, 0.65 m deep and 1.00 m tall, with a plinth,
/// a panelled front and a worktop that overhangs the front only. Its sides
/// are flat and flush with the worktop, so several modules butted together
/// read as one continuous counter. Replaces the KitchenTallCabinet kitchen
/// unit that the store "counter" used to borrow.
/// Output: Models/LowPoly/StoreCounter_Combined.asset (3 submeshes) and
/// Resources/Prefabs/AssetContents/StoreCounter.prefab (root "StoreCounter",
/// child "store_counter" with MeshFilter/MeshRenderer/convex MeshCollider,
/// same structure as KitchenSink.prefab).
/// Menu: Tools / Procedural Cities / Generate Store Counter Prop
/// </summary>
public static class GenerateStoreCounterProp
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string ModelDir = PkgRoot + "/Models/LowPoly";
    private const string MatDir = ModelDir + "/Materials";

    // Module size (metres). Front is +Z, the back (-Z) stands against the wall.
    private const float Width = 1.08f;
    private const float Depth = 0.65f;
    private const float Height = 1.00f;
    private const float TopThickness = 0.05f;
    private const float TopOverhang = 0.03f;     // front only
    private const float PlinthHeight = 0.10f;
    private const float PlinthRecess = 0.05f;    // toe kick, front only
    private const float PanelMargin = 0.06f;     // body left visible around the front panel
    private const float PanelRelief = 0.012f;

    private sealed class MeshBuilder
    {
        public readonly List<Vector3> Verts = new();
        public readonly List<Vector3> Norms = new();
        public readonly List<Vector2> Uvs = new();
        public readonly List<int>[] Tris;

        public MeshBuilder(int submeshes)
        {
            Tris = new List<int>[submeshes];
            for (var i = 0; i < submeshes; i++)
                Tris[i] = new List<int>();
        }

        /// <summary>Axis-aligned box from min/max corners into submesh <paramref name="sub"/>.</summary>
        public void Box(int sub, Vector3 min, Vector3 max)
        {
            var c = new[]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
            };
            Quad(sub, c[4], c[5], c[6], c[7], Vector3.forward);  // +Z
            Quad(sub, c[1], c[0], c[3], c[2], Vector3.back);     // -Z
            Quad(sub, c[5], c[1], c[2], c[6], Vector3.right);    // +X
            Quad(sub, c[0], c[4], c[7], c[3], Vector3.left);     // -X
            Quad(sub, c[7], c[6], c[2], c[3], Vector3.up);       // +Y
            Quad(sub, c[0], c[1], c[5], c[4], Vector3.down);     // -Y
        }

        private void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            var i = Verts.Count;
            Verts.Add(a); Verts.Add(b); Verts.Add(c); Verts.Add(d);
            for (var k = 0; k < 4; k++) Norms.Add(n);
            Uvs.Add(new Vector2(0, 0)); Uvs.Add(new Vector2(1, 0));
            Uvs.Add(new Vector2(1, 1)); Uvs.Add(new Vector2(0, 1));
            // a,b,c,d are listed so cross(b - a, c - a) points outward, which is
            // Unity's front-face (clockwise-from-outside) winding.
            Tris[sub].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, subMeshCount = Tris.Length };
            m.SetVertices(Verts);
            m.SetNormals(Norms);
            m.SetUVs(0, Uvs);
            for (var s = 0; s < Tris.Length; s++)
                m.SetTriangles(Tris[s], s);
            m.RecalculateBounds();
            return m;
        }
    }

    [MenuItem("Tools/Procedural Cities/Generate Store Counter Prop")]
    public static void Generate()
    {
        var matBody = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Wood_Bench.mat");
        var matPanel = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Wood_Wardrobe.mat");
        var matDark = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Wood_Dark.mat");
        if (matBody == null || matPanel == null || matDark == null)
        {
            Debug.LogError("[GenerateStoreCounterProp] Missing LP_Wood_* materials.");
            return;
        }

        const int Body = 0, Panel = 1, Dark = 2;
        var mb = new MeshBuilder(3);

        var hw = Width * 0.5f;
        var back = -Depth * 0.5f;
        var bodyFront = Depth * 0.5f - TopOverhang;
        var bodyTop = Height - TopThickness;

        // Plinth, set back from the front as a toe kick.
        mb.Box(Dark, new Vector3(-hw, 0f, back), new Vector3(hw, PlinthHeight, bodyFront - PlinthRecess));

        // Body: full width, so neighbouring modules meet side to side.
        mb.Box(Body, new Vector3(-hw, PlinthHeight, back), new Vector3(hw, bodyTop, bodyFront));

        // Raised front panel.
        mb.Box(Panel,
            new Vector3(-hw + PanelMargin, PlinthHeight + PanelMargin, bodyFront),
            new Vector3(hw - PanelMargin, bodyTop - PanelMargin, bodyFront + PanelRelief));

        // Worktop: overhangs the front only.
        mb.Box(Dark, new Vector3(-hw, bodyTop, back), new Vector3(hw, Height, Depth * 0.5f));

        var mesh = mb.ToMesh("StoreCounter_Combined");
        var meshPath = $"{ModelDir}/StoreCounter_Combined.asset";
        var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existingMesh != null)
        {
            // Rewrite in place: keeps the asset GUID the prefab points at.
            existingMesh.Clear();
            EditorUtility.CopySerialized(mesh, existingMesh);
            Object.DestroyImmediate(mesh);
            mesh = existingMesh;
            EditorUtility.SetDirty(mesh);
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var prefabPath = $"{PrefabDir}/StoreCounter.prefab";
        var root = new GameObject("StoreCounter");
        try
        {
            var child = new GameObject("store_counter");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = new[] { matBody, matPanel, matDark };
            var mc = child.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateStoreCounterProp] Saved {prefabPath} ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }
}
