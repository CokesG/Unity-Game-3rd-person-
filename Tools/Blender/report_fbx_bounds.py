import sys

import bpy


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b --python report_fbx_bounds.py -- file.fbx frame")

    args = sys.argv[sys.argv.index("--") + 1 :]
    fbx_path = args[0]
    frame = int(args[1]) if len(args) > 1 else 1

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()

    mins = [float("inf"), float("inf"), float("inf")]
    maxs = [float("-inf"), float("-inf"), float("-inf")]
    depsgraph = bpy.context.evaluated_depsgraph_get()

    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue

        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            for vertex in mesh.vertices:
                co = evaluated.matrix_world @ vertex.co
                mins[0] = min(mins[0], co.x)
                mins[1] = min(mins[1], co.y)
                mins[2] = min(mins[2], co.z)
                maxs[0] = max(maxs[0], co.x)
                maxs[1] = max(maxs[1], co.y)
                maxs[2] = max(maxs[2], co.z)
        finally:
            evaluated.to_mesh_clear()

    print(f"BOUNDS_MIN {mins}")
    print(f"BOUNDS_MAX {maxs}")
    print(f"HEIGHT_Z {maxs[2] - mins[2]}")


if __name__ == "__main__":
    main()
