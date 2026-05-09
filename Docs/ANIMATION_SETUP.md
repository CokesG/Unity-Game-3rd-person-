# Animation Setup

## Animator Controller

Current controller:

`Assets/Animations/PlayerHumanoid.controller`

Assign this controller to the `Animator` on `Player/CharacterVisual`.

Keep `Apply Root Motion` disabled.

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

## Placeholder States

The controller contains placeholder states for:

- `Idle`
- `Walk`
- `Run/Jog`
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

Drop clips into matching states or blend tree slots:

- `Idle`: idle clip
- `Walk`: walk forward in-place
- `Run/Jog`: run or jog forward in-place
- `Sprint`: sprint forward in-place
- `Jump Start`: jump takeoff
- `Falling / In Air`: falling or airborne loop
- `Landing`: landing
- `Aim Idle`: aim idle
- `Aim Walk / Strafe`: aim forward, back, left, and right strafe clips
- `Attack Placeholder`: first basic attack clip
- `Ability Placeholder`: temporary ability animation

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

## Testing

In Play mode:

- `Speed` should rise while moving.
- `MovementX` should move negative/positive while strafing in aim mode.
- `MovementY` should move negative/positive while moving back/forward.
- `IsAiming` should be true only while holding right click.
- `IsSprinting` should be true only while holding Shift and moving forward.
- `IsJumping`, `IsFalling`, and `VerticalVelocity` should react during jump/fall.

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
