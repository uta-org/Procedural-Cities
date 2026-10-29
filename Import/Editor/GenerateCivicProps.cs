using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the civic building props (P43, owner Q-20261029-3 A); the town hall's first (P43.3):
/// CivicFlags (three flags on poles in a base), CoatOfArms (a wall-hung shield), Lectern,
/// WaitingChairs (a row of three seats on a beam), LeafletRack (a floor stand of leaflets), Bust
/// (on a pedestal) and CivicVitrine (a glass case on a plinth). The service counter, the rug,
/// the plenary table and chairs and the plants reuse StoreCounter, LivingRug, the meeting room's
/// pieces and the planters. Every piece has its pivot on its bottom face at the centre, front +Z;
/// a wall-hung one has its back at Z = 0. Output: Models/LowPoly/&lt;name&gt;_Combined.asset and
/// Resources/Prefabs/AssetContents/&lt;name&gt;.prefab (GUIDs kept on rebuild).
/// Menu: Tools / Procedural Cities / Generate Civic Props
/// </summary>
public static class GenerateCivicProps
{
    [MenuItem("Tools/Procedural Cities/Generate Civic Props")]
    public static void Generate()
    {
        var brass = LowPolyBoxMesh.GetMat("LP_CV_Brass", new Color(0.78f, 0.62f, 0.28f), 0.8f, 0.6f);
        var dark = LowPolyBoxMesh.GetMat("LP_CV_DarkWood", new Color(0.30f, 0.18f, 0.10f), 0f, 0.35f);
        var stone = LowPolyBoxMesh.GetMat("LP_CV_Stone", new Color(0.82f, 0.80f, 0.76f), 0f, 0.2f);
        var black = LowPolyBoxMesh.GetMat("LP_CV_Black", new Color(0.12f, 0.12f, 0.13f), 0.3f, 0.4f);
        var glass = LowPolyBoxMesh.GetMat("LP_CV_Glass", new Color(0.70f, 0.82f, 0.88f), 0.1f, 0.95f);

        BuildFlags(brass, black,
            LowPolyBoxMesh.GetMat("LP_CV_FlagRed", new Color(0.72f, 0.12f, 0.12f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagYellow", new Color(0.92f, 0.76f, 0.15f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagBlue", new Color(0.14f, 0.24f, 0.58f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagWhite", new Color(0.94f, 0.94f, 0.92f), 0f, 0.2f));
        BuildCoatOfArms(dark, brass,
            LowPolyBoxMesh.GetMat("LP_CV_ShieldRed", new Color(0.62f, 0.10f, 0.10f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_ShieldBlue", new Color(0.12f, 0.20f, 0.50f), 0f, 0.3f));
        BuildLectern(dark, brass);
        BuildWaitingChairs(black, LowPolyBoxMesh.GetMat("LP_CV_Seat", new Color(0.18f, 0.32f, 0.52f), 0f, 0.3f));
        BuildLeafletRack(black, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_LeafWhite", new Color(0.95f, 0.95f, 0.93f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafBlue", new Color(0.45f, 0.62f, 0.85f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafGreen", new Color(0.45f, 0.72f, 0.45f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafOrange", new Color(0.95f, 0.62f, 0.28f), 0f, 0.2f),
        });
        BuildBust(stone, LowPolyBoxMesh.GetMat("LP_CV_Bronze", new Color(0.42f, 0.30f, 0.18f), 0.8f, 0.5f));
        BuildVitrine(dark, glass, brass);

        // The museum's (P43.13).
        BuildSculpturePedestal(stone, LowPolyBoxMesh.GetMat("LP_CV_Bronze", new Color(0.42f, 0.30f, 0.18f), 0.8f, 0.5f), brass);

        // The hotel's (P43.17).
        BuildLuggageCart(brass, LowPolyBoxMesh.GetMat("LP_CV_CartRed", new Color(0.55f, 0.08f, 0.10f), 0f, 0.4f), black);
        BuildLuggageRack(dark, black, LowPolyBoxMesh.GetMat("LP_CV_Suitcase", new Color(0.20f, 0.30f, 0.45f), 0.1f, 0.4f));
        BuildMinibar(dark, black, LowPolyBoxMesh.GetMat("LP_CV_DetectorGrey", new Color(0.72f, 0.73f, 0.75f), 0.3f, 0.4f));

        // The police station's (P43.4).
        var steel = LowPolyBoxMesh.GetMat("LP_CV_Steel", new Color(0.55f, 0.57f, 0.60f), 0.8f, 0.5f);
        BuildCellBars(steel);
        BuildWantedBoard(dark, LowPolyBoxMesh.GetMat("LP_CV_Cork", new Color(0.70f, 0.52f, 0.34f), 0f, 0.1f),
            LowPolyBoxMesh.GetMat("LP_CV_Poster", new Color(0.93f, 0.90f, 0.80f), 0f, 0.2f), black);
        BuildMetalDetector(LowPolyBoxMesh.GetMat("LP_CV_DetectorGrey", new Color(0.72f, 0.73f, 0.75f), 0.3f, 0.4f), black);
        BuildPoliceCar(LowPolyBoxMesh.GetMat("LP_CV_CarWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_CV_CarBlue", new Color(0.10f, 0.20f, 0.55f), 0.2f, 0.6f), black,
            LowPolyBoxMesh.GetMat("LP_CV_CarGlass", new Color(0.20f, 0.26f, 0.32f), 0.3f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_CV_LightBlue", new Color(0.20f, 0.45f, 1.00f), 0f, 0.9f));

        // The fire station's (P43.5).
        var fireRed = LowPolyBoxMesh.GetMat("LP_CV_FireRed", new Color(0.78f, 0.08f, 0.06f), 0.3f, 0.6f);
        var tan = LowPolyBoxMesh.GetMat("LP_CV_BunkerTan", new Color(0.62f, 0.52f, 0.30f), 0f, 0.2f);
        var reflective = LowPolyBoxMesh.GetMat("LP_CV_Reflective", new Color(0.95f, 0.88f, 0.25f), 0f, 0.7f);
        BuildFireEngine(fireRed, steel, black, LowPolyBoxMesh.GetMat("LP_CV_CarGlass", new Color(0.20f, 0.26f, 0.32f), 0.3f, 0.9f), reflective);
        BuildHoseReel(fireRed, LowPolyBoxMesh.GetMat("LP_CV_Hose", new Color(0.85f, 0.82f, 0.70f), 0f, 0.2f));
        BuildHelmetRack(dark, LowPolyBoxMesh.GetMat("LP_CV_HelmetYellow", new Color(0.95f, 0.78f, 0.10f), 0.1f, 0.6f), fireRed);
        BuildFirePole(steel);
        BuildToolBench(dark, steel, black);
        BuildAirTanks(steel, LowPolyBoxMesh.GetMat("LP_CV_TankYellow", new Color(0.90f, 0.75f, 0.15f), 0.3f, 0.6f), black);
        BuildBunkerBoots(black, tan, reflective);
        BuildFireGarageDoor(fireRed, steel, LowPolyBoxMesh.GetMat("LP_CV_GarageGlass", new Color(0.55f, 0.65f, 0.72f), 0.2f, 0.9f));

        // Healthcare's (P43.6).
        var white = LowPolyBoxMesh.GetMat("LP_CV_MedWhite", new Color(0.94f, 0.95f, 0.96f), 0.1f, 0.5f);
        var mint = LowPolyBoxMesh.GetMat("LP_CV_MedMint", new Color(0.62f, 0.82f, 0.78f), 0f, 0.4f);
        var vinyl = LowPolyBoxMesh.GetMat("LP_CV_MedVinyl", new Color(0.30f, 0.45f, 0.62f), 0f, 0.4f);
        BuildExamCouch(steel, vinyl, white);
        BuildFoldingScreen(steel, mint);
        BuildMedicalCart(steel, white, LowPolyBoxMesh.GetMat("LP_CV_MedBlue", new Color(0.35f, 0.55f, 0.85f), 0f, 0.4f));
        BuildWheelchair(steel, black);
        BuildIvStand(steel, LowPolyBoxMesh.GetMat("LP_CV_IvBag", new Color(0.85f, 0.92f, 0.95f), 0f, 0.95f));
        BuildMedicalScale(steel, white, black);
        BuildHospitalBed(steel, white, mint);
        BuildAmbulance(white, LowPolyBoxMesh.GetMat("LP_CV_AmbYellow", new Color(0.95f, 0.85f, 0.15f), 0.2f, 0.6f),
            LowPolyBoxMesh.GetMat("LP_CV_AmbGreen", new Color(0.10f, 0.55f, 0.25f), 0.2f, 0.6f), black,
            LowPolyBoxMesh.GetMat("LP_CV_CarGlass", new Color(0.20f, 0.26f, 0.32f), 0.3f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_CV_LightBlue", new Color(0.20f, 0.45f, 1.00f), 0f, 0.9f));

        // The school's (P43.7).
        var beech = LowPolyBoxMesh.GetMat("LP_CV_Beech", new Color(0.80f, 0.66f, 0.46f), 0f, 0.35f);
        var slate = LowPolyBoxMesh.GetMat("LP_CV_Slate", new Color(0.14f, 0.26f, 0.20f), 0f, 0.15f);
        var chalk = LowPolyBoxMesh.GetMat("LP_CV_Chalk", new Color(0.92f, 0.92f, 0.88f), 0f, 0.1f);
        var orange = LowPolyBoxMesh.GetMat("LP_CV_HoopOrange", new Color(0.92f, 0.42f, 0.10f), 0.2f, 0.5f);
        var matBlue = LowPolyBoxMesh.GetMat("LP_CV_GymMatBlue", new Color(0.16f, 0.36f, 0.72f), 0f, 0.3f);
        BuildSchoolDesk(beech, steel, LowPolyBoxMesh.GetMat("LP_CV_SeatGreen", new Color(0.22f, 0.50f, 0.34f), 0f, 0.3f));
        BuildBlackboard(beech, slate, chalk);
        BuildLabBench(LowPolyBoxMesh.GetMat("LP_CV_LabTop", new Color(0.18f, 0.19f, 0.20f), 0f, 0.6f), white, steel);
        BuildBasketballHoop(white, orange, steel);
        BuildGymMat(matBlue);
        BuildGymBench(beech, steel);
        // The sports centre's (P43.18).
        BuildCourtFloor(LowPolyBoxMesh.GetMat("LP_CV_CourtWood", new Color(0.78f, 0.56f, 0.32f), 0f, 0.5f),
            LowPolyBoxMesh.GetMat("LP_CV_CourtLine", new Color(0.96f, 0.96f, 0.94f), 0f, 0.3f), orange);
        BuildBleachers(LowPolyBoxMesh.GetMat("LP_CV_SeatBlue", new Color(0.15f, 0.30f, 0.65f), 0f, 0.3f), steel);
        BuildHoopStand(white, orange, steel, LowPolyBoxMesh.GetMat("LP_CV_PadBlue", new Color(0.15f, 0.30f, 0.65f), 0f, 0.3f));
        BuildScoreboard(black, LowPolyBoxMesh.GetMat("LP_CV_ScoreRed", new Color(1.00f, 0.20f, 0.15f), 0f, 0.9f),
            LowPolyBoxMesh.GetMat("LP_CV_ScoreAmber", new Color(1.00f, 0.75f, 0.15f), 0f, 0.9f));
        // The secondary school's (P43.10).
        BuildFumeHood(white, glass, steel, LowPolyBoxMesh.GetMat("LP_CV_LabTop", new Color(0.18f, 0.19f, 0.20f), 0f, 0.6f));
        BuildPeriodicTable(white, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_LeafBlue", new Color(0.45f, 0.62f, 0.85f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafGreen", new Color(0.45f, 0.72f, 0.45f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafOrange", new Color(0.95f, 0.62f, 0.28f), 0f, 0.2f),
        });
        BuildSkeletonModel(chalk, steel);
        // The library's (P43.16).
        var bookMats = new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_FlagRed", new Color(0.72f, 0.12f, 0.12f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagBlue", new Color(0.14f, 0.24f, 0.58f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafGreen", new Color(0.45f, 0.72f, 0.45f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagYellow", new Color(0.92f, 0.76f, 0.15f), 0f, 0.2f),
        };
        BuildReadingTable(beech, steel, LowPolyBoxMesh.GetMat("LP_CV_LampGreen", new Color(0.10f, 0.45f, 0.25f), 0.2f, 0.7f),
            LowPolyBoxMesh.GetMat("LP_CV_SeatGreen", new Color(0.22f, 0.50f, 0.34f), 0f, 0.3f));
        BuildLibraryStack(steel, bookMats);
        // The university's (P43.11).
        BuildLectureSeatRow(beech, steel, LowPolyBoxMesh.GetMat("LP_CV_LectureSeat", new Color(0.55f, 0.12f, 0.14f), 0f, 0.3f));
        BuildBookcase(beech, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_FlagRed", new Color(0.72f, 0.12f, 0.12f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagBlue", new Color(0.14f, 0.24f, 0.58f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafGreen", new Color(0.45f, 0.72f, 0.45f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_FlagYellow", new Color(0.92f, 0.76f, 0.15f), 0f, 0.2f),
        });

        // The supermarket's (P43.8).
        var basketRed = LowPolyBoxMesh.GetMat("LP_CV_BasketRed", new Color(0.80f, 0.12f, 0.12f), 0.1f, 0.4f);
        BuildCheckoutCounter(white, black, steel, LowPolyBoxMesh.GetMat("LP_CV_Belt", new Color(0.10f, 0.10f, 0.11f), 0f, 0.2f));
        BuildFridgeCase(white, glass, steel, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_GoodsMilk", new Color(0.92f, 0.93f, 0.95f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_GoodsGreen", new Color(0.35f, 0.65f, 0.30f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_GoodsOrange", new Color(0.95f, 0.55f, 0.15f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_GoodsBlue", new Color(0.20f, 0.40f, 0.80f), 0f, 0.3f),
        });
        BuildTrolleyRow(steel, basketRed);
        BuildBasketStack(basketRed, black);

        // The bank's (P43.19).
        var navy = LowPolyBoxMesh.GetMat("LP_CV_BankNavy", new Color(0.10f, 0.16f, 0.32f), 0.2f, 0.5f);
        BuildBankCounter(navy, white, glass, black);
        BuildAtm(steel, navy, black, LowPolyBoxMesh.GetMat("LP_CV_PumpScreen", new Color(0.10f, 0.20f, 0.18f), 0.3f, 0.9f));
        BuildQueueStanchions(steel, LowPolyBoxMesh.GetMat("LP_CV_BeltRed", new Color(0.70f, 0.10f, 0.12f), 0f, 0.4f));
        BuildBankSafe(LowPolyBoxMesh.GetMat("LP_CV_SafeGrey", new Color(0.35f, 0.37f, 0.40f), 0.7f, 0.5f), steel);

        // The pharmacy's (P43.20).
        var crossGreen = LowPolyBoxMesh.GetMat("LP_CV_CrossGreen", new Color(0.10f, 0.85f, 0.30f), 0f, 0.9f);
        BuildPharmacyCounter(white, crossGreen, black);
        BuildPharmacyShelf(white, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_GoodsMilk", new Color(0.92f, 0.93f, 0.95f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_GoodsGreen", new Color(0.35f, 0.65f, 0.30f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_GoodsBlue", new Color(0.20f, 0.40f, 0.80f), 0f, 0.3f),
            LowPolyBoxMesh.GetMat("LP_CV_GoodsOrange", new Color(0.95f, 0.55f, 0.15f), 0f, 0.3f),
        });
        BuildPharmacyCross(crossGreen, black);
        BuildBloodPressureKiosk(white, crossGreen, black);

        // The shopping centre's (P43.12).
        BuildMallBench(LowPolyBoxMesh.GetMat("LP_CV_Beech", new Color(0.80f, 0.66f, 0.46f), 0f, 0.35f), steel);
        BuildMallKiosk(white, LowPolyBoxMesh.GetMat("LP_CV_KioskTeal", new Color(0.10f, 0.55f, 0.58f), 0.1f, 0.5f), glass,
            LowPolyBoxMesh.GetMat("LP_CV_GoodsOrange", new Color(0.95f, 0.55f, 0.15f), 0f, 0.3f));
        BuildDirectoryBoard(steel, LowPolyBoxMesh.GetMat("LP_CV_PumpScreen", new Color(0.10f, 0.20f, 0.18f), 0.3f, 0.9f), new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_LeafBlue", new Color(0.45f, 0.62f, 0.85f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafGreen", new Color(0.45f, 0.72f, 0.45f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafOrange", new Color(0.95f, 0.62f, 0.28f), 0f, 0.2f),
        });

        // The bar's (P43.14).
        var darkWood = LowPolyBoxMesh.GetMat("LP_CV_DarkWood", new Color(0.30f, 0.18f, 0.10f), 0f, 0.35f);
        BuildBarHighTable(darkWood, black, steel);
        BuildDartboard(black, LowPolyBoxMesh.GetMat("LP_CV_FlagRed", new Color(0.72f, 0.12f, 0.12f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_Poster", new Color(0.93f, 0.90f, 0.80f), 0f, 0.2f), darkWood);
        BuildJukebox(darkWood, LowPolyBoxMesh.GetMat("LP_CV_JukeboxGlow", new Color(1.00f, 0.60f, 0.20f), 0f, 0.9f), glass, steel);

        // The nightclub's (P43.15).
        var neonPink = LowPolyBoxMesh.GetMat("LP_CV_NeonPink", new Color(1.00f, 0.20f, 0.70f), 0f, 0.9f);
        var neonCyan = LowPolyBoxMesh.GetMat("LP_CV_NeonCyan", new Color(0.20f, 0.90f, 1.00f), 0f, 0.9f);
        var neonViolet = LowPolyBoxMesh.GetMat("LP_CV_NeonViolet", new Color(0.55f, 0.25f, 1.00f), 0f, 0.9f);
        BuildDjBooth(black, neonPink, steel);
        BuildDanceFloor(new[] { neonPink, neonCyan, neonViolet, black });
        BuildSpeakerStack(black, steel);
        BuildDiscoBall(LowPolyBoxMesh.GetMat("LP_CV_Mirror", new Color(0.85f, 0.88f, 0.92f), 1.0f, 0.95f), steel);

        // The petrol station's (P43.9).
        var brandGreen = LowPolyBoxMesh.GetMat("LP_CV_FuelGreen", new Color(0.05f, 0.45f, 0.25f), 0.2f, 0.5f);
        var brandYellow = LowPolyBoxMesh.GetMat("LP_CV_FuelYellow", new Color(0.98f, 0.80f, 0.10f), 0.2f, 0.5f);
        var screen = LowPolyBoxMesh.GetMat("LP_CV_PumpScreen", new Color(0.10f, 0.20f, 0.18f), 0.3f, 0.9f);
        BuildPetrolCanopy(white, brandGreen, brandYellow, steel);
        BuildFuelPump(white, brandGreen, black, screen);
        BuildPriceTotem(brandGreen, brandYellow, white, black);
        BuildAirPump(LowPolyBoxMesh.GetMat("LP_CV_AirRed", new Color(0.80f, 0.15f, 0.12f), 0.2f, 0.5f), black, steel);
        BuildIceChest(white, LowPolyBoxMesh.GetMat("LP_CV_IceBlue", new Color(0.35f, 0.60f, 0.90f), 0f, 0.4f), glass);
        BuildOilRack(steel, new[] { brandYellow, basketRed, black, LowPolyBoxMesh.GetMat("LP_CV_GoodsBlue", new Color(0.20f, 0.40f, 0.80f), 0f, 0.3f) });
        BuildNewspaperStand(steel, new[]
        {
            LowPolyBoxMesh.GetMat("LP_CV_LeafWhite", new Color(0.95f, 0.95f, 0.93f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafBlue", new Color(0.45f, 0.62f, 0.85f), 0f, 0.2f),
            LowPolyBoxMesh.GetMat("LP_CV_LeafOrange", new Color(0.95f, 0.62f, 0.28f), 0f, 0.2f),
        });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static LowPolyBoxMesh.Part Box(Material mat, float x0, float y0, float z0, float x1, float y1, float z1) =>
        LowPolyBoxMesh.Part.FromMinMax(mat, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

    /// <summary>A 12-sided prism <paramref name="width"/> across the flats (six boxes turned 30 degrees apart).</summary>
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
        // The street pieces (patrol car, garage door) are drawn instanced beyond the near band,
        // which needs instancing on their materials (P37 gotcha); harmless indoors.
        foreach (var material in materials)
        {
            if (material != null && !material.enableInstancing)
            {
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }
        }

        LowPolyBoxMesh.SavePrefab(name, child, mesh, materials, mesh.bounds.center, mesh.bounds.size);
    }

    private static void BuildFlags(Material brass, Material black, Material red, Material yellow, Material blue, Material white)
    {
        // A base 0.9 x 0.3 m holding three poles 2.4 m high, a flag hanging from each.
        var parts = new List<LowPolyBoxMesh.Part> { Box(black, -0.45f, 0f, -0.15f, 0.45f, 0.08f, 0.15f) };
        var flags = new[] { (red, yellow), (blue, white), (yellow, red) };
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 0.32f;
            Round(parts, brass, x, 0.08f, 0f, 0.035f, 2.40f);
            Round(parts, brass, x, 2.40f, 0f, 0.07f, 2.46f);
            var (a, b) = flags[i];
            parts.Add(Box(a, x + 0.02f, 1.55f, -0.008f, x + 0.30f, 2.30f, 0.008f));
            parts.Add(Box(b, x + 0.02f, 1.80f, -0.009f, x + 0.30f, 2.05f, 0.009f));
        }

        Save("CivicFlags", "civic_flags", parts);
    }

    private static void BuildCoatOfArms(Material wood, Material brass, Material red, Material blue)
    {
        // A wooden plaque 0.60 x 0.75 m with a quartered shield and a crown, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.30f, 0f, 0f, 0.30f, 0.75f, 0.03f),
            Box(red, -0.20f, 0.30f, 0.03f, 0f, 0.55f, 0.045f),
            Box(blue, 0f, 0.30f, 0.03f, 0.20f, 0.55f, 0.045f),
            Box(blue, -0.20f, 0.12f, 0.03f, 0f, 0.30f, 0.045f),
            Box(red, 0f, 0.12f, 0.03f, 0.20f, 0.30f, 0.045f),
            new(red, new Vector3(0f, 0.10f, 0.037f), new Vector3(0.20f, 0.20f, 0.015f), Quaternion.Euler(0f, 0f, 45f)),
            Box(brass, -0.22f, 0.55f, 0.03f, 0.22f, 0.58f, 0.05f),
            Box(brass, -0.14f, 0.58f, 0.03f, 0.14f, 0.68f, 0.05f),
        };
        Save("CoatOfArms", "coat_of_arms", parts);
    }

    private static void BuildLectern(Material wood, Material brass)
    {
        // A base, a column and a sloping top facing -Z (the speaker stands behind it, the hall in front +Z).
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.30f, 0f, -0.25f, 0.30f, 0.06f, 0.25f),
            Box(wood, -0.22f, 0.06f, -0.15f, 0.22f, 1.00f, 0.15f),
            new(wood, new Vector3(0f, 1.08f, -0.02f), new Vector3(0.56f, 0.05f, 0.42f), Quaternion.Euler(-15f, 0f, 0f)),
            Box(brass, -0.10f, 0.55f, 0.15f, 0.10f, 0.70f, 0.16f),
        };
        Save("Lectern", "lectern", parts);
    }

    private static void BuildWaitingChairs(Material frame, Material seat)
    {
        // Three seats 0.5 m wide on a beam with two legs, backs at -Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.78f, 0.36f, -0.06f, 0.78f, 0.40f, 0.06f),
            Box(frame, -0.70f, 0f, -0.05f, -0.64f, 0.36f, 0.05f),
            Box(frame, 0.64f, 0f, -0.05f, 0.70f, 0.36f, 0.05f),
            Box(frame, -0.74f, 0f, -0.25f, -0.60f, 0.02f, 0.25f),
            Box(frame, 0.60f, 0f, -0.25f, 0.74f, 0.02f, 0.25f),
        };
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 0.52f;
            parts.Add(Box(seat, x - 0.24f, 0.40f, -0.22f, x + 0.24f, 0.46f, 0.24f));
            parts.Add(new LowPolyBoxMesh.Part(seat, new Vector3(x, 0.70f, -0.24f), new Vector3(0.48f, 0.48f, 0.05f), Quaternion.Euler(-8f, 0f, 0f)));
        }

        Save("WaitingChairs", "waiting_chairs", parts);
    }

    private static void BuildLeafletRack(Material frame, Material[] leaflets)
    {
        // A floor stand 1.4 m high with four sloping shelves of leaflets facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.25f, 0f, -0.20f, 0.25f, 0.03f, 0.20f),
            Box(frame, -0.02f, 0.03f, -0.10f, 0.02f, 1.40f, -0.06f),
            Box(frame, -0.22f, 0.30f, -0.10f, 0.22f, 1.40f, -0.08f),
        };
        for (var r = 0; r < 4; r++)
        {
            var y = 0.42f + r * 0.26f;
            parts.Add(new LowPolyBoxMesh.Part(frame, new Vector3(0f, y, -0.02f), new Vector3(0.44f, 0.02f, 0.12f), Quaternion.Euler(20f, 0f, 0f)));
            for (var c = 0; c < 3; c++)
                parts.Add(new LowPolyBoxMesh.Part(leaflets[(r + c) % leaflets.Length], new Vector3((c - 1) * 0.14f, y + 0.10f, -0.05f),
                    new Vector3(0.12f, 0.20f, 0.01f), Quaternion.Euler(-12f, 0f, 0f)));
        }

        Save("LeafletRack", "leaflet_rack", parts);
    }

    private static void BuildBust(Material stone, Material bronze)
    {
        // A square pedestal 1.1 m high and a bronze bust on it, facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(stone, -0.22f, 0f, -0.22f, 0.22f, 0.08f, 0.22f),
            Box(stone, -0.17f, 0.08f, -0.17f, 0.17f, 1.02f, 0.17f),
            Box(stone, -0.20f, 1.02f, -0.20f, 0.20f, 1.10f, 0.20f),
            Box(bronze, -0.18f, 1.10f, -0.10f, 0.18f, 1.30f, 0.10f),
        };
        Round(parts, bronze, 0f, 1.30f, 0f, 0.08f, 1.36f);
        Round(parts, bronze, 0f, 1.36f, 0.01f, 0.18f, 1.58f);
        Round(parts, bronze, 0f, 1.58f, 0.0f, 0.15f, 1.62f);
        Save("Bust", "bust", parts);
    }

    private static void BuildCellBars(Material steel)
    {
        // A cell front 2.4 x 2.5 m standing on the floor: top and bottom rails, bars every 0.15 m,
        // a door frame in the middle third and its lock box, its back at Z = 0 (it stands off a wall).
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -1.20f, 0f, 0f, 1.20f, 0.06f, 0.06f),
            Box(steel, -1.20f, 2.44f, 0f, 1.20f, 2.50f, 0.06f),
            Box(steel, -1.20f, 1.10f, 0.005f, 1.20f, 1.15f, 0.055f),
            Box(steel, -0.40f, 0.06f, 0f, -0.36f, 2.44f, 0.06f),
            Box(steel, 0.36f, 0.06f, 0f, 0.40f, 2.44f, 0.06f),
            Box(steel, 0.20f, 1.00f, 0.06f, 0.32f, 1.20f, 0.10f),
        };
        for (var x = -1.14f; x <= 1.15f; x += 0.15f)
            parts.Add(Box(steel, x - 0.012f, 0.06f, 0.018f, x + 0.012f, 2.44f, 0.042f));
        Save("CellBars", "cell_bars", parts);
    }

    private static void BuildWantedBoard(Material frame, Material cork, Material poster, Material ink)
    {
        // A cork board 1.0 x 0.7 m with six "wanted" posters, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(frame, -0.50f, 0f, 0f, 0.50f, 0.70f, 0.03f),
            Box(cork, -0.46f, 0.04f, 0.03f, 0.46f, 0.66f, 0.035f),
        };
        for (var i = 0; i < 6; i++)
        {
            var x = -0.32f + (i % 3) * 0.32f;
            var y = 0.10f + (i / 3) * 0.30f;
            parts.Add(Box(poster, x - 0.12f, y, 0.035f, x + 0.12f, y + 0.24f, 0.038f));
            parts.Add(Box(ink, x - 0.06f, y + 0.08f, 0.038f, x + 0.06f, y + 0.20f, 0.040f));
            parts.Add(Box(ink, x - 0.09f, y + 0.02f, 0.038f, x + 0.09f, y + 0.05f, 0.040f));
        }

        Save("WantedBoard", "wanted_board", parts);
    }

    private static void BuildMetalDetector(Material body, Material black)
    {
        // A walk-through arch 0.9 m wide inside, 2.2 m high, pillars 0.12 m thick along Z 0.5 m.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.57f, 0f, -0.25f, -0.45f, 2.10f, 0.25f),
            Box(body, 0.45f, 0f, -0.25f, 0.57f, 2.10f, 0.25f),
            Box(body, -0.57f, 2.10f, -0.25f, 0.57f, 2.25f, 0.25f),
            Box(black, -0.20f, 2.12f, 0.25f, 0.20f, 2.22f, 0.26f),
        };
        Save("MetalDetector", "metal_detector", parts);
    }

    private static void BuildPoliceCar(Material white, Material blue, Material black, Material glass, Material light)
    {
        // A saloon 4.4 x 1.8 m along +Z, a blue stripe, a light bar on the roof and four wheels.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.90f, 0.30f, -2.20f, 0.90f, 0.95f, 2.20f),
            Box(white, -0.80f, 0.95f, -1.10f, 0.80f, 1.40f, 0.90f),
            Box(glass, -0.81f, 1.00f, -1.05f, 0.81f, 1.35f, 0.85f),
            Box(blue, -0.91f, 0.55f, -2.10f, 0.91f, 0.70f, 2.10f),
            Box(black, -0.55f, 1.40f, -0.20f, 0.55f, 1.44f, 0.20f),
            Box(light, -0.55f, 1.44f, -0.15f, -0.05f, 1.54f, 0.15f),
            Box(LowPolyBoxMesh.GetMat("LP_CV_LightRed", new Color(1.0f, 0.15f, 0.12f), 0f, 0.9f), 0.05f, 1.44f, -0.15f, 0.55f, 1.54f, 0.15f),
            Box(black, -0.85f, 0.32f, 2.19f, 0.85f, 0.50f, 2.22f),
        };
        foreach (var (x, z) in new[] { (-0.80f, -1.35f), (0.80f, -1.35f), (-0.80f, 1.35f), (0.80f, 1.35f) })
            parts.Add(Box(black, x - 0.12f, 0f, z - 0.34f, x + 0.12f, 0.62f, z + 0.34f));
        Save("PoliceCar", "police_car", parts);
    }

