using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds Radiator (P36): a low-poly white column radiator 1.00 m wide, 0.60 m tall and
/// 0.10 m deep, hung 0.12 m above the floor, pivot on the floor at its centre, front +Z
/// (its back, with two wall brackets, against a wall, as the kitchen modules). Twenty
/// columns over a top and a bottom header, a valve and a pipe stub at each lower end.
/// Output: Models/LowPoly/Radiator_Combined.asset and
/// Resources/Prefabs/AssetContents/Radiator.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Radiator Prop
/// </summary>
public static class GenerateRadiatorProp
{
    private const float Width = 1.0f;
    private const float Height = 0.60f;
    private const float Depth = 0.10f;
    private const float Lift = 0.12f;
    private const int Columns = 20;
    private const float HeaderHeight = 0.05f;

    [MenuItem("Tools/Procedural Cities/Generate Radiator Prop")]
    public static void Generate()
    {
        var enamel = LowPolyBoxMesh.GetMat("LP_Radiator_White", new Color(0.93f, 0.93f, 0.91f), 0.1f, 0.55f);
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateRadiatorProp] Missing LP_Chrome.");
            return;
        }

        var hw = Width * 0.5f;
        var back = -Depth * 0.5f;
        var front = Depth * 0.5f;
        var bottom = Lift;
        var top = Lift + Height;
        var parts = new List<LowPolyBoxMesh.Part>
        {
            // Headers along the top and the bottom, set back a little.
            LowPolyBoxMesh.Part.FromMinMax(enamel, new Vector3(-hw, top - HeaderHeight, back + 0.015f), new Vector3(hw, top, front - 0.015f)),
            LowPolyBoxMesh.Part.FromMinMax(enamel, new Vector3(-hw, bottom, back + 0.015f), new Vector3(hw, bottom + HeaderHeight, front - 0.015f)),
        };

        // Columns, full depth, with a gap between each.
        var pitch = Width / Columns;
        for (var i = 0; i < Columns; i++)
        {
            var x0 = -hw + i * pitch + pitch * 0.15f;
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(enamel, new Vector3(x0, bottom, back), new Vector3(x0 + pitch * 0.7f, top, front)));
        }

        // Wall brackets behind, a valve and a pipe stub to the floor at each lower end.
        foreach (var sx in new[] { -1f, 1f })
        {
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(enamel, new Vector3(sx * 0.3f - 0.02f, top - 0.12f, back - 0.03f), new Vector3(sx * 0.3f + 0.02f, top - 0.08f, back)));
            var x = sx * (hw + 0.025f);
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(chrome, new Vector3(x - 0.02f, bottom, -0.02f), new Vector3(x + 0.02f, bottom + 0.06f, 0.02f)));
            parts.Add(LowPolyBoxMesh.Part.FromMinMax(chrome, new Vector3(x - 0.008f, 0f, -0.008f), new Vector3(x + 0.008f, bottom, 0.008f)));
        }

        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("Radiator_Combined", parts, out var materials));
        var bounds = mesh.bounds;
        LowPolyBoxMesh.SavePrefab("Radiator", "radiator", mesh, materials, bounds.center, bounds.size);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
