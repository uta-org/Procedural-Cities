"""
Process the 3 files that failed in Blender 4.3:
- toaster.FBX (FBX v6100)
- Saucepan-ts.fbx (ASCII FBX)
- 3DSTYLISH-fsd000.3DS (3DS format)

Uses Blender 3.3 which has legacy format support.
"""
import bpy
import sys
import os
import re


PKG_ROOT = r"C:\Users\arodriguezg\Unity3D\procedural-cities\procedural-cities-2022\src\Packages\Procedural-Cities\Import"

FILES = [
    ("Models/Toaster/toaster.FBX", "fbx"),
    ("Models/Saucepan/Saucepan-ts.fbx", "fbx"),
    ("Models/Sofa Hepburn Modular/3DSTYLISH-fsd000.3DS", "3ds"),
]


def get_base_material_name(mat_name):
    match = re.match(r'^(.+)\.(\d{3})$', mat_name)
    if match:
        return match.group(1)
    return mat_name


def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)


def consolidate_materials(obj):
    if obj.type != 'MESH' or not obj.data.materials:
        return 0
    mesh = obj.data
    materials = list(mesh.materials)
    if not materials:
        return 0

    base_names = {}
    for i, mat in enumerate(materials):
        base_names[i] = get_base_material_name(mat.name) if mat else f"__none_{i}"

    base_to_first = {}
    slot_remap = {}
    for i in range(len(materials)):
        bn = base_names[i]
        if bn not in base_to_first:
            base_to_first[bn] = i
        slot_remap[i] = base_to_first[bn]

    needs_work = any(slot_remap[i] != i for i in slot_remap)
    if not needs_work:
        return 0

    original_count = len(materials)
    for poly in mesh.polygons:
        if poly.material_index in slot_remap:
            poly.material_index = slot_remap[poly.material_index]

    used_slots = set(poly.material_index for poly in mesh.polygons)
    new_materials = []
    old_to_new = {}
    for i in sorted(used_slots):
        if i < len(materials):
            old_to_new[i] = len(new_materials)
            new_materials.append(materials[i])

    for poly in mesh.polygons:
        if poly.material_index in old_to_new:
            poly.material_index = old_to_new[poly.material_index]

    mesh.materials.clear()
    for mat in new_materials:
        mesh.materials.append(mat)

    return original_count - len(new_materials)


def join_objects_by_material(objects):
    if len(objects) <= 1:
        return objects

    groups = {}
    for obj in objects:
        if obj.type != 'MESH':
            continue
        mat_key = frozenset(
            get_base_material_name(mat.name) if mat else "__none"
            for mat in obj.data.materials
        ) if obj.data.materials else frozenset(["__empty"])
        groups.setdefault(mat_key, []).append(obj)

    result = []
    for mat_key, group_objs in groups.items():
        if len(group_objs) <= 1:
            result.extend(group_objs)
            continue
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


def process_file(rel_path, fmt):
    input_path = os.path.join(PKG_ROOT, rel_path)
    if not os.path.exists(input_path):
        print(f"  NOT FOUND: {input_path}")
        return None

    print(f"\nProcessing: {rel_path}")
    clean_scene()

    try:
        if fmt == "fbx":
            bpy.ops.import_scene.fbx(filepath=input_path)
        elif fmt == "3ds":
            bpy.ops.import_scene.autodesk_3ds(filepath=input_path)
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
    print(f"  Before: {objects_before} objects, {total_before} mat slots, {len(unique_bases)} unique")

    if total_before <= len(unique_bases) * 1.2:
        print(f"  Skipping (no redundancy)")
        return None

    for obj in list(mesh_objects):
        consolidate_materials(obj)

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    join_objects_by_material(mesh_objects)

    final = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    total_after = sum(len(obj.data.materials) for obj in final)
    print(f"  After: {len(final)} objects, {total_after} mat slots")

    if total_after >= total_before:
        print(f"  No improvement")
        return None

    # For 3DS, export as FBX
    if fmt == "3ds":
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
    for rel_path, fmt in FILES:
        try:
            process_file(rel_path, fmt)
        except Exception as e:
            print(f"  ERROR: {e}")
            import traceback
            traceback.print_exc()


if __name__ == "__main__":
    main()
