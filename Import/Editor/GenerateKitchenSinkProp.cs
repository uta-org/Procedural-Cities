using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the kitchen sink module ("KitchenSink"): a base-cabinet counter
/// module (same materials/proportions as Kitchen4) whose countertop
/// has a rectangular basin sunk into it, taking almost the whole width and
/// depth of the module and 0.20 m deep, with a chrome faucet. Replaces the
/// bathroom pedestal "Sink" prefab that km_sink used to borrow.
/// Output: Models/LowPoly/KitchenSink_Combined.asset (4 submeshes) and
/// Resources/Prefabs/AssetContents/KitchenSink.prefab (root "KitchenSink",
/// child "kitchen_sink" with MeshFilter/MeshRenderer/convex MeshCollider,
/// same structure as Kitchen2.prefab).
/// Menu: Tools / Procedural Cities / Generate Kitchen Sink Prop
/// </summary>
public static class GenerateKitchenSinkProp
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string ModelDir = PkgRoot + "/Models/LowPoly";
    private const string MatDir = ModelDir + "/Materials";

    // Module size (metres). Front is +Z, like Kitchen2/4.
    private const float Width = 0.90f;
    private const float Depth = 0.60f;
    private const float CounterTopY = 0.90f;
    private const float CounterThickness = 0.05f;
    private const float BasinDepth = 0.20f;      // top of counter to basin floor
    private const float BasinFloor = 0.02f;      // thickness of the basin bottom
    private const float BasinWidth = 0.78f;      // opening, almost the full width
    private const float BasinLength = 0.50f;     // opening, almost the full depth
    private const float LinerWall = 0.012f;

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

    [MenuItem("Tools/Procedural Cities/Generate Kitchen Sink Prop")]
    public static void Generate()
    {
        var matCabinet = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Cabinet_White.mat");
        var matGranite = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Granite.mat");
        var matMetal = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Metal_Sink.mat");
        var matChrome = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Chrome.mat");
        if (matCabinet == null || matGranite == null || matMetal == null || matChrome == null)
        {
            Debug.LogError("[GenerateKitchenSinkProp] Missing LP_* materials.");
            return;
        }

        const int Cab = 0, Gran = 1, Metal = 2, Chrome = 3;
        var mb = new MeshBuilder(4);

        var hw = Width * 0.5f;
        var hd = Depth * 0.5f;
        var apronTop = CounterTopY - CounterThickness;          // 0.85
        var basinTop = CounterTopY;                              // 0.90
        var basinBottom = CounterTopY - BasinDepth;              // 0.70 (top face of the liner floor)
        var floorBottom = basinBottom - BasinFloor;              // 0.68
        var hx = BasinWidth * 0.5f;
        var hz = BasinLength * 0.5f;

        // Lower cabinet body: stops under the basin floor so nothing pokes into the basin.
        mb.Box(Cab, new Vector3(-hw, 0f, -hd), new Vector3(hw, floorBottom, hd));

        // Apron ring between cabinet body and countertop: same outline, hollow where the basin sits.
        Ring(mb, Cab, floorBottom, apronTop, hw, hd, hx, hz);

        // Countertop ring with a small overhang (granite), opening = basin opening.
        const float overhang = 0.02f;
        Ring(mb, Gran, apronTop, basinTop, hw + overhang, hd + overhang, hx, hz);

        // Basin liner (metal): floor + four walls, flush with the countertop opening.
        mb.Box(Metal, new Vector3(-hx, floorBottom, -hz), new Vector3(hx, basinBottom, hz));
        mb.Box(Metal, new Vector3(-hx, basinBottom, -hz), new Vector3(-hx + LinerWall, basinTop, hz));
        mb.Box(Metal, new Vector3(hx - LinerWall, basinBottom, -hz), new Vector3(hx, basinTop, hz));
        mb.Box(Metal, new Vector3(-hx + LinerWall, basinBottom, -hz), new Vector3(hx - LinerWall, basinTop, -hz + LinerWall));
        mb.Box(Metal, new Vector3(-hx + LinerWall, basinBottom, hz - LinerWall), new Vector3(hx - LinerWall, basinTop, hz));

        // Faucet on the back rim: base, neck, spout over the basin, two small taps.
        var rimBackZ = -(hz + hd + overhang) * 0.5f;
        mb.Box(Chrome, new Vector3(-0.04f, basinTop, rimBackZ - 0.03f), new Vector3(0.04f, basinTop + 0.03f, rimBackZ + 0.03f));
        mb.Box(Chrome, new Vector3(-0.012f, basinTop + 0.03f, rimBackZ - 0.012f), new Vector3(0.012f, basinTop + 0.24f, rimBackZ + 0.012f));
        mb.Box(Chrome, new Vector3(-0.012f, basinTop + 0.21f, rimBackZ - 0.012f), new Vector3(0.012f, basinTop + 0.24f, rimBackZ + 0.17f));
        foreach (var sx in new[] { -1f, 1f })
            mb.Box(Chrome, new Vector3(sx * 0.10f - 0.015f, basinTop, rimBackZ - 0.015f), new Vector3(sx * 0.10f + 0.015f, basinTop + 0.05f, rimBackZ + 0.015f));

        // Two door handles on the cabinet front.
        foreach (var sx in new[] { -1f, 1f })
            mb.Box(Chrome, new Vector3(sx * 0.12f - 0.01f, 0.48f, hd), new Vector3(sx * 0.12f + 0.01f, 0.58f, hd + 0.02f));

        var mesh = mb.ToMesh("KitchenSink_Combined");
        var meshPath = $"{ModelDir}/KitchenSink_Combined.asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);

        // Prefab: duplicate Kitchen2.prefab (same structure), then repoint mesh + materials.
        var prefabPath = $"{PrefabDir}/KitchenSink.prefab";
        AssetDatabase.DeleteAsset(prefabPath);
        if (!AssetDatabase.CopyAsset($"{PrefabDir}/Kitchen2.prefab", prefabPath))
        {
            Debug.LogError("[GenerateKitchenSinkProp] Could not duplicate Kitchen2.prefab.");
            return;
        }

        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            contents.name = "KitchenSink";
            var child = contents.transform.GetChild(0);
            child.name = "kitchen_sink";
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            child.GetComponent<MeshRenderer>().sharedMaterials = new[] { matCabinet, matGranite, matMetal, matChrome };
            var mc = child.GetComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateKitchenSinkProp] Saved {prefabPath} ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }

    /// <summary>Rectangular ring (4 boxes) from outer half-extents to the inner opening half-extents.</summary>
    private static void Ring(MeshBuilder mb, int sub, float y0, float y1, float ohx, float ohz, float ihx, float ihz)
    {
        mb.Box(sub, new Vector3(-ohx, y0, -ohz), new Vector3(-ihx, y1, ohz));   // left
        mb.Box(sub, new Vector3(ihx, y0, -ohz), new Vector3(ohx, y1, ohz));     // right
        mb.Box(sub, new Vector3(-ihx, y0, -ohz), new Vector3(ihx, y1, -ihz));   // back
        mb.Box(sub, new Vector3(-ihx, y0, ihz), new Vector3(ihx, y1, ohz));     // front
    }
}
