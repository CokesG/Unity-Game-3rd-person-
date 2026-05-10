# Animation Promotion Workflow

Last updated: 2026-05-10

This project promotes hero animation one clip at a time. The Player root, `CharacterController`, camera, jump physics, collision, and inputs stay code-driven. Animation clips only pose the visual child.

## Current Live Contract

- Normal WASD uses `Run`.
- `Left Ctrl` or `Right Ctrl` is slow walk and uses `Walk`.
- Shift requests sprint speed, but sprint still uses the `Run` visual until a sprint clip is promoted.
- Crouch uses the vetted Mixamo crouch clips.
- Jump uses `Nightfall_FullQuality_JumpSafe_Baked` for `Jump Start`.
- Falling and landing clips are disabled until clean clips pass review.
- `Animator.applyRootMotion` stays off.

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
6. Test `SampleScene` immediately before promoting another state.
7. Commit after the state works in play mode.

## Mistakes To Avoid

- Do not promote multiple new clips at once.
- Do not assign raw GLB `.anim` clips directly to the live Player.
- Do not keep bad `Falling / In Air` or `Landing` motions connected while testing jump.
- Do not let Mixamo running-jump clips drive live jump until they are baked, previewed, and proven stable.
- Do not rename live skeletons to force transform-path binding.
- Do not use `Run/Jog` naming in code paths; the live run state is `Run`.
- Do not enable root motion while the `CharacterController` owns movement.
- Do not tune animation speed before the physical movement value is correct.

## Current Quarantine

These clips exist for reference or sandbox review, not live play:

- `Nightfall_Mixamo_JumpFull_Baked`
- `Nightfall_Mixamo_JumpStart_Baked`
- `Nightfall_Mixamo_JumpAir_Baked`
- `Nightfall_Mixamo_JumpLand_Baked`
- `Nightfall_Mixamo_RunningJump_Baked`
- The deprecated procedural air and landing placeholders

## Next Recommended Order

1. Verify the safe jump with `jumpHeight` `0.5625`.
2. Promote a sprint clip for Shift.
3. Find or author a clean falling loop.
4. Find or author soft and hard landings.
5. Replace hard state switching with a locomotion Blend Tree after idle, walk, run, sprint, jump, crouch, and aim basics are stable.
