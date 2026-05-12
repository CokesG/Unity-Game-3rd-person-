import math
import os
import sys

import bpy
from mathutils import Vector


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


def create_ik_target(name):
    target = bpy.data.objects.new(name, None)
    target.empty_display_type = "SPHERE"
    target.empty_display_size = 0.05
    bpy.context.collection.objects.link(target)
    return target


def setup_hand_ik(armature):
    targets = {}
    for hand_name in ("LeftHand", "RightHand"):
        hand = armature.pose.bones.get(hand_name)
        if hand is None:
            continue

        target = create_ik_target(f"CrouchWalkIK_{hand_name}")
        constraint = hand.constraints.new(type="IK")
        constraint.name = "CrouchWalk_WeaponReadyIK"
        constraint.target = target
        constraint.chain_count = 2
        constraint.use_rotation = True
        constraint.influence = 1.0
        targets[hand_name] = target

    return targets


def mesh_bounds(mesh_objects):
    bpy.context.view_layer.update()
    min_corner = Vector((float("inf"), float("inf"), float("inf")))
    max_corner = Vector((float("-inf"), float("-inf"), float("-inf")))
    has_bounds = False

    for mesh in mesh_objects:
        for corner in mesh.bound_box:
            world = mesh.matrix_world @ Vector(corner)
            min_corner.x = min(min_corner.x, world.x)
            min_corner.y = min(min_corner.y, world.y)
            min_corner.z = min(min_corner.z, world.z)
            max_corner.x = max(max_corner.x, world.x)
            max_corner.y = max(max_corner.y, world.y)
            max_corner.z = max(max_corner.z, world.z)
            has_bounds = True

    if not has_bounds:
        return Vector((0.0, 0.0, 0.0)), Vector((0.0, 0.0, 0.0))

    return min_corner, max_corner


def key_all_bones(armature, frame):
    bpy.context.scene.frame_set(frame)
    for bone in armature.pose.bones:
        bone.keyframe_insert(data_path="location", frame=frame)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame)
        bone.keyframe_insert(data_path="scale", frame=frame)


def mesh_min_z(mesh_objects):
    min_corner, _ = mesh_bounds(mesh_objects)
    return min_corner.z


def ground_pose_to_mesh(armature, mesh_objects, target_min_z=0.015):
    hips = armature.pose.bones.get("Hips")
    if hips is None:
        return

    for _ in range(3):
        current_min_z = mesh_min_z(mesh_objects)
        error = target_min_z - current_min_z
        if abs(error) < 0.005:
            return

        original_y = hips.location.y
        hips.location.y = original_y + 1.0
        sampled_min_z = mesh_min_z(mesh_objects)
        hips.location.y = original_y

        slope = sampled_min_z - current_min_z
        if abs(slope) < 0.0001:
            return

        hips.location.y += max(-25.0, min(25.0, error / slope))


def action_fcurves(action):
    if hasattr(action, "fcurves"):
        return list(action.fcurves)

    curves = []
    for layer in getattr(action, "layers", []):
        for strip in getattr(layer, "strips", []):
            for channelbag in getattr(strip, "channelbags", []):
                curves.extend(getattr(channelbag, "fcurves", []))
    return curves


def apply_base_crouch(armature, bob=0.0, sway=0.0):
    add_location(armature, "Hips", (sway, -17.5 + bob, -8.0))
    add_rotation(armature, "Hips", (-21.0, 0.0, 0.0))
    add_rotation(armature, "Spine", (13.0, 0.0, 0.0))
    add_rotation(armature, "Spine01", (15.0, 0.0, 0.0))
    add_rotation(armature, "Spine02", (8.0, 0.0, 0.0))
    add_rotation(armature, "LeftUpLeg", (84.0, 0.0, -7.0))
    add_rotation(armature, "RightUpLeg", (84.0, 0.0, 7.0))
    add_rotation(armature, "LeftLeg", (-128.0, 0.0, 0.0))
    add_rotation(armature, "RightLeg", (-128.0, 0.0, 0.0))
    add_rotation(armature, "LeftFoot", (50.0, 0.0, 0.0))
    add_rotation(armature, "RightFoot", (50.0, 0.0, 0.0))

    # Start the arms from a compact, lowered pose; IK below keeps the hands in a
    # two-hand weapon-ready pocket during the bake.
    add_rotation(armature, "LeftArm", (0.0, 0.0, 60.0))
    add_rotation(armature, "RightArm", (0.0, 0.0, -60.0))
    add_rotation(armature, "LeftForeArm", (20.0, 0.0, 0.0))
    add_rotation(armature, "RightForeArm", (20.0, 0.0, 0.0))


