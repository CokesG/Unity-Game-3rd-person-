# Gunplay Spec

Last updated: 2026-05-09

## Goal

Build one excellent rifle loop before adding a weapon roster.

The first target is a Fortnite-style third-person rifle feel: readable hip spread, tighter ADS, recoil as the main ADS skill check, clear hit feedback, and no shooting lockout after slide/jump unless intentionally tuned.

## Implemented V1

New runtime scripts:

- `WeaponDefinition`: data asset for weapon tuning.
- `PlayerWeaponController`: reads attack/reload input, handles ammo, reloads, spread, recoil, hitscan shots, muzzle obstruction, and damage.
- `TargetDummy`: damageable target with health, hit flash, defeat, and reset.
- `TPSReticleHUD`: crosshair, blocked-muzzle color, hitmarker, damage number, ammo/state readout.
- `TpsRuntimeBootstrap`: adds the prototype rifle, HUD, and simple dummies at runtime if the current scene does not already have them.
- `TpsTestGymBuilder`: editor tool for a full graybox test gym.

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
- Head child named `Head_Critical` produces critical damage.
- Hitmarker appears on hit.
- Miss feedback is weaker than hit feedback.
- Reticle turns red when muzzle is blocked.

## Next Gunplay Work

1. Replace runtime fallback weapon with committed weapon assets for every test scene.
2. Add muzzle flash and audio clips.
3. Add reload/fire animations through upper-body layers.
4. Add shotgun with pellet spread.
5. Add SMG with stronger close-range spread growth.
6. Add DMR/sniper with projectile or precise hitscan behavior.
