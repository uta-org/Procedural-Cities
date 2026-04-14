"""
Blender script to merge redundant submeshes that share the same base material.
Run with: blender --background --python merge_submeshes.py -- <input.fbx> <output.fbx>

Logic:
1. Import FBX/3DS
2. For each mesh object, find faces sharing the same base material (strip .NNN suffix)
3. Consolidate material slots: replace .NNN variants with the base material
4. Remove empty material slots and re-index faces
5. Join objects that end up with identical single materials
6. Export back to FBX
"""

import bpy
import sys
import os
import re


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
    # Clean orphan data
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)


def import_file(filepath):
    """Import FBX or 3DS file."""
    ext = os.path.splitext(filepath)[1].lower()
    if ext in ('.fbx',):
        bpy.ops.import_scene.fbx(filepath=filepath)
    elif ext in ('.3ds',):
        bpy.ops.import_scene.autodesk_3ds(filepath=filepath)
    elif ext in ('.obj',):
        bpy.ops.import_scene.obj(filepath=filepath)
    else:
        print(f"Unsupported format: {ext}")
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

    # Build mapping: slot_index -> base_material_name
    base_names = {}
    for i, mat in enumerate(materials):
        if mat is None:
            base_names[i] = f"__none_{i}"
        else:
            base_names[i] = get_base_material_name(mat.name)

    # Find unique base names and their first occurrence index
    base_to_first_slot = {}
    slot_remap = {}  # old_slot -> new_slot

    for i, base_name in sorted(base_names.items()):
        if base_name not in base_to_first_slot:
            base_to_first_slot[base_name] = i
        slot_remap[i] = base_to_first_slot[base_name]

    # Check if any consolidation needed
    if all(slot_remap[i] == i for i in slot_remap):
        return 0

    original_count = len(materials)

    # Remap face material indices
    for poly in mesh.polygons:
        old_idx = poly.material_index
        if old_idx in slot_remap:
            poly.material_index = slot_remap[old_idx]

    # Now remove unused material slots (from the end to preserve indices)
    slots_to_remove = set()
    for old_idx, new_idx in slot_remap.items():
        if old_idx != new_idx:
            slots_to_remove.add(old_idx)

    # Remove slots from highest index to lowest
    for slot_idx in sorted(slots_to_remove, reverse=True):
        obj.active_material_index = slot_idx
        # Before removing, remap all references above this slot
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

    # Simpler approach: rebuild material list
    # First, collect which base slots are actually used
    used_slots = set()
    for poly in mesh.polygons:
        used_slots.add(poly.material_index)

    # Build new material list (only used base slots)
    new_materials = []
    old_to_new = {}
    for i in sorted(used_slots):
        if i < len(materials) and materials[i] is not None:
            old_to_new[i] = len(new_materials)
            new_materials.append(materials[i])
        else:
            old_to_new[i] = len(new_materials)
            new_materials.append(None)

    # Remap polygon material indices
    for poly in mesh.polygons:
        poly.material_index = old_to_new.get(poly.material_index, 0)

    # Clear and reassign materials
    mesh.materials.clear()
    for mat in new_materials:
        mesh.materials.append(mat)

    new_count = len(new_materials)
    return original_count - new_count


def join_objects_by_material(objects):
    """
    Join mesh objects that share the same set of materials.
    Returns the list of resulting objects.
    """
    if len(objects) <= 1:
        return objects

    # Group objects by their material set (frozenset of base material names)
    groups = {}
    for obj in objects:
        if obj.type != 'MESH':
            continue
        mat_set = frozenset(
            get_base_material_name(mat.name) if mat else "__none"
            for mat in obj.data.materials
        )
        if mat_set not in groups:
            groups[mat_set] = []
        groups[mat_set].append(obj)

    result = []
    for mat_set, group_objs in groups.items():
        if len(group_objs) <= 1:
            result.extend(group_objs)
            continue

        # Join all objects in this group
        bpy.ops.object.select_all(action='DESELECT')
        for obj in group_objs:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = group_objs[0]
        bpy.ops.object.join()

        joined = bpy.context.active_object
        # Re-consolidate materials after join (may have duplicated slots)
        consolidate_materials(joined)
        result.append(joined)

    return result


def process_file(input_path, output_path):
    """Main processing pipeline."""
    print(f"\n{'='*60}")
    print(f"Processing: {os.path.basename(input_path)}")
    print(f"{'='*60}")

    clean_scene()

    if not import_file(input_path):
        return False

    # Get all mesh objects
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    print(f"Imported {len(mesh_objects)} mesh objects")

    # Count total material slots before
    total_slots_before = sum(len(obj.data.materials) for obj in mesh_objects)
    unique_bases_before = set()
    for obj in mesh_objects:
        for mat in obj.data.materials:
            if mat:
                unique_bases_before.add(get_base_material_name(mat.name))

    print(f"Before: {total_slots_before} material slots, {len(unique_bases_before)} unique base materials")

    # Step 1: Consolidate materials within each object
    total_reduced = 0
    for obj in mesh_objects:
        reduced = consolidate_materials(obj)
        if reduced > 0:
            print(f"  {obj.name}: reduced {reduced} material slots")
            total_reduced += reduced

    # Step 2: Join objects sharing the same materials
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    objects_before = len(mesh_objects)
    result_objects = join_objects_by_material(mesh_objects)
    objects_after = len([obj for obj in bpy.context.scene.objects if obj.type == 'MESH'])

    # Count after
    mesh_objects_final = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    total_slots_after = sum(len(obj.data.materials) for obj in mesh_objects_final)

    print(f"After: {total_slots_after} material slots, {objects_after} objects")
    print(f"Reduction: {total_slots_before} -> {total_slots_after} slots, {objects_before} -> {objects_after} objects")

    # Export
    ext = os.path.splitext(input_path)[1].lower()
    # Always export as FBX for Unity compatibility
    bpy.ops.export_scene.fbx(
        filepath=output_path,
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

    print(f"Exported to: {output_path}")
    return True


def main():
    # Parse arguments after "--"
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        print("Usage: blender --background --python merge_submeshes.py -- <input> <output>")
        return

    if len(argv) < 2:
        print("Usage: blender --background --python merge_submeshes.py -- <input> <output>")
        return

    input_path = argv[0]
    output_path = argv[1]

    if not os.path.exists(input_path):
        print(f"Input file not found: {input_path}")
        return

    process_file(input_path, output_path)


if __name__ == "__main__":
    main()
