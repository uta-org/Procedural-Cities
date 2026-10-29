using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the beam pergola over a restaurant's storefronts (P42.3, owner Q-20261029-4 A): a beam
/// (<c>PergolaBeam</c>, 0.08 x 0.16 m, 1 m long along +Z from the wall, its pivot on its bottom
/// face at the wall end; the city stretches it to the pergola's depth) and a header
/// (<c>PergolaHeader</c>, 0.10 x 0.20 m, 1 m long along X, its pivot on its bottom face at the
/// centre; the city stretches it along the facade, as the wall plate and as the front beam).
/// Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Pergola
/// </summary>
public static class GeneratePergola
{
    [MenuItem("Tools/Procedural Cities/Generate Pergola")]
    public static void Generate()
    {
        var wood = LowPolyBoxMesh.GetMat("LP_PG_Wood", new Color(0.45f, 0.30f, 0.18f), 0f, 0.25f);
        var end = LowPolyBoxMesh.GetMat("LP_PG_WoodEnd", new Color(0.36f, 0.23f, 0.13f), 0f, 0.2f);

        // The beam, its outer end cut at a slant like a rafter tail.
        Save("PergolaBeam", "pergola_beam", new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.04f, 0f, 0f, 0.04f, 0.16f, 0.94f),
            new(wood, new Vector3(0f, 0.11f, 0.955f), new Vector3(0.08f, 0.10f, 0.08f), Quaternion.Euler(30f, 0f, 0f)),
            Box(end, -0.041f, 0.02f, 0.94f, 0.041f, 0.12f, 0.95f),
        });
        Save("PergolaHeader", "pergola_header", new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.5f, 0f, -0.05f, 0.5f, 0.20f, 0.05f),
        });
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
}
