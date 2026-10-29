using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the civic building props (P43, owner Q-20261029-3 A); the town hall's first (P43.3):
/// CivicFlags (three flags on poles in a base), CoatOfArms (a wall-hung shield), Lectern,
/// WaitingChairs (a row of three seats on a beam), LeafletRack (a floor stand of leaflets), Bust
/// (on a pedestal) and CivicVitrine (a glass case on a plinth). The service counter, the rug,
/// the plenary table and chairs and the plants reuse StoreCounter, LivingRug, the meeting room's
/// pieces and the planters. Every piece has its pivot on its bottom face at the centre, front +Z;
/// a wall-hung one has its back at Z = 0. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Civic Props
/// </summary>
public static class GenerateCivicProps
{
    [MenuItem("Tools/Procedural Cities/Generate Civic Props")]
    public static void Generate()
    {
        var brass = LowPolyBoxMesh.GetMat("LP_CV_Brass", new Color(0.78f, 0.62f, 0.28f), 0.8f, 0.6f);
        var dark = LowPolyBoxMesh.GetMat("LP_CV_DarkWood", new Color(0.30f, 0.18f, 0.10f), 0f, 0.35f);
        var stone = LowPolyBoxMesh.GetMat("LP_CV_Stone", new Color(0.82f, 0.80f, 0.76f), 0f, 0.2f);
        var black = LowPolyBoxMesh.GetMat("LP_CV_Black", new Color(0.12f, 0.12f, 0.13f), 0.3f, 0.4f);
        var glass = LowPolyBoxMesh.GetMat("LP_CV_Glass", new Color(0.70f, 0.82f, 0.88f), 0.1f, 0.95f);

        BuildFlags(brass, black,
            LowPolyBoxMesh.GetMat("LP_CV_FlagRed", new Color(0.72f, 0.12f, 0.12f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagYellow", new Color(0.92f, 0.76f, 0.15f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagBlue", new Color(0.14f, 0.24f, 0.58f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagWhite", new Color(0.94f, 0.94f, 0.92f), 0f, 0.2f));
        BuildCoatOfArms(dark, brass,
            LowPolyBoxMesh.GetMat("LP_CV_ShieldRed", new Color(0.62f, 0.10f, 0.10f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_ShieldBlue", new Color(0.12f, 0.20f, 0.50f), 0f, 0.3f));
        BuildLectern(dark, brass);
        BuildWaitingChairs(black, LowPolyBoxMesh.GetMat("LP_CV_Seat", new Color(0.18f, 0.32f, 0.52f), 0f, 0.3f));
        BuildLeafletRack(black, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_LeafWhite", new Color(0.95f, 0.95f, 0.93f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafBlue", new Color(0.45f, 0.62f, 0.85f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafGreen", new Color(0.45f, 0.72f, 0.45f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafOrange", new Color(0.95f, 0.62f, 0.28f), 0f, 0.2f),
        });
        BuildBust(stone, LowPolyBoxMesh.GetMat("LP_CV_Bronze", new Color(0.42f, 0.30f, 0.18f), 0.8f, 0.5f));
        BuildVitrine(dark, glass, brass);

        // The police station's (P43.4).
        var steel = LowPolyBoxMesh.GetMat("LP_CV_Steel", new Color(0.55f, 0.57f, 0.60f), 0.8f, 0.5f);
        BuildCellBars(steel);
        BuildWantedBoard(dark, LowPolyBoxMesh.GetMat("LP_CV_Cork", new Color(0.70f, 0.52f, 0.34f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_CV_Poster", new Color(0.93f, 0.90f, 0.80f), 0f, 0.2f), black);
        BuildMetalDetector(LowPolyBoxMesh.GetMat("LP_CV_DetectorGrey", new Color(0.72f, 0.73f, 0.75f), 0.3f, 0.4f), black);
        BuildPoliceCar(LowPolyBoxMesh.GetMat("LP_CV_CarWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_CV_CarBlue", new Color(0.10f, 0.20f, 0.55f), 0.2f, 0.6f), black,
            LowPolyBoxMesh.GetMat("LP_CV_CarGlass", new Color(0.20f, 0.26f, 0.32f), 0.3f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_CV_LightBlue", new Color(0.20f, 0.45f, 1.00f), 0f, 0.9f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    /// <summary>A 12-sided prism <paramref name="width"/> across the flats (six boxes turned 30 degrees apart).</summary>
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

    private static void BuildFlags(Material brass, Material black, Material red, Material yellow, Material blue, Material white)
    {
        // A base 0.9 x 0.3 m holding three poles 2.4 m high, a flag hanging from each.
        var parts = new List<LowPolyBoxMesh.Part> { Box(black, -0.45f, 0f, -0.15f, 0.45f, 0.08f, 0.15f) };
        var flags = new[] { (red, yellow), (blue, white), (yellow, red) };
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 0.32f;
            Round(parts, brass, x, 0.08f, 0f, 0.035f, 2.40f);
            Round(parts, brass, x, 2.40f, 0f, 0.07f, 2.46f);
            var (a, b) = flags[i];
            parts.Add(Box(a, x + 0.02f, 1.55f, -0.008f, x + 0.30f, 2.30f, 0.008f));
            parts.Add(Box(b, x + 0.02f, 1.80f, -0.009f, x + 0.30f, 2.05f, 0.009f));
        }

        Save("CivicFlags", "civic_flags", parts);
    }

    private static void BuildCoatOfArms(Material wood, Material brass, Material red, Material blue)
    {
        // A wooden plaque 0.60 x 0.75 m with a quartered shield and a crown, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.30f, 0f, 0f, 0.30f, 0.75f, 0.03f),
            Box(red, -0.20f, 0.30f, 0.03f, 0f, 0.55f, 0.045f),
            Box(blue, 0f, 0.30f, 0.03f, 0.20f, 0.55f, 0.045f),
            Box(blue, -0.20f, 0.12f, 0.03f, 0f, 0.30f, 0.045f),
            Box(red, 0f, 0.12f, 0.03f, 0.20f, 0.30f, 0.045f),
            new(red, new Vector3(0f, 0.10f, 0.037f), new Vector3(0.20f, 0.20f, 0.015f), Quaternion.Euler(0f, 0f, 45f)),
            Box(brass, -0.22f, 0.55f, 0.03f, 0.22f, 0.58f, 0.05f),
            Box(brass, -0.14f, 0.58f, 0.03f, 0.14f, 0.68f, 0.05f),
        };
        Save("CoatOfArms", "coat_of_arms", parts);
    }

    private static void BuildLectern(Material wood, Material brass)
    {
        // A base, a column and a sloping top facing -Z (the speaker stands behind it, the hall in front +Z).
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.30f, 0f, -0.25f, 0.30f, 0.06f, 0.25f),
            Box(wood, -0.22f, 0.06f, -0.15f, 0.22f, 1.00f, 0.15f),
            new(wood, new Vector3(0f, 1.08f, -0.02f), new Vector3(0.56f, 0.05f, 0.42f), Quaternion.Euler(-15f, 0f, 0f)),
            Box(brass, -0.10f, 0.55f, 0.15f, 0.10f, 0.70f, 0.16f),
        };
        Save("Lectern", "lectern", parts);
    }

    private static void BuildWaitingChairs(Material frame, Material seat)
    {
        // Three seats 0.5 m wide on a beam with two legs, backs at -Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.78f, 0.36f, -0.06f, 0.78f, 0.40f, 0.06f),
            Box(frame, -0.70f, 0f, -0.05f, -0.64f, 0.36f, 0.05f),
            Box(frame, 0.64f, 0f, -0.05f, 0.70f, 0.36f, 0.05f),
            Box(frame, -0.74f, 0f, -0.25f, -0.60f, 0.02f, 0.25f),
            Box(frame, 0.60f, 0f, -0.25f, 0.74f, 0.02f, 0.25f),
        };
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 0.52f;
            parts.Add(Box(seat, x - 0.24f, 0.40f, -0.22f, x + 0.24f, 0.46f, 0.24f));
            parts.Add(new LowPolyBoxMesh.Part(seat, new Vector3(x, 0.70f, -0.24f), new Vector3(0.48f, 0.48f, 0.05f), Quaternion.Euler(-8f, 0f, 0f)));
        }

        Save("WaitingChairs", "waiting_chairs", parts);
    }

    private static void BuildLeafletRack(Material frame, Material[] leaflets)
    {
        // A floor stand 1.4 m high with four sloping shelves of leaflets facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.25f, 0f, -0.20f, 0.25f, 0.03f, 0.20f),
            Box(frame, -0.02f, 0.03f, -0.10f, 0.02f, 1.40f, -0.06f),
            Box(frame, -0.22f, 0.30f, -0.10f, 0.22f, 1.40f, -0.08f),
        };
        for (var r = 0; r < 4; r++)
        {
            var y = 0.42f + r * 0.26f;
            parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(0f, y, -0.02f), new Vector3(0.44f, 0.02f, 0.12f), Quaternion.Euler(20f, 0f, 0f)));
            for (var c = 0; c < 3; c++)
                parts.Add(new LowPolyBoxMesh.Part(leaflets[(r + c) % leaflets.Length], new Vector3((c - 1) * 0.14f, y + 0.10f, -0.05f),
                    new Vector3(0.12f, 0.20f, 0.01f), Quaternion.Euler(-12f, 0f, 0f)));
        }

        Save("LeafletRack", "leaflet_rack", parts);
    }

    private static void BuildBust(Material stone, Material bronze)
    {
        // A square pedestal 1.1 m high and a bronze bust on it, facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(stone, -0.22f, 0f, -0.22f, 0.22f, 0.08f, 0.22f),
            Box(stone, -0.17f, 0.08f, -0.17f, 0.17f, 1.02f, 0.17f),
            Box(stone, -0.20f, 1.02f, -0.20f, 0.20f, 1.10f, 0.20f),
            Box(bronze, -0.18f, 1.10f, -0.10f, 0.18f, 1.30f, 0.10f),
        };
        Round(parts, bronze, 0f, 1.30f, 0f, 0.08f, 1.36f);
        Round(parts, bronze, 0f, 1.36f, 0.01f, 0.18f, 1.58f);
        Round(parts, bronze, 0f, 1.58f, 0.0f, 0.15f, 1.62f);
        Save("Bust", "bust", parts);
    }

    private static void BuildCellBars(Material steel)
    {
        // A cell front 2.4 x 2.5 m standing on the floor: top and bottom rails, bars every 0.15 m,
        // a door frame in the middle third and its lock box, its back at Z = 0 (it stands off a wall).
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -1.20f, 0f, 0f, 1.20f, 0.06f, 0.06f),
            Box(steel, -1.20f, 2.44f, 0f, 1.20f, 2.50f, 0.06f),
            Box(steel, -1.20f, 1.10f, 0.005f, 1.20f, 1.15f, 0.055f),
            Box(steel, -0.40f, 0.06f, 0f, -0.36f, 2.44f, 0.06f),
            Box(steel, 0.36f, 0.06f, 0f, 0.40f, 2.44f, 0.06f),
            Box(steel, 0.20f, 1.00f, 0.06f, 0.32f, 1.20f, 0.10f),
        };
        for (var x = -1.14f; x <= 1.15f; x += 0.15f)
            parts.Add(Box(steel, x - 0.012f, 0.06f, 0.018f, x + 0.012f, 2.44f, 0.042f));
        Save("CellBars", "cell_bars", parts);
    }

