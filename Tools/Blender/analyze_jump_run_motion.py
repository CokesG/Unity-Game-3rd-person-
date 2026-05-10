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


def armatures():
    return [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]


def world_z(armature, bone_name):
    bone = armature.pose.bones.get(bone_name)
    if bone is None:
        return None

    return (armature.matrix_world @ bone.matrix).translation.z


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b --python analyze_jump_run_motion.py -- source.glb out.json")

    args = sys.argv[sys.argv.index("--") + 1 :]
    source_path = args[0]
    out_path = args[1]

    clear_scene()
    import_asset(source_path)

    found_armatures = armatures()
    if not found_armatures:
        raise RuntimeError("No armature found.")

    armature = found_armatures[0]
    action = armature.animation_data.action if armature.animation_data else None
    if action is None:
        raise RuntimeError("No action found.")

    frame_start = int(action.frame_range[0])
    frame_end = int(action.frame_range[1])
    scene = bpy.context.scene

    samples = []
    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()

        left_foot = world_z(armature, "LeftFoot")
        right_foot = world_z(armature, "RightFoot")
        left_toe = world_z(armature, "LeftToeBase")
        right_toe = world_z(armature, "RightToeBase")
        foot_values = [value for value in [left_foot, right_foot, left_toe, right_toe] if value is not None]

        samples.append(
            {
                "frame": frame,
                "hips_z": world_z(armature, "Hips"),
                "left_foot_z": left_foot,
                "right_foot_z": right_foot,
                "left_toe_z": left_toe,
                "right_toe_z": right_toe,
                "min_foot_z": min(foot_values) if foot_values else None,
            }
        )

    if samples:
        base_foot = min(sample["min_foot_z"] for sample in samples if sample["min_foot_z"] is not None)
        for sample in samples:
            sample["foot_lift"] = sample["min_foot_z"] - base_foot if sample["min_foot_z"] is not None else None

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as handle:
        json.dump(
            {
                "source": source_path,
                "action": action.name,
                "frame_range": [frame_start, frame_end],
                "samples": samples,
            },
            handle,
            indent=2,
        )


if __name__ == "__main__":
    main()
