# Animation Promotion Workflow

Last updated: 2026-05-12

This project promotes hero animation one clip at a time. The Player root, `CharacterController`, camera, jump physics, collision, and inputs stay code-driven. Animation clips only pose the visual child.

## Current Live Contract

- Normal WASD uses `Run`.
- `Left Alt` or `Right Alt` is slow walk and uses `Walk`.
- Shift requests sprint speed, but sprint still uses the `Run` visual until a sprint clip is promoted.
- Crouch gameplay is enabled.
- Crouch transitions and a stable held crouch pose are promoted:
  - `Stand To Crouch`: `User_Stand_To_Crouch` from `Assets/Animations/NightfallVanguard/UserCrouch/User_StandToCrouch_Crouching.fbx`
  - `Crouch Idle`: held final frame of `User_Stand_To_Crouch`, state speed `0`
  - `Stand Up`: `User_Crouch_To_Stand` from `Assets/Animations/NightfallVanguard/UserCrouch/User_CrouchToStand_Standing.fbx`
- `Crouch Walk` is pulled from live play. `SampleScene` has `crouchWalkClipPromoted` and `forceCrouchWalkWhenMoving` off, so moving while crouched holds the stable crouch pose until the directional set is re-audited.
- While crouched and moving, the live character should hold the reviewed crouch pose until a proper crouch-walk set is authored and passes sandbox review.
- First-pass procedural crouch-walk candidates are generated under `Assets/Animations/NightfallVanguard/UserCrouchWalkProcedural/` from the accepted `User_StandToCrouch_Crouching` final pose.
- Grounded full-quality Nightfall procedural crouch-walk candidates are generated under `Assets/Art/Characters/NightfallVanguard/Exports/ProceduralCrouchWalk/` by `Tools/Blender/create_nightfall_crouch_walk_procedural.py`, but those outputs remain reference/debug assets only.
- The authored crouch-walk source set remains under `Assets/Animations/NightfallVanguard/UserCrouchWalk/` for later audit. Review one direction at a time: Forward, Back, Left, Right.
- Reimport candidate FBXs with `Tools/TPS/Nightfall/Reimport Crouch Walk Candidates` after replacing or regenerating them.
- `PlayerAnimationController` grounds the visual child while grounded by comparing foot/toe bones to the `CharacterController` capsule foot. Renderer bounds are only a fallback. This corrects vertical/root offset from clips without moving the gameplay root.
- Animation Rigging is now part of the project. Use `Tools/TPS/Nightfall/Setup Animation Rigging Helpers` after Package Manager resolves `com.unity.animation.rigging`.
- `PlayerAnimationController.allowCrouchAnimationClips` is on for the currently reviewed crouch set. If a crouch clip regresses, turn off only that individual promoted flag instead of disabling the whole movement system.
- Jump uses `Nightfall_FullQuality_JumpSafe_Baked` for `Jump Start`.
- Falling and landing clips are disabled until clean clips pass review.
- `Animator.applyRootMotion` stays off.
- The Nightfall armature child must stay named `NightfallVanguard_FullQuality_Armature`. If Unity reloads an instance with the legacy child name `Armature`, `PlayerAnimationController` and the sandbox repair tooling rename it before rebinding the Animator.

## Jump Tuning

Current values:

- `jumpHeight`: `0.5625`
- `gravity`: `-22`
- `fallGravityMultiplier`: `1.5`

`jumpHeight` was lowered 25% from `0.75` to reduce the floaty hang time. Tune jump height in both places when needed:

- `Assets/Scripts/Player/ThirdPersonMotor.cs`
- `Assets/Scenes/SampleScene.unity`

Do not fix jump height by enabling root motion or by using an animation that moves the root upward. The controller owns the actual arc; the clip should visually sell takeoff without driving gameplay movement.

## Promotion Flow

1. Preview the source clip in `Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity`.
2. Reject clips that roll, attack, swim, collapse, stretch, float, or put the feet far through the floor.
3. Bake or retarget only the accepted clip to the Nightfall full-quality rig.
4. Promote one state in `Assets/Animations/PlayerHumanoid.controller`.
5. Enable only the matching promotion flag in `PlayerAnimationController`.
6. For crouch, enable `allowCrouchAnimationClips` only for the pieces that survived review. Do not enable crouch-walk just because crouch idle works.
7. Test `SampleScene` immediately before promoting another state.
8. Commit after the state works in play mode.

## Mistakes To Avoid

- Do not promote multiple new clips at once.
- Do not assign raw GLB `.anim` clips directly to the live Player.
- Do not keep bad `Falling / In Air` or `Landing` motions connected while testing jump.
- Do not let Mixamo running-jump clips drive live jump until they are baked, previewed, and proven stable.
- Do not promote Blender-baked Mixamo clips made with raw local `COPY_TRANSFORMS` unless they have passed live rig review. That bake path does not compensate for different rest-pose bone axes. The currently promoted crouch transitions are Unity Humanoid retargeted source clips, not the failed Blender-baked crouch clips.
- Do not rename individual bones to force transform-path binding. The only approved name repair is the armature object name: `Armature` -> `NightfallVanguard_FullQuality_Armature`, because the Nightfall Avatar expects that skeleton path.
- Do not save animation-preview bone poses into `SampleScene` or the sandbox. A Nightfall prefab scene instance should not contain per-bone `m_LocalPosition`, `m_LocalRotation`, or `m_LocalEulerAnglesHint` overrides; those make every animation start from a skewed skeleton.
- Do not use `Run/Jog` naming in code paths; the live run state is `Run`.
- Do not enable root motion while the `CharacterController` owns movement.
- Do not tune animation speed before the physical movement value is correct.
- Do not fix a floating crouch clip by moving the `Player` root. Ground only the visual child or fix the clip's root curves.

