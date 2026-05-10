# Animation Setup

## Animator Controller

Current controller:

`Assets/Animations/PlayerHumanoid.controller`

The live controller is assigned to the full-quality Nightfall visual in `SampleScene`. At the moment, `Idle`, `Walk`, `Run`, the safe Nightfall-native jump, and the vetted Mixamo crouch pass are promoted to the live controller:

- `Idle`: `Nightfall_FullQuality_Idle_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Idle_Baked.fbx`
- `Walk`: `Nightfall_FullQuality_Walk_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Walk_Baked.fbx`
- `Run`: `Nightfall_FullQuality_Run_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_Run_Baked.fbx`
- `Jump Start`: `Nightfall_FullQuality_JumpSafe_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_FullQuality_JumpSafe_Baked.fbx`
- `Crouch Idle`: `Nightfall_Mixamo_CrouchIdle_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked/NightfallVanguard_Mixamo_CrouchIdle_Baked.fbx`
- `Crouch Walk`: `Nightfall_Mixamo_CrouchWalk_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked/NightfallVanguard_Mixamo_CrouchWalk_Baked.fbx`
- `Stand Up`: `Nightfall_Mixamo_StandUp_Baked` from `Assets/Art/Characters/NightfallVanguard/Exports/MixamoBaked/NightfallVanguard_Mixamo_StandUp_Baked.fbx`

Mixamo import rule:

- Download FBX, no skin, 30 FPS.
- Use `Rig > Animation Type: Humanoid`.
- Use `Avatar Definition: Create From This Model` for Mixamo animation-only FBXs because Mixamo uses its own skeleton. Unity retargets that humanoid source avatar onto the Nightfall humanoid avatar at runtime.
- For this project, the Mixamo source files are then baked in Blender onto the Nightfall skeleton with `Tools/Blender/bake_mixamo_to_nightfall.py`, and the live controller uses those baked Nightfall FBXs.
- Keep root motion off for now. The CharacterController still owns real movement, jump height, collision, and landing.

Assign additional clips to the live Player visual only after clips are verified in the sandbox and confirmed to be compatible with the live full-quality rig.

Keep `Apply Root Motion` disabled.

Sandbox controller:

`Assets/Animations/NightfallVanguard/Nightfall_AnimationSandbox.controller`

Use the sandbox controller first when testing unknown clips.

Sandbox scene:

`Assets/Scenes/AnimationSandbox_Nightfall.unity`

The sandbox exists so bad clips cannot corrupt the live `SampleScene` Player setup.

Linked Meshy GLB animation sandbox:

`Assets/Scenes/AnimationSandbox_Nightfall_Linked.unity`

This scene uses cloned clips from the new labeled Meshy GLBs and a sandbox-only armature rename so the generic GLB curves bind to the current full-quality model. See `Docs/NIGHTFALL_ANIMATION_LINKING.md`.

## Parameters

Floats:

- `Speed`
- `MovementX`
- `MovementY`
- `VerticalVelocity`

Bools:

- `IsGrounded`
- `IsSprinting`
- `IsAiming`
- `IsJumping`
- `IsFalling`

Triggers:

- `Jump`
- `Land`
- `PrimaryAttack`
- `AbilityPrimary`
- `AbilitySecondary`
- `Ultimate`

`PlayerWeaponController` triggers `PrimaryAttack` when the prototype rifle fires. This is intentionally trigger-level integration for now; upper-body aim/fire/reload layers should come after the rifle and camera loop feel good in the test gym.

## Placeholder States

The controller contains placeholder states for:

- `Idle`
- `Walk`
- `Run`
- `Sprint`
- `Jump Start`
- `Falling / In Air`
- `Landing`
- `Aim Idle`
- `Aim Walk / Strafe`
- `Attack Placeholder`
- `Ability Placeholder`
- `Locomotion Blend`
- `Aim Strafe Blend`

## Clip Mapping

Drop clips into matching states or blend tree slots in the sandbox first:

- `Idle`: idle clip
- `Walk`: walk forward in-place
- `Run`: run forward in-place
- `Sprint`: sprint forward in-place
- `Jump Start`: jump takeoff
- `Falling / In Air`: falling or airborne loop
- `Landing`: landing
- `Aim Idle`: aim idle
- `Aim Walk / Strafe`: aim forward, back, left, and right strafe clips
- `Attack Placeholder`: first basic attack clip
- `Ability Placeholder`: temporary ability animation

