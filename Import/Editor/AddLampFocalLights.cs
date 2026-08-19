using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds a disabled focal <see cref="Light"/> to every lamp/fixture prefab
/// <c>GenerateLampProps.cs</c> built (which deliberately skipped the Light
/// component for this later phase) plus the pre-existing <c>Lamp1</c> floor
/// lamp mesh. Table/floor lamps are the ones <c>LampInteractionManager</c>
/// lets the player toggle with E; wall sconces and ceiling fixtures instead
/// get switched on automatically by the room entry trigger (see
/// <c>InteriorStreamingManager.EnsureRoomDecorationsLoaded</c>) — this script
/// only bakes the (disabled) Light component, the same for both groups.
/// Menu: Tools / Procedural Cities / Add Lamp Focal Lights
/// </summary>
public static class AddLampFocalLights
{
    private static readonly (string PrefabName, float Intensity, float Range, bool AnchorAtTop)[] Targets =
    {
        // Table/floor lamps: geometry rises from a floor/table-anchored base,
        // so the shade sits near the top of the combined bounds.
        ("TableLampRound", 1.2f, 3f, true),
        ("TableLampCone", 1.2f, 3f, true),
        ("Lamp1", 1.8f, 5f, true),
        // Wall sconces / ceiling fixtures: geometry hangs or projects away
        // from a wall/ceiling mount point at local origin, so the shade is
        // closer to the combined bounds' centre than to either extreme.
        ("WallSconceDrum", 1.3f, 3.5f, false),
        ("WallSconceTorch", 1.3f, 3.5f, false),
        ("CeilingFlushDisc", 2.2f, 6f, false),
        ("CeilingPendant", 2.2f, 6f, false),
        ("CeilingPanel", 2.2f, 6f, false),
    };

    [MenuItem("Tools/Procedural Cities/Add Lamp Focal Lights")]
    public static void Generate()
    {
        var touched = 0;
        foreach (var (prefabName, intensity, range, anchorAtTop) in Targets)
        {
            if (AddFocalLight(prefabName, intensity, range, anchorAtTop))
                touched++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AddLampFocalLights] Added/verified disabled focal lights on {touched}/{Targets.Length} lamp prefabs.");
    }

    private static bool AddFocalLight(string prefabName, float intensity, float range, bool anchorAtTop)
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
            var anchorY = anchorAtTop ? bounds.max.y - 0.03f : bounds.center.y;
            var lightGO = new GameObject("FocalLight");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(bounds.center.x, anchorY, bounds.center.z);

            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f); // matches LP_Lamp_ShadeWarm's warm tone
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None; // several of these can be on in one house at once
            light.enabled = false; // table/floor: toggled by LampInteractionManager (E).
                                    // wall/ceiling: enabled by the room entry trigger.

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
