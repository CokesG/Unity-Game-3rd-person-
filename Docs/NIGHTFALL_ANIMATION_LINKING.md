# Nightfall Animation Linking

Last updated: 2026-05-10

## What Was Added

The new Meshy animation GLBs were used as source files and cloned into native Unity `.anim` clips.

Current live promotion:

- `Idle` is now baked through Blender into `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Idle_Baked.fbx`.
- The baked clip is named `Nightfall_FullQuality_Idle_Baked`.
- `Walk` is now baked through Blender into `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Walk_Baked.fbx`.
- The baked clip is named `Nightfall_FullQuality_Walk_Baked`.
- `Run` is now baked through Blender into `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Run_Baked.fbx`.
- The baked clip is named `Nightfall_FullQuality_Run_Baked`.
- `Jump Start` is now a safe Nightfall-native clip created from the working run rig:
  - `NightfallVanguard_FullQuality_JumpSafe_Baked.fbx`
  - The baked clip is named `Nightfall_FullQuality_JumpSafe_Baked`.
- A basic jump set was created in Blender as a temporary placeholder and is now deprecated for live use:
  - `NightfallVanguard_FullQuality_JumpRun_Cropped_Baked.fbx`, cropped from `Jump_Run_withSkin.glb` frames `7-17` so the obvious run-up is removed.
  - `NightfallVanguard_FullQuality_AirLoop_Procedural.fbx`
  - `NightfallVanguard_FullQuality_Land_Procedural.fbx`
- The live crouch pass now uses Mixamo FBX animation-only clips from `Assets/Animations/NightfallVanguard/Mixamo`.
- Mixamo jump clips are currently quarantined:
  - `Mixamo_Jumping.fbx` was baked into `Nightfall_Mixamo_JumpFull_Baked`, but the preview leans too aggressively and causes a ragdoll-like visual in live play.
  - `Nightfall_Mixamo_JumpStart_Baked`, `Nightfall_Mixamo_JumpAir_Baked`, and `Nightfall_Mixamo_JumpLand_Baked` exist as sandbox/reference clips, but they are not active in `SampleScene`.
  - `Mixamo_CrouchingIdle.fbx` is baked into `Nightfall_Mixamo_CrouchIdle_Baked`.
  - `Mixamo_CrouchWalking.fbx` is baked into `Nightfall_Mixamo_CrouchWalk_Baked`.
  - `Mixamo_CrouchedToStanding.fbx` is baked into `Nightfall_Mixamo_StandUp_Baked`.
- `Assets/Animations/PlayerHumanoid.controller` uses the baked idle clip for `Idle` at speed `0.45`, the baked walk clip for `Walk` at speed `1.0`, the baked run clip for `Run` at speed `1.0`, the safe Nightfall-native jump clip for `Jump Start`, and the Mixamo clips for crouch.
- The live `PlayerAnimationController` has code-driven state switching enabled with `walkClipPromoted` and `runClipPromoted` true. Normal WASD movement uses `Run`; Ctrl slow walk, aim movement, and crouch movement use `Walk` until their own clips are promoted.
- The locomotion feel is intentionally shooter-style default run/jog instead of keyboard walk-to-run gating. See `Docs/LOCOMOTION_FEEL_REFERENCE.md`.
- The raw `Jump_Run_withSkin.glb` remains a moving-jump reference because its preview includes running before the jump. It is not used by the live player.
- Sprint, combat, roll, slide, and ability clips are still sandbox-only until each one is baked or retargeted and reviewed.

Source folder:

```text
Assets/Animations/NightfallVanguard/Meshy_AI_Nightfall_Vanguard_biped
```

Generated Unity clips:

```text
Assets/Animations/NightfallVanguard/GLBClips
```

Linked sandbox controller:

```text
Assets/Animations/NightfallVanguard/Nightfall_GLB_Linked.controller
```

Linked sandbox scene:

```text
Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity
```

Sandbox hotkey driver:

```text
Assets/Scripts/Animation/NightfallAnimationSandboxDriver.cs
```

## GLB Or FBX?

For this pass, GLB is acceptable as a source format because Unity imports these files with `AnimationClip` subassets.

For the final production character pipeline, FBX is still safer because Unity's Humanoid rig import, avatar copying, and retargeting workflow are more predictable with FBX.

Current finding:

- The GLB clips are generic transform clips, not Unity Humanoid clips.
- Their binding paths start with `Armature/Hips/...`.
- The current full-quality FBX rig used in the live Player has a different armature object name.
- The linked sandbox scene uses the existing full-quality FBX model but renames the sandbox instance armature to `Armature`, so the cloned GLB clips can bind correctly.
- The raw GLB `.anim` clips must not be assigned directly to the live `PlayerHumanoid.controller`. They can deform or stretch the full-quality mesh because they animate transform paths from the source GLB skeleton instead of using Unity Humanoid Avatar retargeting.

Do not assign these clips directly to the live Player until they are reviewed in the sandbox.

## Unity Retargeting Rule

Unity's Humanoid retargeting workflow depends on valid Humanoid Avatars, not matching raw transform path names. The source animation and destination character need properly configured Avatars so Unity can map the source humanoid pose onto the target humanoid skeleton.

Official Unity references:

- Humanoid retargeting requires humanoid models with configured Avatars: https://docs.unity.cn/6000.1/Documentation/Manual/Retargeting.html
- Separate animation files should use `Rig > Animation Type: Humanoid`, and can use `Avatar Definition: Copy From Other Avatar` when they share the same skeleton as the model: https://docs.unity.cn/2022.1/Documentation/Manual/ConfiguringtheAvatar.html
- Unity's Avatar system is what maps humanoid body parts so animation can move between characters: https://docs.unity.cn/Manual/AvatarCreationandSetup.html

