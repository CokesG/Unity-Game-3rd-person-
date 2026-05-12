# Gunplay Spec

Last updated: 2026-05-12

## Goal

Build one excellent rifle loop before adding a weapon roster.

The first target is a Fortnite-style third-person rifle feel: readable hip spread, tighter ADS, recoil as the main ADS skill check, clear hit feedback, and no shooting lockout after slide/jump unless intentionally tuned.

## Implemented V1

New runtime scripts:

- `WeaponDefinition`: data asset for weapon tuning.
- `PlayerWeaponController`: reads attack/reload input, handles ammo, reloads, spread, recoil, hitscan shots, muzzle obstruction, and damage.
- `TargetDummy`: damageable target with health, hit flash, defeat, and reset.
- `CriticalHitbox`: explicit marker for colliders that should count as critical hits.
- `TPSReticleHUD`: crosshair, blocked-muzzle color, hitmarker, damage number, ammo/state readout, and live gun tuning debug overlay.
- `TpsRuntimeBootstrap`: adds the prototype rifle, HUD, and simple dummies at runtime if the current scene does not already have them.
- `TpsTestGymBuilder`: editor tool for a full graybox test gym.

## Live Debug Overlay

Press `F1` in Play mode to show/hide the gun tuning overlay.

The overlay reports:

- Weapon state, ammo, reload countdown, reload progress, fire cooldown, and muzzle blocked state.
- Movement mode, speed, desired speed, acceleration, vertical velocity, grounded state, coyote timer, jump buffer, slide buffer, grounded-stable timer, jump lock, slide speed, and blocked stand attempts.
- Recoil burst index, last pitch/yaw kick, camera recoil pitch/yaw, and recoil reset delay.
- Total shots fired, world hits, registered target hits, no-damage world hits, critical hits, misses, and blocked shots.
- Lifetime accuracy, recent rolling accuracy, critical-hit rate, total damage, recent DPS, raw body DPS, sustained body DPS, and observed RPM.
- Current spread, spread add, hip/ADS spread, and spread recovery rate.
- Last shot registration state, last damage, last hit distance, last collider hit, and last registered target.
- Estimated TTK against the focused target's current health and full health.
- Per-target current-life damage, session damage, registered hits, critical hits, defeats, and last damage.

The core distinction in the overlay is:

- `World hits`: the shot ray hit any non-player collider.
- `Registered hits`: a shot reached an `IDamageable` target and actually applied damage.
- `NoDmg`: a world hit that did not reach an `IDamageable`; use this to find bad layers, cover clipping, or collider occlusion.
- `Misses`: the shot ray hit nothing.
- `Blocked`: the muzzle obstruction check forced the shot into cover before it could reach the camera aim point.

## Prototype Rifle Defaults

```text
Weapon: Prototype Rifle
Fire mode: automatic
Shot model: hitscan
Damage: 24 body
Headshot multiplier: 1.8
Max range: 180
Fire rate: 540 RPM
Magazine: 30
Reload: 1.9s
Empty reload: 2.25s
Hip spread: 2.4 degrees
ADS spread: 0.18 degrees
Moving spread add: 0.65 degrees
Airborne spread add: 1.1 degrees
Slide spread add: 1.4 degrees
Camera recoil pitch: 0.42
Camera recoil yaw: 0.18
Recoil reset delay: 0.25s
Recoil pitch ramp per shot: 0.035
Max recoil pitch per shot: 0.68
Recoil yaw pattern step: 0.045
Max recoil yaw pattern: 0.28
Recoil yaw randomness: 0.04
```

## Input

- Fire: left mouse.
- Aim: hold right mouse.
- Reload: `R` fallback.
- Shoulder swap: `V` or middle mouse fallback.
- Sprint: left Shift.
- Slide/crouch: left Ctrl or C through the existing input asset.

## Required Manual Tests

- Fire while idle, moving, aiming, jumping, and sliding.
- ADS spread is tighter than hip spread.
- Holding fire grows spread/recoil.
- Releasing fire recovers spread.
- Rifle reloads with `R`.
- Empty magazine starts empty reload when firing.
- Damageable dummies flash and reset after defeat.
- Colliders with `CriticalHitbox` produce critical damage.
- Head child named `Head_Critical` still produces critical damage as a fallback.
- Prototype target body colliders must not occlude `Head_Critical`; the default `TargetDummy` shortens its capsule when a `Head_Critical` child exists.
- Hitmarker appears on hit.
- Miss feedback is weaker than hit feedback.
- Reticle turns red when muzzle is blocked.
- `F1` overlay shows reload countdown in real time.
- `F1` overlay increments registered hits and stacked target damage only when `IDamageable` receives damage.
- `F1` overlay shows recoil burst/kick data and movement buffer data while firing, jumping, crouching, and sliding.

## Next Gunplay Work

1. Tune the prototype rifle recoil pattern in Play mode until ADS sustained fire has a learnable pull.
2. Replace runtime fallback weapon with committed weapon assets for every test scene.
3. Add muzzle flash and audio clips.
4. Add reload/fire animations through upper-body layers.
5. Add shotgun with pellet spread.
6. Add SMG with stronger close-range spread growth.
7. Add DMR/sniper with projectile or precise hitscan behavior.
