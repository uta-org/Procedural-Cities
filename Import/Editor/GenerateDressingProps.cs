using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the dressing room props (P40.8, owner-approved list Q-20261028-9), shared with the
/// closets (P40.9): ShoeBoxes (a stack of three), ClothesRail (a rail on two feet with clothes on
/// hangers), ShoeRack (two shelves of shoes), Suitcase (upright, a handle on top) and HatBox. The
/// pouf, floor mirror, jewellery box, basket and rug reuse Pouf, FloorMirror, JewelleryBox,
/// LaundryBasket and BedsideRug. Every piece has its pivot on the bottom face at the centre,
/// front +Z. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Dressing Props
/// </summary>
public static class GenerateDressingProps
{
    [MenuItem("Tools/Procedural Cities/Generate Dressing Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateDressingProps] Missing LP_Chrome.");
            return;
        }

        var cardboard = LowPolyBoxMesh.GetMat("LP_DR_Box", new Color(0.82f, 0.74f, 0.62f), 0f, 0.2f);
        var lid = LowPolyBoxMesh.GetMat("LP_DR_BoxLid", new Color(0.92f, 0.90f, 0.86f), 0f, 0.2f);
        var black = LowPolyBoxMesh.GetMat("LP_DR_Black", new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.4f);
        var clothes = new[]
        {
            LowPolyBoxMesh.GetMat("LP_DR_ClothNavy", new Color(0.16f, 0.22f, 0.38f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_DR_ClothCream", new Color(0.90f, 0.86f, 0.76f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_DR_ClothRed", new Color(0.65f, 0.18f, 0.18f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_DR_ClothGrey", new Color(0.45f, 0.46f, 0.48f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_DR_ClothGreen", new Color(0.28f, 0.42f, 0.30f), 0f, 0.05f),
        };

        BuildShoeBoxes(cardboard, lid, black);
        BuildClothesRail(chrome, LowPolyBoxMesh.GetMat("LP_DR_Hanger", new Color(0.55f, 0.40f, 0.25f), 0f, 0.4f), clothes);
        BuildShoeRack(LowPolyBoxMesh.GetMat("LP_DR_Rack", new Color(0.50f, 0.36f, 0.22f), 0f, 0.35f), black, clothes);
        BuildSuitcase(LowPolyBoxMesh.GetMat("LP_DR_Suitcase", new Color(0.18f, 0.35f, 0.55f), 0.2f, 0.6f), black, chrome);
        BuildHatBox(LowPolyBoxMesh.GetMat("LP_DR_HatBox", new Color(0.85f, 0.62f, 0.66f), 0f, 0.3f), lid);
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

    private static void BuildShoeBoxes(Material box, Material lid, Material label)
    {
        // Three boxes 0.34 x 0.12 x 0.22 m, stacked a little askew.
        var parts = new List<LowPolyBoxMesh.Part>();
        var turn = new[] { 0f, 4f, -3f };
        for (var i = 0; i < 3; i++)
        {
            var rotation = Quaternion.Euler(0f, turn[i], 0f);
            var y = i * 0.12f;
            parts.Add(new LowPolyBoxMesh.Part(box, new Vector3(0f, y + 0.05f, 0f), new Vector3(0.33f, 0.10f, 0.21f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(lid, new Vector3(0f, y + 0.105f, 0f), new Vector3(0.34f, 0.03f, 0.22f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(label, rotation * new Vector3(0f, 0f, 0.106f) + new Vector3(0f, y + 0.06f, 0f),
                new Vector3(0.08f, 0.04f, 0.004f), rotation));
        }

        Save("ShoeBoxes", "shoe_boxes", parts);
    }

    private static void BuildClothesRail(Material metal, Material hanger, Material[] clothes)
    {
        // 1.10 m long, 1.55 m high, its feet 0.45 m deep: clothes on hangers along X.
        const float w = 1.10f, h = 1.55f, d = 0.45f;
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var sx in new[] { -1f, 1f })
        {
            parts.Add(Box(metal, sx * w * 0.5f - 0.015f, 0.03f, -0.015f, sx * w * 0.5f + 0.015f, h, 0.015f));
            parts.Add(Box(metal, sx * w * 0.5f - 0.02f, 0f, -d * 0.5f, sx * w * 0.5f + 0.02f, 0.03f, d * 0.5f));
        }

        parts.Add(Box(metal, -w * 0.5f, h - 0.03f, -0.012f, w * 0.5f, h, 0.012f));
        for (var i = 0; i < 8; i++)
        {
            var x = -0.42f + i * 0.12f;
            var length = i % 3 == 0 ? 0.95f : i % 3 == 1 ? 0.70f : 0.80f;
            parts.Add(Box(hanger, x - 0.01f, h - 0.09f, -0.20f, x + 0.01f, h - 0.07f, 0.20f));
            parts.Add(Box(clothes[i % clothes.Length], x - 0.03f, h - 0.08f - length, -0.22f, x + 0.03f, h - 0.08f, 0.22f));
        }

        Save("ClothesRail", "clothes_rail", parts);
    }

    private static void BuildShoeRack(Material wood, Material black, Material[] shoes)
    {
        // 0.80 x 0.50 x 0.30 m, back at -Z: two shelves of shoes on a frame.
        const float w = 0.80f, h = 0.50f, d = 0.30f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -w * 0.5f, 0f, -d * 0.5f, -w * 0.5f + 0.025f, h, d * 0.5f),
            Box(wood, w * 0.5f - 0.025f, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            Box(wood, -w * 0.5f, h - 0.02f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
        };
        for (var s = 0; s < 2; s++)
        {
            var y = 0.04f + s * 0.22f;
            parts.Add(Box(wood, -w * 0.5f + 0.025f, y - 0.015f, -d * 0.5f, w * 0.5f - 0.025f, y, d * 0.5f));
            for (var i = 0; i < 3; i++)
            {
                var x = -0.24f + i * 0.24f;
                var mat = (i + s) % 2 == 0 ? black : shoes[(i + s * 3) % shoes.Length];
                foreach (var side in new[] { -0.045f, 0.045f })
                {
                    parts.Add(Box(mat, x + side - 0.035f, y, -0.12f, x + side + 0.035f, y + 0.05f, 0.13f));
                    parts.Add(Box(mat, x + side - 0.035f, y + 0.05f, -0.12f, x + side + 0.035f, y + 0.10f, -0.03f));
                }
            }
        }

        Save("ShoeRack", "shoe_rack", parts);
    }

    private static void BuildSuitcase(Material shell, Material black, Material metal)
    {
        // Upright, 0.45 x 0.62 x 0.25 m on four wheels, its pull handle on top.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(shell, -0.225f, 0.04f, -0.125f, 0.225f, 0.66f, 0.125f),
            Box(black, -0.228f, 0.33f, -0.128f, 0.228f, 0.35f, 0.128f),
            Box(black, -0.20f, 0.66f, -0.02f, -0.17f, 0.70f, 0.02f),
            Box(black, 0.17f, 0.66f, -0.02f, 0.20f, 0.70f, 0.02f),
            Box(metal, -0.20f, 0.70f, -0.02f, 0.20f, 0.72f, 0.02f),
            Box(black, -0.05f, 0.60f, 0.125f, 0.05f, 0.62f, 0.15f),
        };
        foreach (var sx in new[] { -1f, 1f })
        {
            foreach (var sz in new[] { -1f, 1f })
                parts.Add(Box(black, sx * 0.18f - 0.025f, 0f, sz * 0.085f - 0.025f, sx * 0.18f + 0.025f, 0.04f, sz * 0.085f + 0.025f));
        }

        for (var i = 0; i < 3; i++)
            parts.Add(Box(shell, -0.15f + i * 0.15f - 0.01f, 0.08f, 0.125f, -0.15f + i * 0.15f + 0.01f, 0.62f, 0.135f));
        Save("Suitcase", "suitcase", parts);
    }

    private static void BuildHatBox(Material box, Material band)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, box, 0f, 0f, 0f, 0.36f, 0.20f);
        Round(parts, band, 0f, 0.20f, 0f, 0.37f, 0.24f);
        parts.Add(Box(band, -0.10f, 0.24f, -0.01f, 0.10f, 0.25f, 0.01f));
        Save("HatBox", "hat_box", parts);
    }
}