Practical rule for this project:

- Sandbox GLB clips are preview-only until baked/retargeted.
- The live Player should use Humanoid FBX clips or baked clips exported specifically for `NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx`.
- Mixamo FBX animation-only clips should be imported as Humanoid with `Avatar Definition: Create From This Model`. Do not copy the Nightfall avatar onto Mixamo files because the skeletons are different.
- The live promoted clips are the Blender-baked Nightfall FBXs under `Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked`. Those use the Nightfall skeleton directly and can copy the Nightfall avatar.
- Do not rename live skeleton bones to force raw `.anim` binding. That can make the mesh explode.

## Clip Mapping

| Sandbox State | Unity Clip |
| --- | --- |
| Idle | `Nightfall_Idle.anim` |
| Walk | `Nightfall_Walk.anim` |
| Run | `Nightfall_RunJog.anim` |
| Sprint | `Nightfall_RunJog.anim` |
| Jump Start | `Nightfall_FullQuality_JumpSafe_Baked` |
| Falling / In Air | inactive/reference: `Nightfall_Mixamo_JumpAir_Baked` |
| Landing | inactive/reference: `Nightfall_Mixamo_JumpLand_Baked` |
| Crouch Idle | `Nightfall_Mixamo_CrouchIdle_Baked` |
| Crouch Walk | `Nightfall_Mixamo_CrouchWalk_Baked` |
| Stand Up | `Nightfall_Mixamo_StandUp_Baked` |
| Running Jump Preview | `Nightfall_Mixamo_RunningJump_Baked` |
| Aim Walk / Strafe | `Nightfall_AimWalkStrafe.anim` |
| Slide | `Nightfall_Slide.anim` |
| Attack Placeholder | `Nightfall_Attack_ArcheryShot.anim` |
| Ability Placeholder | `Nightfall_Ability_Charge.anim` |
| Roll Dodge | `Nightfall_RollDodge.anim` |
| Charged Slash | `Nightfall_ChargedSlash.anim` |
| Thrust Slash | `Nightfall_ThrustSlash.anim` |
| Spartan Kick | `Nightfall_SpartanKick.anim` |
| Back Left Run | `Nightfall_BackLeftRun.anim` |
| Back Right Run | `Nightfall_BackRightRun.anim` |
| Walk Turn Left | `Nightfall_WalkTurnLeft.anim` |
| Walk Turn Right | `Nightfall_WalkTurnRight.anim` |
| Crawl Backward | `Nightfall_CrawlBackward.anim` |
| Swim Forward | `Nightfall_SwimForward.anim` |
| Swim Idle | `Nightfall_SwimIdle.anim` |

## How To Test

Open:

```text
Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity
```

Press Play.

Hotkeys:

```text
1 Idle
2 Walk
3 Run
4 Sprint
5 Jump
6 Aim Walk / Strafe
7 Slide
8 Attack Placeholder
9 Ability Placeholder
0 Roll Dodge
```

Review one clip at a time.

Accept a clip only if:

- Feet stay close to the ground when expected.
- The character faces forward.
- The character does not roll, curl, swim, or attack during locomotion.
- The clip does not drag the model below the floor.
- The pose does not destroy shoulders, hips, knees, wrists, or hands.

Reject or quarantine a clip if:

- Walk becomes a roll.
- Run becomes crouched or airborne.
- The root drifts wildly.
- The pose is sideways.
- The clip is clearly an ability/combat move, not locomotion.

## Promotion Path

After a clip passes sandbox review:

1. Promote only that clip to `Assets/Animations/PlayerHumanoid.controller`.
2. Keep `Animator.applyRootMotion = false`.
3. Keep movement on the `CharacterController`.
4. Enable live animation only after idle, walk, run, and jump pass.
5. Test `SampleScene` movement after each promoted clip.

If the live full-quality rig cannot play these clips cleanly, use Blender to normalize the rig and animation paths or export FBX animation clips against a matching skeleton.

## Deprecated Procedural Jump Clips

Generated by:

```text
Tools/Blender/create_nightfall_basic_jump.py
Tools/Blender/bake_nightfall_clip_range.py
```

The old basic jump placeholder uses:

- Cropped `Jump Start`: non-looping, sourced from `Jump_Run_withSkin.glb` frames `7-17`.
- `Falling / In Air`: looping procedural placeholder.
- `Landing`: non-looping procedural placeholder.

The physical jump still comes from `ThirdPersonMotor`; these clips only pose the visual character. Keep `Animator.applyRootMotion = false`. In the live player, `jumpClipPromoted`, `airClipPromoted`, and `landClipPromoted` are disabled so questionable jump clips cannot cause ragdoll-like posing, stuck landing, or overlapping jump states.

The current live jump height is `0.5625` in `ThirdPersonMotor` and `SampleScene`, down 25% from the earlier `0.75` prototype height. See `Docs/ANIMATION_PROMOTION_WORKFLOW.md` before changing jump clips or jump tuning again.

## Mixamo Download Targets

Current Mixamo files are valid for this project because they are FBX, no skin, 30 FPS, animation-only files.

Good next Mixamo targets:

- `Falling Idle` or a clean loopable fall pose, so the airborne state does not depend on a sliced jump clip forever.
- `Hard Landing` and `Soft Landing`, so short hops and long falls can have different land reactions.
- `Sprint Forward` or `Fast Run`, in-place, no skin, for Shift sprint.
- `Strafe Left`, `Strafe Right`, `Walk Backward`, and aim/rifle locomotion clips for right-click aim mode.
- `Running Slide` for the slide mechanic.
- `Dodge Roll` only after the basic locomotion loop is stable.
