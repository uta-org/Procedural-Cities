using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the restaurants' illuminated signs (P88.6, P88.7):
/// <list type="bullet">
/// <item>RestaurantPylonSign: a tall sign on a pole for the one-storey restaurant buildings, seen from
/// afar, tall enough to be seen from a distance: a 7.2 m pole on a base, a lit box
/// 2.4 x 1.3 m with a plate and a knife and fork on both faces. Pivot at the foot of the pole, faces +Z
/// and -Z.</item>
/// <item>RestaurantNeonSign: a flat neon sign for over a restaurant's door:
/// a dark board 1.6 x 0.45 m with a tube frame, a cup and three tube strokes. Pivot on the bottom of
/// its back face (Z = 0, against the wall or standing on a pergola), front +Z.</item>
/// <item>RestaurantBladeSign: a round blade sign perpendicular to the wall: a wall plate, an arm 0.85 m out and a 0.6 m disc hanging from it with
/// a lit ring and a mug on both faces. Pivot on the bottom of the wall plate, the arm along +Z.</item>
/// </list>
/// The glowing parts use emissive URP Lit materials (LP_SIGN_*Glow, instanced, emission always on), so
/// the signs glow by day and by night without a runtime driver. Output: Models/LowPoly/&lt;name&gt;_Combined.asset
/// and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Restaurant Sign Props
/// </summary>
public static class GenerateRestaurantSignProps
{
    [MenuItem("Tools/Procedural Cities/Generate Restaurant Sign Props")]
    public static void Generate()
    {
        var metal = LowPolyBoxMesh.GetMat("LP_SIGN_Metal", new Color(0.20f, 0.21f, 0.23f), 0.7f, 0.5f);
        var board = LowPolyBoxMesh.GetMat("LP_SIGN_Board", new Color(0.08f, 0.08f, 0.10f), 0.1f, 0.4f);
        var red = LowPolyBoxMesh.GetMat("LP_SIGN_Red", new Color(0.70f, 0.10f, 0.08f), 0.2f, 0.5f);
        var warm = Glow("LP_SIGN_WarmGlow", new Color(1.00f, 0.86f, 0.55f), 1.6f);
        var pink = Glow("LP_SIGN_PinkGlow", new Color(1.00f, 0.25f, 0.62f), 2.2f);
        var cyan = Glow("LP_SIGN_CyanGlow", new Color(0.25f, 0.95f, 1.00f), 2.2f);
        var amber = Glow("LP_SIGN_AmberGlow", new Color(1.00f, 0.62f, 0.12f), 2.0f);

        BuildPylon(metal, red, warm, amber);
        BuildNeon(board, metal, pink, cyan);
        BuildBlade(metal, red, warm, amber);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>An emissive URP Lit material (instanced): <paramref name="color"/> glowing at <paramref name="intensity"/>.</summary>
    private static Material Glow(string name, Color color, float intensity)
    {
        // The keyword and the flags before the asset is written, as AddLampBulbs does; an existing
        // asset is set again and saved at once.
        var path = $"{LowPolyBoxMesh.MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var create = mat == null;
        if (create)
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", color * intensity);
        if (create)
        {
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
        }

        return mat;
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    /// <summary>A 12-sided prism <paramref name="width"/> across along Y-up (as GenerateRestaurantProps).</summary>
    private static void Round(List<LowPolyBoxMesh.Part> parts, Material mat, float x, float y0, float z, float width, float y1)
    {
        var centre = new Vector3(x, (y0 + y1) * 0.5f, z);
        var size = new Vector3(width, y1 - y0, width * Mathf.Tan(15f * Mathf.Deg2Rad));
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(mat, centre, size, Quaternion.Euler(0f, i * 30f, 0f)));
    }

    /// <summary>A 12-sided disc <paramref name="diameter"/> across in the YZ plane (its axis along X), centred at <paramref name="centre"/>.</summary>
    private static void Disc(List<LowPolyBoxMesh.Part> parts, Material mat, Vector3 centre, float diameter, float thickness)
    {
        var size = new Vector3(thickness, diameter, diameter * Mathf.Tan(15f * Mathf.Deg2Rad));
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(mat, centre, size, Quaternion.Euler(i * 30f, 0f, 0f)));
    }