    private static void BuildFireEngine(Material red, Material steel, Material black, Material glass, Material stripe)
    {
        // An engine 8.0 x 2.5 m along +Z: a cab at the front, a body with lockers, a ladder on the roof.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(red, -1.25f, 0.45f, 1.6f, 1.25f, 3.00f, 4.0f),
            Box(glass, -1.20f, 2.00f, 3.98f, 1.20f, 2.85f, 4.02f),
            Box(glass, -1.26f, 2.00f, 2.40f, 1.26f, 2.85f, 3.60f),
            Box(red, -1.25f, 0.45f, -4.0f, 1.25f, 2.90f, 1.6f),
            Box(stripe, -1.27f, 1.10f, -4.0f, 1.27f, 1.25f, 4.0f),
            Box(steel, -1.27f, 0.50f, -3.8f, 1.27f, 0.95f, 1.4f),
            Box(steel, -0.60f, 2.95f, -3.9f, -0.50f, 3.10f, 1.2f),
            Box(steel, 0.50f, 2.95f, -3.9f, 0.60f, 3.10f, 1.2f),
            Box(LowPolyBoxMesh.GetMat("LP_CV_LightBlue", new Color(0.20f, 0.45f, 1.00f), 0f, 0.9f), -0.90f, 3.00f, 3.4f, 0.90f, 3.15f, 3.7f),
            Box(black, -1.20f, 0.30f, 3.98f, 1.20f, 0.55f, 4.05f),
        };
        for (var r = 0; r < 7; r++)
            parts.Add(Box(steel, -0.55f, 3.00f, -3.6f + r * 0.7f, 0.55f, 3.06f, -3.55f + r * 0.7f));
        foreach (var (x, z) in new[] { (-1.05f, 2.8f), (1.05f, 2.8f), (-1.05f, -1.4f), (1.05f, -1.4f), (-1.05f, -2.8f), (1.05f, -2.8f) })
            parts.Add(Box(black, x - 0.18f, 0f, z - 0.50f, x + 0.18f, 1.00f, z + 0.50f));
        Save("FireEngine", "fire_engine", parts);
    }

    private static void BuildHoseReel(Material red, Material hose)
    {
        // A wall-mounted reel 0.7 m across with the hose coiled on it, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part> { Box(red, -0.40f, 0f, 0f, 0.40f, 0.80f, 0.04f), Box(red, -0.05f, 0.35f, 0.04f, 0.05f, 0.45f, 0.20f) };
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(hose, new Vector3(0f, 0.40f, 0.14f), new Vector3(0.70f, 0.12f, 0.16f), Quaternion.Euler(0f, 0f, i * 30f)));
        Save("HoseReel", "hose_reel", parts);
    }

    private static void BuildHelmetRack(Material wood, Material helmet, Material red)
    {
        // A wall rail 1.2 m long with four helmets on pegs and a jacket below each, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part> { Box(wood, -0.60f, 1.00f, 0f, 0.60f, 1.10f, 0.04f) };
        for (var i = 0; i < 4; i++)
        {
            var x = -0.45f + i * 0.30f;
            parts.Add(Box(wood, x - 0.02f, 1.02f, 0.04f, x + 0.02f, 1.06f, 0.16f));
            parts.Add(Box(helmet, x - 0.12f, 1.06f, 0.04f, x + 0.12f, 1.22f, 0.28f));
            parts.Add(Box(red, x - 0.13f, 0.30f, 0.04f, x + 0.13f, 1.00f, 0.14f));
        }

        Save("HelmetRack", "helmet_rack", parts);
    }

    private static void BuildFirePole(Material steel)
    {
        // A pole 3.8 m high in a floor ring, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, steel, 0f, 0f, 0f, 0.08f, 3.80f);
        Round(parts, steel, 0f, 0f, 0f, 0.60f, 0.03f);
        Save("FirePole", "fire_pole", parts);
    }

    private static void BuildToolBench(Material wood, Material steel, Material black)
    {
        // A bench 1.6 x 0.6 m with a pegboard of tools behind, facing +Z.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.80f, 0.85f, -0.30f, 0.80f, 0.90f, 0.30f),
            Box(steel, -0.78f, 0f, -0.28f, -0.72f, 0.85f, -0.22f),
            Box(steel, 0.72f, 0f, -0.28f, 0.78f, 0.85f, -0.22f),
            Box(steel, -0.78f, 0f, 0.22f, -0.72f, 0.85f, 0.28f),
            Box(steel, 0.72f, 0f, 0.22f, 0.78f, 0.85f, 0.28f),
            Box(wood, -0.80f, 0.90f, -0.30f, 0.80f, 1.80f, -0.27f),
            Box(black, -0.60f, 1.20f, -0.27f, -0.45f, 1.55f, -0.24f),
            Box(black, -0.20f, 1.10f, -0.27f, -0.05f, 1.60f, -0.24f),
            Box(steel, 0.20f, 1.25f, -0.27f, 0.45f, 1.35f, -0.24f),
            Box(black, 0.30f, 0.90f, -0.05f, 0.60f, 1.05f, 0.15f),
        };
        Save("ToolBench", "tool_bench", parts);
    }

    private static void BuildAirTanks(Material steel, Material tank, Material black)
    {
        // A rack 1.2 m long holding six breathing air cylinders upright, facing +Z.
        var parts = new List<LowPolyBoxMesh.Part> { Box(steel, -0.60f, 0f, -0.15f, 0.60f, 0.05f, 0.15f), Box(steel, -0.60f, 0.70f, -0.17f, 0.60f, 0.74f, -0.13f) };
        for (var i = 0; i < 6; i++)
        {
            var x = -0.50f + i * 0.20f;
            Round(parts, tank, x, 0.05f, 0f, 0.17f, 0.70f);
            Round(parts, black, x, 0.70f, 0f, 0.06f, 0.80f);
        }

        Save("AirTanks", "air_tanks", parts);
    }

    private static void BuildBunkerBoots(Material black, Material tan, Material stripe)
    {
        // A pair of boots with the bunker trousers rolled down over them, ready to step into.
        var parts = new List<LowPolyBoxMesh.Part>();
        foreach (var x in new[] { -0.13f, 0.13f })
        {
            parts.Add(Box(black, x - 0.08f, 0f, -0.14f, x + 0.08f, 0.38f, 0.10f));
            parts.Add(Box(black, x - 0.08f, 0f, 0.10f, x + 0.08f, 0.10f, 0.18f));
            parts.Add(Box(tan, x - 0.12f, 0.20f, -0.16f, x + 0.12f, 0.48f, 0.12f));
            parts.Add(Box(stripe, x - 0.125f, 0.30f, -0.165f, x + 0.125f, 0.34f, 0.125f));
        }

        Save("BunkerBoots", "bunker_boots", parts);
    }

    private static void BuildFireGarageDoor(Material red, Material steel, Material glass)
    {
        // A red sectional door 3.6 x 3.6 m with a window row, flush on a facade (back at Z = 0).
        var parts = new List<LowPolyBoxMesh.Part> { Box(steel, -1.90f, 0f, 0f, 1.90f, 3.75f, 0.05f) };
        for (var p = 0; p < 6; p++)
        {
            var y = p * 0.60f;
            parts.Add(Box(red, -1.80f, y + 0.02f, 0.05f, 1.80f, y + 0.58f, 0.09f));
        }

        for (var w = 0; w < 4; w++)
            parts.Add(Box(glass, -1.60f + w * 0.85f, 2.50f, 0.09f, -1.00f + w * 0.85f, 2.80f, 0.095f));
        Save("FireGarageDoor", "fire_garage_door", parts);
    }

    private static void BuildExamCouch(Material steel, Material vinyl, Material paper)
    {
        // An examination couch 1.9 x 0.65 m, 0.75 m high, the head raised at +Z, a paper roll on it.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.30f, 0f, -0.90f, 0.30f, 0.60f, 0.90f),
            Box(vinyl, -0.33f, 0.60f, -0.95f, 0.33f, 0.72f, 0.55f),
            new(vinyl, new Vector3(0f, 0.80f, 0.74f), new Vector3(0.66f, 0.12f, 0.45f), Quaternion.Euler(-25f, 0f, 0f)),
            Box(paper, -0.25f, 0.72f, -0.80f, 0.25f, 0.725f, 0.50f),
        };
        Save("ExamCouch", "exam_couch", parts);
    }

    private static void BuildFoldingScreen(Material steel, Material cloth)
    {
        // A folding screen of three panels 0.6 m wide, 1.7 m high, zigzagged.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 0.56f;
            var yaw = i == 1 ? 0f : (i == 0 ? 20f : -20f);
            parts.Add(new LowPolyBoxMesh.Part(cloth, new Vector3(x, 0.95f, i == 1 ? 0f : 0.08f), new Vector3(0.56f, 1.40f, 0.02f), Quaternion.Euler(0f, yaw, 0f)));
            parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(x, 0.85f, i == 1 ? 0f : 0.08f), new Vector3(0.60f, 1.70f, 0.012f), Quaternion.Euler(0f, yaw, 0f)));
        }

        Save("FoldingScreen", "folding_screen", parts);
    }

    private static void BuildMedicalCart(Material steel, Material white, Material blue)
    {
        // A dressing cart 0.6 x 0.45 m with two trays and boxes of supplies, on castors.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.30f, 0.10f, -0.22f, 0.30f, 0.14f, 0.22f),
            Box(white, -0.30f, 0.86f, -0.22f, 0.30f, 0.90f, 0.22f),
            Box(blue, -0.20f, 0.90f, -0.12f, -0.02f, 0.98f, 0.10f),
            Box(white, 0.02f, 0.90f, -0.15f, 0.22f, 1.02f, 0.05f),
            Box(blue, -0.25f, 0.14f, -0.15f, 0.20f, 0.30f, 0.15f),
        };
        foreach (var (x, z) in new[] { (-0.27f, -0.19f), (0.27f, -0.19f), (-0.27f, 0.19f), (0.27f, 0.19f) })
        {
            parts.Add(Box(steel, x - 0.015f, 0.06f, z - 0.015f, x + 0.015f, 0.90f, z + 0.015f));
            parts.Add(Box(steel, x - 0.03f, 0f, z - 0.03f, x + 0.03f, 0.06f, z + 0.03f));
        }

        Save("MedicalCart", "medical_cart", parts);
    }

    private static void BuildWheelchair(Material steel, Material black)
    {
        // A wheelchair 0.65 x 0.9 m facing +Z: seat, back, two big wheels and footrests.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -0.23f, 0.48f, -0.20f, 0.23f, 0.52f, 0.22f),
            Box(black, -0.23f, 0.52f, -0.24f, 0.23f, 0.95f, -0.20f),
            Box(steel, -0.20f, 0.10f, 0.25f, 0.20f, 0.13f, 0.40f),
            Box(steel, -0.24f, 0.13f, 0.20f, -0.20f, 0.50f, 0.24f),
            Box(steel, 0.20f, 0.13f, 0.20f, 0.24f, 0.50f, 0.24f),
            Box(steel, -0.24f, 0.88f, -0.34f, 0.24f, 0.92f, -0.30f),
        };
        foreach (var x in new[] { -0.29f, 0.29f })
        {
            for (var i = 0; i < 3; i++)
                parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(x, 0.30f, -0.05f), new Vector3(0.03f, 0.60f, 0.16f), Quaternion.Euler(i * 60f, 0f, 0f)));
            parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(x, 0.30f, -0.05f), new Vector3(0.035f, 0.08f, 0.08f), Quaternion.identity));
        }

        Save("Wheelchair", "wheelchair", parts);
    }

    private static void BuildIvStand(Material steel, Material bag)
    {
        // A drip stand 1.9 m high on a five-spoke base, a bag hanging from its hook.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 5; i++)
            parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(0f, 0.04f, 0f), new Vector3(0.03f, 0.03f, 0.55f), Quaternion.Euler(0f, i * 36f, 0f)));
        Round(parts, steel, 0f, 0.05f, 0f, 0.025f, 1.90f);
        parts.Add(Box(steel, -0.18f, 1.86f, -0.01f, 0.18f, 1.88f, 0.01f));
        parts.Add(Box(bag, 0.08f, 1.55f, -0.04f, 0.18f, 1.84f, 0.04f));
        Save("IvStand", "iv_stand", parts);
    }

    private static void BuildMedicalScale(Material steel, Material white, Material black)
    {
        // A column scale: a platform 0.4 m square, a column 1.5 m high with its beam and height rod.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.20f, 0f, -0.20f, 0.20f, 0.08f, 0.20f),
            Box(black, -0.18f, 0.08f, -0.18f, 0.18f, 0.085f, 0.18f),
            Box(steel, -0.03f, 0.08f, -0.22f, 0.03f, 1.50f, -0.16f),
            Box(white, -0.16f, 1.20f, -0.22f, 0.16f, 1.30f, -0.16f),
            Box(steel, -0.01f, 1.50f, -0.20f, 0.01f, 2.00f, -0.18f),
            Box(steel, -0.01f, 1.95f, -0.20f, 0.01f, 1.97f, 0.05f),
        };
        Save("MedicalScale", "medical_scale", parts);
    }

    private static void BuildHospitalBed(Material steel, Material white, Material sheet)
    {
        // A bed 2.1 x 0.95 m along +Z: frame with rails and castors, a raised head end.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.47f, 0.35f, -1.05f, 0.47f, 0.45f, 1.05f),
            Box(sheet, -0.44f, 0.45f, -1.00f, 0.44f, 0.60f, 0.45f),
            new(sheet, new Vector3(0f, 0.72f, 0.72f), new Vector3(0.88f, 0.15f, 0.60f), Quaternion.Euler(-30f, 0f, 0f)),
            Box(white, -0.47f, 0.35f, 1.02f, 0.47f, 1.05f, 1.06f),
            Box(white, -0.47f, 0.35f, -1.06f, 0.47f, 0.85f, -1.02f),
            Box(steel, -0.49f, 0.60f, -0.40f, -0.47f, 0.78f, 0.60f),
            Box(steel, 0.47f, 0.60f, -0.40f, 0.49f, 0.78f, 0.60f),
        };
        foreach (var (x, z) in new[] { (-0.40f, -0.98f), (0.40f, -0.98f), (-0.40f, 0.98f), (0.40f, 0.98f) })
            parts.Add(Box(steel, x - 0.03f, 0f, z - 0.03f, x + 0.03f, 0.35f, z + 0.03f));
        Save("HospitalBed", "hospital_bed", parts);
    }

    private static void BuildAmbulance(Material white, Material yellow, Material green, Material black, Material glass, Material light)
    {
        // A van 6.0 x 2.1 m along +Z: a cab, a box body in a chequered livery, a light bar.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -1.05f, 0.40f, 1.60f, 1.05f, 1.80f, 3.00f),
            Box(glass, -1.00f, 1.20f, 2.98f, 1.00f, 1.75f, 3.02f),
            Box(yellow, -1.05f, 0.40f, -3.00f, 1.05f, 2.70f, 1.60f),
            Box(white, -1.07f, 2.00f, -3.00f, 1.07f, 2.30f, 1.60f),
            Box(black, -0.90f, 0.30f, 2.98f, 0.90f, 0.50f, 3.05f),
            Box(light, -0.80f, 2.70f, 1.20f, 0.80f, 2.82f, 1.50f),
        };
        for (var i = 0; i < 9; i++)
        {
            var z = -2.90f + i * 0.50f;
            if (i % 2 == 0)
            {
                parts.Add(Box(green, -1.07f, 1.00f, z, 1.07f, 1.35f, z + 0.50f));
            }
        }

        foreach (var (x, z) in new[] { (-0.90f, 2.20f), (0.90f, 2.20f), (-0.90f, -2.10f), (0.90f, -2.10f) })
            parts.Add(Box(black, x - 0.16f, 0f, z - 0.38f, x + 0.16f, 0.76f, z + 0.38f));
        Save("Ambulance", "ambulance", parts);
    }

    private static void BuildSchoolDesk(Material wood, Material steel, Material seat)
    {
        // A pupil's desk 0.65 x 0.45 m (top at 0.72 m) and its chair behind it, the pupil looking
        // along +Z: the desk at Z 0.05..0.50, the chair at Z -0.50..-0.08, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.325f, 0.70f, 0.05f, 0.325f, 0.73f, 0.50f),
            Box(steel, -0.30f, 0f, 0.08f, -0.27f, 0.70f, 0.11f),
            Box(steel, 0.27f, 0f, 0.08f, 0.30f, 0.70f, 0.11f),
            Box(steel, -0.30f, 0f, 0.44f, -0.27f, 0.70f, 0.47f),
            Box(steel, 0.27f, 0f, 0.44f, 0.30f, 0.70f, 0.47f),
            Box(steel, -0.30f, 0.55f, 0.44f, 0.30f, 0.58f, 0.47f),
            Box(wood, -0.20f, 0.42f, -0.45f, 0.20f, 0.45f, -0.08f),
            Box(seat, -0.20f, 0.55f, -0.50f, 0.20f, 0.82f, -0.47f),
            Box(steel, -0.18f, 0f, -0.44f, -0.16f, 0.42f, -0.42f),
            Box(steel, 0.16f, 0f, -0.44f, 0.18f, 0.42f, -0.42f),
            Box(steel, -0.18f, 0f, -0.11f, -0.16f, 0.42f, -0.09f),
            Box(steel, 0.16f, 0f, -0.11f, 0.18f, 0.42f, -0.09f),
            Box(steel, -0.18f, 0.42f, -0.50f, -0.16f, 0.82f, -0.47f),
            Box(steel, 0.16f, 0.42f, -0.50f, 0.18f, 0.82f, -0.47f),
        };
        Save("SchoolDesk", "school_desk", parts);
    }

    private static void BuildBlackboard(Material wood, Material slate, Material chalk)
    {
        // A green board 2.4 x 1.2 m in a wooden frame with a chalk tray, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -1.25f, 0f, 0f, 1.25f, 1.26f, 0.03f),
            Box(slate, -1.20f, 0.05f, 0.03f, 1.20f, 1.21f, 0.04f),
            Box(wood, -1.25f, 0f, 0.03f, 1.25f, 0.03f, 0.09f),
            Box(chalk, -1.10f, 0.03f, 0.04f, -1.02f, 0.05f, 0.06f),
            Box(chalk, -0.90f, 0.03f, 0.05f, -0.84f, 0.05f, 0.07f),
            Box(chalk, -0.80f, 0.55f, 0.04f, 0.40f, 0.57f, 0.045f),
            Box(chalk, -0.80f, 0.80f, 0.04f, 0.10f, 0.82f, 0.045f),
            Box(chalk, -0.80f, 0.30f, 0.04f, 0.70f, 0.32f, 0.045f),
        };
        Save("Blackboard", "blackboard", parts);
    }

    private static void BuildLabBench(Material top, Material body, Material steel)
    {
        // A laboratory bench 1.8 x 0.75 m (top at 0.90 m) on a cupboard base, a sink and a swan-neck
        // tap at one end and a gas tap at the other, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.88f, 0f, -0.35f, 0.88f, 0.86f, 0.35f),
            Box(top, -0.90f, 0.86f, -0.375f, 0.90f, 0.90f, 0.375f),
            Box(steel, 0.45f, 0.88f, -0.18f, 0.80f, 0.905f, 0.18f),
            Box(steel, 0.60f, 0.90f, 0.22f, 0.64f, 1.20f, 0.26f),
            Box(steel, 0.60f, 1.16f, 0.06f, 0.64f, 1.20f, 0.26f),
            Box(steel, -0.70f, 0.90f, 0.28f, -0.66f, 1.00f, 0.32f),
            Box(steel, -0.30f, 0.90f, 0.28f, -0.26f, 1.00f, 0.32f),
        };
        Save("LabBench", "lab_bench", parts);
    }

    private static void BuildBasketballHoop(Material board, Material rim, Material steel)
    {
        // A wall-mounted backboard 1.2 x 0.8 m on a bracket with the rim and a net, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.40f, 0.10f, 0f, 0.40f, 0.70f, 0.30f),
            Box(board, -0.60f, 0f, 0.30f, 0.60f, 0.80f, 0.34f),
            Box(rim, -0.25f, 0.18f, 0.34f, 0.25f, 0.42f, 0.345f),
        };
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(rim, new Vector3(0f, 0.15f, 0.57f), new Vector3(0.46f, 0.02f, 0.46f * Mathf.Tan(15f * Mathf.Deg2Rad)), Quaternion.Euler(0f, i * 30f, 0f)));
        Round(parts, board, 0f, -0.25f, 0.57f, 0.30f, 0.14f);
        Save("BasketballHoop", "basketball_hoop", parts);
    }

    private static void BuildGymMat(Material mat)
    {
        // A folded-edge gym mat 2.0 x 1.0 m, 0.08 m thick, centred.
        var parts = new List<LowPolyBoxMesh.Part> { Box(mat, -0.50f, 0f, -1.00f, 0.50f, 0.08f, 1.00f) };
        Save("GymMat", "gym_mat", parts);
    }

    private static void BuildGymBench(Material wood, Material steel)
    {
        // A gym bench 2.5 x 0.30 m, seat at 0.33 m, on two wooden legs with steel caps, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -1.25f, 0.29f, -0.15f, 1.25f, 0.33f, 0.15f),
            Box(wood, -1.05f, 0.02f, -0.15f, -0.95f, 0.29f, 0.15f),
            Box(wood, 0.95f, 0.02f, -0.15f, 1.05f, 0.29f, 0.15f),
            Box(steel, -1.06f, 0f, -0.16f, -0.94f, 0.02f, 0.16f),
            Box(steel, 0.94f, 0f, -0.16f, 1.06f, 0.02f, 0.16f),
            Box(wood, -1.20f, 0.20f, -0.03f, 1.20f, 0.24f, 0.03f),
        };
        Save("GymBench", "gym_bench", parts);
    }

    private static void BuildCheckoutCounter(Material body, Material black, Material steel, Material belt)
    {
        // A checkout 1.1 x 2.5 m: a cabinet with the belt along Z, the scanner and till at the +Z
        // end, the bagging well beyond, the cashier's stool on the +X side; centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.45f, 0f, -1.25f, 0.30f, 0.85f, 1.25f),
            Box(belt, -0.40f, 0.85f, -1.20f, 0.25f, 0.88f, 0.40f),
            Box(steel, -0.42f, 0.85f, 0.45f, 0.27f, 0.90f, 0.80f),
            Box(black, 0.05f, 0.88f, 0.50f, 0.25f, 1.20f, 0.70f),
            Box(black, 0.05f, 1.20f, 0.45f, 0.28f, 1.38f, 0.75f),
            Box(steel, -0.45f, 0.70f, 0.85f, 0.30f, 0.74f, 1.25f),
            Box(steel, -0.45f, 0.74f, 1.22f, 0.30f, 0.95f, 1.25f),
            Box(steel, 0.35f, 0f, 0.35f, 0.39f, 0.60f, 0.39f),
            Box(black, 0.30f, 0.60f, 0.30f, 0.55f, 0.66f, 0.55f),
            Box(steel, -0.05f, 0.85f, -1.20f, -0.02f, 1.30f, -1.17f),
            Box(black, -0.20f, 1.30f, -1.20f, 0.10f, 1.45f, -1.17f),
        };
        Save("CheckoutCounter", "checkout_counter", parts);
    }

    private static void BuildFridgeCase(Material body, Material glass, Material steel, Material[] goods)
    {
        // An upright fridge case 2.5 x 0.9 m, 2.0 m high, four glass doors with goods on four
        // shelves behind them; back at -Z, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -1.25f, 0f, -0.45f, 1.25f, 0.20f, 0.45f),
            Box(body, -1.25f, 1.85f, -0.45f, 1.25f, 2.00f, 0.45f),
            Box(body, -1.25f, 0.20f, -0.45f, 1.25f, 1.85f, -0.40f),
            Box(body, -1.25f, 0.20f, -0.45f, -1.20f, 1.85f, 0.45f),
            Box(body, 1.20f, 0.20f, -0.45f, 1.25f, 1.85f, 0.45f),
            Box(glass, -1.20f, 0.20f, 0.40f, 1.20f, 1.85f, 0.42f),
        };
        for (var d = 1; d < 4; d++)
            parts.Add(Box(steel, -1.20f + d * 0.60f - 0.015f, 0.20f, 0.40f, -1.20f + d * 0.60f + 0.015f, 1.85f, 0.43f));
        for (var shelf = 0; shelf < 4; shelf++)
        {
            var y = 0.25f + shelf * 0.40f;
            parts.Add(Box(steel, -1.20f, y, -0.40f, 1.20f, y + 0.02f, 0.35f));
            for (var i = 0; i < 6; i++)
            {
                var x = -1.10f + i * 0.38f;
                parts.Add(Box(goods[(shelf + i) % goods.Length], x, y + 0.02f, -0.30f, x + 0.30f, y + 0.28f, 0.30f));
            }
        }

        Save("FridgeCase", "fridge_case", parts);
    }

    private static void BuildTrolleyRow(Material steel, Material handle)
    {
        // Three shopping trolleys nested in a row 1.9 m long along Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 3; i++)
        {
            var z = -0.95f + i * 0.35f;
            parts.Add(Box(steel, -0.28f, 0.45f, z, 0.28f, 0.48f, z + 0.90f));
            parts.Add(Box(steel, -0.28f, 0.48f, z, -0.26f, 0.95f, z + 0.90f));
            parts.Add(Box(steel, 0.26f, 0.48f, z, 0.28f, 0.95f, z + 0.90f));
            parts.Add(Box(steel, -0.28f, 0.48f, z + 0.88f, 0.28f, 0.95f, z + 0.90f));
            parts.Add(Box(handle, -0.30f, 1.00f, z - 0.05f, 0.30f, 1.04f, z - 0.01f));
            parts.Add(Box(steel, -0.25f, 0.05f, z, -0.22f, 0.45f, z + 0.03f));
            parts.Add(Box(steel, 0.22f, 0.05f, z, 0.25f, 0.45f, z + 0.03f));
        }

        parts.Add(Box(steel, -0.26f, 0f, -0.95f, 0.26f, 0.05f, 0.95f));
        Save("TrolleyRow", "trolley_row", parts);
    }

    private static void BuildBasketStack(Material red, Material black)
    {
        // Six red hand baskets stacked on a low stand, centred.
        var parts = new List<LowPolyBoxMesh.Part> { Box(black, -0.25f, 0f, -0.18f, 0.25f, 0.06f, 0.18f) };
        for (var i = 0; i < 6; i++)
        {
            var y = 0.06f + i * 0.08f;
            parts.Add(Box(red, -0.23f, y, -0.16f, 0.23f, y + 0.07f, 0.16f));
        }

        parts.Add(Box(black, -0.20f, 0.53f, -0.01f, 0.20f, 0.58f, 0.01f));
        Save("BasketStack", "basket_stack", parts);
    }

    private static void BuildPetrolCanopy(Material white, Material green, Material yellow, Material steel)
    {
        // A forecourt canopy 8.0 m wide and 4.5 m deep out from a facade, its back at Z = 0 (on the
        // wall), the deck 4.5 m up with a green and yellow fascia, two columns at the outer edge and
        // light panels under the deck.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -4.00f, 4.50f, 0f, 4.00f, 4.80f, 4.50f),
            Box(green, -4.05f, 4.40f, 4.50f, 4.05f, 5.00f, 4.55f),
            Box(green, -4.05f, 4.40f, 0f, -4.00f, 5.00f, 4.55f),
            Box(green, 4.00f, 4.40f, 0f, 4.05f, 5.00f, 4.55f),
            Box(yellow, -4.06f, 4.40f, 4.55f, 4.06f, 4.52f, 4.57f),
            Box(steel, -3.20f, 0f, 3.70f, -2.90f, 4.50f, 4.00f),
            Box(steel, 2.90f, 0f, 3.70f, 3.20f, 4.50f, 4.00f),
            Box(steel, -3.30f, 0f, 3.60f, -2.80f, 0.30f, 4.10f),
            Box(steel, 2.80f, 0f, 3.60f, 3.30f, 0.30f, 4.10f),
        };
        for (var i = 0; i < 3; i++)
            parts.Add(Box(white, -2.80f + i * 2.30f, 4.48f, 1.50f, -1.60f + i * 2.30f, 4.50f, 3.00f));
        Save("PetrolCanopy", "petrol_canopy", parts);
    }

    private static void BuildFuelPump(Material white, Material green, Material black, Material screen)
    {
        // A fuel dispenser 0.9 x 0.5 m, 1.9 m high, on a kerbed island 1.6 x 0.9 m, a display and
        // nozzles on both long faces (+Z and -Z), centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.80f, 0f, -0.45f, 0.80f, 0.15f, 0.45f),
            Box(white, -0.45f, 0.15f, -0.25f, 0.45f, 1.85f, 0.25f),
            Box(green, -0.47f, 1.55f, -0.27f, 0.47f, 1.95f, 0.27f),
            Box(screen, -0.30f, 1.15f, 0.25f, 0.10f, 1.45f, 0.26f),
            Box(screen, -0.10f, 1.15f, -0.26f, 0.30f, 1.45f, -0.25f),
        };
        for (var i = 0; i < 3; i++)
        {
            var x = -0.30f + i * 0.30f;
            parts.Add(Box(black, x - 0.04f, 0.75f, 0.25f, x + 0.04f, 0.95f, 0.33f));
            parts.Add(Box(black, x - 0.04f, 0.75f, -0.33f, x + 0.04f, 0.95f, -0.25f));
        }

        Save("FuelPump", "fuel_pump", parts);
    }

    private static void BuildPriceTotem(Material green, Material yellow, Material white, Material black)
    {
        // A price sign 1.2 x 0.4 m, 4.0 m high: the brand panel on top and four price rows on both
        // faces (+Z and -Z), centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(green, -0.60f, 0f, -0.20f, 0.60f, 4.00f, 0.20f),
            Box(yellow, -0.55f, 3.20f, -0.21f, 0.55f, 3.90f, 0.21f),
        };
        for (var row = 0; row < 4; row++)
        {
            var y = 1.20f + row * 0.45f;
            parts.Add(Box(white, -0.50f, y, -0.21f, 0.10f, y + 0.35f, 0.21f));
            parts.Add(Box(black, 0.15f, y, -0.21f, 0.50f, y + 0.35f, 0.21f));
        }

        Save("PriceTotem", "price_totem", parts);
    }

    private static void BuildAirPump(Material red, Material black, Material steel)
    {
        // An air and water post 0.4 x 0.3 m, 1.3 m high, a hose coiled on its side, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.25f, 0f, -0.20f, 0.25f, 0.05f, 0.20f),
            Box(red, -0.20f, 0.05f, -0.15f, 0.20f, 1.30f, 0.15f),
            Box(black, -0.12f, 0.95f, 0.15f, 0.12f, 1.15f, 0.16f),
        };
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(black, new Vector3(0.22f, 0.60f, 0f), new Vector3(0.04f, 0.30f, 0.30f * Mathf.Tan(15f * Mathf.Deg2Rad)), Quaternion.Euler(i * 30f, 0f, 0f)));
        Save("AirPump", "air_pump", parts);
    }

    private static void BuildIceChest(Material white, Material blue, Material glass)
    {
        // An ice merchandiser 1.2 x 0.7 m, 1.0 m high with a sliding glass lid, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.60f, 0f, -0.35f, 0.60f, 0.92f, 0.35f),
            Box(blue, -0.61f, 0.30f, -0.36f, 0.61f, 0.70f, 0.36f),
            Box(glass, -0.56f, 0.92f, -0.31f, 0.56f, 0.95f, 0.31f),
            Box(white, -0.60f, 0.92f, -0.35f, 0.60f, 1.00f, -0.31f),
            Box(white, -0.60f, 0.92f, 0.31f, 0.60f, 1.00f, 0.35f),
        };
        Save("IceChest", "ice_chest", parts);
    }

    private static void BuildOilRack(Material steel, Material[] cans)
    {
        // A rack of motor oil and screen wash 1.0 x 0.4 m, 1.5 m high, three shelves, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.50f, 0f, -0.20f, -0.47f, 1.50f, 0.20f),
            Box(steel, 0.47f, 0f, -0.20f, 0.50f, 1.50f, 0.20f),
            Box(steel, -0.50f, 0f, -0.20f, 0.50f, 1.50f, -0.18f),
        };
        for (var shelf = 0; shelf < 3; shelf++)
        {
            var y = 0.15f + shelf * 0.48f;
            parts.Add(Box(steel, -0.47f, y, -0.18f, 0.47f, y + 0.02f, 0.20f));
            for (var i = 0; i < 5; i++)
            {
                var x = -0.42f + i * 0.17f;
                parts.Add(Box(cans[(shelf + i) % cans.Length], x, y + 0.02f, -0.10f, x + 0.13f, y + 0.28f, 0.12f));
            }
        }

        Save("OilRack", "oil_rack", parts);
    }

    private static void BuildNewspaperStand(Material steel, Material[] papers)
    {
        // A tiered newspaper stand 0.8 x 0.5 m, 1.2 m high, three sloping tiers, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.40f, 0f, -0.25f, -0.37f, 1.20f, 0.25f),
            Box(steel, 0.37f, 0f, -0.25f, 0.40f, 1.20f, 0.25f),
        };
        for (var tier = 0; tier < 3; tier++)
        {
            var y = 0.45f + tier * 0.30f;
            var z = 0.15f - tier * 0.15f;
            parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(0f, y, z), new Vector3(0.74f, 0.02f, 0.22f), Quaternion.Euler(-25f, 0f, 0f)));
            for (var i = 0; i < 3; i++)
                parts.Add(new LowPolyBoxMesh.Part(papers[(tier + i) % papers.Length], new Vector3(-0.24f + i * 0.24f, y + 0.02f, z), new Vector3(0.22f, 0.01f, 0.20f), Quaternion.Euler(-25f, 0f, 0f)));
        }

        Save("NewspaperStand", "newspaper_stand", parts);
    }

    private static void BuildFumeHood(Material body, Material glass, Material steel, Material top)
    {
        // A fume cupboard 1.5 x 0.8 m, 2.3 m high: a cabinet base, a worktop, the glass sash and the
        // duct box above, back at -Z, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.75f, 0f, -0.40f, 0.75f, 0.86f, 0.40f),
            Box(top, -0.75f, 0.86f, -0.40f, 0.75f, 0.90f, 0.40f),
            Box(body, -0.75f, 0.90f, -0.40f, -0.70f, 2.30f, 0.40f),
            Box(body, 0.70f, 0.90f, -0.40f, 0.75f, 2.30f, 0.40f),
            Box(body, -0.75f, 0.90f, -0.40f, 0.75f, 2.30f, -0.36f),
            Box(body, -0.75f, 1.90f, -0.40f, 0.75f, 2.30f, 0.40f),
            Box(glass, -0.70f, 1.15f, 0.36f, 0.70f, 1.90f, 0.38f),
            Box(steel, -0.70f, 1.13f, 0.36f, 0.70f, 1.16f, 0.40f),
            Box(steel, -0.20f, 2.30f, -0.25f, 0.20f, 2.60f, 0.15f),
        };
        Save("FumeHood", "fume_hood", parts);
    }

    private static void BuildPeriodicTable(Material white, Material[] cells)
    {
        // A periodic table poster 1.4 x 0.8 m, back at Z = 0: 18 x 7 coloured cells with the two
        // gaps of the real table.
        var parts = new List<LowPolyBoxMesh.Part> { Box(white, -0.70f, 0f, 0f, 0.70f, 0.80f, 0.01f) };
        for (var row = 0; row < 7; row++)
        {
            for (var col = 0; col < 18; col++)
            {
                if ((row == 0 && col > 0 && col < 17) || (row < 3 && col > 1 && col < 12))
                    continue;
                var x = -0.66f + col * 0.073f;
                var y = 0.70f - row * 0.095f;
                var cell = cells[col < 2 ? 0 : col < 12 ? 1 : 2];
                parts.Add(Box(cell, x, y - 0.08f, 0.01f, x + 0.065f, y, 0.015f));
            }
        }

        Save("PeriodicTable", "periodic_table", parts);
    }

    private static void BuildSkeletonModel(Material bone, Material steel)
    {
        // An anatomy skeleton 1.7 m tall hung from a stand on a five-legged base, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.02f, 0.05f, -0.15f, 0.02f, 1.85f, -0.11f),
            Box(steel, -0.02f, 1.81f, -0.15f, 0.02f, 1.85f, 0f),
            Box(bone, -0.10f, 1.55f, -0.08f, 0.10f, 1.78f, 0.10f),
            Box(bone, -0.03f, 1.45f, -0.03f, 0.03f, 1.55f, 0.03f),
            Box(bone, -0.18f, 1.10f, -0.08f, 0.18f, 1.45f, 0.08f),
            Box(bone, -0.03f, 0.95f, -0.03f, 0.03f, 1.10f, 0.03f),
            Box(bone, -0.16f, 0.85f, -0.06f, 0.16f, 0.98f, 0.06f),
            Box(bone, -0.26f, 0.80f, -0.03f, -0.21f, 1.42f, 0.03f),
            Box(bone, 0.21f, 0.80f, -0.03f, 0.26f, 1.42f, 0.03f),
            Box(bone, -0.13f, 0.10f, -0.03f, -0.07f, 0.85f, 0.03f),
            Box(bone, 0.07f, 0.10f, -0.03f, 0.13f, 0.85f, 0.03f),
        };
        for (var i = 0; i < 5; i++)
            parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(0f, 0.03f, -0.13f), new Vector3(0.04f, 0.04f, 0.50f), Quaternion.Euler(0f, i * 72f, 0f)));
        Save("SkeletonModel", "skeleton_model", parts);
    }

    private static void BuildBookcase(Material wood, Material[] books)
    {
        // A bookcase 1.0 x 0.35 m, 2.0 m high, five shelves of books, back at -Z, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.50f, 0f, -0.175f, -0.48f, 2.00f, 0.175f),
            Box(wood, 0.48f, 0f, -0.175f, 0.50f, 2.00f, 0.175f),
            Box(wood, -0.50f, 0f, -0.175f, 0.50f, 2.00f, -0.155f),
            Box(wood, -0.50f, 1.98f, -0.175f, 0.50f, 2.00f, 0.175f),
        };
        for (var shelf = 0; shelf < 5; shelf++)
        {
            var y = 0.04f + shelf * 0.39f;
            parts.Add(Box(wood, -0.48f, y - 0.02f, -0.155f, 0.48f, y, 0.175f));
            var x = -0.46f;
            for (var i = 0; x < 0.40f; i++)
            {
                var width = 0.04f + ((shelf * 7 + i * 3) % 4) * 0.01f;
                var height = 0.24f + ((shelf + i * 5) % 3) * 0.03f;
                parts.Add(Box(books[(shelf + i) % books.Length], x, y, -0.13f, x + width, y + height, 0.12f));
                x += width + 0.005f;
            }
        }

        Save("Bookcase", "bookcase", parts);
    }

    private static void BuildBankCounter(Material body, Material top, Material glass, Material black)
    {
        // A teller's counter 2.4 x 1.6 m: the teller's floor and chair at the back (-Z, against the
        // wall), the counter 0.7 m deep at the front (+Z) with a glass screen and a monitor per
        // window (two windows), centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -1.20f, 0f, 0.10f, 1.20f, 1.05f, 0.80f),
            Box(top, -1.22f, 1.05f, 0.08f, 1.22f, 1.10f, 0.82f),
            Box(glass, -1.20f, 1.10f, 0.40f, 1.20f, 1.90f, 0.42f),
            Box(body, -0.02f, 1.10f, 0.10f, 0.02f, 1.90f, 0.80f),
        };
        foreach (var x in new[] { -0.60f, 0.60f })
        {
            parts.Add(Box(black, x - 0.22f, 1.10f, 0.15f, x + 0.22f, 1.45f, 0.20f));
            parts.Add(Box(black, x - 0.22f, 0.45f, -0.55f, x + 0.22f, 0.52f, -0.15f));
            parts.Add(Box(black, x - 0.22f, 0.52f, -0.58f, x + 0.22f, 1.00f, -0.53f));
            parts.Add(Box(black, x - 0.03f, 0f, -0.38f, x + 0.03f, 0.45f, -0.32f));
        }

        Save("BankCounter", "bank_counter", parts);
    }

    private static void BuildAtm(Material steel, Material navy, Material black, Material screen)
    {
        // A free-standing cash machine 0.7 x 0.6 m, 1.8 m high against a wall, the fascia facing
        // +Z: screen, keypad, card slot and the cash drawer, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(navy, -0.35f, 0f, -0.30f, 0.35f, 1.80f, 0.30f),
            Box(steel, -0.30f, 0.95f, 0.30f, 0.30f, 1.10f, 0.40f),
            Box(screen, -0.22f, 1.20f, 0.30f, 0.22f, 1.50f, 0.32f),
            Box(black, -0.12f, 0.98f, 0.36f, 0.12f, 1.06f, 0.41f),
            Box(black, -0.15f, 0.80f, 0.30f, 0.15f, 0.86f, 0.33f),
            Box(steel, -0.35f, 1.62f, 0.30f, 0.35f, 1.80f, 0.34f),
        };
        Save("Atm", "atm", parts);
    }

    private static void BuildQueueStanchions(Material steel, Material belt)
    {
        // Four posts 2.4 m in a row along X with retractable belts between them: the queue's lane, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 4; i++)
        {
            var x = -1.20f + i * 0.80f;
            Round(parts, steel, x, 0f, 0f, 0.30f, 0.02f);
            Round(parts, steel, x, 0f, 0f, 0.06f, 0.95f);
            if (i < 3)
                parts.Add(Box(belt, x, 0.85f, -0.01f, x + 0.80f, 0.90f, 0.01f));
        }

        Save("QueueStanchions", "queue_stanchions", parts);
    }

    private static void BuildBankSafe(Material body, Material steel)
    {
        // A strongbox 0.9 x 0.8 m, 1.4 m high, its door with a wheel and hinges facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(body, -0.45f, 0f, -0.40f, 0.45f, 1.40f, 0.40f),
            Box(body, -0.40f, 0.06f, 0.40f, 0.40f, 1.34f, 0.43f),
            Box(steel, -0.42f, 0.25f, 0.40f, -0.36f, 0.40f, 0.45f),
            Box(steel, -0.42f, 1.00f, 0.40f, -0.36f, 1.15f, 0.45f),
        };
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(0.10f, 0.75f, 0.46f), new Vector3(0.30f, 0.03f, 0.03f), Quaternion.Euler(0f, 0f, i * 30f)));
        Save("BankSafe", "bank_safe", parts);
    }

    private static void BuildPharmacyCounter(Material white, Material green, Material black)
    {
        // A dispensing counter 2.6 x 1.5 m: the counter 0.6 m deep at the front (+Z) with a green
        // band and a till, the pharmacist's floor behind it (-Z, against the wall), centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -1.30f, 0f, 0.15f, 1.30f, 1.00f, 0.75f),
            Box(white, -1.32f, 1.00f, 0.13f, 1.32f, 1.05f, 0.77f),
            Box(green, -1.30f, 0.80f, 0.75f, 1.30f, 0.90f, 0.76f),
            Box(black, 0.60f, 1.05f, 0.25f, 0.95f, 1.15f, 0.55f),
            Box(black, 0.65f, 1.15f, 0.25f, 0.90f, 1.35f, 0.30f),
        };
        Save("PharmacyCounter", "pharmacy_counter", parts);
    }

    private static void BuildPharmacyShelf(Material white, Material[] boxes)
    {
        // A wall unit of medicine drawers and shelves 2.0 x 0.4 m, 2.2 m high, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -1.00f, 0f, -0.20f, 1.00f, 0.90f, 0.20f),
            Box(white, -1.00f, 0.90f, -0.20f, 1.00f, 2.20f, -0.17f),
            Box(white, -1.00f, 0.90f, -0.20f, -0.97f, 2.20f, 0.10f),
            Box(white, 0.97f, 0.90f, -0.20f, 1.00f, 2.20f, 0.10f),
        };
        for (var d = 0; d < 4; d++)
            parts.Add(Box(boxes[3], -0.95f + d * 0.48f, 0.42f, 0.20f, -0.55f + d * 0.48f, 0.46f, 0.21f));
        for (var shelf = 0; shelf < 4; shelf++)
        {
            var y = 0.92f + shelf * 0.32f;
            parts.Add(Box(white, -0.97f, y, -0.17f, 0.97f, y + 0.02f, 0.10f));
            for (var i = 0; i < 10; i++)
            {
                var x = -0.94f + i * 0.19f;
                parts.Add(Box(boxes[(shelf + i) % 3], x, y + 0.02f, -0.12f, x + 0.15f, y + 0.20f, 0.06f));
            }
        }

        Save("PharmacyShelf", "pharmacy_shelf", parts);
    }

    private static void BuildPharmacyCross(Material green, Material black)
    {
        // The green cross sign 0.7 m across on a wall, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -0.08f, 0.30f, 0f, 0.08f, 0.40f, 0.04f),
            Box(green, -0.35f, 0.24f, 0.04f, 0.35f, 0.46f, 0.12f),
            Box(green, -0.11f, 0f, 0.04f, 0.11f, 0.70f, 0.12f),
        };
        Save("PharmacyCross", "pharmacy_cross", parts);
    }

    private static void BuildBloodPressureKiosk(Material white, Material green, Material black)
    {
        // A blood pressure kiosk 0.8 x 0.9 m: a seat, the arm cuff unit and its screen, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(white, -0.40f, 0f, -0.45f, 0.40f, 1.30f, -0.25f),
            Box(green, -0.40f, 1.10f, -0.25f, 0.40f, 1.30f, -0.20f),
            Box(black, -0.20f, 0.85f, -0.25f, 0.20f, 1.05f, -0.22f),
            Box(white, -0.30f, 0.40f, -0.25f, 0.30f, 0.46f, 0.30f),
            Box(white, -0.25f, 0f, 0.05f, 0.25f, 0.40f, 0.25f),
            Box(white, 0.30f, 0.60f, -0.25f, 0.40f, 0.75f, 0.10f),
        };
        Save("BloodPressureKiosk", "blood_pressure_kiosk", parts);
    }

    private static void BuildLectureSeatRow(Material wood, Material steel, Material seat)
    {
        // A lecture hall row 3.6 m wide: six upholstered seats (0.6 m apart) on a steel rail and
        // the writing strip in front of them, the audience looking along +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -1.80f, 0.72f, 0.15f, 1.80f, 0.75f, 0.47f),
            Box(wood, -1.80f, 0f, 0.42f, 1.80f, 0.72f, 0.45f),
            Box(steel, -1.80f, 0f, -0.30f, 1.80f, 0.06f, -0.20f),
        };
        for (var i = 0; i < 6; i++)
        {
            var x = -1.50f + i * 0.60f;
            parts.Add(Box(seat, x - 0.25f, 0.42f, -0.40f, x + 0.25f, 0.50f, 0.02f));
            parts.Add(Box(seat, x - 0.25f, 0.50f, -0.47f, x + 0.25f, 0.95f, -0.40f));
            parts.Add(Box(steel, x + 0.27f, 0.06f, -0.45f, x + 0.30f, 0.65f, 0f));
        }

        Save("LectureSeatRow", "lecture_seat_row", parts);
    }

    private static void BuildMallBench(Material wood, Material steel)
    {
        // A backless mall bench 1.8 x 0.5 m, seat at 0.45 m, of wooden slats on two steel frames, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 5; i++)
        {
            var z = -0.24f + i * 0.10f;
            parts.Add(Box(wood, -0.90f, 0.40f, z, 0.90f, 0.45f, z + 0.08f));
        }

        foreach (var x in new[] { -0.70f, 0.70f })
        {
            parts.Add(Box(steel, x - 0.03f, 0f, -0.22f, x + 0.03f, 0.40f, -0.16f));
            parts.Add(Box(steel, x - 0.03f, 0f, 0.16f, x + 0.03f, 0.40f, 0.22f));
            parts.Add(Box(steel, x - 0.03f, 0.36f, -0.22f, x + 0.03f, 0.40f, 0.22f));
        }

        Save("MallBench", "mall_bench", parts);
    }

    private static void BuildMallKiosk(Material white, Material teal, Material glass, Material goods)
    {
        // A kiosk cart 2.0 x 1.2 m: a counter with a glass display, four posts and a teal canopy at
        // 2.5 m, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(teal, -1.00f, 0f, -0.60f, 1.00f, 0.95f, 0.60f),
            Box(white, -1.02f, 0.95f, -0.62f, 1.02f, 1.00f, 0.62f),
            Box(glass, -0.90f, 1.00f, -0.50f, 0.90f, 1.30f, 0.50f),
            Box(goods, -0.80f, 1.00f, -0.40f, 0.80f, 1.10f, 0.40f),
            Box(teal, -1.10f, 2.40f, -0.70f, 1.10f, 2.50f, 0.70f),
        };
        foreach (var x in new[] { -0.95f, 0.95f })
        {
            foreach (var z in new[] { -0.55f, 0.55f })
                parts.Add(Box(white, x - 0.03f, 1.00f, z - 0.03f, x + 0.03f, 2.40f, z + 0.03f));
        }

        Save("MallKiosk", "mall_kiosk", parts);
    }

    private static void BuildDirectoryBoard(Material steel, Material screen, Material[] zones)
    {
        // A mall directory totem 1.0 x 0.3 m, 2.0 m high, a floor plan with coloured zones on both
        // faces (+Z and -Z), centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -0.50f, 0f, -0.15f, 0.50f, 2.00f, 0.15f),
            Box(screen, -0.42f, 0.70f, -0.16f, 0.42f, 1.85f, 0.16f),
        };
        for (var i = 0; i < 6; i++)
        {
            var x = -0.38f + (i % 3) * 0.26f;
            var y = 0.80f + (i / 3) * 0.50f;
            parts.Add(Box(zones[i % zones.Length], x, y, -0.17f, x + 0.22f, y + 0.40f, 0.17f));
        }

        Save("DirectoryBoard", "directory_board", parts);
    }

    private static void BuildSculpturePedestal(Material stone, Material bronze, Material brass)
    {
        // A stone plinth 0.6 x 0.6 m, 1.0 m high, with an abstract bronze sculpture on it (three
        // turned blocks and a ring) and a brass plaque on its front (+Z), centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(stone, -0.32f, 0f, -0.32f, 0.32f, 0.05f, 0.32f),
            Box(stone, -0.28f, 0.05f, -0.28f, 0.28f, 1.00f, 0.28f),
            Box(brass, -0.10f, 0.70f, 0.28f, 0.10f, 0.78f, 0.29f),
            new LowPolyBoxMesh.Part(bronze, new Vector3(0f, 1.15f, 0f), new Vector3(0.22f, 0.30f, 0.18f), Quaternion.Euler(0f, 25f, 10f)),
            new LowPolyBoxMesh.Part(bronze, new Vector3(0.05f, 1.40f, 0f), new Vector3(0.14f, 0.28f, 0.12f), Quaternion.Euler(0f, -30f, -20f)),
            new LowPolyBoxMesh.Part(bronze, new Vector3(-0.04f, 1.60f, 0.02f), new Vector3(0.10f, 0.16f, 0.10f), Quaternion.Euler(15f, 45f, 0f)),
        };
        for (var i = 0; i < 8; i++)
            parts.Add(new LowPolyBoxMesh.Part(bronze, new Vector3(0f, 1.62f, 0f) + Quaternion.Euler(0f, 0f, i * 45f) * new Vector3(0.12f, 0f, 0f),
                new Vector3(0.03f, 0.10f, 0.03f), Quaternion.Euler(0f, 0f, i * 45f)));
        Save("SculpturePedestal", "sculpture_pedestal", parts);
    }

    private static void BuildBarHighTable(Material wood, Material black, Material steel)
    {
        // A round high table 0.6 m across, top at 1.05 m, with a stool on each side along X;
        // 1.5 x 0.7 m in all, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        Round(parts, wood, 0f, 1.02f, 0f, 0.60f, 1.06f);
        Round(parts, steel, 0f, 0f, 0f, 0.07f, 1.02f);
        Round(parts, steel, 0f, 0f, 0f, 0.45f, 0.03f);
        foreach (var x in new[] { -0.55f, 0.55f })
        {
            Round(parts, black, x, 0.72f, 0f, 0.36f, 0.78f);
            Round(parts, steel, x, 0f, 0f, 0.05f, 0.72f);
            Round(parts, steel, x, 0f, 0f, 0.32f, 0.03f);
            Round(parts, steel, x, 0.30f, 0f, 0.30f, 0.32f);
        }

        Save("BarHighTable", "bar_high_table", parts);
    }

    private static void BuildDartboard(Material black, Material red, Material cream, Material wood)
    {
        // A dartboard 0.45 m across on a wooden backboard 0.6 x 0.8 m, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part> { Box(wood, -0.30f, 0f, 0f, 0.30f, 0.80f, 0.03f) };
        var rings = new[] { (0.45f, black), (0.36f, cream), (0.26f, red), (0.16f, black), (0.05f, red) };
        for (var r = 0; r < rings.Length; r++)
        {
            var (width, mat) = rings[r];
            for (var i = 0; i < 6; i++)
                parts.Add(new LowPolyBoxMesh.Part(mat, new Vector3(0f, 0.45f, 0.04f + r * 0.004f),
                    new Vector3(width, width * Mathf.Tan(15f * Mathf.Deg2Rad), 0.02f), Quaternion.Euler(0f, 0f, i * 30f)));
        }

        Save("Dartboard", "dartboard", parts);
    }

    private static void BuildJukebox(Material wood, Material glow, Material glass, Material steel)
    {
        // A jukebox 0.9 x 0.7 m, 1.6 m high: a wooden cabinet, a glowing arch and the glass
        // window over the record stack, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.45f, 0f, -0.35f, 0.45f, 1.20f, 0.30f),
            Box(glow, -0.45f, 1.20f, -0.35f, 0.45f, 1.30f, 0.30f),
            Box(glow, -0.40f, 1.30f, -0.30f, 0.40f, 1.45f, 0.25f),
            Box(glow, -0.30f, 1.45f, -0.25f, 0.30f, 1.58f, 0.20f),
            Box(glow, -0.45f, 0.10f, 0.30f, -0.38f, 1.20f, 0.34f),
            Box(glow, 0.38f, 0.10f, 0.30f, 0.45f, 1.20f, 0.34f),
            Box(glass, -0.32f, 0.70f, 0.30f, 0.32f, 1.10f, 0.32f),
            Box(steel, -0.25f, 0.30f, 0.30f, 0.25f, 0.55f, 0.32f),
        };
        Save("Jukebox", "jukebox", parts);
    }

    private static void BuildDjBooth(Material black, Material neon, Material steel)
    {
        // A DJ booth 2.0 x 0.9 m, 1.1 m high with a neon band, two decks and a mixer on top and a
        // laptop stand, the DJ's side at -Z (against the wall), facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -1.00f, 0f, -0.10f, 1.00f, 1.05f, 0.45f),
            Box(neon, -1.00f, 0.80f, 0.45f, 1.00f, 0.86f, 0.46f),
            Box(neon, -1.00f, 0.10f, 0.45f, 1.00f, 0.14f, 0.46f),
            Box(steel, -1.02f, 1.05f, -0.12f, 1.02f, 1.08f, 0.47f),
            Box(black, -0.75f, 1.08f, 0f, -0.30f, 1.14f, 0.40f),
            Box(black, 0.30f, 1.08f, 0f, 0.75f, 1.14f, 0.40f),
            Box(steel, -0.20f, 1.08f, 0.05f, 0.20f, 1.16f, 0.35f),
            Box(steel, 0.80f, 1.08f, 0.10f, 0.95f, 1.30f, 0.30f),
        };
        Save("DjBooth", "dj_booth", parts);
    }

    private static void BuildDanceFloor(Material[] tiles)
    {
        // A lit dance floor 3 x 3 m of 0.5 m tiles in the neon colours, 0.05 m high, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var i = 0; i < 6; i++)
        {
            for (var j = 0; j < 6; j++)
            {
                var x = -1.50f + i * 0.50f;
                var z = -1.50f + j * 0.50f;
                parts.Add(Box(tiles[(i * 3 + j * 5 + (i * j) % 3) % tiles.Length], x + 0.01f, 0f, z + 0.01f, x + 0.49f, 0.05f, z + 0.49f));
            }
        }

        Save("DanceFloor", "dance_floor", parts);
    }

    private static void BuildSpeakerStack(Material black, Material steel)
    {
        // Two stacked PA cabinets 0.7 x 0.7 m, 1.8 m high, the cones facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -0.35f, 0f, -0.35f, 0.35f, 0.90f, 0.35f),
            Box(black, -0.33f, 0.90f, -0.33f, 0.33f, 1.80f, 0.33f),
        };
        foreach (var (y, w) in new[] { (0.40f, 0.44f), (1.20f, 0.30f), (1.60f, 0.14f) })
        {
            for (var i = 0; i < 6; i++)
                parts.Add(new LowPolyBoxMesh.Part(steel, new Vector3(0f, y, 0.35f), new Vector3(w, w * Mathf.Tan(15f * Mathf.Deg2Rad), 0.02f), Quaternion.Euler(0f, 0f, i * 30f)));
        }

        Save("SpeakerStack", "speaker_stack", parts);
    }

    private static void BuildDiscoBall(Material mirror, Material steel)
    {
        // A mirror ball 0.4 m across hung 0.4 m below its ceiling mount; the pivot at the mount (Y = 0).
        var parts = new List<LowPolyBoxMesh.Part> { Box(steel, -0.01f, -0.40f, -0.01f, 0.01f, 0f, 0.01f) };
        for (var i = 0; i < 6; i++)
        {
            parts.Add(new LowPolyBoxMesh.Part(mirror, new Vector3(0f, -0.60f, 0f), new Vector3(0.40f, 0.40f * Mathf.Tan(15f * Mathf.Deg2Rad), 0.40f), Quaternion.Euler(0f, 0f, i * 30f)));
            parts.Add(new LowPolyBoxMesh.Part(mirror, new Vector3(0f, -0.60f, 0f), new Vector3(0.40f * Mathf.Tan(15f * Mathf.Deg2Rad), 0.40f, 0.40f), Quaternion.Euler(i * 30f, 0f, 0f)));
        }

        Save("DiscoBall", "disco_ball", parts);
    }

    private static void BuildReadingTable(Material wood, Material steel, Material lamp, Material seat)
    {
        // A reading table 1.8 x 0.9 m (top at 0.75 m) along X with two green banker's lamps and
        // four chairs (two each side along Z), 2.0 x 1.9 m in all, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.90f, 0.72f, -0.45f, 0.90f, 0.76f, 0.45f),
            Box(wood, -0.85f, 0f, -0.40f, -0.80f, 0.72f, -0.35f),
            Box(wood, 0.80f, 0f, -0.40f, 0.85f, 0.72f, -0.35f),
            Box(wood, -0.85f, 0f, 0.35f, -0.80f, 0.72f, 0.40f),
            Box(wood, 0.80f, 0f, 0.35f, 0.85f, 0.72f, 0.40f),
        };
        foreach (var x in new[] { -0.45f, 0.45f })
        {
            parts.Add(Box(steel, x - 0.06f, 0.76f, -0.06f, x + 0.06f, 0.78f, 0.06f));
            parts.Add(Box(steel, x - 0.01f, 0.78f, -0.01f, x + 0.01f, 1.00f, 0.01f));
            parts.Add(Box(lamp, x - 0.18f, 1.00f, -0.08f, x + 0.18f, 1.08f, 0.08f));
            foreach (var side in new[] { -1f, 1f })
            {
                var z = side * 0.70f;
                parts.Add(Box(seat, x - 0.21f, 0.44f, z - 0.20f, x + 0.21f, 0.48f, z + 0.20f));
                parts.Add(Box(seat, x - 0.21f, 0.48f, z + side * 0.17f, x + 0.21f, 0.90f, z + side * 0.21f));
                parts.Add(Box(steel, x - 0.19f, 0f, z - 0.18f, x - 0.16f, 0.44f, z - 0.15f));
                parts.Add(Box(steel, x + 0.16f, 0f, z - 0.18f, x + 0.19f, 0.44f, z - 0.15f));
                parts.Add(Box(steel, x - 0.19f, 0f, z + 0.15f, x - 0.16f, 0.44f, z + 0.18f));
                parts.Add(Box(steel, x + 0.16f, 0f, z + 0.15f, x + 0.19f, 0.44f, z + 0.18f));
            }
        }

        Save("ReadingTable", "reading_table", parts);
    }

    private static void BuildLibraryStack(Material steel, Material[] books)
    {
        // A double-sided steel stack 2.0 x 0.6 m, 2.1 m high, six shelves of books on each face, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(steel, -1.00f, 0f, -0.30f, -0.97f, 2.10f, 0.30f),
            Box(steel, 0.97f, 0f, -0.30f, 1.00f, 2.10f, 0.30f),
            Box(steel, -1.00f, 0f, -0.02f, 1.00f, 2.10f, 0.02f),
            Box(steel, -1.00f, 2.06f, -0.30f, 1.00f, 2.10f, 0.30f),
        };
        for (var shelf = 0; shelf < 6; shelf++)
        {
            var y = 0.05f + shelf * 0.34f;
            parts.Add(Box(steel, -0.97f, y - 0.02f, -0.30f, 0.97f, y, 0.30f));
            foreach (var side in new[] { -1f, 1f })
            {
                var x = -0.95f;
                for (var i = 0; x < 0.90f; i++)
                {
                    var width = 0.04f + ((shelf * 7 + i * 3 + (side > 0 ? 1 : 0)) % 4) * 0.01f;
                    var height = 0.22f + ((shelf + i * 5) % 3) * 0.03f;
                    parts.Add(Box(books[(shelf + i + (side > 0 ? 2 : 0)) % books.Length], x, y,
                        side > 0 ? 0.04f : -0.26f, x + width, y + height, side > 0 ? 0.26f : -0.04f));
                    x += width + 0.005f;
                }
            }
        }

        Save("LibraryStack", "library_stack", parts);
    }

    private static void BuildLuggageCart(Material brass, Material carpet, Material black)
    {
        // A bellhop's cart 1.1 x 0.7 m, 1.9 m high: a carpeted deck, two brass uprights and the
        // hanging bar, a suitcase on the deck, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(carpet, -0.55f, 0.15f, -0.35f, 0.55f, 0.22f, 0.35f),
            Box(brass, -0.55f, 0.10f, -0.35f, 0.55f, 0.15f, 0.35f),
            Box(brass, -0.53f, 0.22f, -0.03f, -0.49f, 1.90f, 0.03f),
            Box(brass, 0.49f, 0.22f, -0.03f, 0.53f, 1.90f, 0.03f),
            Box(brass, -0.53f, 1.84f, -0.03f, 0.53f, 1.90f, 0.03f),
            Box(black, -0.35f, 0.22f, -0.25f, 0.25f, 0.70f, 0f),
        };
        foreach (var x in new[] { -0.45f, 0.45f })
        {
            foreach (var z in new[] { -0.28f, 0.28f })
                parts.Add(Box(black, x - 0.04f, 0f, z - 0.04f, x + 0.04f, 0.10f, z + 0.04f));
        }

        Save("LuggageCart", "luggage_cart", parts);
    }

    private static void BuildLuggageRack(Material wood, Material black, Material suitcase)
    {
        // A folding luggage rack 0.7 x 0.45 m with webbing straps and a suitcase on it, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            new LowPolyBoxMesh.Part(wood, new Vector3(-0.30f, 0.25f, 0f), new Vector3(0.03f, 0.55f, 0.03f), Quaternion.Euler(30f, 0f, 0f)),
            new LowPolyBoxMesh.Part(wood, new Vector3(-0.30f, 0.25f, 0f), new Vector3(0.03f, 0.55f, 0.03f), Quaternion.Euler(-30f, 0f, 0f)),
            new LowPolyBoxMesh.Part(wood, new Vector3(0.30f, 0.25f, 0f), new Vector3(0.03f, 0.55f, 0.03f), Quaternion.Euler(30f, 0f, 0f)),
            new LowPolyBoxMesh.Part(wood, new Vector3(0.30f, 0.25f, 0f), new Vector3(0.03f, 0.55f, 0.03f), Quaternion.Euler(-30f, 0f, 0f)),
            Box(black, -0.32f, 0.47f, -0.18f, 0.32f, 0.49f, 0.18f),
            Box(suitcase, -0.30f, 0.49f, -0.20f, 0.30f, 0.75f, 0.20f),
            Box(black, -0.08f, 0.75f, -0.02f, 0.08f, 0.79f, 0.02f),
        };
        Save("LuggageRack", "luggage_rack", parts);
    }

    private static void BuildMinibar(Material wood, Material black, Material steel)
    {
        // A minibar cabinet 0.5 x 0.5 m, 0.9 m high: the fridge door below, a kettle and two cups
        // on top, facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.25f, 0f, -0.25f, 0.25f, 0.85f, 0.25f),
            Box(black, -0.22f, 0.05f, 0.25f, 0.22f, 0.55f, 0.26f),
            Box(steel, 0.15f, 0.25f, 0.26f, 0.18f, 0.40f, 0.28f),
            Box(steel, -0.18f, 0.85f, -0.10f, -0.04f, 1.02f, 0.04f),
            Box(steel, 0.04f, 0.85f, -0.05f, 0.10f, 0.92f, 0.01f),
            Box(steel, 0.13f, 0.85f, -0.05f, 0.19f, 0.92f, 0.01f),
        };
        Save("Minibar", "minibar", parts);
    }

    private static void BuildCourtFloor(Material wood, Material line, Material key)
    {
        // A basketball court 7.5 x 14 m (long along Z): the wood, the boundary, the centre line
        // and circle, both keys, 1 cm high, centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -3.75f, 0f, -7.00f, 3.75f, 0.005f, 7.00f),
            Box(line, -3.75f, 0.005f, -7.00f, -3.70f, 0.010f, 7.00f),
            Box(line, 3.70f, 0.005f, -7.00f, 3.75f, 0.010f, 7.00f),
            Box(line, -3.75f, 0.005f, -7.00f, 3.75f, 0.010f, -6.95f),
            Box(line, -3.75f, 0.005f, 6.95f, 3.75f, 0.010f, 7.00f),
            Box(line, -3.75f, 0.005f, -0.025f, 3.75f, 0.010f, 0.025f),
        };
        for (var i = 0; i < 12; i++)
            parts.Add(new LowPolyBoxMesh.Part(line, new Vector3(0f, 0.0075f, 0f) + Quaternion.Euler(0f, i * 30f, 0f) * new Vector3(0f, 0f, 1.20f),
                new Vector3(0.64f, 0.005f, 0.05f), Quaternion.Euler(0f, i * 30f, 0f)));
        foreach (var side in new[] { -1f, 1f })
        {
            var z0 = side * 7.0f;
            var z1 = side * 2.2f;
            parts.Add(Box(key, -1.20f, 0.005f, Mathf.Min(z0, z1), 1.20f, 0.008f, Mathf.Max(z0, z1)));
            parts.Add(Box(line, -1.25f, 0.008f, Mathf.Min(z1, z1 + side * 0.05f), 1.25f, 0.010f, Mathf.Max(z1, z1 + side * 0.05f)));
        }

        Save("CourtFloor", "court_floor", parts);
    }

    private static void BuildHoopStand(Material board, Material rim, Material steel, Material pad)
    {
        // A portable hoop: a padded base 1.2 x 1.0 m, a post rising to 3.0 m and the arm reaching
        // forward (+Z) to the backboard over the court, the rim 3.05 m up; base centred.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(pad, -0.60f, 0f, -0.70f, 0.60f, 0.45f, 0.30f),
            Box(steel, -0.08f, 0.45f, -0.45f, 0.08f, 3.00f, -0.30f),
            Box(steel, -0.06f, 2.85f, -0.45f, 0.06f, 2.97f, 0.65f),
            Box(board, -0.90f, 2.90f, 0.65f, 0.90f, 3.95f, 0.70f),
            Box(rim, -0.30f, 3.05f, 0.70f, 0.30f, 3.40f, 0.71f),
        };
        for (var i = 0; i < 6; i++)
            parts.Add(new LowPolyBoxMesh.Part(rim, new Vector3(0f, 3.05f, 0.93f), new Vector3(0.46f, 0.02f, 0.46f * Mathf.Tan(15f * Mathf.Deg2Rad)), Quaternion.Euler(0f, i * 30f, 0f)));
        Save("HoopStand", "hoop_stand", parts);
    }

    private static void BuildBleachers(Material seat, Material steel)
    {
        // Three tiers of seats 4.0 m long on a steel frame, 1.5 m deep and 1.1 m high, rising
        // towards the back (-Z, against the wall), facing +Z, centred.
        var parts = new List<LowPolyBoxMesh.Part>();
        for (var t = 0; t < 3; t++)
        {
            var z0 = 0.75f - (t + 1) * 0.50f;
            var y = 0.35f + t * 0.35f;
            parts.Add(Box(steel, -2.00f, 0f, z0, 2.00f, y - 0.05f, z0 + 0.50f));
            parts.Add(Box(seat, -1.95f, y - 0.05f, z0 + 0.10f, 1.95f, y, z0 + 0.45f));
        }

        Save("Bleachers", "bleachers", parts);
    }

    private static void BuildScoreboard(Material black, Material red, Material amber)
    {
        // A scoreboard 1.8 x 0.9 m: two red scores and the amber clock, back at Z = 0.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(black, -0.90f, 0f, 0f, 0.90f, 0.90f, 0.15f),
            Box(red, -0.80f, 0.15f, 0.15f, -0.35f, 0.55f, 0.16f),
            Box(red, 0.35f, 0.15f, 0.15f, 0.80f, 0.55f, 0.16f),
            Box(amber, -0.25f, 0.60f, 0.15f, 0.25f, 0.82f, 0.16f),
        };
        Save("Scoreboard", "scoreboard", parts);
    }

    private static void BuildVitrine(Material wood, Material glass, Material brass)
    {
        // A plinth 1.0 x 0.5 x 0.8 m and a glass case 0.6 m high on it holding three small objects.
        var parts = new List<LowPolyBoxMesh.Part>
        {
            Box(wood, -0.50f, 0f, -0.25f, 0.50f, 0.80f, 0.25f),
            Box(wood, -0.52f, 0.80f, -0.27f, 0.52f, 0.84f, 0.27f),
            Box(glass, -0.48f, 0.84f, -0.23f, 0.48f, 1.42f, 0.23f),
            Box(wood, -0.50f, 1.42f, -0.25f, 0.50f, 1.46f, 0.25f),
            Box(brass, -0.32f, 0.84f, -0.06f, -0.20f, 1.00f, 0.06f),
            Box(brass, -0.05f, 0.84f, -0.08f, 0.08f, 0.92f, 0.08f),
            Box(brass, 0.20f, 0.84f, -0.04f, 0.28f, 1.06f, 0.04f),
        };
        Save("CivicVitrine", "civic_vitrine", parts);
    }
}
