# Project Status

Last updated: 2026-05-09

## Current Phase

The project is between Phase 1 and Phase 2.

Phase 1 movement and camera are working:

- WASD movement through a `CharacterController`.
- Shift sprinting forward.
- Space jump.
- Third-person camera following `CameraTarget`.
- Hold right click to aim.
- Mouse sensitivity reduced from the first pass.

Phase 2 scaffolding is in place:

- `CharacterVisual` exists under `Player`.
- `GroundCheck` exists under `Player`.
- `Assets/Animations/PlayerHumanoid.controller` exists.
- `PlayerAnimationController` drives Animator parameters from input and motor state.
- Root motion is intentionally disabled for now.

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

## Next Milestone

Add a real humanoid character model and animation clips, then verify:

- Character model faces Unity +Z forward.
- Feet touch the ground.
- Model scale matches the capsule.
- WASD/sprint/jump/aim still feel good.
- Animator parameters update in Play mode.
- No sideways movement, moonwalking, or aim-state sticking.

Combat should come after the character and locomotion visuals are stable.
