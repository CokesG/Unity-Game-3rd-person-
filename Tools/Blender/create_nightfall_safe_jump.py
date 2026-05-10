import math
import os
import sys

import bpy


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_fbx(path):
    bpy.ops.import_scene.fbx(filepath=path)


def armatures():
    return [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]


def meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def capture_pose(armature, frame):
    scene = bpy.context.scene
    scene.frame_set(frame)
    bpy.context.view_layer.update()

    pose = {}
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        pose[bone.name] = {
            "location": bone.location.copy(),
            "rotation": bone.rotation_euler.copy(),
            "scale": bone.scale.copy(),
        }
    return pose


def reset_pose(armature, pose):
    for bone in armature.pose.bones:
        bone_pose = pose[bone.name]
        bone.rotation_mode = "XYZ"
        bone.location = bone_pose["location"].copy()
        bone.rotation_euler = bone_pose["rotation"].copy()
        bone.scale = bone_pose["scale"].copy()


def add_rotation(armature, bone_name, degrees):
    bone = armature.pose.bones.get(bone_name)
    if bone is None:
        return

    bone.rotation_euler.x += math.radians(degrees[0])
    bone.rotation_euler.y += math.radians(degrees[1])
    bone.rotation_euler.z += math.radians(degrees[2])


def key_all_bones(armature, frame):
    bpy.context.scene.frame_set(frame)
    for bone in armature.pose.bones:
        bone.keyframe_insert(data_path="location", frame=frame)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="scale", frame=frame)


def apply_offsets(armature, base_pose, frame, rotations):
    reset_pose(armature, base_pose)
    for bone_name, degrees in rotations.items():
        add_rotation(armature, bone_name, degrees)
    key_all_bones(armature, frame)


def frame_rotations(stage):
    # Small, stable offsets only. The CharacterController owns vertical motion.
    poses = {
        "compress": {
            "Hips": (-4, 0, 0),
            "Spine": (3, 0, 0),
            "Spine01": (4, 0, 0),
            "LeftUpLeg": (10, 0, 3),
            "RightUpLeg": (10, 0, -3),
            "LeftLeg": (-18, 0, 0),
            "RightLeg": (-18, 0, 0),
            "LeftFoot": (6, 0, 0),
            "RightFoot": (6, 0, 0),
            "LeftArm": (-8, 0, -4),
            "RightArm": (-8, 0, 4),
            "LeftForeArm": (8, 0, 0),
            "RightForeArm": (8, 0, 0),
        },
        "lift": {
            "Hips": (4, 0, 0),
            "Spine": (-4, 0, 0),
            "Spine01": (-5, 0, 0),
            "LeftUpLeg": (-8, 0, 5),
            "RightUpLeg": (12, 0, -5),
            "LeftLeg": (14, 0, 0),
            "RightLeg": (-20, 0, 0),
            "LeftFoot": (-8, 0, 0),
            "RightFoot": (8, 0, 0),
            "LeftArm": (-14, 0, -8),
            "RightArm": (-14, 0, 8),
            "LeftForeArm": (12, 0, 0),
            "RightForeArm": (12, 0, 0),
        },
        "air": {
            "Hips": (2, 0, 0),
            "Spine": (-2, 0, 0),
            "Spine01": (-3, 0, 0),
            "LeftUpLeg": (14, 0, 4),
            "RightUpLeg": (-6, 0, -4),
            "LeftLeg": (-20, 0, 0),
            "RightLeg": (10, 0, 0),
            "LeftFoot": (8, 0, 0),
            "RightFoot": (-6, 0, 0),
            "LeftArm": (-10, 0, -6),
            "RightArm": (-10, 0, 6),
            "LeftForeArm": (10, 0, 0),
            "RightForeArm": (10, 0, 0),
        },
        "recover": {
            "Hips": (-2, 0, 0),
            "Spine": (2, 0, 0),
            "Spine01": (2, 0, 0),
            "LeftUpLeg": (8, 0, 2),
            "RightUpLeg": (8, 0, -2),
            "LeftLeg": (-12, 0, 0),
            "RightLeg": (-12, 0, 0),
            "LeftArm": (-6, 0, -3),
            "RightArm": (-6, 0, 3),
        },
    }
    return poses[stage]


def main():
    if "--" not in sys.argv:
        raise SystemExit(
            "Usage: blender -b --python create_nightfall_safe_jump.py -- source_run.fbx out.fbx action_name"
        )

    args = sys.argv[sys.argv.index("--") + 1 :]
    source_run, out_path, action_name = args[:3]

    clear_scene()
    import_fbx(source_run)

    found_armatures = armatures()
    if not found_armatures:
        raise RuntimeError("No armature found in source run FBX.")

    armature = found_armatures[0]
    armature.name = "NightfallVanguard_FullQuality_Armature"
    mesh_objects = meshes()
    if not mesh_objects:
        raise RuntimeError("No mesh found in source run FBX.")

    base_pose = capture_pose(armature, 5)
    action = bpy.data.actions.new(action_name)
    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = action

    apply_offsets(armature, base_pose, 1, frame_rotations("compress"))
    apply_offsets(armature, base_pose, 5, frame_rotations("lift"))
    apply_offsets(armature, base_pose, 11, frame_rotations("air"))
    apply_offsets(armature, base_pose, 17, frame_rotations("recover"))
    action.frame_range = (1, 17)

    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 17
    scene.render.fps = 30

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objects:
        obj.select_set(True)
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
    )


if __name__ == "__main__":
    main()
