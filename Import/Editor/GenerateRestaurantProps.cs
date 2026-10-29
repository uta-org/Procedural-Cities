using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the restaurant props (P40.13, owner-approved list Q-20261028-9): RestaurantMenu, SaltPepper,
/// TableCandle, BarBottles, CashRegister, GlassRack (hung from the ceiling, pivot on its ceiling
/// plate), MenuChalkboard (wall-hung, back face at Z = 0) and HighChair. The napkin holders and the
/// coat stand reuse NapkinHolder and Hanger. Table, bar and floor pieces have their pivot on the
/// bottom face at the centre, front +Z. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Restaurant Props
/// </summary>
public static class GenerateRestaurantProps
{
    [MenuItem("Tools/Procedural Cities/Generate Restaurant Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateRestaurantProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_RS_Black", new Color(0.11f, 0.11f, 0.12f), 0.1f, 0.5f);
        var white = LowPolyBoxMesh.GetMat("LP_RS_White", new Color(0.95f, 0.95f, 0.93f), 0f, 0.6f);
        var wood = LowPolyBoxMesh.GetMat("LP_RS_Wood", new Color(0.50f, 0.34f, 0.20f), 0f, 0.35f);
        var glass = LowPolyBoxMesh.GetMat("LP_RS_Glass", new Color(0.82f, 0.90f, 0.94f), 0f, 0.95f);

        BuildMenu(LowPolyBoxMesh.GetMat("LP_RS_MenuCover", new Color(0.45f, 0.12f, 0.12f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_RS_MenuGold", new Color(0.85f, 0.70f, 0.35f), 0.5f, 0.5f));
        BuildSaltPepper(white, black, chrome);
        BuildTableCandle(glass, LowPolyBoxMesh.GetMat("LP_RS_Wax", new Color(0.96f, 0.93f, 0.84f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_RS_Flame", new Color(1.00f, 0.80f, 0.30f), 0f, 0.5f));
        BuildBarBottles(new[]
        {
            LowPolyBoxMesh.GetMat("LP_RS_BottleGreen", new Color(0.15f, 0.32f, 0.18f), 0.1f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_RS_BottleAmber", new Color(0.55f, 0.30f, 0.08f), 0.1f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_RS_BottleClear", new Color(0.80f, 0.86f, 0.88f), 0.1f, 0.9f),
        }, LowPolyBoxMesh.GetMat("LP_RS_Label", new Color(0.92f, 0.88f, 0.78f), 0f, 0.3f));
        BuildCashRegister(black, LowPolyBoxMesh.GetMat("LP_RS_Screen", new Color(0.30f, 0.55f, 0.65f), 0f, 0.8f), chrome);
        BuildGlassRack(wood, glass, chrome);
        BuildMenuChalkboard(wood, LowPolyBoxMesh.GetMat("LP_RS_Chalk", new Color(0.13f, 0.18f, 0.15f), 0f, 0.2f), white,
            LowPolyBoxMesh.GetMat("LP_RS_ChalkYellow", new Color(0.95f, 0.85f, 0.40f), 0f, 0.2f));
        BuildHighChair(wood, LowPolyBoxMesh.GetMat("LP_RS_Tray", new Color(0.85f, 0.85f, 0.82f), 0f, 0.5f));
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

    private static void Save(string name, string child, List<LowPolyBoxMesh.Part> parts)
    {
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine(name + "_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildMenu(Material cover, Material gold)
    {
        // A closed menu, 0.22 x 0.32 m, lying flat.
        Save("RestaurantMenu", "menu", new List<LowPolyBoxMesh.Part>
        {
            Box(cover, -0.11f, 0f, -0.16f, 0.11f, 0.012f, 0.16f),
            Box(gold, -0.06f, 0.012f, 0.06f, 0.06f, 0.013f, 0.09f),
            Box(gold, -0.10f, 0.012f, -0.15f, -0.095f, 0.013f, 0.15f),
        });
    }

    private static void BuildSaltPepper(Material salt, Material pepper, Material caps)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, salt, -0.025f, 0f, 0f, 0.035f, 0.08f);
        Round(parts, caps, -0.025f, 0.08f, 0f, 0.036f, 0.095f);
        Round(parts, pepper, 0.025f, 0f, 0f, 0.035f, 0.08f);
        Round(parts, caps, 0.025f, 0.08f, 0f, 0.036f, 0.095f);
        Save("SaltPepper", "salt_pepper", parts);
    }

    private static void BuildTableCandle(Material glass, Material wax, Material flame)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, glass, 0f, 0f, 0f, 0.08f, 0.09f);
        Round(parts, wax, 0f, 0.005f, 0f, 0.06f, 0.05f);
        parts.Add(Box(flame, -0.006f, 0.05f, -0.006f, 0.006f, 0.075f, 0.006f));
        Save("TableCandle", "table_candle", parts);
    }

    private static void BuildBarBottles(Material[] bottles, Material label)
    {
        // Three bottles in a row along X, 0.30 m tall.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            var x = -0.09f + i * 0.09f;
            var h = i == 1 ? 0.24f : 0.21f;
            Round(parts, bottles[i], x, 0f, 0f, 0.075f, h);
            Round(parts, label, x, h * 0.35f, 0f, 0.078f, h * 0.65f);
            Round(parts, bottles[i], x, h, 0f, 0.03f, h + 0.08f);
        }

        Save("BarBottles", "bar_bottles", parts);
    }

    private static void BuildCashRegister(Material body, Material screen, Material metal)
    {
        // A till: drawer base, keypad, receipt printer and a customer screen at the back.
        Save("CashRegister", "cash_register", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.20f, 0f, -0.18f, 0.20f, 0.08f, 0.18f),
            Box(metal, -0.19f, 0.02f, 0.18f, 0.19f, 0.06f, 0.182f),
            new(body, new Vector3(-0.05f, 0.10f, 0.05f), new Vector3(0.24f, 0.04f, 0.16f), Quaternion.Euler(15f, 0f, 0f)),
            Box(body, 0.10f, 0.08f, -0.02f, 0.19f, 0.16f, 0.12f),
            Box(body, -0.03f, 0.08f, -0.16f, 0.03f, 0.30f, -0.12f),
            new(screen, new Vector3(0f, 0.34f, -0.14f), new Vector3(0.22f, 0.14f, 0.02f), Quaternion.Euler(-10f, 0f, 0f)),
        });
    }

    private static void BuildGlassRack(Material wood, Material glass, Material metal)
    {
        // Pivot on the ceiling plate: two rods down to a 0.90 x 0.30 m wooden rack with two rows of wine
        // glasses hanging upside down under it (0.45 m deep in all).
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(metal, -0.40f, -0.25f, -0.01f, -0.38f, 0f, 0.01f),
            Box(metal, 0.38f, -0.25f, -0.01f, 0.40f, 0f, 0.01f),
            Box(wood, -0.45f, -0.28f, -0.15f, 0.45f, -0.25f, 0.15f),
        };
        for (var row = 0; row < 2; row++)
        {
            var z = row == 0 ? -0.07f : 0.07f;
            for (var i = 0; i < 6; i++)
            {
                var x = -0.35f + i * 0.14f;
                parts.Add(Box(glass, x - 0.005f, -0.33f, z - 0.005f, x + 0.005f, -0.28f, z + 0.005f));
                Round(parts, glass, x, -0.45f, z, 0.06f, -0.33f);
            }
        }

        Save("GlassRack", "glass_rack", parts);
    }

    private static void BuildMenuChalkboard(Material frame, Material board, Material chalk, Material yellow)
    {
        // Wall-hung, 0.80 x 0.60 m: a heading and lines of chalk.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.40f, 0f, 0f, 0.40f, 0.60f, 0.025f),
            Box(board, -0.37f, 0.03f, 0.025f, 0.37f, 0.57f, 0.027f),
            Box(yellow, -0.20f, 0.47f, 0.027f, 0.20f, 0.52f, 0.028f),
        };
        for (var i = 0; i < 6; i++)
        {
            var y = 0.08f + i * 0.06f;
            var w = i % 2 == 0 ? 0.26f : 0.20f;
            parts.Add(Box(chalk, -0.30f, y, 0.027f, -0.30f + w, y + 0.015f, 0.028f));
            parts.Add(Box(chalk, 0.20f, y, 0.027f, 0.28f, y + 0.015f, 0.028f));
        }

        Save("MenuChalkboard", "menu_chalkboard", parts);
    }

    private static void BuildHighChair(Material wood, Material tray)
    {
        // 0.50 x 1.00 x 0.55 m: splayed legs, a seat at 0.55 m with a back, and a tray to the front.
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var sx in new[] { -1f, 1f })
        {
            foreach (var sz in new[] { -1f, 1f })
                parts.Add(new LowPolyBoxMesh.Part(wood, new Vector3(sx * 0.18f, 0.27f, sz * 0.20f), new Vector3(0.035f, 0.56f, 0.035f),
                    Quaternion.Euler(sz * 6f, 0f, -sx * 6f)));
        }

        parts.Add(Box(wood, -0.18f, 0.54f, -0.16f, 0.18f, 0.58f, 0.16f));
        parts.Add(Box(wood, -0.18f, 0.58f, -0.18f, 0.18f, 1.00f, -0.15f));
        parts.Add(Box(wood, -0.18f, 0.58f, -0.16f, -0.15f, 0.75f, 0.12f));
        parts.Add(Box(wood, 0.15f, 0.58f, -0.16f, 0.18f, 0.75f, 0.12f));
        parts.Add(Box(tray, -0.22f, 0.75f, 0.06f, 0.22f, 0.77f, 0.27f));
        Save("HighChair", "high_chair", parts);
    }
}
