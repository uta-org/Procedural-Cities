using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the dining room props (P40.6, owner-approved list Q-20261028-9): TableRunner,
/// PlaceSetting (plate, fork and knife), CandleCentrepiece, WineSet (bottle and two glasses),
/// NapkinHolder, Trivet, Sideboard, DisplayCabinet, DiningRug (2.40 x 1.80 m) and DiningRugSmall
/// (2.00 x 1.40 m, for a room the large one does not fit). The fruit bowl reuses
/// FruitBowl. Every piece has its pivot on the bottom face at the centre, front +Z (a place
/// setting's front faces its diner). Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Dining Props
/// </summary>
public static class GenerateDiningProps
{
    [MenuItem("Tools/Procedural Cities/Generate Dining Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateDiningProps] Missing LP_Chrome.");
            return;
        }

        var wood = LowPolyBoxMesh.GetMat("LP_DN_Wood", new Color(0.36f, 0.22f, 0.13f), 0f, 0.35f);
        var woodLight = LowPolyBoxMesh.GetMat("LP_DN_WoodLight", new Color(0.55f, 0.38f, 0.24f), 0f, 0.35f);
        var china = LowPolyBoxMesh.GetMat("LP_DN_China", new Color(0.96f, 0.96f, 0.94f), 0f, 0.7f);
        var glass = LowPolyBoxMesh.GetMat("LP_DN_Glass", new Color(0.82f, 0.90f, 0.93f), 0f, 0.95f);
        var brass = LowPolyBoxMesh.GetMat("LP_DN_Brass", new Color(0.78f, 0.62f, 0.30f), 0.8f, 0.6f);

