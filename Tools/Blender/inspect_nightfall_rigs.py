import json
import os
import sys

import bpy


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_asset(path):
    ext = os.path.splitext(path)[1].lower()
    if ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=path)
    else:
        raise ValueError(f"Unsupported asset type: {path}")


def action_curve_count(action):
    if action is None:
        return 0
    if hasattr(action, "fcurves"):
        return len(action.fcurves)
    if hasattr(action, "layers"):
        total = 0
        for layer in action.layers:
            for strip in getattr(layer, "strips", []):
                for channelbag in getattr(strip, "channelbags", []):
                    total += len(getattr(channelbag, "fcurves", []))
        return total
    return 0


def armature_summary(obj):
    action = obj.animation_data.action if obj.animation_data else None
    return {
        "name": obj.name,
        "type": obj.type,
        "location": list(obj.location),
        "rotation_euler": list(obj.rotation_euler),
        "scale": list(obj.scale),
        "bone_count": len(obj.data.bones),
        "bones": [bone.name for bone in obj.data.bones],
        "action": action.name if action else None,
        "frame_range": list(action.frame_range) if action else None,
        "fcurves": action_curve_count(action),
    }


def mesh_summary(obj):
    return {
        "name": obj.name,
        "vertex_count": len(obj.data.vertices),
        "modifiers": [modifier.type for modifier in obj.modifiers],
        "parent": obj.parent.name if obj.parent else None,
    }


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b --python inspect_nightfall_rigs.py -- source.glb target.fbx out.json")

    source_path, target_path, out_path = sys.argv[sys.argv.index("--") + 1 : sys.argv.index("--") + 4]
    result = {}

    clear_scene()
    import_asset(source_path)
    result["source_armatures"] = [armature_summary(obj) for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    result["source_meshes"] = [mesh_summary(obj) for obj in bpy.context.scene.objects if obj.type == "MESH"]

    clear_scene()
    import_asset(target_path)
    result["target_armatures"] = [armature_summary(obj) for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    result["target_meshes"] = [mesh_summary(obj) for obj in bpy.context.scene.objects if obj.type == "MESH"]

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as handle:
        json.dump(result, handle, indent=2)


if __name__ == "__main__":
    main()
