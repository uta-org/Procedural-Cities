using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds LowPoly_TrafficLightDouble: the low-poly traffic light's double version, two
/// signal heads side by side on one pole ("hay una variable del semáforo que es doble",
/// seen in the original project). Same 5.00 m height as LowPoly_TrafficLight (the city
/// scales both from that height), same materials: the pole up to a Y bracket whose two
/// arms carry a head each, three lights on the front (+Z) of each head.
/// Output: Models/LowPoly/TrafficLightDouble_Combined.asset (one submesh per material)
/// and Models/LowPoly/LowPoly_TrafficLightDouble.prefab, rewritten in place so their
/// GUIDs stay. Menu: Tools / Procedural Cities / Generate Double Traffic Light
/// </summary>
public static class GenerateTrafficLightDoubleProp
{
    private const string ModelDir = "Packages/dev.z3nth10n.proceduralcities.import/Models/LowPoly";
    private const string MatDir = ModelDir + "/Materials";
    private const string PrefabName = "LowPoly_TrafficLightDouble";

    // The single model's measures (LowPoly_TrafficLight, whose generator was removed in P41.9).
    private const float PoleRadius = 0.06f;
    private const float HeadWidth = 0.30f;
    private const float HeadHeight = 0.85f;
    private const float HeadDepth = 0.20f;
    private const float HeadCentreY = 4.55f;
    private const float LightRadius = 0.08f;
    private const float LightSpacing = 0.25f;

    // The bracket: the pole stops at its fork, each arm reaches a head's centre line.
    private const float ForkY = 3.80f;
    private const float HeadOffsetX = 0.24f;
    private const float ArmTopY = HeadCentreY - HeadHeight * 0.5f;
    private const float ArmThickness = 0.06f;

    private sealed class MeshBuilder
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

    private static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");

    [MenuItem("Tools/Procedural Cities/Generate Double Traffic Light")]
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

        var mb = new MeshBuilder();
        // Pole up to the fork.
        mb.Cylinder(pole, PoleRadius, ForkY, 8, Matrix4x4.Translate(new Vector3(0f, ForkY * 0.5f, 0f)));
        foreach (var side in new[] { -1f, 1f })
        {
            // Arm from the fork to under its head, leaning out.
            var from = new Vector3(0f, ForkY, 0f);
            var to = new Vector3(side * HeadOffsetX, ArmTopY, 0f);
            var along = to - from;
            var arm = Matrix4x4.TRS((from + to) * 0.5f, Quaternion.FromToRotation(Vector3.up, along.normalized), Vector3.one);
            mb.Cylinder(pole, ArmThickness * 0.5f, along.magnitude + ArmThickness, 8, arm);

            // Head and its three lights on the front.
            var head = new Vector3(side * HeadOffsetX, HeadCentreY, 0f);
            mb.Box(body, new Vector3(HeadWidth, HeadHeight, HeadDepth), Matrix4x4.Translate(head));
            Material[] lights = { red, yellow, green };
            for (var i = 0; i < 3; i++)
            {
                var centre = new Vector3(head.x, 4.8f - i * LightSpacing, HeadDepth * 0.5f + 0.01f);
                mb.Cylinder(lights[i], LightRadius, 0.02f, 8, Matrix4x4.TRS(centre, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
            }
        }

        Save(mb);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Save(MeshBuilder builder)
    {
        var mesh = builder.ToMesh("TrafficLightDouble_Combined", out var materials);
        var meshPath = $"{ModelDir}/TrafficLightDouble_Combined.asset";
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

        var root = new GameObject(PrefabName);
        try
        {
            var child = new GameObject("trafficlight_double");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var collider = child.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            PrefabUtility.SaveAsPrefabAsset(root, $"{ModelDir}/{PrefabName}.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log($"[GenerateTrafficLightDoubleProp] Saved {PrefabName} ({mesh.vertexCount} verts, bounds {mesh.bounds.size}).");
    }
}
