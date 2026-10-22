using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the swing gates that close the way in of a counter: a pair of
/// waist-high leaves, saloon style, that fill the one module slot the U of a
/// store counter or a restaurant bar leaves open in an arm ("so there is never
/// a gap between one module of a bar and the next").
/// <list type="bullet">
/// <item><c>StoreCounterGate</c>: 1.08 m opening (the StoreCounter module), 1.00 m tall,
/// the counter's woods.</item>
/// <item><c>RestaurantBarGate</c>: 1.05 m opening (between the bar modules laid every 1.05 m,
/// each rendered as wide), 1.10 m tall up to the bar's edge, the bar's dark wood and edge.</item>
/// </list>
/// Each prefab: root (pivot on the floor, centre of the opening; +Z is the
/// customers' side like the counter modules, -Z the staff side) with
/// children <c>Posts</c> (a fixed post at each end of the opening, flush
/// with the module or wall beside it, one mesh and a BoxCollider per post)
/// and <c>HingeLeft</c> at -X and <c>HingeRight</c> at +X, just inside the
/// posts, each holding a <c>Leaf</c> (one shared mesh, the right one turned
/// 180°) with a BoxCollider. A hinge's axis is on the +Z face of its leaf
/// and the leaf lies behind it, so a leaf opening towards +Z only ever turns
/// towards the middle of the opening: it never cuts into its post or the
/// module beyond (whose worktop or foot rail sticks out at the front). The
/// leaves are the same on both faces. The runtime CounterGate component
/// (added when a room's furniture spawns) turns the hinges about Y to open.
/// Output: Models/LowPoly/&lt;Name&gt;Leaf.asset, &lt;Name&gt;Posts.asset and
/// Resources/Prefabs/AssetContents/&lt;Name&gt;.prefab.
/// Menu: Tools / Procedural Cities / Generate Counter Gate Props
/// </summary>
public static class GenerateCounterGateProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string ModelDir = PkgRoot + "/Models/LowPoly";
    private const string MatDir = ModelDir + "/Materials";
    private const string ProjectMatDir = "Assets/VIZZUTA/uzProceduralCities/Materials";

    private const float NeighbourClearance = 0.002f; // between a post and the module or wall beside it
    private const float PostWidth = 0.03f;
    private const float PostDepth = 0.06f;         // from the hinge line (z = 0) towards -Z
    private const float HingeGap = 0.005f;         // between a post and its leaf
    private const float CentreClearance = 0.01f;   // between the two leaves when closed
    private const float LeafBottom = 0.10f;        // a swing gate clears the floor
    private const float LeafThickness = 0.04f;
    private const float FrameWidth = 0.06f;        // stiles and top rail
    private const float BottomRailHeight = 0.10f;
    private const float PanelThickness = 0.024f;   // the recessed infill
    private const float CapHeight = 0.04f;

    /// <summary>One gate model.</summary>
    private readonly struct GateSpec
    {
        public readonly string Name;
        public readonly float Opening;
        public readonly float Height;
        public readonly string FrameMat;
        public readonly string PanelMat;
        public readonly string CapMat;

        public GateSpec(string name, float opening, float height, string frameMat, string panelMat, string capMat)
        {
            Name = name;
            Opening = opening;
            Height = height;
            FrameMat = frameMat;
            PanelMat = panelMat;
            CapMat = capMat;
        }
    }

    private static readonly GateSpec[] Specs =
    {
        new("StoreCounterGate", 1.08f, 1.00f,
            $"{MatDir}/LP_Wood_Bench.mat", $"{MatDir}/LP_Wood_Wardrobe.mat", $"{MatDir}/LP_Wood_Dark.mat"),
        new("RestaurantBarGate", 1.05f, 1.10f,
            $"{MatDir}/LP_Wood_Dark.mat", $"{MatDir}/LP_Wood_Dark.mat", $"{ProjectMatDir}/BarCounterEdge.mat"),
    };

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

    [MenuItem("Tools/Procedural Cities/Generate Counter Gate Props")]
    public static void Generate()
    {
        foreach (var spec in Specs)
            Generate(spec);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Generate(GateSpec spec)
    {
        var matFrame = AssetDatabase.LoadAssetAtPath<Material>(spec.FrameMat);
        var matPanel = AssetDatabase.LoadAssetAtPath<Material>(spec.PanelMat);
        var matCap = AssetDatabase.LoadAssetAtPath<Material>(spec.CapMat);
        if (matFrame == null || matPanel == null || matCap == null)
        {
            Debug.LogError($"[GenerateCounterGateProps] Missing materials for {spec.Name}.");
            return;
        }

        // One leaf, from its hinge (x = 0) towards +X.
        const int Frame = 0, Panel = 1, Cap = 2;
        var postOuter = spec.Opening * 0.5f - NeighbourClearance;
        var postInner = postOuter - PostWidth;
        var hingeX = postInner - HingeGap;
        var leafWidth = hingeX - CentreClearance * 0.5f;
        var top = spec.Height - CapHeight;
        var ht = LeafThickness * 0.5f;
        var hp = PanelThickness * 0.5f;
        var mb = new MeshBuilder(3);

        // Stiles, bottom rail and top rail.
        mb.Box(Frame, new Vector3(0f, LeafBottom, -ht), new Vector3(FrameWidth, top, ht));
        mb.Box(Frame, new Vector3(leafWidth - FrameWidth, LeafBottom, -ht), new Vector3(leafWidth, top, ht));
        mb.Box(Frame, new Vector3(FrameWidth, LeafBottom, -ht), new Vector3(leafWidth - FrameWidth, LeafBottom + BottomRailHeight, ht));
        mb.Box(Frame, new Vector3(FrameWidth, top - FrameWidth, -ht), new Vector3(leafWidth - FrameWidth, top, ht));

        // Recessed infill, the same on both faces.
        mb.Box(Panel, new Vector3(FrameWidth, LeafBottom + BottomRailHeight, -hp), new Vector3(leafWidth - FrameWidth, top - FrameWidth, hp));

        // Cap level with the counter's top.
        mb.Box(Cap, new Vector3(0f, top, -ht), new Vector3(leafWidth, spec.Height, ht));

        var mesh = SaveMesh(mb.ToMesh($"{spec.Name}Leaf"), $"{ModelDir}/{spec.Name}Leaf.asset");
        var materials = new[] { matFrame, matPanel, matCap };

        // The two posts, floor to cap, behind the hinge line like the leaves:
        // submesh 0 the frame wood, 1 the cap.
        var posts = new MeshBuilder(2);
        foreach (var side in new[] { -1f, 1f })
        {
            var x0 = side < 0f ? -postOuter : postInner;
            var x1 = side < 0f ? -postInner : postOuter;
            posts.Box(0, new Vector3(x0, 0f, -PostDepth), new Vector3(x1, top, 0f));
            posts.Box(1, new Vector3(x0, top, -PostDepth), new Vector3(x1, spec.Height, 0f));
        }
        var postMesh = SaveMesh(posts.ToMesh($"{spec.Name}Posts"), $"{ModelDir}/{spec.Name}Posts.asset");

        var prefabPath = $"{PrefabDir}/{spec.Name}.prefab";
        var root = new GameObject(spec.Name);
        try
        {
            AddPosts(root.transform, postMesh, new[] { matFrame, matCap }, postInner, postOuter, spec.Height);
            AddLeaf(root.transform, "HingeLeft", -hingeX, 0f, mesh, materials, leafWidth, spec.Height);
            AddLeaf(root.transform, "HingeRight", hingeX, 180f, mesh, materials, leafWidth, spec.Height);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log($"[GenerateCounterGateProps] Saved {prefabPath} (leaf {leafWidth:F3} m wide, {mesh.vertexCount} verts).");
    }

    /// <summary>
    /// A hinge at <paramref name="x"/> and its leaf, turned <paramref name="leafYaw"/>
    /// about the hinge and set behind it (-Z), so the hinge axis is on the leaf's +Z face.
    /// </summary>
    private static void AddLeaf(
        Transform root, string hingeName, float x, float leafYaw, Mesh mesh, Material[] materials, float leafWidth, float height)
    {
        var hinge = new GameObject(hingeName).transform;
        hinge.SetParent(root, false);
        hinge.localPosition = new Vector3(x, 0f, 0f);

        var leaf = new GameObject("Leaf");
        leaf.transform.SetParent(hinge, false);
        leaf.transform.SetLocalPositionAndRotation(
            new Vector3(0f, 0f, -LeafThickness * 0.5f), Quaternion.Euler(0f, leafYaw, 0f));
        leaf.AddComponent<MeshFilter>().sharedMesh = mesh;
        leaf.AddComponent<MeshRenderer>().sharedMaterials = materials;
        var box = leaf.AddComponent<BoxCollider>();
        box.center = new Vector3(leafWidth * 0.5f, (LeafBottom + height) * 0.5f, 0f);
        box.size = new Vector3(leafWidth, height - LeafBottom, LeafThickness);
    }

    /// <summary>The fixed posts at the ends of the opening: one mesh, a BoxCollider each.</summary>
    private static void AddPosts(
        Transform root, Mesh mesh, Material[] materials, float postInner, float postOuter, float height)
    {
        var posts = new GameObject("Posts");
        posts.transform.SetParent(root, false);
        posts.AddComponent<MeshFilter>().sharedMesh = mesh;
        posts.AddComponent<MeshRenderer>().sharedMaterials = materials;
        foreach (var side in new[] { -1f, 1f })
        {
            var box = posts.AddComponent<BoxCollider>();
            box.center = new Vector3(side * (postInner + postOuter) * 0.5f, height * 0.5f, -PostDepth * 0.5f);
            box.size = new Vector3(postOuter - postInner, height, PostDepth);
        }
    }

    /// <summary>Writes <paramref name="mesh"/> to <paramref name="path"/>, in place when it exists (keeps its GUID).</summary>
    private static Mesh SaveMesh(Mesh mesh, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        existing.Clear();
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }
}
