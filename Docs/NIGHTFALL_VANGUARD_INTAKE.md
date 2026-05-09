# Nightfall Vanguard Asset Intake

Last updated: 2026-05-09

## Source Files

Imported Meshy files:

- `Assets/Art/Characters/NightfallVanguard/Source/Meshy_AI_Nightfall_Vanguard_biped_Character_output.glb`
- `Assets/Art/Characters/NightfallVanguard/Source/Meshy_AI_Nightfall_Vanguard_biped_Meshy_AI_Meshy_Merged_Animations.glb`

Generated prototype Unity FBX:

- `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_Prototype_Animated.fbx`
- `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_Prototype_Optimized50k.fbx`

## Blender Inspection

Character GLB:

- Meshes: 2
- Main mesh triangles: about 197k
- Topology: triangles only, no quads
- Armature: 24 bones
- Finger bones: none
- Hand bones: `LeftHand`, `RightHand`
- Animation actions: 1 tiny base clip
- Embedded texture images detected by Blender: none

Merged animations GLB:

- Meshes: 3 before cleanup
- Main mesh triangles: about 197k
- Topology: triangles only, no quads
- Armature: 24 bones
- Finger bones: none
- Hand bones: `LeftHand`, `RightHand`
- Animation actions: 21 in Blender
- Unity imported clips: 40

Small `Icosphere` marker meshes were removed during FBX export.

## Unity Import Status

`NightfallVanguard_Prototype_Animated.fbx` was imported as:

- Rig: Humanoid
- Avatar setup: Create From This Model
- Avatar valid: true
- Avatar humanoid: true
- Mesh vertices in Unity: 272,783
- Imported animation clips: 40

The prototype was placed under:

```text
Player
- CharacterVisual
  - NightfallVanguard_Prototype
    - NightfallVanguard_Armature
    - NightfallVanguard_Mesh
```

The `Animator` now lives on `NightfallVanguard_Prototype`, not on the empty `CharacterVisual` object.

`Animator.applyRootMotion` remains disabled.

Current scene choice:

- The active scene uses the original prototype FBX because the decimated 50k version damaged the visual quality too much.
- The 50k optimized FBX remains in the project as an experiment/reference only.
- Future optimization should use real retopology, not blunt decimation.

## Optimization Pass

The first FBX import was too heavy:

- Original Blender mesh: about 197k triangles.
- Original Unity skinned mesh: 272,783 vertices.

A decimated prototype export was created in Blender as an experiment:

- `NightfallVanguard_Prototype_Optimized50k.fbx`
- Blender mesh target: about 50k triangles.
- Unity imported skinned mesh: 98,731 vertices.
- Unity Humanoid avatar: valid.
- Imported animation clips: 40.
- Skinned mesh quality in scene: `Bone2`.
- `updateWhenOffscreen`: false.

This is not the active visual because quality degraded too much. It is a useful measurement/reference, but production optimization should use clean retopology instead of decimation.

## Animator Mapping

Mapped into `Assets/Animations/PlayerHumanoid.controller`:

- Idle: `Idle_10`
- Walk: `Walking`
- Run/Jog: `Running`
- Sprint: `Running`
- Jump Start: `Jump_Run`
- Falling / In Air: `Jump_Run` placeholder
- Landing: `Jump_Run` placeholder
- Aim Idle: `Idle_10`
- Aim Walk / Strafe: `Walk_Forward_with_Bow_Aimed` plus available turning/back-run placeholders
- Attack Placeholder: `Thrust_Slash` or `Charged_Slash`
- Ability Placeholder: `Spartan_Kick` or `Roll_Dodge`

This mapping is for prototype validation, not final animation tuning.

## Verdict

This asset is usable for:

- Testing scale.
- Testing the Unity Humanoid import path.
- Testing the `CharacterVisual` hierarchy.
- Testing basic animation parameter flow.
- Prototyping movement/aim/combat placeholders.

This asset is not final hero quality because:

- The original is heavy at roughly 197k triangles / 272k imported vertices.
- The optimized prototype is better but still decimated, not cleanly retopologized.
- It is all-triangle topology.
- It has no finger bones.
- The hands cannot support polished weapon grips, casting, item handling, or close-up hero animation yet.
- Texture/material import appears incomplete or not conventional from Blender inspection.

## Recommended Next Step

Use this asset as a temporary prototype only.

For a production character, create or clean a version with:

- 20k-60k triangle final Unity target.
- Quad-based retopo source before Unity export.
- Clear hand geometry.
- Finger bones if combat/weapon/casting animation matters.
- In-place locomotion clips.
- PBR textures assigned in a conventional material workflow.

If the body/costume is liked, keep the visual design and replace or rebuild:

- Hands.
- Rig.
- Retopologized game mesh.
- Texture baking pipeline.
