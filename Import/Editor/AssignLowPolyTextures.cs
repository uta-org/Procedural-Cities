using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Assigns downloaded PBR textures from ambientCG to LowPoly materials.
/// Maps each material to a texture set + tint color.
/// Menu: Tools / Procedural Cities / Assign LowPoly Textures
/// </summary>
public class AssignLowPolyTextures : Editor
{
    // Base path for textures (relative to Assets or Packages)
    static readonly string[] TextureSearchPaths = new[]
    {
        "Packages/dev.z3nth10n.proceduralcities.import/Textures/LowPoly",
        "Assets/Textures/LowPoly"
    };

    static readonly string[] MaterialSearchPaths = new[]
    {
        "Packages/dev.z3nth10n.proceduralcities.import/Models/LowPoly/Materials",
        "Assets/LowPoly/Materials"
    };

    static string ShaderName = "Procedural Cities/LowPoly PBR";

    // =====================================================
    // TEXTURE SET MAPPING
    // Each entry: material name -> (ambientCG_ID, category, tintColor, metallic, smoothness, tiling, triplanar)
    // =====================================================
    struct TexAssignment
    {
        public string textureId;    // e.g. "Wood051"
        public string category;     // e.g. "Wood"
        public Color tint;
        public float metallic;
        public float smoothness;
        public float tiling;
        public bool triplanar;
        public bool emission;
        public Color emissionColor;
        public float emissionIntensity;
    }

    static TexAssignment Tex(string id, string cat, Color tint, float met = 0, float smooth = 0.3f,
        float tiling = 1f, bool triplanar = false, bool emission = false, Color? emColor = null, float emIntensity = 1f)
    {
        return new TexAssignment
        {
            textureId = id, category = cat, tint = tint,
            metallic = met, smoothness = smooth, tiling = tiling,
            triplanar = triplanar, emission = emission,
            emissionColor = emColor ?? Color.black, emissionIntensity = emIntensity
        };
    }

    static Color C(float r, float g, float b) => new Color(r, g, b, 1);

