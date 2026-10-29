using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the restaurant kitchen props (P40.14, owner-approved list Q-20261028-9): StockPot,
/// HangingPans, MagneticKnifeBar and TicketRail (wall-hung, back face at Z = 0), KitchenBinLarge,
/// VegetableCrates, OilCans and StackedTrays. The chopping board and the extinguisher reuse
/// ChoppingBoard and FireExtinguisher. Counter and floor pieces have their pivot on the bottom face
/// at the centre, front +Z. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Restaurant Kitchen Props
/// </summary>
public static class GenerateRestaurantKitchenProps
{
    [MenuItem("Tools/Procedural Cities/Generate Restaurant Kitchen Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateRestaurantKitchenProps] Missing LP_Chrome.");
            return;
        }

        var steel = LowPolyBoxMesh.GetMat("LP_RK_Steel", new Color(0.72f, 0.74f, 0.76f), 0.8f, 0.6f);
        var black = LowPolyBoxMesh.GetMat("LP_RK_Black", new Color(0.11f, 0.11f, 0.12f), 0.2f, 0.5f);
        var wood = LowPolyBoxMesh.GetMat("LP_RK_Wood", new Color(0.62f, 0.45f, 0.28f), 0f, 0.3f);

        BuildStockPot(steel, black);
        BuildHangingPans(steel, black, LowPolyBoxMesh.GetMat("LP_RK_Copper", new Color(0.72f, 0.42f, 0.25f), 0.8f, 0.5f));
        BuildKnifeBar(black, steel, wood);
        BuildTicketRail(steel, LowPolyBoxMesh.GetMat("LP_RK_Ticket", new Color(0.97f, 0.97f, 0.93f), 0f, 0.2f));
        BuildKitchenBin(LowPolyBoxMesh.GetMat("LP_RK_Bin", new Color(0.35f, 0.38f, 0.40f), 0.2f, 0.4f), black);
        BuildVegetableCrates(wood, new[]
        {
            LowPolyBoxMesh.GetMat("LP_RK_Tomato", new Color(0.80f, 0.15f, 0.10f), 0f, 0.5f),
            LowPolyBoxMesh.GetMat("LP_RK_Lettuce", new Color(0.40f, 0.65f, 0.25f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_RK_Potato", new Color(0.70f, 0.55f, 0.35f), 0f, 0.2f),
        });
        BuildOilCans(LowPolyBoxMesh.GetMat("LP_RK_Tin", new Color(0.82f, 0.78f, 0.45f), 0.7f, 0.5f), black,
            LowPolyBoxMesh.GetMat("LP_RK_OilLabel", new Color(0.25f, 0.45f, 0.20f), 0f, 0.3f));
        BuildStackedTrays(steel);
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

    private static void BuildStockPot(Material steel, Material black)
    {
        // A 0.26 m stock pot with a lid and two side handles.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, steel, 0f, 0f, 0f, 0.26f, 0.24f);
        Round(parts, steel, 0f, 0.24f, 0f, 0.27f, 0.255f);
        parts.Add(Box(black, -0.025f, 0.255f, -0.025f, 0.025f, 0.28f, 0.025f));
        parts.Add(Box(black, -0.16f, 0.17f, -0.02f, -0.13f, 0.20f, 0.02f));
        parts.Add(Box(black, 0.13f, 0.17f, -0.02f, 0.16f, 0.20f, 0.02f));
        Save("StockPot", "stock_pot", parts);
    }

    private static void BuildHangingPans(Material rail, Material black, Material copper)
    {
        // Wall-hung rail, 0.90 m, three pans hanging from hooks, their bottoms to the wall.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(rail, -0.45f, 0.58f, 0f, -0.43f, 0.62f, 0.05f),
            Box(rail, 0.43f, 0.58f, 0f, 0.45f, 0.62f, 0.05f),
            Box(rail, -0.45f, 0.585f, 0.04f, 0.45f, 0.605f, 0.06f),
        };
        var sizes = new[] { 0.26f, 0.22f, 0.30f };
        var xs = new[] { -0.28f, 0.02f, 0.28f };
        for (var i = 0; i < 3; i++)
        {
            var r = sizes[i] * 0.5f;
            var mat = i == 1 ? copper : black;
            parts.Add(Box(rail, xs[i] - 0.005f, 0.52f, 0.045f, xs[i] + 0.005f, 0.59f, 0.055f));
            parts.Add(Box(mat, xs[i] - 0.015f, 0.52f - 0.18f, 0.04f, xs[i] + 0.015f, 0.52f, 0.06f));
            var centre = new Vector3(xs[i], 0.52f - 0.18f - r, 0.05f);
            for (var k = 0; k < 6; k++)
                parts.Add(new LowPolyBoxMesh.Part(mat, centre, new Vector3(sizes[i], sizes[i] * Mathf.Tan(15f * Mathf.Deg2Rad), 0.05f),
                    Quaternion.Euler(0f, 0f, k * 30f)));
        }

        Save("HangingPans", "hanging_pans", parts);
    }

    private static void BuildKnifeBar(Material bar, Material blade, Material handle)
    {
        // Wall-hung magnetic strip, 0.50 m, five knives blade up.
        var parts = new List<LowPolyBoxMesh.Part> { Box(bar, -0.25f, 0.10f, 0f, 0.25f, 0.15f, 0.02f) };
        var lengths = new[] { 0.22f, 0.16f, 0.26f, 0.12f, 0.20f };
        for (var i = 0; i < 5; i++)
        {
            var x = -0.18f + i * 0.09f;
            parts.Add(Box(handle, x - 0.012f, 0f, 0.02f, x + 0.012f, 0.11f, 0.035f));
            parts.Add(Box(blade, x - 0.018f, 0.11f, 0.02f, x + 0.018f, 0.11f + lengths[i], 0.024f));
        }

        Save("MagneticKnifeBar", "knife_bar", parts);
    }

    private static void BuildTicketRail(Material rail, Material ticket)
    {
        // Wall-hung order rail, 0.80 m, with tickets clipped along it.
        var parts = new List<LowPolyBoxMesh.Part> { Box(rail, -0.40f, 0.12f, 0f, 0.40f, 0.16f, 0.05f) };
        for (var i = 0; i < 6; i++)
        {
            var x = -0.32f + i * 0.12f + (i % 2) * 0.02f;
            parts.Add(new LowPolyBoxMesh.Part(ticket, new Vector3(x, 0.06f, 0.052f), new Vector3(0.08f, 0.13f, 0.003f), Quaternion.Euler(0f, 0f, (i % 3 - 1) * 4f)));
        }

        Save("TicketRail", "ticket_rail", parts);
    }

    private static void BuildKitchenBin(Material body, Material lid)
    {
        // A 0.50 x 0.80 x 0.50 m bin on castors, its lid on top.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.25f, 0.05f, -0.25f, 0.25f, 0.76f, 0.25f),
            Box(lid, -0.26f, 0.76f, -0.26f, 0.26f, 0.80f, 0.26f),
            Box(lid, -0.08f, 0.80f, 0.15f, 0.08f, 0.82f, 0.20f),
        };
        foreach (var sx in new[] { -0.19f, 0.19f })
        {
            foreach (var sz in new[] { -0.19f, 0.19f })
                parts.Add(Box(lid, sx - 0.03f, 0f, sz - 0.03f, sx + 0.03f, 0.05f, sz + 0.03f));
        }

        Save("KitchenBinLarge", "kitchen_bin_large", parts);
    }

