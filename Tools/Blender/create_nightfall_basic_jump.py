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


def capture_base_pose(armature, frame):
    scene = bpy.context.scene
    scene.frame_set(frame)
    bpy.context.view_layer.update()

    base = {}
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        base[bone.name] = {
            "location": bone.location.copy(),
            "rotation": bone.rotation_euler.copy(),
            "scale": bone.scale.copy(),
        }

    return base


def reset_pose(armature, base_pose):
    for bone in armature.pose.bones:
        pose = base_pose[bone.name]
        bone.rotation_mode = "XYZ"
        bone.location = pose["location"].copy()
        bone.rotation_euler = pose["rotation"].copy()
        bone.scale = pose["scale"].copy()


def add_rotation(armature, bone_name, degrees):
    bone = armature.pose.bones.get(bone_name)
    if bone is None:
        return

    bone.rotation_euler.x += math.radians(degrees[0])
    bone.rotation_euler.y += math.radians(degrees[1])
    bone.rotation_euler.z += math.radians(degrees[2])


def add_location(armature, bone_name, offset):
    bone = armature.pose.bones.get(bone_name)
    if bone is None:
        return

    bone.location.x += offset[0]
    bone.location.y += offset[1]
    bone.location.z += offset[2]


def key_all_bones(armature, frame):
    bpy.context.scene.frame_set(frame)
    for bone in armature.pose.bones:
        bone.keyframe_insert(data_path="location", frame=frame)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="scale", frame=frame)


def apply_pose_offsets(armature, base_pose, frame, offsets):
    reset_pose(armature, base_pose)

    for bone_name, offset in offsets.get("loc", {}).items():
        add_location(armature, bone_name, offset)

    for bone_name, degrees in offsets.get("rot", {}).items():
        add_rotation(armature, bone_name, degrees)

    key_all_bones(armature, frame)


def export_current_action(armature, mesh_objects, out_path, frame_start, frame_end):
    scene = bpy.context.scene
    scene.frame_start = frame_start
    scene.frame_end = frame_end
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


def neutral_pose():
    return {
        "loc": {},
        "rot": {},
    }


def crouch_pose(depth=1.0):
    return {
        "loc": {
            "Hips": (0.0, 0.0, -6.0 * depth),
        },
        "rot": {
            "Hips": (-12.0 * depth, 0.0, 0.0),
            "Spine": (8.0 * depth, 0.0, 0.0),
            "Spine01": (10.0 * depth, 0.0, 0.0),
            "LeftUpLeg": (48.0 * depth, 0.0, 10.0 * depth),
            "RightUpLeg": (48.0 * depth, 0.0, -10.0 * depth),
            "LeftLeg": (-78.0 * depth, 0.0, 0.0),
            "RightLeg": (-78.0 * depth, 0.0, 0.0),
            "LeftFoot": (28.0 * depth, 0.0, 0.0),
            "RightFoot": (28.0 * depth, 0.0, 0.0),
            "LeftArm": (-35.0 * depth, 0.0, -18.0 * depth),
            "RightArm": (-35.0 * depth, 0.0, 18.0 * depth),
            "LeftForeArm": (35.0 * depth, 0.0, 0.0),
            "RightForeArm": (35.0 * depth, 0.0, 0.0),
        },
    }


def takeoff_pose():
    return {
        "loc": {
            "Hips": (0.0, 0.0, 8.0),
        },
        "rot": {
            "Hips": (8.0, 0.0, 0.0),
            "Spine": (-12.0, 0.0, 0.0),
            "Spine01": (-18.0, 0.0, 0.0),
            "LeftUpLeg": (-28.0, 0.0, 4.0),
            "RightUpLeg": (-28.0, 0.0, -4.0),
            "LeftLeg": (34.0, 0.0, 0.0),
            "RightLeg": (34.0, 0.0, 0.0),
            "LeftFoot": (-28.0, 0.0, 0.0),
            "RightFoot": (-28.0, 0.0, 0.0),
            "LeftArm": (-78.0, 0.0, -20.0),
            "RightArm": (-78.0, 0.0, 20.0),
            "LeftForeArm": (45.0, 0.0, 0.0),
            "RightForeArm": (45.0, 0.0, 0.0),
        },
    }


