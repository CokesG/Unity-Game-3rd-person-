# Player Architecture

## Principle

The player is controller-driven.

The `Player` root owns collision, movement, input, camera targeting, animation state, and combat hooks. The humanoid model is a child visual object that follows the root.

## Scene Hierarchy

```text
Player
- CharacterController
- GroundCheck
- CameraTarget
- WeaponMuzzle
- CharacterVisual
  - Animator
  - Rigged humanoid model
- PlayerInputHandler
- ThirdPersonMotor
- PlayerAnimationController
- PlayerCombatHooks
- PlayerWeaponController
```

## Responsibilities

### Player Root

The gameplay object. This object should move through `CharacterController.Move`.

Keep these components here:

- `CharacterController`
- `PlayerInputHandler`
- `ThirdPersonMotor`
- `PlayerAnimationController`
- `PlayerCombatHooks`
- `PlayerWeaponController`

### GroundCheck

Child transform used as the ground probe position. It should stay near the lower part of the capsule.

### CameraTarget

Child transform followed by `ThirdPersonCameraController`. It should sit around upper chest/head height, not on a bone.

### WeaponMuzzle

Child transform used by `PlayerWeaponController` as the third-person shot origin. It can be a temporary player-root child while weapon art is placeholder. Later, it should follow a weapon socket or hand socket, but gameplay still resolves through the player weapon controller.

### CharacterVisual

Visual-only child. Put the humanoid mesh/model here.

Keep:

- Local position adjusted so feet touch the ground.
- Local rotation adjusted so model forward matches Unity +Z.
- Local scale adjusted to fit the capsule.
- Animator `Apply Root Motion` disabled.

## Movement Rules

Normal movement:

- Input is camera-relative.
- Player root rotates toward movement direction.
- Shift sprint only applies when moving forward.

Aim movement:

- Right click is hold, not toggle.
- Player root faces camera forward.
- Animator receives `IsAiming = true`.
- Strafe values are sent through `MovementX` and `MovementY`.

Jumping:

- Space triggers jump only when grounded.
- `Jump` trigger fires on jump start.
- `Land` trigger fires when grounded after being airborne.

## Things To Avoid

- Do not make the humanoid mesh the movement root.
- Do not move collision to the skinned mesh.
- Do not parent the camera to a head/spine bone.
- Do not enable root motion until combat and locomotion are intentionally designed for it.
- Do not scale the `Player` root to fix character model size. Scale `CharacterVisual` or the model child.
- Do not let bullets originate from the camera without a muzzle obstruction check.
