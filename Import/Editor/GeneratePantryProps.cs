using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the home storeroom (pantry) props (P40.17, owner-approved list Q-20261028-9): PantryJars,
/// WaterJugs (two 5-litre jugs), Sacks, FruitBasket, Bicycle (on its side along the wall) and
/// Toolbox. The boxes, suitcase, bucket and mop and the ladder reuse CardboardBoxes, Suitcase,
/// BroomAndMop and Stepladder. Every piece has its pivot on the bottom face at the centre, front +Z.
/// Output: Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Pantry Props
/// </summary>
public static class GeneratePantryProps
{
    [MenuItem("Tools/Procedural Cities/Generate Pantry Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GeneratePantryProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_PN_Black", new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.4f);
        var glass = LowPolyBoxMesh.GetMat("LP_PN_Glass", new Color(0.82f, 0.90f, 0.92f), 0f, 0.95f);
        var wicker = LowPolyBoxMesh.GetMat("LP_PN_Wicker", new Color(0.70f, 0.55f, 0.32f), 0f, 0.2f);

        BuildPantryJars(glass, new[]
        {
            LowPolyBoxMesh.GetMat("LP_PN_Pasta", new Color(0.92f, 0.80f, 0.45f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_PN_Lentils", new Color(0.55f, 0.35f, 0.20f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_PN_Rice", new Color(0.95f, 0.94f, 0.90f), 0f, 0.3f),
        }, LowPolyBoxMesh.GetMat("LP_PN_Lid", new Color(0.65f, 0.45f, 0.25f), 0f, 0.4f));
        BuildWaterJugs(LowPolyBoxMesh.GetMat("LP_PN_Jug", new Color(0.70f, 0.85f, 0.95f), 0f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_PN_JugCap", new Color(0.15f, 0.35f, 0.70f), 0f, 0.5f));
        BuildSacks(LowPolyBoxMesh.GetMat("LP_PN_Sack", new Color(0.78f, 0.70f, 0.55f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_PN_SackPrint", new Color(0.55f, 0.30f, 0.20f), 0f, 0.05f));
        BuildFruitBasket(wicker, new[]
        {
            LowPolyBoxMesh.GetMat("LP_PN_Apple", new Color(0.75f, 0.12f, 0.12f), 0f, 0.5f),
            LowPolyBoxMesh.GetMat("LP_PN_Orange", new Color(0.95f, 0.55f, 0.10f), 0f, 0.4f),
            LowPolyBoxMesh.GetMat("LP_PN_Onion", new Color(0.80f, 0.60f, 0.40f), 0f, 0.3f),
        });
        BuildBicycle(LowPolyBoxMesh.GetMat("LP_PN_Frame", new Color(0.20f, 0.45f, 0.60f), 0.4f, 0.6f), black, chrome);
        BuildToolbox(LowPolyBoxMesh.GetMat("LP_PN_Toolbox", new Color(0.78f, 0.15f, 0.12f), 0.3f, 0.5f), black, chrome);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    /// <summary>
    /// A 12-sided prism <paramref name="width"/> across the flats: six boxes the full width long
    /// and width x tan 15 degrees wide, turned 30 degrees apart (their ends are the sides).
    /// </summary>
    private static void Round(List<LowPolyBoxMesh.Part> parts, Material mat, float x, float y0, float z, float width, float y1)
    {
        var centre = new Vector3(x, (y0 + y1) * 0.5f, z);
        var size = new Vector3(width, y1 - y0, width * Mathf.Tan(15f * Mathf.Deg2Rad));
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(mat, centre, size, Quaternion.Euler(0f, i * 30f, 0f)));
    }

    /// <summary>A 12-sided disc <paramref name="width"/> across, in the XY plane (a wheel), <paramref name="depth"/> thick.</summary>
    private static void Wheel(List<LowPolyBoxMesh.Part> parts, Material mat, Vector3 centre, float width, float depth)
    {
        var size = new Vector3(width, width * Mathf.Tan(15f * Mathf.Deg2Rad), depth);
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(mat, centre, size, Quaternion.Euler(0f, 0f, i * 30f)));
    }

    private static void Save(string name, string child, List<LowPolyBoxMesh.Part> parts)
    {
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine(name + "_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildPantryJars(Material glass, Material[] food, Material lid)
    {
        // Three storage jars in a row, 0.30 m wide.
        var parts = new List<LowPolyBoxMesh.Part>();
        var heights = new[] { 0.18f, 0.14f, 0.16f };
        for (var i = 0; i < 3; i++)
        {
            var x = -0.10f + i * 0.10f;
            Round(parts, food[i], x, 0.005f, 0f, 0.075f, heights[i] * 0.75f);
            Round(parts, glass, x, 0f, 0f, 0.085f, heights[i]);
            Round(parts, lid, x, heights[i], 0f, 0.09f, heights[i] + 0.02f);
        }

        Save("PantryJars", "pantry_jars", parts);
    }

    private static void BuildWaterJugs(Material jug, Material cap)
    {
        // Two 5-litre jugs, 0.40 x 0.32 x 0.16 m.
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var x in new[] { -0.10f, 0.10f })
        {
            parts.Add(Box(jug, x - 0.09f, 0f, -0.075f, x + 0.09f, 0.26f, 0.075f));
            parts.Add(Box(jug, x - 0.03f, 0.26f, -0.03f, x + 0.03f, 0.30f, 0.03f));
            parts.Add(Box(cap, x - 0.035f, 0.30f, -0.035f, x + 0.035f, 0.32f, 0.035f));
            parts.Add(Box(jug, x + 0.04f, 0.22f, -0.01f, x + 0.08f, 0.28f, 0.01f));
        }

        Save("WaterJugs", "water_jugs", parts);
    }

    private static void BuildSacks(Material sack, Material print)
    {
        // Two full sacks leaning together, 0.70 x 0.60 x 0.35 m.
        Save("Sacks", "sacks", new List<LowPolyBoxMesh.Part>
        {
            new(sack, new Vector3(-0.17f, 0.27f, 0f), new Vector3(0.36f, 0.54f, 0.30f), Quaternion.Euler(0f, 0f, 4f)),
            new(sack, new Vector3(-0.17f, 0.56f, 0f), new Vector3(0.28f, 0.08f, 0.22f), Quaternion.Euler(0f, 0f, 4f)),
            new(print, new Vector3(-0.17f, 0.30f, 0.152f), new Vector3(0.20f, 0.16f, 0.004f), Quaternion.Euler(0f, 0f, 4f)),
            new(sack, new Vector3(0.19f, 0.23f, 0.01f), new Vector3(0.32f, 0.46f, 0.28f), Quaternion.Euler(0f, 0f, -6f)),
            new(sack, new Vector3(0.19f, 0.48f, 0.01f), new Vector3(0.24f, 0.07f, 0.20f), Quaternion.Euler(0f, 0f, -6f)),
            new(print, new Vector3(0.19f, 0.26f, 0.152f), new Vector3(0.18f, 0.12f, 0.004f), Quaternion.Euler(0f, 0f, -6f)),
        });
    }

    private static void BuildFruitBasket(Material wicker, Material[] produce)
    {
        // A wicker basket, 0.40 x 0.20 x 0.28 m, with a handle and fruit.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wicker, -0.20f, 0f, -0.14f, 0.20f, 0.015f, 0.14f),
            Box(wicker, -0.20f, 0f, -0.14f, 0.20f, 0.15f, -0.125f),
            Box(wicker, -0.20f, 0f, 0.125f, 0.20f, 0.15f, 0.14f),
            Box(wicker, -0.20f, 0f, -0.125f, -0.185f, 0.15f, 0.125f),
            Box(wicker, 0.185f, 0f, -0.125f, 0.20f, 0.15f, 0.125f),
            Box(wicker, -0.19f, 0.15f, -0.012f, -0.17f, 0.28f, 0.012f),
            Box(wicker, 0.17f, 0.15f, -0.012f, 0.19f, 0.28f, 0.012f),
            Box(wicker, -0.19f, 0.26f, -0.012f, 0.19f, 0.28f, 0.012f),
        };
        for (var i = 0; i < 6; i++)
        {
            parts.Add(new LowPolyBoxMesh.Part(produce[i % produce.Length], new Vector3(-0.12f + (i % 3) * 0.12f, 0.10f + (i / 3) * 0.04f, -0.05f + (i / 3) * 0.10f),
                new Vector3(0.08f, 0.07f, 0.08f), Quaternion.Euler(10f * i, 30f * i, 0f)));
        }

        Save("FruitBasket", "fruit_basket", parts);
    }

    private static void BuildBicycle(Material frame, Material black, Material metal)
    {
        // Along X, 1.70 m long and 1.00 m high, standing upright; its 0.60 m bars across (Z).
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var x in new[] { -0.55f, 0.55f })
        {
            Wheel(parts, black, new Vector3(x, 0.34f, 0f), 0.68f, 0.04f);
            Wheel(parts, metal, new Vector3(x, 0.34f, 0f), 0.56f, 0.045f);
            Wheel(parts, black, new Vector3(x, 0.34f, 0f), 0.08f, 0.06f);
        }

        parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(-0.27f, 0.50f, 0f), new Vector3(0.62f, 0.035f, 0.035f), Quaternion.Euler(0f, 0f, 30f)));
        parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(0.15f, 0.48f, 0f), new Vector3(0.70f, 0.035f, 0.035f), Quaternion.Euler(0f, 0f, -20f)));
        parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(0.05f, 0.72f, 0f), new Vector3(0.75f, 0.035f, 0.035f), Quaternion.Euler(0f, 0f, 2f)));
        parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(-0.38f, 0.50f, 0f), new Vector3(0.035f, 0.50f, 0.035f), Quaternion.Euler(0f, 0f, 18f)));
        parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(0.50f, 0.60f, 0f), new Vector3(0.035f, 0.55f, 0.035f), Quaternion.Euler(0f, 0f, -18f)));
        parts.Add(Box(black, -0.42f, 0.84f, -0.06f, -0.22f, 0.88f, 0.06f));
        parts.Add(Box(metal, 0.40f, 0.96f, -0.30f, 0.44f, 1.00f, 0.30f));
        parts.Add(Box(black, 0.39f, 0.95f, -0.30f, 0.45f, 1.01f, -0.22f));
        parts.Add(Box(black, 0.39f, 0.95f, 0.22f, 0.45f, 1.01f, 0.30f));
        Save("Bicycle", "bicycle", parts);
    }

    private static void BuildToolbox(Material box, Material black, Material metal)
    {
        // A 0.45 x 0.22 x 0.22 m metal toolbox with a handle and latches.
        Save("Toolbox", "toolbox", new List<LowPolyBoxMesh.Part>
        {
            Box(box, -0.225f, 0f, -0.11f, 0.225f, 0.18f, 0.11f),
            Box(box, -0.23f, 0.18f, -0.115f, 0.23f, 0.20f, 0.115f),
            Box(black, -0.12f, 0.20f, -0.012f, -0.10f, 0.26f, 0.012f),
            Box(black, 0.10f, 0.20f, -0.012f, 0.12f, 0.26f, 0.012f),
            Box(black, -0.12f, 0.25f, -0.012f, 0.12f, 0.27f, 0.012f),
            Box(metal, -0.17f, 0.15f, 0.11f, -0.13f, 0.19f, 0.12f),
            Box(metal, 0.13f, 0.15f, 0.11f, 0.17f, 0.19f, 0.12f),
        });
    }
}
