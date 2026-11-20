using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds LowPoly_TrafficLightDouble, the low traffic light of the secondary roads (P112): two
/// signal heads at 90 degrees on top of one pole and a small repeater lower down, as a street
/// traffic light. Same 5.00 m height as LowPoly_TrafficLight (the city scales both from that
/// height), same materials. The main head is the single model's, on top, its three lights on
/// the front (+Z, towards the oncoming cars); the second head stands beside it on the +X side
/// (towards the road), turned 90 degrees, red over a dark middle over green, and shows the
/// opposite of the main head: green while it is red, red otherwise. The repeater, at a driver's
/// eye height, repeats the main head with its lenses. The second head's lenses are
/// LP_Light_Red_Cross and LP_Light_Green_Cross, copies of the lens materials made here, so
/// Rendering.TrafficLights can light them apart.
/// Output: Models/LowPoly/TrafficLightDouble_Combined.asset (one submesh per material)
/// and Models/LowPoly/LowPoly_TrafficLightDouble.prefab, rewritten in place so their
/// GUIDs stay. Menu: Tools / Procedural Cities / Generate Double Traffic Light
/// </summary>
public static class GenerateTrafficLightDoubleProp
{
    internal const string ModelDir = "Packages/dev.z3nth10n.proceduralcities.import/Models/LowPoly";
    internal const string MatDir = ModelDir + "/Materials";
    private const string PrefabName = "LowPoly_TrafficLightDouble";

    // The single model's measures (LowPoly_TrafficLight, whose generator was removed in P41.9).
    private const float PoleRadius = 0.06f;
    private const float HeadWidth = 0.30f;
    private const float HeadHeight = 0.85f;
    private const float HeadDepth = 0.20f;
    private const float HeadCentreY = 4.55f;
    private const float LightRadius = 0.08f;
    private const float LightSpacing = 0.25f;

    // The second head: beside the main one on the +X side of the pole, facing +X.
    private const float CrossBracketGap = 0.04f;

    // The repeater: a small head on the front of the pole, at a driver's eye height once scaled.
    internal const float RepeaterCentreY = 2.5f;
    internal const float RepeaterWidth = 0.22f;
    internal const float RepeaterHeight = 0.5f;
    internal const float RepeaterDepth = 0.16f;
    internal const float RepeaterLightRadius = 0.05f;
    internal const float RepeaterLightSpacing = 0.14f;

    /// <summary>The second head's lens materials, made from these lens materials.</summary>
    internal static readonly (string Name, string Source)[] CrossLenses =
    {
        ("LP_Light_Red_Cross", "LP_Light_Red"),
        ("LP_Light_Green_Cross", "LP_Light_Green"),
    };

    /// <summary>Builds a mesh of boxes and prisms, one submesh per material (shared by the traffic light generators).</summary>
    internal sealed class MeshBuilder
    {
        private readonly List<Vector3> _verts = new();
        private readonly List<Vector3> _norms = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<Material> _materials = new();
        private readonly List<List<int>> _tris = new();

        private List<int> Tris(Material material)
        {
            var index = _materials.IndexOf(material);
            if (index < 0)
            {
                _materials.Add(material);
                _tris.Add(new List<int>());
                index = _materials.Count - 1;
            }

            return _tris[index];
        }

