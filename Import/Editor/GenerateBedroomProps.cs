using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the bedroom props (P40.3, owner-approved list Q-20261028-9): BedsideRug (1.2 x 0.7 m),
/// TeddyBear, AlarmClock, BedsideBook, Slippers (a pair), LaundryBasket, FloorMirror (0.5 x 1.6 m)
/// and JewelleryBox. The cushions, photo frame and coat stand reuse SofaCushion, PhotoFrame and
/// Hanger. Every pivot is on the bottom face at the centre, front +Z. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Bedroom Props
/// </summary>
public static class GenerateBedroomProps
{
    [MenuItem("Tools/Procedural Cities/Generate Bedroom Props")]
    public static void Generate()
    {
        var wood = LowPolyBoxMesh.GetMat("LP_LR_Wood", new Color(0.52f, 0.36f, 0.22f), 0f, 0.35f);
        var black = LowPolyBoxMesh.GetMat("LP_LR_Black", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.5f);

        BuildRug(LowPolyBoxMesh.GetMat("LP_BR_RugField", new Color(0.82f, 0.80f, 0.74f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_BR_RugBorder", new Color(0.30f, 0.40f, 0.55f), 0f, 0.05f));
        BuildTeddy(LowPolyBoxMesh.GetMat("LP_BR_TeddyFur", new Color(0.55f, 0.38f, 0.22f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_BR_TeddyMuzzle", new Color(0.85f, 0.72f, 0.55f), 0f, 0.05f), black);
        BuildAlarmClock(LowPolyBoxMesh.GetMat("LP_BR_ClockBody", new Color(0.75f, 0.18f, 0.16f), 0.2f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_BR_ClockFace", new Color(0.97f, 0.97f, 0.95f), 0f, 0.6f), black);
        BuildBook(LowPolyBoxMesh.GetMat("LP_BR_BookCover", new Color(0.18f, 0.40f, 0.32f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_BR_BookPages", new Color(0.95f, 0.93f, 0.86f), 0f, 0.2f));
        BuildSlippers(LowPolyBoxMesh.GetMat("LP_BR_Slipper", new Color(0.55f, 0.58f, 0.62f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_BR_SlipperSole", new Color(0.25f, 0.22f, 0.20f), 0f, 0.1f));
        BuildLaundryBasket(LowPolyBoxMesh.GetMat("LP_BR_Wicker", new Color(0.72f, 0.58f, 0.38f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_BR_Clothes", new Color(0.45f, 0.55f, 0.75f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_BR_ClothesB", new Color(0.92f, 0.90f, 0.86f), 0f, 0.05f));
        BuildFloorMirror(wood, LowPolyBoxMesh.GetMat("LP_BR_Mirror", new Color(0.78f, 0.84f, 0.88f), 0.9f, 0.95f));
        BuildJewelleryBox(wood, LowPolyBoxMesh.GetMat("LP_BR_Gold", new Color(0.83f, 0.68f, 0.30f), 0.8f, 0.7f));
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

    private static void BuildRug(Material field, Material border)
    {
        const float w = 1.2f, d = 0.7f, h = 0.012f, b = 0.07f;
        Save("BedsideRug", "rug", new List<LowPolyBoxMesh.Part>
        {
            Box(border, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            Box(field, -w * 0.5f + b, h, -d * 0.5f + b, w * 0.5f - b, h + 0.002f, d * 0.5f - b),
        });
    }

    private static void BuildTeddy(Material fur, Material muzzle, Material eyes)
    {
        // Sitting bear, 0.30 m high, facing +Z.
        Save("TeddyBear", "teddy", new List<LowPolyBoxMesh.Part>
        {
            Box(fur, -0.08f, 0.02f, -0.06f, 0.08f, 0.17f, 0.06f),
            Box(fur, -0.07f, 0.17f, -0.06f, 0.07f, 0.29f, 0.06f),
            Box(fur, -0.075f, 0.27f, -0.02f, -0.045f, 0.31f, 0.01f),
            Box(fur, 0.045f, 0.27f, -0.02f, 0.075f, 0.31f, 0.01f),
            Box(muzzle, -0.03f, 0.19f, 0.06f, 0.03f, 0.23f, 0.08f),
            Box(eyes, -0.04f, 0.245f, 0.06f, -0.025f, 0.26f, 0.062f),
            Box(eyes, 0.025f, 0.245f, 0.06f, 0.04f, 0.26f, 0.062f),
            // Arms, and the legs stretched forward.
            Box(fur, -0.12f, 0.07f, -0.03f, -0.08f, 0.16f, 0.04f),
            Box(fur, 0.08f, 0.07f, -0.03f, 0.12f, 0.16f, 0.04f),
            Box(fur, -0.075f, 0f, 0.02f, -0.02f, 0.05f, 0.14f),
            Box(fur, 0.02f, 0f, 0.02f, 0.075f, 0.05f, 0.14f),
        });
    }

    private static void BuildAlarmClock(Material body, Material face, Material hands)
    {
        Save("AlarmClock", "clock", new List<LowPolyBoxMesh.Part>
        {
            Box(hands, -0.045f, 0f, -0.02f, -0.035f, 0.012f, 0.02f),
            Box(hands, 0.035f, 0f, -0.02f, 0.045f, 0.012f, 0.02f),
            Box(body, -0.055f, 0.012f, -0.03f, 0.055f, 0.10f, 0.03f),
            Box(face, -0.042f, 0.022f, 0.03f, 0.042f, 0.09f, 0.032f),
            Box(hands, -0.002f, 0.055f, 0.032f, 0.002f, 0.085f, 0.034f),
            Box(hands, -0.002f, 0.055f, 0.032f, 0.025f, 0.059f, 0.034f),
            Box(body, -0.05f, 0.10f, -0.01f, -0.025f, 0.115f, 0.01f),
            Box(body, 0.025f, 0.10f, -0.01f, 0.05f, 0.115f, 0.01f),
        });
    }

    private static void BuildBook(Material cover, Material pages)
    {
        Save("BedsideBook", "book", new List<LowPolyBoxMesh.Part>
        {
            Box(cover, -0.075f, 0f, -0.105f, 0.075f, 0.004f, 0.105f),
            Box(pages, -0.072f, 0.004f, -0.102f, 0.072f, 0.026f, 0.102f),
            Box(cover, -0.075f, 0.026f, -0.105f, 0.075f, 0.03f, 0.105f),
            Box(cover, -0.078f, 0f, -0.105f, -0.072f, 0.03f, 0.105f),
        });
    }

    private static void BuildSlippers(Material felt, Material sole)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var side = -1; side <= 1; side += 2)
        {
            var x = side * 0.07f;
            var turn = Quaternion.Euler(0f, side * 6f, 0f);
            parts.Add(new LowPolyBoxMesh.Part(sole, new Vector3(x, 0.0075f, 0f), new Vector3(0.095f, 0.015f, 0.26f), turn));
            parts.Add(new LowPolyBoxMesh.Part(felt, new Vector3(x, 0.025f, 0.01f), new Vector3(0.09f, 0.02f, 0.24f), turn));
            parts.Add(new LowPolyBoxMesh.Part(felt, new Vector3(x, 0.045f, 0.055f), new Vector3(0.09f, 0.03f, 0.12f), turn));
        }

        Save("Slippers", "slippers", parts);
    }

    private static void BuildLaundryBasket(Material wicker, Material clothes, Material clothesB)
    {
        const float w = 0.45f, h = 0.48f, d = 0.34f, t = 0.02f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wicker, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, t, d * 0.5f),
            Box(wicker, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, h, -d * 0.5f + t),
            Box(wicker, -w * 0.5f, 0f, d * 0.5f - t, w * 0.5f, h, d * 0.5f),
            Box(wicker, -w * 0.5f, 0f, -d * 0.5f, -w * 0.5f + t, h, d * 0.5f),
            Box(wicker, w * 0.5f - t, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            // Clothes filling it and spilling over the rim.
            Box(clothes, -w * 0.5f + t, t, -d * 0.5f + t, w * 0.5f - t, h - 0.03f, d * 0.5f - t),
            new(clothesB, new Vector3(0.05f, h, 0.02f), new Vector3(0.30f, 0.05f, 0.22f), Quaternion.Euler(0f, 20f, 8f)),
            new(clothes, new Vector3(-0.1f, h + 0.01f, -0.04f), new Vector3(0.18f, 0.04f, 0.16f), Quaternion.Euler(6f, -15f, 0f)),
        };
        // Two darker hoops round the weave.
        var band = LowPolyBoxMesh.GetMat("LP_BR_WickerBand", new Color(0.55f, 0.42f, 0.26f), 0f, 0.2f);
        foreach (var y in new[] { 0.12f, 0.36f })
        {
            parts.Add(Box(band, -w * 0.5f - 0.003f, y, -d * 0.5f - 0.003f, w * 0.5f + 0.003f, y + 0.025f, d * 0.5f + 0.003f));
        }

        Save("LaundryBasket", "basket", parts);
    }

    private static void BuildFloorMirror(Material frame, Material glass)
    {
        const float w = 0.50f, h = 1.60f, d = 0.04f, f = 0.05f;
        Save("FloorMirror", "mirror", new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -w * 0.5f, 0f, -d * 0.5f, w * 0.5f, h, d * 0.5f),
            Box(glass, -w * 0.5f + f, f, d * 0.5f, w * 0.5f - f, h - f, d * 0.5f + 0.003f),
            // Feet, so it stands clear of the skirting.
            Box(frame, -w * 0.5f, 0f, -d * 0.5f - 0.04f, -w * 0.5f + 0.06f, 0.03f, d * 0.5f + 0.06f),
            Box(frame, w * 0.5f - 0.06f, 0f, -d * 0.5f - 0.04f, w * 0.5f, 0.03f, d * 0.5f + 0.06f),
        });
    }

    private static void BuildJewelleryBox(Material wood, Material gold)
    {
        Save("JewelleryBox", "box", new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.09f, 0f, -0.06f, 0.09f, 0.07f, 0.06f),
            Box(wood, -0.093f, 0.07f, -0.063f, 0.093f, 0.09f, 0.063f),
            Box(gold, -0.093f, 0.066f, -0.063f, 0.093f, 0.072f, 0.063f),
            Box(gold, -0.012f, 0.05f, 0.06f, 0.012f, 0.075f, 0.065f),
        });
    }
}