        BuildTableRunner(LowPolyBoxMesh.GetMat("LP_DN_Runner", new Color(0.62f, 0.18f, 0.16f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_DN_RunnerBorder", new Color(0.88f, 0.78f, 0.55f), 0f, 0.05f));
        BuildPlaceSetting(china, LowPolyBoxMesh.GetMat("LP_DN_ChinaRim", new Color(0.45f, 0.58f, 0.72f), 0f, 0.7f), chrome,
            LowPolyBoxMesh.GetMat("LP_DN_Napkin", new Color(0.85f, 0.88f, 0.80f), 0f, 0.05f));
        BuildCandleCentrepiece(brass, LowPolyBoxMesh.GetMat("LP_DN_Candle", new Color(0.96f, 0.93f, 0.84f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_DN_Flame", new Color(1.00f, 0.80f, 0.30f), 0f, 0.5f));
        BuildWineSet(LowPolyBoxMesh.GetMat("LP_DN_Bottle", new Color(0.15f, 0.30f, 0.18f), 0.1f, 0.9f), glass,
            LowPolyBoxMesh.GetMat("LP_DN_Label", new Color(0.92f, 0.88f, 0.78f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_DN_Wine", new Color(0.45f, 0.05f, 0.10f), 0f, 0.8f));
        BuildNapkinHolder(woodLight, LowPolyBoxMesh.GetMat("LP_DN_Napkin", new Color(0.85f, 0.88f, 0.80f), 0f, 0.05f));
        BuildTrivet(LowPolyBoxMesh.GetMat("LP_DN_Cork", new Color(0.70f, 0.53f, 0.35f), 0f, 0.1f), chrome);
        BuildSideboard(wood, woodLight, brass);
        BuildDisplayCabinet(wood, glass, china, brass);
        var rug = LowPolyBoxMesh.GetMat("LP_DN_Rug", new Color(0.42f, 0.30f, 0.45f), 0f, 0.05f);
        var rugBorder = LowPolyBoxMesh.GetMat("LP_DN_RugBorder", new Color(0.80f, 0.70f, 0.55f), 0f, 0.05f);
        BuildDiningRug("DiningRug", 2.40f, 1.80f, rug, rugBorder);
        BuildDiningRug("DiningRugSmall", 2.00f, 1.40f, rug, rugBorder);
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

    private static void BuildTableRunner(Material cloth, Material border)
    {
        // Along X, 1.20 x 0.32 m, flat on the table top.
        Save("TableRunner", "runner", new List<LowPolyBoxMesh.Part>
        {
            Box(cloth, -0.60f, 0f, -0.16f, 0.60f, 0.003f, 0.16f),
            Box(border, -0.60f, 0.003f, -0.16f, 0.60f, 0.004f, -0.14f),
            Box(border, -0.60f, 0.003f, 0.14f, 0.60f, 0.004f, 0.16f),
            Box(border, -0.60f, 0.003f, -0.14f, -0.57f, 0.004f, 0.14f),
            Box(border, 0.57f, 0.003f, -0.14f, 0.60f, 0.004f, 0.14f),
        });
    }

    private static void BuildPlaceSetting(Material china, Material rim, Material cutlery, Material napkin)
    {
        // The diner sits on +Z, facing -Z: the fork on their left (+X), the knife on their right (-X).
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, rim, 0f, 0f, 0f, 0.26f, 0.012f);
        Round(parts, china, 0f, 0.012f, 0f, 0.20f, 0.016f);
        Round(parts, china, 0f, 0f, 0f, 0.27f, 0.008f);
        parts.Add(Box(napkin, 0.15f, 0f, -0.09f, 0.23f, 0.006f, 0.09f));
        parts.Add(Box(cutlery, 0.18f, 0.006f, -0.08f, 0.195f, 0.010f, 0.04f));
        parts.Add(Box(cutlery, 0.175f, 0.006f, 0.04f, 0.20f, 0.010f, 0.08f));
        parts.Add(Box(cutlery, -0.195f, 0f, -0.085f, -0.18f, 0.004f, 0.02f));
        parts.Add(Box(cutlery, -0.198f, 0f, 0.02f, -0.177f, 0.006f, 0.09f));
        Save("PlaceSetting", "place_setting", parts);
    }

    private static void BuildCandleCentrepiece(Material tray, Material wax, Material flame)
    {
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(tray, -0.18f, 0f, -0.06f, 0.18f, 0.012f, 0.06f),
            Box(tray, -0.18f, 0.012f, -0.06f, 0.18f, 0.025f, -0.05f),
            Box(tray, -0.18f, 0.012f, 0.05f, 0.18f, 0.025f, 0.06f),
        };
        var heights = new[] { 0.14f, 0.20f, 0.11f };
        for (var i = 0; i < 3; i++)
        {
            var x = -0.11f + i * 0.11f;
            Round(parts, wax, x, 0.012f, 0f, 0.06f, 0.012f + heights[i]);
            parts.Add(Box(flame, x - 0.006f, 0.012f + heights[i], -0.006f, x + 0.006f, 0.012f + heights[i] + 0.025f, 0.006f));
        }

        Save("CandleCentrepiece", "candles", parts);
    }

    private static void BuildWineSet(Material bottle, Material glass, Material label, Material wine)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, bottle, -0.07f, 0f, 0f, 0.075f, 0.22f);
        Round(parts, label, -0.07f, 0.07f, 0f, 0.078f, 0.15f);
        Round(parts, bottle, -0.07f, 0.22f, 0f, 0.03f, 0.31f);
        foreach (var x in new[] { 0.04f, 0.10f })
        {
            Round(parts, glass, x, 0f, 0.01f, 0.06f, 0.006f);
            parts.Add(Box(glass, x - 0.004f, 0.006f, 0.006f, x + 0.004f, 0.09f, 0.014f));
            Round(parts, glass, x, 0.09f, 0.01f, 0.065f, 0.17f);
            Round(parts, wine, x, 0.092f, 0.01f, 0.058f, 0.12f);
        }

        Save("WineSet", "wine_set", parts);
    }

    private static void BuildNapkinHolder(Material wood, Material napkin)
    {
        Save("NapkinHolder", "napkin_holder", new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.08f, 0f, -0.04f, 0.08f, 0.015f, 0.04f),
            Box(wood, -0.08f, 0.015f, -0.04f, 0.08f, 0.09f, -0.03f),
            Box(wood, -0.08f, 0.015f, 0.03f, 0.08f, 0.09f, 0.04f),
            Box(napkin, -0.075f, 0.015f, -0.028f, 0.075f, 0.13f, 0.028f),
        });
    }

    private static void BuildTrivet(Material cork, Material rim)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, rim, 0f, 0f, 0f, 0.19f, 0.008f);
        Round(parts, cork, 0f, 0.008f, 0f, 0.17f, 0.012f);
        Save("Trivet", "trivet", parts);
    }

    private static void BuildSideboard(Material wood, Material panel, Material brass)
    {
        // 1.50 x 0.80 x 0.45 m, back at -Z: four doors on short legs.
        const float w = 1.50f, h = 0.80f, d = 0.45f, leg = 0.10f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -w * 0.5f, leg, -d * 0.5f, w * 0.5f, h - 0.03f, d * 0.5f - 0.01f),
            Box(wood, -w * 0.5f - 0.01f, h - 0.03f, -d * 0.5f, w * 0.5f + 0.01f, h, d * 0.5f + 0.01f),
        };
        foreach (var sx in new[] { -1f, 1f })
        {
            foreach (var sz in new[] { -1f, 1f })
                parts.Add(Box(wood, sx * (w * 0.5f - 0.05f) - 0.025f, 0f, sz * (d * 0.5f - 0.05f) - 0.025f,
                    sx * (w * 0.5f - 0.05f) + 0.025f, leg, sz * (d * 0.5f - 0.05f) + 0.025f));
        }

