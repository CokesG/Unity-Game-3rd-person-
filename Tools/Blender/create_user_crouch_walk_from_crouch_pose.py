import os
import sys

import bpy


PREFIX = "mixamorig:"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_fbx(path):
    bpy.ops.import_scene.fbx(filepath=path)


def armatures():
    return [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]


def capture_pose(armature, frame):
    bpy.context.scene.frame_set(frame)
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
        bone.location = bone_pose["location"].copy()
        bone.rotation_euler = bone_pose["rotation"].copy()
        bone.scale = bone_pose["scale"].copy()


def add_rotation(armature, bone_name, degrees):
    bone = armature.pose.bones.get(PREFIX + bone_name)
    if bone is None:
        return

    bone.rotation_euler.x += radians(degrees[0])
    bone.rotation_euler.y += radians(degrees[1])
    bone.rotation_euler.z += radians(degrees[2])


def add_location(armature, bone_name, offset):
    bone = armature.pose.bones.get(PREFIX + bone_name)
    if bone is None:
        return

    bone.location.x += offset[0]
    bone.location.y += offset[1]
    bone.location.z += offset[2]


def radians(degrees):
    return degrees * 0.017453292519943295


def key_all_bones(armature, frame):
    bpy.context.scene.frame_set(frame)
    for bone in armature.pose.bones:
        bone.keyframe_insert(data_path="location", frame=frame)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="scale", frame=frame)


def action_fcurves(action):
    if hasattr(action, "fcurves"):
        return list(action.fcurves)

    curves = []
    for layer in getattr(action, "layers", []):
        for strip in getattr(layer, "strips", []):
            for channelbag in getattr(strip, "channelbags", []):
                curves.extend(getattr(channelbag, "fcurves", []))
    return curves


def apply_base_motion(armature, direction, phase):
    if direction == "Forward":
        add_location(armature, "Hips", (phase["sway"], 0.0, phase["bob"]))
        add_rotation(armature, "Hips", (0.0, 0.0, phase["sway"] * -3.0))
    elif direction == "Back":
        add_location(armature, "Hips", (-phase["sway"], 0.0, phase["bob"]))
        add_rotation(armature, "Hips", (0.0, 0.0, phase["sway"] * 3.0))
    elif direction == "Left":
        add_location(armature, "Hips", (-0.03 + phase["sway"] * 0.35, 0.0, phase["bob"]))
        add_rotation(armature, "Hips", (0.0, 0.0, 2.0))
    elif direction == "Right":
        add_location(armature, "Hips", (0.03 - phase["sway"] * 0.35, 0.0, phase["bob"]))
        add_rotation(armature, "Hips", (0.0, 0.0, -2.0))


def apply_forward_back_step(armature, sign, reverse):
    stride = -sign if reverse else sign
    add_rotation(armature, "LeftUpLeg", (8.0 * stride, 0.0, 1.5 * stride))
    add_rotation(armature, "RightUpLeg", (-8.0 * stride, 0.0, -1.5 * stride))
    add_rotation(armature, "LeftLeg", (-5.0 * stride, 0.0, 0.0))
    add_rotation(armature, "RightLeg", (5.0 * stride, 0.0, 0.0))
    add_rotation(armature, "LeftFoot", (2.0 * stride, 0.0, 0.0))
    add_rotation(armature, "RightFoot", (-2.0 * stride, 0.0, 0.0))


def apply_strafe_step(armature, sign, right):
    side = sign if right else -sign
    add_rotation(armature, "LeftUpLeg", (2.0, 0.0, -5.0 * side))
    add_rotation(armature, "RightUpLeg", (2.0, 0.0, -5.0 * side))
    add_rotation(armature, "LeftLeg", (-2.5, 0.0, 1.5 * side))
    add_rotation(armature, "RightLeg", (-2.5, 0.0, 1.5 * side))
    add_rotation(armature, "LeftFoot", (1.0, 0.0, -2.0 * side))
    add_rotation(armature, "RightFoot", (1.0, 0.0, -2.0 * side))


def apply_arm_counterbalance(armature, sign):
    add_rotation(armature, "LeftArm", (-1.5 * sign, 0.0, -1.0 * sign))
    add_rotation(armature, "RightArm", (1.5 * sign, 0.0, -1.0 * sign))


def apply_direction_pose(armature, base_pose, direction, frame, phase):
    reset_pose(armature, base_pose)
    apply_base_motion(armature, direction, phase)

    sign = phase["sign"]
    if direction in {"Forward", "Back"}:
        apply_forward_back_step(armature, sign, direction == "Back")
    elif direction in {"Left", "Right"}:
        apply_strafe_step(armature, sign, direction == "Right")

    apply_arm_counterbalance(armature, sign)
    key_all_bones(armature, frame)


def create_action(armature, base_pose, direction):
    action_name = f"User_CrouchWalk_{direction}_Procedural"
    action = bpy.data.actions.new(action_name)
    armature.animation_data.action = action

    phases = [
        (1, {"sign": 1.0, "bob": 0.0, "sway": -0.025}),
        (8, {"sign": 0.0, "bob": 0.015, "sway": 0.0}),
        (15, {"sign": -1.0, "bob": 0.0, "sway": 0.025}),
        (22, {"sign": 0.0, "bob": 0.015, "sway": 0.0}),
        (29, {"sign": 1.0, "bob": 0.0, "sway": -0.025}),
    ]

    for frame, phase in phases:
        apply_direction_pose(armature, base_pose, direction, frame, phase)

    action.frame_range = (1, 29)
    for curve in action_fcurves(action):
        for keyframe in curve.keyframe_points:
            keyframe.interpolation = "BEZIER"

    return action


def export_action(armature, out_path):
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 29
    scene.render.fps = 30

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        object_types={"ARMATURE"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
    )


def main():
    if "--" not in sys.argv:
        raise SystemExit(
            "Usage: blender -b --python create_user_crouch_walk_from_crouch_pose.py -- source_stand_to_crouch.fbx output_dir"
        )

    args = sys.argv[sys.argv.index("--") + 1 :]
    source_fbx = args[0]
    output_dir = args[1]

    clear_scene()
    import_fbx(source_fbx)

    found_armatures = armatures()
    if not found_armatures:
        raise RuntimeError("No armature found in source crouch FBX.")

    armature = found_armatures[0]
    if armature.animation_data is None or armature.animation_data.action is None:
        raise RuntimeError("Source crouch FBX did not import with an action.")

    source_action = armature.animation_data.action
    final_frame = int(source_action.frame_range[1])
    base_pose = capture_pose(armature, final_frame)

    for direction in ("Forward", "Back", "Left", "Right"):
        create_action(armature, base_pose, direction)
        export_action(
            armature,
            os.path.join(output_dir, f"User_CrouchWalk_{direction}_Procedural.fbx"),
        )


if __name__ == "__main__":
    main()
