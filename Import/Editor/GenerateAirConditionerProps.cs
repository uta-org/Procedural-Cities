using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the air conditioner (P37): AirConditionerIndoor, a white wall-mounted split unit
/// 0.85 x 0.29 x 0.22 m, pivot at its centre, front +Z (back against the wall), with a grey
/// air outlet and louvre along the bottom of its front; and AirConditionerOutdoor, the
/// outdoor unit 0.80 x 0.55 x 0.28 m, pivot on its bottom face at the centre, front +Z,
/// light-grey casing, a dark fan grille with a cross guard on the front and two wall
/// brackets under it; and CeilingVent (P37.4), an office ceiling diffuser 0.60 m square,
/// its top face at the pivot (it hangs from the slab like the ceiling panels), a white frame
/// round three nested darker rings. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Air Conditioner Props
/// </summary>
public static class GenerateAirConditionerProps
{
    [MenuItem("Tools/Procedural Cities/Generate Air Conditioner Props")]
    public static void Generate()
    {
        var white = LowPolyBoxMesh.GetMat("LP_AirCon_White", new Color(0.95f, 0.95f, 0.94f), 0f, 0.5f);
        var casing = LowPolyBoxMesh.GetMat("LP_AirCon_Casing", new Color(0.80f, 0.81f, 0.80f), 0.2f, 0.35f);
        var dark = LowPolyBoxMesh.LoadMat("LP_Metal_DarkGray");
        if (!dark)
        {
            Debug.LogError("[GenerateAirConditionerProps] Missing LP_Metal_DarkGray.");
            return;
        }

        BuildIndoor(white, dark);
        BuildOutdoor(casing, dark);
        BuildCeilingVent(white, LowPolyBoxMesh.GetMat("LP_Vent_Grille", new Color(0.62f, 0.64f, 0.66f), 0.2f, 0.3f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildIndoor(Material white, Material dark)
    {
        const float w = 0.85f, h = 0.29f, d = 0.22f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            LowPolyBoxMesh.Part.FromMinMax(white, new Vector3(-w * 0.5f, -h * 0.5f + 0.05f, -d * 0.5f), new Vector3(w * 0.5f, h * 0.5f, d * 0.5f)),
            // Lower body, set back, with the outlet slot and its louvre.
            LowPolyBoxMesh.Part.FromMinMax(white, new Vector3(-w * 0.5f, -h * 0.5f, -d * 0.5f), new Vector3(w * 0.5f, -h * 0.5f + 0.05f, d * 0.5f - 0.06f)),
            LowPolyBoxMesh.Part.FromMinMax(dark, new Vector3(-w * 0.5f + 0.04f, -h * 0.5f + 0.005f, d * 0.5f - 0.06f), new Vector3(w * 0.5f - 0.04f, -h * 0.5f + 0.05f, d * 0.5f - 0.05f)),
            new(white, new Vector3(0f, -h * 0.5f + 0.03f, d * 0.5f - 0.035f), new Vector3(w - 0.1f, 0.006f, 0.05f), Quaternion.Euler(-25f, 0f, 0f)),
        };
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("AirConditionerIndoor_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab("AirConditionerIndoor", "indoor", mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildCeilingVent(Material frame, Material grille)
    {
        const float side = 0.60f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            // Frame flush with the ceiling, then rings stepping down and in.
            LowPolyBoxMesh.Part.FromMinMax(frame, new Vector3(-side * 0.5f, -0.012f, -side * 0.5f), new Vector3(side * 0.5f, 0f, side * 0.5f)),
        };
        for (var i = 1; i <= 3; i++)
        {
            var half = side * 0.5f - i * 0.065f;
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(i % 2 == 1 ? grille : frame,
                new Vector3(-half, -0.012f - i * 0.008f, -half), new Vector3(half, -0.012f - (i - 1) * 0.008f, half)));
        }

        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("CeilingVent_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab("CeilingVent", "vent", mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildOutdoor(Material casing, Material dark)
    {
        const float w = 0.80f, h = 0.55f, d = 0.28f;
        var front = d * 0.5f;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            LowPolyBoxMesh.Part.FromMinMax(casing, new Vector3(-w * 0.5f, 0f, -d * 0.5f), new Vector3(w * 0.5f, h, front)),
            // Fan grille on the two thirds of the front on the viewer's left (+X seen from the front), and its cross guard.
            LowPolyBoxMesh.Part.FromMinMax(dark, new Vector3(-w * 0.5f + 0.27f, 0.07f, front), new Vector3(w * 0.5f - 0.05f, h - 0.07f, front + 0.004f)),
        };
        var fanX = (-w * 0.5f + 0.27f + w * 0.5f - 0.05f) * 0.5f;
        parts.Add(new LowPolyBoxMesh.Part(casing, new Vector3(fanX, h * 0.5f, front + 0.008f), new Vector3(0.42f, 0.015f, 0.008f), Quaternion.Euler(0f, 0f, 45f)));
        parts.Add(new LowPolyBoxMesh.Part(casing, new Vector3(fanX, h * 0.5f, front + 0.008f), new Vector3(0.42f, 0.015f, 0.008f), Quaternion.Euler(0f, 0f, -45f)));
        // Brackets under the unit, back to the wall.
        foreach (var sx in new[] { -1f, 1f })
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(dark, new Vector3(sx * 0.3f - 0.015f, -0.04f, -d * 0.5f - 0.02f), new Vector3(sx * 0.3f + 0.015f, 0f, front)));

        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("AirConditionerOutdoor_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab("AirConditionerOutdoor", "outdoor", mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }
}
