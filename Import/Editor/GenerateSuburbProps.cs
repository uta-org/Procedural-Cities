using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the low-poly props of the suburbs on the concrete plots (P22, owner 2026-10-26: "a patio
/// must have a wood fence, which you must create, at least 1.8 metres high"; "create new low-poly
/// trash bins and containers to stand around"): a 2 m panel of wooden fence 1.8 m high, a wheelie
/// bin, and a street waste container in four colours, Spanish style (grey with an orange lid for the
/// rest, yellow for packaging, blue for paper, green for glass). Built as the park props are
/// (<see cref="GenerateParkProps"/>: flat-shaded boxes and prisms, a submesh per material), the meshes
/// under Models/LowPoly/Suburb, the prefabs in Resources/Prefabs/AssetContents. Pivot on the ground at
/// the prop's centre, +Z its front; each gets a box collider.
/// Menu: Tools / Procedural Cities / Generate Suburb Props
/// </summary>
public static class GenerateSuburbProps
{
    private const string MeshDir = GenerateParkProps.PkgRoot + "/Models/LowPoly/Suburb";

    /// <summary>Length (m) of a fence panel; placements stretch it along X to the run they cover.</summary>
    public const float FenceLength = 2f;

    /// <summary>Height (m) of the fence (owner: "at least 1.8 metres").</summary>
    public const float FenceHeight = 1.8f;

    [MenuItem("Tools/Procedural Cities/Generate Suburb Props")]
    public static void Generate() => Generate(null);

    /// <summary>Builds the suburb props, saving only those named in <paramref name="only"/> when it is given.</summary>
    public static void Generate(ICollection<string> only)
    {
        GenerateParkProps.EnsureFolder(GenerateParkProps.PkgRoot + "/Models/LowPoly", "Materials");
        GenerateParkProps.EnsureFolder(GenerateParkProps.PkgRoot + "/Models/LowPoly", "Suburb");

        var plank = GenerateParkProps.GetMat("LP_Suburb_FencePlank", new Color(0.60f, 0.42f, 0.25f), 0f, 0.15f);
        var post = GenerateParkProps.GetMat("LP_Suburb_FencePost", new Color(0.40f, 0.27f, 0.15f), 0f, 0.15f);
        var binBody = GenerateParkProps.GetMat("LP_Suburb_BinBody", new Color(0.16f, 0.36f, 0.21f), 0.1f, 0.35f);
        var binLid = GenerateParkProps.GetMat("LP_Suburb_BinLid", new Color(0.11f, 0.26f, 0.15f), 0.1f, 0.4f);
        var tyre = GenerateParkProps.GetMat("LP_Suburb_Tyre", new Color(0.06f, 0.06f, 0.07f), 0f, 0.2f);
        var metal = GenerateParkProps.GetMat("LP_Suburb_Metal", new Color(0.42f, 0.43f, 0.45f), 0.7f, 0.45f);

        var built = new List<string>();
        void Save(string name, GenerateParkProps.MeshBuilder b, Vector3 colliderSize)
        {
            if (only != null && !only.Contains(name))
                return;
            GenerateParkProps.SaveProp(name, b, colliderSize, true, MeshDir);
            built.Add(name);
        }

        // ── Wooden fence: 2 m of vertical planks between two posts, rails behind ──
        {
            var b = new GenerateParkProps.MeshBuilder();
            const int planks = 12;
            const float gap = 0.022f;
            var width = (FenceLength - 0.16f - gap * (planks + 1)) / planks;
            for (var i = 0; i < planks; i++)
            {
                var x = -FenceLength * 0.5f + 0.08f + gap + width * 0.5f + i * (width + gap);
                // Alternating heights a little, for a hand-made look; the taller reach the fence's height.
                var top = FenceHeight - (i % 2) * 0.03f;
                b.Box(plank, new Vector3(x, (top + 0.03f) * 0.5f, 0f), new Vector3(width, top - 0.03f, 0.022f), Quaternion.identity);
            }

            for (var s = -1; s <= 1; s += 2)
                b.Box(post, new Vector3(s * (FenceLength * 0.5f - 0.04f), (FenceHeight + 0.06f) * 0.5f, 0f), new Vector3(0.08f, FenceHeight + 0.06f, 0.08f), Quaternion.identity);
            b.Box(post, new Vector3(0f, 0.32f, -0.03f), new Vector3(FenceLength - 0.16f, 0.07f, 0.035f), Quaternion.identity);
            b.Box(post, new Vector3(0f, FenceHeight - 0.32f, -0.03f), new Vector3(FenceLength - 0.16f, 0.07f, 0.035f), Quaternion.identity);
            Save("SuburbWoodFence", b, new Vector3(FenceLength, FenceHeight, 0.1f));
        }

        // ── Wheelie bin: a tapered body, a lid with a lip, a handle and two wheels at the back ──
        {
            var b = new GenerateParkProps.MeshBuilder();
            b.Prism(binBody, new Vector3(0f, 0.51f, 0f), 0.36f, 0.86f, 4, Quaternion.Euler(0f, 45f, 0f), caps: true, topScale: 1.12f);
            b.Box(binLid, new Vector3(0f, 0.97f, 0.01f), new Vector3(0.6f, 0.05f, 0.64f), Quaternion.identity);
            b.Box(binLid, new Vector3(0f, 0.94f, 0.33f), new Vector3(0.58f, 0.06f, 0.03f), Quaternion.identity);
            b.Box(binLid, new Vector3(0f, 0.9f, -0.31f), new Vector3(0.44f, 0.04f, 0.06f), Quaternion.identity);
            for (var s = -1; s <= 1; s += 2)
            {
                b.Prism(tyre, new Vector3(s * 0.22f, 0.09f, -0.27f), 0.09f, 0.06f, 8, Quaternion.Euler(0f, 0f, 90f), caps: true);
                b.Box(binBody, new Vector3(s * 0.16f, 0.88f, -0.31f), new Vector3(0.04f, 0.1f, 0.04f), Quaternion.identity);
            }

            b.Prism(metal, new Vector3(0f, 0.09f, -0.27f), 0.015f, 0.5f, 6, Quaternion.Euler(0f, 0f, 90f), caps: true);
            Save("SuburbWheelieBin", b, new Vector3(0.6f, 1.0f, 0.7f));
        }

        // ── Street waste containers: a box, a rounded lid, four castors and a foot pedal ──
        var colours = new (string name, Color body, Color lid)[]
        {
            ("Grey", new Color(0.46f, 0.48f, 0.49f), new Color(0.80f, 0.42f, 0.12f)),
            ("Yellow", new Color(0.93f, 0.76f, 0.10f), new Color(0.85f, 0.68f, 0.08f)),
            ("Blue", new Color(0.12f, 0.36f, 0.70f), new Color(0.10f, 0.30f, 0.60f)),
            ("Green", new Color(0.15f, 0.48f, 0.22f), new Color(0.12f, 0.40f, 0.18f)),
        };
        foreach (var (colour, bodyColour, lidColour) in colours)
        {
            var body = GenerateParkProps.GetMat($"LP_Suburb_Container{colour}", bodyColour, 0.15f, 0.4f);
            var lid = GenerateParkProps.GetMat($"LP_Suburb_ContainerLid{colour}", lidColour, 0.15f, 0.45f);
            var b = new GenerateParkProps.MeshBuilder();
            b.Box(body, new Vector3(0f, 0.67f, 0f), new Vector3(1.6f, 1.06f, 1.1f), Quaternion.identity);
            // The lid: a low eight-sided prism lying along X, its lower half inside the body.
            b.Prism(lid, new Vector3(0f, 1.2f, 0f), 0.57f, 1.64f, 8, Quaternion.Euler(0f, 0f, 90f), caps: true);
            b.Box(metal, new Vector3(0f, 0.2f, 0.58f), new Vector3(0.5f, 0.05f, 0.12f), Quaternion.identity);
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                    b.Prism(tyre, new Vector3(sx * 0.68f, 0.07f, sz * 0.42f), 0.07f, 0.05f, 6, Quaternion.Euler(0f, 0f, 90f), caps: true);
            }

            Save($"SuburbContainer{colour}", b, new Vector3(1.65f, 1.78f, 1.2f));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateSuburbProps] Saved {built.Count} suburb prefabs under {GenerateParkProps.PrefabDir}: {string.Join(", ", built)}");
    }
}
