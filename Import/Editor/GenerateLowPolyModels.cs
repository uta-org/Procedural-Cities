using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates low-poly versions of heavy FBX models using ProBuilder primitives.
/// Targets models that were all-tris and couldn't be effectively decimated.
/// Menu: Tools / Procedural Cities / Generate LowPoly Models
/// </summary>
public class GenerateLowPolyModels : EditorWindow
{
    private static string OutputFolder = "Assets/LowPoly";
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    // ReSharper disable once UnusedMember.Local
    [MenuItem("Tools/Procedural Cities/Generate LowPoly Models")]
    private static void GenerateAll()
    {
        var generators = new List<System.Func<int>>();
        foreach (
            var method in typeof(GenerateLowPolyModels).GetMethods(
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
            )
        )
        {
            if (!method.Name.StartsWith("Generate") || method.ReturnType != typeof(int))
                continue;
            if (method.GetParameters().Length != 0)
                continue;

            generators.Add(
                (System.Func<int>)System.Delegate.CreateDelegate(typeof(System.Func<int>), method)
            );
        }

        generators.Sort((left, right) => string.CompareOrdinal(left.Method.Name, right.Method.Name));
        var count = generators.Count;

        try
        {
            Debug.Log($"[LowPoly] Starting generation of {count} models...");
            EnsureFolders();
            materials.Clear();

            int total = 0;
            for (int i = 0; i < count; i++)
            {
                EditorUtility.DisplayProgressBar("Generating LowPoly Models",
                    $"Model {i + 1}/{count}...", (float)i / count);
                total += generators[i]();
            }

            EditorUtility.DisplayProgressBar("Generating LowPoly Models", "Saving assets...", 1f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LowPoly] Generated {total} low-poly models in {OutputFolder}");
            // EditorUtility.DisplayDialog("LowPoly Generator",
            //     $"Successfully generated {total} low-poly models in:\n{OutputFolder}", "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LowPoly] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            // EditorUtility.DisplayDialog("LowPoly Generator",
            //     $"Error: {ex.Message}\nCheck console for details.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void EnsureFolders()
    {
        // Delete stale mesh/prefab assets to prevent 0-vert corruption on re-run.
        // Materials are reused so we keep them.
        if (AssetDatabase.IsValidFolder("Assets/LowPoly"))
        {
            var stale = AssetDatabase.FindAssets("", new[] { OutputFolder });
            foreach (var guid in stale)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(".asset") || p.EndsWith(".prefab"))
                    AssetDatabase.DeleteAsset(p);
            }
        }
        // Ensure physical directories exist on disk (AssetDatabase.CreateFolder
        // may not sync immediately in Unity 6).
        var absOut = Path.Combine(Application.dataPath, "LowPoly");
        var absMat = Path.Combine(absOut, "Materials");
        Directory.CreateDirectory(absOut);
        Directory.CreateDirectory(absMat);
        AssetDatabase.Refresh();
        if (!AssetDatabase.IsValidFolder("Assets/LowPoly"))
            AssetDatabase.CreateFolder("Assets", "LowPoly");
        if (!AssetDatabase.IsValidFolder("Assets/LowPoly/Materials"))
            AssetDatabase.CreateFolder("Assets/LowPoly", "Materials");
    }

    /// <summary>
    /// Creates a hollow cylinder (tube) mesh with open top.
    /// outerRadius/innerRadius define wall thickness, height is total height.
    /// Bottom is closed (disk), top is open to show interior.
    /// </summary>