    private static void BuildWantedBoard(Material frame, Material cork, Material poster, Material ink)
    {
        // A cork board 1.0 x 0.7 m with six "wanted" posters, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.50f, 0f, 0f, 0.50f, 0.70f, 0.03f),
            Box(cork, -0.46f, 0.04f, 0.03f, 0.46f, 0.66f, 0.035f),
        };
        for (var i = 0; i < 6; i++)
        {
            var x = -0.32f + (i % 3) * 0.32f;
            var y = 0.10f + (i / 3) * 0.30f;
            parts.Add(Box(poster, x - 0.12f, y, 0.035f, x + 0.12f, y + 0.24f, 0.038f));
            parts.Add(Box(ink, x - 0.06f, y + 0.08f, 0.038f, x + 0.06f, y + 0.20f, 0.040f));
            parts.Add(Box(ink, x - 0.09f, y + 0.02f, 0.038f, x + 0.09f, y + 0.05f, 0.040f));
        }

        Save("WantedBoard", "wanted_board", parts);
    }

    private static void BuildMetalDetector(Material body, Material black)
    {
        // A walk-through arch 0.9 m wide inside, 2.2 m high, pillars 0.12 m thick along Z 0.5 m.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.57f, 0f, -0.25f, -0.45f, 2.10f, 0.25f),
            Box(body, 0.45f, 0f, -0.25f, 0.57f, 2.10f, 0.25f),
            Box(body, -0.57f, 2.10f, -0.25f, 0.57f, 2.25f, 0.25f),
            Box(black, -0.20f, 2.12f, 0.25f, 0.20f, 2.22f, 0.26f),
        };
        Save("MetalDetector", "metal_detector", parts);
    }

    private static void BuildPoliceCar(Material white, Material blue, Material black, Material glass, Material light)
    {
        // A saloon 4.4 x 1.8 m along +Z, a blue stripe, a light bar on the roof and four wheels.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.90f, 0.30f, -2.20f, 0.90f, 0.95f, 2.20f),
            Box(white, -0.80f, 0.95f, -1.10f, 0.80f, 1.40f, 0.90f),
            Box(glass, -0.81f, 1.00f, -1.05f, 0.81f, 1.35f, 0.85f),
            Box(blue, -0.91f, 0.55f, -2.10f, 0.91f, 0.70f, 2.10f),
            Box(black, -0.55f, 1.40f, -0.20f, 0.55f, 1.44f, 0.20f),
            Box(light, -0.55f, 1.44f, -0.15f, -0.05f, 1.54f, 0.15f),
            Box(LowPolyBoxMesh.GetMat("LP_CV_LightRed", new Color(1.0f, 0.15f, 0.12f), 0f, 0.9f), 0.05f, 1.44f, -0.15f, 0.55f, 1.54f, 0.15f),
            Box(black, -0.85f, 0.32f, 2.19f, 0.85f, 0.50f, 2.22f),
        };
        foreach (var (x, z) in new[] { (-0.80f, -1.35f), (0.80f, -1.35f), (-0.80f, 1.35f), (0.80f, 1.35f) })
            parts.Add(Box(black, x - 0.12f, 0f, z - 0.34f, x + 0.12f, 0.62f, z + 0.34f));
        Save("PoliceCar", "police_car", parts);
    }

    private static void BuildVitrine(Material wood, Material glass, Material brass)
    {
        // A plinth 1.0 x 0.5 x 0.8 m and a glass case 0.6 m high on it holding three small objects.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.50f, 0f, -0.25f, 0.50f, 0.80f, 0.25f),
            Box(wood, -0.52f, 0.80f, -0.27f, 0.52f, 0.84f, 0.27f),
            Box(glass, -0.48f, 0.84f, -0.23f, 0.48f, 1.42f, 0.23f),
            Box(wood, -0.50f, 1.42f, -0.25f, 0.50f, 1.46f, 0.25f),
            Box(brass, -0.32f, 0.84f, -0.06f, -0.20f, 1.00f, 0.06f),
            Box(brass, -0.05f, 0.84f, -0.08f, 0.08f, 0.92f, 0.08f),
            Box(brass, 0.20f, 0.84f, -0.04f, 0.28f, 1.06f, 0.04f),
        };
        Save("CivicVitrine", "civic_vitrine", parts);
    }
}
