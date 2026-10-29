# Procedural-Cities

The low-poly model library of [uzProceduralCities](https://github.com/uta-org/), packaged for Unity.

![Every prefab of the package](images/prefabs-overview.png?raw=true "Every prefab of the package")

This repository started as a master thesis project in Unreal Engine (see
[The original Unreal project](#the-original-unreal-project) below). Today it is a
submodule of the Unity project, and what that project uses is the Unity package in
`Import/`: the furniture, fixtures, street props and decoration a generated city is
filled with. The Unreal sources are still here, untouched, for reference.

## What is in the package

`Import/` is a Unity package, `dev.z3nth10n.proceduralcities.import`. The Unity
project mounts it with a local path in `Packages/manifest.json`:

```json
"dev.z3nth10n.proceduralcities.import": "file:../../Packages/Procedural-Cities/Import"
```

| Folder | Contents |
|---|---|
| `Resources/Prefabs/AssetContents/` | The 181 prefabs the city places, one per placement name. |
| `Models/LowPoly/` | Their combined meshes (`<Name>_Combined.asset`), the `LP_*` materials, the 23 painting images, and the 18 street prefabs `LowPoly_*` (trees, lampposts, benches, fences, fountain, traffic lights, bins, awning, fire hydrant). |
| `Models/Living/`, `Models/Kitchen/`, `Models/Uncategorized/` | Materials of the older imported furniture. |
| `Textures/` | The low-poly material textures (wood, fabric, metal, ceramic, plastic) and the litter atlas. |
| `Resources/Textures/` | Textures loaded at runtime (grass, trees, imported props). |
| `Shaders/` | `LowPolyPBR.shader`. |
| `Editor/` | The C# generators that build most of the models (editor only). |
| `Import-Scripts/` | Blender scripts that converted the original glTF models to FBX. |

The prefabs cover, among others:

- **Homes:** beds, wardrobes, sofas and L/U sectionals, tables and chairs,
  shelves, lamps, paintings, plants and vases, kitchen modules, bathroom fixtures,
  radiators and air conditioners.
- **Small props:** rugs, cushions, books, candles, towels and toiletries, in the
  per-room sets of P40.
- **Offices and shops:** desks, bench desks, laptops, meeting tables, filing
  cabinets, store and restaurant counters.
- **Streets and parks:** trees, lampposts, traffic lights, benches, bins,
  fences, fountains, planters, park plants and litter.

## How the project uses it

- The runtime spawner (`DecorationSpawner` in the Unity project) maps each
  placement name, such as `sofa` or `bath_mat`, to a prefab name in
  `AssetContents/`.
- The prefabs are loaded through the **Remote** Addressables group of the Unity
  project; a few exterior ones are also listed in the scene's
  `CityManager._decorationPrefabs`.
- Pieces drawn at a distance are GPU-instanced (`RenderMeshInstanced`): every
  material an exterior prop uses must have GPU instancing on.

## Generating the models

Most models are built in code rather than modelled by hand: 139 of the 199
prefabs. Each generator is an editor script with a menu entry under
**Tools / Procedural Cities**; it builds its meshes from boxes (`LowPolyBoxMesh`),
saves them to `Models/LowPoly/` and the prefabs to
`Resources/Prefabs/AssetContents/`. A rebuild keeps the asset GUIDs, so nothing
that references a prefab breaks.

| Menu entry | Builds |
|---|---|
| Generate Kitchen Module Props, Generate Restaurant Kitchen Props | Kitchen modules |
| Generate Lamp Props, Hollow Ceiling Pendant Shade, Add Lamp Bulbs, Add Lamp Focal Lights | Lamps and their bulbs |
| Generate Painting Props | Paintings in 7 formats |
| Generate Planter Props, Generate Park Props, Generate Suburb Props, Generate Litter Props | Plants, parks, suburbs, litter |
| Generate Meeting Table Props, Generate Office Bench Props, Generate Filing Cabinet Prop | Office furniture |
| Generate Store Counter Prop, Generate Counter Gate Props, Generate Console Table Prop | Counters and tables |
| Generate Radiator Prop, Generate Air Conditioner Props | Heating and air conditioning |
| Generate Living Room Props, Generate Bedroom Props, Generate Bathroom Props | The per-room small props |
| Generate Double Traffic Light, Generate RubbishBin Prop, Generate Light Switch Prop | Single props (`LowPoly_TrafficLightDouble`, the rubbish bin, the light switch) |

Conventions the generators follow:

- **Pivot and orientation:** on the bottom face at the centre, front +Z.
  Wall-hung pieces have their back face at Z = 0.
- **Size:** real units (metres).
- **Materials:** URP Lit materials named `LP_*`, with GPU instancing on.

The generators are only needed to change or rebuild a model; the saved prefabs,
meshes and materials are what the game loads.

**60 prefabs have no generator;** their saved meshes are the only source:

- **43 core furniture prefabs** (beds, the sofa and its sectional variants,
  chairs, toilet, shower, sink, wardrobe, fridge, TVs, mirrors, office and
  restaurant furniture). They come from the original imported models
  (`Import-Scripts/`) and one-off mesh edits.
- **17 of the `LowPoly_*` street prefabs** (all but the double traffic light).
  Their old ProBuilder generator wrote outside the package and was removed.

## The original Unreal project

The root of the repository (`City.uproject`, `City.sln`, `Source/`, `Config/`,
`images/`, `Scripts/`) is the original project: a framework for generating
complete, seamless procedural cities with interiors for every building, written in
Unreal Engine as a master thesis at LTH.

- Thesis: http://lup.lub.lu.se/luur/download?func=downloadFile&recordOId=8929185&fileOId=8929189
- Demo: https://www.youtube.com/watch?v=n1eZOV8r_g4

The Unity project does not use any of it: it ported the ideas, not the code.

## Licence

MIT, see [LICENCE.txt](LICENCE.txt).
