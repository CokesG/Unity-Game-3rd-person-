# Nightfall Animation Linking

Last updated: 2026-05-09

## What Was Added

The new Meshy animation GLBs were used as source files and cloned into native Unity `.anim` clips.

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

Do not assign these clips directly to the live Player until they are reviewed in the sandbox.

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

