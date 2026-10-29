using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the empty room (under renovation) props (P40.18, owner-approved list Q-20261028-9):
/// PaintCans, PaintRoller (in its tray), DustSheet, WorkLight (on a tripod), CementBags and Rubble.
/// The moving boxes, the painter's ladder, the pallet and the toolbox reuse CardboardBoxes,
/// Stepladder, Pallet and Toolbox. Every piece has its pivot on the bottom face at the centre,
/// front +Z. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Renovation Props
/// </summary>
public static class GenerateRenovationProps
{
    [MenuItem("Tools/Procedural Cities/Generate Renovation Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateRenovationProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_RV_Black", new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.4f);
        var white = LowPolyBoxMesh.GetMat("LP_RV_White", new Color(0.94f, 0.94f, 0.92f), 0f, 0.4f);

        BuildPaintCans(LowPolyBoxMesh.GetMat("LP_RV_Tin", new Color(0.78f, 0.79f, 0.80f), 0.7f, 0.5f), white,
            LowPolyBoxMesh.GetMat("LP_RV_PaintBlue", new Color(0.35f, 0.55f, 0.75f), 0f, 0.5f),
            LowPolyBoxMesh.GetMat("LP_RV_PaintCream", new Color(0.92f, 0.86f, 0.70f), 0f, 0.5f));
        BuildPaintRoller(black, LowPolyBoxMesh.GetMat("LP_RV_Roller", new Color(0.92f, 0.86f, 0.70f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_RV_Tray", new Color(0.40f, 0.42f, 0.45f), 0.2f, 0.4f));
        BuildDustSheet(LowPolyBoxMesh.GetMat("LP_RV_Sheet", new Color(0.86f, 0.84f, 0.78f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_RV_Splash", new Color(0.35f, 0.55f, 0.75f), 0f, 0.3f));
        BuildWorkLight(LowPolyBoxMesh.GetMat("LP_RV_Yellow", new Color(0.90f, 0.70f, 0.10f), 0.3f, 0.5f), black,
            LowPolyBoxMesh.GetMat("LP_RV_Lens", new Color(1.0f, 0.97f, 0.85f), 0f, 0.9f));
        BuildCementBags(LowPolyBoxMesh.GetMat("LP_RV_Bag", new Color(0.82f, 0.80f, 0.74f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_RV_BagPrint", new Color(0.70f, 0.20f, 0.15f), 0f, 0.1f));
        BuildRubble(new[]
        {
            LowPolyBoxMesh.GetMat("LP_RV_Brick", new Color(0.62f, 0.32f, 0.22f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_RV_Plaster", new Color(0.82f, 0.80f, 0.76f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_RV_Dust", new Color(0.55f, 0.52f, 0.48f), 0f, 0.05f),
        });
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

    private static void BuildPaintCans(Material tin, Material label, Material blue, Material cream)
    {
        // Two 15-litre tins side by side and a small one on top, drips of paint on their rims.
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var (x, paint) in new[] { (-0.15f, blue), (0.15f, cream) })
        {
            Round(parts, tin, x, 0f, 0f, 0.28f, 0.32f);
            Round(parts, label, x, 0.08f, 0f, 0.285f, 0.24f);
            Round(parts, paint, x, 0.32f, 0f, 0.26f, 0.325f);
            parts.Add(Box(paint, x + 0.11f, 0.24f, -0.02f, x + 0.142f, 0.32f, 0.02f));
        }

        Round(parts, tin, -0.12f, 0.325f, 0.02f, 0.14f, 0.48f);
        Round(parts, blue, -0.12f, 0.48f, 0.02f, 0.13f, 0.485f);
        Save("PaintCans", "paint_cans", parts);
    }

    private static void BuildPaintRoller(Material handle, Material roller, Material tray)
    {
        // A paint tray, 0.36 x 0.30 m, the roller resting across it.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(tray, -0.18f, 0f, -0.15f, 0.18f, 0.012f, 0.15f),
            Box(tray, -0.18f, 0f, -0.15f, 0.18f, 0.06f, -0.14f),
            Box(tray, -0.18f, 0f, -0.14f, -0.17f, 0.06f, 0.15f),
            Box(tray, 0.17f, 0f, -0.14f, 0.18f, 0.06f, 0.15f),
            new(roller, new Vector3(0f, 0.08f, -0.05f), new Vector3(0.23f, 0.06f, 0.06f), Quaternion.identity),
            new(roller, new Vector3(0f, 0.08f, -0.05f), new Vector3(0.23f, 0.06f, 0.06f), Quaternion.Euler(45f, 0f, 0f)),
            Box(handle, 0.115f, 0.075f, -0.06f, 0.13f, 0.09f, 0.08f),
            new(handle, new Vector3(0.12f, 0.10f, 0.20f), new Vector3(0.025f, 0.025f, 0.26f), Quaternion.Euler(-8f, 0f, 0f)),
        };
        Save("PaintRoller", "paint_roller", parts);
    }

    private static void BuildDustSheet(Material sheet, Material splash)
    {
        // A crumpled sheet, 1.60 x 1.20 m, on the floor: low folds and a few splashes.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(sheet, -0.80f, 0f, -0.60f, 0.80f, 0.008f, 0.60f),
            new(sheet, new Vector3(-0.30f, 0.02f, 0.10f), new Vector3(0.70f, 0.04f, 0.20f), Quaternion.Euler(0f, 20f, 0f)),
            new(sheet, new Vector3(0.35f, 0.015f, -0.25f), new Vector3(0.60f, 0.03f, 0.18f), Quaternion.Euler(0f, -35f, 0f)),
            new(sheet, new Vector3(0.10f, 0.03f, 0.35f), new Vector3(0.50f, 0.06f, 0.15f), Quaternion.Euler(0f, 70f, 0f)),
            Box(splash, -0.45f, 0.008f, -0.30f, -0.33f, 0.009f, -0.20f),
            Box(splash, 0.40f, 0.008f, 0.20f, 0.48f, 0.009f, 0.30f),
            Box(splash, 0.05f, 0.008f, -0.45f, 0.14f, 0.009f, -0.38f),
        };
        Save("DustSheet", "dust_sheet", parts);
    }

    private static void BuildWorkLight(Material frame, Material black, Material lens)
    {
        // A tripod 1.60 m high with two flood lights on a bar, facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            var yaw = i * 120f;
            var dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.28f);
            parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(dir.x * 0.5f, 0.35f, dir.z * 0.5f), new Vector3(0.025f, 0.75f, 0.025f),
                Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(22f, 0f, 0f)));
        }

        parts.Add(Box(black, -0.015f, 0.65f, -0.015f, 0.015f, 1.45f, 0.015f));
        parts.Add(Box(black, -0.30f, 1.44f, -0.015f, 0.30f, 1.47f, 0.015f));
        foreach (var x in new[] { -0.20f, 0.20f })
        {
            parts.Add(Box(frame, x - 0.12f, 1.40f, -0.06f, x + 0.12f, 1.60f, 0.06f));
            parts.Add(Box(lens, x - 0.10f, 1.42f, 0.06f, x + 0.10f, 1.58f, 0.065f));
        }

        Save("WorkLight", "work_light", parts);
    }

    private static void BuildCementBags(Material bag, Material print)
    {
        // Two 25 kg bags lying stacked, 0.65 x 0.24 x 0.42 m.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 2; i++)
        {
            var rotation = Quaternion.Euler(0f, i * 7f, 0f);
            var y = i * 0.12f;
            parts.Add(new LowPolyBoxMesh.Part(bag, new Vector3(0f, y + 0.06f, 0f), new Vector3(0.60f, 0.12f, 0.40f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(print, new Vector3(0f, y + 0.121f, 0f), new Vector3(0.30f, 0.003f, 0.20f), rotation));
        }

        Save("CementBags", "cement_bags", parts);
    }

    private static void BuildRubble(Material[] chunks)
    {
        // A low pile of broken brick and plaster, 0.80 x 0.30 x 0.60 m, against the wall.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, chunks[2], 0f, 0f, 0f, 0.70f, 0.08f);
        Round(parts, chunks[2], 0f, 0.08f, 0f, 0.42f, 0.16f);
        var rng = new System.Random(4018);
        for (var i = 0; i < 18; i++)
        {
            var angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            var r = (float)rng.NextDouble() * 0.28f;
            var size = 0.05f + (float)rng.NextDouble() * 0.08f;
            var y = 0.10f + (0.28f - r) * 0.5f;
            parts.Add(new LowPolyBoxMesh.Part(chunks[i % 2], new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r * 0.8f),
                new Vector3(size * 1.6f, size, size), Quaternion.Euler(rng.Next(0, 60), rng.Next(0, 180), rng.Next(0, 60))));
        }

        Save("Rubble", "rubble", parts);
    }
}