    static readonly Dictionary<string, TexAssignment> MaterialMap = new Dictionary<string, TexAssignment>
    {
        // ===== WOOD =====
        { "LP_Wood_Table",     Tex("Wood051", "Wood", C(0.85f, 0.78f, 0.65f), tiling: 2) },
        { "LP_Wood_Shelf",     Tex("Wood051", "Wood", C(0.88f, 0.75f, 0.6f), tiling: 2) },
        { "LP_Wood_Fence",     Tex("Wood058", "Wood", C(0.8f, 0.7f, 0.55f), tiling: 1.5f) },
        { "LP_Wood_Bench",     Tex("Wood058", "Wood", C(0.9f, 0.75f, 0.55f), tiling: 1.5f) },
        { "LP_Wood_Desk",      Tex("Wood051", "Wood", C(0.82f, 0.72f, 0.58f), tiling: 2) },
        { "LP_Wood_Cutting",   Tex("Wood051", "Wood", C(0.9f, 0.8f, 0.6f), tiling: 3) },
        { "LP_Wood_Chair",     Tex("Wood066", "Wood", C(0.85f, 0.72f, 0.55f), tiling: 2) },
        { "LP_Wood_Dark",      Tex("Wood049", "Wood", C(0.55f, 0.4f, 0.25f), tiling: 2) },
        { "LP_Wood_Door",      Tex("Wood049", "Wood", C(0.75f, 0.55f, 0.4f), tiling: 1) },
        { "LP_Wood_Frame",     Tex("Wood049", "Wood", C(0.7f, 0.5f, 0.3f), tiling: 2) },
        { "LP_Wood_Wardrobe",  Tex("Wood049", "Wood", C(0.72f, 0.52f, 0.38f), tiling: 1.5f) },
        { "LP_Wood_Bed",       Tex("Wood066", "Wood", C(0.78f, 0.6f, 0.42f), tiling: 1.5f) },
        { "KM_ShelfWood",      Tex("Wood066", "Wood", C(0.85f, 0.7f, 0.5f), tiling: 2) },

        // ===== METAL =====
        { "LP_Chrome",           Tex("Metal032", "Metal", C(0.9f, 0.9f, 0.92f), 0.85f, 0.8f, 2) },
        { "LP_Metal_Handle",     Tex("Metal032", "Metal", C(0.8f, 0.78f, 0.75f), 0.7f, 0.5f, 3) },
        { "LP_Metal_Kettle",     Tex("Metal032", "Metal", C(0.85f, 0.85f, 0.87f), 0.8f, 0.7f, 2) },
        { "LP_Metal_Sink",       Tex("Metal032", "Metal", C(0.85f, 0.85f, 0.87f), 0.7f, 0.7f, 2) },
        { "LP_Appliance_Silver", Tex("Metal032", "Metal", C(0.85f, 0.85f, 0.87f), 0.6f, 0.5f, 1.5f) },
        { "KM_Handle",           Tex("Metal032", "Metal", C(0.85f, 0.85f, 0.87f), 0.85f, 0.8f, 3) },
        { "KM_Metal",            Tex("Metal032", "Metal", C(0.8f, 0.8f, 0.82f), 0.7f, 0.6f, 2) },
        { "KM_FilterGrille",     Tex("Metal032", "Metal", C(0.7f, 0.7f, 0.72f), 0.6f, 0.4f, 4) },
        { "LP_Metal_Black",      Tex("Metal049A", "Metal", C(0.3f, 0.3f, 0.3f), 0.5f, 0.3f, 2) },
        { "LP_Metal_Pan",        Tex("Metal049A", "Metal", C(0.5f, 0.5f, 0.52f), 0.6f, 0.5f, 3) },
        { "LP_Metal_Lamppost",   Tex("Metal049A", "Metal", C(0.4f, 0.4f, 0.42f), 0.5f, 0.4f, 1.5f) },
        { "LP_Metal_DarkGray",   Tex("Metal049A", "Metal", C(0.5f, 0.5f, 0.5f), 0.7f, 0.6f, 2) },
        { "KM_Burner",           Tex("Metal049A", "Metal", C(0.45f, 0.45f, 0.48f), 0.5f, 0.4f, 4) },
        { "KM_StoveSurface",     Tex("Metal049A", "Metal", C(0.15f, 0.15f, 0.15f), 0.3f, 0.7f, 2) },
        { "LP_Metal_AC",         Tex("Metal048A", "Metal", C(0.8f, 0.82f, 0.85f), 0.4f, 0.3f, 2) },
        { "LP_Metal_Elevator",   Tex("Metal048A", "Metal", C(0.8f, 0.8f, 0.82f), 0.6f, 0.5f, 1) },
        { "LP_Metal_Locker",     Tex("Metal048A", "Metal", C(0.7f, 0.72f, 0.75f), 0.4f, 0.3f, 1.5f) },
        { "LP_Metal_TrashBox",   Tex("Metal048A", "Metal", C(0.65f, 0.65f, 0.68f), 0.4f, 0.3f, 2) },
        { "LP_Metal_DoorElev",   Tex("Metal048A", "Metal", C(0.75f, 0.75f, 0.78f), 0.7f, 0.6f, 1) },

        // ===== FABRIC =====
        { "LP_Fabric_Gray",        Tex("Fabric030", "Fabric", C(0.65f, 0.62f, 0.6f), tiling: 3) },
        { "LP_Fabric_DarkGray",    Tex("Fabric030", "Fabric", C(0.5f, 0.48f, 0.45f), tiling: 3) },
        { "LP_Fabric_Brown",       Tex("Fabric030", "Fabric", C(0.65f, 0.5f, 0.35f), tiling: 3) },
        { "LP_Fabric_OfficeChair", Tex("Fabric066", "Fabric", C(0.3f, 0.3f, 0.35f), tiling: 4) },
        { "LP_Fabric_Cubicle",     Tex("Fabric066", "Fabric", C(0.75f, 0.75f, 0.78f), tiling: 2) },
        { "LP_Fabric_Pillow",      Tex("Fabric061", "Fabric", C(0.95f, 0.93f, 0.9f), tiling: 2) },
        { "LP_Fabric_Mattress",    Tex("Fabric061", "Fabric", C(0.92f, 0.9f, 0.87f), tiling: 2) },
        { "LP_Fabric_LampShade",   Tex("Fabric061", "Fabric", C(0.95f, 0.9f, 0.8f), tiling: 3) },
        { "LP_Fabric_Blue",        Tex("Fabric066", "Fabric", C(0.4f, 0.55f, 0.8f), tiling: 3) },
        { "LP_Fabric_Red",         Tex("Fabric066", "Fabric", C(0.85f, 0.25f, 0.2f), tiling: 3) },
        { "LP_Fabric_RestChair",   Tex("Fabric066", "Fabric", C(0.75f, 0.2f, 0.2f), tiling: 3) },

        // ===== CONCRETE & STONE =====
        { "LP_Concrete",     Tex("Concrete034", "Stone", C(0.85f, 0.83f, 0.8f), tiling: 1, triplanar: true) },
        { "LP_Stone_Gray",   Tex("Concrete042A", "Stone", C(0.8f, 0.78f, 0.75f), tiling: 1, triplanar: true) },

        // ===== CERAMIC =====
        { "LP_Ceramic_White",  Tex("Tiles107", "Ceramic", C(0.95f, 0.95f, 0.93f), 0, 0.7f, 2) },
        { "LP_Ceramic_Inner",  Tex("Tiles107", "Ceramic", C(0.9f, 0.9f, 0.88f), 0, 0.5f, 2) },
        { "LP_Ceramic_Vase",   Tex("Tiles107", "Ceramic", C(0.8f, 0.55f, 0.35f), 0.1f, 0.6f, 3) },

        // ===== GRANITE / COUNTERTOP =====
        { "LP_Granite",        Tex("Tiles074", "Ceramic", C(0.6f, 0.58f, 0.55f), 0.1f, 0.6f, 2) },
        { "KM_Countertop",     Tex("Tiles074", "Ceramic", C(0.6f, 0.58f, 0.55f), 0.1f, 0.6f, 2) },
        { "KM_DarkInterior",   Tex("Tiles074", "Ceramic", C(0.35f, 0.33f, 0.3f), 0, 0.3f, 2) },

        // ===== PLASTIC =====
        { "LP_Plastic_White",     Tex("Plastic006", "Plastic", C(0.92f, 0.92f, 0.9f), 0, 0.3f, 2) },
        { "LP_Plastic_LightGray", Tex("Plastic010", "Plastic", C(0.85f, 0.85f, 0.87f), 0, 0.3f, 2) },
        { "LP_Plastic_Blue",      Tex("Plastic006", "Plastic", C(0.35f, 0.6f, 0.9f), 0, 0.3f, 2) },
        { "LP_Plastic_Red",       Tex("Plastic006", "Plastic", C(0.85f, 0.25f, 0.25f), 0, 0.3f, 2) },
        { "LP_Plastic_Green_Bin", Tex("Plastic006", "Plastic", C(0.25f, 0.6f, 0.25f), 0, 0.3f, 2) },

        // ===== APPLIANCE / CABINET (white painted) =====
        { "LP_Appliance_White",  Tex("Plastic010", "Plastic", C(0.95f, 0.95f, 0.93f), 0, 0.35f, 1.5f) },
        { "LP_Cabinet_White",    Tex("Plastic010", "Plastic", C(0.92f, 0.9f, 0.87f), 0, 0.3f, 1.5f) },
        { "KM_Cabinet_Body",     Tex("Plastic010", "Plastic", C(0.92f, 0.9f, 0.87f), 0, 0.3f, 1.5f) },
        { "KM_FridgeBody",       Tex("Plastic010", "Plastic", C(0.95f, 0.95f, 0.96f), 0.3f, 0.5f, 1.5f) },

        // ===== NATURE =====
        { "LP_Grass_Green",  Tex("Ground037", "Nature", C(0.5f, 0.85f, 0.35f), tiling: 1, triplanar: true) },
        { "LP_Leaf_Green",   Tex("Ground037", "Nature", C(0.4f, 0.75f, 0.3f), tiling: 1.5f) },

        // ===== SPECIAL - keep flat color, just switch shader =====
        // Glass
        { "LP_Glass_Clear",  Tex("", "", C(0.85f, 0.9f, 0.92f), 0.1f, 0.9f) },
        { "KM_Glass",        Tex("", "", C(0.7f, 0.75f, 0.8f), 0.1f, 0.85f) },
        { "LP_Oven_Glass",   Tex("", "", C(0.15f, 0.12f, 0.1f), 0.1f, 0.8f) },

        // Mirror
        { "LP_Mirror_Silver", Tex("", "", C(0.9f, 0.92f, 0.94f), 0.8f, 0.95f) },

        // Water
        { "LP_Water", Tex("", "", C(0.4f, 0.6f, 0.8f), 0, 0.9f) },

        // TV / Screens
        { "LP_TV_Frame",  Tex("", "", C(0.08f, 0.08f, 0.08f), 0.3f, 0.6f) },
        { "LP_TV_Screen",  Tex("", "", C(0.1f, 0.15f, 0.2f), 0, 0.9f, emission: true,
            emColor: new Color(0.1f, 0.15f, 0.25f), emIntensity: 0.5f) },
        { "KM_Display",    Tex("", "", C(0.1f, 0.3f, 0.1f), 0, 0.3f, emission: true,
            emColor: new Color(0.1f, 0.4f, 0.1f), emIntensity: 0.8f) },

        // Emissive lights
        { "LP_Light_Warm",   Tex("", "", C(0.95f, 0.9f, 0.7f), 0, 0.3f, emission: true,
            emColor: new Color(1f, 0.9f, 0.6f), emIntensity: 2f) },
        { "LP_Light_Green",  Tex("", "", C(0.1f, 0.85f, 0.15f), 0, 0.3f, emission: true,
            emColor: new Color(0.1f, 0.9f, 0.15f), emIntensity: 2f) },
        { "LP_Light_Red",    Tex("", "", C(0.9f, 0.1f, 0.1f), 0, 0.3f, emission: true,
            emColor: new Color(1f, 0.1f, 0.1f), emIntensity: 2f) },
        { "LP_Light_Yellow", Tex("", "", C(0.9f, 0.8f, 0.1f), 0, 0.3f, emission: true,
            emColor: new Color(1f, 0.85f, 0.1f), emIntensity: 2f) },
        { "KM_LightStrip",   Tex("", "", C(0.95f, 0.92f, 0.8f), 0, 0.3f, emission: true,
            emColor: new Color(1f, 0.95f, 0.8f), emIntensity: 1.5f) },

        // Whiteboard / clock / solar
        { "LP_Whiteboard",  Tex("", "", C(0.96f, 0.96f, 0.95f), 0, 0.7f) },
        { "LP_Clock_Face",  Tex("", "", C(0.95f, 0.95f, 0.93f), 0, 0.3f) },
        { "LP_Solar_Panel", Tex("", "", C(0.12f, 0.15f, 0.25f), 0.3f, 0.6f) },
        { "LP_Paint_Red",   Tex("", "", C(0.75f, 0.1f, 0.1f), 0, 0.3f) },
    };

