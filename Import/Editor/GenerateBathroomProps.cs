using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the bathroom props (P40.4, owner-approved list Q-20261028-9): BathMat (0.6 x 0.4 m),
/// TowelRail (wall-hung, a towel over its bar), FoldedTowels, ToiletRollHolder (wall-hung),
/// SinkToiletries (soap dish and a cup of toothbrushes), ToiletBrush, BathroomScale,
/// ShowerBottles and PedalBin. The laundry basket reuses LaundryBasket. Floor and counter
/// pieces have their pivot on the bottom face at the centre, front +Z; the wall-hung ones have
/// their back face at Z = 0, like Mirror. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Bathroom Props
/// </summary>
public static class GenerateBathroomProps
{
    [MenuItem("Tools/Procedural Cities/Generate Bathroom Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateBathroomProps] Missing LP_Chrome.");
            return;
        }

        var white = LowPolyBoxMesh.GetMat("LP_BA_White", new Color(0.95f, 0.95f, 0.95f), 0f, 0.7f);
        var towel = LowPolyBoxMesh.GetMat("LP_BA_Towel", new Color(0.55f, 0.75f, 0.80f), 0f, 0.05f);
        var towelB = LowPolyBoxMesh.GetMat("LP_BA_TowelB", new Color(0.95f, 0.93f, 0.88f), 0f, 0.05f);