    // ========================================
    // SOFA (original: 105K verts) -> ~80 verts
    // Dimensions: h=0.85m w=1.40m (Sofa1)
    // ========================================
    // ReSharper disable UnusedMember.Local
    // GenerateAll discovers the non-public Generate* methods below via reflection.
#if false
    // ========================================
    // BENCH (original: 57K verts) -> ~60 verts
    // Dimensions: h=0.85m w=2.20m
    // ========================================
    static int GenerateBench()
    {
        var root = new GameObject("LowPoly_Bench");
        var matWood = GetMat("LP_Wood_Bench", new Color(0.55f, 0.35f, 0.18f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);

        // Seat planks (3 horizontal)
        for (int i = 0; i < 3; i++)
        {
            var plank = CreateBox(new Vector3(2.0f, 0.04f, 0.12f), matWood);
            plank.transform.SetParent(root.transform);
            plank.transform.localPosition = new Vector3(0, 0.44f, -0.13f + i * 0.13f);
            plank.gameObject.name = $"SeatPlank{i}";
        }

        // Backrest planks (2)
        for (int i = 0; i < 2; i++)
        {
            var plank = CreateBox(new Vector3(2.0f, 0.08f, 0.03f), matWood);
            plank.transform.SetParent(root.transform);
            plank.transform.localPosition = new Vector3(0, 0.58f + i * 0.16f, -0.22f);
            plank.transform.localRotation = Quaternion.Euler(8, 0, 0);
            plank.gameObject.name = $"BackPlank{i}";
        }

        // Metal side supports (2)
        for (int s = 0; s < 2; s++)
        {
            float x = s == 0 ? -0.9f : 0.9f;
            // Front leg
            var legF = CreateBox(new Vector3(0.05f, 0.44f, 0.05f), matMetal);
            legF.transform.SetParent(root.transform);
            legF.transform.localPosition = new Vector3(x, 0.22f, 0.12f);
            legF.gameObject.name = $"LegF{s}";

            // Back leg
            var legB = CreateBox(new Vector3(0.05f, 0.85f, 0.05f), matMetal);
            legB.transform.SetParent(root.transform);
            legB.transform.localPosition = new Vector3(x, 0.425f, -0.22f);
            legB.gameObject.name = $"LegB{s}";
        }

        SavePrefab(root, "LowPoly_Bench");
        return 1;
    }

    // ========================================
    // FOUNTAIN (original: 218K verts) -> ~100 verts
    // Dimensions: h=1.00m w=5.01m
    // ========================================
    static int GenerateFountain()
    {
        var root = new GameObject("LowPoly_Fountain");
        var matStone = GetMat("LP_Fountain_Stone", new Color(0.75f, 0.72f, 0.68f));
        var matStoneDark = GetMat("LP_Fountain_Inner", new Color(0.55f, 0.52f, 0.48f));
        var matWater = GetMat("LP_Fountain_Water", new Color(0.25f, 0.45f, 0.65f, 0.85f), 0f, 0.9f);
        // Enable water emission for subtle glow
        matWater.EnableKeyword("_EMISSION");
        matWater.SetFloat("_UseEmission", 1f);
        matWater.SetColor("_EmissionColor", new Color(0.15f, 0.25f, 0.4f, 1f));
        matWater.SetFloat("_EmissionIntensity", 0.8f);

        // ── Base pool (bottom tier) ──
        // Outer wall
        var poolOuter = CreateCylinder(1.2f, 0.35f, 16, matStone);
        poolOuter.transform.SetParent(root.transform);
        poolOuter.transform.localPosition = new Vector3(0, 0.175f, 0);
        poolOuter.gameObject.name = "PoolOuter";

        // Pool inner (slightly inset, slightly shorter to create rim)
        var poolInner = CreateCylinder(1.08f, 0.30f, 16, matStoneDark);
        poolInner.transform.SetParent(root.transform);
        poolInner.transform.localPosition = new Vector3(0, 0.18f, 0);
        poolInner.gameObject.name = "PoolInner";

        // Water surface in pool
        var water1 = CreateCylinder(1.05f, 0.02f, 16, matWater);
        water1.transform.SetParent(root.transform);
        water1.transform.localPosition = new Vector3(0, 0.30f, 0);
        water1.gameObject.name = "WaterPool";

        // Pool base step (decorative ring at bottom)
        var baseStep = CreateCylinder(1.3f, 0.06f, 16, matStone);
        baseStep.transform.SetParent(root.transform);
        baseStep.transform.localPosition = new Vector3(0, 0.03f, 0);
        baseStep.gameObject.name = "BaseStep";

        // ── Central pedestal ──
        // Lower pedestal (wider base)
        var pedBase = CreateCylinder(0.22f, 0.15f, 10, matStone);
        pedBase.transform.SetParent(root.transform);
        pedBase.transform.localPosition = new Vector3(0, 0.35f + 0.075f, 0);
        pedBase.gameObject.name = "PedestalBase";

        // Central column
        var column = CreateCylinder(0.14f, 0.35f, 10, matStone);
        column.transform.SetParent(root.transform);
        column.transform.localPosition = new Vector3(0, 0.50f + 0.175f, 0);
        column.gameObject.name = "Column";

        // ── Middle basin (second tier) ──
        // Basin bowl
        var midBasin = CreateCylinder(0.48f, 0.10f, 12, matStone);
        midBasin.transform.SetParent(root.transform);
        midBasin.transform.localPosition = new Vector3(0, 0.88f, 0);
        midBasin.gameObject.name = "MidBasin";

        // Basin inner (dark inset)
        var midInner = CreateCylinder(0.40f, 0.06f, 12, matStoneDark);
        midInner.transform.SetParent(root.transform);
        midInner.transform.localPosition = new Vector3(0, 0.89f, 0);
        midInner.gameObject.name = "MidBasinInner";

        // Water in middle basin
        var water2 = CreateCylinder(0.38f, 0.02f, 12, matWater);
        water2.transform.SetParent(root.transform);
        water2.transform.localPosition = new Vector3(0, 0.91f, 0);
        water2.gameObject.name = "WaterMid";

        // Decorative lip ring
        var midLip = CreateCylinder(0.50f, 0.03f, 12, matStone);
        midLip.transform.SetParent(root.transform);
        midLip.transform.localPosition = new Vector3(0, 0.85f, 0);
        midLip.gameObject.name = "MidLip";

        // ── Upper column ──
        var upperCol = CreateCylinder(0.10f, 0.20f, 8, matStone);
        upperCol.transform.SetParent(root.transform);
        upperCol.transform.localPosition = new Vector3(0, 0.93f + 0.10f, 0);
        upperCol.gameObject.name = "UpperColumn";

        // ── Top basin (third tier) ──
        var topBasin = CreateCylinder(0.28f, 0.07f, 10, matStone);
        topBasin.transform.SetParent(root.transform);
        topBasin.transform.localPosition = new Vector3(0, 1.16f, 0);
        topBasin.gameObject.name = "TopBasin";

        // Top basin inner
        var topInner = CreateCylinder(0.22f, 0.04f, 10, matStoneDark);
        topInner.transform.SetParent(root.transform);
        topInner.transform.localPosition = new Vector3(0, 1.17f, 0);
        topInner.gameObject.name = "TopBasinInner";

        // Water in top basin
        var water3 = CreateCylinder(0.20f, 0.015f, 10, matWater);
        water3.transform.SetParent(root.transform);
        water3.transform.localPosition = new Vector3(0, 1.185f, 0);
        water3.gameObject.name = "WaterTop";

        // Top lip ring
        var topLip = CreateCylinder(0.30f, 0.025f, 10, matStone);
        topLip.transform.SetParent(root.transform);
        topLip.transform.localPosition = new Vector3(0, 1.14f, 0);
        topLip.gameObject.name = "TopLip";

        // ── Crown finial ──
        // Spout nozzle
        var spout = CreateCylinder(0.06f, 0.12f, 8, matStone);
        spout.transform.SetParent(root.transform);
        spout.transform.localPosition = new Vector3(0, 1.19f + 0.06f, 0);
        spout.gameObject.name = "Spout";

        // Top cap
        var cap = CreateCylinder(0.09f, 0.03f, 8, matStone);
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0, 1.32f, 0);
        cap.gameObject.name = "Cap";

        SavePrefab(root, "LowPoly_Fountain");
        return 1;
    }

