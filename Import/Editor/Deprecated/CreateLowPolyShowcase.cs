using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Creates a showcase scene for all LowPoly prefabs.
/// Menu: Tools / Procedural Cities / Create LowPoly Showcase Scene
/// </summary>
public class CreateLowPolyShowcase
{
    static string[] PrefabFolders = new[]
    {
        "Packages/dev.z3nth10n.proceduralcities.import/Models/LowPoly",
        "Assets/LowPoly"
    };
    static string ScenePath = "Assets/Scenes/LowPolyShowcase.unity";

    // [MenuItem("Tools/uzProceduralCities/Create LowPoly Showcase Scene")]
    static void Create()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Find all lowpoly prefabs (search both package and Assets paths)
        var allGuids = new HashSet<string>();
        foreach (var folder in PrefabFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (var g in AssetDatabase.FindAssets("t:Prefab LowPoly_", new[] { folder }))
                allGuids.Add(g);
        }
        var prefabs = allGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p != null)
            .OrderBy(p => p.name)
            .ToList();

        if (prefabs.Count == 0)
        {
            Debug.LogError("[LowPoly Showcase] No LowPoly prefabs found in any of: " + string.Join(", ", PrefabFolders));
            return;
        }

        Debug.Log($"[LowPoly Showcase] Found {prefabs.Count} prefabs");

        // Layout config
        int cols = 5;
        float spacingX = 4f;
        float spacingZ = 4f;

        // Create ground plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        float gridWidth = cols * spacingX;
        float gridDepth = Mathf.Ceil(prefabs.Count / (float)cols) * spacingZ;
        ground.transform.position = new Vector3(gridWidth / 2f - spacingX / 2f, -0.01f, gridDepth / 2f - spacingZ / 2f);
        ground.transform.localScale = new Vector3(gridWidth / 8f, 1, gridDepth / 6f);
        var groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.85f, 0.85f, 0.82f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // Parent for all prefabs
        var parent = new GameObject("LowPoly_Models");

        // Instantiate prefabs in grid
        for (int i = 0; i < prefabs.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = col * spacingX;
            float z = row * spacingZ;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
            instance.transform.SetParent(parent.transform);
            instance.transform.position = new Vector3(x, 0, z);

            // Add label
            var labelGo = new GameObject($"Label_{prefabs[i].name}");
            labelGo.transform.SetParent(instance.transform);
            labelGo.transform.localPosition = new Vector3(0, -0.15f, 0);

            var tm = labelGo.AddComponent<TextMesh>();
            string displayName = prefabs[i].name.Replace("LowPoly_", "");
            // Get vert count
            int verts = 0;
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount;
            tm.text = $"{displayName}\n{verts}v";
            tm.fontSize = 32;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.black;
        }

        // Configure camera
        var cam = Camera.main;
        if (cam != null)
        {
            float centerX = (cols - 1) * spacingX / 2f;
            float centerZ = (Mathf.Ceil(prefabs.Count / (float)cols) - 1) * spacingZ / 2f;
            cam.transform.position = new Vector3(centerX, 8f, centerZ - 8f);
            cam.transform.LookAt(new Vector3(centerX, 0.5f, centerZ));
            cam.backgroundColor = new Color(0.75f, 0.82f, 0.9f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        // Configure directional light
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                light.intensity = 1.2f;
                light.shadows = LightShadows.Soft;
            }
        }

        // Save scene
        string sceneDir = Path.GetDirectoryName(ScenePath);
        if (!AssetDatabase.IsValidFolder(sceneDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[LowPoly Showcase] Scene saved to {ScenePath} with {prefabs.Count} models");
    }
}
