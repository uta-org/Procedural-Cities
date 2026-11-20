using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds LowPoly_TrafficLightElbow, the elbow traffic light of the primary roads (P112): a pole
/// that bends at the top into an arm reaching out over the road, with the signal head hanging at
/// its end, plus a small repeater on the pole at a driver's eye height. Modelled at its real size
/// (placed at scale 1, unlike the 5 m single and double models): the arm runs along +X (towards
/// the road), the head and the repeater face +Z (the oncoming cars). The lenses are the single
/// model's LP_Light_* materials, so Rendering.TrafficLights lights them with the rest; its lens
/// offsets there (ElbowLensLocal) follow <see cref="HeadX"/>, <see cref="HeadCentreY"/>,
/// <see cref="LightSpacing"/> and <see cref="HeadDepth"/>.
/// Output: Models/LowPoly/TrafficLightElbow_Combined.asset (one submesh per material) and
/// Models/LowPoly/LowPoly_TrafficLightElbow.prefab, rewritten in place so their GUIDs stay.
/// Menu: Tools / Procedural Cities / Generate Elbow Traffic Light
/// </summary>
public static class GenerateTrafficLightElbowProp
{
    private const string PrefabName = "LowPoly_TrafficLightElbow";

    // The pole, straight up to where it bends.
    private const float PoleRadius = 0.075f;
    private const float PoleHeight = 5.6f;

    // The bend (a quarter circle) and the arm along +X at its top.
    private const float BendRadius = 0.7f;
    private const int BendSegments = 4;
    private const float ArmY = PoleHeight + BendRadius;
    private const float ArmRadius = 0.055f;
    private const float ArmEndX = 4.6f;

    // The head, hanging from the arm near its end, facing +Z.
    internal const float HeadX = 4.4f;
    private const float HeadWidth = 0.36f;
    private const float HeadHeight = 1.0f;
    internal const float HeadDepth = 0.26f;
    private const float HangerHeight = 0.12f;
    internal const float HeadCentreY = ArmY - HangerHeight - HeadHeight * 0.5f;
    private const float LightRadius = 0.11f;
    internal const float LightSpacing = 0.3f;

    // The repeater on the pole.
    private const float RepeaterCentreY = 2.4f;

    // Menu entry off at the owner's request (P144): call the method from code to regenerate the asset.
    // [MenuItem("Tools/uzProceduralCities/Generate Elbow Traffic Light")]
    public static void Generate()
    {
        var pole = GenerateTrafficLightDoubleProp.Load("LP_Metal_Lamppost");
        var body = GenerateTrafficLightDoubleProp.Load("LP_Metal_Black");
        var red = GenerateTrafficLightDoubleProp.Load("LP_Light_Red");
        var yellow = GenerateTrafficLightDoubleProp.Load("LP_Light_Yellow");
        var green = GenerateTrafficLightDoubleProp.Load("LP_Light_Green");
        if (!pole || !body || !red || !yellow || !green)
        {
            Debug.LogError("[GenerateTrafficLightElbowProp] Missing LP_* materials.");
            return;
        }

        var mb = new GenerateTrafficLightDoubleProp.MeshBuilder();

        // The pole, then the bend from vertical to horizontal around (BendRadius, PoleHeight).
        mb.Cylinder(pole, PoleRadius, PoleHeight, 8, Matrix4x4.Translate(new Vector3(0f, PoleHeight * 0.5f, 0f)));
        var bendCentre = new Vector3(BendRadius, PoleHeight, 0f);
        var previous = new Vector3(0f, PoleHeight, 0f);
        for (var k = 1; k <= BendSegments; k++)
        {
            var angle = Mathf.PI * (1f - 0.5f * k / BendSegments);
            var next = bendCentre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * BendRadius;
            var radius = Mathf.Lerp(PoleRadius, ArmRadius, (float)k / BendSegments);
            Segment(mb, pole, previous, next, radius);
            previous = next;
        }

        // The arm out over the road.
        Segment(mb, pole, previous, new Vector3(ArmEndX, ArmY, 0f), ArmRadius);

        // The head on a short hanger, its three lights on the front (+Z).
        mb.Box(pole, new Vector3(0.05f, HangerHeight, 0.05f),
            Matrix4x4.Translate(new Vector3(HeadX, ArmY - HangerHeight * 0.5f, 0f)));
        mb.Box(body, new Vector3(HeadWidth, HeadHeight, HeadDepth), Matrix4x4.Translate(new Vector3(HeadX, HeadCentreY, 0f)));
        Material[] lights = { red, yellow, green };
        for (var i = 0; i < 3; i++)
        {
            var centre = new Vector3(HeadX, HeadCentreY + (1 - i) * LightSpacing, HeadDepth * 0.5f + 0.01f);
            mb.Cylinder(lights[i], LightRadius, 0.02f, 8, Matrix4x4.TRS(centre, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
        }

        GenerateTrafficLightDoubleProp.AddRepeater(mb, body, lights, PoleRadius, 0f, RepeaterCentreY);

        GenerateTrafficLightDoubleProp.Save(mb, "TrafficLightElbow_Combined", PrefabName, "trafficlight_elbow");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>A prism of <paramref name="radius"/> from <paramref name="a"/> to <paramref name="b"/>, a touch longer so the joints close.</summary>
    private static void Segment(GenerateTrafficLightDoubleProp.MeshBuilder mb, Material material, Vector3 a, Vector3 b, float radius)
    {
        var along = b - a;
        var rotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
        mb.Cylinder(material, radius, along.magnitude + radius, 8, Matrix4x4.TRS((a + b) * 0.5f, rotation, Vector3.one));
    }
}