Only copy clips into `PlayerHumanoid.controller` after they pass sandbox testing and are Humanoid-retargeted or baked for the live full-quality rig. A raw GLB `.anim` clip that looks good in the linked sandbox is not automatically safe for the live Player.

The live `PlayerAnimationController` is parameter-driven by default. Leave `driveAnimatorStateMachine` disabled until the Animator transitions or blend trees are intentionally built.

Current live exception: `driveAnimatorStateMachine` is enabled while we promote clips one at a time. `walkClipPromoted` and `runClipPromoted` are true, so normal WASD movement uses `Run`, while Ctrl slow walk and aim movement use `Walk` until their own clips are promoted. Crouch idle/walk/stand use Mixamo clips. Jump uses a short, safe, Nightfall-native clip built from the working run rig. Sprint is still intentionally unpromoted.

Default movement is intentionally run/jog for shooter feel. See `Docs/LOCOMOTION_FEEL_REFERENCE.md`.

For the exact one-clip-at-a-time promotion process and the jump mistakes we hit, see `Docs/ANIMATION_PROMOTION_WORKFLOW.md`.

`Jump_Run_withSkin.glb` is not used raw. The old cropped/procedural jump files and the current Mixamo jump bakes remain as references only. The live player has `jumpClipPromoted` enabled for `Nightfall_FullQuality_JumpSafe_Baked`, while `airClipPromoted` and `landClipPromoted` stay disabled so no questionable fall/land animation can interrupt the controller.

The physical jump arc is intentionally shorter than the early prototype: `jumpHeight` is `0.5625`, gravity is `-22`, and falling gravity uses a `1.5` multiplier in `ThirdPersonMotor` so the visual takeoff does not finish while the controller is still floating. Jump input is ignored while the current jump is locked, so Space spam cannot stack vertical velocity.

## Import Settings

For the main character model:

- Rig > Animation Type: `Humanoid`
- Avatar Definition: `Create From This Model`
- Materials/textures: import with the FBX or assign manually after import.

For separate animation FBXs:

- Rig > Animation Type: `Humanoid`
- Avatar Definition: `Copy From Other Avatar`
- Source Avatar: the character model avatar
- Animation > Loop Time: enabled for idle, walk, run, sprint, aim idle, and strafe loops.
- Animation > Loop Time: usually disabled for jump start, landing, attack, and one-shot abilities.

For raw Meshy GLB animation clips:

- Treat the linked sandbox as preview only.
- Do not assign copied GLB `.anim` clips directly to `PlayerHumanoid.controller`.
- Bake or retarget them to the full-quality rig first, preferably through a Humanoid FBX workflow.

## Testing

In Play mode:

- `Speed` should rise while moving.
- `MovementX` should move negative/positive while strafing in aim mode.
- `MovementY` should move negative/positive while moving back/forward.
- `IsAiming` should be true only while holding right click.
- `IsSprinting` should be true only while holding Shift and moving forward.
- `IsJumping`, `IsFalling`, and `VerticalVelocity` should react during jump/fall.

For the live scene, the Nightfall Animator may remain disabled while the character is only a static visual. That is intentional until idle/walk/run/jump are verified.

## Common Fixes

Facing sideways:

- Rotate the model child under `CharacterVisual`, usually Y = 90 or -90.

Floating:

- Lower the model child local Y. Do not move the `Player` root.

Too small or too large:

- Scale the model child or `CharacterVisual`. Do not scale `Player`.

Animation not playing:

- Confirm the Animator Controller is assigned to `CharacterVisual`.
- Confirm the state has a Motion clip assigned.
- Confirm the FBX rig is Humanoid and avatar is valid.

Moonwalking:

- Use in-place clips.
- Confirm model forward is Unity +Z.
- Confirm the clip is not authored facing sideways.

Aim stuck:

- Confirm right click is configured as hold input.
- Watch `PlayerInputHandler.AimPressed` and Animator `IsAiming`.

Camera following wrong object:

- Camera should follow `Player/CameraTarget`, not the model and not a bone.
