using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the meeting room props (P40.12, owner-approved list Q-20261028-9): CeilingProjector
/// (pivot on its ceiling plate, hanging down), ProjectorScreen (wall-hung, back face at Z = 0),
/// WaterBottleGlass, NotepadPen (front +Z to the person sitting at it), ConferenceSpeaker,
/// FlipChart and WaterJug. The laptops, remote, coat stand and bin reuse Laptop, TvRemote, Hanger
/// and the room's rubbish bin. Table and floor pieces have their pivot on the bottom face at the
/// centre, front +Z. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Meeting Props
/// </summary>
public static class GenerateMeetingProps
{
    [MenuItem("Tools/Procedural Cities/Generate Meeting Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateMeetingProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_MT_Black", new Color(0.11f, 0.11f, 0.12f), 0.1f, 0.5f);
        var white = LowPolyBoxMesh.GetMat("LP_MT_White", new Color(0.95f, 0.95f, 0.94f), 0f, 0.5f);
        var glass = LowPolyBoxMesh.GetMat("LP_MT_Glass", new Color(0.80f, 0.90f, 0.94f), 0f, 0.95f);
        var water = LowPolyBoxMesh.GetMat("LP_MT_Water", new Color(0.55f, 0.78f, 0.90f), 0f, 0.9f);

        BuildCeilingProjector(white, black, glass);
        BuildProjectorScreen(white, black);
        BuildWaterBottleGlass(LowPolyBoxMesh.GetMat("LP_MT_Bottle", new Color(0.60f, 0.82f, 0.92f), 0f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_MT_Cap", new Color(0.15f, 0.35f, 0.70f), 0f, 0.5f), glass, water);
        BuildNotepadPen(LowPolyBoxMesh.GetMat("LP_MT_Paper", new Color(0.97f, 0.96f, 0.88f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_MT_Lines", new Color(0.55f, 0.65f, 0.80f), 0f, 0.2f), black, chrome);
        BuildConferenceSpeaker(black, LowPolyBoxMesh.GetMat("LP_MT_Led", new Color(0.20f, 0.80f, 0.40f), 0f, 0.8f));
        BuildFlipChart(chrome, white, LowPolyBoxMesh.GetMat("LP_MT_Marker", new Color(0.70f, 0.15f, 0.15f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_MT_MarkerBlue", new Color(0.15f, 0.30f, 0.65f), 0f, 0.3f));
        BuildWaterJug(glass, water);
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

    private static void BuildCeilingProjector(Material body, Material dark, Material lens)
    {
        // Pivot on the ceiling plate: a 0.30 m pole down to a 0.32 x 0.10 x 0.28 m projector, lens to +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(dark, -0.08f, -0.02f, -0.08f, 0.08f, 0f, 0.08f),
            Box(dark, -0.02f, -0.30f, -0.02f, 0.02f, -0.02f, 0.02f),
            Box(dark, -0.10f, -0.32f, -0.06f, 0.10f, -0.30f, 0.06f),
            Box(body, -0.16f, -0.42f, -0.14f, 0.16f, -0.32f, 0.14f),
            Box(dark, -0.15f, -0.415f, -0.14f, -0.08f, -0.33f, -0.141f),
        };
        Round(parts, lens, 0.07f, -0.40f, 0.145f, 0.06f, -0.34f);
        Save("CeilingProjector", "ceiling_projector", parts);
    }

    private static void BuildProjectorScreen(Material screen, Material dark)
    {
        // Wall-hung roll-down screen, 1.80 x 1.40 m with its case on top, back at Z = 0.
        Save("ProjectorScreen", "projector_screen", new List<LowPolyBoxMesh.Part>
        {
            Box(dark, -0.95f, 1.40f, 0f, 0.95f, 1.50f, 0.10f),
            Box(screen, -0.90f, 0.05f, 0.04f, 0.90f, 1.40f, 0.045f),
            Box(dark, -0.90f, 1.30f, 0.045f, 0.90f, 1.40f, 0.047f),
            Box(dark, -0.92f, 0f, 0.03f, 0.92f, 0.05f, 0.06f),
            Box(dark, -0.03f, -0.10f, 0.04f, 0.03f, 0f, 0.05f),
        });
    }

    private static void BuildWaterBottleGlass(Material bottle, Material cap, Material glass, Material water)
    {
        // A 0.5 l bottle and a glass beside it.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, bottle, -0.04f, 0f, 0f, 0.065f, 0.17f);
        Round(parts, bottle, -0.04f, 0.17f, 0f, 0.03f, 0.20f);
        Round(parts, cap, -0.04f, 0.20f, 0f, 0.032f, 0.215f);
        Round(parts, glass, 0.05f, 0f, 0f, 0.065f, 0.11f);
        Round(parts, water, 0.05f, 0.005f, 0f, 0.058f, 0.07f);
        Save("WaterBottleGlass", "water_bottle_glass", parts);
    }

    private static void BuildNotepadPen(Material paper, Material lines, Material cover, Material pen)
    {
        // An A5 pad, 0.15 x 0.21 m, and a pen beside it on the right of the person at +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(cover, -0.075f, 0f, -0.105f, 0.075f, 0.004f, 0.105f),
            Box(paper, -0.072f, 0.004f, -0.10f, 0.072f, 0.012f, 0.102f),
            Box(cover, -0.075f, 0.004f, -0.105f, 0.075f, 0.014f, -0.095f),
        };
        for (var i = 0; i < 5; i++)
            parts.Add(Box(lines, -0.06f, 0.012f, -0.07f + i * 0.035f, 0.06f, 0.0125f, -0.067f + i * 0.035f));
        parts.Add(new LowPolyBoxMesh.Part(pen, new Vector3(-0.10f, 0.005f, 0f), new Vector3(0.01f, 0.01f, 0.14f), Quaternion.Euler(0f, 8f, 0f)));
        Save("NotepadPen", "notepad_pen", parts);
    }

    private static void BuildConferenceSpeaker(Material body, Material led)
    {
        // A low three-armed conference phone, 0.30 m across.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, body, 0f, 0f, 0f, 0.12f, 0.045f);
        for (var i = 0; i < 3; i++)
        {
            var rotation = Quaternion.Euler(0f, i * 120f, 0f);
            parts.Add(new LowPolyBoxMesh.Part(body, rotation * new Vector3(0f, 0.015f, 0.09f), new Vector3(0.07f, 0.03f, 0.13f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(led, rotation * new Vector3(0f, 0.031f, 0.13f), new Vector3(0.02f, 0.003f, 0.02f), rotation));
        }

        Save("ConferenceSpeaker", "conference_speaker", parts);
    }

    private static void BuildFlipChart(Material metal, Material paper, Material red, Material blue)
    {
        // A tripod easel, 0.70 m wide and 1.85 m high, the pad leaning back a little, front +Z.
        var lean = Quaternion.Euler(-8f, 0f, 0f);
        var parts = new List<LowPolyBoxMesh.Part>
        {
            new(metal, new Vector3(-0.30f, 0.90f, 0.05f), new Vector3(0.025f, 1.80f, 0.025f), Quaternion.Euler(-8f, 0f, 3f)),
            new(metal, new Vector3(0.30f, 0.90f, 0.05f), new Vector3(0.025f, 1.80f, 0.025f), Quaternion.Euler(-8f, 0f, -3f)),
            new(metal, new Vector3(0f, 0.75f, -0.10f), new Vector3(0.025f, 1.55f, 0.025f), Quaternion.Euler(10f, 0f, 0f)),
            new(metal, new Vector3(0f, 0.92f, 0.05f), new Vector3(0.66f, 0.05f, 0.06f), lean),
            new(paper, new Vector3(0f, 1.35f, 0.025f), new Vector3(0.66f, 0.85f, 0.02f), lean),
            new(red, new Vector3(-0.10f, 1.55f, 0.04f), new Vector3(0.40f, 0.025f, 0.004f), lean),
            new(blue, new Vector3(-0.05f, 1.42f, 0.038f), new Vector3(0.48f, 0.02f, 0.004f), lean),
            new(blue, new Vector3(-0.12f, 1.32f, 0.036f), new Vector3(0.32f, 0.02f, 0.004f), lean),
            new(red, new Vector3(0.12f, 1.18f, 0.034f), new Vector3(0.18f, 0.12f, 0.004f), lean),
        };
        Save("FlipChart", "flip_chart", parts);
    }

    private static void BuildWaterJug(Material glass, Material water)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, glass, 0f, 0f, 0f, 0.12f, 0.22f);
        Round(parts, water, 0f, 0.005f, 0f, 0.11f, 0.16f);
        parts.Add(Box(glass, 0.06f, 0.05f, -0.012f, 0.09f, 0.18f, 0.012f));
        parts.Add(Box(glass, -0.07f, 0.19f, -0.02f, -0.05f, 0.22f, 0.02f));
        Save("WaterJug", "water_jug", parts);
    }
}
