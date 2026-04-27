using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProceduralCities.Import.Editor
{
    public static class RestorePrefabTransforms
    {
        private const string JsonResourceName = "prefab_transforms";
        private const string ShowcaseRootName = "PrefabShowcase";

        [System.Serializable]
        private class Vec3Json
        {
            public float x,
                y,
                z;
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
        }

        [System.Serializable]
        private class TransformData
        {
            public float playerHeight;
            public string generatedDate;
            public List<PrefabEntry> prefabs;
        }

        [MenuItem("Procedural Cities/Restore Prefab Transforms (Showcase Scene)")]
        public static void RestoreFromJson()
        {
            var root = GameObject.Find(ShowcaseRootName);
            if (root == null)
            {
                Debug.LogError(
                    $"[RestorePrefabTransforms] '{ShowcaseRootName}' not found in scene."
                );
                return;
            }

            var jsonAsset = Resources.Load<TextAsset>(JsonResourceName);
            if (jsonAsset == null)
            {
                Debug.LogError(
                    $"[RestorePrefabTransforms] Resource '{JsonResourceName}' not found. Ensure prefab_transforms.json is in a Resources/ folder."
                );
                return;
            }

            var data = JsonUtility.FromJson<TransformData>(jsonAsset.text);
            if (data == null || data.prefabs == null || data.prefabs.Count == 0)
            {
                Debug.LogError("[RestorePrefabTransforms] JSON parse failed or empty.");
                return;
            }

            var byName = new Dictionary<string, PrefabEntry>();
            foreach (var p in data.prefabs)
                byName[p.sceneName] = p;

            int applied = 0;
            int skipped = 0;
            foreach (Transform child in root.transform)
            {
                PrefabEntry entry;
                if (!byName.TryGetValue(child.name, out entry))
                {
                    Debug.LogWarning(
                        $"[RestorePrefabTransforms] No JSON entry for '{child.name}', skipping."
                    );
                    skipped++;
                    continue;
                }

                child.localPosition = new Vector3(
                    entry.position.x,
                    entry.position.y,
                    entry.position.z
                );
                child.localEulerAngles = new Vector3(
                    entry.rotation.x,
                    entry.rotation.y,
                    entry.rotation.z
                );
                child.localScale = Vector3.one * entry.scale;

                // Fix label scale so it stays at lossy (1,1,1)
                foreach (Transform sub in child)
                {
                    if (sub.name.StartsWith("Label_"))
                    {
                        float labelScale = 1f / entry.scale;
                        sub.localScale = Vector3.one * labelScale;
                    }
                }

                applied++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            Debug.Log(
                $"[RestorePrefabTransforms] Done. Applied: {applied}, Skipped: {skipped}. "
                    + $"JSON generated: {data.generatedDate}, Player height: {data.playerHeight}m"
            );
        }
    }
}
