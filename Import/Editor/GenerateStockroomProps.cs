using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the stock room props (P40.16, owner-approved list Q-20261028-9), for StoreBack and Storage:
/// CardboardBoxes (a stack), Pallet (with a load of boxes), HandTruck, Stepladder (folded, leaning on
/// the wall), PalletJack, PlasticCrates, PackingTape, Trolley (platform trolley) and StretchFilmRoll.
/// The extinguisher reuses FireExtinguisher. Every piece has its pivot on the bottom face at the
/// centre, front +Z; the leaning ones have their back at -Z against the wall. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Stockroom Props
/// </summary>
public static class GenerateStockroomProps
{
    [MenuItem("Tools/Procedural Cities/Generate Stockroom Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateStockroomProps] Missing LP_Chrome.");
            return;
        }

        var cardboard = LowPolyBoxMesh.GetMat("LP_SR_Cardboard", new Color(0.72f, 0.56f, 0.36f), 0f, 0.15f);
        var tape = LowPolyBoxMesh.GetMat("LP_SR_Tape", new Color(0.82f, 0.70f, 0.45f), 0f, 0.6f);
        var wood = LowPolyBoxMesh.GetMat("LP_SR_PalletWood", new Color(0.70f, 0.58f, 0.40f), 0f, 0.2f);
        var black = LowPolyBoxMesh.GetMat("LP_SR_Black", new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.4f);
        var red = LowPolyBoxMesh.GetMat("LP_SR_Red", new Color(0.75f, 0.15f, 0.12f), 0.3f, 0.5f);
        var blue = LowPolyBoxMesh.GetMat("LP_SR_Blue", new Color(0.18f, 0.35f, 0.65f), 0f, 0.5f);

        BuildCardboardBoxes(cardboard, tape);
        BuildPallet(wood, cardboard, tape);
        BuildHandTruck(red, black, chrome);
        BuildStepladder(chrome, black);
        BuildPalletJack(LowPolyBoxMesh.GetMat("LP_SR_Yellow", new Color(0.90f, 0.70f, 0.10f), 0.3f, 0.5f), black, chrome);
        BuildPlasticCrates(blue, LowPolyBoxMesh.GetMat("LP_SR_Green", new Color(0.25f, 0.55f, 0.30f), 0f, 0.5f));
        BuildPackingTape(tape, black);
        BuildTrolley(blue, black, chrome);
        BuildStretchFilmRoll(LowPolyBoxMesh.GetMat("LP_SR_FilmRoll", new Color(0.80f, 0.86f, 0.90f), 0f, 0.9f), cardboard);
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

    private static void TapedBox(List<LowPolyBoxMesh.Part> parts, Material box, Material tape, Vector3 centre, Vector3 size, Quaternion rotation)
    {
        parts.Add(new LowPolyBoxMesh.Part(box, centre, size, rotation));
        parts.Add(new LowPolyBoxMesh.Part(tape, centre + rotation * new Vector3(0f, size.y * 0.5f, 0f), new Vector3(0.05f, 0.004f, size.z + 0.002f), rotation));
    }

    private static void BuildCardboardBoxes(Material box, Material tape)
    {
        // Three boxes stacked against the wall, 0.60 x 0.95 x 0.40 m in all.
        var parts = new List<LowPolyBoxMesh.Part>();
        TapedBox(parts, box, tape, new Vector3(-0.05f, 0.20f, 0f), new Vector3(0.50f, 0.40f, 0.40f), Quaternion.identity);
        TapedBox(parts, box, tape, new Vector3(0.02f, 0.58f, 0.01f), new Vector3(0.44f, 0.36f, 0.36f), Quaternion.Euler(0f, 5f, 0f));
        TapedBox(parts, box, tape, new Vector3(0f, 0.86f, 0f), new Vector3(0.34f, 0.20f, 0.28f), Quaternion.Euler(0f, -8f, 0f));
        Save("CardboardBoxes", "cardboard_boxes", parts);
    }

