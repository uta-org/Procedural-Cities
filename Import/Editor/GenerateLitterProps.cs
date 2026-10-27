using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the litter props (P23, owner 2026-10-27: "add a bit of pollution: newspapers on the ground,
/// crushed cans, cardboard boxes, cups... think of 10 other props"; the owner chose six small ones and
/// four bigger pieces of junk): newspaper, crushed can, cardboard box, cup, plastic bottle, glass
/// bottle, paper ball, plastic bag, drink carton, cigarette butts, old tyre, traffic cone, broken
/// chair and a mattress. Low-poly and flat-shaded like the park and suburb props, but textured: every
/// prop maps into one atlas (Textures/Litter/LitterAtlas.png, built by build_litter_atlas.py from
/// ambientCG CC0 textures and public-domain newspaper pages, see CREDITS.txt) and shares one material,
/// so the game draws a whole region of litter as one mesh. A prop's colour variants sit side by side
/// in the atlas: the game shifts U by the prop's variant step (the runtime LitterCatalog mirrors the
/// names, variants and steps below). Pivot on the ground at the prop's centre; no shadows; the four
/// big ones get a box collider.
/// Menu: Tools / Procedural Cities / Generate Litter Props
/// </summary>
public static class GenerateLitterProps
{
    private const string MeshDir = GenerateParkProps.PkgRoot + "/Models/LowPoly/Litter";
    private const string AtlasPath = GenerateParkProps.PkgRoot + "/Textures/Litter/LitterAtlas.png";
    private const string MaterialPath = GenerateParkProps.PkgRoot + "/Models/LowPoly/Materials/LP_Litter.mat";

    /// <summary>Atlas size (px) and cell size (px): build_litter_atlas.py's layout.</summary>
    private const float AtlasSize = 1024f;
    private const int CellPx = 128;

    /// <summary>Pixels kept clear inside a cell's edge, so mipmaps do not bleed the neighbour in.</summary>
    private const float InsetPx = 3f;

    [MenuItem("Tools/Procedural Cities/Generate Litter Props")]
    public static void Generate()
    {
        GenerateParkProps.EnsureFolder(GenerateParkProps.PkgRoot + "/Models/LowPoly", "Materials");
        GenerateParkProps.EnsureFolder(GenerateParkProps.PkgRoot + "/Models/LowPoly", "Litter");
        var material = GetAtlasMaterial();
        var built = new List<string>();
        void Save(string name, Builder b, Vector3? collider = null)
        {
            b.SnapToGround();
            var mesh = b.Build(name);
            var meshPath = $"{MeshDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                mesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            var root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (collider.HasValue)
            {
                var box = root.AddComponent<BoxCollider>();
                box.size = collider.Value;
                box.center = new Vector3(mesh.bounds.center.x, collider.Value.y * 0.5f, mesh.bounds.center.z);
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"{GenerateParkProps.PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            built.Add(name);
        }

        Save("LitterNewspaper", Newspaper());
        Save("LitterCan", Can());
        Save("LitterCardboardBox", CardboardBox());
        Save("LitterCup", Cup());
        Save("LitterPlasticBottle", PlasticBottle());
        Save("LitterGlassBottle", GlassBottle());
        Save("LitterPaperBall", PaperBall());
        Save("LitterPlasticBag", PlasticBag());
        Save("LitterDrinkCarton", DrinkCarton());
        Save("LitterCigaretteButts", CigaretteButts());
        Save("LitterTyre", Tyre(), new Vector3(0.64f, 0.2f, 0.64f));
        Save("LitterTrafficCone", TrafficCone(), new Vector3(0.36f, 0.5f, 0.36f));
        Save("LitterBrokenChair", BrokenChair(), new Vector3(0.46f, 0.86f, 0.46f));
        Save("LitterMattress", Mattress(), new Vector3(0.9f, 0.18f, 1.9f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateLitterProps] Saved {built.Count} litter prefabs under {GenerateParkProps.PrefabDir}: {string.Join(", ", built)}");
    }

    // ───────────────────────────── The props ─────────────────────────────

    /// <summary>A tabloid folded down the middle, one half lying flat, the other lifting a little; both sides printed.</summary>
    private static Builder Newspaper()
    {
        var b = new Builder();
        var uv = Cell(0, 0, 2, 2);
        const int nx = 5, nz = 4;
        const float w = 0.30f, d = 0.42f;
        var p = new Vector3[nx, nz];
        var t = new Vector2[nx, nz];
        var lift = Mathf.Tan(16f * Mathf.Deg2Rad);
        for (var i = 0; i < nx; i++)
        {
            for (var k = 0; k < nz; k++)
            {
                var x = -w * 0.5f + w * i / (nx - 1);
                var z = -d * 0.5f + d * k / (nz - 1);
                var y = 0.004f + Mathf.Max(0f, x) * lift + Noise(i, k, 1) * 0.006f;
                p[i, k] = new Vector3(x, y, z);
                t[i, k] = At(uv, (float)i / (nx - 1), (float)k / (nz - 1));
            }
        }

        for (var i = 0; i < nx - 1; i++)
        {
            for (var k = 0; k < nz - 1; k++)
                b.TwoSidedQuad(p[i, k], p[i + 1, k], p[i + 1, k + 1], p[i, k + 1], t[i, k], t[i + 1, k], t[i + 1, k + 1], t[i, k + 1]);
        }

        return b;
    }

    /// <summary>A drinks can on its side, crushed in the middle: the label round it, metal at the rims and ends.</summary>
    private static Builder Can()
    {
        var b = new Builder();
        var label = Sub(4, 0, 0, 96);
        var metal = Sub(4, 0, 96, 128);
        var rings = new[]
        {
            new Ring(-0.062f, 0.029f, 0.029f, 0f, 0f, metal),
            new Ring(-0.057f, 0.033f, 0.033f, 0f, 1f, metal),
            new Ring(-0.057f, 0.033f, 0.033f, 0f, 1f, label),
            new Ring(0.004f, 0.016f, 0.046f, -0.012f, 0.45f, label),
            new Ring(0.052f, 0.033f, 0.033f, 0f, 0f, label),
            new Ring(0.052f, 0.033f, 0.033f, 0f, 0f, metal),
            new Ring(0.058f, 0.029f, 0.029f, 0f, 1f, metal),
        };
        b.Lathe(rings, 8, metal, metal);
        return b;
    }

    /// <summary>An open cardboard box: four walls, a bottom and three flaps leaning out (one torn off).</summary>
    private static Builder CardboardBox()
    {
        var b = new Builder();
        var uv = Cell(5, 1);
        const float w = 0.40f, d = 0.30f, h = 0.26f;
        var c = new Vector3[8];
        for (var i = 0; i < 8; i++)
            c[i] = new Vector3((i & 1) == 0 ? -w / 2 : w / 2, (i & 2) == 0 ? 0f : h, (i & 4) == 0 ? -d / 2 : d / 2);
        b.TwoSidedQuad(c[0], c[1], c[5], c[4], uv); // bottom
        b.TwoSidedQuad(c[0], c[1], c[3], c[2], uv); // back
        b.TwoSidedQuad(c[4], c[5], c[7], c[6], uv); // front
        b.TwoSidedQuad(c[0], c[4], c[6], c[2], uv); // left
        b.TwoSidedQuad(c[1], c[5], c[7], c[3], uv); // right
        // Flaps: hinged on the top edges, leaning out at different angles; the back one is missing.
        void Flap(Vector3 a, Vector3 e, Vector3 outward, float depth, float degreesFromUp)
        {
            var dir = Quaternion.AngleAxis(degreesFromUp, Vector3.Cross(Vector3.up, outward)) * Vector3.up;
            b.TwoSidedQuad(a, e, e + dir * depth, a + dir * depth, uv);
        }
        Flap(c[6], c[7], Vector3.forward, 0.15f, 35f);
        Flap(c[2], c[6], Vector3.left, 0.14f, 70f);
        Flap(c[3], c[7], Vector3.right, 0.14f, 20f);
        return b;
    }

    /// <summary>A paper cup on its side: the printed outside, the plain inside, a closed bottom.</summary>
    private static Builder Cup()
    {
        var b = new Builder();
        var body = Sub(0, 2, 0, 100);
        var inside = Sub(0, 2, 100, 128);
        var outer = new[]
        {
            new Ring(-0.05f, 0.026f, 0.026f, 0f, 0f, body),
            new Ring(0.05f, 0.04f, 0.04f, 0f, 1f, body),
        };
        b.Lathe(outer, 8, inside, null);
        var inner = new[]
        {
            new Ring(-0.047f, 0.024f, 0.024f, 0f, 0f, inside),
            new Ring(0.05f, 0.038f, 0.038f, 0f, 1f, inside),
        };
        b.Lathe(inner, 8, inside, null, inward: true);
        return b;
    }

    /// <summary>A half-litre plastic bottle on its side, dented: the body with its label band, the cap.</summary>
    private static Builder PlasticBottle()
    {
        var b = new Builder();
        var body = Sub(3, 2, 16, 128);
        var cap = Sub(3, 2, 0, 14);
        var rings = new[]
        {
            new Ring(-0.105f, 0.030f, 0.030f, 0f, 0f, body),
            new Ring(-0.098f, 0.034f, 0.034f, 0f, 0.05f, body),
            new Ring(-0.02f, 0.027f, 0.036f, -0.004f, 0.45f, body),
            new Ring(0.05f, 0.034f, 0.034f, 0f, 0.8f, body),
            new Ring(0.085f, 0.015f, 0.015f, 0f, 0.92f, body),
            new Ring(0.095f, 0.014f, 0.014f, 0f, 1f, body),
            new Ring(0.095f, 0.016f, 0.016f, 0f, 0f, cap),
            new Ring(0.112f, 0.016f, 0.016f, 0f, 1f, cap),
        };
        b.Lathe(rings, 8, body, cap);
        return b;
    }

    /// <summary>A glass bottle on its side: the body with its paper label, the shoulder, a foiled neck.</summary>
    private static Builder GlassBottle()
    {
        var b = new Builder();
        var body = Sub(6, 2, 12, 128);
        var neck = Sub(6, 2, 0, 12);
        var rings = new[]
        {
            new Ring(-0.12f, 0.034f, 0.034f, 0f, 0f, body),
            new Ring(-0.112f, 0.037f, 0.037f, 0f, 0.05f, body),
            new Ring(0.05f, 0.037f, 0.037f, 0f, 0.75f, body),
            new Ring(0.10f, 0.015f, 0.015f, 0f, 0.95f, body),
            new Ring(0.14f, 0.014f, 0.014f, 0f, 1f, body),
            new Ring(0.14f, 0.014f, 0.014f, 0f, 0f, neck),
            new Ring(0.17f, 0.016f, 0.016f, 0f, 1f, neck),
        };
        b.Lathe(rings, 8, body, neck);
        return b;
    }

    /// <summary>A crumpled ball of paper: a lumpy low sphere.</summary>
    private static Builder PaperBall()
    {
        var b = new Builder();
        var uv = Cell(3, 1);
        const int rings = 4, segments = 6;
        const float r = 0.045f;
        Vector3 P(int ring, int s)
        {
            var theta = Mathf.PI * ring / rings;
            var phi = Mathf.PI * 2f * (s % segments) / segments;
            var bump = ring == 0 || ring == rings ? 1f : 1f + Noise(ring, s % segments, 7) * 0.28f;
            return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta) * 0.85f, Mathf.Sin(theta) * Mathf.Sin(phi)) * (r * bump);
        }
        Vector2 T(Vector3 v) => At(uv, 0.5f + v.x / (4f * r), 0.5f + (v.z + v.y * 0.5f) / (4f * r));
        for (var ring = 0; ring < rings; ring++)
        {
            for (var s = 0; s < segments; s++)
            {
                var a = P(ring, s);
                var c = P(ring + 1, s + 1);
                var e = P(ring + 1, s);
                var f = P(ring, s + 1);
                if (ring > 0)
                    b.Tri(a, f, e, T(a), T(f), T(e), (a + f + e) / 3f);
                if (ring < rings - 1)
                    b.Tri(f, c, e, T(f), T(c), T(e), (f + c + e) / 3f);
            }
        }

        return b;
    }

    /// <summary>A plastic bag lying deflated, a pocket of air in it, its two handles at one end.</summary>
    private static Builder PlasticBag()
    {
        var b = new Builder();
        var uv = Cell(0, 3);
        const int nx = 5, nz = 4;
        const float w = 0.34f, d = 0.26f;
        var p = new Vector3[nx, nz];
        var t = new Vector2[nx, nz];
        for (var i = 0; i < nx; i++)
        {
            for (var k = 0; k < nz; k++)
            {
                var x = -w * 0.5f + w * i / (nx - 1);
                var z = -d * 0.5f + d * k / (nz - 1);
                var edge = i == 0 || k == 0 || i == nx - 1 || k == nz - 1;
                var pocket = Mathf.Max(0f, 1f - ((x + 0.03f) * (x + 0.03f) + z * z) / (0.13f * 0.13f));
                var y = edge ? 0.004f + Noise(i, k, 3) * 0.006f : 0.01f + pocket * 0.06f + Noise(i, k, 4) * 0.012f;
                p[i, k] = new Vector3(x, y, z);
                t[i, k] = At(uv, (float)i / (nx - 1), (float)k / (nz - 1));
            }
        }

        for (var i = 0; i < nx - 1; i++)
        {
            for (var k = 0; k < nz - 1; k++)
                b.TwoSidedQuad(p[i, k], p[i + 1, k], p[i + 1, k + 1], p[i, k + 1], t[i, k], t[i + 1, k], t[i + 1, k + 1], t[i, k + 1]);
        }

        // Handles: two thin arches beyond the bag's end.
        foreach (var z0 in new[] { -0.07f, 0.05f })
        {
            var prev = new Vector3(w * 0.5f, 0.005f, z0);
            for (var s = 1; s <= 4; s++)
            {
                var a = Mathf.PI * s / 4f;
                var next = new Vector3(w * 0.5f + Mathf.Sin(a) * 0.06f, 0.005f + Mathf.Sin(a) * 0.03f, z0 + (1f - Mathf.Cos(a)) * 0.0f);
                b.TwoSidedQuad(prev, next, next + new Vector3(0f, 0f, 0.02f), prev + new Vector3(0f, 0f, 0.02f), Cell(0, 3));
                prev = next;
            }
        }

        return b;
    }

    /// <summary>A drinks carton lying on its face, its gable end and a straw sticking out.</summary>
    private static Builder DrinkCarton()
    {
        var b = new Builder();
        var face = Sub(3, 3, 22, 104);
        var band = Sub(3, 3, 0, 22);
        const float l = 0.12f, h = 0.04f, d = 0.065f;
        var o = new Vector3(0f, h / 2, 0f);
        b.Box(o, new Vector3(l, h, d), Quaternion.identity, face, face, Cell(3, 3), band);
        // The gable: a low ridge at the +X end.
        var e0 = new Vector3(l / 2, 0f, -d / 2);
        var e1 = new Vector3(l / 2, 0f, d / 2);
        var e2 = new Vector3(l / 2, h, d / 2);
        var e3 = new Vector3(l / 2, h, -d / 2);
        var r0 = new Vector3(l / 2 + 0.022f, h / 2, -d / 2);
        var r1 = new Vector3(l / 2 + 0.022f, h / 2, d / 2);
        b.Quad(e0, e1, r1, r0, band, new Vector3(1f, -1f, 0f));
        b.Quad(e3, e2, r1, r0, band, new Vector3(1f, 1f, 0f));
        b.Tri(e0, e3, r0, At(band, 0f, 0f), At(band, 1f, 0f), At(band, 0.5f, 1f), new Vector3(0f, 0f, -1f));
        b.Tri(e1, e2, r1, At(band, 0f, 0f), At(band, 1f, 0f), At(band, 0.5f, 1f), new Vector3(0f, 0f, 1f));
        // The straw.
        b.Box(new Vector3(l / 2 - 0.01f, h + 0.012f, 0.015f), new Vector3(0.08f, 0.006f, 0.006f), Quaternion.Euler(0f, 20f, 18f), band);
        return b;
    }

    /// <summary>Four cigarette butts dropped together: cork filter, a stub of paper, the ash end.</summary>
    private static Builder CigaretteButts()
    {
        var b = new Builder();
        var filter = Sub(6, 3, 0, 62);
        var paper = Sub(6, 3, 66, 128);
        var spots = new[] { (0f, 0f, 0f), (0.05f, 0.03f, 70f), (-0.04f, 0.05f, 140f), (0.02f, -0.06f, 200f) };
        foreach (var (x, z, yaw) in spots)
        {
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var centre = new Vector3(x, 0.0045f, z);
            var rings = new[]
            {
                new Ring(-0.015f, 0.0045f, 0.0045f, 0f, 1f, filter),
                new Ring(0f, 0.0045f, 0.0045f, 0f, 0f, filter),
                new Ring(0f, 0.0042f, 0.0042f, 0f, 1f, paper),
                new Ring(0.013f, 0.0042f, 0.0042f, 0f, 0f, paper),
            };
            b.Lathe(rings, 4, filter, Sub(6, 3, 110, 128), transform: Matrix4x4.TRS(centre, rotation, Vector3.one));
        }

        return b;
    }

    /// <summary>An old tyre lying flat: the tread round the outside, the sidewalls top and bottom.</summary>
    private static Builder Tyre()
    {
        var b = new Builder();
        var tread = Sub(0, 4, 0, 128);
        var wall = Px(128 + 8, 4 * CellPx, 256, 5 * CellPx);
        const int n = 12;
        const float inner = 0.19f, outer = 0.31f, bulge = 0.325f, height = 0.2f;
        Vector3 R(float r, float y, int i)
        {
            var a = Mathf.PI * 2f * (i % n) / n;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }
        Vector2 W(Vector3 v) => At(wall, 0.5f + v.x / (2f * bulge), 0.5f + v.z / (2f * bulge));
        for (var i = 0; i < n; i++)
        {
            var j = i + 1;
            float v0 = (float)i / n, v1 = (float)j / n;
            // Tread: two bands bulging out at mid height.
            b.Quad(R(outer, 0f, i), R(bulge, height / 2, i), R(bulge, height / 2, j), R(outer, 0f, j),
                At(tread, 0f, v0), At(tread, 0.5f, v0), At(tread, 0.5f, v1), At(tread, 0f, v1), R(1f, height / 4, i) + R(1f, 0f, j));
            b.Quad(R(bulge, height / 2, i), R(outer, height, i), R(outer, height, j), R(bulge, height / 2, j),
                At(tread, 0.5f, v0), At(tread, 1f, v0), At(tread, 1f, v1), At(tread, 0.5f, v1), R(1f, height, i) + R(1f, height, j));
            // Sidewalls, top and bottom, and the inside.
            b.Quad(R(inner, height, i), R(outer, height, i), R(outer, height, j), R(inner, height, j),
                W(R(inner, height, i)), W(R(outer, height, i)), W(R(outer, height, j)), W(R(inner, height, j)), Vector3.up);
            b.Quad(R(inner, 0f, i), R(outer, 0f, i), R(outer, 0f, j), R(inner, 0f, j),
                W(R(inner, 0f, i)), W(R(outer, 0f, i)), W(R(outer, 0f, j)), W(R(inner, 0f, j)), Vector3.down);
            var mid = (R(inner, 0f, i) + R(inner, 0f, j)) * 0.5f;
            b.Quad(R(inner, 0f, i), R(inner, height, i), R(inner, height, j), R(inner, 0f, j),
                W(R(inner, 0f, i)), W(R(inner * 0.8f, 0f, i)), W(R(inner * 0.8f, 0f, j)), W(R(inner, 0f, j)), -new Vector3(mid.x, 0f, mid.z));
        }

        return b;
    }

    /// <summary>A road cone: a square black base and an orange cone with two white bands.</summary>
    private static Builder TrafficCone()
    {
        var b = new Builder();
        var cone = Sub(7, 3, 0, 108);
        var foot = Sub(7, 3, 112, 128);
        b.Box(new Vector3(0f, 0.015f, 0f), new Vector3(0.36f, 0.03f, 0.36f), Quaternion.identity, foot);
        var rings = new[]
        {
            new Ring(0.03f, 0.135f, 0.135f, 0f, 0f, cone),
            new Ring(0.50f, 0.026f, 0.026f, 0f, 1f, cone),
        };
        b.Lathe(rings, 8, null, Sub(7, 3, 0, 12), transform: Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one));
        return b;
    }

    /// <summary>A wooden chair, broken: one front leg snapped short, the back's lower rail hanging off one post.</summary>
    private static Builder BrokenChair()
    {
        var b = new Builder();
        var wood = Cell(3, 4);
        const float s = 0.42f, seat = 0.44f, back = 0.86f, leg = 0.035f;
        var half = s / 2 - leg / 2;
        b.Box(new Vector3(0f, seat, 0f), new Vector3(s, 0.035f, s), Quaternion.identity, wood);
        b.Box(new Vector3(-half, seat / 2, half), new Vector3(leg, seat, leg), Quaternion.identity, wood);
        b.Box(new Vector3(half, seat - 0.11f, half), new Vector3(leg, 0.22f, leg), Quaternion.identity, wood); // snapped
        b.Box(new Vector3(half + 0.05f, 0.02f, half - 0.05f), new Vector3(leg, 0.2f, leg), Quaternion.Euler(0f, 30f, 90f), wood); // its lost piece
        b.Box(new Vector3(-half, back / 2, -half), new Vector3(leg, back, leg), Quaternion.identity, wood);
        b.Box(new Vector3(half, back / 2, -half), new Vector3(leg, back, leg), Quaternion.identity, wood);
        b.Box(new Vector3(0f, back - 0.06f, -half), new Vector3(s, 0.06f, 0.022f), Quaternion.identity, wood);
        b.Box(new Vector3(-0.05f, 0.6f, -half), new Vector3(s * 0.9f, 0.05f, 0.022f), Quaternion.Euler(0f, 0f, -24f), wood);
        return b;
    }

    /// <summary>A single mattress: the quilted cover on its faces, the ticking round its sides.</summary>
    private static Builder Mattress()
    {
        var b = new Builder();
        var cover = Px(0, 5 * CellPx, 512, 5 * CellPx + 192);
        var ticking = Px(0, 5 * CellPx + 192, 512, 7 * CellPx);
        // Cover faces with U along the mattress' length (Z).
        b.Box(new Vector3(0f, 0.09f, 0f), new Vector3(1.9f, 0.18f, 0.9f), Quaternion.Euler(0f, 90f, 0f), cover, cover, ticking, ticking);
        return b;
    }

    // ───────────────────────────── Atlas and material ─────────────────────────────

    /// <summary>The shared litter material, its atlas imported for it first.</summary>
    private static Material GetAtlasMaterial()
    {
        if (AssetImporter.GetAtPath(AtlasPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 2;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "LP_Litter", enableInstancing = true };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", atlas);
        material.mainTexture = atlas;
        material.color = Color.white;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>The UV rectangle of atlas pixels x0..x1, y0..y1 (y from the top), inset against bleeding.</summary>
    private static Rect Px(float x0, float y0, float x1, float y1) =>
        new((x0 + InsetPx) / AtlasSize, 1f - (y1 - InsetPx) / AtlasSize, (x1 - x0 - 2f * InsetPx) / AtlasSize, (y1 - y0 - 2f * InsetPx) / AtlasSize);

    /// <summary>The UV rectangle of cells (col, row) .. (col + w - 1, row + h - 1).</summary>
    private static Rect Cell(int col, int row, int w = 1, int h = 1) =>
        Px(col * CellPx, row * CellPx, (col + w) * CellPx, (row + h) * CellPx);

    /// <summary>The UV rectangle of rows y0..y1 (px from the cell's top) of cell (col, row).</summary>
    private static Rect Sub(int col, int row, float y0, float y1) =>
        Px(col * CellPx, row * CellPx + y0, (col + 1) * CellPx, row * CellPx + y1);

    /// <summary>The point (u, v) of <paramref name="r"/>, v = 1 its top.</summary>
    private static Vector2 At(Rect r, float u, float v) => new(r.xMin + r.width * u, r.yMin + r.height * v);

    /// <summary>A repeatable value in -1..1 for grid point (i, k).</summary>
    private static float Noise(int i, int k, int salt)
    {
        var h = (uint)(i * 73856093) ^ (uint)(k * 19349663) ^ (uint)(salt * 83492791);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 0xffff) / 32767.5f - 1f;
    }

    // ───────────────────────────── Mesh builder ─────────────────────────────

    /// <summary>A ring of a lathe along X: its centre x, radii (Y, Z), a vertical offset, its V in <see cref="Uv"/>.</summary>
    private readonly struct Ring
    {
        public readonly float X, RadiusY, RadiusZ, OffsetY, V;
        public readonly Rect Uv;

        public Ring(float x, float radiusY, float radiusZ, float offsetY, float v, Rect uv)
        {
            X = x;
            RadiusY = radiusY;
            RadiusZ = radiusZ;
            OffsetY = offsetY;
            V = v;
            Uv = uv;
        }
    }

    /// <summary>
    /// Flat-shaded, textured mesh builder: every triangle its own three vertices with the face normal
    /// (the low-poly look), one submesh.
    /// </summary>
    private sealed class Builder
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<int> _triangles = new();

        /// <summary>A triangle facing <paramref name="outward"/> (any vector its normal must have a positive dot with).</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 outward)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-14f)
                return;
            normal.Normalize();
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                (ub, uc) = (uc, ub);
                normal = -normal;
            }

            Add(a, ua, normal);
            Add(b, ub, normal);
            Add(c, uc, normal);
        }

        /// <summary>A quad a-b-c-d facing <paramref name="outward"/>.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 outward)
        {
            Tri(a, b, c, ua, ub, uc, outward);
            Tri(a, c, d, ua, uc, ud, outward);
        }

        /// <summary>A quad a-b-c-d over the whole of <paramref name="uv"/>.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uv, Vector3 outward) =>
            Quad(a, b, c, d, At(uv, 0f, 0f), At(uv, 1f, 0f), At(uv, 1f, 1f), At(uv, 0f, 1f), outward);

        /// <summary>A quad seen from both sides.</summary>
        public void TwoSidedQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            var normal = Vector3.Cross(b - a, c - a);
            Quad(a, b, c, d, ua, ub, uc, ud, normal);
            Quad(a, b, c, d, ua, ub, uc, ud, -normal);
        }

        /// <summary>A quad over the whole of <paramref name="uv"/>, seen from both sides.</summary>
        public void TwoSidedQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uv) =>
            TwoSidedQuad(a, b, c, d, At(uv, 0f, 0f), At(uv, 1f, 0f), At(uv, 1f, 1f), At(uv, 0f, 1f));

        /// <summary>
        /// A box of <paramref name="size"/> at <paramref name="centre"/>: its top and bottom faces over
        /// <paramref name="top"/> and <paramref name="bottom"/>, the faces across X over <paramref name="ends"/>,
        /// the faces across Z over <paramref name="sides"/> (each the whole rectangle).
        /// </summary>
        public void Box(Vector3 centre, Vector3 size, Quaternion rotation, Rect top, Rect bottom, Rect sides, Rect ends)
        {
            var h = size * 0.5f;
            var c = new Vector3[8];
            for (var i = 0; i < 8; i++)
                c[i] = centre + rotation * Vector3.Scale(h, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;
            var forward = rotation * Vector3.forward;
            Quad(c[2], c[3], c[7], c[6], top, up);
            Quad(c[0], c[1], c[5], c[4], bottom, -up);
            Quad(c[0], c[2], c[6], c[4], ends, -right);
            Quad(c[1], c[3], c[7], c[5], ends, right);
            Quad(c[0], c[1], c[3], c[2], sides, -forward);
            Quad(c[4], c[5], c[7], c[6], sides, forward);
        }

        /// <summary>A box with every face over <paramref name="uv"/>.</summary>
        public void Box(Vector3 centre, Vector3 size, Quaternion rotation, Rect uv) => Box(centre, size, rotation, uv, uv, uv, uv);

        /// <summary>
        /// A lathe along X through <paramref name="rings"/>: each pair of rings joined by a band of
        /// <paramref name="sides"/> quads (U round the ring, V between the rings' V in their rectangle),
        /// capped at the first and last ring when a cap rectangle is given. Where the texture changes, two
        /// rings stand at the same place, one with each rectangle, and no band joins them.
        /// <paramref name="inward"/> turns the faces inside (the inside of a cup).
        /// </summary>
        public void Lathe(Ring[] rings, int sides, Rect? startCap, Rect? endCap, bool inward = false, Matrix4x4? transform = null)
        {
            var m = transform ?? Matrix4x4.identity;
            Vector3 P(in Ring r, int s)
            {
                var a = Mathf.PI * 2f * (s % sides) / sides;
                return m.MultiplyPoint3x4(new Vector3(r.X, r.OffsetY + Mathf.Cos(a) * r.RadiusY, Mathf.Sin(a) * r.RadiusZ));
            }
            Vector3 Axis(in Ring r) => m.MultiplyPoint3x4(new Vector3(r.X, r.OffsetY, 0f));

            for (var k = 0; k < rings.Length - 1; k++)
            {
                var r0 = rings[k];
                var r1 = rings[k + 1];
                if (r0.Uv != r1.Uv)
                    continue; // a change of texture: the two rings stand at the same place, no band between them
                for (var s = 0; s < sides; s++)
                {
                    var u0 = (float)s / sides;
                    var u1 = (float)(s + 1) / sides;
                    var a = P(in r0, s);
                    var b = P(in r0, s + 1);
                    var c = P(in r1, s + 1);
                    var d = P(in r1, s);
                    var outward = (a + b + c + d) * 0.25f - (Axis(in r0) + Axis(in r1)) * 0.5f;
                    Quad(a, b, c, d, At(r0.Uv, u0, r0.V), At(r0.Uv, u1, r0.V), At(r0.Uv, u1, r1.V), At(r0.Uv, u0, r1.V), inward ? -outward : outward);
                }
            }

            void Cap(in Ring r, Rect uv, float direction)
            {
                var centre = Axis(in r);
                var normal = m.MultiplyVector(new Vector3(direction, 0f, 0f));
                for (var s = 0; s < sides; s++)
                {
                    var a0 = Mathf.PI * 2f * s / sides;
                    var a1 = Mathf.PI * 2f * (s + 1) / sides;
                    Tri(centre, P(in r, s), P(in r, s + 1), At(uv, 0.5f, 0.5f),
                        At(uv, 0.5f + 0.5f * Mathf.Cos(a0), 0.5f + 0.5f * Mathf.Sin(a0)),
                        At(uv, 0.5f + 0.5f * Mathf.Cos(a1), 0.5f + 0.5f * Mathf.Sin(a1)), inward ? -normal : normal);
                }
            }

            if (startCap.HasValue)
                Cap(in rings[0], startCap.Value, -1f);
            if (endCap.HasValue)
                Cap(in rings[rings.Length - 1], endCap.Value, 1f);
        }

        /// <summary>Moves every vertex so the lowest one is on the ground (y = 0).</summary>
        public void SnapToGround()
        {
            var min = float.MaxValue;
            foreach (var v in _vertices)
                min = Mathf.Min(min, v.y);
            for (var i = 0; i < _vertices.Count; i++)
                _vertices[i] -= new Vector3(0f, min, 0f);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Add(Vector3 v, Vector2 uv, Vector3 normal)
        {
            _triangles.Add(_vertices.Count);
            _vertices.Add(v);
            _normals.Add(normal);
            _uvs.Add(uv);
        }
    }
}
