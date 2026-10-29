using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the office props (P40.11, owner-approved list Q-20261028-9): PaperTray (three stacked
/// letter trays), DeskPhone, Photocopier, RingBinders (a row of four) and LaptopClosed. The
/// printer, mug, headphones, cork board and desk lamp reuse the study's and the desk set's
/// models. Every piece has its pivot on the bottom face at the centre, front +Z. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Office Props
/// </summary>
public static class GenerateOfficeProps
{
    [MenuItem("Tools/Procedural Cities/Generate Office Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateOfficeProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_OF_Black", new Color(0.11f, 0.11f, 0.12f), 0.1f, 0.5f);
        var grey = LowPolyBoxMesh.GetMat("LP_OF_Grey", new Color(0.72f, 0.73f, 0.75f), 0.1f, 0.5f);
        var paper = LowPolyBoxMesh.GetMat("LP_OF_Paper", new Color(0.97f, 0.97f, 0.95f), 0f, 0.2f);

        BuildPaperTray(black, paper);
        BuildDeskPhone(black, grey, LowPolyBoxMesh.GetMat("LP_OF_Screen", new Color(0.35f, 0.55f, 0.65f), 0f, 0.8f));
        BuildPhotocopier(grey, LowPolyBoxMesh.GetMat("LP_OF_Light", new Color(0.88f, 0.88f, 0.86f), 0f, 0.4f), black, paper);
        BuildRingBinders(new[]
        {
            LowPolyBoxMesh.GetMat("LP_OF_BinderBlue", new Color(0.15f, 0.30f, 0.60f), 0f, 0.4f),
            LowPolyBoxMesh.GetMat("LP_OF_BinderRed", new Color(0.65f, 0.15f, 0.15f), 0f, 0.4f),
            black,
            LowPolyBoxMesh.GetMat("LP_OF_BinderGreen", new Color(0.20f, 0.45f, 0.25f), 0f, 0.4f),
        }, paper);
        BuildLaptopClosed(LowPolyBoxMesh.GetMat("LP_OF_Laptop", new Color(0.55f, 0.57f, 0.60f), 0.6f, 0.6f), black);
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

    private static void BuildPaperTray(Material tray, Material paper)
    {
        // Three letter trays, 0.26 x 0.20 x 0.34 m, open to the front, papers in them.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            var y = i * 0.065f;
            parts.Add(Box(tray, -0.13f, y, -0.17f, 0.13f, y + 0.008f, 0.17f));
            parts.Add(Box(tray, -0.13f, y, -0.17f, -0.122f, y + 0.06f, 0.17f));
            parts.Add(Box(tray, 0.122f, y, -0.17f, 0.13f, y + 0.06f, 0.17f));
            parts.Add(Box(tray, -0.13f, y, -0.17f, 0.13f, y + 0.06f, -0.162f));
            parts.Add(Box(paper, -0.105f, y + 0.008f, -0.15f, 0.105f, y + 0.008f + 0.01f * (3 - i), 0.15f));
        }

        Save("PaperTray", "paper_tray", parts);
    }

    private static void BuildDeskPhone(Material body, Material keys, Material screen)
    {
        // 0.20 x 0.09 x 0.20 m, the handset on the left, a sloped keypad and a small screen.
        Save("DeskPhone", "desk_phone", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.10f, 0f, -0.10f, 0.10f, 0.04f, 0.10f),
            new(body, new Vector3(0.02f, 0.055f, -0.01f), new Vector3(0.14f, 0.03f, 0.16f), Quaternion.Euler(15f, 0f, 0f)),
            new(screen, new Vector3(0.03f, 0.075f, -0.05f), new Vector3(0.09f, 0.004f, 0.04f), Quaternion.Euler(15f, 0f, 0f)),
            new(keys, new Vector3(0.03f, 0.065f, 0.025f), new Vector3(0.08f, 0.004f, 0.07f), Quaternion.Euler(15f, 0f, 0f)),
            Box(body, -0.10f, 0.04f, -0.09f, -0.055f, 0.07f, 0.09f),
            Box(body, -0.095f, 0.07f, -0.09f, -0.06f, 0.09f, -0.055f),
            Box(body, -0.095f, 0.07f, 0.055f, -0.06f, 0.09f, 0.09f),
        });
    }

    private static void BuildPhotocopier(Material body, Material light, Material dark, Material paper)
    {
        // 0.70 x 1.15 x 0.62 m, back at -Z: a cabinet with paper drawers, the copier on it,
        // the lid and its control panel on top, an output tray to the right.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.35f, 0.05f, -0.31f, 0.35f, 0.70f, 0.31f),
            Box(dark, -0.33f, 0f, -0.29f, 0.33f, 0.05f, 0.29f),
            Box(light, -0.35f, 0.70f, -0.31f, 0.35f, 1.05f, 0.31f),
            Box(dark, -0.34f, 1.05f, -0.30f, 0.34f, 1.08f, 0.30f),
            Box(light, -0.34f, 1.08f, -0.30f, 0.34f, 1.12f, 0.20f),
            Box(dark, 0.05f, 1.06f, 0.20f, 0.33f, 1.15f, 0.31f),
            Box(dark, 0.35f, 0.80f, -0.20f, 0.52f, 0.81f, 0.20f),
            Box(paper, 0.36f, 0.81f, -0.15f, 0.50f, 0.83f, 0.15f),
        };
        for (var i = 0; i < 3; i++)
        {
            var y = 0.10f + i * 0.20f;
            parts.Add(Box(light, -0.33f, y, 0.31f, 0.33f, y + 0.17f, 0.315f));
            parts.Add(Box(dark, -0.10f, y + 0.12f, 0.315f, 0.10f, y + 0.14f, 0.33f));
        }

        Save("Photocopier", "photocopier", parts);
    }

    private static void BuildRingBinders(Material[] covers, Material label)
    {
        // Four binders standing in a row, spines to the front (+Z): 0.30 x 0.32 x 0.28 m.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 4; i++)
        {
            var x = -0.12f + i * 0.08f;
            parts.Add(Box(covers[i], x - 0.035f, 0f, -0.14f, x + 0.035f, 0.32f, 0.14f));
            parts.Add(Box(label, x - 0.02f, 0.18f, 0.14f, x + 0.02f, 0.28f, 0.142f));
            parts.Add(Box(label, x - 0.012f, 0.05f, 0.14f, x + 0.012f, 0.08f, 0.142f));
        }

        Save("RingBinders", "ring_binders", parts);
    }

    private static void BuildLaptopClosed(Material shell, Material dark)
    {
        Save("LaptopClosed", "laptop_closed", new List<LowPolyBoxMesh.Part>
        {
            Box(shell, -0.17f, 0f, -0.12f, 0.17f, 0.012f, 0.12f),
            Box(dark, -0.168f, 0.012f, -0.118f, 0.168f, 0.013f, 0.118f),
            Box(shell, -0.17f, 0.013f, -0.12f, 0.17f, 0.022f, 0.12f),
            Box(dark, -0.03f, 0.022f, -0.01f, 0.03f, 0.0225f, 0.03f),
        });
    }
}
