using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds three low-poly potted-plant props for hallway decoration: two tall
/// floor-standing variants ("bush" and "palm") and one small tabletop variant.
/// Deployed straight to Resources/Prefabs/AssetContents (same convention as
/// GenerateRubbishBinProp.cs) so DecorationSpawner's Resources.Load fallback
/// resolves "planter_tall_bush" / "planter_tall_palm" / "planter_table" — none
/// of these names exist in CityManager's Inspector-assigned prefab arrays, so
/// that fallback path is the only one that will ever find them.
/// Interior furniture renders at the prefab's OWN authored size (DecorationSpawner
/// .SpawnFurniture applies FurnitureScaleOverride, defaulting to 1.0 — it never
/// reads FurniturePlacement.Scale), so these are modelled at real-world meters.
/// Menu: Tools / Procedural Cities / Generate Planter Props
/// </summary>
public static class GeneratePlanterProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";

    [MenuItem("Tools/Procedural Cities/Generate Planter Props")]
    public static void Generate()
    {
        EnsureMatFolder();

        var matPot = GetMat("LP_Planter_Pot", new Color(0.52f, 0.28f, 0.16f), 0.05f, 0.3f);
        var matLeafBright = GetMat("LP_Planter_LeafBright", new Color(0.24f, 0.52f, 0.20f), 0f, 0.25f);
        var matLeafDark = GetMat("LP_Planter_LeafDark", new Color(0.14f, 0.34f, 0.14f), 0f, 0.25f);
        var matTrunk = GetMat("LP_Planter_Trunk", new Color(0.33f, 0.24f, 0.14f), 0f, 0.2f);

        SaveBoth("PlanterTallBush", () => BuildTallBush("PlanterTallBush", matPot, matLeafBright, matLeafDark));
        SaveBoth("PlanterTallPalm", () => BuildTallPalm("PlanterTallPalm", matPot, matTrunk, matLeafBright, matLeafDark));
        SaveBoth("PlanterTable", () => BuildTablePlanter("PlanterTable", matPot, matLeafBright, matLeafDark));

        // P38: the medium floor pot and the medium and large vases (the small ones are
        // PlanterTable and Vase, the large pots the two tall planters).
        var matCeramic = GetMat("LP_Vase_Ceramic", new Color(0.86f, 0.84f, 0.78f), 0f, 0.6f);
        var matCeramicBlue = GetMat("LP_Vase_CeramicBlue", new Color(0.22f, 0.36f, 0.58f), 0f, 0.6f);
        var matPetalRed = GetMat("LP_Flower_Red", new Color(0.78f, 0.16f, 0.18f), 0f, 0.3f);
        var matPetalYellow = GetMat("LP_Flower_Yellow", new Color(0.95f, 0.80f, 0.20f), 0f, 0.3f);
        SaveBoth("PlanterMedium", () => BuildMediumPlanter("PlanterMedium", matPot, matLeafBright, matLeafDark));
        SaveBoth("VaseMedium", () => BuildMediumVase("VaseMedium", matCeramicBlue, matLeafDark, matPetalRed, matPetalYellow));
        SaveBoth("VaseLarge", () => BuildLargeVase("VaseLarge", matCeramic, matCeramicBlue));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GeneratePlanterProps] Saved 6 planter and vase prefabs under {PrefabDir}");
    }

    private static void SaveBoth(string shortName, System.Func<GameObject> build)
    {
        var a = build();
        PrefabUtility.SaveAsPrefabAsset(a, $"{PrefabDir}/{shortName}.prefab");
        Object.DestroyImmediate(a);
    }

    // ── Pot helper, shared by all three variants ────────────────────────
    private static float BuildPot(Transform parent, Material matPot, float radius, float height)
    {
        var bodyHalfHeight = height * 0.5f;
        AddCylinder("Base", parent, matPot, new Vector3(0, 0.015f, 0), radius * 0.82f, 0.015f);
        AddCylinder("Pot", parent, matPot, new Vector3(0, 0.03f + bodyHalfHeight, 0), radius, bodyHalfHeight);
        var rimY = 0.03f + height;
        AddCylinder("Rim", parent, matPot, new Vector3(0, rimY, 0), radius * 1.10f, 0.02f);
        return rimY + 0.02f; // world Y where the soil/foliage starts
    }

    // ── Tall "bush": rounded canopy of overlapping spheres ──────────────
    private static GameObject BuildTallBush(string rootName, Material matPot, Material leafA, Material leafB)
    {
        var root = new GameObject(rootName);
        var potTop = BuildPot(root.transform, matPot, 0.22f, 0.42f);

        var offsets = new[]
        {
            new Vector3(0f, 0.20f, 0f),
            new Vector3(0.14f, 0.30f, 0.05f),
            new Vector3(-0.13f, 0.33f, -0.08f),
            new Vector3(0.05f, 0.42f, 0.13f),
            new Vector3(-0.08f, 0.46f, -0.05f),
        };
        var radii = new[] { 0.24f, 0.20f, 0.21f, 0.18f, 0.16f };
        var mats = new[] { leafA, leafB, leafA, leafB, leafA };
        for (var i = 0; i < offsets.Length; i++)
        {
            AddSphere($"Leaf{i}", root.transform, mats[i],
                new Vector3(offsets[i].x, potTop + offsets[i].y, offsets[i].z), radii[i]);
        }

        AddCollider(root, potTop + 0.30f, 0.26f);
        return root;
    }

    // ── Tall "palm": thin trunk with fanned blade leaves ────────────────
    private static GameObject BuildTallPalm(string rootName, Material matPot, Material matTrunk, Material leafA, Material leafB)
    {
        var root = new GameObject(rootName);
        var potTop = BuildPot(root.transform, matPot, 0.20f, 0.40f);

        const float trunkHeight = 0.55f;
        AddCylinder("Trunk", root.transform, matTrunk,
            new Vector3(0, potTop + trunkHeight * 0.5f, 0), 0.035f, trunkHeight * 0.5f);

        var frondTop = potTop + trunkHeight;
        const int frondCount = 6;
        for (var i = 0; i < frondCount; i++)
        {
            var angleDeg = i * (360f / frondCount);
            var blade = AddBox($"Frond{i}", root.transform, i % 2 == 0 ? leafA : leafB,
                Vector3.zero, new Vector3(0.02f, 0.22f, 0.055f));
            blade.transform.localPosition = new Vector3(0, frondTop, 0);
            blade.transform.localRotation =
                Quaternion.Euler(0, angleDeg, 0) * Quaternion.Euler(55f, 0, 0);
            // Slide the blade out along its own tilted length so it fans
            // outward from the trunk tip instead of pivoting through it.
            blade.transform.localPosition += blade.transform.up * 0.20f;
        }

        AddSphere("FrondCenter", root.transform, matTrunk, new Vector3(0, frondTop, 0), 0.045f);
        AddCollider(root, potTop + trunkHeight * 0.5f, 0.20f);
        return root;
    }

    // ── Small tabletop pot with a compact 3-leaf cluster ────────────────
    private static GameObject BuildTablePlanter(string rootName, Material matPot, Material leafA, Material leafB)
    {
        var root = new GameObject(rootName);
        var potTop = BuildPot(root.transform, matPot, 0.09f, 0.15f);

        var offsets = new[]
        {
            new Vector3(0f, 0.07f, 0f),
            new Vector3(0.05f, 0.10f, 0.02f),
            new Vector3(-0.045f, 0.11f, -0.03f),
        };
        var radii = new[] { 0.085f, 0.065f, 0.06f };
        var mats = new[] { leafA, leafB, leafA };
        for (var i = 0; i < offsets.Length; i++)
        {
            AddSphere($"Leaf{i}", root.transform, mats[i],
                new Vector3(offsets[i].x, potTop + offsets[i].y, offsets[i].z), radii[i]);
        }

        AddCollider(root, potTop + 0.08f, 0.10f);
        return root;
    }

    // ── Medium floor pot (P38): about 0.6 m, a low round bush ───────────
    private static GameObject BuildMediumPlanter(string rootName, Material matPot, Material leafA, Material leafB)
    {
        var root = new GameObject(rootName);
        var potTop = BuildPot(root.transform, matPot, 0.15f, 0.26f);

        var offsets = new[]
        {
            new Vector3(0f, 0.10f, 0f),
            new Vector3(0.09f, 0.16f, 0.04f),
            new Vector3(-0.08f, 0.18f, -0.05f),
            new Vector3(0.02f, 0.24f, 0.07f),
        };
        var radii = new[] { 0.15f, 0.12f, 0.12f, 0.10f };
        var mats = new[] { leafA, leafB, leafA, leafB };
        for (var i = 0; i < offsets.Length; i++)
        {
            AddSphere($"Leaf{i}", root.transform, mats[i],
                new Vector3(offsets[i].x, potTop + offsets[i].y, offsets[i].z), radii[i]);
        }

        AddCollider(root, potTop * 0.5f + 0.12f, 0.17f);
        return root;
    }

    // ── Medium vase (P38): about 0.45 m with flowers, for furniture tops ─
    private static GameObject BuildMediumVase(string rootName, Material matVase, Material stem, Material petalA, Material petalB)
    {
        var root = new GameObject(rootName);
        AddCylinder("Foot", root.transform, matVase, new Vector3(0, 0.01f, 0), 0.05f, 0.01f);
        AddSphere("Belly", root.transform, matVase, new Vector3(0, 0.10f, 0), 0.08f);
        AddCylinder("Neck", root.transform, matVase, new Vector3(0, 0.20f, 0), 0.035f, 0.04f);
        AddCylinder("Lip", root.transform, matVase, new Vector3(0, 0.245f, 0), 0.045f, 0.008f);

        // Five stems fanning out of the neck, a flower head on each.
        for (var i = 0; i < 5; i++)
        {
            var angle = i * 72f;
            var tilt = i == 0 ? 0f : 18f;
            var dir = Quaternion.Euler(0, angle, 0) * Quaternion.Euler(tilt, 0, 0) * Vector3.up;
            var length = 0.16f + (i % 2) * 0.03f;
            var stemGo = AddCylinder($"Stem{i}", root.transform, stem, Vector3.zero, 0.004f, length * 0.5f);
            stemGo.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            stemGo.transform.localPosition = new Vector3(0, 0.24f, 0) + dir * (length * 0.5f);
            AddSphere($"Flower{i}", root.transform, i % 2 == 0 ? petalA : petalB,
                new Vector3(0, 0.24f, 0) + dir * length, 0.03f);
        }

        AddCollider(root, 0.22f, 0.09f);
        return root;
    }

    // ── Large floor vase (P38): about 0.9 m, tall and narrow-necked ─────
    private static GameObject BuildLargeVase(string rootName, Material matVase, Material band)
    {
        var root = new GameObject(rootName);
        AddCylinder("Foot", root.transform, matVase, new Vector3(0, 0.02f, 0), 0.10f, 0.02f);
        AddCylinder("Body", root.transform, matVase, new Vector3(0, 0.30f, 0), 0.16f, 0.26f);
        AddSphere("Shoulder", root.transform, matVase, new Vector3(0, 0.56f, 0), 0.16f);
        AddCylinder("Band", root.transform, band, new Vector3(0, 0.40f, 0), 0.162f, 0.03f);
        AddCylinder("Neck", root.transform, matVase, new Vector3(0, 0.76f, 0), 0.07f, 0.10f);
        AddCylinder("Lip", root.transform, band, new Vector3(0, 0.87f, 0), 0.09f, 0.015f);

        AddCollider(root, 0.45f, 0.17f);
        return root;
    }

    private static void AddCollider(GameObject root, float centerY, float radius)
    {
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, centerY, 0);
        collider.height = centerY * 2f;
        collider.radius = radius;
    }

    private static void EnsureMatFolder()
    {
        if (AssetDatabase.IsValidFolder(MatDir))
            return;
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        AssetDatabase.CreateFolder(PkgRoot + "/Models/LowPoly", "Materials");
    }

    private static GameObject AddCylinder(
        string name, Transform parent, Material mat, Vector3 localPos, float radius, float halfHeight)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(radius * 2f, halfHeight, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject AddBox(
        string name, Transform parent, Material mat, Vector3 localPos, Vector3 halfExtents)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = halfExtents * 2f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject AddSphere(
        string name, Transform parent, Material mat, Vector3 localPos, float radius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * (radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static Material GetMat(string name, Color color, float metallic, float smoothness)
    {
        var path = $"{MatDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name, color = color };
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
