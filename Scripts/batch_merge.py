"""
Batch process all redundant FBX/3DS files to merge submeshes with same materials.
Run with: blender --background --python batch_merge.py

Processes files in-place (originals already backed up to _backups_originals/).
"""

import bpy
import sys
import os
import re
import tempfile
import shutil


PKG_ROOT = r"C:\Users\arodriguezg\Unity3D\procedural-cities\procedural-cities-2022\src\Packages\Procedural-Cities\Import"

# Files to process with their relative paths
# Excluding .spm (SpeedTree) and .3DS that needs special handling
FILES = [
    "Models/Kitchen/kitchen3.fbx",
    "Models/Kitchen/kitchen2.FBX",
    "Models/Street/rooftop_solar.fbx",
    "Models/Living/locker.FBX",
    "Models/Living/doorMesh_Rectangle0.fbx",
    "Models/Living/Shelf/elevatorDoors.fbx",
    "Models/Restaurant/TableRes.fbx",
    "Models/Bathroom/mirror.fbx",
    "Models/Living/Shelf/shelf1.fbx",
    "Models/Living/Shelf/shelf_store.fbx",
    "Models/Tree/tree.FBX",
    "Models/Kitchen/oven1.fbx",
    "Models/Office/officePositionTable.fbx",
    "Models/Restaurant/chairRes.fbx",
    "Models/Uncategorized/street_light.fbx",
    "Models/Toaster/toaster.FBX",
    "Models/Street/stairmesh.fbx",
    "Models/Office/dispensermesh.fbx",
    "Models/Saucepan/Saucepan-ts.fbx",
    "Models/Living/Shelf/shelf4_Box07.fbx",
    "Models/Bathroom/sink.fbx",
    "Models/Living/bed.fbx",
    "Models/Bathroom/toiletmesh.fbx",
    "Models/Living/Shelf/shelf2 - Copy.fbx",
    "Models/Kitchen/kitchen4.fbx",
    "Models/Living/wardrobe.fbx",
    "Models/Bathroom/toilet.fbx",
    # 3DS file
    "Models/Sofa Hepburn Modular/3DSTYLISH-fsd000.3DS",
]


def get_base_material_name(mat_name):
    """Strip .NNN numeric suffix to get base material name."""
    match = re.match(r'^(.+)\.(\d{3})$', mat_name)
    if match:
        return match.group(1)
    return mat_name


