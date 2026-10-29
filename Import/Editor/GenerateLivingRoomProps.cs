using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the living room props (P40.2, owner-approved list Q-20261028-9): LivingRug
/// (2.0 x 1.4 m), SofaCushion and SofaCushionB (0.40 m square), FoldedThrow (laid over a sofa
/// arm), MagazineRack, StackedBooks, Figurine, PhotoFrame, Candles (on a tray), TvRemote,
/// GameConsole (with a controller) and Pouf (0.48 m square). Every pivot is on the bottom
/// face at the centre, front +Z. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Living Room Props
/// </summary>
public static class GenerateLivingRoomProps
{
    [MenuItem("Tools/Procedural Cities/Generate Living Room Props")]
    public static void Generate()
    {
        var wood = LowPolyBoxMesh.GetMat("LP_LR_Wood", new Color(0.52f, 0.36f, 0.22f), 0f, 0.35f);
        var black = LowPolyBoxMesh.GetMat("LP_LR_Black", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.5f);

        BuildRug(LowPolyBoxMesh.GetMat("LP_LR_RugField", new Color(0.66f, 0.30f, 0.24f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_LR_RugBorder", new Color(0.86f, 0.80f, 0.68f), 0f, 0.05f));
        BuildCushion("SofaCushion", LowPolyBoxMesh.GetMat("LP_LR_CushionMustard", new Color(0.80f, 0.60f, 0.20f), 0f, 0.1f));
        BuildCushion("SofaCushionB", LowPolyBoxMesh.GetMat("LP_LR_CushionTeal", new Color(0.18f, 0.45f, 0.48f), 0f, 0.1f));
        BuildThrow(LowPolyBoxMesh.GetMat("LP_LR_Throw", new Color(0.55f, 0.60f, 0.68f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_LR_ThrowStripe", new Color(0.90f, 0.88f, 0.84f), 0f, 0.1f));
        BuildMagazineRack(wood);
        BuildStackedBooks();
        BuildFigurine(LowPolyBoxMesh.GetMat("LP_LR_Ceramic", new Color(0.94f, 0.93f, 0.90f), 0f, 0.7f), wood);
        BuildPhotoFrame(black, LowPolyBoxMesh.GetMat("LP_LR_Photo", new Color(0.55f, 0.70f, 0.82f), 0f, 0.6f));
        BuildCandles(wood, LowPolyBoxMesh.GetMat("LP_LR_Candle", new Color(0.96f, 0.93f, 0.84f), 0f, 0.3f), black);
        BuildRemote(black, LowPolyBoxMesh.GetMat("LP_LR_Button", new Color(0.75f, 0.20f, 0.18f), 0f, 0.4f));
        BuildGameConsole(black, LowPolyBoxMesh.GetMat("LP_LR_ConsoleLight", new Color(0.20f, 0.55f, 0.95f), 0f, 0.8f));
        BuildPouf(LowPolyBoxMesh.GetMat("LP_LR_Pouf", new Color(0.32f, 0.42f, 0.30f), 0f, 0.1f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    private static void Save(string name, string child, List<LowPolyBoxMesh.Part> parts)
    {
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine(name + "_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildRug(Material field, Material border)
    {
        const float w = 2.0f, d = 1.4f, h = 0.012f, b = 0.10f;
        Save("LivingRug", "rug", new List<LowPolyBoxMesh.Part>
        {
            Box(border, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            Box(field, -w * 0.5f + b, h, -d * 0.5f + b, w * 0.5f - b, h + 0.002f, d * 0.5f - b),
            Box(border, -0.35f, h + 0.002f, -0.25f, 0.35f, h + 0.003f, 0.25f),
        });
    }

    private static void BuildCushion(string name, Material fabric)
    {
        // A puffy square: thin seamed edge, fuller middle.
        const float w = 0.40f, h = 0.40f;
        Save(name, "cushion", new List<LowPolyBoxMesh.Part>
        {
            Box(fabric, -w * 0.5f, 0f, -0.04f, w * 0.5f, h, 0.04f),
            Box(fabric, -w * 0.5f + 0.04f, 0.04f, -0.07f, w * 0.5f - 0.04f, h - 0.04f, 0.07f),
            Box(fabric, -w * 0.5f + 0.09f, 0.09f, -0.085f, w * 0.5f - 0.09f, h - 0.09f, 0.085f),
        });
    }

    private static void BuildThrow(Material fabric, Material stripe)
    {
        // Folded over a sofa arm: a top fold and two hanging flaps, along the depth (Z).
        const float w = 0.20f, d = 0.42f, t = 0.02f, drop = 0.14f;
        Save("FoldedThrow", "throw", new List<LowPolyBoxMesh.Part>
        {
            Box(fabric, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, t, d * 0.5f),
            Box(fabric, -w * 0.5f - t, -drop, -d * 0.5f, -w * 0.5f, t, d * 0.5f),
            Box(fabric, w * 0.5f, -drop, -d * 0.5f, w * 0.5f + t, t, d * 0.5f),
            Box(stripe, -w * 0.5f - t - 0.001f, -drop + 0.02f, -d * 0.5f, w * 0.5f + t + 0.001f, -drop + 0.04f, d * 0.5f),
        });
    }

    private static void BuildMagazineRack(Material wood)
    {
        const float w = 0.40f, h = 0.38f, d = 0.22f, t = 0.02f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, t, d * 0.5f),
            Box(wood, -w * 0.5f, 0f, -d * 0.5f, -w * 0.5f + t, h, d * 0.5f),
            Box(wood, w * 0.5f - t, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            Box(wood, -w * 0.5f, 0f, -t * 0.5f, w * 0.5f, h * 0.8f, t * 0.5f),
        };
        var colours = new[] { new Color(0.85f, 0.25f, 0.22f), new Color(0.95f, 0.95f, 0.92f), new Color(0.25f, 0.45f, 0.75f), new Color(0.95f, 0.80f, 0.30f) };
        for (var i = 0; i < 4; i++)
        {
            var mat = LowPolyBoxMesh.GetMat($"LP_LR_Magazine{i}", colours[i], 0f, 0.3f);
            var z = (i < 2 ? -1f : 1f) * (0.03f + (i % 2) * 0.03f);
            parts.Add(Box(mat, -w * 0.5f + t + 0.02f, t, z - 0.006f, w * 0.5f - t - 0.02f, h + 0.02f - i * 0.02f, z + 0.006f));
        }

        Save("MagazineRack", "rack", parts);
    }

    private static void BuildStackedBooks()
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        var colours = new[] { new Color(0.20f, 0.30f, 0.50f), new Color(0.62f, 0.18f, 0.16f), new Color(0.88f, 0.84f, 0.72f) };
        var sizes = new[] { new Vector3(0.28f, 0.045f, 0.21f), new Vector3(0.25f, 0.035f, 0.19f), new Vector3(0.22f, 0.03f, 0.16f) };
        var y = 0f;
        for (var i = 0; i < 3; i++)
        {
            var mat = LowPolyBoxMesh.GetMat($"LP_LR_Book{i}", colours[i], 0f, 0.3f);
            var s = sizes[i];
            parts.Add(new LowPolyBoxMesh.Part(mat, new Vector3(i * 0.01f, y + s.y * 0.5f, -i * 0.008f), s, Quaternion.Euler(0f, i * 7f - 5f, 0f)));
            y += s.y;
        }

        Save("StackedBooks", "books", parts);
    }

    private static void BuildFigurine(Material ceramic, Material wood)
    {
        Save("Figurine", "figurine", new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.05f, 0f, -0.05f, 0.05f, 0.02f, 0.05f),
            new(ceramic, new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.12f, 0.05f), Quaternion.Euler(0f, 45f, 0f)),
            new(ceramic, new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.12f, 0.05f), Quaternion.identity),
            new(ceramic, new Vector3(0f, 0.17f, 0f), new Vector3(0.045f, 0.05f, 0.045f), Quaternion.Euler(30f, 45f, 0f)),
        });
    }

    private static void BuildPhotoFrame(Material frame, Material photo)
    {
        // Standing frame leaning back 12 degrees on a strut.
        var tilt = Quaternion.Euler(-12f, 0f, 0f);
        Save("PhotoFrame", "frame", new List<LowPolyBoxMesh.Part>
        {
            new(frame, tilt * new Vector3(0f, 0.11f, 0f), new Vector3(0.18f, 0.22f, 0.015f), tilt),
            new(photo, tilt * new Vector3(0f, 0.11f, 0.008f), new Vector3(0.14f, 0.18f, 0.002f), tilt),
            new(frame, new Vector3(0f, 0.08f, -0.05f), new Vector3(0.02f, 0.15f, 0.01f), Quaternion.Euler(25f, 0f, 0f)),
        });
    }

    private static void BuildCandles(Material wood, Material wax, Material wick)
    {
        var parts = new List<LowPolyBoxMesh.Part> { Box(wood, -0.12f, 0f, -0.06f, 0.12f, 0.015f, 0.06f) };
        var heights = new[] { 0.16f, 0.11f, 0.07f };
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 0.075f;
            parts.Add(new LowPolyBoxMesh.Part(wax, new Vector3(x, 0.015f + heights[i] * 0.5f, 0f), new Vector3(0.05f, heights[i], 0.05f), Quaternion.Euler(0f, 45f, 0f)));
            parts.Add(new LowPolyBoxMesh.Part(wax, new Vector3(x, 0.015f + heights[i] * 0.5f, 0f), new Vector3(0.05f, heights[i], 0.05f), Quaternion.identity));
            parts.Add(Box(wick, x - 0.003f, 0.015f + heights[i], -0.003f, x + 0.003f, 0.03f + heights[i], 0.003f));
        }

        Save("Candles", "candles", parts);
    }

    private static void BuildRemote(Material body, Material button)
    {
        Save("TvRemote", "remote", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.025f, 0f, -0.09f, 0.025f, 0.02f, 0.09f),
            Box(button, -0.008f, 0.02f, 0.05f, 0.008f, 0.024f, 0.066f),
        });
    }

    private static void BuildGameConsole(Material body, Material light)
    {
        Save("GameConsole", "console", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.15f, 0f, -0.11f, 0.15f, 0.06f, 0.11f),
            Box(light, -0.13f, 0.03f, 0.11f, 0.13f, 0.035f, 0.112f),
            // Controller beside it, turned a little.
            new(body, new Vector3(0.26f, 0.02f, 0.04f), new Vector3(0.15f, 0.04f, 0.09f), Quaternion.Euler(0f, -15f, 0f)),
            new(light, new Vector3(0.26f, 0.041f, 0.04f), new Vector3(0.03f, 0.003f, 0.03f), Quaternion.Euler(0f, -15f, 0f)),
        });
    }

    private static void BuildPouf(Material fabric)
    {
        // Square upholstered pouf with a slightly narrower, softer top.
        const float side = 0.48f, h = 0.40f;
        Save("Pouf", "pouf", new List<LowPolyBoxMesh.Part>
        {
            Box(fabric, -side * 0.5f, 0f, -side * 0.5f, side * 0.5f, h - 0.04f, side * 0.5f),
            Box(fabric, -side * 0.5f + 0.025f, h - 0.04f, -side * 0.5f + 0.025f, side * 0.5f - 0.025f, h, side * 0.5f - 0.025f),
        });
    }
}