    private static void BuildVegetableCrates(Material wood, Material[] produce)
    {
        // Two slatted crates, 0.50 x 0.25 x 0.35 m, stacked a little askew, full of produce.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var c = 0; c < 2; c++)
        {
            var rotation = Quaternion.Euler(0f, c * 6f, 0f);
            var y = c * 0.26f;
            parts.Add(new LowPolyBoxMesh.Part(wood, new Vector3(0f, y + 0.01f, 0f), new Vector3(0.50f, 0.02f, 0.35f), rotation));
            foreach (var h in new[] { 0.06f, 0.18f })
            {
                parts.Add(new LowPolyBoxMesh.Part(wood, rotation * new Vector3(0f, 0f, 0.165f) + new Vector3(0f, y + h, 0f), new Vector3(0.50f, 0.06f, 0.02f), rotation));
                parts.Add(new LowPolyBoxMesh.Part(wood, rotation * new Vector3(0f, 0f, -0.165f) + new Vector3(0f, y + h, 0f), new Vector3(0.50f, 0.06f, 0.02f), rotation));
            }

            parts.Add(new LowPolyBoxMesh.Part(wood, rotation * new Vector3(-0.24f, 0f, 0f) + new Vector3(0f, y + 0.125f, 0f), new Vector3(0.02f, 0.25f, 0.35f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(wood, rotation * new Vector3(0.24f, 0f, 0f) + new Vector3(0f, y + 0.125f, 0f), new Vector3(0.02f, 0.25f, 0.35f), rotation));
            for (var i = 0; i < 8; i++)
            {
                var px = -0.18f + (i % 4) * 0.12f;
                var pz = i < 4 ? -0.07f : 0.07f;
                parts.Add(new LowPolyBoxMesh.Part(produce[(i + c) % produce.Length], rotation * new Vector3(px, 0f, pz) + new Vector3(0f, y + 0.19f, 0f),
                    new Vector3(0.09f, 0.08f, 0.09f), Quaternion.Euler(10f * i, 25f * i, 0f)));
            }
        }

        Save("VegetableCrates", "vegetable_crates", parts);
    }

    private static void BuildOilCans(Material tin, Material cap, Material label)
    {
        // Two 20-litre square tins side by side, 0.24 x 0.36 x 0.24 m each.
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var x in new[] { -0.13f, 0.13f })
        {
            parts.Add(Box(tin, x - 0.12f, 0f, -0.12f, x + 0.12f, 0.36f, 0.12f));
            parts.Add(Box(label, x - 0.10f, 0.10f, 0.12f, x + 0.10f, 0.26f, 0.122f));
            parts.Add(Box(cap, x + 0.04f, 0.36f, 0.04f, x + 0.08f, 0.38f, 0.08f));
            parts.Add(Box(cap, x - 0.08f, 0.36f, -0.01f, x + 0.02f, 0.39f, 0.01f));
        }

        Save("OilCans", "oil_cans", parts);
    }

    private static void BuildStackedTrays(Material steel)
    {
        // Five gastronorm trays, 0.53 x 0.33 m, stacked.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 5; i++)
        {
            var y = i * 0.025f;
            parts.Add(Box(steel, -0.265f, y, -0.165f, 0.265f, y + 0.005f, 0.165f));
            parts.Add(Box(steel, -0.265f, y, -0.165f, 0.265f, y + 0.024f, -0.155f));
            parts.Add(Box(steel, -0.265f, y, 0.155f, 0.265f, y + 0.024f, 0.165f));
            parts.Add(Box(steel, -0.265f, y, -0.155f, -0.255f, y + 0.024f, 0.155f));
            parts.Add(Box(steel, 0.255f, y, -0.155f, 0.265f, y + 0.024f, 0.155f));
        }

        Save("StackedTrays", "stacked_trays", parts);
    }
}
