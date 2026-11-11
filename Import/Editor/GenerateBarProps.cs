using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the back-bar props of the restaurants (P88.1, owner request 2026-11-11):
/// <list type="bullet">
/// <item>Six bottles of different shapes for the back-bar shelves: BarBottleWine, BarBottleWhisky (square),
/// BarBottleBeer, BarBottleGin (squat), BarBottleVodka (tall and thin) and BarBottleLiqueur (a flat flask).</item>
/// <item>Four glasses: BarGlassHighball (tubo), BarGlassWine (copa), BarGlassBalloon (balón) and
/// BarGlassTumbler (vaso normal).</item>
/// <item>BeerTap: a chrome beer tower for the bar top, its faucet and drip tray towards -Z (the staff
/// side when it takes a bar module's rotation) and the brand medallion towards +Z.</item>
/// <item>Barrels: BeerKeg (steel), BeerKegTapped (the keg with its coupler and a beer line that runs
/// +Z and up into the counter above it) and BeerBarrelWood (a hooped wooden barrel).</item>
/// <item>Half-full crates: BeerCrateHalf (a yellow crate, 11 of 20 brown bottles) and SodaCrateHalf
/// (a red crate, 13 of 24 bottles).</item>
/// </list>
/// Every piece has its pivot on the bottom face at the centre, front +Z, and is built at true scale
/// (no FurnitureScaleOverride). Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Bar Props
/// </summary>
public static class GenerateBarProps
{
    [MenuItem("Tools/Procedural Cities/Generate Bar Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateBarProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_RS_Black", new Color(0.11f, 0.11f, 0.12f), 0.1f, 0.5f);
        var label = LowPolyBoxMesh.GetMat("LP_RS_Label", new Color(0.92f, 0.88f, 0.78f), 0f, 0.3f);
        var gold = LowPolyBoxMesh.GetMat("LP_RS_MenuGold", new Color(0.85f, 0.70f, 0.35f), 0.5f, 0.5f);
        var glass = LowPolyBoxMesh.GetMat("LP_BAR_Glass", new Color(0.86f, 0.93f, 0.96f), 0f, 0.95f);
        var green = LowPolyBoxMesh.GetMat("LP_RS_BottleGreen", new Color(0.15f, 0.32f, 0.18f), 0.1f, 0.9f);
        var amber = LowPolyBoxMesh.GetMat("LP_RS_BottleAmber", new Color(0.55f, 0.30f, 0.08f), 0.1f, 0.9f);
        var clear = LowPolyBoxMesh.GetMat("LP_RS_BottleClear", new Color(0.80f, 0.86f, 0.88f), 0.1f, 0.9f);
        var brown = LowPolyBoxMesh.GetMat("LP_BAR_BottleBrown", new Color(0.30f, 0.16f, 0.06f), 0.1f, 0.9f);
        var blue = LowPolyBoxMesh.GetMat("LP_BAR_BottleBlue", new Color(0.45f, 0.62f, 0.78f), 0.1f, 0.9f);
        var red = LowPolyBoxMesh.GetMat("LP_BAR_Red", new Color(0.62f, 0.10f, 0.10f), 0f, 0.5f);
        var purple = LowPolyBoxMesh.GetMat("LP_BAR_BottlePurple", new Color(0.38f, 0.12f, 0.30f), 0.1f, 0.9f);
        var steel = LowPolyBoxMesh.GetMat("LP_BAR_Steel", new Color(0.70f, 0.72f, 0.74f), 0.8f, 0.6f);
        var wood = LowPolyBoxMesh.GetMat("LP_BAR_BarrelWood", new Color(0.46f, 0.29f, 0.15f), 0f, 0.3f);
        var iron = LowPolyBoxMesh.GetMat("LP_BAR_Iron", new Color(0.18f, 0.18f, 0.19f), 0.6f, 0.4f);

        BuildBottles(green, amber, clear, brown, blue, purple, label, black, red, gold, chrome);
        BuildGlasses(glass);
        BuildBeerTap(chrome, black, gold, red);
        BuildKegs(steel, black, red, wood, iron);
        BuildCrates(LowPolyBoxMesh.GetMat("LP_BAR_CrateYellow", new Color(0.90f, 0.72f, 0.12f), 0f, 0.4f),
            LowPolyBoxMesh.GetMat("LP_BAR_CrateRed", new Color(0.72f, 0.12f, 0.10f), 0f, 0.4f), brown, green, clear, gold, red);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    /// <summary>
    /// A 12-sided prism <paramref name="width"/> across the flats from <paramref name="y0"/> to
    /// <paramref name="y1"/>: six boxes the full width long and width x tan 15 degrees wide, turned
    /// 30 degrees apart (as GenerateRestaurantProps).
    /// </summary>
    private static void Round(List<LowPolyBoxMesh.Part> parts, Material mat, float x, float y0, float z, float width, float y1)
    {
        var centre = new Vector3(x, (y0 + y1) * 0.5f, z);
        var size = new Vector3(width, y1 - y0, width * Mathf.Tan(15f * Mathf.Deg2Rad));
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(mat, centre, size, Quaternion.Euler(0f, i * 30f, 0f)));
    }

