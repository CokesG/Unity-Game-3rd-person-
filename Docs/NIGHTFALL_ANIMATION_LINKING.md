# Nightfall Animation Linking

Last updated: 2026-05-10

## What Was Added

The new Meshy animation GLBs were used as source files and cloned into native Unity `.anim` clips.

Current live promotion:

- `Idle` is now baked through Blender into `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Idle_Baked.fbx`.
- The baked clip is named `Nightfall_FullQuality_Idle_Baked`.
- `Walk` is now baked through Blender into `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Walk_Baked.fbx`.
- The baked clip is named `Nightfall_FullQuality_Walk_Baked`.
- `Assets/Animations/PlayerHumanoid.controller` uses the baked idle clip for `Idle` at speed `0.45` and the baked walk clip for `Walk` at speed `1.0`.
- The live `PlayerAnimationController` has code-driven state switching enabled, but only `walkClipPromoted` is true. Until run/sprint are promoted, all grounded movement uses the walk animation.
- Run, sprint, jump, combat, roll, slide, and ability clips are still sandbox-only until each one is baked or retargeted and reviewed.

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
- Do not rename live skeleton bones to force raw `.anim` binding. That can make the mesh explode.

## Clip Mapping

| Sandbox State | Unity Clip |
| --- | --- |
| Idle | `Nightfall_Idle.anim` |
| Walk | `Nightfall_Walk.anim` |
| Run/Jog | `Nightfall_RunJog.anim` |
| Sprint | `Nightfall_RunJog.anim` |
| Jump Start | `Nightfall_JumpStart.anim` |
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
