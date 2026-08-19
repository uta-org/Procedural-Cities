using UnityEditor;
using UnityEngine;

/// <summary>
/// Commits 5-6 of the pendant-lamp bulb work. Commit 5: "la parte
/// cilindrica la hagas hueca, y dentro del hueco del cilindro metras la
/// esfera del bulbo de la bombilla" — the hanging ceiling lamp's shade was
/// a solid capped cylinder (see GenerateLampProps.cs BuildCeilingPendant),
/// so the bulb AddLampBulbs.cs adds inside it (commit 1: y=-0.53, just past
/// the underside) was always going to be fully hidden or, at best, only
/// barely visible peeking past the rim. Rebuilds "Shade" as an open tube
/// (side wall only, no top/bottom caps, double-sided material) so it reads
/// as a genuinely hollow shade, and moves the bulb anchor back inside it
/// (y=-0.45, the shade's own centre) now that the interior is actually
/// visible instead of solid.
/// Commit 6: "a la lampara que cuelga le faltaria la tapa de arriba del
/// cilindro para terminar" — an open tube alone has no top, so looking up
/// from directly underneath shows straight through into the cord/canopy
/// instead of a finished shade interior. Adds "ShadeTopCap", a flat
/// annulus (a ring, not a solid disc — it needs a hole for the Cord to
/// pass through) capping the tube's open top.
/// Menu: Tools / Procedural Cities / Hollow Ceiling Pendant Shade
/// </summary>
public static class HollowCeilingPendantShade
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";
    private const string MeshDir = PkgRoot + "/Models/LowPoly/Meshes";

    private const float ShadeRadius = 0.10f;
    private const float ShadeHeight = 0.12f; // matches the old cylinder's halfHeight=0.06 * 2
    private const int ShadeSegments = 16;
    private static readonly Vector3 BulbAnchorInsideShade = new(0f, -0.45f, 0f);

    // Cord radius is 0.006 (see GenerateLampProps.cs BuildCeilingPendant) --
    // twice that leaves a small, visually-tidy clearance around it.
    private const float CordHoleRadius = 0.012f;
    private static readonly Vector3 ShadeTopCapLocalPos = new(0f, -0.45f + ShadeHeight * 0.5f, 0f);

    [MenuItem("Tools/Procedural Cities/Hollow Ceiling Pendant Shade")]
    public static void Generate()
    {
        var path = FindPrefabPath("CeilingPendant");
        if (path == null)
        {
            Debug.LogWarning("[HollowCeilingPendantShade] CeilingPendant prefab not found.");
            return;
        }

        EnsureMeshFolder();
        var doubleSidedMat = GetOrCreateDoubleSidedShadeMat();

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var shade = root.transform.Find("Shade");
            if (shade == null)
            {
                Debug.LogWarning("[HollowCeilingPendantShade] CeilingPendant has no 'Shade' child.");
                return;
            }

            var mf = shade.GetComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogWarning("[HollowCeilingPendantShade] Shade has no MeshFilter.");
                return;
            }

            // Undo the primitive cylinder's non-uniform scale trick
            // (AddCylinder scales a unit primitive by (radius*2, halfHeight,
            // radius*2)) — this new mesh is authored at real world size
            // directly, so the transform goes back to identity scale.
            shade.localScale = Vector3.one;
            mf.sharedMesh = GetOrCreateOpenTubeMeshAsset(ShadeRadius, ShadeHeight, ShadeSegments);

            var mr = shade.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sharedMaterial = doubleSidedMat;

            var anchor = root.transform.Find("BulbAnchor");
            if (anchor != null)
                anchor.localPosition = BulbAnchorInsideShade;

            EnsureShadeTopCap(root, doubleSidedMat);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[HollowCeilingPendantShade] CeilingPendant's Shade is now an open tube with a top cap; bulb anchor moved inside it.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// A double-sided (Cull Off) clone of LP_Lamp_ShadeCool so the tube's
    /// interior renders too — the shared original stays single-sided since
    /// every other prefab using it (TableLampCone, etc.) is a solid shape
    /// with no interior to see.
    /// </summary>
    private static Material GetOrCreateDoubleSidedShadeMat()
    {
        var path = $"{MatDir}/LP_Lamp_ShadeCool_DoubleSided.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var sourcePath = $"{MatDir}/LP_Lamp_ShadeCool.mat";
        var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        var mat = source != null
            ? new Material(source)
            : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.95f, 0.95f, 0.90f),
            };
        mat.name = "LP_Lamp_ShadeCool_DoubleSided";
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureMeshFolder()
    {
        if (!AssetDatabase.IsValidFolder(PkgRoot + "/Models/LowPoly"))
            AssetDatabase.CreateFolder(PkgRoot + "/Models", "LowPoly");
        if (!AssetDatabase.IsValidFolder(MeshDir))
            AssetDatabase.CreateFolder(PkgRoot + "/Models/LowPoly", "Meshes");
    }

    /// <summary>
    /// A bare runtime Mesh assigned to a MeshFilter isn't automatically
    /// embedded when the prefab is saved (PrefabUtility.SaveAsPrefabAsset
    /// silently drops the reference — verified live: the Shade's sharedMesh
    /// came back null after saving), so this persists it as its own asset
    /// first, the same way GetMat persists materials.
    /// </summary>
    private static Mesh GetOrCreateOpenTubeMeshAsset(float radius, float height, int segments)
    {
        var path = $"{MeshDir}/CeilingPendant_ShadeOpenTube.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        var mesh = BuildOpenTubeMesh(radius, height, segments);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// Side-wall-only cylinder (no top/bottom caps) so the interior is
    /// visible through either open end. Outward normals; the double-sided
    /// material (Cull Off) is what makes the interior face render too.
    /// </summary>
    private static Mesh BuildOpenTubeMesh(float radius, float height, int segments)
    {
        var mesh = new Mesh { name = "OpenTube" };
        var ringVerts = segments + 1;
        var vertices = new Vector3[ringVerts * 2];
        var normals = new Vector3[ringVerts * 2];
        var uvs = new Vector2[ringVerts * 2];
        var halfHeight = height * 0.5f;

        for (var i = 0; i <= segments; i++)
        {
            var angle = (float)i / segments * Mathf.PI * 2f;
            var x = Mathf.Cos(angle);
            var z = Mathf.Sin(angle);
            var normal = new Vector3(x, 0f, z);

            vertices[i] = new Vector3(x * radius, halfHeight, z * radius);
            vertices[ringVerts + i] = new Vector3(x * radius, -halfHeight, z * radius);
            normals[i] = normal;
            normals[ringVerts + i] = normal;
            uvs[i] = new Vector2((float)i / segments, 1f);
            uvs[ringVerts + i] = new Vector2((float)i / segments, 0f);
        }

        var triangles = new int[segments * 6];
        for (var i = 0; i < segments; i++)
        {
            var ti = i * 6;
            var top0 = i;
            var top1 = i + 1;
            var bot0 = ringVerts + i;
            var bot1 = ringVerts + i + 1;

            triangles[ti + 0] = top0;
            triangles[ti + 1] = bot0;
            triangles[ti + 2] = top1;

            triangles[ti + 3] = top1;
            triangles[ti + 4] = bot0;
            triangles[ti + 5] = bot1;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>
    /// Adds (or repositions, if already present) "ShadeTopCap" — a flat
    /// annulus closing the open tube's top, with a hole for the Cord to
    /// pass through. Sibling of "Shade", not a child of it: the cap and the
    /// tube wall are both flat/thin single-sided-ish surfaces that don't
    /// need to share a transform, and keeping them separate objects means
    /// re-running this method can freely replace one without touching the
    /// other.
    /// </summary>
    private static void EnsureShadeTopCap(GameObject root, Material material)
    {
        var existing = root.transform.Find("ShadeTopCap");
        var capGO = existing != null ? existing.gameObject : new GameObject("ShadeTopCap");
        if (existing == null)
        {
            capGO.transform.SetParent(root.transform, false);
            capGO.AddComponent<MeshFilter>();
            capGO.AddComponent<MeshRenderer>();
        }

        capGO.transform.localPosition = ShadeTopCapLocalPos;
        capGO.transform.localRotation = Quaternion.identity;
        capGO.transform.localScale = Vector3.one;

        capGO.GetComponent<MeshFilter>().sharedMesh =
            GetOrCreateAnnulusMeshAsset(ShadeRadius, CordHoleRadius, ShadeSegments);
        capGO.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    /// <summary>See GetOrCreateOpenTubeMeshAsset — same "persist before assigning" requirement.</summary>
    private static Mesh GetOrCreateAnnulusMeshAsset(float outerRadius, float innerRadius, int segments)
    {
        var path = $"{MeshDir}/CeilingPendant_ShadeTopCap.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        var mesh = BuildAnnulusMesh(outerRadius, innerRadius, segments);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    /// <summary>
    /// A flat ring (outer radius to inner radius, normals down) — the
    /// underside is what a viewer looking up through the shade's open
    /// bottom sees, and the double-sided material makes it visible from
    /// above too in case the canopy/cord gap ever lets daylight through.
    /// </summary>
    private static Mesh BuildAnnulusMesh(float outerRadius, float innerRadius, int segments)
    {
        var mesh = new Mesh { name = "Annulus" };
        var ringVerts = segments + 1;
        var vertices = new Vector3[ringVerts * 2];
        var normals = new Vector3[ringVerts * 2];
        var uvs = new Vector2[ringVerts * 2];

        for (var i = 0; i <= segments; i++)
        {
            var angle = (float)i / segments * Mathf.PI * 2f;
            var x = Mathf.Cos(angle);
            var z = Mathf.Sin(angle);

            vertices[i] = new Vector3(x * outerRadius, 0f, z * outerRadius);
            vertices[ringVerts + i] = new Vector3(x * innerRadius, 0f, z * innerRadius);
            normals[i] = Vector3.down;
            normals[ringVerts + i] = Vector3.down;
            uvs[i] = new Vector2((float)i / segments, 1f);
            uvs[ringVerts + i] = new Vector2((float)i / segments, 0f);
        }

        var triangles = new int[segments * 6];
        for (var i = 0; i < segments; i++)
        {
            var ti = i * 6;
            var outer0 = i;
            var outer1 = i + 1;
            var inner0 = ringVerts + i;
            var inner1 = ringVerts + i + 1;

            triangles[ti + 0] = outer0;
            triangles[ti + 1] = inner1;
            triangles[ti + 2] = inner0;

            triangles[ti + 3] = outer0;
            triangles[ti + 4] = outer1;
            triangles[ti + 5] = inner1;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static string FindPrefabPath(string prefabName)
    {
        var guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        foreach (var guid in guids)
        {
            var candidatePath = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(candidatePath) == prefabName)
                return candidatePath;
        }
        return null;
    }
}
