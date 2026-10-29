using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the shop props (P40.15, owner-approved list Q-20261028-9): ShopBaskets (a stack of
/// shopping baskets), Mannequin, SaleSign (wall-hung, back face at Z = 0), SpinnerRack (a rotating
/// display), CardTerminal, SecurityGate (one anti-theft post; a pair flanks the door) and EntranceMat.
/// The till, the clothes rail and the bags reuse CashRegister, ClothesRail and ShoppingBags. Every
/// other piece has its pivot on the bottom face at the centre, front +Z. Output:
/// Models/LowPoly/&lt;name&gt;_Combined.asset and Resources/Prefabs/AssetContents/&lt;name&gt;.prefab
/// (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Shop Props
/// </summary>
public static class GenerateShopProps
{
    [MenuItem("Tools/Procedural Cities/Generate Shop Props")]
    public static void Generate()
    {
        var chrome = LowPolyBoxMesh.LoadMat("LP_Chrome");
        if (!chrome)
        {
            Debug.LogError("[GenerateShopProps] Missing LP_Chrome.");
            return;
        }

        var black = LowPolyBoxMesh.GetMat("LP_SH_Black", new Color(0.11f, 0.11f, 0.12f), 0.1f, 0.5f);
        var white = LowPolyBoxMesh.GetMat("LP_SH_White", new Color(0.95f, 0.95f, 0.94f), 0f, 0.5f);
        var red = LowPolyBoxMesh.GetMat("LP_SH_Red", new Color(0.80f, 0.12f, 0.10f), 0f, 0.5f);

        BuildShopBaskets(red, black);
        BuildMannequin(white, LowPolyBoxMesh.GetMat("LP_SH_Dress", new Color(0.20f, 0.35f, 0.55f), 0f, 0.1f), chrome);
        BuildSaleSign(red, white);
        BuildSpinnerRack(chrome, new[]
        {
            LowPolyBoxMesh.GetMat("LP_SH_CardA", new Color(0.95f, 0.75f, 0.30f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_SH_CardB", new Color(0.45f, 0.70f, 0.85f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_SH_CardC", new Color(0.80f, 0.40f, 0.55f), 0f, 0.3f),
        });
        BuildCardTerminal(black, LowPolyBoxMesh.GetMat("LP_SH_Screen", new Color(0.30f, 0.55f, 0.65f), 0f, 0.8f), white);
        BuildSecurityGate(LowPolyBoxMesh.GetMat("LP_SH_Gate", new Color(0.85f, 0.88f, 0.90f), 0f, 0.8f), black);
        BuildEntranceMat(black, LowPolyBoxMesh.GetMat("LP_SH_Mat", new Color(0.30f, 0.30f, 0.32f), 0f, 0.05f));
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

    private static void BuildShopBaskets(Material basket, Material handle)
    {
        // Six plastic baskets, 0.42 x 0.22 x 0.30 m, nested in a stack 0.47 m high.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 6; i++)
        {
            var y = i * 0.05f;
            parts.Add(Box(basket, -0.20f, y, -0.14f, 0.20f, y + 0.012f, 0.14f));
            parts.Add(Box(basket, -0.21f, y, -0.15f, 0.21f, y + 0.22f, -0.14f));
            parts.Add(Box(basket, -0.21f, y, 0.14f, 0.21f, y + 0.22f, 0.15f));
            parts.Add(Box(basket, -0.21f, y, -0.14f, -0.20f, y + 0.22f, 0.14f));
            parts.Add(Box(basket, 0.20f, y, -0.14f, 0.21f, y + 0.22f, 0.14f));
        }

        parts.Add(Box(handle, -0.16f, 0.47f, -0.01f, 0.16f, 0.49f, 0.01f));
        Save("ShopBaskets", "shop_baskets", parts);
    }

    private static void BuildMannequin(Material body, Material dress, Material metal)
    {
        // A headless dress form on a stand, 1.65 m high, facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, metal, 0f, 0f, 0f, 0.36f, 0.025f);
        parts.Add(Box(metal, -0.015f, 0.025f, -0.015f, 0.015f, 0.85f, 0.015f));
        Round(parts, dress, 0f, 0.80f, 0f, 0.36f, 1.00f);
        Round(parts, dress, 0f, 1.00f, 0f, 0.30f, 1.25f);
        Round(parts, dress, 0f, 1.25f, 0f, 0.36f, 1.42f);
        Round(parts, body, 0f, 1.42f, 0f, 0.14f, 1.52f);
        Round(parts, body, 0f, 1.52f, 0f, 0.06f, 1.62f);
        Round(parts, metal, 0f, 1.62f, 0f, 0.05f, 1.65f);
        foreach (var side in new[] { -1f, 1f })
            parts.Add(new LowPolyBoxMesh.Part(dress, new Vector3(side * 0.20f, 1.30f, 0f), new Vector3(0.07f, 0.22f, 0.09f), Quaternion.Euler(0f, 0f, side * 12f)));
        Save("Mannequin", "mannequin", parts);
    }

    private static void BuildSaleSign(Material red, Material white)
    {
        // Wall-hung board, 0.60 x 0.40 m: a white band and a big "%" in white.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(red, -0.30f, 0f, 0f, 0.30f, 0.40f, 0.015f),
            Box(white, -0.27f, 0.29f, 0.015f, 0.27f, 0.36f, 0.017f),
            new(white, new Vector3(0f, 0.15f, 0.016f), new Vector3(0.025f, 0.20f, 0.002f), Quaternion.Euler(0f, 0f, -35f)),
        };
        Round(parts, white, -0.07f, 0.20f, 0.0165f, 0.05f, 0.25f);
        Round(parts, white, 0.07f, 0.05f, 0.0165f, 0.05f, 0.10f);
        Save("SaleSign", "sale_sign", parts);
    }

    private static void BuildSpinnerRack(Material metal, Material[] cards)
    {
        // A rotating display, 0.50 m across and 1.70 m high, with four sides of pockets.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, metal, 0f, 0f, 0f, 0.50f, 0.03f);
        parts.Add(Box(metal, -0.02f, 0.03f, -0.02f, 0.02f, 1.70f, 0.02f));
        for (var side = 0; side < 4; side++)
        {
            var rotation = Quaternion.Euler(0f, side * 90f, 0f);
            parts.Add(new LowPolyBoxMesh.Part(metal, rotation * new Vector3(0f, 1.0f, 0.10f), new Vector3(0.30f, 1.20f, 0.01f), rotation));
            for (var row = 0; row < 5; row++)
            {
                for (var col = 0; col < 2; col++)
                {
                    parts.Add(new LowPolyBoxMesh.Part(cards[(row + col + side) % cards.Length],
                        rotation * new Vector3(-0.07f + col * 0.14f, 0.52f + row * 0.23f, 0.12f), new Vector3(0.12f, 0.18f, 0.02f), rotation));
                }
            }
        }

        Save("SpinnerRack", "spinner_rack", parts);
    }

    private static void BuildCardTerminal(Material body, Material screen, Material keys)
    {
        Save("CardTerminal", "card_terminal", new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.05f, 0f, -0.09f, 0.05f, 0.03f, 0.09f),
            new(body, new Vector3(0f, 0.05f, -0.01f), new Vector3(0.08f, 0.03f, 0.15f), Quaternion.Euler(20f, 0f, 0f)),
            new(screen, new Vector3(0f, 0.075f, -0.05f), new Vector3(0.06f, 0.004f, 0.05f), Quaternion.Euler(20f, 0f, 0f)),
            new(keys, new Vector3(0f, 0.060f, 0.02f), new Vector3(0.06f, 0.004f, 0.06f), Quaternion.Euler(20f, 0f, 0f)),
        });
    }

    private static void BuildSecurityGate(Material panel, Material foot)
    {
        // One anti-theft post: a 1.50 m clear panel on a dark foot, its face along X.
        Save("SecurityGate", "security_gate", new List<LowPolyBoxMesh.Part>
        {
            Box(foot, -0.20f, 0f, -0.06f, 0.20f, 0.06f, 0.06f),
            Box(panel, -0.18f, 0.06f, -0.02f, 0.18f, 1.50f, 0.02f),
            Box(foot, -0.18f, 1.46f, -0.025f, 0.18f, 1.50f, 0.025f),
            Box(foot, -0.18f, 0.06f, -0.025f, -0.16f, 1.46f, 0.025f),
            Box(foot, 0.16f, 0.06f, -0.025f, 0.18f, 1.46f, 0.025f),
        });
    }

    private static void BuildEntranceMat(Material border, Material mat)
    {
        // 1.20 x 0.80 m, flat; its long side along X (the door).
        Save("EntranceMat", "entrance_mat", new List<LowPolyBoxMesh.Part>
        {
            Box(border, -0.60f, 0f, -0.40f, 0.60f, 0.012f, 0.40f),
            Box(mat, -0.55f, 0.012f, -0.35f, 0.55f, 0.016f, 0.35f),
        });
    }
}
