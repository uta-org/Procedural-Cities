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
        [MenuItem("Procedural Cities/Bake Scales Into Prefabs")]
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
            EditorUtility.DisplayDialog("Bake Scales Into Prefabs", msg, "OK");
        }

        // ─────────────────────────────────────────────────────
        //  RESET SCENE INSTANCE SCALES TO (1,1,1)
        //  Run this after BakeAll, with PrefabOrientationShowcase
        //  or PrefabShowcase open.
        // ─────────────────────────────────────────────────────
        [MenuItem("Procedural Cities/Reset Scene Scales to 1 (OrientationShowcase)")]
        public static void ResetOrientationShowcaseScales()
        {
            ResetSceneScales("OrientationShowcase");
        }

        [MenuItem("Procedural Cities/Reset Scene Scales to 1 (PrefabShowcase)")]
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
        [MenuItem("Procedural Cities/Fix Imported Model Scales (after Bake)")]
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
            if (fixedCount > 0)
                EditorUtility.DisplayDialog("Fix Imported Models",
                    $"Fixed {fixedCount} imported model import scales.", "OK");
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
