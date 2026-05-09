# Project Status

Last updated: 2026-05-09

## Current Phase

The project is in Phase 2 stabilization.

Phase 1 movement and camera are working:

- WASD movement through a `CharacterController`.
- Shift sprinting forward at a faster movement speed than normal run.
- Space jump.
- Third-person camera following `CameraTarget`.
- Hold right click to aim.
- C toggles crouch when grounded.
- C while sprinting forward starts a short slide and ends crouched.
- Mouse sensitivity reduced from the first pass.

Phase 2 scaffolding is in place:

- `CharacterVisual` exists under `Player`.
- `GroundCheck` exists under `Player`.
- `Assets/Animations/PlayerHumanoid.controller` exists.
- `PlayerAnimationController` drives Animator parameters from input and motor state.
- Root motion is intentionally disabled for now.
- A clean model-only Nightfall Vanguard visual is active under `Player/CharacterVisual`.
- The old Meshy animated prototype remains disabled in the scene for reference.
- `Docs/WORLD_CLASS_TPS_AGENT_BRIEF.md` now captures the researched Fortnite-style movement/gunplay target and the missing documentation agents need before pushing toward polished combat.
- `Assets/Scenes/AnimationSandbox_Nightfall.unity` now exists for one-clip-at-a-time animation testing away from the live Player.
- `Assets/Animations/NightfallVanguard/Nightfall_AnimationSandbox.controller` now exists with safe placeholder states and parameters for clip testing.
- Movement feel V1 is implemented in `ThirdPersonMotor`.
- Camera/aim V1 is implemented in `ThirdPersonCameraController`.
- Prototype rifle, target dummy damage, reticle HUD, runtime bootstrap, and test gym builder scripts are in place.
- Gun tuning debug V1 is implemented behind `F1` in Play mode. It shows reload countdown, reload progress, ammo, shot counts, registered target hits, world hits, misses, critical hits, blocked shots, accuracy, DPS, RPM, spread, TTK estimates, and per-target damage stacks.

The current visual is:

```text
Player
- CharacterVisual
  - NightfallVanguard_Prototype       disabled, contains bad/mixed Meshy clips
  - NightfallVanguard_FullQuality     active wrapper, full-quality mesh, no imported animations
    - NightfallVanguard_FullQuality_Rig
```

The rig child's Animator is assigned but disabled until verified idle/walk/run/jump clips are added. This prevents Unity's Humanoid Animator evaluation from pulling the model below the capsule when no valid clip is present.

## Important Architecture Decision

The `Player` root is the gameplay object.

The humanoid model is only a visual child. It should not drive movement, collision, or camera placement.

```text
Player
- CharacterController
- GroundCheck
- CameraTarget
- CharacterVisual
  - Animator
  - rigged humanoid model
- PlayerInputHandler
- ThirdPersonMotor
- PlayerAnimationController
- PlayerCombatHooks
```

Do not move the `CharacterController` to the mesh.
Do not parent the camera to animated bones.
Do not enable root motion unless the movement system is explicitly redesigned to handle it.

## Current Scripts

- `PlayerInputHandler.cs`: reads movement, look, jump, sprint, and aim input.
- `ThirdPersonMotor.cs`: owns controller-driven movement, rotation, gravity, jump, and exposed movement state.
- `ThirdPersonCameraController.cs`: follows `CameraTarget`, handles aim shoulder view, collision, pitch/yaw, and reduced sensitivity.
- `PlayerAnimationController.cs`: sends movement and combat parameters to the Animator.
- `PlayerCombatHooks.cs`: placeholder combat input hooks that fire Animator triggers.
- `PlayerWeaponController.cs`: prototype rifle, ammo, reload, spread, recoil, hitscan, and muzzle obstruction.
- `WeaponDefinition.cs`: data asset shape for weapon tuning.
- `TargetDummy.cs`: damage target with current-life/session damage, registered hit, critical hit, defeat, and last-hit debug accounting.
- `TPSReticleHUD.cs`: reticle, hitmarker, ammo/state readout, blocked muzzle feedback, and `F1` gun tuning overlay.
- `TpsRuntimeBootstrap.cs`: adds prototype rifle/HUD/dummies at runtime if a scene has none.
- `TpsTestGymBuilder.cs`: Unity editor menu for creating a graybox movement/gunplay test gym.

## Play Mode Debug Controls

- `F1`: toggle gun tuning debug overlay.
- Left mouse: fire prototype rifle.
- Right mouse: hold aim.
- `R`: reload fallback.
- `V` or middle mouse: shoulder swap fallback.
- C / left Ctrl: crouch or slide depending on movement state.

`PlayerAnimationController` now defaults to parameter-driving only. It does not force manual Animator state crossfades unless `driveAnimatorStateMachine` is explicitly enabled later after the controller has verified states and transitions.

## Next Milestone

Use `Docs/HERO_CHARACTER_PIPELINE.md` and add animation clips back one by one in `AnimationSandbox_Nightfall`, then verify:

- Character model faces Unity +Z forward.
- Feet touch the ground.
- Model scale matches the capsule.
- WASD/sprint/jump/aim still feel good.
- Animator parameters update in Play mode.
- No sideways movement, moonwalking, or aim-state sticking.

Combat should come after the character and locomotion visuals are stable.

Do not bulk-assign the Meshy merged animation file again. The first live animation pass should enable the Animator only after adding one known-good idle clip, then one known-good walk, then one known-good run/jog, then jump.

Before a polished combat pass, agents should use `Docs/WORLD_CLASS_TPS_AGENT_BRIEF.md` to define movement feel, camera/aim behavior, weapon data, reticle/shot-origin rules, hit feedback, and manual acceptance tests.

The immediate next validation step is to open Unity, use `Tools > TPS > Create Test Gym Scene`, then run `Docs/PLAYTEST_CHECKLIST.md`.
