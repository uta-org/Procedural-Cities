using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the study props (P40.7, owner-approved list Q-20261028-9), shared with the offices
/// (P40.11): Globe, PenPot, WasteBasket, CorkBoard (wall-hung), Printer, Mug and
/// Headphones, and FilingCabinetSmall (two drawers, 0.66 m: FilingCabinet is the office's 1.32 m
/// one). The stacked books reuse the living room's StackedBooks (P40.2), the bookcase Shelf. Desk
/// and floor pieces have their pivot on the bottom face at the centre, front +Z; CorkBoard has
/// its back face at Z = 0, like Mirror. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Study Props
/// </summary>
public static class GenerateStudyProps
{
    [MenuItem("Tools/Procedural Cities/Generate Study Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateStudyProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_ST_Black", new Color(0.10f, 0.10f, 0.11f), 0.1f, 0.5f);
        var grey = LowPolyBoxMesh.GetMat("LP_ST_Grey", new Color(0.78f, 0.79f, 0.80f), 0f, 0.5f);
        var paper = LowPolyBoxMesh.GetMat("LP_ST_Paper", new Color(0.97f, 0.97f, 0.95f), 0f, 0.2f);
        var books = new[]
        {
            LowPolyBoxMesh.GetMat("LP_ST_BookRed", new Color(0.62f, 0.16f, 0.14f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_ST_BookBlue", new Color(0.16f, 0.28f, 0.52f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_ST_BookGreen", new Color(0.20f, 0.40f, 0.25f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_ST_BookOchre", new Color(0.78f, 0.60f, 0.22f), 0f, 0.3f),
        };

        BuildGlobe(LowPolyBoxMesh.GetMat("LP_ST_Ocean", new Color(0.20f, 0.45f, 0.70f), 0f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_ST_Land", new Color(0.45f, 0.62f, 0.30f), 0f, 0.4f),
            LowPolyBoxMesh.GetMat("LP_ST_Wood", new Color(0.45f, 0.30f, 0.18f), 0f, 0.4f), chrome);
        BuildPenPot(black, books);
        BuildWasteBasket(LowPolyBoxMesh.GetMat("LP_ST_Basket", new Color(0.35f, 0.36f, 0.38f), 0.5f, 0.4f), paper);
        BuildCorkBoard(LowPolyBoxMesh.GetMat("LP_ST_Cork", new Color(0.70f, 0.53f, 0.35f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_ST_Frame", new Color(0.55f, 0.40f, 0.25f), 0f, 0.4f), paper,
            LowPolyBoxMesh.GetMat("LP_ST_NoteYellow", new Color(0.98f, 0.90f, 0.40f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_ST_NotePink", new Color(0.95f, 0.60f, 0.70f), 0f, 0.2f), books[0]);
        BuildPrinter(grey, black, paper);
        BuildMug(LowPolyBoxMesh.GetMat("LP_ST_Mug", new Color(0.92f, 0.92f, 0.90f), 0f, 0.7f),
            LowPolyBoxMesh.GetMat("LP_ST_Coffee", new Color(0.25f, 0.14f, 0.07f), 0f, 0.7f));
        BuildHeadphones(black, grey);
        BuildFilingCabinetSmall(LowPolyBoxMesh.GetMat("LP_ST_Cabinet", new Color(0.55f, 0.57f, 0.60f), 0.5f, 0.5f), chrome, paper);
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

    private static void BuildGlobe(Material ocean, Material land, Material wood, Material metal)
    {
        // A 0.24 m sphere (three 12-sided slices) on a tilted half ring and a round base.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, wood, 0f, 0f, 0f, 0.15f, 0.025f);
        parts.Add(Box(wood, -0.012f, 0.025f, -0.012f, 0.012f, 0.10f, 0.012f));
        const float cy = 0.23f;
        Round(parts, ocean, 0f, cy - 0.12f, 0f, 0.14f, cy - 0.08f);
        Round(parts, ocean, 0f, cy - 0.08f, 0f, 0.22f, cy + 0.08f);
        Round(parts, ocean, 0f, cy + 0.08f, 0f, 0.14f, cy + 0.12f);
        parts.Add(new LowPolyBoxMesh.Part(land, new Vector3(0.06f, cy + 0.03f, 0.09f), new Vector3(0.09f, 0.08f, 0.012f), Quaternion.Euler(0f, 35f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(land, new Vector3(-0.08f, cy - 0.02f, 0.07f), new Vector3(0.07f, 0.10f, 0.012f), Quaternion.Euler(0f, -45f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(land, new Vector3(-0.02f, cy + 0.05f, -0.105f), new Vector3(0.10f, 0.06f, 0.012f), Quaternion.Euler(0f, 5f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(metal, new Vector3(0f, cy, 0f), new Vector3(0.012f, 0.27f, 0.012f), Quaternion.Euler(0f, 0f, 23f)));
        parts.Add(new LowPolyBoxMesh.Part(metal, new Vector3(0.05f, cy - 0.10f, 0f), new Vector3(0.012f, 0.06f, 0.012f), Quaternion.Euler(0f, 0f, 60f)));
        Save("Globe", "globe", parts);
    }

    private static void BuildPenPot(Material pot, Material[] pens)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, pot, 0f, 0f, 0f, 0.08f, 0.10f);
        for (var i = 0; i < 4; i++)
        {
            var angle = i * 90f + 20f;
            var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.018f, 0f, 0f);
            parts.Add(new LowPolyBoxMesh.Part(pens[i % pens.Length], new Vector3(offset.x, 0.10f, offset.z),
                new Vector3(0.009f, 0.14f, 0.009f), Quaternion.Euler(offset.z * 300f, 0f, -offset.x * 300f)));
        }

        Save("PenPot", "pen_pot", parts);
    }

    private static void BuildWasteBasket(Material mesh, Material paper)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, mesh, 0f, 0f, 0f, 0.24f, 0.012f);
        for (var i = 0; i < 6; i++)
        {
            var rotation = Quaternion.Euler(0f, i * 30f, 0f);
            foreach (var side in new[] { -1f, 1f })
                parts.Add(new LowPolyBoxMesh.Part(mesh, rotation * new Vector3(side * 0.13f, 0.16f, 0f), new Vector3(0.008f, 0.32f, 0.07f), rotation));
        }

        parts.Add(new LowPolyBoxMesh.Part(paper, new Vector3(0.02f, 0.27f, 0.01f), new Vector3(0.08f, 0.07f, 0.07f), Quaternion.Euler(20f, 30f, 15f)));
        Save("WasteBasket", "waste_basket", parts);
    }

    private static void BuildCorkBoard(Material cork, Material frame, Material paper, Material yellow, Material pink, Material pin)
    {
        // Back on the wall at Z = 0: 0.90 x 0.60 m.
        const float w = 0.90f, h = 0.60f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(cork, -w * 0.5f, 0f, 0f, w * 0.5f, h, 0.015f),
            Box(frame, -w * 0.5f, 0f, 0f, w * 0.5f, 0.025f, 0.022f),
            Box(frame, -w * 0.5f, h - 0.025f, 0f, w * 0.5f, h, 0.022f),
            Box(frame, -w * 0.5f, 0f, 0f, -w * 0.5f + 0.025f, h, 0.022f),
            Box(frame, w * 0.5f - 0.025f, 0f, 0f, w * 0.5f, h, 0.022f),
        };
        var notes = new[]
        {
            (paper, -0.28f, 0.36f, 0.21f, 0.28f, -4f), (yellow, -0.02f, 0.42f, 0.10f, 0.10f, 6f), (pink, 0.12f, 0.40f, 0.10f, 0.10f, -8f),
            (paper, 0.25f, 0.24f, 0.18f, 0.13f, 3f), (yellow, -0.05f, 0.18f, 0.10f, 0.10f, -3f),
        };
        foreach (var (mat, x, y, nw, nh, tilt) in notes)
        {
            parts.Add(new LowPolyBoxMesh.Part(mat, new Vector3(x, y, 0.017f), new Vector3(nw, nh, 0.002f), Quaternion.Euler(0f, 0f, tilt)));
            parts.Add(Box(pin, x - 0.008f, y + nh * 0.5f - 0.025f, 0.018f, x + 0.008f, y + nh * 0.5f - 0.009f, 0.028f));
        }

        Save("CorkBoard", "cork_board", parts);
    }

    private static void BuildPrinter(Material body, Material dark, Material paper)
    {
        // 0.42 x 0.20 x 0.34 m, the output tray to the front.
        Save("Printer", "printer", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.21f, 0f, -0.17f, 0.21f, 0.17f, 0.15f),
            Box(dark, -0.21f, 0.17f, -0.17f, 0.21f, 0.20f, 0.02f),
            Box(dark, -0.16f, 0.06f, 0.15f, 0.16f, 0.07f, 0.24f),
            Box(paper, -0.105f, 0.07f, 0.12f, 0.105f, 0.075f, 0.23f),
            Box(paper, -0.105f, 0.20f, -0.16f, 0.105f, 0.27f, -0.155f),
            Box(dark, 0.12f, 0.17f, 0.04f, 0.19f, 0.175f, 0.13f),
            Box(dark, -0.18f, 0.02f, 0.15f, 0.18f, 0.05f, 0.152f),
        });
    }

    private static void BuildMug(Material mug, Material coffee)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, mug, 0f, 0f, 0f, 0.08f, 0.095f);
        Round(parts, coffee, 0f, 0.095f, 0f, 0.07f, 0.097f);
        parts.Add(Box(mug, 0.04f, 0.02f, -0.008f, 0.065f, 0.03f, 0.008f));
        parts.Add(Box(mug, 0.04f, 0.07f, -0.008f, 0.065f, 0.08f, 0.008f));
        parts.Add(Box(mug, 0.055f, 0.02f, -0.008f, 0.065f, 0.08f, 0.008f));
        Save("Mug", "mug", parts);
    }

    private static void BuildFilingCabinetSmall(Material body, Material handle, Material label)
    {
        // 0.42 x 0.66 x 0.55 m, back at -Z, two drawers; its top takes a printer.
        const float w = 0.42f, h = 0.66f, d = 0.55f;
        var parts = new List<LowPolyBoxMesh.Part> { Box(body, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f - 0.01f) };
        for (var i = 0; i < 2; i++)
        {
            var y0 = 0.03f + i * 0.315f;
            var y1 = y0 + 0.30f;
            parts.Add(Box(body, -w * 0.5f + 0.015f, y0, d * 0.5f - 0.01f, w * 0.5f - 0.015f, y1, d * 0.5f));
            parts.Add(Box(handle, -0.08f, y1 - 0.08f, d * 0.5f, 0.08f, y1 - 0.06f, d * 0.5f + 0.025f));
            parts.Add(Box(label, -0.05f, y1 - 0.05f, d * 0.5f, 0.05f, y1 - 0.02f, d * 0.5f + 0.003f));
        }

        Save("FilingCabinetSmall", "filing_cabinet_small", parts);
    }

    private static void BuildHeadphones(Material black, Material grey)
    {
        // Lying on the desk: two ear cups joined by the band, laid flat.
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var side in new[] { -1f, 1f })
        {
            Round(parts, black, side * 0.075f, 0f, 0.03f, 0.08f, 0.035f);
            Round(parts, grey, side * 0.075f, 0.035f, 0.03f, 0.06f, 0.04f);
        }

        parts.Add(Box(black, -0.08f, 0.005f, -0.07f, 0.08f, 0.02f, -0.05f));
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(-0.09f, 0.012f, -0.02f), new Vector3(0.02f, 0.015f, 0.08f), Quaternion.Euler(0f, -25f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(0.09f, 0.012f, -0.02f), new Vector3(0.02f, 0.015f, 0.08f), Quaternion.Euler(0f, 25f, 0f)));
        Save("Headphones", "headphones", parts);
    }
}
