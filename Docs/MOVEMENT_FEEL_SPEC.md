# Movement Feel Spec

Last updated: 2026-05-09

## Goal

The player controller should feel responsive, readable, and combat-mobile before advanced traversal is added.

The `Player` root remains controller-driven through `ThirdPersonMotor`. Root motion stays off.

## Implemented V1

`ThirdPersonMotor` now supports:

- Ground acceleration and deceleration instead of instant horizontal velocity.
- Separate acceleration for normal, sprint, aim, and air movement.
- Separate rotation speeds for normal movement, sprint, aim, and slide.
- Coyote time.
- Jump buffering.
- Slide input buffering.
- Slide steering.
- Low-ceiling stand checks.
- Debug readouts for movement state, speed, buffers, and blocked stand attempts.

## Starting Tuning Values

```text
Walk speed: 2.5
Run speed: 5.5
Sprint speed: 7.25
Aim speed: 3.0
Crouch speed: 2.0
Normal acceleration: 34
Sprint acceleration: 42
Aim acceleration: 28
Ground deceleration: 46
Air acceleration: 10
Air deceleration: 4
Air control: 0.55
Normal rotation speed: 15
Aim rotation speed: 24
Sprint rotation speed: 12
Slide rotation speed: 10
Jump height: 1.5
Gravity: -9.81
Coyote time: 0.10s
Jump buffer: 0.12s
Slide input buffer: 0.10s
Slide start speed: 8.5
Slide end speed: 3.0
Slide duration: 0.9s
Slide steer strength: 5
```

## Required Manual Tests

- WASD starts quickly without snapping instantly to full speed.
- Releasing movement stops cleanly without ice sliding.
- Sprint only activates while moving forward and holding Sprint.
- Pressing crouch during sprint starts slide.
- Pressing crouch just before slide eligibility still slides if the buffer window catches it.
- Slide can steer slightly but cannot turn instantly.
- Slide exits to crouch.
- Jump can trigger just after stepping off a ledge.
- Jump can trigger if pressed just before landing.
- Jump from crouch only happens when there is room to stand.
- Aim movement faces camera forward and allows strafe.
- Camera target lowers smoothly while crouching/sliding.

## Do Not Add Yet

- Wall kick.
- Roll landing.
- Wall scramble.
- Mantle.
- Stamina.

Add those only after rifle, reticle, and camera trust are stable.
