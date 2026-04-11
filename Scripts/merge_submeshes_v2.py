"""
Blender script v2: Merge redundant submeshes that share the same base material.
Uses Blender-native operators to avoid corrupting material-to-face assignments.

Run with: blender --background --python merge_submeshes_v2.py
"""

import bpy
import sys
import os
import re


PKG_ROOT = r"C:\Users\arodriguezg\Unity3D\procedural-cities\procedural-cities-2022\src\Packages\Procedural-Cities\Import"
BACKUP_ROOT = os.path.join(PKG_ROOT, "_backups_originals")

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
    "Models/Street/stairmesh.fbx",
    "Models/Office/dispensermesh.fbx",
    "Models/Living/Shelf/shelf4_Box07.fbx",
    "Models/Bathroom/sink.fbx",
    "Models/Living/bed.fbx",
    "Models/Bathroom/toiletmesh.fbx",
    "Models/Living/Shelf/shelf2 - Copy.fbx",
    "Models/Kitchen/kitchen4.fbx",
    "Models/Living/wardrobe.fbx",
    "Models/Bathroom/toilet.fbx",
]


def get_base_material_name(mat_name):
    """Strip .NNN numeric suffix to get base material name."""
    match = re.match(r'^(.+)\.(\d{3})$', mat_name)
    if match:
        return match.group(1)
    return mat_name


def clean_scene():
    """Remove all objects and orphan data from the scene."""
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


def consolidate_materials_v2(obj):
    """
    Consolidate .NNN variant materials into their base using direct polygon remapping.
    Does NOT clear the material list; instead remaps faces and then removes unused slots.
    """
    if obj.type != 'MESH' or not obj.data.materials:
        return 0

    mesh = obj.data
    mat_count = len(mesh.materials)
    if mat_count <= 1:
        return 0

    # Build base_name -> first slot index mapping
    base_to_slot = {}
    remap = {}  # old_slot -> target_slot
    for i in range(mat_count):
        mat = mesh.materials[i]
        if mat is None:
            remap[i] = i
            continue
        base_name = get_base_material_name(mat.name)
        if base_name not in base_to_slot:
            base_to_slot[base_name] = i
        remap[i] = base_to_slot[base_name]

    # Check if anything needs remapping
    needs_work = any(remap[i] != i for i in remap)
    if not needs_work:
        return 0

    # Remap polygon material indices
    for poly in mesh.polygons:
        old_idx = poly.material_index
        if old_idx in remap and remap[old_idx] != old_idx:
            poly.material_index = remap[old_idx]

    # Now remove unused material slots using Blender operator
    # We need to be in object mode with this object selected
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    # Remove slots that are no longer referenced by any face
    used_slots = set()
    for poly in mesh.polygons:
        used_slots.add(poly.material_index)

    # Remove from highest to lowest to preserve indices
    removed = 0
    for slot_idx in range(mat_count - 1, -1, -1):
        if slot_idx not in used_slots:
            obj.active_material_index = slot_idx
            bpy.ops.object.material_slot_remove()
            removed += 1
            # After removing a slot, update used_slots indices
            new_used = set()
            for s in used_slots:
                if s > slot_idx:
                    new_used.add(s - 1)
                elif s < slot_idx:
                    new_used.add(s)
                # s == slot_idx shouldn't be in used_slots for removed slots
            used_slots = new_used

    return removed


def join_objects_by_material(objects):
    """Join mesh objects sharing the same set of base materials."""
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
        groups.setdefault(mat_key, []).append(obj)

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
        # After join, Blender may have duplicated material slots; clean up
        consolidate_materials_v2(joined)
        result.append(joined)

    return result


def process_file(rel_path):
    """Process a single file from backup, overwriting the output."""
    # Always read from BACKUP to start fresh
    backup_path = os.path.join(BACKUP_ROOT, rel_path)
    output_path = os.path.join(PKG_ROOT, rel_path)

    if not os.path.exists(backup_path):
        print(f"  Backup not found: {backup_path}")
        return None

    print(f"\n{'='*60}")
    print(f"Processing: {rel_path}")

    clean_scene()

    try:
        bpy.ops.import_scene.fbx(filepath=backup_path)
    except Exception as e:
        print(f"  Import failed: {e}")
        return None

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    total_before = sum(len(obj.data.materials) for obj in mesh_objects)
    objects_before = len(mesh_objects)

    unique_bases = set()
    for obj in mesh_objects:
        for mat in obj.data.materials:
            if mat:
                unique_bases.add(get_base_material_name(mat.name))

    print(f"  Before: {objects_before} objects, {total_before} mat slots, {len(unique_bases)} unique bases")

    if total_before <= len(unique_bases) * 1.2:
        print(f"  Skipping (no redundancy)")
        return None

    # Step 1: Consolidate materials within each object
    for obj in list(mesh_objects):
        reduced = consolidate_materials_v2(obj)
        if reduced > 0:
            print(f"    {obj.name}: removed {reduced} unused slots")

    # Step 2: Join objects that share the same materials
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    join_objects_by_material(mesh_objects)

    # Count after
    final_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    total_after = sum(len(obj.data.materials) for obj in final_objects)
    objects_after = len(final_objects)

    # Verify: each mesh should have subMeshCount = number of materials
    for obj in final_objects:
        used = set()
        for poly in obj.data.polygons:
            used.add(poly.material_index)
        mat_count = len(obj.data.materials)
        mat_names = [m.name if m else "None" for m in obj.data.materials]
        print(f"    {obj.name}: {mat_count} mat slots, {len(used)} used indices, matls={mat_names[:5]}{'...' if len(mat_names)>5 else ''}")

    print(f"  After: {objects_after} objects, {total_after} mat slots")
    print(f"  Reduction: {total_before} -> {total_after} slots, {objects_before} -> {objects_after} objects")

    if total_after >= total_before:
        print(f"  No improvement, skipping export")
        return None

    # Export to temp, then replace
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
        if os.path.exists(output_path):
            os.remove(output_path)
        os.rename(temp_path, output_path)
        print(f"  Saved: {output_path}")
        return (total_before, total_after)
    except Exception as e:
        print(f"  Export error: {e}")
        if os.path.exists(temp_path):
            os.remove(temp_path)
        return None


def main():
    print("=" * 60)
    print("BATCH SUBMESH MERGE v2 (from backups)")
    print("=" * 60)

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
            print(f"  ERROR: {e}")
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