    // ReSharper disable once UnusedMember.Local
    // [MenuItem("Tools/uzProceduralCities/Assign LowPoly Textures")]
    static void AssignAll()
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[TexAssign] Shader '{ShaderName}' not found. Make sure LowPolyPBR.shader is imported.");
            return;
        }

        // Find all materials
        var allMats = new Dictionary<string, Material>();
        foreach (var searchPath in MaterialSearchPaths)
        {
            if (!AssetDatabase.IsValidFolder(searchPath)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { searchPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !allMats.ContainsKey(mat.name))
                    allMats[mat.name] = mat;
            }
        }

        Debug.Log($"[TexAssign] Found {allMats.Count} materials");

        // Cache loaded textures by (category, textureId, mapType)
        var texCache = new Dictionary<string, Texture2D>();

        int assigned = 0;
        int skipped = 0;
        int notFound = 0;

        foreach (var kvp in MaterialMap)
        {
            string matName = kvp.Key;
            var assign = kvp.Value;

            if (!allMats.TryGetValue(matName, out var mat))
            {
                Debug.LogWarning($"[TexAssign] Material not found: {matName}");
                notFound++;
                continue;
            }

            mat.shader = shader;
            mat.color = assign.tint;
            mat.SetFloat("_Metallic", assign.metallic);
            mat.SetFloat("_Glossiness", assign.smoothness);
            mat.SetVector("_Tiling", new Vector4(assign.tiling, assign.tiling, 0, 0));

            // Triplanar
            if (assign.triplanar)
            {
                mat.EnableKeyword("_TRIPLANAR");
                mat.SetFloat("_UseTriplanar", 1);
                mat.SetFloat("_TriplanarScale", assign.tiling);
            }
            else
            {
                mat.DisableKeyword("_TRIPLANAR");
                mat.SetFloat("_UseTriplanar", 0);
            }

            // Emission
            if (assign.emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetFloat("_UseEmission", 1);
                mat.SetColor("_EmissionColor", assign.emissionColor);
                mat.SetFloat("_EmissionIntensity", assign.emissionIntensity);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetFloat("_UseEmission", 0);
            }

            // Assign textures if we have a texture set
            if (!string.IsNullOrEmpty(assign.textureId))
            {
                AssignTexture(mat, "_MainTex",      assign, "Color",       texCache);
                AssignTexture(mat, "_BumpMap",       assign, "NormalGL",    texCache);
                AssignTexture(mat, "_ParallaxMap",   assign, "Displacement", texCache);
                AssignTexture(mat, "_OcclusionMap",  assign, "AmbientOcclusion", texCache);
            }

            EditorUtility.SetDirty(mat);
            assigned++;
        }

        // Handle unmapped materials (just switch shader, keep color)
        foreach (var kvp in allMats)
        {
            if (!MaterialMap.ContainsKey(kvp.Key))
            {
                var mat = kvp.Value;
                var oldColor = mat.color;
                var oldMet = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0;
                var oldSmooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.3f;
                mat.shader = shader;
                mat.color = oldColor;
                mat.SetFloat("_Metallic", oldMet);
                mat.SetFloat("_Glossiness", oldSmooth);
                mat.SetVector("_Tiling", new Vector4(1, 1, 0, 0));
                EditorUtility.SetDirty(mat);
                skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TexAssign] Done: {assigned} assigned, {skipped} shader-only, {notFound} not found");
        // EditorUtility.DisplayDialog("Assign LowPoly Textures",
        //     $"Assigned: {assigned}\nShader-only: {skipped}\nNot found: {notFound}", "OK");
    }

    static void AssignTexture(Material mat, string property, TexAssignment assign, string mapType,
        Dictionary<string, Texture2D> cache)
    {
        string key = $"{assign.category}/{assign.textureId}_{mapType}";
        if (!cache.TryGetValue(key, out var tex))
        {
            tex = FindTexture(assign.textureId, assign.category, mapType);
            cache[key] = tex;
        }

        if (tex != null)
        {
            mat.SetTexture(property, tex);

            // Configure normal map import settings
            if (mapType == "NormalGL")
            {
                var path = AssetDatabase.GetAssetPath(tex);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
            }
        }
    }

    static Texture2D FindTexture(string textureId, string category, string mapType)
    {
        // Try different naming patterns ambientCG uses
        string[] patterns = new[]
        {
            $"{textureId}_1K_{mapType}",
            $"{textureId}_1K-JPG_{mapType}",
        };

        foreach (var searchPath in TextureSearchPaths)
        {
            if (!AssetDatabase.IsValidFolder(searchPath)) continue;
            string catPath = $"{searchPath}/{category}";
            if (!AssetDatabase.IsValidFolder(catPath)) continue;

            foreach (var pattern in patterns)
            {
                var guids = AssetDatabase.FindAssets(pattern, new[] { catPath });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null) return tex;
                }
            }
        }

        return null;
    }
}