def clean_scene():
    """Remove all objects from the scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)
    for block in bpy.data.images:
        if block.users == 0:
            bpy.data.images.remove(block)


def import_file(filepath):
    """Import FBX or 3DS file."""
    ext = os.path.splitext(filepath)[1].lower()
    if ext in ('.fbx',):
        bpy.ops.import_scene.fbx(filepath=filepath)
    elif ext in ('.3ds',):
        bpy.ops.import_scene.autodesk_3ds(filepath=filepath)
    else:
        print(f"  Unsupported format: {ext}")
        return False
    return True


def consolidate_materials(obj):
    """
    For a mesh object, consolidate .NNN variant materials into their base.
    Returns the number of material slots reduced.
    """
    if obj.type != 'MESH' or not obj.data.materials:
        return 0

    mesh = obj.data
    materials = list(mesh.materials)
    if not materials:
        return 0

    # Map each slot to its base material name
    base_names = {}
    for i, mat in enumerate(materials):
        if mat is None:
            base_names[i] = f"__none_{i}"
        else:
            base_names[i] = get_base_material_name(mat.name)

    # Find first slot for each base name
    base_to_first_slot = {}
    slot_remap = {}

    for i in range(len(materials)):
        base_name = base_names[i]
        if base_name not in base_to_first_slot:
            base_to_first_slot[base_name] = i
        slot_remap[i] = base_to_first_slot[base_name]

    # Check if any consolidation needed
    needs_work = False
    for i in slot_remap:
        if slot_remap[i] != i:
            needs_work = True
            break
    if not needs_work:
        return 0

    original_count = len(materials)

    # Remap face material indices to point to base slots
    for poly in mesh.polygons:
        old_idx = poly.material_index
        if old_idx in slot_remap:
            poly.material_index = slot_remap[old_idx]

    # Collect which slots are actually used now
    used_slots = set()
    for poly in mesh.polygons:
        used_slots.add(poly.material_index)

    # Build compact material list
    new_materials = []
    old_to_new = {}
    for i in sorted(used_slots):
        if i < len(materials):
            old_to_new[i] = len(new_materials)
            new_materials.append(materials[i])

    # Remap again to compact indices
    for poly in mesh.polygons:
        if poly.material_index in old_to_new:
            poly.material_index = old_to_new[poly.material_index]

    # Rebuild material slots
    mesh.materials.clear()
    for mat in new_materials:
        mesh.materials.append(mat)

    return original_count - len(new_materials)


def join_objects_by_material(objects):
    """Join mesh objects that share the same set of base materials."""
    if len(objects) <= 1:
        return objects

    groups = {}
    non_mesh = []
    for obj in objects:
        if obj.type != 'MESH':
            non_mesh.append(obj)
            continue
        mat_key = frozenset(
            get_base_material_name(mat.name) if mat else "__none"
            for mat in obj.data.materials
        ) if obj.data.materials else frozenset(["__empty"])
        if mat_key not in groups:
            groups[mat_key] = []
        groups[mat_key].append(obj)

    result = list(non_mesh)
    for mat_key, group_objs in groups.items():
        if len(group_objs) <= 1:
            result.extend(group_objs)
            continue

        # Apply transforms before joining
        bpy.ops.object.select_all(action='DESELECT')
        for obj in group_objs:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = group_objs[0]
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

        bpy.ops.object.join()
        joined = bpy.context.active_object
        consolidate_materials(joined)
        result.append(joined)

    return result


def process_file(rel_path):
    """Process a single file. Returns (before_slots, after_slots) or None on error."""
    input_path = os.path.join(PKG_ROOT, rel_path)
    if not os.path.exists(input_path):
        print(f"  NOT FOUND: {input_path}")
        return None

    ext = os.path.splitext(input_path)[1].lower()
    filename = os.path.basename(input_path)
    print(f"\n{'='*60}")
    print(f"Processing: {rel_path}")

    clean_scene()

    if not import_file(input_path):
        return None

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    total_slots_before = sum(len(obj.data.materials) for obj in mesh_objects)
    objects_before = len(mesh_objects)

    unique_bases = set()
    for obj in mesh_objects:
        for mat in obj.data.materials:
            if mat:
                unique_bases.add(get_base_material_name(mat.name))

    print(f"  Before: {objects_before} objects, {total_slots_before} mat slots, {len(unique_bases)} unique bases")

    # Skip if ratio is too low (< 1.5x) after closer inspection
    if total_slots_before <= len(unique_bases) * 1.2:
        print(f"  Skipping (no significant redundancy)")
        return None

    # Step 1: Consolidate materials within each object
    for obj in list(mesh_objects):
        consolidate_materials(obj)

    # Step 2: Join objects sharing the same materials
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    join_objects_by_material(mesh_objects)

    # Final count
    mesh_objects_final = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    total_slots_after = sum(len(obj.data.materials) for obj in mesh_objects_final)
    objects_after = len(mesh_objects_final)

    print(f"  After: {objects_after} objects, {total_slots_after} mat slots")
    print(f"  Reduction: {total_slots_before} -> {total_slots_after} slots")

    if total_slots_after >= total_slots_before:
        print(f"  No improvement, skipping export")
        return None

    # Export to temp file first, then replace original
    # Always export as FBX (even for .3DS input) for Unity compatibility
    if ext == '.3ds':
        # For 3DS, export as FBX with same name but .fbx extension
        output_path = os.path.splitext(input_path)[0] + ".fbx"
    else:
        output_path = input_path

    temp_path = output_path + ".tmp"
    try:
        bpy.ops.export_scene.fbx(
            filepath=temp_path,
            use_selection=False,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            bake_space_transform=False,
            object_types={'MESH', 'ARMATURE', 'EMPTY'},
            use_mesh_modifiers=True,
            mesh_smooth_type='FACE',
            path_mode='COPY',
            embed_textures=False,
            axis_forward='-Z',
            axis_up='Y',
        )
        # Replace the original
        if os.path.exists(output_path) and output_path != input_path:
            pass  # new file, don't delete anything extra
        if os.path.exists(output_path):
            os.remove(output_path)
        os.rename(temp_path, output_path)
        print(f"  Saved: {output_path}")
    except Exception as e:
        print(f"  Export error: {e}")
        if os.path.exists(temp_path):
            os.remove(temp_path)
        return None

    return (total_slots_before, total_slots_after)


def main():
    print("="*60)
    print("BATCH SUBMESH MERGE")
    print("="*60)

    results = {}
    total_before = 0
    total_after = 0

    for rel_path in FILES:
        try:
            result = process_file(rel_path)
            if result:
                results[rel_path] = result
                total_before += result[0]
                total_after += result[1]
        except Exception as e:
            print(f"  ERROR processing {rel_path}: {e}")
            import traceback
            traceback.print_exc()

    print(f"\n{'='*60}")
    print("SUMMARY")
    print(f"{'='*60}")
    print(f"Files processed: {len(results)}/{len(FILES)}")
    for path, (before, after) in sorted(results.items(), key=lambda x: x[1][0] - x[1][1], reverse=True):
        print(f"  {os.path.basename(path)}: {before} -> {after} ({before - after} saved)")
    print(f"\nTotal: {total_before} -> {total_after} mat slots ({total_before - total_after} saved)")


if __name__ == "__main__":
    main()
