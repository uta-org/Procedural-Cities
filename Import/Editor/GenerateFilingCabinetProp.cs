using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds FilingCabinet (P33): a low-poly four-drawer office filing cabinet, 0.47 m wide,
/// 0.62 m deep and 1.32 m tall, pivot on the floor at its centre, front +Z (its back goes
/// against a wall, as the kitchen modules). Light-grey steel body, four drawer fronts
/// standing 0.012 m proud with a gap between them, a chrome pull on each.
/// Output: Models/LowPoly/FilingCabinet_Combined.asset (one submesh per material) and
/// Resources/Prefabs/AssetContents/FilingCabinet.prefab, rewritten in place so their GUIDs
/// stay. Menu: Tools / Procedural Cities / Generate Filing Cabinet Prop
/// </summary>
public static class GenerateFilingCabinetProp
{
    private const float Width = 0.47f;
    private const float Depth = 0.62f;
    private const float Height = 1.32f;
    private const float Plinth = 0.04f;
    private const int Drawers = 4;
    private const float DrawerGap = 0.012f;
    private const float DrawerRelief = 0.012f;

    [MenuItem("Tools/Procedural Cities/Generate Filing Cabinet Prop")]
    public static void Generate()
    {
        var steel = LowPolyBoxMesh.GetMat("LP_FilingCabinet_Steel", new Color(0.66f, 0.68f, 0.70f), 0.35f, 0.45f);
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        var dark = LowPolyBoxMesh.LoadMat("LP_Metal_DarkGray");
        if (!chrome || !dark)
        {
            Debug.LogError("[GenerateFilingCabinetProp] Missing LP_Chrome / LP_Metal_DarkGray.");
            return;
        }

        var parts = new List<LowPolyBoxMesh.Part>();
        var hw = Width * 0.5f;
        var hd = Depth * 0.5f;

        // Body on a dark plinth set back from the front.
        parts.Add(LowPolyBoxMesh.Part.FromMinMax(dark, new Vector3(-hw + 0.02f, 0f, -hd + 0.02f), new Vector3(hw - 0.02f, Plinth, hd - 0.04f)));
        parts.Add(LowPolyBoxMesh.Part.FromMinMax(steel, new Vector3(-hw, Plinth, -hd), new Vector3(hw, Height, hd)));

        // Drawer fronts and their pulls.
        var drawerHeight = (Height - Plinth - DrawerGap * (Drawers + 1)) / Drawers;
        for (var i = 0; i < Drawers; i++)
        {
            var bottom = Plinth + DrawerGap + i * (drawerHeight + DrawerGap);
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(steel, new Vector3(-hw + 0.015f, bottom, hd), new Vector3(hw - 0.015f, bottom + drawerHeight, hd + DrawerRelief)));
            var pullY = bottom + drawerHeight * 0.62f;
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(chrome, new Vector3(-0.07f, pullY, hd + DrawerRelief), new Vector3(0.07f, pullY + 0.02f, hd + DrawerRelief + 0.025f)));
            // Label holder above the pull.
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(dark, new Vector3(-0.045f, pullY + 0.05f, hd + DrawerRelief), new Vector3(0.045f, pullY + 0.085f, hd + DrawerRelief + 0.004f)));
        }

        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("FilingCabinet_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab("FilingCabinet", "filingcabinet", mesh, materials,
            new Vector3(0f, Height * 0.5f, DrawerRelief * 0.5f), new Vector3(Width, Height, Depth + DrawerRelief));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