def air_pose(variant=0.0):
    return {
        "loc": {
            "Hips": (0.0, 0.0, 4.0 + variant * 1.5),
        },
        "rot": {
            "Hips": (6.0 + variant * 2.0, 0.0, 0.0),
            "Spine": (-8.0, 0.0, 0.0),
            "Spine01": (-10.0, 0.0, 0.0),
            "LeftUpLeg": (26.0, 0.0, 8.0),
            "RightUpLeg": (26.0, 0.0, -8.0),
            "LeftLeg": (-42.0, 0.0, 0.0),
            "RightLeg": (-42.0, 0.0, 0.0),
            "LeftFoot": (18.0, 0.0, 0.0),
            "RightFoot": (18.0, 0.0, 0.0),
            "LeftArm": (-45.0, 0.0, -12.0),
            "RightArm": (-45.0, 0.0, 12.0),
            "LeftForeArm": (26.0, 0.0, 0.0),
            "RightForeArm": (26.0, 0.0, 0.0),
        },
    }


def land_recover_pose():
    return {
        "loc": {
            "Hips": (0.0, 0.0, -3.0),
        },
        "rot": {
            "Hips": (-6.0, 0.0, 0.0),
            "Spine": (5.0, 0.0, 0.0),
            "Spine01": (6.0, 0.0, 0.0),
            "LeftUpLeg": (20.0, 0.0, 4.0),
            "RightUpLeg": (20.0, 0.0, -4.0),
            "LeftLeg": (-34.0, 0.0, 0.0),
            "RightLeg": (-34.0, 0.0, 0.0),
            "LeftArm": (-20.0, 0.0, -8.0),
            "RightArm": (-20.0, 0.0, 8.0),
        },
    }


def create_clip(armature, base_pose, action_name, keyed_offsets):
    action = bpy.data.actions.new(action_name)
    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = action

    for frame, offsets in keyed_offsets:
        apply_pose_offsets(armature, base_pose, frame, offsets)

    action.frame_range = (keyed_offsets[0][0], keyed_offsets[-1][0])
    return action


def load_base_scene(base_fbx):
    clear_scene()
    import_fbx(base_fbx)

    found_armatures = armatures()
    if not found_armatures:
        raise RuntimeError("No armature found in base FBX.")

    armature = found_armatures[0]
    armature.name = "NightfallVanguard_FullQuality_Armature"

    mesh_objects = meshes()
    if not mesh_objects:
        raise RuntimeError("No mesh found in base FBX.")

    return armature, mesh_objects


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b --python create_nightfall_basic_jump.py -- base_idle.fbx output_dir")

    args = sys.argv[sys.argv.index("--") + 1 :]
    base_fbx = args[0]
    output_dir = args[1]

    clips = [
        (
            "Nightfall_FullQuality_AirLoop_Procedural",
            "NightfallVanguard_FullQuality_AirLoop_Procedural.fbx",
            [
                (1, air_pose(0.0)),
                (10, air_pose(0.35)),
                (20, air_pose(0.0)),
            ],
        ),
        (
            "Nightfall_FullQuality_Land_Procedural",
            "NightfallVanguard_FullQuality_Land_Procedural.fbx",
            [
                (1, air_pose(0.0)),
                (5, crouch_pose(0.85)),
                (10, land_recover_pose()),
                (14, neutral_pose()),
            ],
        ),
    ]

    for action_name, filename, keyed_offsets in clips:
        armature, mesh_objects = load_base_scene(base_fbx)
        base_pose = capture_base_pose(armature, keyed_offsets[0][0])
        create_clip(armature, base_pose, action_name, keyed_offsets)
        out_path = os.path.join(output_dir, filename)
        export_current_action(armature, mesh_objects, out_path, keyed_offsets[0][0], keyed_offsets[-1][0])


if __name__ == "__main__":
    main()
