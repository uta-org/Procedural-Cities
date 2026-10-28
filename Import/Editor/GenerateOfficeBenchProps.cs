using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the open-office bench desk and the laptop (P34): OfficeBenchTopWhite and
/// OfficeBenchTopCream, 1.00 x 1.00 m tops 0.03 m thick with their upper face at 0.75 m,
/// which the city scales in X/Z to the desk's length and depth (white, or a very light
/// yellow); OfficeBenchLeg, a 0.05 m dark-grey leg up to the top's underside; and Laptop,
/// an open low-poly laptop 0.34 m wide whose screen faces local -Z (towards the seat, as the
/// monitor's), its pivot under the middle of the base. Pivots on the floor (the laptop's on
/// the desk top). Menu: Tools / Procedural Cities / Generate Office Bench Props
/// </summary>
public static class GenerateOfficeBenchProps
{
    /// <summary>Height of a bench top's upper face (m).</summary>
    public const float TopHeight = 0.75f;

    private const float TopThickness = 0.03f;
    private const float LegSide = 0.05f;

    [MenuItem("Tools/Procedural Cities/Generate Office Bench Props")]
    public static void Generate()
    {
        var white = LowPolyBoxMesh.GetMat("LP_Desk_White", new Color(0.92f, 0.92f, 0.90f), 0f, 0.35f);
        var cream = LowPolyBoxMesh.GetMat("LP_Desk_Cream", new Color(0.95f, 0.92f, 0.78f), 0f, 0.35f);
        var legMetal = LowPolyBoxMesh.LoadMat("LP_Metal_DarkGray");
        var screen = LowPolyBoxMesh.LoadMat("LP_TV_Screen");
        var frame = LowPolyBoxMesh.LoadMat("LP_TV_Frame");
        if (!legMetal || !screen || !frame)
        {
            Debug.LogError("[GenerateOfficeBenchProps] Missing LP_Metal_DarkGray / LP_TV_Screen / LP_TV_Frame.");
            return;
        }

        var topCenter = new Vector3(0f, TopHeight - TopThickness * 0.5f, 0f);
        var topSize = new Vector3(1f, TopThickness, 1f);
        foreach (var (name, material) in new[] { ("OfficeBenchTopWhite", white), ("OfficeBenchTopCream", cream) })
        {
            var parts = new List<LowPolyBoxMesh.Part> { new(material, topCenter, topSize, Quaternion.identity) };
            var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine($"{name}_Combined", parts, out var materials));
            LowPolyBoxMesh.SavePrefab(name, "top", mesh, materials, topCenter, topSize);
        }

        var legHeight = TopHeight - TopThickness;
        var legCenter = new Vector3(0f, legHeight * 0.5f, 0f);
        var legSize = new Vector3(LegSide, legHeight, LegSide);
        {
            var parts = new List<LowPolyBoxMesh.Part> { new(legMetal, legCenter, legSize, Quaternion.identity) };
            var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("OfficeBenchLeg_Combined", parts, out var materials));
            LowPolyBoxMesh.SavePrefab("OfficeBenchLeg", "leg", mesh, materials, legCenter, legSize);
        }

        BuildLaptop(frame, screen);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>An open laptop: base with a darker keyboard plate, the lid tilted 15° back from upright.</summary>
    private static void BuildLaptop(Material body, Material screen)
    {
        const float width = 0.34f;
        const float depth = 0.24f;
        const float baseHeight = 0.018f;
        const float lidHeight = 0.22f;
        const float lidThickness = 0.008f;
        var keys = LowPolyBoxMesh.GetMat("LP_Laptop_Keys", new Color(0.10f, 0.10f, 0.11f), 0f, 0.2f);

        var parts = new List<LowPolyBoxMesh.Part>
        {
            LowPolyBoxMesh.Part.FromMinMax(body, new Vector3(-width * 0.5f, 0f, -depth * 0.5f), new Vector3(width * 0.5f, baseHeight, depth * 0.5f)),
            LowPolyBoxMesh.Part.FromMinMax(keys, new Vector3(-width * 0.5f + 0.02f, baseHeight, -0.01f), new Vector3(width * 0.5f - 0.02f, baseHeight + 0.002f, depth * 0.5f - 0.02f)),
        };

        // Lid hinged at the back edge (+Z), leaning back; the screen on its front face.
        var lidRotation = Quaternion.Euler(15f, 0f, 0f);
        var hinge = new Vector3(0f, baseHeight, depth * 0.5f - lidThickness * 0.5f);
        var lidCenter = hinge + lidRotation * new Vector3(0f, lidHeight * 0.5f, 0f);
        parts.Add(new LowPolyBoxMesh.Part(body, lidCenter, new Vector3(width, lidHeight, lidThickness), lidRotation));
        var screenCenter = lidCenter + lidRotation * new Vector3(0f, 0.005f, -lidThickness * 0.5f - 0.001f);
        parts.Add(new LowPolyBoxMesh.Part(screen, screenCenter, new Vector3(width - 0.03f, lidHeight - 0.03f, 0.002f), lidRotation));

        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine("Laptop_Combined", parts, out var materials));
        var bounds = mesh.bounds;
        LowPolyBoxMesh.SavePrefab("Laptop", "laptop", mesh, materials, bounds.center, bounds.size);
    }
}