    // ========================== BATCH 2 ==========================

    // AWNING h=1.50 w=3.83
    static int GenerateAwning()
    {
        var root = new GameObject("LowPoly_Awning");
        var matFabric = GetMat("LP_Fabric_Red", new Color(0.7f, 0.15f, 0.12f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        // Canopy (angled)
        var canopy = CreateBox(new Vector3(3.8f, 0.04f, 1.2f), matFabric);
        canopy.transform.SetParent(root.transform);
        canopy.transform.localPosition = new Vector3(0, 1.35f, 0.3f);
        canopy.transform.localRotation = Quaternion.Euler(15, 0, 0);
        canopy.gameObject.name = "Canopy";
        // Front bar
        var bar = CreateBox(new Vector3(3.8f, 0.04f, 0.04f), matMetal);
        bar.transform.SetParent(root.transform);
        bar.transform.localPosition = new Vector3(0, 1.1f, 0.85f);
        bar.gameObject.name = "FrontBar";
        // Support rods (2)
        for (int i = 0; i < 2; i++)
        {
            var rod = CreateBox(new Vector3(0.03f, 0.03f, 0.8f), matMetal);
            rod.transform.SetParent(root.transform);
            rod.transform.localPosition = new Vector3(i == 0 ? -1.7f : 1.7f, 1.25f, 0.5f);
            rod.transform.localRotation = Quaternion.Euler(15, 0, 0);
            rod.gameObject.name = $"Rod{i}";
        }
        SavePrefab(root, "LowPoly_Awning");
        return 1;
    }

    // FENCE h=1.20 w=1.78
    static int GenerateFence()
    {
        var root = new GameObject("LowPoly_Fence");
        var matWood = GetMat("LP_Wood_Fence", new Color(0.52f, 0.38f, 0.22f));
        // Horizontal rails (2)
        for (int i = 0; i < 2; i++)
        {
            var rail = CreateBox(new Vector3(1.78f, 0.06f, 0.04f), matWood);
            rail.transform.SetParent(root.transform);
            rail.transform.localPosition = new Vector3(0, 0.3f + i * 0.55f, 0);
            rail.gameObject.name = $"Rail{i}";
        }
        // Vertical pickets (7)
        for (int i = 0; i < 7; i++)
        {
            var picket = CreateBox(new Vector3(0.06f, 1.2f, 0.02f), matWood);
            picket.transform.SetParent(root.transform);
            picket.transform.localPosition = new Vector3(-0.78f + i * 0.26f, 0.6f, 0);
            picket.gameObject.name = $"Picket{i}";
        }
        SavePrefab(root, "LowPoly_Fence");
        return 1;
    }

    // FIREHYDRANT h=0.60 w=0.41
    static int GenerateFireHydrant()
    {
        var root = new GameObject("LowPoly_FireHydrant");
        var matRed = GetMat("LP_Paint_Red", new Color(0.75f, 0.1f, 0.1f));
        var matCap = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        // Body
        var body = CreateCylinder(0.1f, 0.45f, 8, matRed);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.225f, 0);
        body.gameObject.name = "Body";
        // Top cap
        var cap = CreateCylinder(0.07f, 0.08f, 8, matCap);
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0, 0.5f, 0);
        cap.gameObject.name = "Cap";
        // Side nozzles
        for (int i = 0; i < 2; i++)
        {
            var nozzle = CreateCylinder(0.04f, 0.1f, 6, matRed);
            nozzle.transform.SetParent(root.transform);
            nozzle.transform.localPosition = new Vector3(i == 0 ? -0.14f : 0.14f, 0.32f, 0);
            nozzle.transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 90 : -90);
            nozzle.gameObject.name = $"Nozzle{i}";
        }
        SavePrefab(root, "LowPoly_FireHydrant");
        return 1;
    }

    // LAMPPOST h=4.50 w=1.41
    static int GenerateLamppost()
    {
        var root = new GameObject("LowPoly_Lamppost");
        var matPole = GetMat("LP_Lamppost_Pole", new Color(0.25f, 0.25f, 0.27f), 0.6f, 0.45f);
        var matBase = GetMat("LP_Lamppost_Base", new Color(0.20f, 0.20f, 0.22f), 0.5f, 0.4f);
        var matArm = GetMat("LP_Lamppost_Arm", new Color(0.28f, 0.28f, 0.30f), 0.55f, 0.4f);
        var matHousing = GetMat("LP_Lamppost_Housing", new Color(0.35f, 0.35f, 0.38f), 0.5f, 0.35f);
        var matLens = GetMat("LP_Lamppost_Lens", new Color(0.95f, 0.92f, 0.75f), 0.0f, 0.8f);

        // Base - wider octagonal cylinder
        var baseBottom = CreateCylinder(0.18f, 0.08f, 8, matBase);
        baseBottom.transform.SetParent(root.transform);
        baseBottom.transform.localPosition = new Vector3(0, 0.04f, 0);
        baseBottom.gameObject.name = "BaseBottom";

        var baseTop = CreateCylinder(0.14f, 0.06f, 8, matBase);
        baseTop.transform.SetParent(root.transform);
        baseTop.transform.localPosition = new Vector3(0, 0.11f, 0);
        baseTop.gameObject.name = "BaseTop";

        // Lower pole section (thicker)
        var poleLower = CreateCylinder(0.055f, 1.5f, 8, matPole);
        poleLower.transform.SetParent(root.transform);
        poleLower.transform.localPosition = new Vector3(0, 0.89f, 0);
        poleLower.gameObject.name = "PoleLower";

        // Upper pole section (thinner, tapered)
        var poleUpper = CreateCylinder(0.045f, 2.5f, 8, matPole);
        poleUpper.transform.SetParent(root.transform);
        poleUpper.transform.localPosition = new Vector3(0, 2.89f, 0);
        poleUpper.gameObject.name = "PoleUpper";

        // Curved gooseneck arm – smooth cylinder pipe
        int armSteps = 16;
        float armRadius = 0.55f;
        float armCenterY = 4.14f;
        float startAngle = 85f;
        float endAngle = -5f;
        float pipeR = 0.03f;

        for (int i = 0; i < armSteps; i++)
        {
            float t0 = (float)i / armSteps;
            float t1 = (float)(i + 1) / armSteps;
            float a0 = Mathf.Lerp(startAngle, endAngle, t0) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(startAngle, endAngle, t1) * Mathf.Deg2Rad;

            float z0 = Mathf.Cos(a0) * armRadius;
            float y0 = Mathf.Sin(a0) * armRadius + armCenterY - armRadius;
            float z1 = Mathf.Cos(a1) * armRadius;
            float y1 = Mathf.Sin(a1) * armRadius + armCenterY - armRadius;

            float midZ = (z0 + z1) * 0.5f;
            float midY = (y0 + y1) * 0.5f;
            float dz = z1 - z0;
            float dy = y1 - y0;
            float segLen = Mathf.Sqrt(dz * dz + dy * dy) * 1.2f;
            float rotX = Mathf.Atan2(dz, dy) * Mathf.Rad2Deg;

            var seg = CreateCylinder(pipeR, segLen, 6, matArm);
            seg.transform.SetParent(root.transform);
            seg.transform.localPosition = new Vector3(0, midY, midZ);
            seg.transform.localRotation = Quaternion.Euler(rotX, 0, 0);
            seg.gameObject.name = $"Arm{i}";
        }

        // Light housing - positioned at arm endpoint
        // arm end at -5°: z=cos(-5)*0.55=0.548, y=sin(-5)*0.55+3.59=3.542
        var housing = CreateBox(new Vector3(0.22f, 0.06f, 0.38f), matHousing);
        housing.transform.SetParent(root.transform);
        housing.transform.localPosition = new Vector3(0, 3.51f, 0.55f);
        housing.transform.localRotation = Quaternion.Euler(5f, 0, 0);
        housing.gameObject.name = "Housing";

        // Light lens (bottom of housing)
        var lens = CreateBox(new Vector3(0.18f, 0.02f, 0.32f), matLens);
        lens.transform.SetParent(root.transform);
        lens.transform.localPosition = new Vector3(0, 3.47f, 0.55f);
        lens.gameObject.name = "Lens";

        // Small collar where pole meets arm
        var collar = CreateCylinder(0.06f, 0.06f, 8, matBase);
        collar.transform.SetParent(root.transform);
        collar.transform.localPosition = new Vector3(0, 4.14f, 0);
        collar.gameObject.name = "Collar";

        SavePrefab(root, "LowPoly_Lamppost");
        return 1;
    }

    // RUBBISHBIN h=0.90 w=0.86
    static int GenerateRubbishBin()
    {
        var root = new GameObject("LowPoly_RubbishBin");
        var matPlastic = GetMat("LP_Plastic_Green_Bin", new Color(0.15f, 0.4f, 0.15f));
        var body = CreateCylinder(0.35f, 0.85f, 8, matPlastic);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.425f, 0);
        body.gameObject.name = "Body";
        // Lid
        var lid = CreateCylinder(0.37f, 0.05f, 8, matPlastic);
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.875f, 0);
        lid.gameObject.name = "Lid";
        SavePrefab(root, "LowPoly_RubbishBin");
        return 1;
    }

    // TRAFFICLIGHT h=5.00 w=0.68
    static int GenerateTrafficLight()
    {
        var root = new GameObject("LowPoly_TrafficLight");
        var matPole = GetMat("LP_Metal_Lamppost", new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.4f);
        var matBody = GetMat("LP_Metal_Black", new Color(0.1f, 0.1f, 0.1f), 0.5f, 0.3f);
        var matRed = GetMat("LP_Light_Red", new Color(0.9f, 0.1f, 0.1f));
        var matYellow = GetMat("LP_Light_Yellow", new Color(0.9f, 0.8f, 0.1f));
        var matGreen = GetMat("LP_Light_Green", new Color(0.1f, 0.85f, 0.15f));
        // Pole
        var pole = CreateCylinder(0.06f, 4.2f, 8, matPole);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 2.1f, 0);
        pole.gameObject.name = "Pole";
        // Housing
        var housing = CreateBox(new Vector3(0.3f, 0.85f, 0.2f), matBody);
        housing.transform.SetParent(root.transform);
        housing.transform.localPosition = new Vector3(0, 4.55f, 0);
        housing.gameObject.name = "Housing";
        // Lights
        Material[] lightMats = { matRed, matYellow, matGreen };
        for (int i = 0; i < 3; i++)
        {
            var light = CreateCylinder(0.08f, 0.02f, 8, lightMats[i]);
            light.transform.SetParent(root.transform);
            light.transform.localPosition = new Vector3(0, 4.8f - i * 0.25f, 0.11f);
            light.transform.localRotation = Quaternion.Euler(90, 0, 0);
            light.gameObject.name = $"Light{i}";
        }
        SavePrefab(root, "LowPoly_TrafficLight");
        return 1;
    }

    // TRASHBOX h=0.70 w=0.75
    static int GenerateTrashBox()
    {
        var root = new GameObject("LowPoly_TrashBox");
        var matMetal = GetMat("LP_Metal_TrashBox", new Color(0.45f, 0.45f, 0.48f), 0.4f, 0.3f);
        var body = CreateBox(new Vector3(0.7f, 0.65f, 0.5f), matMetal);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.325f, 0);
        body.gameObject.name = "Body";
        // Lid (hinged)
        var lid = CreateBox(new Vector3(0.72f, 0.03f, 0.52f), matMetal);
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.665f, 0);
        lid.gameObject.name = "Lid";
        SavePrefab(root, "LowPoly_TrashBox");
        return 1;
    }

    // TRASHCAN h=0.35 w=0.19
    static int GenerateTrashCan()
    {
        var root = new GameObject("LowPoly_TrashCan");
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var can = CreateCylinder(0.09f, 0.32f, 8, matMetal);
        can.transform.SetParent(root.transform);
        can.transform.localPosition = new Vector3(0, 0.16f, 0);
        can.gameObject.name = "Can";
        SavePrefab(root, "LowPoly_TrashCan");
        return 1;
    }

    // ====== TREES ======
    // Shared tree materials
    static Material TreeTrunk => GetMat("LP_Tree_Trunk", new Color(0.40f, 0.26f, 0.13f), 0f, 0.15f);
    static Material TreeLeaf => GetMat("LP_Tree_Leaf", new Color(0.18f, 0.42f, 0.12f), 0f, 0.1f);
    static Material TreeLeafDark => GetMat("LP_Tree_LeafDark", new Color(0.12f, 0.32f, 0.08f), 0f, 0.1f);
    static Material TreeLeafLight => GetMat("LP_Tree_LeafLight", new Color(0.30f, 0.55f, 0.18f), 0f, 0.1f);
    static Material TreeLeafYellow => GetMat("LP_Tree_LeafYellow", new Color(0.65f, 0.58f, 0.12f), 0f, 0.1f);
    static Material TreeLeafOrange => GetMat("LP_Tree_LeafOrange", new Color(0.72f, 0.38f, 0.10f), 0f, 0.1f);

    // Tree: classic conical pine/fir — 3 stacked cones + cylinder trunk
    static int GenerateTree()
    {
        var root = new GameObject("LowPoly_Tree");
        var matTrunk = TreeTrunk;
        var matLeaf = TreeLeafDark;

        // Trunk
        var trunk = CreateCylinder(0.18f, 1.5f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 0.75f, 0);
        trunk.gameObject.name = "Trunk";

        // 3 stacked cones (bottom to top: larger to smaller)
        float[] coneR = { 1.35f, 1.05f, 0.75f };
        float[] coneH = { 1.50f, 1.35f, 1.20f };
        float[] coneY = { 1.35f, 2.25f, 3.15f };
        for (int i = 0; i < 3; i++)
        {
            var cone = ShapeGenerator.GenerateCone(PivotLocation.Center, coneR[i], coneH[i], 7);
            cone.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
            cone.transform.SetParent(root.transform);
            cone.transform.localPosition = new Vector3(0, coneY[i] + coneH[i] / 2f, 0);
            cone.gameObject.name = $"Canopy{i}";
        }

        SavePrefab(root, "LowPoly_Tree");
        return 1;
    }

    // Tree1: round deciduous tree — icosahedron canopy + cylinder trunk
    static int GenerateTree1()
    {
        var root = new GameObject("LowPoly_Tree1");
        var matTrunk = TreeTrunk;
        var matLeaf = TreeLeaf;

        // Trunk
        var trunk = CreateCylinder(0.21f, 2.4f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 1.2f, 0);
        trunk.gameObject.name = "Trunk";

        // Main canopy sphere
        var canopy = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 1.65f, 1);
        canopy.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
        canopy.transform.SetParent(root.transform);
        canopy.transform.localPosition = new Vector3(0, 3.45f, 0);
        canopy.gameObject.name = "Canopy";

        // Secondary smaller sphere for volume
        var canopy2 = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 1.14f, 1);
        canopy2.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
        canopy2.transform.SetParent(root.transform);
        canopy2.transform.localPosition = new Vector3(0.6f, 3.0f, 0.45f);
        canopy2.gameObject.name = "Canopy1";

        SavePrefab(root, "LowPoly_Tree1");
        return 1;
    }

    // Tree2: oak/wide tree — flat wide canopy with multiple icosahedrons
    static int GenerateTree2()
    {
        var root = new GameObject("LowPoly_Tree2");
        var matTrunk = TreeTrunk;
        var matLeaf = TreeLeafLight;

        // Trunk (thicker, shorter)
        var trunk = CreateCylinder(0.27f, 1.8f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 0.9f, 0);
        trunk.gameObject.name = "Trunk";

        // Wide canopy: 4 overlapping icosahedrons
        Vector3[] offsets = {
            new Vector3(0, 2.85f, 0),
            new Vector3(0.75f, 2.55f, 0.6f),
            new Vector3(-0.66f, 2.64f, -0.45f),
            new Vector3(-0.3f, 2.40f, 0.84f),
        };
        float[] sizes = { 1.50f, 1.20f, 1.14f, 1.05f };
        for (int i = 0; i < 4; i++)
        {
            var sphere = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, sizes[i], 1);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
            sphere.transform.SetParent(root.transform);
            sphere.transform.localPosition = offsets[i];
            sphere.gameObject.name = $"Canopy{i}";
        }

        SavePrefab(root, "LowPoly_Tree2");
        return 1;
    }

    // Tree3: autumn/fall tree — single cone canopy, warm colors
    static int GenerateTree3()
    {
        var root = new GameObject("LowPoly_Tree3");
        var matTrunk = TreeTrunk;

        // Trunk
        var trunk = CreateCylinder(0.195f, 2.1f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 1.05f, 0);
        trunk.gameObject.name = "Trunk";

        // Single large cone canopy (autumn yellow-orange)
        var canopy = ShapeGenerator.GenerateCone(PivotLocation.Center, 1.50f, 2.55f, 8);
        canopy.GetComponent<MeshRenderer>().sharedMaterial = TreeLeafYellow;
        canopy.transform.SetParent(root.transform);
        canopy.transform.localPosition = new Vector3(0, 1.95f + 1.275f, 0);
        canopy.gameObject.name = "Canopy";

        // Smaller orange accent cone on top
        var top = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.84f, 1.35f, 7);
        top.GetComponent<MeshRenderer>().sharedMaterial = TreeLeafOrange;
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 3.45f + 0.675f, 0);
        top.gameObject.name = "CanopyTop";

        SavePrefab(root, "LowPoly_Tree3");
        return 1;
    }

    // Tree4: tall organic deciduous — elongated canopy of overlapping icosahedrons
    static int GenerateTree4()
    {
        var root = new GameObject("LowPoly_Tree4");
        var matTrunk = TreeTrunk;
        var matLeaf = TreeLeaf;
        var matLeafDark = TreeLeafDark;

        // Trunk — slim and tall
        var trunk = CreateCylinder(0.15f, 2.4f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 1.2f, 0);
        trunk.gameObject.name = "Trunk";

        // Elongated organic canopy: 5 overlapping spheres stacked vertically
        Vector3[] offsets = {
            new Vector3(0, 2.6f, 0),         // bottom cluster
            new Vector3(0.25f, 3.2f, 0.15f), // mid-left
            new Vector3(-0.2f, 3.7f, -0.1f), // mid-right
            new Vector3(0.1f, 4.3f, 0.12f),  // upper
            new Vector3(-0.05f, 4.8f, -0.05f), // top
        };
        float[] sizes = { 1.0f, 1.1f, 1.05f, 0.9f, 0.7f };
        Material[] mats = { matLeaf, matLeafDark, matLeaf, matLeafDark, matLeaf };
        for (int i = 0; i < 5; i++)
        {
            var sphere = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, sizes[i], 1);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = mats[i];
            sphere.transform.SetParent(root.transform);
            sphere.transform.localPosition = offsets[i];
            sphere.gameObject.name = $"Canopy{i}";
        }

        SavePrefab(root, "LowPoly_Tree4");
        return 1;
    }

    // Tree5: dead/dry tree — bare trunk with exposed branches, no leaves
    static int GenerateTree5()
    {
        var root = new GameObject("LowPoly_Tree5");
        var matTrunk = TreeTrunk;

        // Main trunk — slightly tapered feel via thicker base
        var trunk = CreateCylinder(0.18f, 3.0f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
        trunk.gameObject.name = "Trunk";

        // Upper trunk (thinner continuation)
        var upperTrunk = CreateCylinder(0.12f, 1.5f, 5, matTrunk);
        upperTrunk.transform.SetParent(root.transform);
        upperTrunk.transform.localPosition = new Vector3(0, 3.75f, 0);
        upperTrunk.gameObject.name = "UpperTrunk";

        // Branch 1 — lower right, angled out
        var b1 = CreateCylinder(0.07f, 1.8f, 5, matTrunk);
        b1.transform.SetParent(root.transform);
        b1.transform.localPosition = new Vector3(0.6f, 2.4f, 0.2f);
        b1.transform.localRotation = Quaternion.Euler(0, 0, -40f);
        b1.gameObject.name = "Branch0";

        // Branch 2 — lower left
        var b2 = CreateCylinder(0.065f, 1.5f, 5, matTrunk);
        b2.transform.SetParent(root.transform);
        b2.transform.localPosition = new Vector3(-0.5f, 2.8f, -0.3f);
        b2.transform.localRotation = Quaternion.Euler(0, 30f, 45f);
        b2.gameObject.name = "Branch1";

        // Branch 3 — mid right, shorter
        var b3 = CreateCylinder(0.055f, 1.2f, 5, matTrunk);
        b3.transform.SetParent(root.transform);
        b3.transform.localPosition = new Vector3(0.45f, 3.5f, -0.15f);
        b3.transform.localRotation = Quaternion.Euler(10f, -20f, -50f);
        b3.gameObject.name = "Branch2";

        // Branch 4 — upper left
        var b4 = CreateCylinder(0.05f, 1.0f, 5, matTrunk);
        b4.transform.SetParent(root.transform);
        b4.transform.localPosition = new Vector3(-0.35f, 4.0f, 0.25f);
        b4.transform.localRotation = Quaternion.Euler(-15f, 40f, 55f);
        b4.gameObject.name = "Branch3";

        // Branch 5 — top fork right
        var b5 = CreateCylinder(0.045f, 0.9f, 5, matTrunk);
        b5.transform.SetParent(root.transform);
        b5.transform.localPosition = new Vector3(0.25f, 4.3f, 0.1f);
        b5.transform.localRotation = Quaternion.Euler(5f, -10f, -35f);
        b5.gameObject.name = "Branch4";

        // Branch 6 — top fork left
        var b6 = CreateCylinder(0.04f, 0.8f, 5, matTrunk);
        b6.transform.SetParent(root.transform);
        b6.transform.localPosition = new Vector3(-0.2f, 4.5f, -0.1f);
        b6.transform.localRotation = Quaternion.Euler(0, 20f, 40f);
        b6.gameObject.name = "Branch5";

        SavePrefab(root, "LowPoly_Tree5");
        return 1;
    }

    // Tree6: palm tree — tall curved trunk with fan of flat cone fronds at top
    static int GenerateTree6()
    {
        var root = new GameObject("LowPoly_Tree6");
        var matTrunk = TreeTrunk;
        var matLeaf = TreeLeaf;

        // Trunk — tall and slim
        var trunk = CreateCylinder(0.12f, 4.0f, 6, matTrunk);
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 2.0f, 0);
        trunk.gameObject.name = "Trunk";

        // Upper trunk — slightly thinner continuation
        var upperTrunk = CreateCylinder(0.09f, 1.2f, 6, matTrunk);
        upperTrunk.transform.SetParent(root.transform);
        upperTrunk.transform.localPosition = new Vector3(0, 4.6f, 0);
        upperTrunk.gameObject.name = "UpperTrunk";

        // Palm fronds — 6 flat cones radiating outward from top
        float frondY = 5.0f;
        float frondLen = 2.2f;
        float frondRadius = 0.6f;
        float[] angles = { 0f, 60f, 120f, 180f, 240f, 300f };
        float[] tilts = { 35f, 30f, 40f, 32f, 38f, 28f };
        for (int i = 0; i < 6; i++)
        {
            var frond = ShapeGenerator.GenerateCone(PivotLocation.Center, frondRadius, frondLen, 4);
            frond.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
            frond.transform.SetParent(root.transform);

            // Position at top of trunk, then rotate outward
            float rad = angles[i] * Mathf.Deg2Rad;
            float offsetX = Mathf.Sin(rad) * 0.3f;
            float offsetZ = Mathf.Cos(rad) * 0.3f;
            frond.transform.localPosition = new Vector3(offsetX, frondY, offsetZ);
            frond.transform.localRotation = Quaternion.Euler(tilts[i], angles[i], 0);
            frond.gameObject.name = $"Frond{i}";
        }

        // Small coconut cluster — 3 tiny spheres
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            var coconut = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.12f, 0);
            coconut.GetComponent<MeshRenderer>().sharedMaterial = matTrunk;
            coconut.transform.SetParent(root.transform);
            coconut.transform.localPosition = new Vector3(
                Mathf.Sin(a) * 0.15f, 5.05f, Mathf.Cos(a) * 0.15f);
            coconut.gameObject.name = $"Coconut{i}";
        }

        SavePrefab(root, "LowPoly_Tree6");
        return 1;
    }
#endif
    // ReSharper restore UnusedMember.Local
}