## Rig Binding Failure We Hit

The linked sandbox looked like it was ignoring number keys because the Animator state changed, but the visible rig stayed static. Unity reported the Avatar as valid and humanoid, yet `Animator.GetBoneTransform(HumanBodyBones.Hips)` returned `NULL`.

Cause:

- The visible model had a child named `Armature`.
- The Nightfall Avatar skeleton expected `NightfallVanguard_FullQuality_Armature`.
- That path mismatch prevented Unity from resolving humanoid bones on the scene instance.

Fix:

- Rename the direct armature child to `NightfallVanguard_FullQuality_Armature`.
- Keep `Animator.applyRootMotion = false`.
- Call `Animator.Rebind()` after the rename.
- Use `Tools/TPS/Nightfall/Repair Animation Sandbox` if the sandbox starts showing duplicate controls, static poses, or disappearing states again.

Verification:

- `Hips`, `LeftFoot`, `RightFoot`, `LeftHand`, and `RightHand` must resolve through `Animator.GetBoneTransform`.
- Idle, walk, and run should show different foot positions over time.
- The sandbox camera frames the skinned mesh bounds, not the root pivot, so the full body should stay visible.

## Current Quarantine

These clips exist for reference or sandbox review, not live play:

- `Nightfall_Mixamo_JumpFull_Baked`
- `Nightfall_Mixamo_JumpStart_Baked`
- `Nightfall_Mixamo_JumpAir_Baked`
- `Nightfall_Mixamo_JumpLand_Baked`
- `Nightfall_Mixamo_RunningJump_Baked`
- `Nightfall_Mixamo_CrouchWalk_Baked`
- `Nightfall_Mixamo_CrouchIdle_Baked`
- The deprecated procedural air and landing placeholders

The failed live crouch transition test tried to use one source clip three ways:

- `Stand To Crouch`: `Nightfall_Mixamo_StandUp_Baked`, state speed `-1`, started from the end.
- `Crouch Idle`: `Nightfall_Mixamo_StandUp_Baked`, state speed `0`, held at the first crouched frame.
- `Stand Up`: `Nightfall_Mixamo_StandUp_Baked`, state speed `1.15`.

That still deformed the character. The likely root cause was the Blender bake path, not the idea of reversing a stand-up clip. `Tools/Blender/bake_mixamo_to_nightfall.py` copies local Mixamo bone transforms directly onto Nightfall bones, but the two skeletons do not share identical rest-pose axes. The accepted transition clips came from the Unity Humanoid-retargeted Mixamo source path instead.

## Next Recommended Order

1. Build and approve a live aim-strafe directional state or blend tree.
2. Run `Tools/TPS/Nightfall/Setup Animation Rigging Helpers` and tune foot IK weights/offsets.
3. Promote a sprint clip for Shift.
4. Find or author a clean falling loop.
5. Find or author soft and hard landings.
6. Replace hard state switching with broader locomotion Blend Trees after idle, walk, run, sprint, jump, crouch, and aim basics are stable.

## Linked Sandbox Stability Test

Use this before touching `SampleScene`:

1. Open `Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity`.
2. Press Play.
3. Click the on-screen buttons or use the number keys.
4. Confirm `1 Idle`, `2 Walk`, `3 Run/Jog`, and `4 Jump Start` visibly animate.
5. Confirm none of the other buttons make the character disappear.
6. Watch the full body from the side and front. The test passes only if the feet stay believable, the spine does not fold sideways, the character does not float upward, and the hands/arms stay attached cleanly.

This sandbox is not a movement test scene. WASD movement is tested in `SampleScene`; the sandbox only forces one animation state at a time.

If nothing changes when clicking buttons, check the HUD status line. `Missing Animator state` means the sandbox controller does not contain the requested state. `No Animator/controller assigned` means the scene object lost its Animator reference.

The linked sandbox is intentionally conservative right now:

- `1 Idle` uses `Nightfall_FullQuality_Idle_Baked`.
- `2 Walk` uses `Nightfall_FullQuality_Walk_Baked`.
- `3 Run/Jog` and `Sprint` use `Nightfall_FullQuality_Run_Baked`.
- `4 Jump Start` and `Running Jump` use `Nightfall_FullQuality_JumpSafe_Baked`.
- `7 Stand -> Crouch` and `0 Crouch -> Stand` are now live-promoted after sandbox review.
- `8 Crouched Idle` remains sandbox-only. Live crouch idle currently holds the first frame of `Mixamo_Crouched_To_Standing` because the dedicated crouch idle clip leaned the character.
- `9 Crouched Walk` reviews the authored directional set. Review each direction from front and side for foot contact, pelvis height, arm pose, and leg crossing before promoting the full directional set.
- Unknown, attack, and ability preview states fall back to idle/walk until each clip is promoted safely.

Do not promote the remaining crouch states into `SampleScene` just because the buttons move. The sandbox is where we decide whether the clip quality is acceptable; live crouch gameplay should keep the stable held crouch idle even while trialing crouch-walk.