        /// <summary>A box of <paramref name="size"/> placed by <paramref name="transform"/> (its centre at the origin).</summary>
        public void Box(Material material, Vector3 size, Matrix4x4 transform)
        {
            var h = size * 0.5f;
            Vector3 P(float x, float y, float z) => transform.MultiplyPoint3x4(new Vector3(x * h.x, y * h.y, z * h.z));
            Vector3 N(Vector3 n) => transform.MultiplyVector(n).normalized;
            var tris = Tris(material);
            Quad(tris, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), N(Vector3.forward));
            Quad(tris, P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), N(Vector3.back));
            Quad(tris, P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), N(Vector3.right));
            Quad(tris, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), N(Vector3.left));
            Quad(tris, P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1), N(Vector3.up));
            Quad(tris, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), N(Vector3.down));
        }

        /// <summary>A prism of <paramref name="segments"/> sides along local Y (a low-poly cylinder) with its caps.</summary>
        public void Cylinder(Material material, float radius, float length, int segments, Matrix4x4 transform)
        {
            var tris = Tris(material);
            var half = length * 0.5f;
            for (var i = 0; i < segments; i++)
            {
                var a0 = Mathf.PI * 2f * i / segments;
                var a1 = Mathf.PI * 2f * (i + 1) / segments;
                var c0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var c1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                var am = (a0 + a1) * 0.5f;
                var n = transform.MultiplyVector(new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am))).normalized;
                Quad(tris,
                    transform.MultiplyPoint3x4(c1 + Vector3.down * half), transform.MultiplyPoint3x4(c0 + Vector3.down * half),
                    transform.MultiplyPoint3x4(c0 + Vector3.up * half), transform.MultiplyPoint3x4(c1 + Vector3.up * half), n);
                Tri(tris, transform.MultiplyPoint3x4(Vector3.up * half), transform.MultiplyPoint3x4(c1 + Vector3.up * half),
                    transform.MultiplyPoint3x4(c0 + Vector3.up * half), transform.MultiplyVector(Vector3.up).normalized);
                Tri(tris, transform.MultiplyPoint3x4(Vector3.down * half), transform.MultiplyPoint3x4(c0 + Vector3.down * half),
                    transform.MultiplyPoint3x4(c1 + Vector3.down * half), transform.MultiplyVector(Vector3.down).normalized);
            }
        }

        private void Quad(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            var i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c); _verts.Add(d);
            for (var k = 0; k < 4; k++) _norms.Add(n);
            _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(1, 0));
            _uvs.Add(new Vector2(1, 1)); _uvs.Add(new Vector2(0, 1));
            // Unity's front face is clockwise seen from outside.
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0f)
                tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            else
                tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        private void Tri(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            var i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            for (var k = 0; k < 3; k++) _norms.Add(n);
            _uvs.Add(new Vector2(0.5f, 1)); _uvs.Add(new Vector2(0, 0)); _uvs.Add(new Vector2(1, 0));
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0f)
                tris.AddRange(new[] { i, i + 2, i + 1 });
            else
                tris.AddRange(new[] { i, i + 1, i + 2 });
        }

        public Mesh ToMesh(string name, out Material[] materials)
        {
            var m = new Mesh { name = name, subMeshCount = _tris.Count };
            m.SetVertices(_verts);
            m.SetNormals(_norms);
            m.SetUVs(0, _uvs);
            for (var s = 0; s < _tris.Count; s++)
                m.SetTriangles(_tris[s], s);
            m.RecalculateBounds();
            materials = _materials.ToArray();
            return m;
        }
    }

    internal static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");

    // Menu entry off at the owner's request (P144): call the method from code to regenerate the asset.
    // [MenuItem("Tools/uzProceduralCities/Generate Double Traffic Light")]
    public static void Generate()
    {
        var pole = Load("LP_Metal_Lamppost");
        var body = Load("LP_Metal_Black");
        var red = Load("LP_Light_Red");
        var yellow = Load("LP_Light_Yellow");
        var green = Load("LP_Light_Green");
        if (!pole || !body || !red || !yellow || !green)
        {
            Debug.LogError("[GenerateTrafficLightDoubleProp] Missing LP_* materials.");
            return;
        }

        var redCross = CrossLens(CrossLenses[0].Name, red);
        var greenCross = CrossLens(CrossLenses[1].Name, green);

        var mb = new MeshBuilder();
        // Pole up to the main head, as the single model.
        var poleTop = HeadCentreY - HeadHeight * 0.5f;
        mb.Cylinder(pole, PoleRadius, poleTop, 8, Matrix4x4.Translate(new Vector3(0f, poleTop * 0.5f, 0f)));

        // Main head and its three lights on the front (+Z).
        mb.Box(body, new Vector3(HeadWidth, HeadHeight, HeadDepth), Matrix4x4.Translate(new Vector3(0f, HeadCentreY, 0f)));
        Material[] lights = { red, yellow, green };
        for (var i = 0; i < 3; i++)
        {
            var centre = new Vector3(0f, 4.8f - i * LightSpacing, HeadDepth * 0.5f + 0.01f);
            mb.Cylinder(lights[i], LightRadius, 0.02f, 8, Matrix4x4.TRS(centre, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
        }

        // Second head beside the main one, turned 90 degrees: its back on a short bracket from
        // the main head's side, red over a dark middle over green on +X.
        var crossX = HeadWidth * 0.5f + CrossBracketGap + HeadDepth * 0.5f;
        mb.Box(pole, new Vector3(CrossBracketGap, 0.06f, 0.06f),
            Matrix4x4.Translate(new Vector3(HeadWidth * 0.5f + CrossBracketGap * 0.5f, HeadCentreY, 0f)));
        mb.Box(body, new Vector3(HeadDepth, HeadHeight, HeadWidth), Matrix4x4.Translate(new Vector3(crossX, HeadCentreY, 0f)));
        Material[] crossLights = { redCross, body, greenCross };
        for (var i = 0; i < 3; i++)
        {
            var centre = new Vector3(crossX + HeadDepth * 0.5f + 0.01f, 4.8f - i * LightSpacing, 0f);
            mb.Cylinder(crossLights[i], LightRadius, 0.02f, 8, Matrix4x4.TRS(centre, Quaternion.Euler(0f, 0f, 90f), Vector3.one));
        }

        // The repeater on the front of the pole, its lenses the main head's.
        AddRepeater(mb, body, lights, PoleRadius, 0f, RepeaterCentreY);

        Save(mb, "TrafficLightDouble_Combined", PrefabName, "trafficlight_double");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// A repeater (a small three-light head) on the front (+Z) of a pole of radius
    /// <paramref name="poleRadius"/> standing at x = <paramref name="x"/>, centred at height
    /// <paramref name="centreY"/>; <paramref name="lights"/> red, amber, green from the top.
    /// </summary>
    internal static void AddRepeater(MeshBuilder mb, Material body, Material[] lights, float poleRadius, float x, float centreY)
    {
        var z = poleRadius + 0.005f + RepeaterDepth * 0.5f;
        mb.Box(body, new Vector3(RepeaterWidth, RepeaterHeight, RepeaterDepth), Matrix4x4.Translate(new Vector3(x, centreY, z)));
        for (var i = 0; i < 3; i++)
        {
            var centre = new Vector3(x, centreY + (1 - i) * RepeaterLightSpacing, z + RepeaterDepth * 0.5f + 0.01f);
            mb.Cylinder(lights[i], RepeaterLightRadius, 0.02f, 8, Matrix4x4.TRS(centre, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
        }
    }

    /// <summary>
    /// The second head's lens <paramref name="name"/>: a copy of <paramref name="source"/>
    /// (made once, then kept so its GUID stays), unlit until the day-night cycle lights it.
    /// </summary>
    internal static Material CrossLens(string name, Material source)
    {
        var path = $"{MatDir}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var create = material == null;
        if (create)
            material = new Material(source) { name = name };
        else
            material.CopyPropertiesFromMaterial(source);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.SetColor("_EmissionColor", source.GetColor("_BaseColor") * 0.04f);
        if (create)
            AssetDatabase.CreateAsset(material, path);
        else
            EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// Saves <paramref name="builder"/>'s mesh as Models/LowPoly/<paramref name="meshName"/>.asset and a
    /// prefab <paramref name="prefabName"/> with one child <paramref name="childName"/> (renderer and
    /// convex collider), both rewritten in place so their GUIDs stay.
    /// </summary>
    internal static void Save(MeshBuilder builder, string meshName, string prefabName, string childName)
    {
        var mesh = builder.ToMesh(meshName, out var materials);
        var meshPath = $"{ModelDir}/{meshName}.asset";
        var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existingMesh != null)
        {
            existingMesh.Clear();
            EditorUtility.CopySerialized(mesh, existingMesh);
            Object.DestroyImmediate(mesh);
            mesh = existingMesh;
            EditorUtility.SetDirty(mesh);
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var root = new GameObject(prefabName);
        try
        {
            var child = new GameObject(childName);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var collider = child.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            PrefabUtility.SaveAsPrefabAsset(root, $"{ModelDir}/{prefabName}.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log($"[GenerateTrafficLightDoubleProp] Saved {prefabName} ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }
}
