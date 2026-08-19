using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds a disabled focal <see cref="Light"/> to the three table/floor lamp
/// prefabs — the ones <c>LampInteractionManager</c> lets the player toggle
/// with E (see <c>GenerateLampProps.cs</c>, which built the meshes but
/// deliberately skipped the Light component for this later phase). Wall
/// sconces and ceiling fixtures are intentionally NOT touched here — those
/// get their own focal lights, auto-switched by room entry rather than by
/// the player, in a separate phase.
/// Menu: Tools / Procedural Cities / Add Lamp Focal Lights
/// </summary>
public static class AddLampFocalLights
{
    private static readonly (string PrefabName, float Intensity, float Range)[] Targets =
    {
        ("TableLampRound", 1.2f, 3f),
        ("TableLampCone", 1.2f, 3f),
        ("Lamp1", 1.8f, 5f),
    };

    [MenuItem("Tools/Procedural Cities/Add Lamp Focal Lights")]
    public static void Generate()
    {
        var touched = 0;
        foreach (var (prefabName, intensity, range) in Targets)
        {
            if (AddFocalLight(prefabName, intensity, range))
                touched++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AddLampFocalLights] Added/verified disabled focal lights on {touched}/{Targets.Length} lamp prefabs.");
    }

    private static bool AddFocalLight(string prefabName, float intensity, float range)
    {
        var path = FindPrefabPath(prefabName);
        if (path == null)
        {
            Debug.LogWarning($"[AddLampFocalLights] Prefab not found: {prefabName}");
            return false;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponentInChildren<Light>(true) != null)
                return true; // already has a focal light (e.g. re-running the menu item).

            var bounds = ComputeLocalBounds(root);
            var lightGO = new GameObject("FocalLight");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y - 0.03f, bounds.center.z);

            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f); // matches LP_Lamp_ShadeWarm's warm tone
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None; // several of these can be on in one house at once
            light.enabled = false; // toggled by LampInteractionManager (E key)

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// Combined mesh-renderer bounds in the prefab root's local space. The
    /// root GameObject from LoadPrefabContents sits at world-space identity,
    /// so this doubles as a light-placement anchor near the lamp's shade/bulb
    /// without hand-measuring each mesh's geometry.
    /// </summary>
    private static Bounds ComputeLocalBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.one * 0.2f);

        var worldBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        var localCenter = root.transform.InverseTransformPoint(worldBounds.center);
        return new Bounds(localCenter, worldBounds.size);
    }

    private static string FindPrefabPath(string prefabName)
    {
        var guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        foreach (var guid in guids)
        {
            var candidatePath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(candidatePath) == prefabName)
                return candidatePath;
        }
        return null;
    }
}