    /// <summary>
    /// A round profile: <paramref name="profile"/> holds (height, width) pairs from the bottom up, and
    /// each step is a prism of the step's width (a stepped taper, low-poly on purpose).
    /// </summary>
    private static void Lathe(List<LowPolyBoxMesh.Part> parts, Material mat, float x, float z, params Vector2[] profile)
    {
        for (var i = 0; i + 1 < profile.Length; i++)
            Round(parts, mat, x, profile[i].x, z, profile[i].y, profile[i + 1].x);
    }

    private static void Save(string name, string child, List<LowPolyBoxMesh.Part> parts)
    {
        var mesh = LowPolyBoxMesh.SaveMesh(LowPolyBoxMesh.Combine(name + "_Combined", parts, out var materials));
        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    // ---------------------------------------------------------------- bottles

    private static void BuildBottles(Material green, Material amber, Material clear, Material brown, Material blue,
        Material purple, Material label, Material black, Material red, Material gold, Material chrome)
    {
        // Wine: a round body 0.075 m across, sloping shoulders, a long neck and a dark red foil. 0.30 m.
        var parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, green, 0f, 0f, new Vector2(0f, 0.075f), new Vector2(0.19f, 0.062f), new Vector2(0.21f, 0.045f),
            new Vector2(0.225f, 0.028f), new Vector2(0.27f, 0f));
        Round(parts, label, 0f, 0.06f, 0f, 0.077f, 0.15f);
        Round(parts, red, 0f, 0.27f, 0f, 0.030f, 0.30f);
        Save("BarBottleWine", "bar_bottle_wine", parts);

        // Whisky: a square amber body, a short shoulder and neck, a black cap and a front label. 0.25 m.
        parts = new List<LowPolyBoxMesh.Part>
        {
            Box(amber, -0.042f, 0f, -0.042f, 0.042f, 0.17f, 0.042f),
            Box(amber, -0.030f, 0.17f, -0.030f, 0.030f, 0.19f, 0.030f),
            Box(label, -0.032f, 0.05f, 0.042f, 0.032f, 0.12f, 0.044f),
            Box(gold, -0.020f, 0.125f, 0.042f, 0.020f, 0.14f, 0.044f),
        };
        Round(parts, amber, 0f, 0.19f, 0f, 0.028f, 0.22f);
        Round(parts, black, 0f, 0.22f, 0f, 0.036f, 0.25f);
        Save("BarBottleWhisky", "bar_bottle_whisky", parts);

        // Beer: a short brown body, a long tapering neck and a gold crown cap. 0.23 m.
        parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, brown, 0f, 0f, new Vector2(0f, 0.062f), new Vector2(0.13f, 0.050f), new Vector2(0.15f, 0.036f),
            new Vector2(0.165f, 0.026f), new Vector2(0.22f, 0f));
        Round(parts, label, 0f, 0.04f, 0f, 0.064f, 0.10f);
        Round(parts, gold, 0f, 0.22f, 0f, 0.029f, 0.23f);
        Save("BarBottleBeer", "bar_bottle_beer", parts);

