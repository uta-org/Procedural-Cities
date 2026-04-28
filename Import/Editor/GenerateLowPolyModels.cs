using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Generates low-poly versions of heavy FBX models using ProBuilder primitives.
/// Targets models that were all-tris and couldn't be effectively decimated.
/// Menu: Procedural Cities / Generate LowPoly Models
/// </summary>
public class GenerateLowPolyModels : EditorWindow
{
    static string OutputFolder = "Assets/LowPoly";
    static string MatFolder = "Assets/LowPoly/Materials";

    // Cached materials
    static Dictionary<string, Material> materials = new Dictionary<string, Material>();

    static System.Func<int>[] AllGenerators => new System.Func<int>[]
    {
        GenerateSofa, GenerateBench, GenerateSink, GenerateOven, GenerateTV,
        GenerateDispenser, GenerateStair, GenerateFountain, GenerateWardrobe,
        GenerateShelf, GenerateFridge, GenerateToilet, GenerateKitchenCounter,
        GenerateHanger, GenerateMirror,
        GenerateAwning, GenerateBed, GenerateBush, GenerateChair, GenerateChoppingBoard,
        GenerateClock, GenerateComputer, GenerateComputerUser, GenerateCup,
        GenerateDoor, GenerateDoorFrame, GenerateElevator, GenerateFence,
        GenerateFireHydrant, GenerateGlass, GenerateGrass, GenerateHanger1,
        GenerateKettle, GenerateKitchen2, GenerateKitchen3, GenerateKitchen4,
        GenerateLamp0, GenerateLamp1, GenerateLamp2, GenerateLamp3, GenerateLamp4,
        GenerateLamppost, GenerateLargeTable, GenerateLocker, GenerateMirror1,
        GenerateMirror2, GenerateOfficeChair, GenerateOfficeCubicle,
        GenerateOfficeMeetingTable, GenerateOfficeTable, GenerateOfficeWhiteboard,
        GeneratePan0, GeneratePan1, GenerateRestaurantChair, GenerateRestaurantTable,
        GenerateRooftopAc, GenerateRooftopSolar, GenerateRubbishBin,
        GenerateShelf1, GenerateShelf2, GenerateShelf3, GenerateShelf4, GenerateShelf5,
        GenerateSmallTable, GenerateSofa1, GenerateStoreShelf, GenerateToaster,
        GenerateToilet1, GenerateTrafficLight, GenerateTrashBox, GenerateTrashCan,
        GenerateVase,
        // Modular Kitchen (1x1x1 units, 7DTD-style)
        GenerateKM_CabinetBase, GenerateKM_CabinetDrawer, GenerateKM_Sink,
        GenerateKM_Stove, GenerateKM_Oven, GenerateKM_CabinetWall,
        GenerateKM_CabinetCorner, GenerateKM_Countertop, GenerateKM_Fridge,
        GenerateKM_Dishwasher, GenerateKM_Hood, GenerateKM_Microwave,
        GenerateKM_ShelfOpen, GenerateKM_Island,
    };

