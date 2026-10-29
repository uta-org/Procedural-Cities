using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the tower crane that stands beside a building under works (P40.23, owner Q-20261029-7 B:
/// "a big crane that covers the building"), in three pieces the city stacks (owner: "modules as
/// high as each floor, always ending one or two modules above the floors"):
/// <list type="bullet">
/// <item><c>TowerCraneBase</c>: the concrete footing, 4 x 4 x 1 m.</item>
/// <item><c>TowerCraneMast</c>: one lattice mast module, <see cref="ModuleHeight"/> high (the
/// city's floor height; scaled in height when the floor height differs).</item>
/// <item><c>TowerCraneTop</c>: the slewing unit with the cab, a lattice jib 46 m long along +Z
/// with its trolley and hook, the counter-jib with its counterweights along -Z, and the tower head
/// with the pendant ties.</item>
/// </list>
/// Every pivot is on its bottom face at the mast's centre. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Tower Crane
/// </summary>
public static class GenerateTowerCrane
{
    /// <summary>Mast module height, metres: the default floor height.</summary>
    private const float ModuleHeight = 4f;

    /// <summary>The top piece's bottom (its pivot, on the last mast module), metres.</summary>
    private const float TopBottom = 0f;

    /// <summary>Mast width across its chords, metres.</summary>
    private const float MastWidth = 1.6f;

    /// <summary>Lattice segment length of the jib and counter-jib, metres.</summary>
    private const float Segment = 3f;

    /// <summary>Jib length from the mast's centre, metres.</summary>
    private const float JibLength = 46f;

    /// <summary>Counter-jib length from the mast's centre, metres.</summary>
    private const float CounterJibLength = 14f;

    [MenuItem("Tools/Procedural Cities/Generate Tower Crane")]
    public static void Generate()
    {
        var yellow = LowPolyBoxMesh.GetMat("LP_TC_Yellow", new Color(0.95f, 0.72f, 0.08f), 0.3f, 0.45f);
        var concrete = LowPolyBoxMesh.GetMat("LP_TC_Concrete", new Color(0.62f, 0.61f, 0.58f), 0f, 0.1f);
        var black = LowPolyBoxMesh.GetMat("LP_TC_Black", new Color(0.10f, 0.10f, 0.11f), 0.4f, 0.4f);
        var white = LowPolyBoxMesh.GetMat("LP_TC_White", new Color(0.93f, 0.93f, 0.90f), 0f, 0.4f);
        var glass = LowPolyBoxMesh.GetMat("LP_TC_Glass", new Color(0.35f, 0.50f, 0.60f), 0.2f, 0.9f);
        var red = LowPolyBoxMesh.GetMat("LP_TC_Red", new Color(0.80f, 0.12f, 0.10f), 0f, 0.5f);

        Save("TowerCraneBase", "tower_crane_base", new List<LowPolyBoxMesh.Part> { Box(concrete, -2f, 0f, -2f, 2f, 1f, 2f) });

        var mast = new List<LowPolyBoxMesh.Part>();
        BuildMastModule(mast, yellow);
        Save("TowerCraneMast", "tower_crane_mast", mast);

        var top = new List<LowPolyBoxMesh.Part>();
        BuildSlewingUnit(top, yellow, white, glass);
        BuildJib(top, yellow, black, red);
        BuildCounterJib(top, yellow, concrete);
        BuildTowerHead(top, yellow, black, red);
        Save("TowerCraneTop", "tower_crane_top", top);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Save(string name, string child, List<LowPolyBoxMesh.Part> parts)
    {
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine(name + "_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    /// <summary>A bar of <paramref name="thickness"/> from <paramref name="a"/> to <paramref name="b"/>.</summary>
    private static LowPolyBoxMesh.Part Bar(Material mat, Vector3 a, Vector3 b, float thickness)
    {
        var d = b - a;
        return new LowPolyBoxMesh.Part(mat, (a + b) * 0.5f, new Vector3(thickness, thickness, d.magnitude),
            Quaternion.LookRotation(d.normalized, Mathf.Abs(d.normalized.y) > 0.99f ? Vector3.forward : Vector3.up));
    }

    private static void BuildMastModule(List<LowPolyBoxMesh.Part> parts, Material steel)
    {
        // One module: the four chords, a ring at its foot (the next module's foot closes its top)
        // and a cross of diagonals on each face.
        const float h = MastWidth * 0.5f;
        var corners = new[] { new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h) };
        foreach (var c in corners)
            parts.Add(Box(steel, c.x - 0.09f, 0f, c.z - 0.09f, c.x + 0.09f, ModuleHeight, c.z + 0.09f));

        var up = Vector3.up * ModuleHeight;
        for (var i = 0; i < 4; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % 4];
            parts.Add(Bar(steel, a + Vector3.up * 0.05f, b + Vector3.up * 0.05f, 0.10f));
            parts.Add(Bar(steel, a, b + up, 0.08f));
            parts.Add(Bar(steel, b, a + up, 0.08f));
        }
    }

    private static void BuildSlewingUnit(List<LowPolyBoxMesh.Part> parts, Material steel, Material white, Material glass)
    {
        // The turntable and the machinery deck on the mast, the cab on the jib's right side.
        parts.Add(Box(steel, -1.1f, TopBottom, -1.1f, 1.1f, TopBottom + 1.2f, 1.1f));
        parts.Add(Box(white, 1.1f, TopBottom + 0.2f, 0.2f, 2.7f, TopBottom + 2.2f, 2.0f));
        parts.Add(Box(glass, 1.15f, TopBottom + 1.0f, 2.0f, 2.65f, TopBottom + 2.1f, 2.03f));
        parts.Add(Box(glass, 2.7f, TopBottom + 1.0f, 0.3f, 2.73f, TopBottom + 2.1f, 1.9f));
    }

    private static void BuildJib(List<LowPolyBoxMesh.Part> parts, Material steel, Material cable, Material red)
    {
        // A triangular lattice, 1.4 m wide and 1.6 m high: two bottom chords and a top chord, a
        // ring and diagonals on both sides every segment; the trolley at 60 % with its hook.
        const float y0 = TopBottom + 1.2f;
        const float y1 = y0 + 1.6f;
        const float z0 = 1.1f;
        parts.Add(Box(steel, -0.75f, y0, z0, -0.6f, y0 + 0.15f, JibLength));
        parts.Add(Box(steel, 0.6f, y0, z0, 0.75f, y0 + 0.15f, JibLength));
        parts.Add(Box(steel, -0.07f, y1 - 0.15f, z0, 0.07f, y1, JibLength - 1f));
        var segments = Mathf.RoundToInt((JibLength - z0) / Segment);
        var step = (JibLength - z0) / segments;
        for (var s = 0; s <= segments; s++)
        {
            var z = z0 + s * step;
            var top = new Vector3(0f, y1 - 0.08f, Mathf.Min(z, JibLength - 1f));
            parts.Add(Bar(steel, new Vector3(-0.67f, y0 + 0.08f, z), new Vector3(0.67f, y0 + 0.08f, z), 0.08f));
            if (s == segments)
                continue;
            var next = new Vector3(0f, y1 - 0.08f, Mathf.Min(z + step, JibLength - 1f));
            parts.Add(Bar(steel, new Vector3(-0.67f, y0 + 0.08f, z), next, 0.07f));
            parts.Add(Bar(steel, new Vector3(0.67f, y0 + 0.08f, z), next, 0.07f));
            parts.Add(Bar(steel, new Vector3(-0.67f, y0 + 0.08f, z), top, 0.07f));
            parts.Add(Bar(steel, new Vector3(0.67f, y0 + 0.08f, z), top, 0.07f));
        }

        parts.Add(Box(red, -0.3f, y1, JibLength - 1.2f, 0.3f, y1 + 0.3f, JibLength - 0.6f));
        const float trolleyZ = JibLength * 0.6f;
        parts.Add(Box(steel, -0.9f, y0 - 0.6f, trolleyZ - 0.8f, 0.9f, y0, trolleyZ + 0.8f));
        foreach (var x in new[] { -0.3f, 0.3f })
            parts.Add(Box(cable, x - 0.02f, y0 - 10f, trolleyZ - 0.02f, x + 0.02f, y0 - 0.6f, trolleyZ + 0.02f));
        parts.Add(Box(red, -0.45f, y0 - 11.2f, trolleyZ - 0.25f, 0.45f, y0 - 10f, trolleyZ + 0.25f));
        parts.Add(Box(cable, -0.06f, y0 - 11.9f, trolleyZ - 0.06f, 0.06f, y0 - 11.2f, trolleyZ + 0.06f));
    }

    private static void BuildCounterJib(List<LowPolyBoxMesh.Part> parts, Material steel, Material concrete)
    {
        // A flat platform with a handrail and four concrete counterweights at its end.
        const float y0 = TopBottom + 1.2f;
        parts.Add(Box(steel, -1.0f, y0, -CounterJibLength, 1.0f, y0 + 0.35f, -1.1f));
        foreach (var x in new[] { -1.0f, 0.92f })
        {
            parts.Add(Box(steel, x, y0 + 1.0f, -CounterJibLength, x + 0.08f, y0 + 1.08f, -1.1f));
            for (var z = -CounterJibLength; z < -1.1f; z += Segment)
                parts.Add(Box(steel, x, y0 + 0.35f, z, x + 0.08f, y0 + 1.0f, z + 0.08f));
        }

        for (var i = 0; i < 4; i++)
        {
            var z = -CounterJibLength + 0.1f + i * 0.85f;
            parts.Add(Box(concrete, -1.1f, y0 - 2.4f, z, 1.1f, y0 + 0.35f, z + 0.8f));
        }
    }

    private static void BuildTowerHead(List<LowPolyBoxMesh.Part> parts, Material steel, Material cable, Material red)
    {
        // Four posts from the turntable to an apex 8 m up, a red light on it, and the pendant
        // ties to the jib (at 70 %) and to the counter-jib's end.
        const float y0 = TopBottom + 1.2f;
        var apex = new Vector3(0f, y0 + 8f, 0f);
        foreach (var (x, z) in new[] { (-0.9f, -0.9f), (0.9f, -0.9f), (0.9f, 0.9f), (-0.9f, 0.9f) })
            parts.Add(Bar(steel, new Vector3(x, y0, z), apex, 0.16f));
        parts.Add(Box(red, -0.2f, apex.y, -0.2f, 0.2f, apex.y + 0.4f, 0.2f));
        foreach (var x in new[] { -0.25f, 0.25f })
        {
            parts.Add(Bar(cable, apex + new Vector3(x, 0f, 0f), new Vector3(x, y0 + 1.6f, JibLength * 0.7f), 0.05f));
            parts.Add(Bar(cable, apex + new Vector3(x, 0f, 0f), new Vector3(x, y0 + 0.35f, -CounterJibLength + 0.5f), 0.05f));
        }
    }
}
