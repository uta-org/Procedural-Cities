using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the corridor props (P40.10, owner-approved list Q-20261028-9): FireExtinguisher,
/// ExitSign, EvacuationPlan, GelDispenser and Mailboxes (wall-hung, back face at Z = 0, like
/// Mirror), CorridorBench, UmbrellaStand, Doormat and RunnerRug (on the floor, pivot on the
/// bottom face at the centre, front +Z). The bin reuses PedalBin. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Corridor Props
/// </summary>
public static class GenerateCorridorProps
{
    [MenuItem("Tools/Procedural Cities/Generate Corridor Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateCorridorProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_CO_Black", new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.4f);
        var white = LowPolyBoxMesh.GetMat("LP_CO_White", new Color(0.95f, 0.95f, 0.93f), 0f, 0.5f);
        var red = LowPolyBoxMesh.GetMat("LP_CO_Red", new Color(0.78f, 0.10f, 0.08f), 0.2f, 0.6f);
        var green = LowPolyBoxMesh.GetMat("LP_CO_Green", new Color(0.10f, 0.60f, 0.30f), 0f, 0.6f);

        BuildFireExtinguisher(red, black, chrome, white);
        BuildExitSign(green, white);
        BuildEvacuationPlan(LowPolyBoxMesh.GetMat("LP_CO_Plan", new Color(0.92f, 0.92f, 0.88f), 0f, 0.2f), green, red, chrome);
        BuildGelDispenser(white, LowPolyBoxMesh.GetMat("LP_CO_Gel", new Color(0.55f, 0.80f, 0.90f), 0f, 0.8f), black);
        BuildMailboxes(LowPolyBoxMesh.GetMat("LP_CO_Mailbox", new Color(0.58f, 0.60f, 0.62f), 0.6f, 0.5f), black, white);
        BuildCorridorBench(LowPolyBoxMesh.GetMat("LP_CO_Wood", new Color(0.62f, 0.45f, 0.28f), 0f, 0.35f), black);
        BuildUmbrellaStand(black, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CO_UmbrellaNavy", new Color(0.15f, 0.20f, 0.40f), 0f, 0.3f), red, black,
        }, chrome);
        BuildDoormat(LowPolyBoxMesh.GetMat("LP_CO_Coir", new Color(0.55f, 0.40f, 0.25f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_CO_CoirDark", new Color(0.30f, 0.22f, 0.15f), 0f, 0.05f));
        BuildRunnerRug(LowPolyBoxMesh.GetMat("LP_CO_Runner", new Color(0.45f, 0.18f, 0.16f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_CO_RunnerBorder", new Color(0.82f, 0.72f, 0.52f), 0f, 0.05f));
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

    private static void BuildFireExtinguisher(Material red, Material black, Material metal, Material label)
    {
        // Wall-hung: a bracket on the wall (Z = 0) and the 0.55 m cylinder in front of it, with its sign above.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(metal, -0.05f, 0.10f, 0f, 0.05f, 0.40f, 0.015f),
            Box(red, -0.10f, 0.62f, 0f, 0.10f, 0.82f, 0.008f),
            Box(label, -0.07f, 0.66f, 0.008f, 0.07f, 0.78f, 0.010f),
        };
        Round(parts, red, 0f, 0f, 0.08f, 0.15f, 0.48f);
        parts.Add(Box(black, -0.025f, 0.48f, 0.065f, 0.025f, 0.54f, 0.095f));
        parts.Add(Box(black, 0.0f, 0.52f, 0.07f, 0.09f, 0.535f, 0.09f));
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(-0.06f, 0.42f, 0.14f), new Vector3(0.018f, 0.20f, 0.018f), Quaternion.Euler(0f, 0f, -20f)));
        parts.Add(Box(label, -0.05f, 0.18f, 0.154f, 0.05f, 0.30f, 0.156f));
        Save("FireExtinguisher", "fire_extinguisher", parts);
    }

    private static void BuildExitSign(Material green, Material white)
    {
        // Wall-hung light box, 0.36 x 0.14 m, a running figure and an arrow in white.
        Save("ExitSign", "exit_sign", new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.18f, 0f, 0f, 0.18f, 0.14f, 0.05f),
            Box(green, -0.17f, 0.01f, 0.05f, 0.17f, 0.13f, 0.052f),
            Box(white, -0.13f, 0.03f, 0.052f, -0.11f, 0.11f, 0.054f),
            Box(white, -0.14f, 0.09f, 0.052f, -0.10f, 0.11f, 0.054f),
            Box(white, -0.05f, 0.065f, 0.052f, 0.10f, 0.075f, 0.054f),
            new(white, new Vector3(0.11f, 0.08f, 0.053f), new Vector3(0.04f, 0.01f, 0.002f), Quaternion.Euler(0f, 0f, -40f)),
            new(white, new Vector3(0.11f, 0.06f, 0.053f), new Vector3(0.04f, 0.01f, 0.002f), Quaternion.Euler(0f, 0f, 40f)),
        });
    }

    private static void BuildEvacuationPlan(Material paper, Material green, Material red, Material frame)
    {
        // Wall-hung framed plan, 0.42 x 0.30 m: corridors in green, "you are here" in red.
        Save("EvacuationPlan", "evacuation_plan", new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.21f, 0f, 0f, 0.21f, 0.30f, 0.012f),
            Box(paper, -0.195f, 0.015f, 0.012f, 0.195f, 0.285f, 0.014f),
            Box(green, -0.17f, 0.13f, 0.014f, 0.17f, 0.15f, 0.015f),
            Box(green, -0.01f, 0.04f, 0.014f, 0.01f, 0.25f, 0.015f),
            Box(green, 0.12f, 0.06f, 0.014f, 0.14f, 0.13f, 0.015f),
            Box(red, -0.09f, 0.16f, 0.014f, -0.07f, 0.18f, 0.016f),
            Box(green, -0.18f, 0.25f, 0.014f, -0.10f, 0.27f, 0.015f),
        });
    }

    private static void BuildGelDispenser(Material body, Material gel, Material black)
    {
        // Wall-hung, 0.12 x 0.24 x 0.10 m, a window on the gel and a lever under it.
        Save("GelDispenser", "gel_dispenser", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.06f, 0f, 0f, 0.06f, 0.24f, 0.10f),
            Box(gel, -0.04f, 0.08f, 0.10f, 0.04f, 0.18f, 0.102f),
            Box(black, -0.045f, 0.02f, 0.10f, 0.045f, 0.05f, 0.115f),
        });
    }

    private static void BuildMailboxes(Material metal, Material black, Material label)
    {
        // Wall-hung bank of 4 x 3 letter boxes, 0.96 x 0.66 x 0.28 m.
        var parts = new List<LowPolyBoxMesh.Part> { Box(metal, -0.48f, 0f, 0f, 0.48f, 0.66f, 0.27f) };
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                var x = -0.36f + col * 0.24f;
                var y = 0.11f + row * 0.22f;
                parts.Add(Box(metal, x - 0.11f, y - 0.10f, 0.27f, x + 0.11f, y + 0.10f, 0.28f));
                parts.Add(Box(black, x - 0.07f, y + 0.04f, 0.28f, x + 0.07f, y + 0.06f, 0.283f));
                parts.Add(Box(label, x - 0.04f, y - 0.03f, 0.28f, x + 0.04f, y, 0.282f));
                parts.Add(Box(black, x + 0.07f, y - 0.06f, 0.28f, x + 0.085f, y - 0.045f, 0.29f));
            }
        }

        Save("Mailboxes", "mailboxes", parts);
    }

    private static void BuildCorridorBench(Material wood, Material metal)
    {
        // 1.20 x 0.45 x 0.40 m, slatted seat on two metal frames, back at -Z.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 4; i++)
            parts.Add(Box(wood, -0.60f, 0.42f, -0.20f + i * 0.10f, 0.60f, 0.45f, -0.20f + i * 0.10f + 0.085f));
        foreach (var sx in new[] { -0.50f, 0.50f })
        {
            parts.Add(Box(metal, sx - 0.02f, 0f, -0.19f, sx + 0.02f, 0.42f, -0.16f));
            parts.Add(Box(metal, sx - 0.02f, 0f, 0.16f, sx + 0.02f, 0.42f, 0.19f));
            parts.Add(Box(metal, sx - 0.02f, 0.38f, -0.19f, sx + 0.02f, 0.42f, 0.19f));
            parts.Add(Box(metal, sx - 0.02f, 0f, -0.19f, sx + 0.02f, 0.03f, 0.19f));
        }

        Save("CorridorBench", "corridor_bench", parts);
    }

    private static void BuildUmbrellaStand(Material stand, Material[] umbrellas, Material metal)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, stand, 0f, 0f, 0f, 0.24f, 0.50f);
        Round(parts, metal, 0f, 0.48f, 0f, 0.25f, 0.50f);
        var tilt = new[] { new Vector3(8f, 0f, -6f), new Vector3(-6f, 0f, 9f), new Vector3(4f, 0f, 4f) };
        var offsets = new[] { new Vector3(-0.05f, 0f, -0.03f), new Vector3(0.05f, 0f, 0.02f), new Vector3(0f, 0f, 0.05f) };
        for (var i = 0; i < 3; i++)
        {
            var rotation = Quaternion.Euler(tilt[i]);
            var up = rotation * Vector3.up;
            parts.Add(new LowPolyBoxMesh.Part(umbrellas[i], offsets[i] + up * 0.48f + new Vector3(0f, 0.02f, 0f), new Vector3(0.07f, 0.70f, 0.07f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(metal, offsets[i] + up * 0.88f + new Vector3(0f, 0.02f, 0f), new Vector3(0.012f, 0.10f, 0.012f), rotation));
            parts.Add(new LowPolyBoxMesh.Part(stand, offsets[i] + up * 0.93f + new Vector3(0.025f, 0.02f, 0f), new Vector3(0.06f, 0.02f, 0.02f), rotation));
        }

        Save("UmbrellaStand", "umbrella_stand", parts);
    }

    private static void BuildDoormat(Material coir, Material dark)
    {
        // 0.70 x 0.45 m, flat, a dark border; its long side along X (the door's width).
        Save("Doormat", "doormat", new List<LowPolyBoxMesh.Part>
        {
            Box(dark, -0.35f, 0f, -0.225f, 0.35f, 0.012f, 0.225f),
            Box(coir, -0.31f, 0.012f, -0.185f, 0.31f, 0.015f, 0.185f),
        });
    }

    private static void BuildRunnerRug(Material rug, Material border)
    {
        // 2.40 x 0.70 m, flat, along X.
        Save("RunnerRug", "runner_rug", new List<LowPolyBoxMesh.Part>
        {
            Box(border, -1.20f, 0f, -0.35f, 1.20f, 0.008f, 0.35f),
            Box(rug, -1.13f, 0.008f, -0.28f, 1.13f, 0.012f, 0.28f),
            Box(border, -1.00f, 0.012f, -0.02f, 1.00f, 0.013f, 0.02f),
        });
    }
}