    [MenuItem("Procedural Cities/Generate LowPoly Models")]
    static void Generate()
    {
        var generators = AllGenerators;
        int count = generators.Length;
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
            EditorUtility.DisplayDialog("LowPoly Generator",
                $"Successfully generated {total} low-poly models in:\n{OutputFolder}", "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LowPoly] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("LowPoly Generator",
                $"Error: {ex.Message}\nCheck console for details.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void EnsureFolders()
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

    static Material GetMat(string name, Color color, float metallic = 0f, float smoothness = 0.3f)
    {
        if (materials.TryGetValue(name, out var cached)) return cached;

        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }
        materials[name] = mat;
        return mat;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        string path = $"{OutputFolder}/{name}.prefab";

        // Strip ProBuilder components before saving (keep only MeshFilter/MeshRenderer)
        foreach (var pb in go.GetComponentsInChildren<ProBuilderMesh>())
        {
            pb.ToMesh();
            pb.Refresh();
            // Bake the mesh
            var mf = pb.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var bakedMesh = Object.Instantiate(mf.sharedMesh);
                bakedMesh.name = pb.gameObject.name;
                string meshPath = $"{OutputFolder}/{name}_{pb.gameObject.name}.asset";
                AssetDatabase.CreateAsset(bakedMesh, meshPath);
                mf.sharedMesh = bakedMesh;
            }
            Object.DestroyImmediate(pb);
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        int verts = 0;
        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount;
        Debug.Log($"[LowPoly] {name}: {verts} verts");
        return prefab;
    }

    static ProBuilderMesh CreateBox(Vector3 size, Material mat)
    {
        var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
        pb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return pb;
    }

    static ProBuilderMesh CreateCylinder(float radius, float height, int sides, Material mat)
    {
        var pb = ShapeGenerator.GenerateCylinder(PivotLocation.Center, sides, radius, height, 1, -1);
        pb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return pb;
    }

    // ========================================
    // SOFA (original: 105K verts) -> ~80 verts
    // Dimensions: h=0.85m w=1.40m (Sofa1)
    // ========================================
    static int GenerateSofa()
    {
        var root = new GameObject("LowPoly_Sofa");

        var matFabric = GetMat("LP_Fabric_Gray", new Color(0.45f, 0.42f, 0.40f));
        var matCushion = GetMat("LP_Fabric_DarkGray", new Color(0.35f, 0.32f, 0.30f));
        var matLegs = GetMat("LP_Wood_Dark", new Color(0.25f, 0.15f, 0.08f));

        // Seat base
        var seat = CreateBox(new Vector3(1.4f, 0.2f, 0.6f), matFabric);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.25f, 0);
        seat.gameObject.name = "Seat";

        // Back rest
        var back = CreateBox(new Vector3(1.4f, 0.5f, 0.12f), matFabric);
        back.transform.SetParent(root.transform);
        back.transform.localPosition = new Vector3(0, 0.55f, -0.24f);
        back.gameObject.name = "Back";

        // Left armrest
        var armL = CreateBox(new Vector3(0.12f, 0.3f, 0.6f), matCushion);
        armL.transform.SetParent(root.transform);
        armL.transform.localPosition = new Vector3(-0.64f, 0.4f, 0);
        armL.gameObject.name = "ArmLeft";

        // Right armrest
        var armR = CreateBox(new Vector3(0.12f, 0.3f, 0.6f), matCushion);
        armR.transform.SetParent(root.transform);
        armR.transform.localPosition = new Vector3(0.64f, 0.4f, 0);
        armR.gameObject.name = "ArmRight";

        // Cushions (2)
        var cush1 = CreateBox(new Vector3(0.6f, 0.08f, 0.5f), matCushion);
        cush1.transform.SetParent(root.transform);
        cush1.transform.localPosition = new Vector3(-0.32f, 0.39f, 0.02f);
        cush1.gameObject.name = "Cushion1";

        var cush2 = CreateBox(new Vector3(0.6f, 0.08f, 0.5f), matCushion);
        cush2.transform.SetParent(root.transform);
        cush2.transform.localPosition = new Vector3(0.32f, 0.39f, 0.02f);
        cush2.gameObject.name = "Cushion2";

        // 4 legs
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.58f : 0.58f);
            float z = (i < 2 ? -0.22f : 0.22f);
            var leg = CreateBox(new Vector3(0.06f, 0.15f, 0.06f), matLegs);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.075f, z);
            leg.gameObject.name = $"Leg{i}";
        }

        SavePrefab(root, "LowPoly_Sofa");
        return 1;
    }

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
    // SINK (original: 41K verts) -> ~50 verts
    // Dimensions: h=0.85m w=0.67m
    // ========================================
    static int GenerateSink()
    {
        var root = new GameObject("LowPoly_Sink");
        var matCeramic = GetMat("LP_Ceramic_White", new Color(0.92f, 0.92f, 0.90f), 0f, 0.7f);
        var matChrome = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);

        // Basin (outer box)
        var basin = CreateBox(new Vector3(0.6f, 0.18f, 0.45f), matCeramic);
        basin.transform.SetParent(root.transform);
        basin.transform.localPosition = new Vector3(0, 0.76f, 0);
        basin.gameObject.name = "Basin";

        // Basin inner (recessed - smaller, darker)
        var inner = CreateBox(new Vector3(0.5f, 0.14f, 0.35f), GetMat("LP_Ceramic_Inner", new Color(0.85f, 0.85f, 0.83f)));
        inner.transform.SetParent(root.transform);
        inner.transform.localPosition = new Vector3(0, 0.78f, 0);
        inner.gameObject.name = "BasinInner";

        // Pedestal
        var pedestal = CreateBox(new Vector3(0.2f, 0.67f, 0.2f), matCeramic);
        pedestal.transform.SetParent(root.transform);
        pedestal.transform.localPosition = new Vector3(0, 0.335f, 0);
        pedestal.gameObject.name = "Pedestal";

        // Faucet base
        var faucetBase = CreateCylinder(0.02f, 0.12f, 8, matChrome);
        faucetBase.transform.SetParent(root.transform);
        faucetBase.transform.localPosition = new Vector3(0, 0.91f, -0.15f);
        faucetBase.gameObject.name = "FaucetBase";

        // Faucet spout
        var spout = CreateBox(new Vector3(0.03f, 0.03f, 0.12f), matChrome);
        spout.transform.SetParent(root.transform);
        spout.transform.localPosition = new Vector3(0, 0.96f, -0.09f);
        spout.gameObject.name = "FaucetSpout";

        SavePrefab(root, "LowPoly_Sink");
        return 1;
    }

    // ========================================
    // OVEN (original: 40K verts) -> ~60 verts
    // Dimensions: h=0.85m w=0.85m
    // ========================================
    static int GenerateOven()
    {
        var root = new GameObject("LowPoly_Oven");
        var matBody = GetMat("LP_Appliance_White", new Color(0.9f, 0.9f, 0.88f));
        var matDoor = GetMat("LP_Oven_Glass", new Color(0.15f, 0.12f, 0.1f), 0.1f, 0.8f);
        var matKnob = GetMat("LP_Appliance_Silver", new Color(0.7f, 0.7f, 0.72f), 0.6f, 0.5f);
        var matBurner = GetMat("LP_Metal_Black", new Color(0.1f, 0.1f, 0.1f), 0.5f, 0.3f);

        // Main body
        var body = CreateBox(new Vector3(0.6f, 0.85f, 0.6f), matBody);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.425f, 0);
        body.gameObject.name = "Body";

        // Oven door window
        var door = CreateBox(new Vector3(0.45f, 0.3f, 0.02f), matDoor);
        door.transform.SetParent(root.transform);
        door.transform.localPosition = new Vector3(0, 0.28f, 0.31f);
        door.gameObject.name = "DoorWindow";

        // Stovetop
        var top = CreateBox(new Vector3(0.6f, 0.02f, 0.6f), matBody);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.86f, 0);
        top.gameObject.name = "Stovetop";

        // Burners (4 cylinders)
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.15f : 0.15f);
            float z = (i < 2 ? -0.15f : 0.15f);
            var burner = CreateCylinder(0.08f, 0.015f, 12, matBurner);
            burner.transform.SetParent(root.transform);
            burner.transform.localPosition = new Vector3(x, 0.878f, z);
            burner.gameObject.name = $"Burner{i}";
        }

        // Knobs (4)
        for (int i = 0; i < 4; i++)
        {
            var knob = CreateCylinder(0.015f, 0.02f, 6, matKnob);
            knob.transform.SetParent(root.transform);
            knob.transform.localPosition = new Vector3(-0.22f + i * 0.14f, 0.68f, 0.31f);
            knob.transform.localRotation = Quaternion.Euler(90, 0, 0);
            knob.gameObject.name = $"Knob{i}";
        }

        SavePrefab(root, "LowPoly_Oven");
        return 1;
    }

    // ========================================
    // TV (original: 64K verts) -> ~30 verts
    // Dimensions: h=0.50m w=0.88m
    // ========================================
    static int GenerateTV()
    {
        var root = new GameObject("LowPoly_TV");
        var matFrame = GetMat("LP_TV_Frame", new Color(0.08f, 0.08f, 0.08f), 0.3f, 0.6f);
        var matScreen = GetMat("LP_TV_Screen", new Color(0.05f, 0.08f, 0.12f), 0f, 0.9f);

        // Screen
        var screen = CreateBox(new Vector3(0.88f, 0.50f, 0.04f), matFrame);
        screen.transform.SetParent(root.transform);
        screen.transform.localPosition = new Vector3(0, 0.35f, 0);
        screen.gameObject.name = "Frame";

        // Screen surface
        var surface = CreateBox(new Vector3(0.82f, 0.44f, 0.005f), matScreen);
        surface.transform.SetParent(root.transform);
        surface.transform.localPosition = new Vector3(0, 0.35f, 0.023f);
        surface.gameObject.name = "Screen";

        // Stand base
        var standBase = CreateBox(new Vector3(0.3f, 0.02f, 0.15f), matFrame);
        standBase.transform.SetParent(root.transform);
        standBase.transform.localPosition = new Vector3(0, 0.01f, 0);
        standBase.gameObject.name = "StandBase";

        // Stand neck
        var standNeck = CreateBox(new Vector3(0.06f, 0.08f, 0.06f), matFrame);
        standNeck.transform.SetParent(root.transform);
        standNeck.transform.localPosition = new Vector3(0, 0.06f, 0);
        standNeck.gameObject.name = "StandNeck";

        SavePrefab(root, "LowPoly_TV");
        return 1;
    }

    // ========================================
    // DISPENSER (original: 68K verts) -> ~40 verts
    // Dimensions: h=1.10m w=0.39m
    // ========================================
    static int GenerateDispenser()
    {
        var root = new GameObject("LowPoly_Dispenser");
        var matBody = GetMat("LP_Appliance_White", new Color(0.9f, 0.9f, 0.88f));
        var matTop = GetMat("LP_Plastic_LightGray", new Color(0.8f, 0.8f, 0.82f));
        var matTap = GetMat("LP_Plastic_Blue", new Color(0.2f, 0.4f, 0.7f));

        // Main body
        var body = CreateBox(new Vector3(0.32f, 0.7f, 0.32f), matBody);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.35f, 0);
        body.gameObject.name = "Body";

        // Water bottle (cylinder on top)
        var bottle = CreateCylinder(0.12f, 0.35f, 10, matTop);
        bottle.transform.SetParent(root.transform);
        bottle.transform.localPosition = new Vector3(0, 0.875f, 0);
        bottle.gameObject.name = "Bottle";

        // Taps (2 small boxes)
        var tapBlue = CreateBox(new Vector3(0.04f, 0.04f, 0.04f), matTap);
        tapBlue.transform.SetParent(root.transform);
        tapBlue.transform.localPosition = new Vector3(-0.06f, 0.45f, 0.18f);
        tapBlue.gameObject.name = "TapCold";

        var tapRed = CreateBox(new Vector3(0.04f, 0.04f, 0.04f), GetMat("LP_Plastic_Red", new Color(0.7f, 0.15f, 0.15f)));
        tapRed.transform.SetParent(root.transform);
        tapRed.transform.localPosition = new Vector3(0.06f, 0.45f, 0.18f);
        tapRed.gameObject.name = "TapHot";

        // Drip tray
        var tray = CreateBox(new Vector3(0.2f, 0.02f, 0.12f), matBody);
        tray.transform.SetParent(root.transform);
        tray.transform.localPosition = new Vector3(0, 0.3f, 0.2f);
        tray.gameObject.name = "DripTray";

        SavePrefab(root, "LowPoly_Dispenser");
        return 1;
    }

    // ========================================
    // STAIR (original: 43K verts) -> ~100 verts
    // Dimensions: h=3.00m w=2.22m
    // ========================================
    static int GenerateStair()
    {
        var root = new GameObject("LowPoly_Stair");
        var matConcrete = GetMat("LP_Concrete", new Color(0.7f, 0.68f, 0.65f));
        var matRailing = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);

        // 10 steps
        float stepH = 0.3f;
        float stepD = 0.3f;
        float stepW = 2.0f;
        for (int i = 0; i < 10; i++)
        {
            var step = CreateBox(new Vector3(stepW, stepH, stepD), matConcrete);
            step.transform.SetParent(root.transform);
            step.transform.localPosition = new Vector3(0, stepH * 0.5f + stepH * i, stepD * i);
            step.gameObject.name = $"Step{i}";
        }

        // Left railing
        var railL = CreateBox(new Vector3(0.04f, 0.04f, 4.0f), matRailing);
        railL.transform.SetParent(root.transform);
        railL.transform.localPosition = new Vector3(-1.0f, 1.5f + 0.45f, 1.35f);
        railL.transform.localRotation = Quaternion.Euler(45, 0, 0);
        railL.gameObject.name = "RailingLeft";

        // Right railing
        var railR = CreateBox(new Vector3(0.04f, 0.04f, 4.0f), matRailing);
        railR.transform.SetParent(root.transform);
        railR.transform.localPosition = new Vector3(1.0f, 1.5f + 0.45f, 1.35f);
        railR.transform.localRotation = Quaternion.Euler(45, 0, 0);
        railR.gameObject.name = "RailingRight";

        SavePrefab(root, "LowPoly_Stair");
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

    // ========================================
    // WARDROBE (original: 35K verts) -> ~50 verts
    // Dimensions: h=2.00m w=1.06m
    // ========================================
    static int GenerateWardrobe()
    {
        var root = new GameObject("LowPoly_Wardrobe");
        var matWood = GetMat("LP_Wood_Wardrobe", new Color(0.42f, 0.28f, 0.18f));
        var matHandle = GetMat("LP_Metal_Handle", new Color(0.6f, 0.58f, 0.55f), 0.7f, 0.5f);

        // Main body
        var body = CreateBox(new Vector3(1.0f, 2.0f, 0.55f), matWood);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1.0f, 0);
        body.gameObject.name = "Body";

        // Door split line
        var split = CreateBox(new Vector3(0.01f, 1.9f, 0.01f), matHandle);
        split.transform.SetParent(root.transform);
        split.transform.localPosition = new Vector3(0, 1.0f, 0.28f);
        split.gameObject.name = "DoorSplit";

        // Handles (2)
        var handleL = CreateBox(new Vector3(0.02f, 0.12f, 0.02f), matHandle);
        handleL.transform.SetParent(root.transform);
        handleL.transform.localPosition = new Vector3(-0.08f, 1.0f, 0.29f);
        handleL.gameObject.name = "HandleLeft";

        var handleR = CreateBox(new Vector3(0.02f, 0.12f, 0.02f), matHandle);
        handleR.transform.SetParent(root.transform);
        handleR.transform.localPosition = new Vector3(0.08f, 1.0f, 0.29f);
        handleR.gameObject.name = "HandleRight";

        // Base molding
        var molding = CreateBox(new Vector3(1.04f, 0.06f, 0.58f), matWood);
        molding.transform.SetParent(root.transform);
        molding.transform.localPosition = new Vector3(0, 0.03f, 0);
        molding.gameObject.name = "BaseMolding";

        // Top crown
        var crown = CreateBox(new Vector3(1.04f, 0.04f, 0.58f), matWood);
        crown.transform.SetParent(root.transform);
        crown.transform.localPosition = new Vector3(0, 2.02f, 0);
        crown.gameObject.name = "Crown";

        SavePrefab(root, "LowPoly_Wardrobe");
        return 1;
    }

    // ========================================
    // SHELF (original: 29K verts) -> ~60 verts
    // Dimensions: h=1.80m w=1.08m
    // ========================================
    static int GenerateShelf()
    {
        var root = new GameObject("LowPoly_Shelf");
        var matWood = GetMat("LP_Wood_Shelf", new Color(0.55f, 0.38f, 0.22f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);

        // 5 shelves
        for (int i = 0; i < 5; i++)
        {
            var shelf = CreateBox(new Vector3(1.0f, 0.03f, 0.35f), matWood);
            shelf.transform.SetParent(root.transform);
            shelf.transform.localPosition = new Vector3(0, 0.02f + i * 0.44f, 0);
            shelf.gameObject.name = $"Shelf{i}";
        }

        // 4 vertical supports
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.47f : 0.47f);
            float z = (i < 2 ? -0.15f : 0.15f);
            var support = CreateBox(new Vector3(0.03f, 1.78f, 0.03f), matMetal);
            support.transform.SetParent(root.transform);
            support.transform.localPosition = new Vector3(x, 0.89f, z);
            support.gameObject.name = $"Support{i}";
        }

        SavePrefab(root, "LowPoly_Shelf");
        return 1;
    }

    // ========================================
    // FRIDGE (original: 6K verts, but keeping) -> ~40 verts
    // Dimensions: h=1.80m w=0.76m
    // ========================================
    static int GenerateFridge()
    {
        var root = new GameObject("LowPoly_Fridge");
        var matBody = GetMat("LP_Appliance_White", new Color(0.9f, 0.9f, 0.88f));
        var matHandle = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);

        // Main body
        var body = CreateBox(new Vector3(0.7f, 1.8f, 0.65f), matBody);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.9f, 0);
        body.gameObject.name = "Body";

        // Freezer door line
        var freezerLine = CreateBox(new Vector3(0.68f, 0.015f, 0.01f), matHandle);
        freezerLine.transform.SetParent(root.transform);
        freezerLine.transform.localPosition = new Vector3(0, 1.3f, 0.33f);
        freezerLine.gameObject.name = "FreezerLine";

        // Handle top (freezer)
        var handleTop = CreateBox(new Vector3(0.02f, 0.15f, 0.03f), matHandle);
        handleTop.transform.SetParent(root.transform);
        handleTop.transform.localPosition = new Vector3(0.3f, 1.55f, 0.35f);
        handleTop.gameObject.name = "HandleTop";

        // Handle bottom (fridge)
        var handleBot = CreateBox(new Vector3(0.02f, 0.2f, 0.03f), matHandle);
        handleBot.transform.SetParent(root.transform);
        handleBot.transform.localPosition = new Vector3(0.3f, 0.9f, 0.35f);
        handleBot.gameObject.name = "HandleBottom";

        SavePrefab(root, "LowPoly_Fridge");
        return 1;
    }

    // ========================================
    // TOILET (original: 128K verts) -> ~60 verts
    // Dimensions: h=0.40m w=0.33m (Toilet1)
    // ========================================
    static int GenerateToilet()
    {
        var root = new GameObject("LowPoly_Toilet");
        var matCeramic = GetMat("LP_Ceramic_White", new Color(0.92f, 0.92f, 0.90f), 0f, 0.7f);
        var matSeat = GetMat("LP_Plastic_White", new Color(0.88f, 0.88f, 0.86f));
        var matChrome = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);

        // Bowl base
        var bowl = CreateCylinder(0.16f, 0.3f, 10, matCeramic);
        bowl.transform.SetParent(root.transform);
        bowl.transform.localPosition = new Vector3(0, 0.15f, 0.05f);
        bowl.gameObject.name = "Bowl";

        // Seat (flattened cylinder)
        var seat = CreateCylinder(0.17f, 0.03f, 10, matSeat);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.31f, 0.05f);
        seat.gameObject.name = "Seat";

        // Tank
        var tank = CreateBox(new Vector3(0.32f, 0.25f, 0.14f), matCeramic);
        tank.transform.SetParent(root.transform);
        tank.transform.localPosition = new Vector3(0, 0.32f, -0.14f);
        tank.gameObject.name = "Tank";

        // Tank lid
        var lid = CreateBox(new Vector3(0.34f, 0.03f, 0.15f), matCeramic);
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.46f, -0.14f);
        lid.gameObject.name = "TankLid";

        // Flush handle
        var flush = CreateBox(new Vector3(0.06f, 0.02f, 0.02f), matChrome);
        flush.transform.SetParent(root.transform);
        flush.transform.localPosition = new Vector3(0.18f, 0.44f, -0.14f);
        flush.gameObject.name = "FlushHandle";

        SavePrefab(root, "LowPoly_Toilet");
        return 1;
    }

    // ========================================
    // KITCHEN COUNTER (original: 305K/162K verts) -> ~80 verts
    // Dimensions: h=0.90m w=2.17m (Kitchen4)
    // ========================================
    static int GenerateKitchenCounter()
    {
        var root = new GameObject("LowPoly_KitchenCounter");
        var matCounter = GetMat("LP_Granite", new Color(0.35f, 0.33f, 0.3f), 0.1f, 0.6f);
        var matCabinet = GetMat("LP_Cabinet_White", new Color(0.88f, 0.86f, 0.82f));
        var matHandle = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        var matSink = GetMat("LP_Metal_Sink", new Color(0.7f, 0.7f, 0.72f), 0.7f, 0.7f);

        // Countertop
        var top = CreateBox(new Vector3(2.1f, 0.05f, 0.65f), matCounter);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.88f, 0);
        top.gameObject.name = "Countertop";

        // Cabinet body
        var cabinet = CreateBox(new Vector3(2.1f, 0.82f, 0.6f), matCabinet);
        cabinet.transform.SetParent(root.transform);
        cabinet.transform.localPosition = new Vector3(0, 0.41f, 0);
        cabinet.gameObject.name = "Cabinet";

        // Cabinet doors (4)
        for (int i = 0; i < 4; i++)
        {
            float x = -0.78f + i * 0.52f;
            var doorLine = CreateBox(new Vector3(0.01f, 0.7f, 0.01f), matHandle);
            doorLine.transform.SetParent(root.transform);
            doorLine.transform.localPosition = new Vector3(x, 0.41f, 0.305f);
            doorLine.gameObject.name = $"DoorLine{i}";

            var handle = CreateBox(new Vector3(0.02f, 0.08f, 0.02f), matHandle);
            handle.transform.SetParent(root.transform);
            handle.transform.localPosition = new Vector3(x + 0.04f, 0.45f, 0.32f);
            handle.gameObject.name = $"Handle{i}";
        }

        // Sink basin (recessed rectangle)
        var sinkBasin = CreateBox(new Vector3(0.4f, 0.04f, 0.35f), matSink);
        sinkBasin.transform.SetParent(root.transform);
        sinkBasin.transform.localPosition = new Vector3(0.5f, 0.87f, 0);
        sinkBasin.gameObject.name = "SinkBasin";

        SavePrefab(root, "LowPoly_KitchenCounter");
        return 1;
    }

    // ========================================
    // HANGER (original: 603K verts!) -> ~40 verts
    // Dimensions: h=1.70m w=0.47m
    // ========================================
    static int GenerateHanger()
    {
        var root = new GameObject("LowPoly_Hanger");
        var matWood = GetMat("LP_Wood_Dark", new Color(0.25f, 0.15f, 0.08f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);

        // Central pole
        var pole = CreateCylinder(0.03f, 1.5f, 8, matWood);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.85f, 0);
        pole.gameObject.name = "Pole";

        // Base (cross)
        var baseX = CreateBox(new Vector3(0.45f, 0.04f, 0.06f), matWood);
        baseX.transform.SetParent(root.transform);
        baseX.transform.localPosition = new Vector3(0, 0.02f, 0);
        baseX.gameObject.name = "BaseX";

        var baseZ = CreateBox(new Vector3(0.06f, 0.04f, 0.45f), matWood);
        baseZ.transform.SetParent(root.transform);
        baseZ.transform.localPosition = new Vector3(0, 0.02f, 0);
        baseZ.gameObject.name = "BaseZ";

        // Top hooks (6 arms radiating out)
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * 0.18f;
            float z = Mathf.Sin(angle) * 0.18f;

            var hook = CreateBox(new Vector3(0.02f, 0.02f, 0.2f), matMetal);
            hook.transform.SetParent(root.transform);
            hook.transform.localPosition = new Vector3(x, 1.6f, z);
            hook.transform.localRotation = Quaternion.Euler(0, -i * 60f, 15f);
            hook.gameObject.name = $"Hook{i}";
        }

        // Top cap
        var cap = CreateCylinder(0.05f, 0.04f, 8, matWood);
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0, 1.62f, 0);
        cap.gameObject.name = "Cap";

        SavePrefab(root, "LowPoly_Hanger");
        return 1;
    }

    // ========================================
    // MIRROR (original: 70K verts) -> ~20 verts
    // Dimensions: h=0.80m w=0.59m
    // ========================================
    static int GenerateMirror()
    {
        var root = new GameObject("LowPoly_Mirror");
        var matFrame = GetMat("LP_Wood_Frame", new Color(0.4f, 0.25f, 0.12f));
        var matMirror = GetMat("LP_Mirror_Silver", new Color(0.85f, 0.88f, 0.9f), 0.8f, 0.95f);

        // Frame
        var frame = CreateBox(new Vector3(0.59f, 0.80f, 0.04f), matFrame);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.5f, 0);
        frame.gameObject.name = "Frame";

        // Mirror surface
        var mirror = CreateBox(new Vector3(0.51f, 0.72f, 0.005f), matMirror);
        mirror.transform.SetParent(root.transform);
        mirror.transform.localPosition = new Vector3(0, 0.5f, 0.023f);
        mirror.gameObject.name = "MirrorSurface";

        SavePrefab(root, "LowPoly_Mirror");
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

    // BED h=0.55 w=1.77
    static int GenerateBed()
    {
        var root = new GameObject("LowPoly_Bed");
        var matFrame = GetMat("LP_Wood_Bed", new Color(0.45f, 0.3f, 0.18f));
        var matMattress = GetMat("LP_Fabric_Mattress", new Color(0.9f, 0.88f, 0.85f));
        var matPillow = GetMat("LP_Fabric_Pillow", new Color(0.95f, 0.93f, 0.9f));
        var matBlanket = GetMat("LP_Fabric_Blue", new Color(0.25f, 0.35f, 0.55f));
        // Frame
        var frame = CreateBox(new Vector3(1.7f, 0.15f, 2.0f), matFrame);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.075f, 0);
        frame.gameObject.name = "Frame";
        // Mattress
        var mattress = CreateBox(new Vector3(1.6f, 0.2f, 1.9f), matMattress);
        mattress.transform.SetParent(root.transform);
        mattress.transform.localPosition = new Vector3(0, 0.25f, 0);
        mattress.gameObject.name = "Mattress";
        // Pillows
        for (int i = 0; i < 2; i++)
        {
            var pillow = CreateBox(new Vector3(0.55f, 0.1f, 0.35f), matPillow);
            pillow.transform.SetParent(root.transform);
            pillow.transform.localPosition = new Vector3(i == 0 ? -0.4f : 0.4f, 0.4f, -0.75f);
            pillow.gameObject.name = $"Pillow{i}";
        }
        // Blanket
        var blanket = CreateBox(new Vector3(1.55f, 0.05f, 1.2f), matBlanket);
        blanket.transform.SetParent(root.transform);
        blanket.transform.localPosition = new Vector3(0, 0.38f, 0.3f);
        blanket.gameObject.name = "Blanket";
        // Headboard
        var headboard = CreateBox(new Vector3(1.7f, 0.45f, 0.06f), matFrame);
        headboard.transform.SetParent(root.transform);
        headboard.transform.localPosition = new Vector3(0, 0.35f, -1.03f);
        headboard.gameObject.name = "Headboard";
        SavePrefab(root, "LowPoly_Bed");
        return 1;
    }

    // BUSH h=1.00 w=1.04
    static int GenerateBush()
    {
        var root = new GameObject("LowPoly_Bush");
        var matLeaf = GetMat("LP_Leaf_Green", new Color(0.2f, 0.45f, 0.15f));
        // 3 overlapping icosahedrons
        float[] sizes = { 0.5f, 0.42f, 0.38f };
        Vector3[] offsets = { Vector3.zero, new Vector3(0.25f, -0.05f, 0.15f), new Vector3(-0.2f, -0.08f, -0.12f) };
        for (int i = 0; i < 3; i++)
        {
            var sphere = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, sizes[i], 1);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = matLeaf;
            sphere.transform.SetParent(root.transform);
            sphere.transform.localPosition = new Vector3(offsets[i].x, 0.5f + offsets[i].y, offsets[i].z);
            sphere.gameObject.name = $"Bush{i}";
        }
        SavePrefab(root, "LowPoly_Bush");
        return 1;
    }

    // CHAIR h=0.90 w=0.47
    static int GenerateChair()
    {
        var root = new GameObject("LowPoly_Chair");
        var matWood = GetMat("LP_Wood_Chair", new Color(0.5f, 0.35f, 0.2f));
        // Seat
        var seat = CreateBox(new Vector3(0.42f, 0.04f, 0.42f), matWood);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.45f, 0);
        seat.gameObject.name = "Seat";
        // Backrest
        var back = CreateBox(new Vector3(0.42f, 0.42f, 0.04f), matWood);
        back.transform.SetParent(root.transform);
        back.transform.localPosition = new Vector3(0, 0.68f, -0.19f);
        back.gameObject.name = "Back";
        // 4 legs
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.17f : 0.17f);
            float z = (i < 2 ? -0.17f : 0.17f);
            var leg = CreateBox(new Vector3(0.03f, 0.45f, 0.03f), matWood);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.225f, z);
            leg.gameObject.name = $"Leg{i}";
        }
        SavePrefab(root, "LowPoly_Chair");
        return 1;
    }

    // CHOPPINGBOARD h=0.10 w=2.92
    static int GenerateChoppingBoard()
    {
        var root = new GameObject("LowPoly_ChoppingBoard");
        var matWood = GetMat("LP_Wood_Cutting", new Color(0.6f, 0.45f, 0.25f));
        var board = CreateBox(new Vector3(0.35f, 0.02f, 0.25f), matWood);
        board.transform.SetParent(root.transform);
        board.transform.localPosition = new Vector3(0, 0.01f, 0);
        board.gameObject.name = "Board";
        // Handle
        var handle = CreateBox(new Vector3(0.08f, 0.02f, 0.06f), matWood);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0.22f, 0.01f, 0);
        handle.gameObject.name = "Handle";
        SavePrefab(root, "LowPoly_ChoppingBoard");
        return 1;
    }

    // CLOCK h=0.30 w=0.30
    static int GenerateClock()
    {
        var root = new GameObject("LowPoly_Clock");
        float R = 0.14f;        // outer radius
        float faceR = 0.125f;   // face radius
        float depth = 0.035f;   // body thickness
        float cy = 0.15f + R;   // center Y so bottom touches y=0.15

        var matBody = GetMat("LP_Plastic_White", new Color(0.88f, 0.88f, 0.86f));
        var matFace = GetMat("LP_Clock_Face", new Color(0.96f, 0.96f, 0.94f));
        // Face needs emission so it's visible on vertical surfaces with overhead lighting
        matFace.EnableKeyword("_EMISSION");
        matFace.SetFloat("_UseEmission", 1f);
        matFace.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 0.88f, 1f));
        matFace.SetFloat("_EmissionIntensity", 2f);
        matFace.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        var matHand = GetMat("LP_Metal_Black", new Color(0.1f, 0.1f, 0.1f), 0.5f, 0.3f);
        var matRim = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.75f), 0.8f, 0.7f);
        var matMarker = GetMat("LP_Clock_Marker", new Color(0.15f, 0.15f, 0.15f));

        // Body (main disc, stands upright facing +Z)
        var body = CreateCylinder(R, depth, 24, matBody);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, cy, 0);
        body.transform.localRotation = Quaternion.Euler(90, 0, 0);
        body.gameObject.name = "Body";

        // Chrome rim ring (slightly larger, thin) - on front face (-Z)
        var rim = CreateCylinder(R + 0.005f, 0.008f, 24, matRim);
        rim.transform.SetParent(root.transform);
        rim.transform.localPosition = new Vector3(0, cy, -(depth * 0.5f));
        rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
        rim.gameObject.name = "Rim";

        // White face disc - on front face (-Z)
        float faceZ = -(depth * 0.5f + 0.002f);
        var face = CreateCylinder(faceR, 0.003f, 24, matFace);
        face.transform.SetParent(root.transform);
        face.transform.localPosition = new Vector3(0, cy, faceZ);
        face.transform.localRotation = Quaternion.Euler(90, 0, 0);
        face.gameObject.name = "Face";

        // Hour markers (12 ticks around the face) - on front
        float markerZ = faceZ - 0.002f;
        for (int h = 0; h < 12; h++)
        {
            float angle = h * 30f * Mathf.Deg2Rad;
            float dist = faceR * 0.82f;
            float mx = Mathf.Sin(angle) * dist;
            float my = Mathf.Cos(angle) * dist;
            // Major ticks at 12/3/6/9, minor for others
            bool major = (h % 3 == 0);
            float tickLen = major ? 0.018f : 0.012f;
            float tickW = major ? 0.008f : 0.005f;
            var tick = CreateBox(new Vector3(tickW, tickLen, 0.003f), matMarker);
            tick.transform.SetParent(root.transform);
            tick.transform.localPosition = new Vector3(mx, cy + my, markerZ);
            tick.transform.localRotation = Quaternion.Euler(0, 0, -h * 30f);
            tick.gameObject.name = $"Tick_{h}";
        }

        // Hour hand (short, thick) - pointing towards ~2 o'clock position
        float handZ = markerZ - 0.003f;
        float hourAngleDeg = 60f; // ~2 o'clock position
        var hourH = CreateBox(new Vector3(0.012f, 0.065f, 0.004f), matHand);
        hourH.transform.SetParent(root.transform);
        float hourDist = 0.032f;
        float hourRad = hourAngleDeg * Mathf.Deg2Rad;
        hourH.transform.localPosition = new Vector3(
            Mathf.Sin(hourRad) * hourDist,
            cy + Mathf.Cos(hourRad) * hourDist,
            handZ);
        hourH.transform.localRotation = Quaternion.Euler(0, 0, -hourAngleDeg);
        hourH.gameObject.name = "HourHand";

        // Minute hand (long, thin) - pointing towards ~10 o'clock position
        float minAngleDeg = -60f; // ~10 o'clock position
        var minH = CreateBox(new Vector3(0.008f, 0.09f, 0.004f), matHand);
        minH.transform.SetParent(root.transform);
        float minDist = 0.045f;
        float minRad = minAngleDeg * Mathf.Deg2Rad;
        minH.transform.localPosition = new Vector3(
            Mathf.Sin(minRad) * minDist,
            cy + Mathf.Cos(minRad) * minDist,
            handZ);
        minH.transform.localRotation = Quaternion.Euler(0, 0, -minAngleDeg);
        minH.gameObject.name = "MinuteHand";

        // Center pin
        var pin = CreateCylinder(0.008f, 0.008f, 8, matHand);
        pin.transform.SetParent(root.transform);
        pin.transform.localPosition = new Vector3(0, cy, handZ - 0.002f);
        pin.transform.localRotation = Quaternion.Euler(90, 0, 0);
        pin.gameObject.name = "CenterPin";

        SavePrefab(root, "LowPoly_Clock");
        return 1;
    }

    // COMPUTER h=0.45 w=0.54 (monitor)
    static int GenerateComputer()
    {
        var root = new GameObject("LowPoly_Computer");
        var matFrame = GetMat("LP_TV_Frame", new Color(0.08f, 0.08f, 0.08f), 0.3f, 0.6f);
        var matScreen = GetMat("LP_TV_Screen", new Color(0.05f, 0.08f, 0.12f), 0f, 0.9f);
        // Monitor
        var frame = CreateBox(new Vector3(0.5f, 0.35f, 0.03f), matFrame);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.3f, 0);
        frame.gameObject.name = "Monitor";
        var screen = CreateBox(new Vector3(0.44f, 0.29f, 0.005f), matScreen);
        screen.transform.SetParent(root.transform);
        screen.transform.localPosition = new Vector3(0, 0.3f, 0.018f);
        screen.gameObject.name = "Screen";
        // Stand
        var neck = CreateBox(new Vector3(0.06f, 0.1f, 0.06f), matFrame);
        neck.transform.SetParent(root.transform);
        neck.transform.localPosition = new Vector3(0, 0.08f, 0);
        neck.gameObject.name = "Neck";
        var base_ = CreateBox(new Vector3(0.2f, 0.02f, 0.15f), matFrame);
        base_.transform.SetParent(root.transform);
        base_.transform.localPosition = new Vector3(0, 0.01f, 0);
        base_.gameObject.name = "Base";
        // Keyboard
        var kb = CreateBox(new Vector3(0.35f, 0.015f, 0.12f), matFrame);
        kb.transform.SetParent(root.transform);
        kb.transform.localPosition = new Vector3(0, 0.008f, 0.25f);
        kb.gameObject.name = "Keyboard";
        SavePrefab(root, "LowPoly_Computer");
        return 1;
    }

    // COMPUTERUSER h=0.45 w=0.20 (desktop PC tower)
    static int GenerateComputerUser()
    {
        var root = new GameObject("LowPoly_ComputerUser");
        var matCase = GetMat("LP_TV_Frame", new Color(0.08f, 0.08f, 0.08f), 0.3f, 0.6f);
        var matFront = GetMat("LP_Plastic_LightGray", new Color(0.25f, 0.25f, 0.25f), 0.1f, 0.4f);
        var matVent = GetMat("LP_Metal_DarkGray", new Color(0.12f, 0.12f, 0.12f), 0.4f, 0.3f);
        var matLed = GetMat("LP_Light_Green", new Color(0.1f, 0.8f, 0.15f));
        // Enable LED emission
        matLed.EnableKeyword("_EMISSION");
        matLed.SetFloat("_UseEmission", 1f);
        matLed.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 0.2f, 1f));
        matLed.SetFloat("_EmissionIntensity", 3f);

        float caseH = 0.42f, caseW = 0.18f, caseD = 0.40f;

        // Main case body
        var body = CreateBox(new Vector3(caseW, caseH, caseD), matCase);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, caseH * 0.5f, 0);
        body.gameObject.name = "Case";

        // Front panel (slightly lighter)
        var front = CreateBox(new Vector3(caseW - 0.01f, caseH - 0.02f, 0.005f), matFront);
        front.transform.SetParent(root.transform);
        front.transform.localPosition = new Vector3(0, caseH * 0.5f, -(caseD * 0.5f + 0.001f));
        front.gameObject.name = "FrontPanel";

        // Power button (small cylinder, top of front)
        var pwrBtn = CreateCylinder(0.008f, 0.005f, 8, matVent);
        pwrBtn.transform.SetParent(root.transform);
        pwrBtn.transform.localPosition = new Vector3(0, caseH - 0.04f, -(caseD * 0.5f + 0.003f));
        pwrBtn.transform.localRotation = Quaternion.Euler(90, 0, 0);
        pwrBtn.gameObject.name = "PowerButton";

        // Power LED
        var led = CreateBox(new Vector3(0.005f, 0.003f, 0.003f), matLed);
        led.transform.SetParent(root.transform);
        led.transform.localPosition = new Vector3(0, caseH - 0.06f, -(caseD * 0.5f + 0.003f));
        led.gameObject.name = "PowerLED";

        // DVD/Optical drive bay
        var dvd = CreateBox(new Vector3(caseW * 0.75f, 0.025f, 0.005f), matVent);
        dvd.transform.SetParent(root.transform);
        dvd.transform.localPosition = new Vector3(0, caseH - 0.10f, -(caseD * 0.5f + 0.002f));
        dvd.gameObject.name = "OpticalDrive";

        // Front ventilation grille (lower)
        var ventFront = CreateBox(new Vector3(caseW * 0.6f, 0.08f, 0.004f), matVent);
        ventFront.transform.SetParent(root.transform);
        ventFront.transform.localPosition = new Vector3(0, 0.08f, -(caseD * 0.5f + 0.002f));
        ventFront.gameObject.name = "FrontVent";

        // Side panel lines (2 subtle indentations)
        for (int i = 0; i < 2; i++)
        {
            float side = (i == 0) ? 1f : -1f;
            var sidePanel = CreateBox(new Vector3(0.003f, caseH - 0.06f, caseD - 0.06f), matVent);
            sidePanel.transform.SetParent(root.transform);
            sidePanel.transform.localPosition = new Vector3(side * (caseW * 0.5f + 0.001f), caseH * 0.5f, 0);
            sidePanel.gameObject.name = $"SidePanel_{i}";
        }

        // Rear exhaust vent
        var ventRear = CreateBox(new Vector3(caseW * 0.5f, 0.06f, 0.004f), matVent);
        ventRear.transform.SetParent(root.transform);
        ventRear.transform.localPosition = new Vector3(0, caseH - 0.06f, caseD * 0.5f + 0.002f);
        ventRear.gameObject.name = "RearVent";

        // Rubber feet (4 small pads)
        for (int i = 0; i < 4; i++)
        {
            float fx = (i % 2 == 0 ? -1f : 1f) * (caseW * 0.35f);
            float fz = (i < 2 ? -1f : 1f) * (caseD * 0.35f);
            var foot = CreateBox(new Vector3(0.02f, 0.008f, 0.02f), matVent);
            foot.transform.SetParent(root.transform);
            foot.transform.localPosition = new Vector3(fx, 0.004f, fz);
            foot.gameObject.name = $"Foot_{i}";
        }

        SavePrefab(root, "LowPoly_ComputerUser");
        return 1;
    }

    // CUP h=0.10 w=0.23
    static int GenerateCup()
    {
        var root = new GameObject("LowPoly_Cup");
        var matCeramic = GetMat("LP_Ceramic_White", new Color(0.92f, 0.92f, 0.90f), 0f, 0.7f);
        var cup = CreateCylinder(0.04f, 0.09f, 8, matCeramic);
        cup.transform.SetParent(root.transform);
        cup.transform.localPosition = new Vector3(0, 0.045f, 0);
        cup.gameObject.name = "Cup";
        // Handle
        var handle = CreateBox(new Vector3(0.01f, 0.05f, 0.03f), matCeramic);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0.05f, 0.045f, 0);
        handle.gameObject.name = "Handle";
        SavePrefab(root, "LowPoly_Cup");
        return 1;
    }

    // DOOR h=2.10 w=1.22
    static int GenerateDoor()
    {
        var root = new GameObject("LowPoly_Door");
        var matWood = GetMat("LP_Wood_Door", new Color(0.42f, 0.28f, 0.16f));
        var matHandle = GetMat("LP_Metal_Handle", new Color(0.6f, 0.58f, 0.55f), 0.7f, 0.5f);
        // Door panel
        var panel = CreateBox(new Vector3(0.9f, 2.0f, 0.05f), matWood);
        panel.transform.SetParent(root.transform);
        panel.transform.localPosition = new Vector3(0, 1.0f, 0);
        panel.gameObject.name = "Panel";
        // Upper panel detail
        var upperP = CreateBox(new Vector3(0.7f, 0.6f, 0.01f), matWood);
        upperP.transform.SetParent(root.transform);
        upperP.transform.localPosition = new Vector3(0, 1.5f, 0.031f);
        upperP.gameObject.name = "UpperPanel";
        // Lower panel detail
        var lowerP = CreateBox(new Vector3(0.7f, 0.6f, 0.01f), matWood);
        lowerP.transform.SetParent(root.transform);
        lowerP.transform.localPosition = new Vector3(0, 0.5f, 0.031f);
        lowerP.gameObject.name = "LowerPanel";
        // Handle
        var hndl = CreateCylinder(0.015f, 0.06f, 6, matHandle);
        hndl.transform.SetParent(root.transform);
        hndl.transform.localPosition = new Vector3(0.35f, 1.0f, 0.04f);
        hndl.transform.localRotation = Quaternion.Euler(90, 0, 0);
        hndl.gameObject.name = "Handle";
        SavePrefab(root, "LowPoly_Door");
        return 1;
    }

    // DOORFRAME h=2.20 w=1.01
    static int GenerateDoorFrame()
    {
        var root = new GameObject("LowPoly_DoorFrame");
        var matWood = GetMat("LP_Wood_Frame", new Color(0.4f, 0.25f, 0.12f));
        // Left jamb
        var left = CreateBox(new Vector3(0.08f, 2.2f, 0.12f), matWood);
        left.transform.SetParent(root.transform);
        left.transform.localPosition = new Vector3(-0.5f, 1.1f, 0);
        left.gameObject.name = "LeftJamb";
        // Right jamb
        var right = CreateBox(new Vector3(0.08f, 2.2f, 0.12f), matWood);
        right.transform.SetParent(root.transform);
        right.transform.localPosition = new Vector3(0.5f, 1.1f, 0);
        right.gameObject.name = "RightJamb";
        // Header
        var header = CreateBox(new Vector3(1.08f, 0.08f, 0.12f), matWood);
        header.transform.SetParent(root.transform);
        header.transform.localPosition = new Vector3(0, 2.16f, 0);
        header.gameObject.name = "Header";
        SavePrefab(root, "LowPoly_DoorFrame");
        return 1;
    }

    // ELEVATOR h=2.40 w=3.10
    static int GenerateElevator()
    {
        var root = new GameObject("LowPoly_Elevator");
        var matMetal = GetMat("LP_Metal_Elevator", new Color(0.6f, 0.6f, 0.62f), 0.6f, 0.5f);
        var matDoor = GetMat("LP_Metal_DoorElev", new Color(0.55f, 0.55f, 0.58f), 0.7f, 0.6f);
        // Back wall
        var backW = CreateBox(new Vector3(2.0f, 2.4f, 0.05f), matMetal);
        backW.transform.SetParent(root.transform);
        backW.transform.localPosition = new Vector3(0, 1.2f, -0.9f);
        backW.gameObject.name = "BackWall";
        // Side walls
        for (int i = 0; i < 2; i++)
        {
            var side = CreateBox(new Vector3(0.05f, 2.4f, 1.8f), matMetal);
            side.transform.SetParent(root.transform);
            side.transform.localPosition = new Vector3(i == 0 ? -1.0f : 1.0f, 1.2f, 0);
            side.gameObject.name = $"SideWall{i}";
        }
        // Doors (2)
        for (int i = 0; i < 2; i++)
        {
            var door = CreateBox(new Vector3(0.48f, 2.2f, 0.03f), matDoor);
            door.transform.SetParent(root.transform);
            door.transform.localPosition = new Vector3(i == 0 ? -0.25f : 0.25f, 1.1f, 0.88f);
            door.gameObject.name = $"Door{i}";
        }
        // Floor
        var floor = CreateBox(new Vector3(2.0f, 0.03f, 1.8f), matMetal);
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = new Vector3(0, 0.015f, 0);
        floor.gameObject.name = "Floor";
        SavePrefab(root, "LowPoly_Elevator");
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

    // GLASS h=0.15 w=0.14
    static int GenerateGlass()
    {
        var root = new GameObject("LowPoly_Glass");
        var matGlass = GetMat("LP_Glass_Clear", new Color(0.85f, 0.9f, 0.92f, 0.4f), 0.1f, 0.9f);
        var glass = CreateCylinder(0.035f, 0.12f, 8, matGlass);
        glass.transform.SetParent(root.transform);
        glass.transform.localPosition = new Vector3(0, 0.06f, 0);
        glass.gameObject.name = "Glass";
        SavePrefab(root, "LowPoly_Glass");
        return 1;
    }

    // GRASS h=0.15 w=0.18
    static int GenerateGrass()
    {
        var root = new GameObject("LowPoly_Grass");
        var matGrass = GetMat("LP_Grass_Blade", new Color(0.30f, 0.65f, 0.18f));
        var matGrassDark = GetMat("LP_Grass_Dark", new Color(0.20f, 0.50f, 0.12f));
        var matGrassTip = GetMat("LP_Grass_Tip", new Color(0.45f, 0.75f, 0.25f));

        // Create grass blade clusters using thin prisms at varying angles
        // Each blade is a thin triangular prism leaning outward
        int bladeIndex = 0;
        float[] heights = { 0.10f, 0.14f, 0.12f, 0.08f, 0.13f, 0.11f, 0.15f, 0.09f, 0.12f, 0.10f, 0.14f, 0.07f };
        float[] radii = { 0.00f, 0.03f, 0.05f, 0.04f, 0.06f, 0.02f, 0.04f, 0.07f, 0.03f, 0.05f, 0.06f, 0.01f };
        float[] angles = { 0f, 30f, 60f, 100f, 140f, 175f, 210f, 250f, 280f, 315f, 345f, 55f };
        float[] tilts = { 0f, 8f, 12f, 5f, 15f, 7f, 10f, 18f, 6f, 14f, 9f, 3f };

        for (int i = 0; i < heights.Length; i++)
        {
            float h = heights[i];
            float r = radii[i];
            float angle = angles[i];
            float tilt = tilts[i];

            // Alternate between main green and dark green for variation
            Material mat = (i % 3 == 0) ? matGrassDark : (i % 3 == 1) ? matGrass : matGrassTip;

            var blade = ShapeGenerator.GeneratePrism(PivotLocation.Center, new Vector3(0.015f, h, 0.008f));
            blade.GetComponent<MeshRenderer>().sharedMaterial = mat;
            blade.transform.SetParent(root.transform);

            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * r;
            float z = Mathf.Sin(rad) * r;
            blade.transform.localPosition = new Vector3(x, h * 0.5f, z);

            // Tilt outward from center and rotate around Y
            blade.transform.localRotation = Quaternion.Euler(-tilt * Mathf.Cos(rad), angle + 90f, -tilt * Mathf.Sin(rad));
            blade.gameObject.name = $"Blade{bladeIndex++}";
        }

        SavePrefab(root, "LowPoly_Grass");
        return 1;
    }

    // HANGER1 h=0.40 w=1.03 (wall-mounted coat rack)
    static int GenerateHanger1()
    {
        var root = new GameObject("LowPoly_Hanger1");
        var matWood = GetMat("LP_Wood_Dark", new Color(0.25f, 0.15f, 0.08f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        // Bar
        var bar = CreateBox(new Vector3(1.0f, 0.06f, 0.06f), matWood);
        bar.transform.SetParent(root.transform);
        bar.transform.localPosition = new Vector3(0, 0.35f, 0);
        bar.gameObject.name = "Bar";
        // Hooks (5)
        for (int i = 0; i < 5; i++)
        {
            var hook = CreateBox(new Vector3(0.02f, 0.08f, 0.03f), matMetal);
            hook.transform.SetParent(root.transform);
            hook.transform.localPosition = new Vector3(-0.4f + i * 0.2f, 0.28f, 0.04f);
            hook.gameObject.name = $"Hook{i}";
        }
        SavePrefab(root, "LowPoly_Hanger1");
        return 1;
    }

    // KETTLE h=0.25 w=0.29
    static int GenerateKettle()
    {
        var root = new GameObject("LowPoly_Kettle");
        var matBody = GetMat("LP_Kettle_Body", new Color(0.72f, 0.72f, 0.75f), 0.8f, 0.7f);
        var matLid = GetMat("LP_Kettle_Lid", new Color(0.65f, 0.65f, 0.68f), 0.75f, 0.65f);
        var matHandle = GetMat("LP_Kettle_Handle", new Color(0.15f, 0.15f, 0.15f), 0.1f, 0.3f);

        // Body - wider at bottom, narrower at top (two stacked cylinders)
        var bodyLower = CreateCylinder(0.11f, 0.10f, 12, matBody);
        bodyLower.transform.SetParent(root.transform);
        bodyLower.transform.localPosition = new Vector3(0, 0.05f, 0);
        bodyLower.gameObject.name = "BodyLower";

        var bodyUpper = CreateCylinder(0.10f, 0.10f, 12, matBody);
        bodyUpper.transform.SetParent(root.transform);
        bodyUpper.transform.localPosition = new Vector3(0, 0.15f, 0);
        bodyUpper.gameObject.name = "BodyUpper";

        // Lid
        var lid = CreateCylinder(0.085f, 0.02f, 12, matLid);
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.21f, 0);
        lid.gameObject.name = "Lid";

        // Lid knob
        var knob = CreateCylinder(0.02f, 0.015f, 8, matHandle);
        knob.transform.SetParent(root.transform);
        knob.transform.localPosition = new Vector3(0, 0.2275f, 0);
        knob.gameObject.name = "LidKnob";

        // Arched handle - multiple small boxes forming an arc over the top
        int handleSegments = 7;
        float handleRadius = 0.07f;
        float handleCenterY = 0.22f;
        for (int i = 0; i < handleSegments; i++)
        {
            float t = (float)i / (handleSegments - 1);
            float angle = Mathf.Lerp(30f, 150f, t) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * handleRadius;
            float y = Mathf.Sin(angle) * handleRadius + handleCenterY;
            float segAngle = Mathf.Lerp(30f, 150f, t);

            var seg = CreateBox(new Vector3(0.015f, 0.025f, 0.015f), matHandle);
            seg.transform.SetParent(root.transform);
            seg.transform.localPosition = new Vector3(0, y, x);
            seg.transform.localRotation = Quaternion.Euler(segAngle - 90f, 0, 0);
            seg.gameObject.name = $"Handle{i}";
        }

        // Spout - angled cylinder-like shape using boxes
        var spoutBase = CreateBox(new Vector3(0.03f, 0.07f, 0.03f), matBody);
        spoutBase.transform.SetParent(root.transform);
        spoutBase.transform.localPosition = new Vector3(0.11f, 0.14f, 0);
        spoutBase.transform.localRotation = Quaternion.Euler(0, 0, -25f);
        spoutBase.gameObject.name = "SpoutBase";

        var spoutTip = CreateBox(new Vector3(0.025f, 0.04f, 0.025f), matBody);
        spoutTip.transform.SetParent(root.transform);
        spoutTip.transform.localPosition = new Vector3(0.14f, 0.19f, 0);
        spoutTip.transform.localRotation = Quaternion.Euler(0, 0, -15f);
        spoutTip.gameObject.name = "SpoutTip";

        SavePrefab(root, "LowPoly_Kettle");
        return 1;
    }

    // KITCHEN2 h=2.20 w=0.91 (tall kitchen cabinet)
    static int GenerateKitchen2()
    {
        var root = new GameObject("LowPoly_Kitchen2");
        var matCabinet = GetMat("LP_Cabinet_White", new Color(0.88f, 0.86f, 0.82f));
        var matHandle = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        var matCounter = GetMat("LP_Granite", new Color(0.35f, 0.33f, 0.3f), 0.1f, 0.6f);
        // Lower cabinet
        var lower = CreateBox(new Vector3(0.85f, 0.85f, 0.6f), matCabinet);
        lower.transform.SetParent(root.transform);
        lower.transform.localPosition = new Vector3(0, 0.425f, 0);
        lower.gameObject.name = "LowerCabinet";
        // Counter
        var counter = CreateBox(new Vector3(0.88f, 0.04f, 0.63f), matCounter);
        counter.transform.SetParent(root.transform);
        counter.transform.localPosition = new Vector3(0, 0.87f, 0);
        counter.gameObject.name = "Counter";
        // Upper cabinet
        var upper = CreateBox(new Vector3(0.85f, 0.7f, 0.35f), matCabinet);
        upper.transform.SetParent(root.transform);
        upper.transform.localPosition = new Vector3(0, 1.8f, -0.12f);
        upper.gameObject.name = "UpperCabinet";
        // Handles
        for (int i = 0; i < 2; i++)
        {
            var h = CreateBox(new Vector3(0.02f, 0.08f, 0.02f), matHandle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(i == 0 ? -0.1f : 0.1f, i == 0 ? 0.5f : 1.6f, i == 0 ? 0.31f : 0.06f);
            h.gameObject.name = $"Handle{i}";
        }
        SavePrefab(root, "LowPoly_Kitchen2");
        return 1;
    }

    // KITCHEN3 h=0.90 w=1.59
    static int GenerateKitchen3()
    {
        var root = new GameObject("LowPoly_Kitchen3");
        var matCabinet = GetMat("LP_Cabinet_White", new Color(0.88f, 0.86f, 0.82f));
        var matCounter = GetMat("LP_Granite", new Color(0.35f, 0.33f, 0.3f), 0.1f, 0.6f);
        var matHandle = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        var top = CreateBox(new Vector3(1.5f, 0.05f, 0.65f), matCounter);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.88f, 0);
        top.gameObject.name = "Countertop";
        var cab = CreateBox(new Vector3(1.5f, 0.82f, 0.6f), matCabinet);
        cab.transform.SetParent(root.transform);
        cab.transform.localPosition = new Vector3(0, 0.41f, 0);
        cab.gameObject.name = "Cabinet";
        for (int i = 0; i < 3; i++)
        {
            var h = CreateBox(new Vector3(0.02f, 0.08f, 0.02f), matHandle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(-0.42f + i * 0.42f, 0.45f, 0.31f);
            h.gameObject.name = $"Handle{i}";
        }
        SavePrefab(root, "LowPoly_Kitchen3");
        return 1;
    }

    // KITCHEN4 h=0.90 w=2.17
    static int GenerateKitchen4()
    {
        var root = new GameObject("LowPoly_Kitchen4");
        var matCabinet = GetMat("LP_Cabinet_White", new Color(0.88f, 0.86f, 0.82f));
        var matCounter = GetMat("LP_Granite", new Color(0.35f, 0.33f, 0.3f), 0.1f, 0.6f);
        var matHandle = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        var matSink = GetMat("LP_Metal_Sink", new Color(0.7f, 0.7f, 0.72f), 0.7f, 0.7f);
        var top = CreateBox(new Vector3(2.1f, 0.05f, 0.65f), matCounter);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.88f, 0);
        top.gameObject.name = "Countertop";
        var cab = CreateBox(new Vector3(2.1f, 0.82f, 0.6f), matCabinet);
        cab.transform.SetParent(root.transform);
        cab.transform.localPosition = new Vector3(0, 0.41f, 0);
        cab.gameObject.name = "Cabinet";
        for (int i = 0; i < 4; i++)
        {
            var h = CreateBox(new Vector3(0.02f, 0.08f, 0.02f), matHandle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(-0.65f + i * 0.43f, 0.45f, 0.31f);
            h.gameObject.name = $"Handle{i}";
        }
        var sink = CreateBox(new Vector3(0.4f, 0.04f, 0.35f), matSink);
        sink.transform.SetParent(root.transform);
        sink.transform.localPosition = new Vector3(0.55f, 0.87f, 0);
        sink.gameObject.name = "SinkBasin";
        SavePrefab(root, "LowPoly_Kitchen4");
        return 1;
    }

    // LAMP0 h=0.40 w=0.21 (table lamp)
    static int GenerateLamp0()
    {
        var root = new GameObject("LowPoly_Lamp0");
        var matBase = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var matShade = GetMat("LP_Fabric_LampShade", new Color(0.9f, 0.85f, 0.75f));
        var basePart = CreateCylinder(0.06f, 0.02f, 8, matBase);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.01f, 0);
        basePart.gameObject.name = "Base";
        var pole = CreateCylinder(0.015f, 0.25f, 6, matBase);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.14f, 0);
        pole.gameObject.name = "Pole";
        var shade = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.1f, 0.12f, 8);
        shade.GetComponent<MeshRenderer>().sharedMaterial = matShade;
        shade.transform.SetParent(root.transform);
        shade.transform.localPosition = new Vector3(0, 0.33f, 0);
        shade.gameObject.name = "Shade";
        SavePrefab(root, "LowPoly_Lamp0");
        return 1;
    }

    // LAMP1 h=1.50 w=0.42 (floor lamp)
    static int GenerateLamp1()
    {
        var root = new GameObject("LowPoly_Lamp1");
        var matBase = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var matShade = GetMat("LP_Fabric_LampShade", new Color(0.9f, 0.85f, 0.75f));
        var basePart = CreateCylinder(0.12f, 0.03f, 10, matBase);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.015f, 0);
        basePart.gameObject.name = "Base";
        var pole = CreateCylinder(0.015f, 1.2f, 6, matBase);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.63f, 0);
        pole.gameObject.name = "Pole";
        var shade = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.18f, 0.22f, 8);
        shade.GetComponent<MeshRenderer>().sharedMaterial = matShade;
        shade.transform.SetParent(root.transform);
        shade.transform.localPosition = new Vector3(0, 1.35f, 0);
        shade.gameObject.name = "Shade";
        SavePrefab(root, "LowPoly_Lamp1");
        return 1;
    }

    // LAMP2 h=1.50 w=0.32 (floor lamp slim)
    static int GenerateLamp2()
    {
        var root = new GameObject("LowPoly_Lamp2");
        var matBase = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var matShade = GetMat("LP_Plastic_LightGray", new Color(0.8f, 0.8f, 0.82f));
        var basePart = CreateCylinder(0.1f, 0.03f, 8, matBase);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.015f, 0);
        basePart.gameObject.name = "Base";
        var pole = CreateCylinder(0.012f, 1.2f, 6, matBase);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.63f, 0);
        pole.gameObject.name = "Pole";
        var shade = CreateCylinder(0.14f, 0.25f, 8, matShade);
        shade.transform.SetParent(root.transform);
        shade.transform.localPosition = new Vector3(0, 1.35f, 0);
        shade.gameObject.name = "Shade";
        SavePrefab(root, "LowPoly_Lamp2");
        return 1;
    }

    // LAMP3 h=0.60 w=0.37 (desk lamp)
    static int GenerateLamp3()
    {
        var root = new GameObject("LowPoly_Lamp3");
        var matBase = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var matShade = GetMat("LP_Metal_Kettle", new Color(0.7f, 0.7f, 0.72f), 0.8f, 0.7f);
        var basePart = CreateCylinder(0.08f, 0.02f, 8, matBase);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.01f, 0);
        basePart.gameObject.name = "Base";
        // Arm
        var arm = CreateBox(new Vector3(0.02f, 0.4f, 0.02f), matBase);
        arm.transform.SetParent(root.transform);
        arm.transform.localPosition = new Vector3(0, 0.22f, 0);
        arm.transform.localRotation = Quaternion.Euler(0, 0, 15);
        arm.gameObject.name = "Arm";
        // Head
        var head = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.08f, 0.1f, 8);
        head.GetComponent<MeshRenderer>().sharedMaterial = matShade;
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0.06f, 0.48f, 0);
        head.transform.localRotation = Quaternion.Euler(0, 0, 30);
        head.gameObject.name = "Head";
        SavePrefab(root, "LowPoly_Lamp3");
        return 1;
    }

    // LAMP4 h=1.50 w=0.31 (floor lamp modern)
    static int GenerateLamp4()
    {
        var root = new GameObject("LowPoly_Lamp4");
        var matBase = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var matShade = GetMat("LP_Fabric_LampShade", new Color(0.9f, 0.85f, 0.75f));
        var basePart = CreateCylinder(0.1f, 0.025f, 8, matBase);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.013f, 0);
        basePart.gameObject.name = "Base";
        var pole = CreateCylinder(0.012f, 1.25f, 6, matBase);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.65f, 0);
        pole.gameObject.name = "Pole";
        var shade = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.13f, 0.2f, 8);
        shade.GetComponent<MeshRenderer>().sharedMaterial = matShade;
        shade.transform.SetParent(root.transform);
        shade.transform.localPosition = new Vector3(0, 1.38f, 0);
        shade.gameObject.name = "Shade";
        SavePrefab(root, "LowPoly_Lamp4");
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
        float endAngle = 10f;
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

        // Light housing - angled box
        var housing = CreateBox(new Vector3(0.22f, 0.06f, 0.38f), matHousing);
        housing.transform.SetParent(root.transform);
        housing.transform.localPosition = new Vector3(0, 3.62f, 0.54f);
        housing.transform.localRotation = Quaternion.Euler(5f, 0, 0);
        housing.gameObject.name = "Housing";

        // Light lens (bottom of housing)
        var lens = CreateBox(new Vector3(0.18f, 0.02f, 0.32f), matLens);
        lens.transform.SetParent(root.transform);
        lens.transform.localPosition = new Vector3(0, 3.58f, 0.54f);
        lens.gameObject.name = "Lens";

        // Small collar where pole meets arm
        var collar = CreateCylinder(0.06f, 0.06f, 8, matBase);
        collar.transform.SetParent(root.transform);
        collar.transform.localPosition = new Vector3(0, 4.14f, 0);
        collar.gameObject.name = "Collar";

        SavePrefab(root, "LowPoly_Lamppost");
        return 1;
    }

    // LARGETABLE h=0.75 w=1.51
    static int GenerateLargeTable()
    {
        var root = new GameObject("LowPoly_LargeTable");
        var matWood = GetMat("LP_Wood_Table", new Color(0.52f, 0.38f, 0.22f));
        // Top
        var top = CreateBox(new Vector3(1.4f, 0.05f, 0.8f), matWood);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.73f, 0);
        top.gameObject.name = "Top";
        // 4 legs
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.6f : 0.6f);
            float z = (i < 2 ? -0.32f : 0.32f);
            var leg = CreateBox(new Vector3(0.05f, 0.7f, 0.05f), matWood);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.35f, z);
            leg.gameObject.name = $"Leg{i}";
        }
        SavePrefab(root, "LowPoly_LargeTable");
        return 1;
    }

    // LOCKER h=1.80 w=2.90
    static int GenerateLocker()
    {
        var root = new GameObject("LowPoly_Locker");
        var matMetal = GetMat("LP_Metal_Locker", new Color(0.5f, 0.52f, 0.55f), 0.4f, 0.3f);
        var matHandle = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        // 4 locker units
        for (int i = 0; i < 4; i++)
        {
            float x = -1.08f + i * 0.72f;
            var unit = CreateBox(new Vector3(0.68f, 1.8f, 0.5f), matMetal);
            unit.transform.SetParent(root.transform);
            unit.transform.localPosition = new Vector3(x, 0.9f, 0);
            unit.gameObject.name = $"Unit{i}";
            var hndl = CreateBox(new Vector3(0.02f, 0.1f, 0.02f), matHandle);
            hndl.transform.SetParent(root.transform);
            hndl.transform.localPosition = new Vector3(x + 0.28f, 0.9f, 0.26f);
            hndl.gameObject.name = $"Handle{i}";
        }
        SavePrefab(root, "LowPoly_Locker");
        return 1;
    }

    // MIRROR1 h=0.80 w=0.51 (round mirror)
    static int GenerateMirror1()
    {
        var root = new GameObject("LowPoly_Mirror1");
        var matFrame = GetMat("LP_Wood_Frame", new Color(0.4f, 0.25f, 0.12f));
        var matMirror = GetMat("LP_Mirror_Silver", new Color(0.85f, 0.88f, 0.9f), 0.8f, 0.95f);
        var frame = CreateCylinder(0.25f, 0.04f, 12, matFrame);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.4f, 0);
        frame.transform.localRotation = Quaternion.Euler(90, 0, 0);
        frame.gameObject.name = "Frame";
        var mirror = CreateCylinder(0.22f, 0.005f, 12, matMirror);
        mirror.transform.SetParent(root.transform);
        mirror.transform.localPosition = new Vector3(0, 0.4f, 0.023f);
        mirror.transform.localRotation = Quaternion.Euler(90, 0, 0);
        mirror.gameObject.name = "MirrorSurface";
        SavePrefab(root, "LowPoly_Mirror1");
        return 1;
    }

    // MIRROR2 h=0.80 w=0.59
    static int GenerateMirror2()
    {
        var root = new GameObject("LowPoly_Mirror2");
        var matFrame = GetMat("LP_Metal_Handle", new Color(0.6f, 0.58f, 0.55f), 0.7f, 0.5f);
        var matMirror = GetMat("LP_Mirror_Silver", new Color(0.85f, 0.88f, 0.9f), 0.8f, 0.95f);
        var frame = CreateBox(new Vector3(0.59f, 0.80f, 0.04f), matFrame);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.5f, 0);
        frame.gameObject.name = "Frame";
        var mirror = CreateBox(new Vector3(0.51f, 0.72f, 0.005f), matMirror);
        mirror.transform.SetParent(root.transform);
        mirror.transform.localPosition = new Vector3(0, 0.5f, 0.023f);
        mirror.gameObject.name = "MirrorSurface";
        SavePrefab(root, "LowPoly_Mirror2");
        return 1;
    }

    // OFFICECHAIR h=1.20 w=0.85
    static int GenerateOfficeChair()
    {
        var root = new GameObject("LowPoly_OfficeChair");
        var matSeat = GetMat("LP_Fabric_OfficeChair", new Color(0.15f, 0.15f, 0.2f));
        var matMetal = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        var matPlastic = GetMat("LP_Plastic_Black", new Color(0.08f, 0.08f, 0.08f), 0.2f, 0.4f);

        // === SEAT (cushioned look: thicker with slight bevel) ===
        var seat = CreateBox(new Vector3(0.48f, 0.08f, 0.45f), matSeat);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.48f, 0.02f);
        seat.gameObject.name = "Seat";
        // Seat bottom plate
        var seatPlate = CreateBox(new Vector3(0.46f, 0.02f, 0.43f), matPlastic);
        seatPlate.transform.SetParent(root.transform);
        seatPlate.transform.localPosition = new Vector3(0, 0.43f, 0.02f);
        seatPlate.gameObject.name = "SeatPlate";

        // === BACKREST (taller, with slight lumbar curve via two segments) ===
        // Lower back (slightly reclined)
        var backLower = CreateBox(new Vector3(0.46f, 0.25f, 0.04f), matSeat);
        backLower.transform.SetParent(root.transform);
        backLower.transform.localPosition = new Vector3(0, 0.68f, -0.20f);
        backLower.transform.localRotation = Quaternion.Euler(-8, 0, 0);
        backLower.gameObject.name = "BackLower";
        // Upper back (more reclined)
        var backUpper = CreateBox(new Vector3(0.44f, 0.28f, 0.035f), matSeat);
        backUpper.transform.SetParent(root.transform);
        backUpper.transform.localPosition = new Vector3(0, 0.94f, -0.23f);
        backUpper.transform.localRotation = Quaternion.Euler(-12, 0, 0);
        backUpper.gameObject.name = "BackUpper";
        // Back shell (plastic backing)
        var backShell = CreateBox(new Vector3(0.44f, 0.50f, 0.015f), matPlastic);
        backShell.transform.SetParent(root.transform);
        backShell.transform.localPosition = new Vector3(0, 0.80f, -0.24f);
        backShell.transform.localRotation = Quaternion.Euler(-10, 0, 0);
        backShell.gameObject.name = "BackShell";

        // === ARMRESTS (with arm pads on top) ===
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            // Vertical support
            var armPost = CreateBox(new Vector3(0.03f, 0.18f, 0.03f), matPlastic);
            armPost.transform.SetParent(root.transform);
            armPost.transform.localPosition = new Vector3(side * 0.24f, 0.56f, -0.08f);
            armPost.gameObject.name = $"ArmPost{i}";
            // Arm pad (soft top)
            var armPad = CreateBox(new Vector3(0.06f, 0.025f, 0.22f), matPlastic);
            armPad.transform.SetParent(root.transform);
            armPad.transform.localPosition = new Vector3(side * 0.24f, 0.66f, -0.04f);
            armPad.gameObject.name = $"ArmPad{i}";
        }

        // === GAS LIFT CYLINDER ===
        var liftOuter = CreateCylinder(0.035f, 0.12f, 8, matPlastic);
        liftOuter.transform.SetParent(root.transform);
        liftOuter.transform.localPosition = new Vector3(0, 0.37f, 0);
        liftOuter.gameObject.name = "LiftOuter";
        // Inner chrome pole (visible between hub and lift)
        var pole = CreateCylinder(0.018f, 0.22f, 6, matMetal);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.22f, 0);
        pole.gameObject.name = "Pole";

        // === 5-STAR BASE WITH CASTERS ===
        // Central hub (wide, squat cylinder)
        var hub = CreateCylinder(0.055f, 0.045f, 10, matPlastic);
        hub.transform.SetParent(root.transform);
        hub.transform.localPosition = new Vector3(0, 0.065f, 0);
        hub.gameObject.name = "Hub";
        // Hub top cap (connects to pole)
        var hubCap = CreateCylinder(0.03f, 0.02f, 8, matMetal);
        hubCap.transform.SetParent(root.transform);
        hubCap.transform.localPosition = new Vector3(0, 0.095f, 0);
        hubCap.gameObject.name = "HubCap";

        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72 * Mathf.Deg2Rad;
            float legLen = 0.30f;
            float dx = Mathf.Sin(angle);
            float dz = Mathf.Cos(angle);
            float yRot = i * 72f;

            // Main leg arm (wider, shaped like a real chair leg)
            var leg = CreateBox(new Vector3(0.038f, 0.028f, legLen), matPlastic);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(
                dx * legLen * 0.5f, 0.050f, dz * legLen * 0.5f);
            leg.transform.localRotation = Quaternion.Euler(0, yRot, 0);
            leg.gameObject.name = $"BaseLeg{i}";

            // Leg tip (slight widening at the end where caster attaches)
            var legTip = CreateBox(new Vector3(0.032f, 0.038f, 0.045f), matPlastic);
            legTip.transform.SetParent(root.transform);
            legTip.transform.localPosition = new Vector3(
                dx * (legLen - 0.01f), 0.040f, dz * (legLen - 0.01f));
            legTip.transform.localRotation = Quaternion.Euler(0, yRot, 0);
            legTip.gameObject.name = $"LegTip{i}";

            // Caster wheel (lying on its side, visible under the leg tip)
            var caster = CreateCylinder(0.018f, 0.022f, 8, matPlastic);
            caster.transform.SetParent(root.transform);
            caster.transform.localPosition = new Vector3(
                dx * legLen, 0.018f, dz * legLen);
            // Rotate to lie on side, perpendicular to leg direction
            caster.transform.localRotation = Quaternion.Euler(0, yRot, 90);
            caster.gameObject.name = $"Caster{i}";
        }

        SavePrefab(root, "LowPoly_OfficeChair");
        return 1;
    }

    // OFFICECUBICLE h=1.50 w=1.34
    static int GenerateOfficeCubicle()
    {
        var root = new GameObject("LowPoly_OfficeCubicle");
        var matPanel = GetMat("LP_Fabric_Cubicle", new Color(0.55f, 0.55f, 0.58f));
        var matFrame = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        // Back panel
        var backP = CreateBox(new Vector3(1.3f, 1.5f, 0.04f), matPanel);
        backP.transform.SetParent(root.transform);
        backP.transform.localPosition = new Vector3(0, 0.75f, -0.5f);
        backP.gameObject.name = "BackPanel";
        // Side panel
        var sideP = CreateBox(new Vector3(0.04f, 1.5f, 1.0f), matPanel);
        sideP.transform.SetParent(root.transform);
        sideP.transform.localPosition = new Vector3(-0.65f, 0.75f, 0);
        sideP.gameObject.name = "SidePanel";
        // Desk
        var desk = CreateBox(new Vector3(1.2f, 0.04f, 0.6f), GetMat("LP_Wood_Desk", new Color(0.5f, 0.38f, 0.24f)));
        desk.transform.SetParent(root.transform);
        desk.transform.localPosition = new Vector3(0, 0.73f, -0.15f);
        desk.gameObject.name = "Desk";
        SavePrefab(root, "LowPoly_OfficeCubicle");
        return 1;
    }

    // OFFICEMEETINGTABLE h=0.75 w=4.00
    static int GenerateOfficeMeetingTable()
    {
        var root = new GameObject("LowPoly_OfficeMeetingTable");
        var matWood = GetMat("LP_Wood_Table", new Color(0.52f, 0.38f, 0.22f));
        var matMetal = GetMat("LP_Chrome", new Color(0.75f, 0.75f, 0.78f), 0.85f, 0.8f);
        var top = CreateBox(new Vector3(3.8f, 0.05f, 1.4f), matWood);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.73f, 0);
        top.gameObject.name = "Top";
        // 4 legs
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1.7f : 1.7f);
            float z = (i < 2 ? -0.55f : 0.55f);
            var leg = CreateBox(new Vector3(0.06f, 0.7f, 0.06f), matMetal);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.35f, z);
            leg.gameObject.name = $"Leg{i}";
        }
        SavePrefab(root, "LowPoly_OfficeMeetingTable");
        return 1;
    }

    // OFFICETABLE h=0.75 w=1.17
    static int GenerateOfficeTable()
    {
        var root = new GameObject("LowPoly_OfficeTable");
        var matWood = GetMat("LP_Wood_Desk", new Color(0.5f, 0.38f, 0.24f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var top = CreateBox(new Vector3(1.1f, 0.04f, 0.6f), matWood);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.73f, 0);
        top.gameObject.name = "Top";
        // Legs
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.48f : 0.48f);
            float z = (i < 2 ? -0.25f : 0.25f);
            var leg = CreateBox(new Vector3(0.04f, 0.7f, 0.04f), matMetal);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.35f, z);
            leg.gameObject.name = $"Leg{i}";
        }
        // Back panel (modesty panel)
        var panel = CreateBox(new Vector3(1.0f, 0.4f, 0.02f), matWood);
        panel.transform.SetParent(root.transform);
        panel.transform.localPosition = new Vector3(0, 0.48f, -0.28f);
        panel.gameObject.name = "ModestyPanel";
        SavePrefab(root, "LowPoly_OfficeTable");
        return 1;
    }

    // OFFICEWHITEBOARD h=1.20 w=2.07
    static int GenerateOfficeWhiteboard()
    {
        var root = new GameObject("LowPoly_OfficeWhiteboard");
        var matFrame = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var matBoard = GetMat("LP_Whiteboard", new Color(0.96f, 0.96f, 0.95f), 0f, 0.7f);
        var frame = CreateBox(new Vector3(2.0f, 1.2f, 0.04f), matFrame);
        frame.transform.SetParent(root.transform);
        frame.transform.localPosition = new Vector3(0, 0.7f, 0);
        frame.gameObject.name = "Frame";
        var board = CreateBox(new Vector3(1.9f, 1.1f, 0.005f), matBoard);
        board.transform.SetParent(root.transform);
        board.transform.localPosition = new Vector3(0, 0.7f, 0.023f);
        board.gameObject.name = "Board";
        // Tray
        var tray = CreateBox(new Vector3(1.0f, 0.03f, 0.06f), matFrame);
        tray.transform.SetParent(root.transform);
        tray.transform.localPosition = new Vector3(0, 0.12f, 0.04f);
        tray.gameObject.name = "Tray";
        SavePrefab(root, "LowPoly_OfficeWhiteboard");
        return 1;
    }

    // PAN0 h=0.08 w=0.37
    static int GeneratePan0()
    {
        var root = new GameObject("LowPoly_Pan0");
        var matMetal = GetMat("LP_Metal_Pan", new Color(0.25f, 0.25f, 0.27f), 0.6f, 0.5f);
        var pan = CreateCylinder(0.13f, 0.05f, 10, matMetal);
        pan.transform.SetParent(root.transform);
        pan.transform.localPosition = new Vector3(0, 0.025f, 0);
        pan.gameObject.name = "Pan";
        var handle = CreateBox(new Vector3(0.03f, 0.02f, 0.15f), matMetal);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0, 0.03f, 0.2f);
        handle.gameObject.name = "Handle";
        SavePrefab(root, "LowPoly_Pan0");
        return 1;
    }

    // PAN1 h=0.10 w=0.40
    static int GeneratePan1()
    {
        var root = new GameObject("LowPoly_Pan1");
        var matMetal = GetMat("LP_Metal_Pan", new Color(0.25f, 0.25f, 0.27f), 0.6f, 0.5f);
        var pot = CreateCylinder(0.13f, 0.1f, 10, matMetal);
        pot.transform.SetParent(root.transform);
        pot.transform.localPosition = new Vector3(0, 0.05f, 0);
        pot.gameObject.name = "Pot";
        // 2 side handles
        for (int i = 0; i < 2; i++)
        {
            var h = CreateBox(new Vector3(0.06f, 0.02f, 0.02f), matMetal);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(i == 0 ? -0.16f : 0.16f, 0.08f, 0);
            h.gameObject.name = $"Handle{i}";
        }
        // Lid
        var lid = CreateCylinder(0.12f, 0.015f, 10, matMetal);
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.11f, 0);
        lid.gameObject.name = "Lid";
        SavePrefab(root, "LowPoly_Pan1");
        return 1;
    }

    // RESTAURANTCHAIR h=0.90 w=0.64
    static int GenerateRestaurantChair()
    {
        var root = new GameObject("LowPoly_RestaurantChair");
        var matSeat = GetMat("LP_Fabric_RestChair", new Color(0.55f, 0.12f, 0.12f));
        var matFrame = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var seat = CreateBox(new Vector3(0.44f, 0.05f, 0.42f), matSeat);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.45f, 0);
        seat.gameObject.name = "Seat";
        var back = CreateBox(new Vector3(0.44f, 0.4f, 0.04f), matSeat);
        back.transform.SetParent(root.transform);
        back.transform.localPosition = new Vector3(0, 0.68f, -0.19f);
        back.gameObject.name = "Back";
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.18f : 0.18f);
            float z = (i < 2 ? -0.17f : 0.17f);
            var leg = CreateCylinder(0.015f, 0.45f, 6, matFrame);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.225f, z);
            leg.gameObject.name = $"Leg{i}";
        }
        SavePrefab(root, "LowPoly_RestaurantChair");
        return 1;
    }

    // RESTAURANTTABLE h=0.75 w=0.98
    static int GenerateRestaurantTable()
    {
        var root = new GameObject("LowPoly_RestaurantTable");
        var matWood = GetMat("LP_Wood_Table", new Color(0.52f, 0.38f, 0.22f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var top = CreateCylinder(0.45f, 0.04f, 10, matWood);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.73f, 0);
        top.gameObject.name = "Top";
        var pole = CreateCylinder(0.04f, 0.55f, 6, matMetal);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.42f, 0);
        pole.gameObject.name = "Pole";
        var basePart = CreateCylinder(0.2f, 0.04f, 8, matMetal);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.02f, 0);
        basePart.gameObject.name = "Base";
        SavePrefab(root, "LowPoly_RestaurantTable");
        return 1;
    }

    // ROOFTOPAC h=0.80 w=0.74
    static int GenerateRooftopAc()
    {
        var root = new GameObject("LowPoly_RooftopAc");
        var matMetal = GetMat("LP_Metal_AC", new Color(0.6f, 0.62f, 0.65f), 0.4f, 0.3f);
        var matFan = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var body = CreateBox(new Vector3(0.7f, 0.6f, 0.7f), matMetal);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.3f, 0);
        body.gameObject.name = "Body";
        // Fan grill on top
        var grill = CreateCylinder(0.25f, 0.02f, 10, matFan);
        grill.transform.SetParent(root.transform);
        grill.transform.localPosition = new Vector3(0, 0.61f, 0);
        grill.gameObject.name = "Grill";
        // Feet
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.28f : 0.28f);
            float z = (i < 2 ? -0.28f : 0.28f);
            var foot = CreateBox(new Vector3(0.06f, 0.06f, 0.06f), matFan);
            foot.transform.SetParent(root.transform);
            foot.transform.localPosition = new Vector3(x, -0.03f, z);
            foot.gameObject.name = $"Foot{i}";
        }
        SavePrefab(root, "LowPoly_RooftopAc");
        return 1;
    }

    // ROOFTOPSOLAR h=0.40 w=0.66
    static int GenerateRooftopSolar()
    {
        var root = new GameObject("LowPoly_RooftopSolar");
        var matPanel = GetMat("LP_Solar_Panel", new Color(0.12f, 0.15f, 0.25f), 0.3f, 0.6f);
        var matFrame = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        var panel = CreateBox(new Vector3(0.6f, 0.03f, 0.95f), matPanel);
        panel.transform.SetParent(root.transform);
        panel.transform.localPosition = new Vector3(0, 0.25f, 0);
        panel.transform.localRotation = Quaternion.Euler(25, 0, 0);
        panel.gameObject.name = "Panel";
        // Support legs
        for (int i = 0; i < 2; i++)
        {
            var leg = CreateBox(new Vector3(0.03f, 0.2f, 0.03f), matFrame);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(i == 0 ? -0.22f : 0.22f, 0.1f, -0.2f);
            leg.gameObject.name = $"Leg{i}";
        }
        SavePrefab(root, "LowPoly_RooftopSolar");
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

    // SHELF1..5 variants
    static int GenerateShelf1()
    {
        return GenerateShelfVariant("LowPoly_Shelf1", 5.5f, 1.8f, 5);
    }
    static int GenerateShelf2()
    {
        return GenerateShelfVariant("LowPoly_Shelf2", 1.0f, 1.8f, 5);
    }
    static int GenerateShelf3()
    {
        return GenerateShelfVariant("LowPoly_Shelf3", 1.0f, 1.8f, 5);
    }
    static int GenerateShelf4()
    {
        return GenerateShelfVariant("LowPoly_Shelf4", 3.4f, 1.8f, 5);
    }
    static int GenerateShelf5()
    {
        return GenerateShelfVariant("LowPoly_Shelf5", 1.5f, 1.8f, 5);
    }

    static int GenerateShelfVariant(string name, float width, float height, int shelves)
    {
        var root = new GameObject(name);
        var matWood = GetMat("LP_Wood_Shelf", new Color(0.55f, 0.38f, 0.22f));
        var matMetal = GetMat("LP_Metal_DarkGray", new Color(0.3f, 0.3f, 0.3f), 0.7f, 0.6f);
        float spacing = height / (shelves - 1);
        for (int i = 0; i < shelves; i++)
        {
            var shelf = CreateBox(new Vector3(width - 0.1f, 0.03f, 0.35f), matWood);
            shelf.transform.SetParent(root.transform);
            shelf.transform.localPosition = new Vector3(0, 0.02f + i * spacing, 0);
            shelf.gameObject.name = $"Shelf{i}";
        }
        int posts = Mathf.Max(2, Mathf.CeilToInt(width / 1.2f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float x = -(width - 0.1f) / 2f + i * ((width - 0.1f) / (posts - 1));
            var post = CreateBox(new Vector3(0.03f, height, 0.03f), matMetal);
            post.transform.SetParent(root.transform);
            post.transform.localPosition = new Vector3(x, height / 2f, 0.15f);
            post.gameObject.name = $"Post{i}";
            var post2 = CreateBox(new Vector3(0.03f, height, 0.03f), matMetal);
            post2.transform.SetParent(root.transform);
            post2.transform.localPosition = new Vector3(x, height / 2f, -0.15f);
            post2.gameObject.name = $"PostB{i}";
        }
        SavePrefab(root, name);
        return 1;
    }

    // SMALLTABLE h=0.50 w=0.58
    static int GenerateSmallTable()
    {
        var root = new GameObject("LowPoly_SmallTable");
        var matWood = GetMat("LP_Wood_Table", new Color(0.52f, 0.38f, 0.22f));
        var top = CreateCylinder(0.27f, 0.035f, 8, matWood);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.48f, 0);
        top.gameObject.name = "Top";
        var pole = CreateCylinder(0.03f, 0.35f, 6, matWood);
        pole.transform.SetParent(root.transform);
        pole.transform.localPosition = new Vector3(0, 0.28f, 0);
        pole.gameObject.name = "Pole";
        var basePart = CreateCylinder(0.15f, 0.04f, 8, matWood);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.02f, 0);
        basePart.gameObject.name = "Base";
        SavePrefab(root, "LowPoly_SmallTable");
        return 1;
    }

    // SOFA1 h=0.85 w=1.40
    static int GenerateSofa1()
    {
        var root = new GameObject("LowPoly_Sofa1");
        var matFabric = GetMat("LP_Fabric_Brown", new Color(0.45f, 0.32f, 0.2f));
        var matLegs = GetMat("LP_Wood_Dark", new Color(0.25f, 0.15f, 0.08f));
        var seat = CreateBox(new Vector3(1.3f, 0.2f, 0.55f), matFabric);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.25f, 0);
        seat.gameObject.name = "Seat";
        var back = CreateBox(new Vector3(1.3f, 0.45f, 0.1f), matFabric);
        back.transform.SetParent(root.transform);
        back.transform.localPosition = new Vector3(0, 0.55f, -0.22f);
        back.gameObject.name = "Back";
        for (int i = 0; i < 2; i++)
        {
            var arm = CreateBox(new Vector3(0.1f, 0.25f, 0.55f), matFabric);
            arm.transform.SetParent(root.transform);
            arm.transform.localPosition = new Vector3(i == 0 ? -0.6f : 0.6f, 0.38f, 0);
            arm.gameObject.name = $"Arm{i}";
        }
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -0.55f : 0.55f);
            float z = (i < 2 ? -0.2f : 0.2f);
            var leg = CreateBox(new Vector3(0.05f, 0.15f, 0.05f), matLegs);
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(x, 0.075f, z);
            leg.gameObject.name = $"Leg{i}";
        }
        SavePrefab(root, "LowPoly_Sofa1");
        return 1;
    }

    // STORESHELF h=2.00 w=5.18
    static int GenerateStoreShelf()
    {
        return GenerateShelfVariant("LowPoly_StoreShelf", 5.0f, 2.0f, 5);
    }

    // TOASTER h=0.20 w=0.53
    static int GenerateToaster()
    {
        var root = new GameObject("LowPoly_Toaster");
        var matMetal = GetMat("LP_Metal_Kettle", new Color(0.7f, 0.7f, 0.72f), 0.8f, 0.7f);
        var matSlot = GetMat("LP_Metal_Black", new Color(0.1f, 0.1f, 0.1f), 0.5f, 0.3f);
        var body = CreateBox(new Vector3(0.25f, 0.17f, 0.14f), matMetal);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.085f, 0);
        body.gameObject.name = "Body";
        // Slots
        for (int i = 0; i < 2; i++)
        {
            var slot = CreateBox(new Vector3(0.08f, 0.01f, 0.1f), matSlot);
            slot.transform.SetParent(root.transform);
            slot.transform.localPosition = new Vector3(-0.05f + i * 0.1f, 0.175f, 0);
            slot.gameObject.name = $"Slot{i}";
        }
        // Lever
        var lever = CreateBox(new Vector3(0.04f, 0.03f, 0.015f), matMetal);
        lever.transform.SetParent(root.transform);
        lever.transform.localPosition = new Vector3(0.14f, 0.1f, 0);
        lever.gameObject.name = "Lever";
        SavePrefab(root, "LowPoly_Toaster");
        return 1;
    }

    // TOILET1 h=0.40 w=0.33
    static int GenerateToilet1()
    {
        var root = new GameObject("LowPoly_Toilet1");
        var matCeramic = GetMat("LP_Ceramic_White", new Color(0.92f, 0.92f, 0.90f), 0f, 0.7f);
        var matSeat = GetMat("LP_Plastic_White", new Color(0.88f, 0.88f, 0.86f));
        var bowl = CreateCylinder(0.15f, 0.28f, 10, matCeramic);
        bowl.transform.SetParent(root.transform);
        bowl.transform.localPosition = new Vector3(0, 0.14f, 0.04f);
        bowl.gameObject.name = "Bowl";
        var seat = CreateCylinder(0.16f, 0.025f, 10, matSeat);
        seat.transform.SetParent(root.transform);
        seat.transform.localPosition = new Vector3(0, 0.29f, 0.04f);
        seat.gameObject.name = "Seat";
        var tank = CreateBox(new Vector3(0.3f, 0.22f, 0.12f), matCeramic);
        tank.transform.SetParent(root.transform);
        tank.transform.localPosition = new Vector3(0, 0.28f, -0.12f);
        tank.gameObject.name = "Tank";
        SavePrefab(root, "LowPoly_Toilet1");
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

    // VASE h=0.30 w=0.13
    static int GenerateVase()
    {
        var root = new GameObject("LowPoly_Vase");
        var matCeramic = GetMat("LP_Ceramic_Vase", new Color(0.6f, 0.35f, 0.2f), 0.1f, 0.6f);
        // Body (cylinder narrowing at top)
        var body = CreateCylinder(0.06f, 0.25f, 8, matCeramic);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.125f, 0);
        body.gameObject.name = "Body";
        // Lip
        var lip = CreateCylinder(0.045f, 0.04f, 8, matCeramic);
        lip.transform.SetParent(root.transform);
        lip.transform.localPosition = new Vector3(0, 0.27f, 0);
        lip.gameObject.name = "Lip";
        // Base
        var basePart = CreateCylinder(0.05f, 0.02f, 8, matCeramic);
        basePart.transform.SetParent(root.transform);
        basePart.transform.localPosition = new Vector3(0, 0.01f, 0);
        basePart.gameObject.name = "Base";
        SavePrefab(root, "LowPoly_Vase");
        return 1;
    }

    // ========================== MODULAR KITCHEN (1x1x1) ==========================
    // All modules are exactly 1 unit wide, 1 unit tall, 1 unit deep.
    // Base cabinets: countertop at y=0.9, cabinet body 0..0.85, toe kick 0..0.1
    // Wall cabinets: body at y=0..1 (mounted at any height)
    // Naming: KM_ = Kitchen Module

    static Material KM_Body => GetMat("KM_Cabinet_Body", new Color(0.88f, 0.86f, 0.82f));
    static Material KM_Counter => GetMat("KM_Countertop", new Color(0.35f, 0.33f, 0.3f), 0.1f, 0.6f);
    static Material KM_Handle => GetMat("KM_Handle", new Color(0.7f, 0.7f, 0.72f), 0.85f, 0.8f);
    static Material KM_Metal => GetMat("KM_Metal", new Color(0.6f, 0.6f, 0.62f), 0.7f, 0.6f);
    static Material KM_Dark => GetMat("KM_DarkInterior", new Color(0.2f, 0.18f, 0.16f));
    static Material KM_Glass => GetMat("KM_Glass", new Color(0.7f, 0.75f, 0.8f, 0.5f), 0.1f, 0.85f);

    // Shared: add toe kick + countertop to a root, returns the root
    static void KM_AddBaseFrame(GameObject root)
    {
        // Toe kick
        var kick = CreateBox(new Vector3(0.92f, 0.1f, 0.04f), KM_Dark);
        kick.transform.SetParent(root.transform);
        kick.transform.localPosition = new Vector3(0, 0.05f, 0.48f);
        kick.gameObject.name = "ToeKick";
        // Countertop
        var top = CreateBox(new Vector3(1.0f, 0.05f, 1.0f), KM_Counter);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.925f, 0);
        top.gameObject.name = "Countertop";
    }

    // 1. KM_CabinetBase: base cabinet with two doors
    static int GenerateKM_CabinetBase()
    {
        var root = new GameObject("LowPoly_KM_CabinetBase");
        KM_AddBaseFrame(root);
        // Cabinet body
        var body = CreateBox(new Vector3(0.96f, 0.8f, 0.96f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Door split line
        var split = CreateBox(new Vector3(0.005f, 0.7f, 0.01f), KM_Dark);
        split.transform.SetParent(root.transform);
        split.transform.localPosition = new Vector3(0, 0.48f, 0.486f);
        split.gameObject.name = "DoorSplit";
        // Handles
        for (int i = 0; i < 2; i++)
        {
            var h = CreateBox(new Vector3(0.02f, 0.1f, 0.02f), KM_Handle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(i == 0 ? -0.06f : 0.06f, 0.55f, 0.5f);
            h.gameObject.name = $"Handle{i}";
        }
        SavePrefab(root, "LowPoly_KM_CabinetBase");
        return 1;
    }

    // 2. KM_CabinetDrawer: base cabinet with 3 drawers
    static int GenerateKM_CabinetDrawer()
    {
        var root = new GameObject("LowPoly_KM_CabinetDrawer");
        KM_AddBaseFrame(root);
        var body = CreateBox(new Vector3(0.96f, 0.8f, 0.96f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // 3 drawer lines + handles
        float[] drawerY = { 0.28f, 0.5f, 0.72f };
        for (int i = 0; i < 3; i++)
        {
            var line = CreateBox(new Vector3(0.88f, 0.005f, 0.01f), KM_Dark);
            line.transform.SetParent(root.transform);
            line.transform.localPosition = new Vector3(0, drawerY[i], 0.486f);
            line.gameObject.name = $"DrawerLine{i}";
            var h = CreateBox(new Vector3(0.15f, 0.02f, 0.02f), KM_Handle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(0, drawerY[i] + 0.08f, 0.5f);
            h.gameObject.name = $"DrawerHandle{i}";
        }
        SavePrefab(root, "LowPoly_KM_CabinetDrawer");
        return 1;
    }

    // 3. KM_Sink: base cabinet with integrated sink basin + faucet
    static int GenerateKM_Sink()
    {
        var root = new GameObject("LowPoly_KM_Sink");
        KM_AddBaseFrame(root);
        var body = CreateBox(new Vector3(0.96f, 0.8f, 0.96f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Doors
        var split = CreateBox(new Vector3(0.005f, 0.7f, 0.01f), KM_Dark);
        split.transform.SetParent(root.transform);
        split.transform.localPosition = new Vector3(0, 0.48f, 0.486f);
        split.gameObject.name = "DoorSplit";
        for (int i = 0; i < 2; i++)
        {
            var h = CreateBox(new Vector3(0.02f, 0.1f, 0.02f), KM_Handle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(i == 0 ? -0.06f : 0.06f, 0.55f, 0.5f);
            h.gameObject.name = $"Handle{i}";
        }
        // Sink basin (recessed into countertop)
        var basin = CreateBox(new Vector3(0.55f, 0.12f, 0.4f), KM_Metal);
        basin.transform.SetParent(root.transform);
        basin.transform.localPosition = new Vector3(0, 0.89f, 0.05f);
        basin.gameObject.name = "Basin";
        // Faucet base
        var faucetBase = CreateCylinder(0.02f, 0.15f, 6, KM_Handle);
        faucetBase.transform.SetParent(root.transform);
        faucetBase.transform.localPosition = new Vector3(0, 1.02f, -0.25f);
        faucetBase.gameObject.name = "FaucetBase";
        // Faucet spout
        var spout = CreateBox(new Vector3(0.02f, 0.02f, 0.15f), KM_Handle);
        spout.transform.SetParent(root.transform);
        spout.transform.localPosition = new Vector3(0, 1.08f, -0.18f);
        spout.gameObject.name = "FaucetSpout";
        // Handle knobs
        for (int i = 0; i < 2; i++)
        {
            var knob = CreateCylinder(0.015f, 0.03f, 6, KM_Handle);
            knob.transform.SetParent(root.transform);
            knob.transform.localPosition = new Vector3(i == 0 ? -0.1f : 0.1f, 0.99f, -0.25f);
            knob.gameObject.name = $"Knob{i}";
        }
        SavePrefab(root, "LowPoly_KM_Sink");
        return 1;
    }

    // 4. KM_Stove: countertop with 4 burners
    static int GenerateKM_Stove()
    {
        var root = new GameObject("LowPoly_KM_Stove");
        KM_AddBaseFrame(root);
        var body = CreateBox(new Vector3(0.96f, 0.8f, 0.96f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Stove surface (dark, replaces part of countertop)
        var surface = CreateBox(new Vector3(0.9f, 0.01f, 0.9f), GetMat("KM_StoveSurface", new Color(0.08f, 0.08f, 0.08f), 0.3f, 0.7f));
        surface.transform.SetParent(root.transform);
        surface.transform.localPosition = new Vector3(0, 0.955f, 0);
        surface.gameObject.name = "Surface";
        // 4 burners (2x2 grid)
        var matBurner = GetMat("KM_Burner", new Color(0.25f, 0.25f, 0.28f), 0.5f, 0.4f);
        Vector2[] bPos = { new(-0.2f, -0.2f), new(0.2f, -0.2f), new(-0.2f, 0.2f), new(0.2f, 0.2f) };
        float[] bRadius = { 0.12f, 0.12f, 0.09f, 0.09f }; // front burners larger
        for (int i = 0; i < 4; i++)
        {
            var burner = CreateCylinder(bRadius[i], 0.01f, 10, matBurner);
            burner.transform.SetParent(root.transform);
            burner.transform.localPosition = new Vector3(bPos[i].x, 0.965f, bPos[i].y);
            burner.gameObject.name = $"Burner{i}";
        }
        // Knobs on front face
        for (int i = 0; i < 4; i++)
        {
            var knob = CreateCylinder(0.015f, 0.02f, 6, KM_Handle);
            knob.transform.SetParent(root.transform);
            knob.transform.localPosition = new Vector3(-0.3f + i * 0.2f, 0.82f, 0.49f);
            knob.transform.localRotation = Quaternion.Euler(90, 0, 0);
            knob.gameObject.name = $"Knob{i}";
        }
        SavePrefab(root, "LowPoly_KM_Stove");
        return 1;
    }

    // 5. KM_Oven: built-in oven with glass door
    static int GenerateKM_Oven()
    {
        var root = new GameObject("LowPoly_KM_Oven");
        KM_AddBaseFrame(root);
        // Oven body (slightly inset)
        var body = CreateBox(new Vector3(0.96f, 0.8f, 0.96f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Oven door frame
        var doorFrame = CreateBox(new Vector3(0.86f, 0.55f, 0.03f), KM_Metal);
        doorFrame.transform.SetParent(root.transform);
        doorFrame.transform.localPosition = new Vector3(0, 0.42f, 0.486f);
        doorFrame.gameObject.name = "DoorFrame";
        // Glass window
        var glass = CreateBox(new Vector3(0.72f, 0.35f, 0.005f), KM_Glass);
        glass.transform.SetParent(root.transform);
        glass.transform.localPosition = new Vector3(0, 0.44f, 0.5f);
        glass.gameObject.name = "Glass";
        // Door handle
        var handle = CreateBox(new Vector3(0.35f, 0.025f, 0.025f), KM_Handle);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0, 0.72f, 0.5f);
        handle.gameObject.name = "Handle";
        // Temperature display area
        var display = CreateBox(new Vector3(0.15f, 0.04f, 0.005f), GetMat("KM_Display", new Color(0.1f, 0.3f, 0.1f)));
        display.transform.SetParent(root.transform);
        display.transform.localPosition = new Vector3(0, 0.8f, 0.5f);
        display.gameObject.name = "Display";
        SavePrefab(root, "LowPoly_KM_Oven");
        return 1;
    }

    // 6. KM_CabinetWall: wall-mounted upper cabinet with doors
    static int GenerateKM_CabinetWall()
    {
        var root = new GameObject("LowPoly_KM_CabinetWall");
        var body = CreateBox(new Vector3(0.96f, 0.96f, 0.36f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Door split
        var split = CreateBox(new Vector3(0.005f, 0.88f, 0.01f), KM_Dark);
        split.transform.SetParent(root.transform);
        split.transform.localPosition = new Vector3(0, 0.5f, 0.186f);
        split.gameObject.name = "DoorSplit";
        // Handles
        for (int i = 0; i < 2; i++)
        {
            var h = CreateBox(new Vector3(0.02f, 0.1f, 0.02f), KM_Handle);
            h.transform.SetParent(root.transform);
            h.transform.localPosition = new Vector3(i == 0 ? -0.06f : 0.06f, 0.5f, 0.2f);
            h.gameObject.name = $"Handle{i}";
        }
        SavePrefab(root, "LowPoly_KM_CabinetWall");
        return 1;
    }

    // 7. KM_CabinetCorner: L-shaped corner base cabinet
    static int GenerateKM_CabinetCorner()
    {
        var root = new GameObject("LowPoly_KM_CabinetCorner");
        // Countertop (L-shape = 2 overlapping boxes)
        var topA = CreateBox(new Vector3(1.0f, 0.05f, 0.5f), KM_Counter);
        topA.transform.SetParent(root.transform);
        topA.transform.localPosition = new Vector3(0, 0.925f, 0.25f);
        topA.gameObject.name = "CountertopA";
        var topB = CreateBox(new Vector3(0.5f, 0.05f, 0.5f), KM_Counter);
        topB.transform.SetParent(root.transform);
        topB.transform.localPosition = new Vector3(-0.25f, 0.925f, -0.25f);
        topB.gameObject.name = "CountertopB";
        // Cabinet body L-shape
        var bodyA = CreateBox(new Vector3(0.96f, 0.8f, 0.46f), KM_Body);
        bodyA.transform.SetParent(root.transform);
        bodyA.transform.localPosition = new Vector3(0, 0.5f, 0.25f);
        bodyA.gameObject.name = "BodyA";
        var bodyB = CreateBox(new Vector3(0.46f, 0.8f, 0.46f), KM_Body);
        bodyB.transform.SetParent(root.transform);
        bodyB.transform.localPosition = new Vector3(-0.25f, 0.5f, -0.25f);
        bodyB.gameObject.name = "BodyB";
        // Toe kicks
        var kickA = CreateBox(new Vector3(0.92f, 0.1f, 0.04f), KM_Dark);
        kickA.transform.SetParent(root.transform);
        kickA.transform.localPosition = new Vector3(0, 0.05f, 0.5f);
        kickA.gameObject.name = "ToeKickA";
        var kickB = CreateBox(new Vector3(0.04f, 0.1f, 0.46f), KM_Dark);
        kickB.transform.SetParent(root.transform);
        kickB.transform.localPosition = new Vector3(-0.5f, 0.05f, -0.25f);
        kickB.gameObject.name = "ToeKickB";
        // Handle on angled face
        var door = CreateBox(new Vector3(0.35f, 0.7f, 0.02f), KM_Body);
        door.transform.SetParent(root.transform);
        door.transform.localPosition = new Vector3(0.32f, 0.48f, -0.01f);
        door.transform.localRotation = Quaternion.Euler(0, 45, 0);
        door.gameObject.name = "CornerDoor";
        SavePrefab(root, "LowPoly_KM_CabinetCorner");
        return 1;
    }

    // 8. KM_Countertop: plain countertop with no doors (open shelf underneath)
    static int GenerateKM_Countertop()
    {
        var root = new GameObject("LowPoly_KM_Countertop");
        KM_AddBaseFrame(root);
        // Open frame (just side walls, no door)
        var left = CreateBox(new Vector3(0.02f, 0.8f, 0.96f), KM_Body);
        left.transform.SetParent(root.transform);
        left.transform.localPosition = new Vector3(-0.48f, 0.5f, 0);
        left.gameObject.name = "SideL";
        var right = CreateBox(new Vector3(0.02f, 0.8f, 0.96f), KM_Body);
        right.transform.SetParent(root.transform);
        right.transform.localPosition = new Vector3(0.48f, 0.5f, 0);
        right.gameObject.name = "SideR";
        var back = CreateBox(new Vector3(0.96f, 0.8f, 0.02f), KM_Body);
        back.transform.SetParent(root.transform);
        back.transform.localPosition = new Vector3(0, 0.5f, -0.48f);
        back.gameObject.name = "Back";
        // Internal shelf
        var shelf = CreateBox(new Vector3(0.92f, 0.02f, 0.9f), KM_Body);
        shelf.transform.SetParent(root.transform);
        shelf.transform.localPosition = new Vector3(0, 0.45f, 0);
        shelf.gameObject.name = "InternalShelf";
        SavePrefab(root, "LowPoly_KM_Countertop");
        return 1;
    }

    // 9. KM_Fridge: tall fridge (1x2x1 - two units tall)
    static int GenerateKM_Fridge()
    {
        var root = new GameObject("LowPoly_KM_Fridge");
        // Main body
        var body = CreateBox(new Vector3(0.96f, 1.96f, 0.9f), GetMat("KM_FridgeBody", new Color(0.9f, 0.9f, 0.92f), 0.3f, 0.5f));
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1.0f, 0);
        body.gameObject.name = "Body";
        // Freezer door (top)
        var freezerDoor = CreateBox(new Vector3(0.92f, 0.56f, 0.03f), GetMat("KM_FridgeBody", new Color(0.9f, 0.9f, 0.92f), 0.3f, 0.5f));
        freezerDoor.transform.SetParent(root.transform);
        freezerDoor.transform.localPosition = new Vector3(0, 1.7f, 0.465f);
        freezerDoor.gameObject.name = "FreezerDoor";
        // Fridge door (bottom, larger)
        var fridgeDoor = CreateBox(new Vector3(0.92f, 1.2f, 0.03f), GetMat("KM_FridgeBody", new Color(0.9f, 0.9f, 0.92f), 0.3f, 0.5f));
        fridgeDoor.transform.SetParent(root.transform);
        fridgeDoor.transform.localPosition = new Vector3(0, 0.72f, 0.465f);
        fridgeDoor.gameObject.name = "FridgeDoor";
        // Door split line
        var split = CreateBox(new Vector3(0.92f, 0.008f, 0.01f), KM_Dark);
        split.transform.SetParent(root.transform);
        split.transform.localPosition = new Vector3(0, 1.38f, 0.48f);
        split.gameObject.name = "DoorSplit";
        // Handles
        var hTop = CreateBox(new Vector3(0.25f, 0.025f, 0.03f), KM_Handle);
        hTop.transform.SetParent(root.transform);
        hTop.transform.localPosition = new Vector3(0, 1.72f, 0.49f);
        hTop.gameObject.name = "HandleTop";
        var hBot = CreateBox(new Vector3(0.25f, 0.025f, 0.03f), KM_Handle);
        hBot.transform.SetParent(root.transform);
        hBot.transform.localPosition = new Vector3(0, 1.35f, 0.49f);
        hBot.gameObject.name = "HandleBottom";
        SavePrefab(root, "LowPoly_KM_Fridge");
        return 1;
    }

    // 10. KM_Dishwasher: built-in dishwasher
    static int GenerateKM_Dishwasher()
    {
        var root = new GameObject("LowPoly_KM_Dishwasher");
        KM_AddBaseFrame(root);
        var body = CreateBox(new Vector3(0.96f, 0.8f, 0.96f), KM_Metal);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Door panel
        var door = CreateBox(new Vector3(0.88f, 0.7f, 0.02f), KM_Body);
        door.transform.SetParent(root.transform);
        door.transform.localPosition = new Vector3(0, 0.48f, 0.49f);
        door.gameObject.name = "Door";
        // Handle bar (horizontal)
        var handle = CreateBox(new Vector3(0.4f, 0.025f, 0.03f), KM_Handle);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0, 0.78f, 0.51f);
        handle.gameObject.name = "Handle";
        // Controls (small buttons)
        for (int i = 0; i < 3; i++)
        {
            var btn = CreateCylinder(0.012f, 0.01f, 6, KM_Handle);
            btn.transform.SetParent(root.transform);
            btn.transform.localPosition = new Vector3(-0.1f + i * 0.1f, 0.86f, 0.5f);
            btn.transform.localRotation = Quaternion.Euler(90, 0, 0);
            btn.gameObject.name = $"Button{i}";
        }
        SavePrefab(root, "LowPoly_KM_Dishwasher");
        return 1;
    }

    // 11. KM_Hood: range hood / extractor
    static int GenerateKM_Hood()
    {
        var root = new GameObject("LowPoly_KM_Hood");
        // Chimney (tall narrow box going up)
        var chimney = CreateBox(new Vector3(0.4f, 0.5f, 0.35f), KM_Metal);
        chimney.transform.SetParent(root.transform);
        chimney.transform.localPosition = new Vector3(0, 0.75f, -0.3f);
        chimney.gameObject.name = "Chimney";
        // Hood body (trapezoidal shape approximated as box)
        var hood = CreateBox(new Vector3(0.9f, 0.25f, 0.55f), KM_Metal);
        hood.transform.SetParent(root.transform);
        hood.transform.localPosition = new Vector3(0, 0.38f, -0.2f);
        hood.gameObject.name = "Hood";
        // Filter grille
        var filter = CreateBox(new Vector3(0.75f, 0.01f, 0.4f), GetMat("KM_FilterGrille", new Color(0.5f, 0.5f, 0.52f), 0.6f, 0.4f));
        filter.transform.SetParent(root.transform);
        filter.transform.localPosition = new Vector3(0, 0.26f, -0.2f);
        filter.gameObject.name = "Filter";
        // Light strip
        var light = CreateBox(new Vector3(0.6f, 0.01f, 0.02f), GetMat("KM_LightStrip", new Color(0.95f, 0.92f, 0.8f)));
        light.transform.SetParent(root.transform);
        light.transform.localPosition = new Vector3(0, 0.27f, 0.03f);
        light.gameObject.name = "Light";
        SavePrefab(root, "LowPoly_KM_Hood");
        return 1;
    }

    // 12. KM_Microwave: countertop microwave
    static int GenerateKM_Microwave()
    {
        var root = new GameObject("LowPoly_KM_Microwave");
        // Body
        var body = CreateBox(new Vector3(0.9f, 0.5f, 0.55f), KM_Metal);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.25f, 0);
        body.gameObject.name = "Body";
        // Door
        var door = CreateBox(new Vector3(0.55f, 0.38f, 0.02f), KM_Dark);
        door.transform.SetParent(root.transform);
        door.transform.localPosition = new Vector3(-0.1f, 0.26f, 0.286f);
        door.gameObject.name = "Door";
        // Glass
        var glass = CreateBox(new Vector3(0.45f, 0.3f, 0.005f), KM_Glass);
        glass.transform.SetParent(root.transform);
        glass.transform.localPosition = new Vector3(-0.1f, 0.26f, 0.3f);
        glass.gameObject.name = "Glass";
        // Control panel
        var panel = CreateBox(new Vector3(0.2f, 0.38f, 0.005f), GetMat("KM_Display", new Color(0.1f, 0.3f, 0.1f)));
        panel.transform.SetParent(root.transform);
        panel.transform.localPosition = new Vector3(0.32f, 0.26f, 0.286f);
        panel.gameObject.name = "Panel";
        // Handle
        var handle = CreateBox(new Vector3(0.025f, 0.2f, 0.03f), KM_Handle);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0.16f, 0.26f, 0.3f);
        handle.gameObject.name = "Handle";
        SavePrefab(root, "LowPoly_KM_Microwave");
        return 1;
    }

    // 13. KM_ShelfOpen: open wall shelf with 3 levels
    static int GenerateKM_ShelfOpen()
    {
        var root = new GameObject("LowPoly_KM_ShelfOpen");
        var matWood = GetMat("KM_ShelfWood", new Color(0.55f, 0.38f, 0.22f));
        // 3 shelves
        for (int i = 0; i < 3; i++)
        {
            var shelf = CreateBox(new Vector3(0.96f, 0.025f, 0.3f), matWood);
            shelf.transform.SetParent(root.transform);
            shelf.transform.localPosition = new Vector3(0, 0.15f + i * 0.35f, 0);
            shelf.gameObject.name = $"Shelf{i}";
        }
        // Side brackets
        for (int s = 0; s < 2; s++)
        {
            float x = s == 0 ? -0.46f : 0.46f;
            var bracket = CreateBox(new Vector3(0.02f, 0.96f, 0.28f), matWood);
            bracket.transform.SetParent(root.transform);
            bracket.transform.localPosition = new Vector3(x, 0.5f, 0);
            bracket.gameObject.name = $"Side{s}";
        }
        SavePrefab(root, "LowPoly_KM_ShelfOpen");
        return 1;
    }

    // 14. KM_Island: kitchen island center piece (1x1x1)
    static int GenerateKM_Island()
    {
        var root = new GameObject("LowPoly_KM_Island");
        // Countertop (slightly larger overhang)
        var top = CreateBox(new Vector3(1.0f, 0.05f, 1.0f), KM_Counter);
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.925f, 0);
        top.gameObject.name = "Countertop";
        // Body
        var body = CreateBox(new Vector3(0.92f, 0.82f, 0.92f), KM_Body);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.gameObject.name = "Body";
        // Toe kick all around
        for (int i = 0; i < 4; i++)
        {
            bool xAxis = i < 2;
            float sign = (i % 2 == 0) ? 1 : -1;
            var kick = CreateBox(xAxis ? new Vector3(0.88f, 0.1f, 0.04f) : new Vector3(0.04f, 0.1f, 0.88f), KM_Dark);
            kick.transform.SetParent(root.transform);
            kick.transform.localPosition = new Vector3(
                xAxis ? 0 : sign * 0.46f,
                0.05f,
                xAxis ? sign * 0.46f : 0);
            kick.gameObject.name = $"ToeKick{i}";
        }
        // Drawer on front
        var line = CreateBox(new Vector3(0.6f, 0.005f, 0.01f), KM_Dark);
        line.transform.SetParent(root.transform);
        line.transform.localPosition = new Vector3(0, 0.55f, 0.466f);
        line.gameObject.name = "DrawerLine";
        var handle = CreateBox(new Vector3(0.15f, 0.02f, 0.02f), KM_Handle);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0, 0.65f, 0.48f);
        handle.gameObject.name = "DrawerHandle";
        // Towel bar on side
        var towelBar = CreateBox(new Vector3(0.02f, 0.02f, 0.3f), KM_Handle);
        towelBar.transform.SetParent(root.transform);
        towelBar.transform.localPosition = new Vector3(0.47f, 0.45f, 0);
        towelBar.gameObject.name = "TowelBar";
        SavePrefab(root, "LowPoly_KM_Island");
        return 1;
    }
}
