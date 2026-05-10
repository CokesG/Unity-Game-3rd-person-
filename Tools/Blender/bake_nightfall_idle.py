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


def meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def get_action(obj):
    return obj.animation_data.action if obj.animation_data else None


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b --python bake_nightfall_idle.py -- source.glb target_model.fbx out.fbx [baked_action_name]")

    args = sys.argv[sys.argv.index("--") + 1 :]
    source_path, target_path, out_path = args[:3]
    baked_action_name = args[3] if len(args) > 3 else "Nightfall_FullQuality_Idle_Baked"

    clear_scene()

    import_asset(source_path)
    source_armature = armatures()[0]
    source_action = get_action(source_armature)
    if source_action is None:
        raise RuntimeError("Source file did not import with an armature action.")

    source_armature.name = "Nightfall_Source_Animation_Armature"
    source_meshes = set(meshes())

    import_asset(target_path)
    target_armature = [obj for obj in armatures() if obj != source_armature][0]
    # Keep this exact name so Unity's copied Humanoid Avatar hierarchy matches.
    target_armature.name = "NightfallVanguard_FullQuality_Armature"

    target_meshes = [obj for obj in meshes() if obj not in source_meshes]
    if not target_meshes:
        raise RuntimeError("Target full-quality mesh was not imported.")

    missing = []
    for target_pose_bone in target_armature.pose.bones:
        source_pose_bone = source_armature.pose.bones.get(target_pose_bone.name)
        if source_pose_bone is None:
            missing.append(target_pose_bone.name)
            continue
        constraint = target_pose_bone.constraints.new(type="COPY_TRANSFORMS")
        constraint.name = "BakeFromSourceIdle"
        constraint.target = source_armature
        constraint.subtarget = source_pose_bone.name
        constraint.target_space = "LOCAL"
        constraint.owner_space = "LOCAL"

    if missing:
        raise RuntimeError(f"Missing source bones: {', '.join(missing)}")

    scene = bpy.context.scene
    frame_start = max(1, int(source_action.frame_range[0]))
    frame_end = max(frame_start + 1, int(source_action.frame_range[1]))
    scene.frame_start = frame_start
    scene.frame_end = frame_end
    scene.render.fps = 30

    source_armature.animation_data.action = source_action
    if target_armature.animation_data is None:
        target_armature.animation_data_create()

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    target_armature.select_set(True)
    bpy.context.view_layer.objects.active = target_armature
    bpy.ops.object.mode_set(mode="POSE")

    bpy.ops.nla.bake(
        frame_start=frame_start,
        frame_end=frame_end,
        step=1,
        only_selected=False,
        visual_keying=True,
        clear_constraints=True,
        clear_parents=False,
        use_current_action=False,
        bake_types={"POSE"},
    )

    baked_action = target_armature.animation_data.action
    baked_action.name = baked_action_name

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in target_meshes:
        obj.select_set(True)
    target_armature.select_set(True)
    bpy.context.view_layer.objects.active = target_armature

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
