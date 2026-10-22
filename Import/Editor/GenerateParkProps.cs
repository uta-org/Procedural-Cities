using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the low-poly props of the parks on green plots (owner, 2026-10-22: "weeds, bushes
/// and flowers of three kinds each, low-poly models first"; "a wrought-iron fence about
/// 0.4 m high"; "a playground: benches, a swing, a slide, bins and five more models"):
/// three weeds, three bushes, three flowers, the fence, a park litter bin, a swing, a slide,
/// a seesaw, a spring rider, a sandbox, a climbing frame and a merry-go-round.
/// Each prop is one mesh with a submesh per material, built from its own flat-shaded boxes,
/// prisms and low spheres (Unity's sphere primitive has 515 vertices, too many for props
/// placed by the thousand), saved under Models/LowPoly/Park, with its prefab in
/// Resources/Prefabs/AssetContents, where DecorationSpawner finds the exterior props by name.
/// Pivot on the ground at the prop's centre, +Z its front. Vegetation casts no shadow and
/// has no collider; the fence, the bin and the playground pieces get a box collider.
/// Menu: Tools / Procedural Cities / Generate Park Props
/// </summary>
public static class GenerateParkProps
{
    private const string PkgRoot = "Packages/dev.z3nth10n.proceduralcities.import";
    private const string PrefabDir = PkgRoot + "/Resources/Prefabs/AssetContents";
    private const string MatDir = PkgRoot + "/Models/LowPoly/Materials";
    private const string MeshDir = PkgRoot + "/Models/LowPoly/Park";

    [MenuItem("Tools/Procedural Cities/Generate Park Props")]
    public static void Generate()
    {
        EnsureFolder(PkgRoot + "/Models/LowPoly", "Materials");
        EnsureFolder(PkgRoot + "/Models/LowPoly", "Park");

        var leaf = GetMat("LP_Park_Leaf", new Color(0.16f, 0.40f, 0.14f), 0f, 0.15f);
        var leafLight = GetMat("LP_Park_LeafLight", new Color(0.30f, 0.55f, 0.22f), 0f, 0.15f);
        var leafDark = GetMat("LP_Park_LeafDark", new Color(0.11f, 0.30f, 0.11f), 0f, 0.15f);
        var weed = GetMat("LP_Park_Weed", new Color(0.32f, 0.52f, 0.17f), 0f, 0.1f);
        var weedOlive = GetMat("LP_Park_WeedOlive", new Color(0.42f, 0.48f, 0.20f), 0f, 0.1f);
        var seed = GetMat("LP_Park_Seed", new Color(0.62f, 0.55f, 0.32f), 0f, 0.1f);
        var stem = GetMat("LP_Park_Stem", new Color(0.24f, 0.46f, 0.16f), 0f, 0.1f);
        var red = GetMat("LP_Park_PetalRed", new Color(0.86f, 0.10f, 0.12f), 0f, 0.3f);
        var yellow = GetMat("LP_Park_PetalYellow", new Color(0.98f, 0.82f, 0.12f), 0f, 0.3f);
        var purple = GetMat("LP_Park_PetalPurple", new Color(0.55f, 0.33f, 0.80f), 0f, 0.3f);
        var pink = GetMat("LP_Park_PetalPink", new Color(0.95f, 0.52f, 0.72f), 0f, 0.3f);
        var brown = GetMat("LP_Park_FlowerHeart", new Color(0.45f, 0.28f, 0.10f), 0f, 0.2f);
        var iron = GetMat("LP_Park_WroughtIron", new Color(0.07f, 0.07f, 0.08f), 0.6f, 0.4f);
        var binGreen = GetMat("LP_Park_BinGreen", new Color(0.13f, 0.30f, 0.18f), 0.4f, 0.35f);
        var frameBlue = GetMat("LP_Park_FrameBlue", new Color(0.18f, 0.42f, 0.78f), 0.4f, 0.4f);
        var playRed = GetMat("LP_Park_PlayRed", new Color(0.86f, 0.20f, 0.14f), 0.1f, 0.45f);
        var playYellow = GetMat("LP_Park_PlayYellow", new Color(0.97f, 0.76f, 0.10f), 0.1f, 0.45f);
        var playGreen = GetMat("LP_Park_PlayGreen", new Color(0.20f, 0.68f, 0.30f), 0.1f, 0.45f);
        var chain = GetMat("LP_Park_Chain", new Color(0.55f, 0.56f, 0.58f), 0.8f, 0.5f);
        var timber = GetMat("LP_Park_Timber", new Color(0.55f, 0.36f, 0.20f), 0f, 0.2f);
        var sand = GetMat("LP_Park_Sand", new Color(0.90f, 0.80f, 0.56f), 0f, 0.05f);
        var darkMetal = GetMat("LP_Park_DarkMetal", new Color(0.22f, 0.23f, 0.25f), 0.7f, 0.45f);

        var built = new List<string>();
        void Save(string name, MeshBuilder b, Vector3? colliderSize = null, bool shadows = true)
        {
            SaveProp(name, b, colliderSize, shadows);
            built.Add(name);
        }

        // ── Weeds ─────────────────────────────────────────────────────────
        {
            var b = new MeshBuilder(); // a grass tuft
            for (var i = 0; i < 7; i++)
            {
                var yaw = i * 360f / 7f + 11f * i;
                var height = 0.18f + 0.04f * (i % 3);
                var rot = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(22f, 0f, 0f);
                b.Box(weed, rot * new Vector3(0f, height * 0.5f, 0f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.03f),
                    new Vector3(0.025f, height, 0.008f), rot);
            }
            Save("ParkWeed0", b, shadows: false);
        }
        {
            var b = new MeshBuilder(); // a broadleaf rosette
            for (var i = 0; i < 5; i++)
            {
                var yaw = Quaternion.Euler(0f, i * 72f, 0f);
                b.Box(weed, yaw * new Vector3(0f, 0.02f, 0.08f), new Vector3(0.06f, 0.012f, 0.16f), yaw * Quaternion.Euler(-14f, 0f, 0f));
            }
            b.Sphere(weedOlive, new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.04f, 0.05f), 3, 5);
            Save("ParkWeed1", b, shadows: false);
        }
        {
            var b = new MeshBuilder(); // a tall weed with seed heads
            for (var i = 0; i < 3; i++)
            {
                var yaw = Quaternion.Euler(0f, i * 120f + 20f, 0f);
                var height = 0.34f + 0.05f * i;
                var tilt = yaw * Quaternion.Euler(10f, 0f, 0f);
                var basePos = yaw * new Vector3(0f, 0f, 0.025f);
                b.Prism(weedOlive, basePos + tilt * new Vector3(0f, height * 0.5f, 0f), 0.008f, height, 5, tilt, caps: false);
                b.Box(weedOlive, basePos + tilt * new Vector3(0.025f, height * 0.4f, 0f), new Vector3(0.05f, 0.01f, 0.02f), tilt * Quaternion.Euler(0f, 0f, 25f));
                b.Sphere(seed, basePos + tilt * new Vector3(0f, height + 0.02f, 0f), new Vector3(0.02f, 0.04f, 0.02f), 3, 5);
            }
            Save("ParkWeed2", b, shadows: false);
        }

        // ── Bushes ────────────────────────────────────────────────────────
        {
            var b = new MeshBuilder(); // round
            b.Sphere(leaf, new Vector3(0f, 0.38f, 0f), new Vector3(0.45f, 0.38f, 0.45f), 4, 7);
            b.Sphere(leafDark, new Vector3(0.22f, 0.30f, 0.12f), new Vector3(0.30f, 0.28f, 0.30f), 4, 6);
            b.Sphere(leafDark, new Vector3(-0.2f, 0.28f, -0.15f), new Vector3(0.28f, 0.26f, 0.28f), 4, 6);
            b.Sphere(leaf, new Vector3(0.02f, 0.55f, 0.04f), new Vector3(0.26f, 0.22f, 0.26f), 4, 6);
            Save("ParkBush0", b);
        }
        {
            var b = new MeshBuilder(); // a clipped box hedge
            b.Box(leafDark, new Vector3(0f, 0.27f, 0f), new Vector3(0.85f, 0.54f, 0.85f), Quaternion.identity);
            b.Box(leafDark, new Vector3(0f, 0.57f, 0f), new Vector3(0.74f, 0.06f, 0.74f), Quaternion.identity);
            Save("ParkBush1", b);
        }
        {
            var b = new MeshBuilder(); // flowering
            b.Sphere(leafLight, new Vector3(0f, 0.32f, 0f), new Vector3(0.40f, 0.32f, 0.40f), 4, 7);
            b.Sphere(leafLight, new Vector3(0.24f, 0.24f, -0.06f), new Vector3(0.26f, 0.22f, 0.26f), 4, 6);
            b.Sphere(leafLight, new Vector3(-0.2f, 0.25f, 0.14f), new Vector3(0.26f, 0.22f, 0.26f), 4, 6);
            for (var i = 0; i < 9; i++)
            {
                var yaw = Quaternion.Euler(0f, i * 40f, 0f);
                var up = 0.22f + 0.05f * (i % 3);
                b.Sphere(pink, yaw * new Vector3(0f, up, 0.36f - 0.04f * (i % 2)), Vector3.one * 0.055f, 3, 5);
            }
            Save("ParkBush2", b);
        }

        // ── Flowers ───────────────────────────────────────────────────────
        {
            var b = new MeshBuilder(); // red tulip
            b.Prism(stem, new Vector3(0f, 0.14f, 0f), 0.006f, 0.28f, 5, Quaternion.identity, caps: false);
            b.Box(stem, new Vector3(0.015f, 0.07f, 0f), new Vector3(0.03f, 0.14f, 0.006f), Quaternion.Euler(0f, 0f, -15f));
            b.Box(stem, new Vector3(-0.012f, 0.06f, 0.01f), new Vector3(0.026f, 0.12f, 0.006f), Quaternion.Euler(0f, 40f, 14f));
            b.Prism(red, new Vector3(0f, 0.31f, 0f), 0.034f, 0.06f, 6, Quaternion.identity, caps: true, topScale: 1.25f);
            Save("ParkFlowerRed", b, shadows: false);
        }
        {
            var b = new MeshBuilder(); // yellow daisy
            b.Prism(stem, new Vector3(0f, 0.11f, 0f), 0.006f, 0.22f, 5, Quaternion.identity, caps: false);
            b.Box(stem, new Vector3(0.016f, 0.05f, 0f), new Vector3(0.032f, 0.008f, 0.014f), Quaternion.Euler(0f, 0f, 20f));
            var tilt = Quaternion.Euler(18f, 0f, 0f);
            b.Prism(yellow, new Vector3(0f, 0.225f, 0f), 0.05f, 0.012f, 8, tilt, caps: true);
            b.Prism(brown, tilt * new Vector3(0f, 0.008f, 0f) + new Vector3(0f, 0.225f, 0f), 0.017f, 0.012f, 6, tilt, caps: true);
            Save("ParkFlowerYellow", b, shadows: false);
        }
        {
            var b = new MeshBuilder(); // purple lavender
            for (var i = 0; i < 3; i++)
            {
                var yaw = Quaternion.Euler(0f, i * 120f, 0f);
                var tilt = yaw * Quaternion.Euler(9f + 3f * i, 0f, 0f);
                var height = 0.26f + 0.04f * i;
                b.Prism(stem, tilt * new Vector3(0f, height * 0.5f, 0f), 0.005f, height, 4, tilt, caps: false);
                b.Prism(purple, tilt * new Vector3(0f, height + 0.04f, 0f), 0.013f, 0.08f, 5, tilt, caps: true, topScale: 0.4f);
            }
            Save("ParkFlowerPurple", b, shadows: false);
        }

        // ── Fence: 2 m of low wrought iron, posts at both ends ─────────────
        {
            var b = new MeshBuilder();
            for (var s = -1; s <= 1; s += 2)
            {
                b.Box(iron, new Vector3(s * 0.98f, 0.21f, 0f), new Vector3(0.04f, 0.42f, 0.04f), Quaternion.identity);
                b.Sphere(iron, new Vector3(s * 0.98f, 0.45f, 0f), Vector3.one * 0.03f, 3, 5);
            }
            b.Box(iron, new Vector3(0f, 0.08f, 0f), new Vector3(1.92f, 0.02f, 0.015f), Quaternion.identity);
            b.Box(iron, new Vector3(0f, 0.34f, 0f), new Vector3(1.92f, 0.02f, 0.015f), Quaternion.identity);
            for (var i = 0; i < 13; i++)
            {
                var x = -0.84f + i * 0.14f;
                b.Box(iron, new Vector3(x, 0.22f, 0f), new Vector3(0.012f, 0.30f, 0.012f), Quaternion.identity);
                b.Prism(iron, new Vector3(x, 0.385f, 0f), 0.014f, 0.03f, 4, Quaternion.identity, caps: true, topScale: 0.05f);
            }
            Save("ParkFence", b, new Vector3(2f, 0.42f, 0.06f));
        }

        // ── Park litter bin: a basket on a post ───────────────────────────
        {
            var b = new MeshBuilder();
            b.Prism(binGreen, new Vector3(0f, 0.45f, -0.16f), 0.035f, 0.9f, 6, Quaternion.identity, caps: true);
            b.Prism(binGreen, new Vector3(0f, 0.68f, 0f), 0.18f, 0.44f, 8, Quaternion.identity, caps: true);
            b.Prism(darkMetal, new Vector3(0f, 0.91f, 0f), 0.19f, 0.03f, 8, Quaternion.identity, caps: true);
            Save("ParkBin", b, new Vector3(0.4f, 0.95f, 0.45f));
        }

        // ── Swing: an A-frame, two seats ──────────────────────────────────
        {
            var b = new MeshBuilder();
            const float top = 2.2f;
            for (var s = -1; s <= 1; s += 2)
            {
                for (var f = -1; f <= 1; f += 2)
                {
                    var foot = new Vector3(s * 1.15f, 0f, f * 0.65f);
                    var head = new Vector3(s * 1.15f, top, 0f);
                    b.Strut(frameBlue, foot, head, 0.045f, 6);
                }
            }
            b.Strut(frameBlue, new Vector3(-1.25f, top, 0f), new Vector3(1.25f, top, 0f), 0.05f, 6);
            for (var seat = -1; seat <= 1; seat += 2)
            {
                var x = seat * 0.5f;
                b.Box(playRed, new Vector3(x, 0.45f, 0f), new Vector3(0.46f, 0.04f, 0.2f), Quaternion.identity);
                b.Box(chain, new Vector3(x - 0.2f, (top + 0.47f) * 0.5f, 0f), new Vector3(0.012f, top - 0.47f, 0.012f), Quaternion.identity);
                b.Box(chain, new Vector3(x + 0.2f, (top + 0.47f) * 0.5f, 0f), new Vector3(0.012f, top - 0.47f, 0.012f), Quaternion.identity);
            }
            Save("ParkSwing", b, new Vector3(2.5f, 2.3f, 1.4f));
        }

        // ── Slide: platform, ladder at the back, chute to the front ───────
        {
            var b = new MeshBuilder();
            const float deck = 1.5f;
            b.Box(playYellow, new Vector3(0f, deck, -0.6f), new Vector3(0.8f, 0.06f, 0.8f), Quaternion.identity);
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                    b.Box(playYellow, new Vector3(sx * 0.36f, deck * 0.5f, -0.6f + sz * 0.36f), new Vector3(0.06f, deck, 0.06f), Quaternion.identity);
            for (var sx = -1; sx <= 1; sx += 2)
                b.Box(playYellow, new Vector3(sx * 0.38f, deck + 0.3f, -0.6f), new Vector3(0.04f, 0.04f, 0.8f), Quaternion.identity);
            // Ladder down the back.
            for (var sx = -1; sx <= 1; sx += 2)
                b.Strut(darkMetal, new Vector3(sx * 0.22f, 0f, -1.55f), new Vector3(sx * 0.22f, deck, -1.0f), 0.025f, 5);
            for (var r = 1; r <= 5; r++)
            {
                var t = r / 6f;
                b.Box(darkMetal, new Vector3(0f, deck * t, Mathf.Lerp(-1.55f, -1.0f, t)), new Vector3(0.44f, 0.03f, 0.03f), Quaternion.identity);
            }
            // The chute: from the deck's front edge to the ground.
            var from = new Vector3(0f, deck, -0.2f);
            var to = new Vector3(0f, 0.1f, 2.1f);
            var mid = (from + to) * 0.5f;
            var run = to - from;
            var slope = Quaternion.LookRotation(run.normalized, Vector3.up);
            b.Box(playRed, mid, new Vector3(0.5f, 0.04f, run.magnitude), slope);
            for (var sx = -1; sx <= 1; sx += 2)
                b.Box(playRed, mid + slope * new Vector3(sx * 0.27f, 0.08f, 0f), new Vector3(0.04f, 0.16f, run.magnitude), slope);
            b.Box(playRed, new Vector3(0f, 0.08f, 2.3f), new Vector3(0.5f, 0.04f, 0.4f), Quaternion.identity);
            Save("ParkSlide", b, new Vector3(0.9f, 1.9f, 4.1f));
        }

        // ── Seesaw ────────────────────────────────────────────────────────
        {
            var b = new MeshBuilder();
            b.Box(darkMetal, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.36f, 0.3f), Quaternion.identity);
            var tilt = Quaternion.Euler(0f, 0f, 8f);
            b.Box(playYellow, new Vector3(0f, 0.4f, 0f), new Vector3(3f, 0.06f, 0.26f), tilt);
            for (var s = -1; s <= 1; s += 2)
            {
                b.Box(playRed, new Vector3(0f, 0.4f, 0f) + tilt * new Vector3(s * 1.3f, 0.06f, 0f), new Vector3(0.36f, 0.06f, 0.3f), tilt);
                b.Box(darkMetal, new Vector3(0f, 0.4f, 0f) + tilt * new Vector3(s * 1.0f, 0.2f, 0f), new Vector3(0.04f, 0.32f, 0.04f), tilt);
                b.Box(darkMetal, new Vector3(0f, 0.4f, 0f) + tilt * new Vector3(s * 1.0f, 0.36f, 0f), new Vector3(0.04f, 0.04f, 0.3f), tilt);
            }
            Save("ParkSeesaw", b, new Vector3(3f, 0.8f, 0.4f));
        }

        // ── Spring rider: a little horse on a spring ──────────────────────
        {
            var b = new MeshBuilder();
            b.Prism(darkMetal, new Vector3(0f, 0.02f, 0f), 0.2f, 0.04f, 8, Quaternion.identity, caps: true);
            b.Prism(chain, new Vector3(0f, 0.22f, 0f), 0.08f, 0.36f, 8, Quaternion.identity, caps: true);
            b.Box(playGreen, new Vector3(0f, 0.52f, 0f), new Vector3(0.24f, 0.24f, 0.6f), Quaternion.identity);
            b.Box(playGreen, new Vector3(0f, 0.74f, 0.32f), new Vector3(0.18f, 0.3f, 0.18f), Quaternion.Euler(-20f, 0f, 0f));
            b.Box(playYellow, new Vector3(0f, 0.86f, 0.42f), new Vector3(0.16f, 0.14f, 0.22f), Quaternion.identity);
            b.Box(playYellow, new Vector3(0f, 0.66f, -0.06f), new Vector3(0.26f, 0.05f, 0.28f), Quaternion.identity);
            b.Box(darkMetal, new Vector3(0f, 0.86f, 0.24f), new Vector3(0.36f, 0.03f, 0.03f), Quaternion.identity);
            Save("ParkSpringRider", b, new Vector3(0.4f, 1f, 0.8f));
        }

        // ── Sandbox: a timber frame filled with sand ──────────────────────
        {
            var b = new MeshBuilder();
            for (var s = -1; s <= 1; s += 2)
            {
                b.Box(timber, new Vector3(s * 0.94f, 0.12f, 0f), new Vector3(0.12f, 0.24f, 2f), Quaternion.identity);
                b.Box(timber, new Vector3(0f, 0.12f, s * 0.94f), new Vector3(1.76f, 0.24f, 0.12f), Quaternion.identity);
            }
            b.Box(sand, new Vector3(0f, 0.08f, 0f), new Vector3(1.76f, 0.16f, 1.76f), Quaternion.identity);
            b.Prism(playRed, new Vector3(0.45f, 0.21f, -0.35f), 0.07f, 0.1f, 6, Quaternion.identity, caps: true, topScale: 1.3f);
            Save("ParkSandbox", b, new Vector3(2f, 0.24f, 2f));
        }

        // ── Climbing frame: a cube of bars with a deck ────────────────────
        {
            var b = new MeshBuilder();
            const float side = 1.8f, height = 1.6f;
            var h = side * 0.5f;
            var bars = new[] { playRed, frameBlue, playYellow };
            var k = 0;
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                    b.Strut(bars[k++ % 3], new Vector3(sx * h, 0f, sz * h), new Vector3(sx * h, height, sz * h), 0.035f, 6);
            foreach (var y in new[] { 0.55f, 1.1f, height })
            {
                for (var s = -1; s <= 1; s += 2)
                {
                    b.Strut(bars[k++ % 3], new Vector3(-h, y, s * h), new Vector3(h, y, s * h), 0.03f, 6);
                    b.Strut(bars[k++ % 3], new Vector3(s * h, y, -h), new Vector3(s * h, y, h), 0.03f, 6);
                }
            }
            b.Box(playYellow, new Vector3(0f, 1.1f, 0f), new Vector3(side - 0.1f, 0.05f, side * 0.5f), Quaternion.identity);
            Save("ParkClimbingFrame", b, new Vector3(1.9f, 1.7f, 1.9f));
        }

        // ── Merry-go-round: a turning disc with handles ───────────────────
        {
            var b = new MeshBuilder();
            b.Prism(darkMetal, new Vector3(0f, 0.12f, 0f), 0.18f, 0.24f, 8, Quaternion.identity, caps: true);
            b.Prism(playRed, new Vector3(0f, 0.28f, 0f), 1.0f, 0.08f, 12, Quaternion.identity, caps: true);
            b.Prism(playYellow, new Vector3(0f, 0.6f, 0f), 0.05f, 0.6f, 6, Quaternion.identity, caps: true);
            for (var i = 0; i < 4; i++)
            {
                var yaw = Quaternion.Euler(0f, i * 90f + 45f, 0f);
                b.Strut(playYellow, yaw * new Vector3(0f, 0.32f, 0.85f), yaw * new Vector3(0f, 0.75f, 0.6f), 0.025f, 5);
                b.Strut(playYellow, yaw * new Vector3(0f, 0.75f, 0.6f), new Vector3(0f, 0.85f, 0f), 0.025f, 5);
            }
            Save("ParkMerryGoRound", b, new Vector3(2f, 0.9f, 2f));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GenerateParkProps] Saved {built.Count} park prefabs under {PrefabDir}: {string.Join(", ", built)}");
    }

    /// <summary>Saves the prop's mesh and its prefab (one renderer, a material per submesh).</summary>
    private static void SaveProp(string name, MeshBuilder builder, Vector3? colliderSize, bool shadows)
    {
        var mesh = builder.Build(name, out var materials);
        var meshPath = $"{MeshDir}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            mesh = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var root = new GameObject(name);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        if (colliderSize.HasValue)
        {
            var box = root.AddComponent<BoxCollider>();
            box.size = colliderSize.Value;
            box.center = new Vector3(0f, colliderSize.Value.y * 0.5f, mesh.bounds.center.z);
        }

        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Material GetMat(string name, Color color, float metallic, float smoothness)
    {
        var path = $"{MatDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name, color = color, enableInstancing = true };
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>
    /// Flat-shaded mesh builder: every triangle has its own three vertices and face
    /// normal (the low-poly look), grouped into one submesh per material.
    /// </summary>
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Material> _materials = new();
        private readonly List<List<int>> _indices = new();

        /// <summary>A box of <paramref name="size"/> centred on <paramref name="center"/>.</summary>
        public void Box(Material mat, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var h = size * 0.5f;
            var c = new Vector3[8];
            for (var i = 0; i < 8; i++)
                c[i] = center + rotation * Vector3.Scale(h, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Quad(mat, center, c[0], c[2], c[3], c[1]);
            Quad(mat, center, c[4], c[5], c[7], c[6]);
            Quad(mat, center, c[0], c[1], c[5], c[4]);
            Quad(mat, center, c[2], c[6], c[7], c[3]);
            Quad(mat, center, c[0], c[4], c[6], c[2]);
            Quad(mat, center, c[1], c[3], c[7], c[5]);
        }

        /// <summary>
        /// An upright prism (a low cylinder) of <paramref name="sides"/> sides centred on
        /// <paramref name="center"/>; <paramref name="topScale"/> narrows (cone) or widens its top.
        /// </summary>
        public void Prism(Material mat, Vector3 center, float radius, float height, int sides, Quaternion rotation,
            bool caps, float topScale = 1f)
        {
            var bottom = new Vector3[sides];
            var top = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2f / sides;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                bottom[i] = center + rotation * (dir * radius + Vector3.down * height * 0.5f);
                top[i] = center + rotation * (dir * radius * topScale + Vector3.up * height * 0.5f);
            }

            for (var i = 0; i < sides; i++)
            {
                var j = (i + 1) % sides;
                Quad(mat, center, bottom[i], top[i], top[j], bottom[j]);
            }

            if (!caps)
                return;
            var bc = center + rotation * (Vector3.down * height * 0.5f);
            var tc = center + rotation * (Vector3.up * height * 0.5f);
            for (var i = 0; i < sides; i++)
            {
                var j = (i + 1) % sides;
                Tri(mat, center, bc, bottom[i], bottom[j]);
                Tri(mat, center, tc, top[j], top[i]);
            }
        }

        /// <summary>A prism from <paramref name="from"/> to <paramref name="to"/> (a bar, a leg).</summary>
        public void Strut(Material mat, Vector3 from, Vector3 to, float radius, int sides)
        {
            var axis = to - from;
            var rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            Prism(mat, (from + to) * 0.5f, radius, axis.magnitude, sides, rotation, caps: true);
        }

        /// <summary>A low sphere (an ellipsoid of <paramref name="radii"/>).</summary>
        public void Sphere(Material mat, Vector3 center, Vector3 radii, int rings, int segments)
        {
            Vector3 P(int r, int s)
            {
                var theta = Mathf.PI * r / rings;
                var phi = Mathf.PI * 2f * s / segments;
                return center + Vector3.Scale(radii, new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi)));
            }

            for (var r = 0; r < rings; r++)
            {
                for (var s = 0; s < segments; s++)
                {
                    var a = P(r, s);
                    var b = P(r, s + 1);
                    var c = P(r + 1, s + 1);
                    var d = P(r + 1, s);
                    if (r > 0)
                        Tri(mat, center, a, b, d);
                    if (r < rings - 1)
                        Tri(mat, center, b, c, d);
                }
            }
        }

        private void Quad(Material mat, Vector3 inside, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(mat, inside, a, b, c);
            Tri(mat, inside, a, c, d);
        }

        /// <summary>
        /// A triangle facing away from <paramref name="inside"/> (the centre of its convex
        /// piece), whatever order its corners come in.
        /// </summary>
        private void Tri(Material mat, Vector3 inside, Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-12f)
                return;
            normal.Normalize();
            if (Vector3.Dot(normal, (a + b + c) / 3f - inside) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }
            var index = _materials.IndexOf(mat);
            if (index < 0)
            {
                _materials.Add(mat);
                _indices.Add(new List<int>());
                index = _materials.Count - 1;
            }

            var list = _indices[index];
            foreach (var v in new[] { a, b, c })
            {
                list.Add(_vertices.Count);
                _vertices.Add(v);
                _normals.Add(normal);
            }
        }

        /// <summary>The mesh, one submesh per material in first-use order.</summary>
        public Mesh Build(string name, out Material[] materials)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.subMeshCount = _indices.Count;
            for (var i = 0; i < _indices.Count; i++)
                mesh.SetTriangles(_indices[i], i);
            mesh.RecalculateBounds();
            materials = _materials.ToArray();
            return mesh;
        }
    }
}
