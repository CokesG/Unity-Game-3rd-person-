# Nightfall Vanguard Asset Intake

Last updated: 2026-05-09

## Source Files

Imported Meshy files:

- `Assets/Art/Characters/NightfallVanguard/Source/Meshy_AI_Nightfall_Vanguard_biped_Character_output.glb`
- `Assets/Art/Characters/NightfallVanguard/Source/Meshy_AI_Nightfall_Vanguard_biped_Meshy_AI_Meshy_Merged_Animations.glb`

Generated prototype Unity FBX:

- `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_Prototype_Animated.fbx`
- `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_Prototype_Optimized50k.fbx`
- `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx`

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

- The active scene uses `NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx`.
- The old animated prototype remains under `CharacterVisual`, but is disabled.
- The clean FBX imports as Humanoid with `importAnimation = false`.
- The clean visual is wrapped under `NightfallVanguard_FullQuality`.
- The rig child has an Animator assigned to `Assets/Animations/PlayerHumanoid.controller`, but the Animator component is disabled until a verified idle clip is added.
- `Animator.applyRootMotion` remains disabled.
- The live Animator remains disabled until clips are verified in the sandbox.

Animation sandbox:

- `Assets/Scenes/AnimationSandbox_Nightfall.unity`
- `Assets/Animations/NightfallVanguard/Nightfall_AnimationSandbox.controller`
- The sandbox controller has placeholder states for idle, walk, run/jog, sprint, jump, fall, land, aim, crouch, slide, attack, and ability testing.
- Use the sandbox before touching `Assets/Animations/PlayerHumanoid.controller`.

Why: the animated Meshy FBX contains many action clips in one file, including charge, roll, bow, slide, attack, swim, and locomotion takes. Some clips are mislabeled or unsuitable for direct locomotion, so the scene now uses a clean model-only character while we source or build reliable locomotion clips one at a time.

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

This optimized mesh is no longer the active visual. It caused visible holes/artifacting in game view. The active clean visual uses the full-quality mesh for now, even though it is heavier. Production optimization should use clean retopology instead of blunt decimation.

## Animator Mapping

`Assets/Animations/PlayerHumanoid.controller` is intentionally clean right now.

- Idle, Walk, Sprint, and Jump Start states exist.
- No Meshy motion clips are assigned to these states.
- The controller receives parameters from `PlayerAnimationController`.
- This preserves the working Phase 1 movement while we validate good clips one at a time.

Do not bulk-map the Meshy merged animation file back into the controller. Add only verified, in-place humanoid clips.

`PlayerAnimationController` now sends Animator parameters defensively and defaults to not forcing state crossfades. This keeps unverified Animator state layouts from hijacking movement.

Rejected for automatic locomotion:

- `Walking`: direct clip testing produced a forward roll/curl pose instead of clean walking.
- `Running`: direct clip testing produced a crouched/airborne-looking pose instead of clean running.
- `Female_Head_Down_Charge`: should be a future ability/movement action only, not default run.
- `Roll_Dodge`: should be a future dodge/slide action only, not mapped to WASD.

## Verdict

This asset is usable for:

- Testing scale.
- Testing the Unity Humanoid import path.
- Testing the `CharacterVisual` hierarchy.
- Testing basic animation parameter flow.
- Testing controller-driven movement with a clean rigged visual.
- Prototyping movement/aim/combat placeholders.

This asset is not final hero quality because:

- The original is heavy at roughly 197k triangles / 272k imported vertices.
- The optimized prototype is better but still decimated, not cleanly retopologized.
- It is all-triangle topology.
- It has no finger bones.
- The hands cannot support polished weapon grips, casting, item handling, or close-up hero animation yet.
- Texture/material import appears incomplete or not conventional from Blender inspection.
- The bundled locomotion clips should not be trusted until each one is inspected and tested in isolation.

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
