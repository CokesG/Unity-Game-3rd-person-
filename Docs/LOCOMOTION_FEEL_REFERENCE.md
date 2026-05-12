# Locomotion Feel Reference

Last updated: 2026-05-12

## Current Rule

The Player uses controller-driven movement and code-driven animation state selection.

Keyboard input is digital, so pressing `W` is full forward input. For a third-person shooter, the clean default is for normal WASD to read as a jog/run, not as a walk that quickly pops into run.

Current tuning:

- Normal WASD uses `Run` visually.
- `Left Alt` or `Right Alt` holds `SlowWalkPressed`, which uses walk speed and the `Walk` animation.
- `Left Ctrl` and `Right Ctrl` stay reserved for crouch/slide fallback behavior so sprint-slide input is not suppressed by slow-walk.
- Holding Shift requests sprint speed immediately.
- Until a true sprint clip is promoted, sprint speed still uses the `Run` visual.
- Aim uses slower movement and currently falls back to the `Walk` visual until aim-specific clips are promoted.
- Crouch gameplay is active. Stand-to-crouch, crouch idle, and crouch-to-stand are live-promoted. Crouch-walk is quarantined in live play; the linked sandbox now previews grounded, compact full-quality procedural candidates on `9 Crouched Walk`, and `Q` / `E` cycles forward/back/left/right. Moving crouched in `SampleScene` should still hold the reviewed crouch pose until the full directional set passes review. Grounded visual grounding uses foot/toe bones first and renderer bounds only as a fallback so the visual child can be corrected without moving the `Player` root or `CharacterController`.
- Jump uses a short safe Nightfall-native `Jump Start` clip while the physical jump stays controller-driven and tuned to a low, snappy arc (`jumpHeight` `0.5625`, `gravity` `-22`, falling gravity multiplier `1.5`). Falling and landing clips remain disabled until clean clips pass review.
- Crossfade is `0.18` seconds so state changes do not snap.

## Why This Shape

Unity's own animation guidance separates two concepts:

- Blend Trees are the better long-term approach for similar motions like walk and run because the blend parameter can be speed.
- Transitions/state changes need duration and conditions so motion does not pop between states.
- 1D Blend Tree thresholds should match the movement speeds of the clips when possible.

References:

- Unity Blend Trees: https://docs.unity.cn/Manual/class-BlendTree.html
- Unity Animation Transitions: https://docs.unity.cn/Manual/class-Transition.html
- Unity 1D Blending and thresholds: https://docs.unity3d.com/Manual/BlendTree-1DBlending.html

## Tooling Shortlist

We are not using several Unity packages that would speed this up:

- `com.unity.cinemachine`: use for the third-person follow/aim camera instead of growing a custom camera rig.
- `com.unity.animation.rigging`: use Two Bone IK and aim constraints for feet, hands, weapon aim, and upper-body correction.
- Unity Starter Assets Third Person: use as a reference implementation, not as a full replacement, because our controller already has shooter-specific rules.
- OpenKCC: consider later if CharacterController collision/grounding becomes the blocker.

These packages will not remove animation cleanup work, but they can reduce the amount of custom camera, IK, and controller infrastructure we maintain.

## Near-Term Plan

For clip-by-clip promotion, keep explicit state selection. It is simple and safe while clips are still being evaluated.

After idle, walk, run, sprint, and jump are accepted, replace the hard walk/run state switch with a real locomotion Blend Tree:

- `Idle` around speed `0`
- `Walk` around speed `2.5`, entered by slow walk or analog low input
- `Run` around speed `5.5`, the default WASD movement
- `Sprint` around speed `7.25`, entered by Shift

That will remove most hard cuts and make the animation follow actual movement speed more naturally.

## Tuning Knobs

In `PlayerAnimationController`:

- `locomotionCrossFadeTime`: raise it slightly if transitions still feel sharp.
- `walkClipPromoted`: keep true while Alt slow walk is available.
- `sprintClipPromoted`: keep false until a real sprint clip has passed sandbox review.

In `ThirdPersonMotor`:

- `normalAcceleration`: lower it if movement itself accelerates too fast.
- `sprintAcceleration`: lower it if Shift feels too twitchy.
- `walkSpeed`: tune Alt slow walk speed.
- `runSpeed` and `sprintSpeed`: tune only after animation timing feels acceptable.
- `jumpHeight`: current value is `0.5625`, which is 25% lower than the previous `0.75`. Tune this in the motor and the live scene together; animation clips should follow the controller, not create the jump height.

In `PlayerInputHandler`:

- `SlowWalkPressed` reads a `SlowWalk` action if one exists.
- Until the input asset has a formal action, `Left Alt` and `Right Alt` are used as the fallback slow-walk hold.
- Keep Ctrl/C available for crouch and slide; do not bind slow-walk to Ctrl.
