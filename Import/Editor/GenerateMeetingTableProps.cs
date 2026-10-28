using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the two pieces of the stretchable meeting table (P32): MeetingTableTop, a
/// 1.00 x 1.00 m wooden top 0.05 m thick whose upper face is at 0.75 m, which the
/// city scales in X/Z to the table's length and width, and MeetingTableLeg, a 0.70 m
/// chrome leg placed under it at its own scale (one more per axis every
/// room.meetingTableLegSpacing metres). Same materials as OfficeMeetingTable
/// (LP_Wood_Table, LP_Chrome). Deployed to Resources/Prefabs/AssetContents like the
/// other furniture props.
/// Menu: Tools / Procedural Cities / Generate Meeting Table Props
/// </summary>
public static class GenerateMeetingTableProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";

    /// <summary>Height of the top's upper face (m), as OfficeMeetingTable.</summary>
    public const float TopHeight = 0.75f;

    /// <summary>Thickness of the top (m).</summary>
    public const float TopThickness = 0.05f;

    /// <summary>Side of the leg's square section (m).</summary>
    public const float LegSide = 0.07f;

    [MenuItem("Tools/Procedural Cities/Generate Meeting Table Props")]
    public static void Generate()
    {
        var wood = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Wood_Table.mat");
        var chrome = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/LP_Chrome.mat");
        if (!wood || !chrome)
        {
            Debug.LogError("[GenerateMeetingTableProps] Missing LP_Wood_Table / LP_Chrome.");
            return;
        }

        // Top: 1 x 1 m in X/Z, scaled per table; its pivot on the floor at its centre.
        var top = new GameObject("MeetingTableTop");
        AddBox("Top", top.transform, wood,
            new Vector3(0f, TopHeight - TopThickness * 0.5f, 0f), new Vector3(1f, TopThickness, 1f));
        var topCollider = top.AddComponent<BoxCollider>();
        topCollider.center = new Vector3(0f, TopHeight - TopThickness * 0.5f, 0f);
        topCollider.size = new Vector3(1f, TopThickness, 1f);
        Save(top);

        // Leg: from the floor up to the top's underside.
        var legHeight = TopHeight - TopThickness;
        var leg = new GameObject("MeetingTableLeg");
        AddBox("Leg", leg.transform, chrome,
            new Vector3(0f, legHeight * 0.5f, 0f), new Vector3(LegSide, legHeight, LegSide));
        var legCollider = leg.AddComponent<BoxCollider>();
        legCollider.center = new Vector3(0f, legHeight * 0.5f, 0f);
        legCollider.size = new Vector3(LegSide, legHeight, LegSide);
        Save(leg);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Save(GameObject root)
    {
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{root.name}.prefab");
            Debug.Log($"[GenerateMeetingTableProps] Saved {PrefabDir}/{root.name}.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void AddBox(string name, Transform parent, Material material, Vector3 center, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