        BuildBathMat(LowPolyBoxMesh.GetMat("LP_BA_Mat", new Color(0.40f, 0.62f, 0.70f), 0f, 0.05f));
        BuildTowelRail(chrome, towel);
        BuildFoldedTowels(towel, towelB);
        BuildRollHolder(chrome, white);
        BuildToiletries(white, LowPolyBoxMesh.GetMat("LP_BA_Cup", new Color(0.30f, 0.55f, 0.65f), 0f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_BA_Brush", new Color(0.85f, 0.30f, 0.30f), 0f, 0.4f),
            LowPolyBoxMesh.GetMat("LP_BA_Soap", new Color(0.92f, 0.80f, 0.55f), 0f, 0.5f));
        BuildToiletBrush(white, chrome);
        BuildScale(white, LowPolyBoxMesh.GetMat("LP_BA_Display", new Color(0.15f, 0.18f, 0.20f), 0f, 0.8f));
        BuildShowerBottles(LowPolyBoxMesh.GetMat("LP_BA_BottleA", new Color(0.25f, 0.45f, 0.80f), 0f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_BA_BottleB", new Color(0.95f, 0.65f, 0.25f), 0f, 0.6f), white);
        BuildPedalBin(LowPolyBoxMesh.GetMat("LP_BA_BinBody", new Color(0.80f, 0.81f, 0.82f), 0.6f, 0.6f),
            LowPolyBoxMesh.LoadMat("LP_Metal_DarkGray") ?? chrome);
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

    private static void BuildBathMat(Material mat)
    {
        Save("BathMat", "mat", new List<LowPolyBoxMesh.Part>
        {
            Box(mat, -0.30f, 0f, -0.20f, 0.30f, 0.012f, 0.20f),
            Box(mat, -0.27f, 0.012f, -0.17f, 0.27f, 0.016f, 0.17f),
        });
    }

    private static void BuildTowelRail(Material chrome, Material towel)
    {
        // Back on the wall at Z = 0; the bar 0.55 m up, a towel folded over it.
        const float w = 0.60f, barY = 0.55f, barZ = 0.07f;
        Save("TowelRail", "rail", new List<LowPolyBoxMesh.Part>
        {
            Box(chrome, -w * 0.5f, barY - 0.03f, 0f, -w * 0.5f + 0.03f, barY + 0.03f, barZ + 0.012f),
            Box(chrome, w * 0.5f - 0.03f, barY - 0.03f, 0f, w * 0.5f, barY + 0.03f, barZ + 0.012f),
            Box(chrome, -w * 0.5f, barY - 0.012f, barZ - 0.012f, w * 0.5f, barY + 0.012f, barZ + 0.012f),
            Box(towel, -0.22f, 0.05f, barZ - 0.035f, 0.22f, barY + 0.02f, barZ - 0.015f),
            Box(towel, -0.22f, 0.25f, barZ + 0.015f, 0.22f, barY + 0.02f, barZ + 0.035f),
            Box(towel, -0.22f, barY + 0.012f, barZ - 0.035f, 0.22f, barY + 0.03f, barZ + 0.035f),
        });
    }

    private static void BuildFoldedTowels(Material towel, Material towelB)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            parts.Add(Box(i % 2 == 0 ? towel : towelB, -0.13f + i * 0.005f, i * 0.04f, -0.09f, 0.13f + i * 0.005f, i * 0.04f + 0.038f, 0.09f));
        }

        Save("FoldedTowels", "towels", parts);
    }

    private static void BuildRollHolder(Material chrome, Material paper)
    {
        // Back on the wall at Z = 0, the roll at the pivot's height + 0.06.
        Save("ToiletRollHolder", "holder", new List<LowPolyBoxMesh.Part>
        {
            Box(chrome, -0.07f, 0.02f, 0f, 0.07f, 0.10f, 0.012f),
            Box(chrome, -0.07f, 0.055f, 0.012f, -0.06f, 0.065f, 0.12f),
            new(paper, new Vector3(0f, 0.06f, 0.07f), new Vector3(0.11f, 0.11f, 0.11f), Quaternion.Euler(45f, 0f, 0f)),
            new(paper, new Vector3(0f, 0.06f, 0.07f), new Vector3(0.11f, 0.11f, 0.11f), Quaternion.identity),
        });
    }

    private static void BuildToiletries(Material ceramic, Material cup, Material brush, Material soap)
    {
        Save("SinkToiletries", "toiletries", new List<LowPolyBoxMesh.Part>
        {
            // Soap dish with a bar of soap.
            Box(ceramic, -0.09f, 0f, -0.035f, -0.01f, 0.012f, 0.035f),
            Box(soap, -0.075f, 0.012f, -0.022f, -0.025f, 0.03f, 0.022f),
            // Cup with two toothbrushes.
            Box(cup, 0.02f, 0f, -0.03f, 0.08f, 0.10f, 0.03f),
            new(brush, new Vector3(0.04f, 0.13f, 0f), new Vector3(0.012f, 0.12f, 0.012f), Quaternion.Euler(0f, 0f, 10f)),
            new(ceramic, new Vector3(0.06f, 0.13f, 0.005f), new Vector3(0.012f, 0.12f, 0.012f), Quaternion.Euler(0f, 0f, -12f)),
        });
    }

    private static void BuildToiletBrush(Material holder, Material handle)
    {
        Save("ToiletBrush", "brush", new List<LowPolyBoxMesh.Part>
        {
            new(holder, new Vector3(0f, 0.08f, 0f), new Vector3(0.10f, 0.16f, 0.10f), Quaternion.identity),
            new(holder, new Vector3(0f, 0.08f, 0f), new Vector3(0.10f, 0.16f, 0.10f), Quaternion.Euler(0f, 45f, 0f)),
            Box(handle, -0.01f, 0.16f, -0.01f, 0.01f, 0.38f, 0.01f),
            Box(handle, -0.02f, 0.37f, -0.02f, 0.02f, 0.39f, 0.02f),
        });
    }

    private static void BuildScale(Material body, Material display)
    {
        Save("BathroomScale", "scale", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.15f, 0.005f, -0.15f, 0.15f, 0.035f, 0.15f),
            Box(display, -0.05f, 0.035f, 0.08f, 0.05f, 0.037f, 0.12f),
            Box(display, -0.14f, 0f, -0.14f, 0.14f, 0.005f, 0.14f),
        });
    }

    private static void BuildShowerBottles(Material a, Material b, Material caps)
    {
        Save("ShowerBottles", "bottles", new List<LowPolyBoxMesh.Part>
        {
            Box(a, -0.10f, 0f, -0.03f, -0.035f, 0.22f, 0.03f),
            Box(caps, -0.08f, 0.22f, -0.015f, -0.055f, 0.25f, 0.015f),
            Box(b, -0.025f, 0f, -0.025f, 0.03f, 0.18f, 0.025f),
            Box(caps, -0.01f, 0.18f, -0.012f, 0.015f, 0.20f, 0.012f),
            Box(caps, 0.04f, 0f, -0.03f, 0.10f, 0.15f, 0.03f),
            Box(a, 0.055f, 0.15f, -0.01f, 0.085f, 0.17f, 0.01f),
        });
    }

    private static void BuildPedalBin(Material body, Material dark)
    {
        Save("PedalBin", "bin", new List<LowPolyBoxMesh.Part>
        {
            new(body, new Vector3(0f, 0.14f, 0f), new Vector3(0.22f, 0.28f, 0.18f), Quaternion.identity),
            Box(body, -0.115f, 0.28f, -0.095f, 0.115f, 0.30f, 0.095f),
            Box(dark, -0.05f, 0.005f, 0.09f, 0.05f, 0.02f, 0.13f),
        });
    }
}
