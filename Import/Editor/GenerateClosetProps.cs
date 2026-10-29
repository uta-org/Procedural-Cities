using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the closet props (P40.9, owner-approved list Q-20261028-9): StorageBoxes (two lidded
/// plastic boxes), VacuumCleaner (upright), BroomAndMop (leaning on the wall, a bucket at their
/// foot), ShoppingBags and IroningBoard (folded, leaning on the wall). The shoe boxes, clothes on
/// hangers, suitcase, shoe rack and laundry basket reuse ShoeBoxes, ClothesRail, Suitcase,
/// ShoeRack and LaundryBasket. Every piece has its pivot on the bottom face at the centre, front
/// +Z; the leaning ones have their back at -Z against the wall. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Closet Props
/// </summary>
public static class GenerateClosetProps
{
    [MenuItem("Tools/Procedural Cities/Generate Closet Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateClosetProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_CL_Black", new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.4f);
        BuildStorageBoxes(LowPolyBoxMesh.GetMat("LP_CL_BoxClear", new Color(0.82f, 0.86f, 0.88f), 0f, 0.7f),
            LowPolyBoxMesh.GetMat("LP_CL_BoxLid", new Color(0.30f, 0.50f, 0.70f), 0f, 0.5f),
            LowPolyBoxMesh.GetMat("LP_CL_BoxContent", new Color(0.70f, 0.55f, 0.45f), 0f, 0.1f));
        BuildVacuumCleaner(LowPolyBoxMesh.GetMat("LP_CL_Vacuum", new Color(0.75f, 0.15f, 0.20f), 0.2f, 0.6f), black, chrome);
        BuildBroomAndMop(LowPolyBoxMesh.GetMat("LP_CL_Wood", new Color(0.62f, 0.45f, 0.28f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CL_Bristle", new Color(0.85f, 0.75f, 0.30f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_CL_Mop", new Color(0.88f, 0.88f, 0.84f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_CL_Bucket", new Color(0.20f, 0.45f, 0.75f), 0f, 0.5f));
        BuildShoppingBags(LowPolyBoxMesh.GetMat("LP_CL_Paper", new Color(0.70f, 0.55f, 0.38f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_CL_Tote", new Color(0.30f, 0.55f, 0.40f), 0f, 0.1f), black);
        BuildIroningBoard(LowPolyBoxMesh.GetMat("LP_CL_Cover", new Color(0.55f, 0.70f, 0.80f), 0f, 0.1f), chrome);
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

    private static void BuildStorageBoxes(Material box, Material lid, Material content)
    {
        // Two 0.55 x 0.28 x 0.38 m boxes, stacked.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 2; i++)
        {
            var y = i * 0.30f;
            parts.Add(Box(box, -0.27f, y, -0.18f, 0.27f, y + 0.26f, 0.18f));
            parts.Add(Box(content, -0.24f, y + 0.02f, -0.15f, 0.24f, y + 0.20f, 0.15f));
            parts.Add(Box(lid, -0.28f, y + 0.26f, -0.19f, 0.28f, y + 0.29f, 0.19f));
        }

        Save("StorageBoxes", "storage_boxes", parts);
    }

    private static void BuildVacuumCleaner(Material body, Material black, Material metal)
    {
        // Upright, 1.10 m with its handle: a head on the floor, the body on the tube.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -0.14f, 0f, 0.02f, 0.14f, 0.06f, 0.16f),
            Box(body, -0.10f, 0.06f, -0.12f, 0.10f, 0.52f, 0.06f),
            Box(black, -0.10f, 0.20f, 0.06f, 0.10f, 0.24f, 0.07f),
            Box(metal, -0.018f, 0.52f, -0.06f, 0.018f, 1.02f, -0.025f),
            Box(black, -0.10f, 1.02f, -0.065f, 0.10f, 1.06f, -0.02f),
            Box(black, -0.10f, 1.06f, -0.065f, -0.07f, 1.10f, -0.02f),
            Box(black, 0.07f, 1.06f, -0.065f, 0.10f, 1.10f, -0.02f),
            Box(black, -0.10f, 1.10f, -0.065f, 0.10f, 1.12f, -0.02f),
        };
        foreach (var sx in new[] { -1f, 1f })
            Round(parts, black, sx * 0.10f, 0f, -0.07f, 0.08f, 0.08f);
        Save("VacuumCleaner", "vacuum_cleaner", parts);
    }

    private static void BuildBroomAndMop(Material wood, Material bristle, Material mop, Material bucket)
    {
        // Two handles leaning back on the wall (-Z), a bucket at their foot.
        var lean = Quaternion.Euler(-8f, 0f, 0f);
        var up = lean * Vector3.up;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            new(wood, new Vector3(-0.12f, 0f, 0.05f) + up * 0.70f, new Vector3(0.025f, 1.20f, 0.025f), lean),
            new(bristle, new Vector3(-0.12f, 0.04f, 0.07f), new Vector3(0.26f, 0.08f, 0.06f), Quaternion.identity),
            new(wood, new Vector3(-0.12f, 0.08f, 0.07f), new Vector3(0.28f, 0.03f, 0.05f), Quaternion.identity),
            new(wood, new Vector3(0.10f, 0f, 0.05f) + up * 0.70f, new Vector3(0.025f, 1.20f, 0.025f), Quaternion.Euler(-8f, 0f, -3f)),
        };
        Round(parts, mop, 0.11f, 0f, 0.07f, 0.16f, 0.14f);
        Round(parts, bucket, 0.10f, 0f, 0.12f, 0.26f, 0.24f);
        Save("BroomAndMop", "broom_mop", parts);
    }

    private static void BuildShoppingBags(Material paper, Material tote, Material handle)
    {
        // A paper bag and a tote, leaning together.
        Save("ShoppingBags", "shopping_bags", new List<LowPolyBoxMesh.Part>
        {
            Box(paper, -0.20f, 0f, -0.08f, 0.04f, 0.32f, 0.08f),
            Box(handle, -0.13f, 0.32f, -0.005f, -0.11f, 0.38f, 0.005f),
            Box(handle, -0.05f, 0.32f, -0.005f, -0.03f, 0.38f, 0.005f),
            Box(handle, -0.13f, 0.37f, -0.005f, -0.03f, 0.385f, 0.005f),
            new(tote, new Vector3(0.12f, 0.17f, 0.01f), new Vector3(0.24f, 0.34f, 0.08f), Quaternion.Euler(0f, 0f, -8f)),
            new(tote, new Vector3(0.10f, 0.40f, 0.01f), new Vector3(0.02f, 0.14f, 0.01f), Quaternion.Euler(0f, 0f, 20f)),
        });
    }

    private static void BuildIroningBoard(Material cover, Material metal)
    {
        // Folded, standing on its tail and leaning back on the wall: 1.20 m long, 0.38 m wide.
        var lean = Quaternion.Euler(-10f, 0f, 0f);
        var up = lean * Vector3.up;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            new(cover, new Vector3(0f, 0f, 0.10f) + up * 0.60f, new Vector3(0.38f, 1.20f, 0.025f), lean),
            new(metal, new Vector3(0f, 0f, 0.08f) + up * 0.55f, new Vector3(0.30f, 1.05f, 0.02f), lean),
            new(metal, new Vector3(0f, 0f, 0.13f) + up * 1.18f, new Vector3(0.16f, 0.04f, 0.03f), lean),
        };
        Save("IroningBoard", "ironing_board", parts);
    }
}
