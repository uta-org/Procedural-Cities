import bpy
import sys

# https://blender.stackexchange.com/questions/192871/how-to-delete-all-objects-cameras-meshes-etc-using-python-scripting
def deleteAllObjects():
    """
    Deletes all objects in the current scene
    """
    deleteListObjects = ['MESH', 'CURVE', 'SURFACE', 'META', 'FONT', 'HAIR', 'POINTCLOUD', 'VOLUME', 'GPENCIL',
                     'ARMATURE', 'LATTICE', 'EMPTY', 'LIGHT', 'LIGHT_PROBE', 'CAMERA', 'SPEAKER']

    # Select all objects in the scene to be deleted:

    for o in bpy.context.scene.objects:
        for i in deleteListObjects:
            if o.type == i:
                o.select_set(False)
            else:
                o.select_set(True)
    # Deletes all selected objects in the scene:

    bpy.ops.object.delete()

print("Blender export scene in FBX Format in file fbx="+sys.argv[-1]+"; gltf="+sys.argv[-2])

deleteAllObjects()

# Doc can be found here: https://docs.blender.org/api/current/bpy.ops.export_scene.html
bpy.ops.import_scene.gltf(filepath=sys.argv[-2])
bpy.ops.export_scene.fbx(filepath=sys.argv[-1])