    /// <summary>A 12-sided disc <paramref name="diameter"/> across in the XY plane (its axis along Z), centred at <paramref name="centre"/>.</summary>
    private static void DiscZ(List<LowPolyBoxMesh.Part> parts, Material mat, Vector3 centre, float diameter, float thickness)
    {
        var size = new Vector3(diameter, diameter * Mathf.Tan(15f * Mathf.Deg2Rad), thickness);
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(mat, centre, size, Quaternion.Euler(0f, 0f, i * 30f)));
    }

    private static void Save(string name, string child, List<LowPolyBoxMesh.Part> parts)
    {
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine(name + "_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildPylon(Material metal, Material red, Material warm, Material amber)
    {
        var parts = new List<LowPolyBoxMesh.Part>
        {
            // Base and pole.
            Box(metal, -0.35f, 0f, -0.35f, 0.35f, 0.25f, 0.35f),
            Box(metal, -0.11f, 0.25f, -0.11f, 0.11f, 5.9f, 0.11f),
            // The box: a red frame round a lit panel on each face, a red band on top.
            Box(red, -1.25f, 5.9f, -0.22f, 1.25f, 7.3f, 0.22f),
            Box(warm, -1.12f, 6.0f, 0.22f, 1.12f, 6.95f, 0.235f),
            Box(warm, -1.12f, 6.0f, -0.235f, 1.12f, 6.95f, -0.22f),
            Box(amber, -1.25f, 7.3f, -0.18f, 1.25f, 7.45f, 0.18f),
        };
        // A plate, a fork and a knife on both faces.
        foreach (var z in new[] { 0.235f, -0.25f })
        {
            var z1 = z + 0.015f;
            var outward = z > 0f ? 1f : -1f;
            DiscZ(parts, red, new Vector3(0f, 6.48f, z + outward * 0.007f), 0.62f, 0.014f);
            DiscZ(parts, warm, new Vector3(0f, 6.48f, z + outward * 0.009f), 0.44f, 0.014f);
            parts.Add(Box(red, -0.52f, 6.12f, z, -0.47f, 6.84f, z1));
            parts.Add(Box(red, -0.60f, 6.62f, z, -0.39f, 6.66f, z1));
            parts.Add(Box(red, 0.47f, 6.12f, z, 0.52f, 6.84f, z1));
            parts.Add(Box(red, 0.47f, 6.55f, z, 0.57f, 6.84f, z1));
        }

        Save("RestaurantPylonSign", "restaurant_pylon_sign", parts);
    }

    private static void BuildNeon(Material board, Material metal, Material pink, Material cyan)
    {
        // A dark board on two brackets, a tube frame round it, a cup on the left and three strokes.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(metal, -0.70f, 0.10f, 0f, -0.64f, 0.35f, 0.06f),
            Box(metal, 0.64f, 0.10f, 0f, 0.70f, 0.35f, 0.06f),
            Box(board, -0.80f, 0f, 0.06f, 0.80f, 0.45f, 0.10f),
            Box(pink, -0.77f, 0.03f, 0.10f, 0.77f, 0.055f, 0.125f),
            Box(pink, -0.77f, 0.395f, 0.10f, 0.77f, 0.42f, 0.125f),
            Box(pink, -0.77f, 0.03f, 0.10f, -0.745f, 0.42f, 0.125f),
            Box(pink, 0.745f, 0.03f, 0.10f, 0.77f, 0.42f, 0.125f),
            // The cup: its sides, its bottom, the handle and the steam.
            Box(cyan, -0.62f, 0.11f, 0.10f, -0.60f, 0.29f, 0.12f),
            Box(cyan, -0.46f, 0.11f, 0.10f, -0.44f, 0.29f, 0.12f),
            Box(cyan, -0.62f, 0.11f, 0.10f, -0.44f, 0.13f, 0.12f),
            Box(cyan, -0.44f, 0.17f, 0.10f, -0.39f, 0.19f, 0.12f),
            Box(cyan, -0.40f, 0.17f, 0.10f, -0.38f, 0.25f, 0.12f),
            Box(cyan, -0.44f, 0.24f, 0.10f, -0.39f, 0.26f, 0.12f),
            Box(cyan, -0.57f, 0.31f, 0.10f, -0.555f, 0.37f, 0.12f),
            Box(cyan, -0.51f, 0.31f, 0.10f, -0.495f, 0.37f, 0.12f),
            // Three tube strokes, a word in neon.
            Box(pink, -0.30f, 0.20f, 0.10f, 0.66f, 0.225f, 0.12f),
            Box(cyan, -0.30f, 0.27f, 0.10f, 0.40f, 0.295f, 0.12f),
            Box(cyan, -0.30f, 0.13f, 0.10f, 0.20f, 0.155f, 0.12f),
        };
        Save("RestaurantNeonSign", "restaurant_neon_sign", parts);
    }

    private static void BuildBlade(Material metal, Material red, Material warm, Material amber)
    {
        // A wall plate, an arm out along +Z with a stay, two hooks and the disc hanging under the arm.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(metal, -0.10f, 0f, 0f, 0.10f, 0.90f, 0.03f),
            Box(metal, -0.02f, 0.80f, 0.03f, 0.02f, 0.84f, 0.88f),
            new(metal, new Vector3(0f, 0.55f, 0.32f), new Vector3(0.03f, 0.03f, 0.72f), Quaternion.Euler(-37f, 0f, 0f)),
            Box(metal, -0.01f, 0.72f, 0.24f, 0.01f, 0.80f, 0.26f),
            Box(metal, -0.01f, 0.72f, 0.70f, 0.01f, 0.80f, 0.72f),
        };
        var centre = new Vector3(0f, 0.40f, 0.48f);
        Disc(parts, red, centre, 0.64f, 0.06f);
        // A lit ring and a mug on each face (the faces look along ±X, up and down the street).
        foreach (var side in new[] { 1f, -1f })
        {
            var x = side * 0.035f;
            var mx = side * 0.05f;
            Disc(parts, warm, centre + new Vector3(x, 0f, 0f), 0.56f, 0.012f);
            Disc(parts, red, centre + new Vector3(side * 0.04f, 0f, 0f), 0.46f, 0.012f);
            parts.Add(Box(amber, mx - 0.006f, 0.27f, 0.40f, mx + 0.006f, 0.53f, 0.56f));
            parts.Add(Box(amber, mx - 0.006f, 0.33f, 0.56f, mx + 0.006f, 0.47f, 0.61f));
            parts.Add(Box(warm, mx - 0.007f, 0.50f, 0.40f, mx + 0.007f, 0.55f, 0.56f));
        }

        Save("RestaurantBladeSign", "restaurant_blade_sign", parts);
    }
}
