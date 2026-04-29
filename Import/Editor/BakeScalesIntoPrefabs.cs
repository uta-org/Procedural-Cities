using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralCities.Import.Editor
{
    /// <summary>
    /// Bakes the PrefabOrientationShowcase scene instance scales into mesh vertices
    /// so every prefab looks correct at transform scale (1,1,1).
    /// Also accounts for any mesh-child localScale that was applied directly to the prefab.
    ///
    /// After running:
    ///  - All _Combined.asset meshes have their vertices multiplied by the bake factor.
    ///  - All prefab mesh-child localScale is reset to (1,1,1).
    ///  - prefab_transforms.json scale entries are set to 1.0.
    ///  - Label Y positions are recalculated from the new mesh bounds.
    ///  - worldHeight / worldWidth in JSON are updated from new bounds.
    ///
    /// Menu: Procedural Cities / Bake Scales Into Prefabs
    /// </summary>
    public static class BakeScalesIntoPrefabs
    {
        private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
        private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";

        #region JSON data classes

        [System.Serializable]
        private class Vec3Json
        {
            public float x, y, z;
        }

        [System.Serializable]
        private class PrefabEntry
        {
            public int index;
            public string name;
            public string sceneName;
            public Vec3Json position;
            public Vec3Json rotation;
            public float scale;
            public float worldHeight;
            public float worldWidth;
            public float forwardAngleY;
        }

        [System.Serializable]
        private class TransformData
        {
            public float playerHeight;
            public string generatedDate;
            public List<PrefabEntry> prefabs;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        //  BAKE SCALES INTO MESH VERTICES
        // ─────────────────────────────────────────────────────
        // [MenuItem("Procedural Cities/Bake Scales Into Prefabs")]
        public static void BakeAll()
        {
            // Load JSON
            string jsonPath = FindJsonPath();
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("[BakeScales] Cannot find prefab_transforms.json");
                return;
            }

            string jsonText = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<TransformData>(jsonText);
            if (data?.prefabs == null)
            {
                Debug.LogError("[BakeScales] Failed to parse prefab_transforms.json");
                return;
            }

            // Build lookup by name
            var entryByName = new Dictionary<string, PrefabEntry>();
            foreach (var p in data.prefabs)
                entryByName[p.name] = p;

            // Find all prefabs
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            int baked = 0, skipped = 0, failed = 0;
            var bakedNames = new List<string>();
            var failedNames = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                EditorUtility.DisplayProgressBar("Baking Scales",
                    $"Processing {prefabName} ({i + 1}/{guids.Length})",
                    (float)i / guids.Length);

                if (!entryByName.TryGetValue(prefabName, out var entry))
                {
                    Debug.LogWarning($"[BakeScales] No JSON entry for '{prefabName}', skipping.");
                    skipped++;
                    continue;
                }

                float posScale = entry.scale;

                // Load prefab contents
                var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabContents == null)
                {
                    Debug.LogWarning($"[BakeScales] Cannot load prefab '{prefabName}'");
                    failedNames.Add(prefabName);
                    failed++;
                    continue;
                }

                // Find mesh child and label child
                Transform meshChild = null;
                Transform labelChild = null;
                foreach (Transform child in prefabContents.transform)
                {
                    if (meshChild == null &&
                        (child.GetComponent<MeshFilter>() != null || child.GetComponent<Renderer>() != null))
                        meshChild = child;
                    if (labelChild == null && child.name.StartsWith("Label_"))
                        labelChild = child;
                }

                if (meshChild == null)
                {
                    Debug.LogWarning($"[BakeScales] No mesh child in '{prefabName}'");
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    skipped++;
                    continue;
                }

                float meshChildScale = meshChild.localScale.x; // uniform scale assumed
                float bakeScale = posScale * meshChildScale;

                // Skip if already approximately 1.0
                if (Mathf.Abs(bakeScale - 1f) < 0.001f)
                {
                    Debug.Log($"[BakeScales] '{prefabName}': bakeScale={bakeScale:F6} ≈ 1.0, skipping.");
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    skipped++;
                    continue;
                }

                // Get the mesh
                var mf = meshChild.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    Debug.LogWarning($"[BakeScales] No mesh on '{prefabName}'");
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                    failedNames.Add(prefabName);
                    failed++;
                    continue;
                }

                var mesh = mf.sharedMesh;
                string meshAssetPath = AssetDatabase.GetAssetPath(mesh);

                // Scale mesh vertices
                var vertices = mesh.vertices;
                for (int v = 0; v < vertices.Length; v++)
                    vertices[v] *= bakeScale;

                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                EditorUtility.SetDirty(mesh);

                // Reset mesh child transform
                meshChild.localScale = Vector3.one;
                meshChild.localPosition = Vector3.zero;

                // Recalculate label position from new mesh bounds
                if (labelChild != null)
                {
                    float newLabelY = mesh.bounds.max.y + 0.05f;
                    labelChild.localPosition = new Vector3(0, newLabelY, 0);
                }

                // Save prefab
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                // Update JSON entry
                float oldWorldH = entry.worldHeight;
                float oldWorldW = entry.worldWidth;
                entry.scale = 1f;
                entry.worldHeight = mesh.bounds.size.y;
                entry.worldWidth = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);

                // Recalculate Y position for scene instance:
                // After baking, the scene instance scale is 1.0, so Y = -bounds.min.y
                entry.position.y = -mesh.bounds.min.y;

                Debug.Log($"[BakeScales] '{prefabName}': bakeScale={bakeScale:F6} " +
                          $"(POS={posScale:F6} × meshChild={meshChildScale:F3}) " +
                          $"→ {vertices.Length} verts scaled. " +
                          $"worldH: {oldWorldH:F3}→{entry.worldHeight:F3}, " +
                          $"worldW: {oldWorldW:F3}→{entry.worldWidth:F3} " +
                          $"(mesh: {meshAssetPath})");
                bakedNames.Add(prefabName);
                baked++;
            }

            // Save JSON with all scales set to 1.0
            data.generatedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string output = JsonUtility.ToJson(data, true);
            File.WriteAllText(jsonPath, output);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            string msg = $"Baked: {baked}, Skipped: {skipped}, Failed: {failed}";
            if (failedNames.Count > 0)
                msg += $"\nFailed: {string.Join(", ", failedNames)}";
            Debug.Log($"[BakeScales] {msg}");
        }

        // ─────────────────────────────────────────────────────
        //  RESET SCENE INSTANCE SCALES TO (1,1,1)
        //  Run this after BakeAll, with PrefabOrientationShowcase
        //  or PrefabShowcase open.
        // ─────────────────────────────────────────────────────
        // [MenuItem("Procedural Cities/Reset Scene Scales to 1 (OrientationShowcase)")]
        public static void ResetOrientationShowcaseScales()
        {
            ResetSceneScales("OrientationShowcase");
        }

        // [MenuItem("Procedural Cities/Reset Scene Scales to 1 (PrefabShowcase)")]
        public static void ResetPrefabShowcaseScales()
        {
            ResetSceneScales("PrefabShowcase");
        }

        private static void ResetSceneScales(string rootName)
        {
            var root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.LogError($"[BakeScales] '{rootName}' not found in scene.");
                return;
            }

            // Load JSON for Y position data
            string jsonPath = FindJsonPath();
            TransformData data = null;
            Dictionary<string, PrefabEntry> entryByName = null;
            if (!string.IsNullOrEmpty(jsonPath))
            {
                string jsonText = File.ReadAllText(jsonPath);
                data = JsonUtility.FromJson<TransformData>(jsonText);
                if (data?.prefabs != null)
                {
                    entryByName = new Dictionary<string, PrefabEntry>();
                    foreach (var p in data.prefabs)
                        entryByName[p.name] = p;
                }
            }

            int reset = 0;

            foreach (Transform child in root.transform)
            {
                string prefabName = ParsePrefabName(child.name);

                Undo.RecordObject(child, "Reset Scale to 1");
                child.localScale = Vector3.one;

                // Use JSON Y position (recalculated during bake)
                if (entryByName != null && entryByName.TryGetValue(prefabName, out var entry))
                {
                    child.localPosition = new Vector3(
                        entry.position.x,
                        entry.position.y,
                        entry.position.z
                    );
                }

                // Reset label scale compensation (no longer needed at parent scale 1)
                foreach (Transform sub in child)
                {
                    if (sub.name.StartsWith("Label_"))
                    {
                        Undo.RecordObject(sub, "Reset Label Scale");
                        sub.localScale = Vector3.one;
                    }
                }

                EditorUtility.SetDirty(child.gameObject);
                reset++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[BakeScales] Reset {reset} instances in '{rootName}' to scale (1,1,1).");
        }

        // ─────────────────────────────────────────────────────
        //  FIX IMPORTED MODEL SCALES
        //  For 3DS/OBJ/FBX models, vertex edits are lost on
        //  reimport. This updates the ModelImporter.globalScale
        //  so the correct size persists.
        // ─────────────────────────────────────────────────────
        // [MenuItem("Procedural Cities/Fix Imported Model Scales (after Bake)")]
        public static void FixImportedModelScales()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            int fixedCount = 0;

            string jsonPath = FindJsonPath();
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("[BakeScales] Cannot find prefab_transforms.json");
                return;
            }

            string jsonText = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<TransformData>(jsonText);
            if (data?.prefabs == null) return;

            var entryByName = new Dictionary<string, PrefabEntry>();
            foreach (var p in data.prefabs)
                entryByName[p.name] = p;

            var importedModelExtensions = new HashSet<string> { ".3ds", ".obj", ".fbx", ".blend", ".dae" };

            // First pass: force reimport all imported models to clear any manual vertex edits
            var importedModels = new List<(string prefabName, string prefabPath, string meshPath, ModelImporter importer)>();

            for (int i = 0; i < guids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                var mf = prefab.GetComponentInChildren<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                string meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                string ext = Path.GetExtension(meshPath).ToLowerInvariant();

                if (!importedModelExtensions.Contains(ext)) continue;
                if (!entryByName.ContainsKey(prefabName)) continue;

                var importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
                if (importer == null) continue;

                importedModels.Add((prefabName, prefabPath, meshPath, importer));

                // Force reimport to clear any in-memory vertex modifications
                Debug.Log($"[BakeScales] Force reimporting '{meshPath}' to clear vertex edits...");
                AssetDatabase.ImportAsset(meshPath, ImportAssetOptions.ForceUpdate);
            }

            // Second pass: now mesh bounds are original, compute and apply correct import scale
            foreach (var (prefabName, prefabPath, meshPath, importer) in importedModels)
            {
                var entry = entryByName[prefabName];

                // Reload mesh after reimport
                var freshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var freshMf = freshPrefab?.GetComponentInChildren<MeshFilter>();
                if (freshMf == null || freshMf.sharedMesh == null) continue;

                float originalHeight = freshMf.sharedMesh.bounds.size.y;
                float targetHeight = entry.worldHeight;

                if (originalHeight < 0.0001f || targetHeight < 0.0001f) continue;

                float scaleFactor = targetHeight / originalHeight;
                float oldScale = importer.globalScale;
                float newScale = oldScale * scaleFactor;

                Debug.Log($"[BakeScales] '{prefabName}': imported model '{meshPath}' " +
                          $"importer.globalScale {oldScale:F6} → {newScale:F6} " +
                          $"(originalH={originalHeight:F4} → targetH={targetHeight:F4})");

                importer.globalScale = newScale;
                importer.SaveAndReimport();
                fixedCount++;
            }

            Debug.Log($"[BakeScales] Fixed {fixedCount} imported model scales.");
        }

        // ─────────────────────────────────────────────────────
        //  CORRECT PREFAB PROPORTIONS TO REAL-WORLD SIZES
        //  Reads target heights from GenerateLowPolyModels comments
        //  and scales mesh vertices uniformly so each object matches
        //  realistic proportions relative to a 1.7m player.
        // ─────────────────────────────────────────────────────
        // Target heights (meters) from GenerateLowPolyModels.cs design dimensions
        private static readonly Dictionary<string, float> TargetHeights = new Dictionary<string, float>
        {
            { "Awning",             1.50f },
            { "Bed",                0.55f },
            { "Bench",              0.85f },
            { "Bush",               1.00f },
            { "Chair",              0.90f },
            { "ChoppingBoard",      0.10f },
            { "Clock",              0.30f },
            { "Computer",           0.45f },
            { "ComputerUser",       0.75f },
            { "Cup",                0.10f },
            { "Dispenser",          1.10f },
            { "Door",               2.10f },
            { "DoorFrame",          2.20f },
            { "Elevator",           2.40f },
            { "Fence",              1.20f },
            { "FireHydrant",        0.60f },
            { "Fountain",           1.00f },
            { "Fridge",             1.80f },
            { "Glass",              0.15f },
            { "Grass",              0.15f },
            { "Hanger",             1.70f },
            { "Hanger1",            0.40f },
            { "Kettle",             0.25f },
            { "Kitchen2",           2.20f },
            { "Kitchen3",           0.90f },
            { "Kitchen4",           0.90f },
            { "Lamp0",              0.40f },
            { "Lamp1",              1.50f },
            { "Lamp2",              1.50f },
            { "Lamp3",              0.60f },
            { "Lamp4",              1.50f },
            { "Lamppost",           4.50f },
            { "LargeTable",         0.75f },
            { "Locker",             1.80f },
            { "Mirror",             0.80f },
            { "Mirror1",            0.80f },
            { "Mirror2",            0.80f },
            { "OfficeChair",        1.20f },
            { "OfficeCubicle",      1.50f },
            { "OfficeMeetingTable", 0.75f },
            { "OfficeTable",        0.75f },
            { "OfficeWhiteboard",   1.20f },
            { "Oven",               0.85f },
            { "Pan0",               0.08f },
            { "Pan1",               0.10f },
            { "RestaurantChair",    0.90f },
            { "RestaurantTable",    0.75f },
            { "RooftopAc",          0.80f },
            { "RooftopSolar",       0.40f },
            { "RubbishBin",         0.90f },
            { "Shelf",              1.80f },
            { "Shelf1",             1.80f },
            { "Shelf2",             1.80f },
            { "Shelf3",             1.80f },
            { "Shelf4",             1.80f },
            { "Shelf5",             1.80f },
            { "Sink",               0.85f },
            { "SmallTable",         0.50f },
            { "Sofa",               0.85f },
            { "Sofa1",              0.85f },
            { "Stair",              3.00f },
            { "StoreShelf",         2.00f },
            { "Toaster",            0.20f },
            { "Toilet",             0.40f },
            { "Toilet1",            0.40f },
            { "TrafficLight",       5.00f },
            { "TrashBox",           0.70f },
            { "TrashCan",           0.35f },
            { "Tv",                 0.50f },
            { "Vase",               0.30f },
            { "Wardrobe",           2.00f },
        };

        // [MenuItem("Procedural Cities/Correct Prefab Proportions (Real-World Sizes)")]
        public static void CorrectPrefabProportions()
        {
            string jsonPath = FindJsonPath();
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("[CorrectProportions] Cannot find prefab_transforms.json");
                return;
            }

            string jsonText = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<TransformData>(jsonText);
            if (data?.prefabs == null)
            {
                Debug.LogError("[CorrectProportions] Failed to parse prefab_transforms.json");
                return;
            }

            var entryByName = new Dictionary<string, PrefabEntry>();
            foreach (var p in data.prefabs)
                entryByName[p.name] = p;

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            var importedModelExtensions = new HashSet<string> { ".3ds", ".obj", ".fbx", ".blend", ".dae" };
            int corrected = 0, skipped = 0, failed = 0;
            var correctedNames = new List<string>();
            var failedNames = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                EditorUtility.DisplayProgressBar("Correcting Proportions",
                    $"Processing {prefabName} ({i + 1}/{guids.Length})",
                    (float)i / guids.Length);

                if (!TargetHeights.TryGetValue(prefabName, out float targetH))
                {
                    Debug.Log($"[CorrectProportions] No target height for '{prefabName}', skipping.");
                    skipped++;
                    continue;
                }

                if (!entryByName.TryGetValue(prefabName, out var entry))
                {
                    Debug.LogWarning($"[CorrectProportions] No JSON entry for '{prefabName}', skipping.");
                    skipped++;
                    continue;
                }

                float currentH = entry.worldHeight;
                if (currentH < 0.0001f)
                {
                    Debug.LogWarning($"[CorrectProportions] '{prefabName}' has near-zero height ({currentH}), skipping.");
                    failedNames.Add(prefabName + " (zero height)");
                    failed++;
                    continue;
                }

                float factor = targetH / currentH;

                // Skip if already within 2% tolerance
                if (Mathf.Abs(factor - 1f) < 0.02f)
                {
                    Debug.Log($"[CorrectProportions] '{prefabName}': factor={factor:F4} ≈ 1.0, skipping.");
                    skipped++;
                    continue;
                }

                // Check if mesh is an imported model
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) { failed++; continue; }

                var mf = prefab.GetComponentInChildren<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) { failed++; continue; }

                string meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                string ext = Path.GetExtension(meshPath).ToLowerInvariant();

                if (importedModelExtensions.Contains(ext))
                {
                    // Imported model: adjust ModelImporter.globalScale
                    var importer = AssetImporter.GetAtPath(meshPath) as ModelImporter;
                    if (importer == null) { failed++; continue; }

                    float oldScale = importer.globalScale;
                    importer.globalScale = oldScale * factor;

                    Debug.Log($"[CorrectProportions] '{prefabName}' (imported): globalScale {oldScale:F6} → {importer.globalScale:F6} " +
                              $"(factor={factor:F4}, currentH={currentH:F4} → targetH={targetH:F4})");

                    importer.SaveAndReimport();
                }
                else
                {
                    // _Combined.asset or runtime mesh: scale vertices
                    var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (prefabContents == null) { failed++; continue; }

                    Transform meshChild = null;
                    Transform labelChild = null;
                    foreach (Transform child in prefabContents.transform)
                    {
                        if (meshChild == null &&
                            (child.GetComponent<MeshFilter>() != null || child.GetComponent<Renderer>() != null))
                            meshChild = child;
                        if (labelChild == null && child.name.StartsWith("Label_"))
                            labelChild = child;
                    }

                    if (meshChild == null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabContents);
                        failed++;
                        continue;
                    }

                    var meshFilter = meshChild.GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabContents);
                        failed++;
                        continue;
                    }

                    var mesh = meshFilter.sharedMesh;
                    var vertices = mesh.vertices;
                    for (int v = 0; v < vertices.Length; v++)
                        vertices[v] *= factor;

                    mesh.vertices = vertices;
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    EditorUtility.SetDirty(mesh);

                    // Update label position
                    if (labelChild != null)
                    {
                        float newLabelY = mesh.bounds.max.y + 0.05f;
                        labelChild.localPosition = new Vector3(0, newLabelY, 0);
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }

                // Update JSON entry
                float newWorldH = currentH * factor;
                float newWorldW = entry.worldWidth * factor;
                entry.worldHeight = newWorldH;
                entry.worldWidth = newWorldW;

                // Recalculate Y position: need to reload mesh bounds for the corrected mesh
                {
                    var freshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    var freshMf = freshPrefab?.GetComponentInChildren<MeshFilter>();
                    if (freshMf != null && freshMf.sharedMesh != null)
                    {
                        entry.position.y = -freshMf.sharedMesh.bounds.min.y;
                        entry.worldHeight = freshMf.sharedMesh.bounds.size.y;
                        entry.worldWidth = Mathf.Max(freshMf.sharedMesh.bounds.size.x, freshMf.sharedMesh.bounds.size.z);
                    }
                }

                Debug.Log($"[CorrectProportions] '{prefabName}': factor={factor:F4} " +
                          $"worldH: {currentH:F3}→{newWorldH:F3}, worldW: {entry.worldWidth / factor:F3}→{newWorldW:F3}");
                correctedNames.Add($"{prefabName} ({currentH:F3}→{newWorldH:F3}m)");
                corrected++;
            }

            // Save JSON
            data.generatedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string output = JsonUtility.ToJson(data, true);
            File.WriteAllText(jsonPath, output);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            string msg = $"Corrected: {corrected}, Skipped: {skipped}, Failed: {failed}";
            Debug.Log($"[CorrectProportions] {msg}");
            if (correctedNames.Count > 0)
                Debug.Log($"[CorrectProportions] Corrected objects:\n" + string.Join("\n", correctedNames));
            if (failedNames.Count > 0)
                Debug.Log($"[CorrectProportions] Failed: {string.Join(", ", failedNames)}");
        }

        // ─────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────
        private static string ParsePrefabName(string goName)
        {
            if (goName.Contains("] "))
                return goName.Substring(goName.IndexOf("] ") + 2);
            return goName;
        }

        private static string FindJsonPath()
        {
            var guids = AssetDatabase.FindAssets("prefab_transforms", new[] { "Packages" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("prefab_transforms.json"))
                    return Path.GetFullPath(path);
            }

            const string knownPath =
                "Packages/dev.z3nth10n.proceduralcities.import/Resources/prefab_transforms.json";
            string fullPath = Path.GetFullPath(knownPath);
            if (File.Exists(fullPath))
                return fullPath;

            return null;
        }
    }
}
