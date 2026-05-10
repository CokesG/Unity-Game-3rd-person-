# Hero Character Pipeline

Last updated: 2026-05-09

## Current Rule

The live `SampleScene` is the playable controller scene. Do not use it as the first place to test unknown animation clips.

Use this scene for animation testing:

```text
Assets/Scenes/AnimationSandbox_Nightfall.unity
```

Use this scene for the linked Meshy GLB clip review pass:

```text
Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity
```

Use this Animator Controller for sandbox clip testing:

```text
Assets/Animations/NightfallVanguard/Nightfall_AnimationSandbox.controller
```

The live Player remains controller-driven:

```text
Player
- CharacterController
- GroundCheck
- CameraTarget
- CharacterVisual
  - NightfallVanguard_FullQuality
    - NightfallVanguard_FullQuality_Rig
- PlayerInputHandler
- ThirdPersonMotor
- PlayerAnimationController
- PlayerCombatHooks
```

The humanoid visual is not allowed to drive movement, collision, camera, or root motion.

## Nightfall Vanguard Status

The current Nightfall model is useful as a prototype visual, but not a final hero rig.

Known limitations:

- About 197k source triangles, which is heavy for the main playable character.
- All-triangle topology from the AI model output.
- 24-bone humanoid rig.
- No finger bones.
- Hands are not weapon-grip quality yet.
- Meshy merged animation clips include mislabeled or unsuitable movement actions.

Keep using the model-only FBX for live gameplay until the rig and animation clips are verified:

```text
Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx
```

Do not bulk-wire clips from:

```text
Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_Prototype_Animated.fbx
Assets/Art/Characters/NightfallVanguard/Source/Meshy_AI_Nightfall_Vanguard_biped_Meshy_AI_Meshy_Merged_Animations.glb
```

Those files are reference-only until each action is inspected in Blender and tested in the sandbox.

The labeled Meshy GLB animation exports have now been cloned into native Unity `.anim` clips under:

```text
Assets/Animations/NightfallVanguard/GLBClips
```

They are wired only into the linked sandbox controller:

```text
Assets/Animations/NightfallVanguard/Nightfall_GLB_Linked.controller
```

Do not promote them to the live Player until they pass visual review in `AnimationSandbox_Nightfall_Linked`.

## Production Hero Blueprint

For a real playable hero, the target is:

- Stylized-realistic clothed or armored humanoid.
- A-pose or T-pose neutral stance.
- Unity +Z forward after export.
- Feet on ground with origin at or near the floor.
- 20k-60k triangles for the main in-game character.
- 2k PBR textures as the default target.
- Quad-based retopo source before final Unity export.
- Unity Humanoid-compatible skeleton.
- Finger bones for weapon grip, casting, interact poses, and close camera work.
- Clean weights at shoulders, elbows, wrists, hips, knees, ankles, neck, and fingers.
- No weapon baked into the hands.
- Clothes and gear included for now unless we intentionally build a modular gear system.

## Rigging Standard

Minimum skeleton:

- Root or armature object.
- Hips.
- Spine/chest.
- Neck/head.
- Upper/lower arms.
- Hands.
- Upper/lower legs.
- Feet/toes.

Preferred additions:

- Finger bones.
- Weapon socket or hand socket empties.
- Optional aim or twist helper bones only if they export cleanly and do not break Unity Humanoid.

Current controller policy:

- Root motion off.
- Movement remains on `CharacterController`.
- Animation visually follows motor state.
- Animation can add pose, timing, hit windows, and IK later.

## Clip Promotion Ladder

Every clip must move through this order:

1. Inspect in Blender or Unity preview.
2. Confirm it is in-place unless intentionally designed as a root-motion action.
3. Confirm model faces Unity +Z forward.
4. Import the clip as Humanoid.
5. Use `Copy From Other Avatar` against the Nightfall model avatar.
6. Drop it into `Nightfall_AnimationSandbox.controller`.
7. Open `AnimationSandbox_Nightfall.unity` and test only that clip.
8. Confirm feet, hips, hands, and facing direction are sane.
9. Only then copy the clip into `PlayerHumanoid.controller`.
10. Enable the live Player Animator only after idle, walk, run, and jump are each verified.

Promotion stops if the clip:

- Rolls, charges, swims, attacks, or dodges when it is supposed to walk/run.
- Slides the character across the ground unexpectedly.
- Pulls the character below the floor.
- Faces sideways.
- Causes moonwalking.
- Breaks the shoulders, wrists, hips, knees, or hands.

## Sandbox Test Order

Test in this order:

1. Idle.
2. Walk forward.
3. Run forward.
4. Sprint forward.
5. Jump Start.
6. Falling / In Air.
7. Landing.
8. Aim Idle.
9. Aim Walk / Strafe.
10. Crouch Idle.
11. Crouch Walk.
12. Slide.
13. Attack Placeholder.
14. Ability Placeholder.

Do not test combat, dodge, charge, or roll before idle/walk/run/jump are stable.

## Blender Work Order

Use Blender for the real hero cleanup:

1. Import the preferred model source.
2. Apply scale and transforms.
3. Fix forward direction to Unity +Z.
4. Repair hands and visible finger shapes.
5. Retopo to the 20k-60k triangle target or create a clean source that can be decimated safely.
6. Rebuild or improve the armature with finger bones.
7. Weight paint deformation zones.
8. Create or import clean in-place locomotion clips.
9. Export a model-only FBX.
10. Export separate animation FBXs.

Do not rely on the Meshy auto-rig as final if hands, fingers, weapon grip, or close-up animation matter.

## Unity Promotion Checklist

Before enabling animation in `SampleScene`:

- `Player` root still has the `CharacterController`.
- Camera still follows `Player/CameraTarget`.
- `Animator.applyRootMotion` is false.
- Live Animator is assigned to `PlayerHumanoid.controller`.
- `PlayerHumanoid.controller` has only verified clips.
- `PlayerAnimationController.driveAnimatorStateMachine` remains false unless the Animator transitions are explicitly built.
- Idle clip keeps feet on ground.
- Walk/run clips are in-place.
- Sprint only plays while sprinting forward.
- Jump cannot be spammed upward.
- Hold right click sets `IsAiming`.
- Release right click clears `IsAiming`.
- C toggles crouch/stand.
- Sprint plus C starts slide and ends crouched.
