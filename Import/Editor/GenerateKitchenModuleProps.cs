using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the kitchen counter modules, one family with one footprint: every
/// floor module is 1.00 m wide, 0.65 m deep and 0.90 m to the worktop, with
/// flat sides and a worktop that overhangs the front only, so any of them
/// side by side read as one counter ("deberian tener todos los modulos la
/// misma anchura").
///  - Kitchen1: base cabinet, two doors.
///  - KitchenSink: the same cabinet with a 0.20 m deep basin and a faucet.
///  - KitchenOven: cooker, an oven under a four-burner hob.
///  - KitchenDishwasher: dishwasher under the worktop.
///  - KitchenHood: extractor hood, 1.00 m wide, hung over the cooker.
/// Output per module: Models/LowPoly/&lt;Name&gt;_Combined.asset (one submesh per
/// material) and Resources/Prefabs/AssetContents/&lt;Name&gt;.prefab (root
/// "&lt;Name&gt;", one child with MeshFilter/MeshRenderer/convex MeshCollider).
/// Existing assets are rewritten in place, so their GUIDs stay.
/// Menu: Tools / Procedural Cities / Generate Kitchen Module Props
/// </summary>
public static class GenerateKitchenModuleProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string ModelDir = PkgRoot + "/Models/LowPoly";
    private const string MatDir = ModelDir + "/Materials";

    // Shared module size (metres). Front is +Z, the back (-Z) stands against the wall.
    private const float Width = 1.00f;
    private const float Depth = 0.65f;
    private const float CounterTopY = 0.90f;
    private const float CounterThickness = 0.05f;
    private const float FrontOverhang = 0.02f;   // worktop over the body, front only
    private const float ToeKickHeight = 0.10f;
    private const float ToeKickRecess = 0.05f;
    private const float PanelRelief = 0.012f;

    private const float Hw = Width * 0.5f;
    private const float Back = -Depth * 0.5f;
    private const float Front = Depth * 0.5f;
    private const float BodyFront = Front - FrontOverhang;
    private const float BodyTop = CounterTopY - CounterThickness;

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _verts = new();
        private readonly List<Vector3> _norms = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<Material> _materials = new();
        private readonly List<List<int>> _tris = new();

        private List<int> Tris(Material material)
        {
            var index = _materials.IndexOf(material);
            if (index < 0)
            {
                _materials.Add(material);
                _tris.Add(new List<int>());
                index = _materials.Count - 1;
            }

            return _tris[index];
        }

        /// <summary>Axis-aligned box from min/max corners.</summary>
        public void Box(Material material, Vector3 min, Vector3 max)
        {
            var c = new[]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
            };
            var tris = Tris(material);
            Quad(tris, c[4], c[5], c[6], c[7], Vector3.forward);  // +Z
            Quad(tris, c[1], c[0], c[3], c[2], Vector3.back);     // -Z
            Quad(tris, c[5], c[1], c[2], c[6], Vector3.right);    // +X
            Quad(tris, c[0], c[4], c[7], c[3], Vector3.left);     // -X
            Quad(tris, c[7], c[6], c[2], c[3], Vector3.up);       // +Y
            Quad(tris, c[0], c[1], c[5], c[4], Vector3.down);     // -Y
        }

        /// <summary>Upright prism of <paramref name="segments"/> sides (a low-poly cylinder), with its top cap.</summary>
        public void Disc(Material material, float cx, float cz, float y0, float y1, float radius, int segments = 10)
        {
            var tris = Tris(material);
            var top = _verts.Count;
            _verts.Add(new Vector3(cx, y1, cz));
            _norms.Add(Vector3.up);
            _uvs.Add(new Vector2(0.5f, 0.5f));
            for (var i = 0; i < segments; i++)
            {
                var a = Mathf.PI * 2f * i / segments;
                _verts.Add(new Vector3(cx + Mathf.Cos(a) * radius, y1, cz + Mathf.Sin(a) * radius));
                _norms.Add(Vector3.up);
                _uvs.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (var i = 0; i < segments; i++)
                tris.AddRange(new[] { top, top + 1 + (i + 1) % segments, top + 1 + i });

            for (var i = 0; i < segments; i++)
            {
                var a0 = Mathf.PI * 2f * i / segments;
                var a1 = Mathf.PI * 2f * (i + 1) / segments;
                var p0 = new Vector3(cx + Mathf.Cos(a0) * radius, 0f, cz + Mathf.Sin(a0) * radius);
                var p1 = new Vector3(cx + Mathf.Cos(a1) * radius, 0f, cz + Mathf.Sin(a1) * radius);
                var am = (a0 + a1) * 0.5f;
                var normal = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                Quad(tris,
                    new Vector3(p1.x, y0, p1.z), new Vector3(p0.x, y0, p0.z),
                    new Vector3(p0.x, y1, p0.z), new Vector3(p1.x, y1, p1.z), normal);
            }
        }

        private void Quad(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            var i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c); _verts.Add(d);
            for (var k = 0; k < 4; k++) _norms.Add(n);
            _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(1, 0));
            _uvs.Add(new Vector2(1, 1)); _uvs.Add(new Vector2(0, 1));
            // a,b,c,d are listed so cross(b - a, c - a) points outward, which is
            // Unity's front-face (clockwise-from-outside) winding.
            tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        public Mesh ToMesh(string name, out Material[] materials)
        {
            var m = new Mesh { name = name, subMeshCount = _tris.Count };
            m.SetVertices(_verts);
            m.SetNormals(_norms);
            m.SetUVs(0, _uvs);
            for (var s = 0; s < _tris.Count; s++)
                m.SetTriangles(_tris[s], s);
            m.RecalculateBounds();
            materials = _materials.ToArray();
            return m;
        }
    }

    private sealed class Palette
    {
        public Material Cabinet, Granite, Chrome, SinkMetal, Steel, Black, Glass, Burner, Knob, Dark;

        public bool Complete =>
            Cabinet && Granite && Chrome && SinkMetal && Steel && Black && Glass && Burner && Knob && Dark;
    }

    private static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");

    [MenuItem("Tools/Procedural Cities/Generate Kitchen Module Props")]
    public static void Generate()
    {
        var p = new Palette
        {
            Cabinet = Load("LP_Cabinet_White"),
            Granite = Load("LP_Granite"),
            Chrome = Load("LP_Chrome"),
            SinkMetal = Load("LP_Metal_Sink"),
            Steel = Load("LP_Appliance_Silver"),
            Black = Load("LP_Plastic_Black"),
            Glass = Load("LP_Oven_Glass"),
            Burner = Load("KM_Burner"),
            Knob = Load("LP_Oven_Knob"),
            Dark = Load("LP_Metal_DarkGray"),
        };
        if (!p.Complete)
        {
            Debug.LogError("[GenerateKitchenModuleProps] Missing LP_* / KM_* materials.");
            return;
        }

        Save("Kitchen1", "kitchen1", BuildCabinet(p));
        Save("KitchenSink", "kitchen_sink", BuildSink(p));
        Save("KitchenOven", "kitchen_oven", BuildOven(p));
        Save("KitchenDishwasher", "kitchen_dishwasher", BuildDishwasher(p));
        Save("KitchenHood", "kitchen_hood", BuildHood(p));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>Toe kick and body up to <paramref name="bodyTop"/>, full width.</summary>
    private static void Carcass(MeshBuilder mb, Palette p, Material body, float bodyTop)
    {
        mb.Box(p.Dark, new Vector3(-Hw, 0f, Back), new Vector3(Hw, ToeKickHeight, BodyFront - ToeKickRecess));
        mb.Box(body, new Vector3(-Hw, ToeKickHeight, Back), new Vector3(Hw, bodyTop, BodyFront));
    }

    private static void Worktop(MeshBuilder mb, Material material)
    {
        mb.Box(material, new Vector3(-Hw, BodyTop, Back), new Vector3(Hw, CounterTopY, Front));
    }

    /// <summary>Two door panels between <paramref name="y0"/> and <paramref name="y1"/>, each with its handle.</summary>
    private static void Doors(MeshBuilder mb, Palette p, float y0, float y1)
    {
        const float sideMargin = 0.02f;
        const float middleGap = 0.01f;
        foreach (var side in new[] { -1f, 1f })
        {
            var x0 = side < 0f ? -Hw + sideMargin : middleGap;
            var x1 = side < 0f ? -middleGap : Hw - sideMargin;
            mb.Box(p.Cabinet, new Vector3(x0, y0, BodyFront), new Vector3(x1, y1, BodyFront + PanelRelief));

            var handleX = side * 0.07f;
            var handleY = (y0 + y1) * 0.5f;
            mb.Box(p.Chrome,
                new Vector3(handleX - 0.01f, handleY - 0.06f, BodyFront + PanelRelief),
                new Vector3(handleX + 0.01f, handleY + 0.06f, BodyFront + PanelRelief + 0.02f));
        }
    }

    private static MeshBuilder BuildCabinet(Palette p)
    {
        var mb = new MeshBuilder();
        Carcass(mb, p, p.Cabinet, BodyTop);
        Worktop(mb, p.Granite);
        Doors(mb, p, ToeKickHeight + 0.03f, BodyTop - 0.03f);
        return mb;
    }

    private static MeshBuilder BuildSink(Palette p)
    {
        const float basinDepth = 0.20f;      // top of the worktop to the basin floor
        const float basinFloor = 0.02f;
        const float basinHalfWidth = 0.40f;
        const float basinHalfLength = 0.19f;
        const float basinCentreZ = 0.07f;    // towards the front, leaving the back rim for the faucet
        const float linerWall = 0.012f;
        // The faucet stands this far in front of the module's back. A counter
        // against a facade wall has its back 0.095 m inside the wall (the
        // inner face of a facade is 0.25 m from the room edge, the counter is
        // placed for the 0.125 m of a partition) and a closed window's sash
        // sits on that face: a faucet nearer the back went into the wall or
        // behind the window ("Se va un poco por detrás de la ventana").
        const float faucetBackClearance = 0.13f;

        var basinBottom = CounterTopY - basinDepth;
        var floorBottom = basinBottom - basinFloor;
        var z0 = basinCentreZ - basinHalfLength;
        var z1 = basinCentreZ + basinHalfLength;

        var mb = new MeshBuilder();
        // Body under the basin, then a ring around it up to the worktop.
        Carcass(mb, p, p.Cabinet, floorBottom);
        Ring(mb, p.Cabinet, floorBottom, BodyTop, BodyFront, basinHalfWidth, z0, z1);
        Ring(mb, p.Granite, BodyTop, CounterTopY, Front, basinHalfWidth, z0, z1);
        Doors(mb, p, ToeKickHeight + 0.03f, floorBottom - 0.03f);

        // Basin liner: floor and four walls, flush with the worktop opening.
        mb.Box(p.SinkMetal, new Vector3(-basinHalfWidth, floorBottom, z0), new Vector3(basinHalfWidth, basinBottom, z1));
        mb.Box(p.SinkMetal, new Vector3(-basinHalfWidth, basinBottom, z0), new Vector3(-basinHalfWidth + linerWall, CounterTopY, z1));
        mb.Box(p.SinkMetal, new Vector3(basinHalfWidth - linerWall, basinBottom, z0), new Vector3(basinHalfWidth, CounterTopY, z1));
        mb.Box(p.SinkMetal, new Vector3(-basinHalfWidth + linerWall, basinBottom, z0), new Vector3(basinHalfWidth - linerWall, CounterTopY, z0 + linerWall));
        mb.Box(p.SinkMetal, new Vector3(-basinHalfWidth + linerWall, basinBottom, z1 - linerWall), new Vector3(basinHalfWidth - linerWall, CounterTopY, z1));

        // Faucet on the back rim: base, neck, spout over the basin, two taps.
        var faucetBack = Back + faucetBackClearance;
        var rimZ = faucetBack + 0.03f;
        mb.Box(p.Chrome, new Vector3(-0.04f, CounterTopY, faucetBack), new Vector3(0.04f, CounterTopY + 0.03f, rimZ + 0.03f));
        mb.Box(p.Chrome, new Vector3(-0.012f, CounterTopY + 0.03f, rimZ - 0.012f), new Vector3(0.012f, CounterTopY + 0.24f, rimZ + 0.012f));
        mb.Box(p.Chrome, new Vector3(-0.012f, CounterTopY + 0.21f, rimZ - 0.012f), new Vector3(0.012f, CounterTopY + 0.24f, rimZ + 0.19f));
        foreach (var sx in new[] { -1f, 1f })
            mb.Box(p.Chrome, new Vector3(sx * 0.10f - 0.015f, CounterTopY, rimZ - 0.015f), new Vector3(sx * 0.10f + 0.015f, CounterTopY + 0.05f, rimZ + 0.015f));
        return mb;
    }

    private static MeshBuilder BuildOven(Palette p)
    {
        var mb = new MeshBuilder();
        Carcass(mb, p, p.Steel, BodyTop);

        // Hob: a black glass top in place of the worktop, four burners.
        Worktop(mb, p.Black);
        foreach (var sx in new[] { -1f, 1f })
        {
            mb.Disc(p.Burner, sx * 0.26f, -0.14f, CounterTopY, CounterTopY + 0.008f, 0.10f);
            mb.Disc(p.Burner, sx * 0.26f, 0.13f, CounterTopY, CounterTopY + 0.008f, 0.08f);
        }

        // Control strip with five knobs.
        const float stripBottom = 0.74f;
        mb.Box(p.Black, new Vector3(-Hw + 0.02f, stripBottom, BodyFront), new Vector3(Hw - 0.02f, BodyTop - 0.01f, BodyFront + PanelRelief));
        for (var i = 0; i < 5; i++)
        {
            var x = -0.36f + i * 0.18f;
            mb.Box(p.Knob,
                new Vector3(x - 0.022f, 0.773f, BodyFront + PanelRelief),
                new Vector3(x + 0.022f, 0.817f, BodyFront + PanelRelief + 0.02f));
        }

        // Oven door with its window and a bar handle.
        mb.Box(p.Dark, new Vector3(-Hw + 0.04f, 0.16f, BodyFront), new Vector3(Hw - 0.04f, 0.71f, BodyFront + PanelRelief));
        mb.Box(p.Glass, new Vector3(-0.36f, 0.26f, BodyFront + PanelRelief), new Vector3(0.36f, 0.58f, BodyFront + PanelRelief + 0.004f));
        mb.Box(p.Chrome, new Vector3(-0.40f, 0.64f, BodyFront + PanelRelief + 0.02f), new Vector3(0.40f, 0.67f, BodyFront + PanelRelief + 0.04f));
        foreach (var sx in new[] { -1f, 1f })
            mb.Box(p.Chrome, new Vector3(sx * 0.38f - 0.012f, 0.64f, BodyFront + PanelRelief), new Vector3(sx * 0.38f + 0.012f, 0.67f, BodyFront + PanelRelief + 0.02f));
        return mb;
    }

    private static MeshBuilder BuildDishwasher(Palette p)
    {
        var mb = new MeshBuilder();
        Carcass(mb, p, p.Cabinet, BodyTop);
        Worktop(mb, p.Granite);

        // Control strip with three buttons, then the steel door and its handle.
        const float stripBottom = 0.75f;
        mb.Box(p.Black, new Vector3(-Hw + 0.02f, stripBottom, BodyFront), new Vector3(Hw - 0.02f, BodyTop - 0.01f, BodyFront + PanelRelief));
        for (var i = 0; i < 3; i++)
        {
            var x = 0.22f + i * 0.08f;
            mb.Box(p.Chrome,
                new Vector3(x - 0.02f, 0.785f, BodyFront + PanelRelief),
                new Vector3(x + 0.02f, 0.805f, BodyFront + PanelRelief + 0.006f));
        }

        mb.Box(p.Steel, new Vector3(-Hw + 0.02f, ToeKickHeight + 0.02f, BodyFront), new Vector3(Hw - 0.02f, stripBottom - 0.01f, BodyFront + PanelRelief));
        mb.Box(p.Chrome, new Vector3(-0.38f, 0.66f, BodyFront + PanelRelief + 0.02f), new Vector3(0.38f, 0.69f, BodyFront + PanelRelief + 0.04f));
        foreach (var sx in new[] { -1f, 1f })
            mb.Box(p.Chrome, new Vector3(sx * 0.36f - 0.012f, 0.66f, BodyFront + PanelRelief), new Vector3(sx * 0.36f + 0.012f, 0.69f, BodyFront + PanelRelief + 0.02f));
        return mb;
    }

    /// <summary>
    /// Extractor hood: a canopy as wide as a module and a chimney against the
    /// wall. Its pivot is the underside of the canopy, its back at Z = -0.195.
    /// </summary>
    private static MeshBuilder BuildHood(Palette p)
    {
        const float halfDepth = 0.195f;
        const float canopyHeight = 0.08f;
        const float height = 0.54f;

        var mb = new MeshBuilder();
        mb.Box(p.Steel, new Vector3(-Hw, 0f, -halfDepth), new Vector3(Hw, canopyHeight, halfDepth));
        mb.Box(p.Dark, new Vector3(-Hw + 0.06f, -0.006f, -halfDepth + 0.05f), new Vector3(Hw - 0.06f, 0f, halfDepth - 0.04f));
        mb.Box(p.Steel, new Vector3(-0.17f, canopyHeight, -halfDepth), new Vector3(0.17f, height, 0.06f));
        return mb;
    }

    /// <summary>
    /// Rectangular ring (4 boxes) between y0 and y1: full module width, from the
    /// back to <paramref name="outerFront"/>, around the basin opening.
    /// </summary>
    private static void Ring(
        MeshBuilder mb, Material material, float y0, float y1, float outerFront,
        float innerHalfWidth, float innerZ0, float innerZ1)
    {
        mb.Box(material, new Vector3(-Hw, y0, Back), new Vector3(-innerHalfWidth, y1, outerFront));          // left
        mb.Box(material, new Vector3(innerHalfWidth, y0, Back), new Vector3(Hw, y1, outerFront));            // right
        mb.Box(material, new Vector3(-innerHalfWidth, y0, Back), new Vector3(innerHalfWidth, y1, innerZ0));  // back
        mb.Box(material, new Vector3(-innerHalfWidth, y0, innerZ1), new Vector3(innerHalfWidth, y1, outerFront)); // front
    }

    private static void Save(string prefabName, string childName, MeshBuilder builder)
    {
        var mesh = builder.ToMesh($"{prefabName}_Combined", out var materials);
        var meshPath = $"{ModelDir}/{prefabName}_Combined.asset";
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

        var prefabPath = $"{PrefabDir}/{prefabName}.prefab";
        var root = new GameObject(prefabName);
        try
        {
            var child = new GameObject(childName);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var collider = child.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            // Saving over an existing prefab keeps its GUID.
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log($"[GenerateKitchenModuleProps] Saved {prefabPath} ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }
}