        // Gin: a squat, wide blue body, a short neck and a chrome cap. 0.23 m.
        parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, blue, 0f, 0f, new Vector2(0f, 0.095f), new Vector2(0.15f, 0.075f), new Vector2(0.17f, 0.045f),
            new Vector2(0.18f, 0.032f), new Vector2(0.20f, 0f));
        Round(parts, label, 0f, 0.05f, 0f, 0.097f, 0.11f);
        Round(parts, chrome, 0f, 0.20f, 0f, 0.040f, 0.23f);
        Save("BarBottleGin", "bar_bottle_gin", parts);

        // Vodka: a tall, thin clear body and a red cap. 0.33 m.
        parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, clear, 0f, 0f, new Vector2(0f, 0.066f), new Vector2(0.24f, 0.048f), new Vector2(0.26f, 0.030f),
            new Vector2(0.30f, 0f));
        Round(parts, label, 0f, 0.09f, 0f, 0.068f, 0.17f);
        Round(parts, red, 0f, 0.30f, 0f, 0.034f, 0.33f);
        Save("BarBottleVodka", "bar_bottle_vodka", parts);

        // Liqueur: a flat purple flask (wide in X, thin in Z), a short neck and a gold cap. 0.22 m.
        parts = new List<LowPolyBoxMesh.Part>
        {
            Box(purple, -0.050f, 0f, -0.024f, 0.050f, 0.14f, 0.024f),
            Box(purple, -0.040f, 0.14f, -0.020f, 0.040f, 0.16f, 0.020f),
            Box(label, -0.035f, 0.035f, 0.024f, 0.035f, 0.10f, 0.026f),
        };
        Round(parts, purple, 0f, 0.16f, 0f, 0.026f, 0.195f);
        Round(parts, gold, 0f, 0.195f, 0f, 0.032f, 0.22f);
        Save("BarBottleLiqueur", "bar_bottle_liqueur", parts);
    }

    // ---------------------------------------------------------------- glasses

    private static void BuildGlasses(Material glass)
    {
        // Tubo (highball): a tall straight glass on a thick base. 0.15 m.
        var parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, glass, 0f, 0f, new Vector2(0f, 0.066f), new Vector2(0.015f, 0.064f), new Vector2(0.15f, 0f));
        Save("BarGlassHighball", "bar_glass_highball", parts);

        // Copa (wine glass): a foot, a thin stem and a tulip bowl. 0.19 m.
        parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, glass, 0f, 0f, new Vector2(0f, 0.068f), new Vector2(0.005f, 0.010f), new Vector2(0.085f, 0.045f),
            new Vector2(0.10f, 0.072f), new Vector2(0.15f, 0.078f), new Vector2(0.19f, 0f));
        Save("BarGlassWine", "bar_glass_wine", parts);

        // Balón (balloon glass): a wide foot, a short stem and a big round bowl. 0.20 m.
        parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, glass, 0f, 0f, new Vector2(0f, 0.078f), new Vector2(0.005f, 0.011f), new Vector2(0.065f, 0.060f),
            new Vector2(0.080f, 0.095f), new Vector2(0.095f, 0.112f), new Vector2(0.15f, 0.100f), new Vector2(0.175f, 0.088f),
            new Vector2(0.20f, 0f));
        Save("BarGlassBalloon", "bar_glass_balloon", parts);

        // Vaso normal (tumbler): short and wide on a thick base. 0.095 m.
        parts = new List<LowPolyBoxMesh.Part>();
        Lathe(parts, glass, 0f, 0f, new Vector2(0f, 0.078f), new Vector2(0.018f, 0.080f), new Vector2(0.095f, 0f));
        Save("BarGlassTumbler", "bar_glass_tumbler", parts);
    }

    // ---------------------------------------------------------------- beer tap

    private static void BuildBeerTap(Material chrome, Material black, Material gold, Material red)
    {
        // A beer tower 0.48 m tall: a base plate, a column, a cap, the faucet and its handle towards -Z
        // (the staff), a drip tray under the faucet and a brand medallion on the +Z face (the customers).
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(chrome, -0.07f, 0f, -0.07f, 0.07f, 0.012f, 0.07f),
            Box(black, -0.07f, 0f, -0.20f, 0.07f, 0.018f, -0.075f),
            Box(chrome, -0.065f, 0.018f, -0.195f, 0.065f, 0.020f, -0.08f),
            Box(chrome, -0.012f, 0.30f, -0.12f, 0.012f, 0.325f, -0.025f),
            Box(chrome, -0.010f, 0.25f, -0.13f, 0.010f, 0.325f, -0.11f),
            Box(red, -0.045f, 0.24f, 0.026f, 0.045f, 0.33f, 0.030f),
            Box(gold, -0.030f, 0.255f, 0.030f, 0.030f, 0.315f, 0.032f),
        };
        Round(parts, chrome, 0f, 0.012f, 0f, 0.055f, 0.42f);
        Round(parts, chrome, 0f, 0.42f, 0f, 0.070f, 0.44f);
        // The handle stands on the faucet, leaning back towards the staff.
        parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(0f, 0.395f, -0.085f), new Vector3(0.026f, 0.13f, 0.026f),
            Quaternion.Euler(-12f, 0f, 0f)));
        parts.Add(new LowPolyBoxMesh.Part(gold, new Vector3(0f, 0.465f, -0.10f), new Vector3(0.032f, 0.025f, 0.032f),
            Quaternion.Euler(-12f, 0f, 0f)));
        Save("BeerTap", "beer_tap", parts);
    }

    // ---------------------------------------------------------------- barrels

    private static void BuildKegs(Material steel, Material black, Material red, Material wood, Material iron)
    {
        // A 50 l steel keg, 0.40 m across and 0.60 m tall: rolled rims, two ribs and the spear on top.
        var keg = new List<LowPolyBoxMesh.Part>();
        Round(keg, steel, 0f, 0f, 0f, 0.40f, 0.06f);
        Round(keg, steel, 0f, 0.06f, 0f, 0.38f, 0.54f);
        Round(keg, steel, 0f, 0.18f, 0f, 0.395f, 0.20f);
        Round(keg, steel, 0f, 0.40f, 0f, 0.395f, 0.42f);
        Round(keg, steel, 0f, 0.54f, 0f, 0.40f, 0.60f);
        Round(keg, black, 0f, 0.565f, 0f, 0.34f, 0.575f);
        Round(keg, steel, 0f, 0.575f, 0f, 0.07f, 0.61f);
        Save("BeerKeg", "beer_keg", new List<LowPolyBoxMesh.Part>(keg));

        // The keg coupled to a tap: a black coupler with its lever, a red gas line down the back and the
        // clear beer line rising 0.25 m, then running +Z 0.24 m into the counter that the tap stands on.
        keg.Add(Box(black, -0.045f, 0.61f, -0.045f, 0.045f, 0.68f, 0.045f));
        keg.Add(Box(black, -0.012f, 0.66f, -0.16f, 0.012f, 0.675f, -0.04f));
        keg.Add(Box(red, -0.010f, 0.62f, -0.205f, 0.010f, 0.64f, -0.045f));
        keg.Add(Box(red, -0.010f, 0.08f, -0.215f, 0.010f, 0.64f, -0.195f));
        keg.Add(Box(LowPolyBoxMesh.GetMat("LP_BAR_BeerLine", new Color(0.85f, 0.80f, 0.55f), 0f, 0.8f),
            -0.011f, 0.68f, -0.011f, 0.011f, 0.93f, 0.011f));
        keg.Add(Box(LowPolyBoxMesh.GetMat("LP_BAR_BeerLine", new Color(0.85f, 0.80f, 0.55f), 0f, 0.8f),
            -0.011f, 0.908f, 0f, 0.011f, 0.93f, 0.24f));
        Save("BeerKegTapped", "beer_keg_tapped", keg);

        // A hooped wooden barrel standing on end, 0.46 m across at the belly and 0.62 m tall.
        var barrel = new List<LowPolyBoxMesh.Part>();
        Lathe(barrel, wood, 0f, 0f, new Vector2(0f, 0.40f), new Vector2(0.10f, 0.44f), new Vector2(0.24f, 0.46f),
            new Vector2(0.38f, 0.44f), new Vector2(0.52f, 0.40f), new Vector2(0.62f, 0f));
        Round(barrel, iron, 0f, 0.04f, 0f, 0.415f, 0.08f);
        Round(barrel, iron, 0f, 0.20f, 0f, 0.465f, 0.24f);
        Round(barrel, iron, 0f, 0.38f, 0f, 0.465f, 0.42f);
        Round(barrel, iron, 0f, 0.54f, 0f, 0.415f, 0.58f);
        Save("BeerBarrelWood", "beer_barrel_wood", barrel);
    }

    // ---------------------------------------------------------------- crates

    private static void BuildCrates(Material yellow, Material redCrate, Material brown, Material green, Material clear,
        Material gold, Material red)
    {
        // Beer: a yellow crate 0.40 x 0.27 x 0.30 m with 4 x 5 slots, 11 of them holding a brown bottle
        // whose neck and cap stand out of the crate.
        var parts = CrateShell(yellow, 0.40f, 0.27f, 0.30f);
        var beer = new[] { 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 1, 0 };
        FillCrate(parts, 0.40f, 0.30f, 5, 4, beer, (p, x, z, i) =>
        {
            Lathe(p, brown, x, z, new Vector2(0.012f, 0.058f), new Vector2(0.20f, 0.032f), new Vector2(0.29f, 0f));
            Round(p, gold, x, 0.29f, z, 0.030f, 0.30f);
        });
        Save("BeerCrateHalf", "beer_crate_half", parts);

        // Soft drinks: a red crate 0.40 x 0.24 x 0.27 m with 4 x 6 slots, 13 of them holding a green or
        // clear bottle with a red cap.
        parts = CrateShell(redCrate, 0.40f, 0.24f, 0.27f);
        var soda = new[] { 1, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1, 0 };
        FillCrate(parts, 0.40f, 0.27f, 6, 4, soda, (p, x, z, i) =>
        {
            Lathe(p, i % 3 == 0 ? clear : green, x, z, new Vector2(0.012f, 0.050f), new Vector2(0.18f, 0.030f),
                new Vector2(0.26f, 0f));
            Round(p, red, x, 0.26f, z, 0.028f, 0.272f);
        });
        Save("SodaCrateHalf", "soda_crate_half", parts);
    }

    /// <summary>An open plastic crate <paramref name="width"/> (X) x <paramref name="height"/> x <paramref name="depth"/> (Z) with hand holes at the ends.</summary>
    private static List<LowPolyBoxMesh.Part> CrateShell(Material mat, float width, float height, float depth)
    {
        var hx = width * 0.5f;
        var hz = depth * 0.5f;
        const float t = 0.012f;
        return new List<LowPolyBoxMesh.Part>
        {
            Box(mat, -hx, 0f, -hz, hx, t, hz),
            Box(mat, -hx, 0f, -hz, hx, height, -hz + t),
            Box(mat, -hx, 0f, hz - t, hx, height, hz),
            // The end walls stop under a hand hole and close above it.
            Box(mat, -hx, 0f, -hz, -hx + t, height - 0.07f, hz),
            Box(mat, -hx, height - 0.025f, -hz, -hx + t, height, hz),
            Box(mat, -hx, height - 0.07f, -hz, -hx + t, height - 0.025f, -hz + 0.09f),
            Box(mat, -hx, height - 0.07f, hz - 0.09f, -hx + t, height - 0.025f, hz),
            Box(mat, hx - t, 0f, -hz, hx, height - 0.07f, hz),
            Box(mat, hx - t, height - 0.025f, -hz, hx, height, hz),
            Box(mat, hx - t, height - 0.07f, -hz, hx, height - 0.025f, -hz + 0.09f),
            Box(mat, hx - t, height - 0.07f, hz - 0.09f, hx, height - 0.025f, hz),
        };
    }

    /// <summary>Calls <paramref name="bottle"/> for every slot of a <paramref name="columns"/> x <paramref name="rows"/> grid whose <paramref name="filled"/> flag is 1.</summary>
    private static void FillCrate(List<LowPolyBoxMesh.Part> parts, float width, float depth, int columns, int rows, int[] filled,
        System.Action<List<LowPolyBoxMesh.Part>, float, float, int> bottle)
    {
        var stepX = (width - 0.03f) / columns;
        var stepZ = (depth - 0.03f) / rows;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                var i = r * columns + c;
                if (filled[i] == 0)
                    continue;
                bottle(parts, -width * 0.5f + 0.015f + (c + 0.5f) * stepX, -depth * 0.5f + 0.015f + (r + 0.5f) * stepZ, i);
            }
        }
    }
}
