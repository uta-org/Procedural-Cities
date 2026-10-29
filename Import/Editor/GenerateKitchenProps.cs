using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the kitchen props (P40.5, owner-approved list Q-20261028-9): ChoppingBoard (leaning
/// on the wall), Kettle, KitchenToaster (the Toaster prefab is the microwave), CoffeeMaker, FruitBowl, UtensilPot, SpiceRack (wall-hung),
/// KitchenRoll, FryingPan and TeaTowel. Worktop pieces have their pivot on the bottom face at
/// the centre, front +Z; SpiceRack has its back face at Z = 0, like Mirror; FryingPan's pivot is
/// under the pan's centre (the handle reaches +X); TeaTowel's pivot is the top of the bar it
/// hangs over. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Kitchen Props
/// </summary>
public static class GenerateKitchenProps
{
    [MenuItem("Tools/Procedural Cities/Generate Kitchen Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateKitchenProps] Missing LP_Chrome.");
            return;
        }

        var dark = LowPolyBoxMesh.LoadMat("LP_Metal_DarkGray") ?? chrome;
        var wood = LowPolyBoxMesh.GetMat("LP_KP_Wood", new Color(0.72f, 0.52f, 0.32f), 0f, 0.3f);
        var white = LowPolyBoxMesh.GetMat("LP_KP_White", new Color(0.94f, 0.94f, 0.92f), 0f, 0.6f);
        var black = LowPolyBoxMesh.GetMat("LP_KP_Black", new Color(0.10f, 0.10f, 0.11f), 0.2f, 0.6f);

        BuildChoppingBoard(wood, LowPolyBoxMesh.GetMat("LP_KP_WoodDark", new Color(0.45f, 0.30f, 0.18f), 0f, 0.3f));
        BuildKettle(LowPolyBoxMesh.GetMat("LP_KP_Kettle", new Color(0.78f, 0.80f, 0.82f), 0.7f, 0.7f), black);
        BuildToaster(LowPolyBoxMesh.GetMat("LP_KP_Toaster", new Color(0.80f, 0.22f, 0.20f), 0.2f, 0.6f), black, chrome);
        BuildCoffeeMaker(black, chrome, LowPolyBoxMesh.GetMat("LP_KP_Glass", new Color(0.75f, 0.85f, 0.90f), 0f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_KP_Coffee", new Color(0.22f, 0.12f, 0.06f), 0f, 0.7f));
        BuildFruitBowl(white,
            LowPolyBoxMesh.GetMat("LP_KP_Orange", new Color(0.95f, 0.55f, 0.10f), 0f, 0.4f),
            LowPolyBoxMesh.GetMat("LP_KP_Apple", new Color(0.75f, 0.12f, 0.12f), 0f, 0.5f),
            LowPolyBoxMesh.GetMat("LP_KP_Banana", new Color(0.95f, 0.85f, 0.30f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_KP_Pear", new Color(0.60f, 0.75f, 0.25f), 0f, 0.4f));
        BuildUtensilPot(LowPolyBoxMesh.GetMat("LP_KP_Pot", new Color(0.30f, 0.45f, 0.55f), 0f, 0.6f), wood, chrome, black);
        BuildSpiceRack(wood, new[]
        {
            LowPolyBoxMesh.GetMat("LP_KP_SpiceRed", new Color(0.75f, 0.20f, 0.10f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_KP_SpiceYellow", new Color(0.90f, 0.70f, 0.15f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_KP_SpiceGreen", new Color(0.35f, 0.50f, 0.20f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_KP_SpiceBrown", new Color(0.45f, 0.28f, 0.15f), 0f, 0.3f),
        }, LowPolyBoxMesh.GetMat("LP_KP_JarGlass", new Color(0.85f, 0.90f, 0.92f), 0f, 0.9f), black);
        BuildKitchenRoll(white, chrome);
        BuildFryingPan(black, dark);
        BuildTeaTowel(LowPolyBoxMesh.GetMat("LP_KP_Towel", new Color(0.90f, 0.90f, 0.85f), 0f, 0.05f),
            LowPolyBoxMesh.GetMat("LP_KP_TowelStripe", new Color(0.70f, 0.20f, 0.20f), 0f, 0.05f));
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

    private static void BuildChoppingBoard(Material wood, Material dark)
    {
        // Standing on its edge, its top leaning 12 degrees back on the wall (-Z).
        var tilt = Quaternion.Euler(-12f, 0f, 0f);
        var up = tilt * Vector3.up;
        Save("ChoppingBoard", "board", new List<LowPolyBoxMesh.Part>
        {
            new(wood, up * 0.15f + new Vector3(0f, 0f, 0.01f), new Vector3(0.36f, 0.30f, 0.022f), tilt),
            new(dark, up * 0.26f + new Vector3(0f, 0f, 0.022f), new Vector3(0.04f, 0.03f, 0.004f), tilt),
        });
    }

    private static void BuildKettle(Material body, Material black)
    {
        var parts = new List<LowPolyBoxMesh.Part> { Box(black, -0.085f, 0f, -0.085f, 0.085f, 0.02f, 0.085f) };
        Round(parts, body, 0f, 0.02f, 0f, 0.15f, 0.20f);
        Round(parts, black, 0f, 0.20f, 0f, 0.08f, 0.22f);
        // Spout to +X, handle to -X.
        parts.Add(new LowPolyBoxMesh.Part(body, new Vector3(0.085f, 0.15f, 0f), new Vector3(0.06f, 0.03f, 0.03f), Quaternion.Euler(0f, 0f, 35f)));
        parts.Add(Box(black, -0.105f, 0.06f, -0.012f, -0.085f, 0.20f, 0.012f));
        parts.Add(Box(black, -0.105f, 0.18f, -0.012f, -0.05f, 0.20f, 0.012f));
        parts.Add(Box(black, -0.105f, 0.06f, -0.012f, -0.07f, 0.08f, 0.012f));
        Save("Kettle", "kettle", parts);
    }

    private static void BuildToaster(Material body, Material black, Material chrome)
    {
        // Its long side front to back, the lever on +X.
        Save("KitchenToaster", "toaster", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.08f, 0.01f, -0.13f, 0.08f, 0.17f, 0.13f),
            Box(black, -0.075f, 0f, -0.125f, 0.075f, 0.01f, 0.125f),
            Box(black, -0.045f, 0.17f, -0.10f, -0.015f, 0.172f, 0.10f),
            Box(black, 0.015f, 0.17f, -0.10f, 0.045f, 0.172f, 0.10f),
            Box(chrome, 0.08f, 0.11f, 0.07f, 0.095f, 0.13f, 0.10f),
            Box(chrome, 0.08f, 0.04f, -0.02f, 0.088f, 0.06f, 0.02f),
        });
    }

    private static void BuildCoffeeMaker(Material black, Material chrome, Material glass, Material coffee)
    {
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -0.10f, 0f, -0.12f, 0.10f, 0.03f, 0.12f),
            Box(black, -0.10f, 0.03f, -0.12f, 0.10f, 0.34f, -0.03f),
            Box(black, -0.10f, 0.26f, -0.03f, 0.10f, 0.34f, 0.12f),
            Box(chrome, -0.03f, 0.29f, 0.12f, 0.03f, 0.31f, 0.125f),
        };
        Round(parts, glass, 0f, 0.03f, 0.04f, 0.13f, 0.18f);
        Round(parts, coffee, 0f, 0.035f, 0.04f, 0.12f, 0.10f);
        parts.Add(Box(black, -0.012f, 0.06f, 0.105f, 0.012f, 0.16f, 0.125f));
        Save("CoffeeMaker", "coffee_maker", parts);
    }

    private static void BuildFruitBowl(Material bowl, Material orange, Material apple, Material banana, Material pear)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, bowl, 0f, 0f, 0f, 0.14f, 0.02f);
        Round(parts, bowl, 0f, 0.02f, 0f, 0.22f, 0.06f);
        parts.Add(new LowPolyBoxMesh.Part(orange, new Vector3(-0.05f, 0.085f, -0.03f), new Vector3(0.07f, 0.07f, 0.07f), Quaternion.Euler(0f, 30f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(apple, new Vector3(0.05f, 0.085f, -0.035f), new Vector3(0.065f, 0.065f, 0.065f), Quaternion.Euler(0f, 15f, 10f)));
        parts.Add(new LowPolyBoxMesh.Part(pear, new Vector3(0.01f, 0.09f, 0.05f), new Vector3(0.06f, 0.075f, 0.06f), Quaternion.Euler(10f, 40f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(banana, new Vector3(0f, 0.12f, 0f), new Vector3(0.17f, 0.03f, 0.035f), Quaternion.Euler(0f, -25f, 8f)));
        Save("FruitBowl", "fruit_bowl", parts);
    }

    private static void BuildUtensilPot(Material pot, Material wood, Material chrome, Material black)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, pot, 0f, 0f, 0f, 0.10f, 0.13f);
        parts.Add(new LowPolyBoxMesh.Part(wood, new Vector3(-0.02f, 0.20f, 0f), new Vector3(0.018f, 0.20f, 0.01f), Quaternion.Euler(0f, 0f, 8f)));
        parts.Add(new LowPolyBoxMesh.Part(wood, new Vector3(-0.034f, 0.30f, 0f), new Vector3(0.04f, 0.05f, 0.012f), Quaternion.Euler(0f, 0f, 8f)));
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(0.02f, 0.19f, 0.015f), new Vector3(0.016f, 0.18f, 0.01f), Quaternion.Euler(0f, 0f, -8f)));
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(0.034f, 0.285f, 0.015f), new Vector3(0.06f, 0.06f, 0.006f), Quaternion.Euler(0f, 0f, -8f)));
        parts.Add(new LowPolyBoxMesh.Part(chrome, new Vector3(0.005f, 0.20f, -0.02f), new Vector3(0.012f, 0.20f, 0.012f), Quaternion.Euler(8f, 0f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(chrome, new Vector3(0.005f, 0.30f, -0.035f), new Vector3(0.04f, 0.06f, 0.04f), Quaternion.Euler(8f, 45f, 0f)));
        Save("UtensilPot", "utensil_pot", parts);
    }

    private static void BuildSpiceRack(Material wood, Material[] spices, Material glass, Material lid)
    {
        // Back on the wall at Z = 0: a back board with two shelves of five jars.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.20f, 0f, 0f, 0.20f, 0.20f, 0.012f),
            Box(wood, -0.20f, 0f, 0.012f, 0.20f, 0.012f, 0.08f),
            Box(wood, -0.20f, 0.10f, 0.012f, 0.20f, 0.11f, 0.08f),
            Box(wood, -0.20f, 0.012f, 0.012f, -0.19f, 0.20f, 0.08f),
            Box(wood, 0.19f, 0.012f, 0.012f, 0.20f, 0.20f, 0.08f),
            Box(wood, -0.19f, 0.035f, 0.074f, 0.19f, 0.042f, 0.08f),
            Box(wood, -0.19f, 0.135f, 0.074f, 0.19f, 0.142f, 0.08f),
        };
        for (var shelf = 0; shelf < 2; shelf++)
        {
            var y = shelf == 0 ? 0.012f : 0.11f;
            for (var i = 0; i < 5; i++)
            {
                var x = -0.152f + i * 0.076f;
                parts.Add(Box(glass, x - 0.022f, y, 0.02f, x + 0.022f, y + 0.055f, 0.065f));
                parts.Add(Box(spices[(i + shelf * 2) % spices.Length], x - 0.02f, y + 0.002f, 0.022f, x + 0.02f, y + 0.04f, 0.063f));
                parts.Add(Box(lid, x - 0.022f, y + 0.055f, 0.02f, x + 0.022f, y + 0.068f, 0.065f));
            }
        }

        Save("SpiceRack", "spice_rack", parts);
    }

    private static void BuildKitchenRoll(Material paper, Material chrome)
    {
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, chrome, 0f, 0f, 0f, 0.13f, 0.012f);
        Round(parts, paper, 0f, 0.012f, 0f, 0.11f, 0.26f);
        parts.Add(Box(chrome, -0.008f, 0.26f, -0.008f, 0.008f, 0.29f, 0.008f));
        parts.Add(Box(chrome, -0.015f, 0.285f, -0.015f, 0.015f, 0.295f, 0.015f));
        Save("KitchenRoll", "kitchen_roll", parts);
    }

    private static void BuildFryingPan(Material pan, Material handle)
    {
        // Pivot under the pan's centre, the handle along +X.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, pan, 0f, 0f, 0f, 0.20f, 0.012f);
        Round(parts, pan, 0f, 0.012f, 0f, 0.24f, 0.05f);
        Round(parts, handle, 0f, 0.012f, 0f, 0.21f, 0.014f);
        parts.Add(Box(handle, 0.16f, 0.035f, -0.015f, 0.34f, 0.05f, 0.015f));
        Save("FryingPan", "frying_pan", parts);
    }

    private static void BuildTeaTowel(Material towel, Material stripe)
    {
        // Pivot at the top of the bar it hangs over (a bar about 0.02 m thick): the long fall in
        // front (+Z), the short one behind.
        Save("TeaTowel", "tea_towel", new List<LowPolyBoxMesh.Part>
        {
            Box(towel, -0.13f, -0.26f, 0.011f, 0.13f, 0f, 0.016f),
            Box(stripe, -0.13f, -0.22f, 0.016f, 0.13f, -0.20f, 0.017f),
            Box(stripe, -0.13f, -0.17f, 0.016f, 0.13f, -0.16f, 0.017f),
            Box(towel, -0.13f, -0.12f, -0.016f, 0.13f, 0f, -0.011f),
            Box(towel, -0.13f, 0f, -0.016f, 0.13f, 0.006f, 0.016f),
        });
    }
}
