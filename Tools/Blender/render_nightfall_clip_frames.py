import os
import sys

import bpy


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def import_fbx(path):
    bpy.ops.import_scene.fbx(filepath=path)


def frame_values(start, end, count):
    if count <= 1:
        return [start]

    return [round(start + (end - start) * i / (count - 1)) for i in range(count)]


def setup_scene(output_dir):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512

    camera_data = bpy.data.cameras.new("ContactSheetCamera")
    camera = bpy.data.objects.new("ContactSheetCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = (0.0, -4.2, 1.3)
    camera.rotation_euler = (1.35, 0.0, 0.0)
    camera_data.lens = 45
    scene.camera = camera

    light_data = bpy.data.lights.new("KeyLight", "AREA")
    light = bpy.data.objects.new("KeyLight", light_data)
    bpy.context.collection.objects.link(light)
    light.location = (0.0, -3.0, 4.5)
    light_data.energy = 450
    light_data.size = 4.0

    os.makedirs(output_dir, exist_ok=True)


def set_origin_and_material():
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            material = bpy.data.materials.new(obj.name + "_PreviewMaterial")
            material.diffuse_color = (0.72, 0.72, 0.68, 1.0)
            obj.data.materials.clear()
            obj.data.materials.append(material)


def render_clip(path, output_dir, label):
    clear_scene()
    import_fbx(path)
    setup_scene(output_dir)
    set_origin_and_material()

    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if not armatures:
        raise RuntimeError(f"No armature found in {path}")

    armature = armatures[0]
    action = armature.animation_data.action if armature.animation_data else None
    if action is None:
        raise RuntimeError(f"No action found in {path}")

    start = int(action.frame_range[0])
    end = int(action.frame_range[1])
    scene = bpy.context.scene

    for index, frame in enumerate(frame_values(start, end, 4), start=1):
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(output_dir, f"{label}_{index:02d}_frame_{frame:03d}.png")
        bpy.ops.render.render(write_still=True)


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b --python render_nightfall_clip_frames.py -- output_dir label=file.fbx [...]")

    args = sys.argv[sys.argv.index("--") + 1 :]
    output_dir = args[0]

    for spec in args[1:]:
        label, path = spec.split("=", 1)
        render_clip(path, output_dir, label)


if __name__ == "__main__":
    main()