def apply_forward_back_step(armature, sign, reverse=False):
    stride = -sign if reverse else sign
    add_rotation(armature, "LeftUpLeg", (6.5 * stride, 0.0, 0.0))
    add_rotation(armature, "RightUpLeg", (-6.5 * stride, 0.0, 0.0))
    add_rotation(armature, "LeftLeg", (-4.5 * stride, 0.0, 0.0))
    add_rotation(armature, "RightLeg", (4.5 * stride, 0.0, 0.0))
    add_rotation(armature, "LeftFoot", (2.5 * stride, 0.0, 0.0))
    add_rotation(armature, "RightFoot", (-2.5 * stride, 0.0, 0.0))


def apply_strafe_step(armature, sign, right=False):
    side = sign if right else -sign
    add_rotation(armature, "Hips", (0.0, 0.0, -2.0 if right else 2.0))
    add_rotation(armature, "LeftUpLeg", (2.0, 0.0, -5.0 * side))
    add_rotation(armature, "RightUpLeg", (2.0, 0.0, -5.0 * side))
    add_rotation(armature, "LeftLeg", (-3.0, 0.0, 1.0 * side))
    add_rotation(armature, "RightLeg", (-3.0, 0.0, 1.0 * side))
    add_rotation(armature, "LeftFoot", (1.0, 0.0, -1.5 * side))
    add_rotation(armature, "RightFoot", (1.0, 0.0, -1.5 * side))


def place_hand_targets(ik_targets, mesh_objects, frame, sign):
    min_corner, max_corner = mesh_bounds(mesh_objects)
    center = (min_corner + max_corner) * 0.5

    hand_positions = {
        "LeftHand": Vector((-0.18, center.y - 0.34 - 0.015 * sign, min_corner.z + 0.83)),
        "RightHand": Vector((0.18, center.y - 0.40 + 0.015 * sign, min_corner.z + 0.80)),
    }

    for hand_name, position in hand_positions.items():
        target = ik_targets.get(hand_name)
        if target is None:
            continue

        target.location = position
        target.keyframe_insert(data_path="location", frame=frame)


def apply_direction_pose(armature, mesh_objects, ik_targets, base_pose, direction, frame, sign, bob, sway):
    reset_pose(armature, base_pose)
    apply_base_crouch(armature, bob, sway)

    if direction == "Forward":
        apply_forward_back_step(armature, sign)
    elif direction == "Back":
        apply_forward_back_step(armature, sign, reverse=True)
    elif direction == "Left":
        apply_strafe_step(armature, sign, right=False)
    elif direction == "Right":
        apply_strafe_step(armature, sign, right=True)

    ground_pose_to_mesh(armature, mesh_objects)
    place_hand_targets(ik_targets, mesh_objects, frame, sign)
    for _ in range(4):
        bpy.context.view_layer.update()

    key_all_bones(armature, frame)


def create_action(armature, mesh_objects, base_pose, direction):
    action = bpy.data.actions.new(f"Nightfall_FullQuality_CrouchWalk_{direction}_Procedural")
    armature.animation_data.action = action
    ik_targets = setup_hand_ik(armature)

    phases = [
        (1, 1.0, 0.0, -0.045),
        (9, 0.0, 0.10, 0.0),
        (17, -1.0, 0.0, 0.045),
        (25, 0.0, 0.10, 0.0),
        (33, 1.0, 0.0, -0.045),
    ]

    for frame, sign, bob, sway in phases:
        apply_direction_pose(armature, mesh_objects, ik_targets, base_pose, direction, frame, sign, bob, sway)

    action.frame_range = (1, 33)
    for curve in action_fcurves(action):
        for keyframe in curve.keyframe_points:
            keyframe.interpolation = "BEZIER"

    return action


def export_current_action(armature, mesh_objects, out_path):
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 33
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


def load_source(source_fbx):
    clear_scene()
    import_fbx(source_fbx)

    found_armatures = armatures()
    if not found_armatures:
        raise RuntimeError("No armature found in source FBX.")

    armature = found_armatures[0]
    armature.name = "NightfallVanguard_FullQuality_Armature"

    mesh_objects = meshes()
    if not mesh_objects:
        raise RuntimeError("No mesh found in source FBX.")

    if armature.animation_data is None:
        armature.animation_data_create()

    return armature, mesh_objects


def main():
    if "--" not in sys.argv:
        raise SystemExit(
            "Usage: blender -b --python create_nightfall_crouch_walk_procedural.py -- source_idle.fbx output_dir"
        )

    args = sys.argv[sys.argv.index("--") + 1 :]
    source_fbx = args[0]
    output_dir = args[1]

    for direction in ("Forward", "Back", "Left", "Right"):
        armature, mesh_objects = load_source(source_fbx)
        base_pose = capture_pose(armature, 1)
        create_action(armature, mesh_objects, base_pose, direction)
        export_current_action(
            armature,
            mesh_objects,
            os.path.join(output_dir, f"NightfallVanguard_FullQuality_CrouchWalk_{direction}_Procedural.fbx"),
        )


if __name__ == "__main__":
    main()