        for (var i = 0; i < 4; i++)
        {
            var x0 = -w * 0.5f + 0.02f + i * (w - 0.04f) / 4f;
            var x1 = x0 + (w - 0.04f) / 4f - 0.01f;
            parts.Add(Box(panel, x0, leg + 0.03f, d * 0.5f - 0.01f, x1, h - 0.06f, d * 0.5f));
            var hx = i % 2 == 0 ? x1 - 0.04f : x0 + 0.04f;
            parts.Add(Box(brass, hx - 0.008f, h * 0.5f + 0.02f, d * 0.5f, hx + 0.008f, h * 0.5f + 0.12f, d * 0.5f + 0.02f));
        }

        Save("Sideboard", "sideboard", parts);
    }

    private static void BuildDisplayCabinet(Material wood, Material glass, Material china, Material brass)
    {
        // 0.90 x 1.90 x 0.42 m, back at -Z: a closed base, glass doors over three shelves of china.
        const float w = 0.90f, h = 1.90f, d = 0.42f, baseTop = 0.70f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, baseTop, d * 0.5f),
            Box(wood, -w * 0.5f, baseTop, -d * 0.5f, w * 0.5f, h, -d * 0.5f + 0.02f),
            Box(wood, -w * 0.5f, baseTop, -d * 0.5f, -w * 0.5f + 0.03f, h, d * 0.5f),
            Box(wood, w * 0.5f - 0.03f, baseTop, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            Box(wood, -w * 0.5f - 0.01f, h - 0.04f, -d * 0.5f, w * 0.5f + 0.01f, h, d * 0.5f + 0.01f),
            Box(glass, -w * 0.5f + 0.03f, baseTop, d * 0.5f - 0.012f, w * 0.5f - 0.03f, h - 0.04f, d * 0.5f - 0.004f),
            Box(wood, -0.012f, baseTop, d * 0.5f - 0.014f, 0.012f, h - 0.04f, d * 0.5f),
            Box(brass, -0.04f, baseTop - 0.20f, d * 0.5f, 0.04f, baseTop - 0.18f, d * 0.5f + 0.02f),
            Box(brass, -0.05f, baseTop + 0.45f, d * 0.5f, -0.035f, baseTop + 0.60f, d * 0.5f + 0.015f),
            Box(brass, 0.035f, baseTop + 0.45f, d * 0.5f, 0.05f, baseTop + 0.60f, d * 0.5f + 0.015f),
        };
        for (var s = 0; s < 3; s++)
        {
            var y = baseTop + 0.02f + s * 0.38f;
            parts.Add(Box(glass, -w * 0.5f + 0.03f, y - 0.012f, -d * 0.5f + 0.02f, w * 0.5f - 0.03f, y, d * 0.5f - 0.02f));
            // Plates on edge against the back, cups or bowls in front.
            for (var i = 0; i < 4; i++)
            {
                var x = -0.30f + i * 0.20f;
                parts.Add(new LowPolyBoxMesh.Part(china, new Vector3(x, y + 0.12f, -d * 0.5f + 0.05f),
                    new Vector3(0.22f, 0.22f, 0.015f), Quaternion.Euler(-10f, 0f, 0f)));
                if (s != 1 || i % 2 == 0)
                    Round(parts, china, x + 0.05f, y, 0.05f, 0.08f, y + (s == 2 ? 0.05f : 0.08f));
            }
        }

        Save("DisplayCabinet", "display_cabinet", parts);
    }

    private static void BuildDiningRug(string name, float length, float width, Material rug, Material border)
    {
        // Along X, flat on the floor: a 0.10 m border and an inner line 0.25 m in.
        float hx = length * 0.5f, hz = width * 0.5f;
        Save(name, "rug", new List<LowPolyBoxMesh.Part>
        {
            Box(border, -hx, 0f, -hz, hx, 0.008f, hz),
            Box(rug, -hx + 0.10f, 0.008f, -hz + 0.10f, hx - 0.10f, 0.012f, hz - 0.10f),
            Box(border, -hx + 0.25f, 0.012f, -hz + 0.25f, hx - 0.25f, 0.013f, -hz + 0.28f),
            Box(border, -hx + 0.25f, 0.012f, hz - 0.28f, hx - 0.25f, 0.013f, hz - 0.25f),
            Box(border, -hx + 0.25f, 0.012f, -hz + 0.28f, -hx + 0.28f, 0.013f, hz - 0.28f),
            Box(border, hx - 0.28f, 0.012f, -hz + 0.28f, hx - 0.25f, 0.013f, hz - 0.28f),
        });
    }
}