    private static void BuildPallet(Material wood, Material box, Material tape)
    {
        // A 1.20 x 0.80 m pallet with a load of boxes, 1.11 m high (no film wrap: an opaque
        // material hid the boxes behind a white block).
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 5; i++)
            parts.Add(Box(wood, -0.60f, 0.12f, -0.40f + i * 0.18f, 0.60f, 0.145f, -0.40f + i * 0.18f + 0.10f));
        foreach (var x in new[] { -0.55f, 0f, 0.55f })
        {
            parts.Add(Box(wood, x - 0.05f, 0.022f, -0.40f, x + 0.05f, 0.12f, 0.40f));
            parts.Add(Box(wood, x - 0.05f, 0f, -0.40f, x + 0.05f, 0.022f, 0.40f));
        }

        for (var layer = 0; layer < 2; layer++)
        {
            for (var col = 0; col < 3; col++)
            {
                for (var row = 0; row < 2; row++)
                    TapedBox(parts, box, tape, new Vector3(-0.38f + col * 0.38f, 0.145f + 0.24f + layer * 0.48f, -0.19f + row * 0.38f), new Vector3(0.36f, 0.46f, 0.36f), Quaternion.identity);
            }
        }

        Save("Pallet", "pallet", parts);
    }

    private static void BuildHandTruck(Material frame, Material black, Material metal)
    {
        // A two-wheeled hand truck standing upright, its nose plate on the floor at +Z, 1.20 m.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(metal, -0.20f, 0f, 0f, 0.20f, 0.012f, 0.22f),
            Box(frame, -0.20f, 0.012f, -0.02f, -0.17f, 1.20f, 0.01f),
            Box(frame, 0.17f, 0.012f, -0.02f, 0.20f, 1.20f, 0.01f),
            Box(frame, -0.17f, 0.40f, -0.02f, 0.17f, 0.43f, 0f),
            Box(frame, -0.17f, 0.80f, -0.02f, 0.17f, 0.83f, 0f),
            Box(black, -0.20f, 1.15f, -0.03f, 0.20f, 1.20f, 0.02f),
        };
        foreach (var x in new[] { -0.24f, 0.24f })
        {
            for (var k = 0; k < 6; k++)
                parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(x, 0.11f, -0.07f), new Vector3(0.05f, 0.22f, 0.22f * Mathf.Tan(15f * Mathf.Deg2Rad)), Quaternion.Euler(k * 30f, 0f, 0f)));
        }

        Save("HandTruck", "hand_truck", parts);
    }

    private static void BuildStepladder(Material metal, Material black)
    {
        // A folded stepladder leaning back on the wall: 0.45 m wide, 1.50 m long.
        var lean = Quaternion.Euler(-10f, 0f, 0f);
        var up = lean * Vector3.up;
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var x in new[] { -0.21f, 0.21f })
        {
            parts.Add(new LowPolyBoxMesh.Part(metal, new Vector3(x, 0f, 0.14f) + up * 0.75f, new Vector3(0.03f, 1.50f, 0.06f), lean));
            parts.Add(new LowPolyBoxMesh.Part(metal, new Vector3(x, 0f, 0.08f) + up * 0.72f, new Vector3(0.025f, 1.44f, 0.03f), lean));
        }

        for (var i = 0; i < 4; i++)
            parts.Add(new LowPolyBoxMesh.Part(metal, new Vector3(0f, 0f, 0.15f) + up * (0.30f + i * 0.30f), new Vector3(0.40f, 0.02f, 0.10f), lean));
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(0f, 0f, 0.13f) + up * 1.47f, new Vector3(0.45f, 0.05f, 0.12f), lean));
        Save("Stepladder", "stepladder", parts);
    }

    private static void BuildPalletJack(Material body, Material black, Material metal)
    {
        // Forks along +Z (1.15 m), the pump and its handle at -Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.28f, 0.02f, -0.30f, -0.12f, 0.08f, 0.85f),
            Box(body, 0.12f, 0.02f, -0.30f, 0.28f, 0.08f, 0.85f),
            Box(body, -0.28f, 0.02f, -0.40f, 0.28f, 0.20f, -0.25f),
            Box(body, -0.08f, 0.20f, -0.40f, 0.08f, 0.40f, -0.28f),
            new(black, new Vector3(0f, 0.75f, -0.45f), new Vector3(0.04f, 0.75f, 0.04f), Quaternion.Euler(-12f, 0f, 0f)),
            Box(black, -0.15f, 1.10f, -0.56f, 0.15f, 1.14f, -0.50f),
        };
        foreach (var x in new[] { -0.20f, 0.20f })
            parts.Add(Box(metal, x - 0.03f, 0f, 0.75f, x + 0.03f, 0.04f, 0.83f));
        Save("PalletJack", "pallet_jack", parts);
    }

    private static void BuildPlasticCrates(Material blue, Material green)
    {
        // Three stackable crates, 0.60 x 0.30 x 0.40 m each, 0.90 m high.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            var mat = i == 1 ? green : blue;
            var y = i * 0.30f;
            parts.Add(Box(mat, -0.30f, y, -0.20f, 0.30f, y + 0.03f, 0.20f));
            parts.Add(Box(mat, -0.30f, y, -0.20f, 0.30f, y + 0.30f, -0.18f));
            parts.Add(Box(mat, -0.30f, y, 0.18f, 0.30f, y + 0.30f, 0.20f));
            parts.Add(Box(mat, -0.30f, y, -0.18f, -0.28f, y + 0.30f, 0.18f));
            parts.Add(Box(mat, 0.28f, y, -0.18f, 0.30f, y + 0.30f, 0.18f));
        }

        Save("PlasticCrates", "plastic_crates", parts);
    }

    private static void BuildPackingTape(Material tape, Material dispenser)
    {
        // A tape gun lying on the table with its roll.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(dispenser, -0.12f, 0f, -0.02f, 0.10f, 0.025f, 0.02f),
            Box(dispenser, 0.06f, 0.025f, -0.02f, 0.10f, 0.10f, 0.02f),
        };
        for (var k = 0; k < 6; k++)
            parts.Add(new LowPolyBoxMesh.Part(tape, new Vector3(-0.02f, 0.06f, 0f), new Vector3(0.10f, 0.10f * Mathf.Tan(15f * Mathf.Deg2Rad), 0.05f), Quaternion.Euler(0f, 0f, k * 30f)));
        Save("PackingTape", "packing_tape", parts);
    }

    private static void BuildTrolley(Material deck, Material black, Material metal)
    {
        // A platform trolley, 0.90 x 0.60 m deck at 0.20 m, its push handle at -Z, 1.00 m.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(deck, -0.45f, 0.15f, -0.30f, 0.45f, 0.20f, 0.30f),
            Box(metal, -0.43f, 0.20f, -0.32f, -0.40f, 1.00f, -0.29f),
            Box(metal, 0.40f, 0.20f, -0.32f, 0.43f, 1.00f, -0.29f),
            Box(black, -0.43f, 0.97f, -0.33f, 0.43f, 1.00f, -0.28f),
        };
        foreach (var x in new[] { -0.36f, 0.36f })
        {
            foreach (var z in new[] { -0.22f, 0.22f })
                Round(parts, black, x, 0f, z, 0.12f, 0.15f);
        }

        Save("Trolley", "trolley", parts);
    }

    private static void BuildStretchFilmRoll(Material film, Material core)
    {
        // A 0.50 m roll of stretch film standing on end.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, film, 0f, 0f, 0f, 0.12f, 0.50f);
        Round(parts, core, 0f, 0.50f, 0f, 0.05f, 0.53f);
        Save("StretchFilmRoll", "stretch_film_roll", parts);
    }
}
