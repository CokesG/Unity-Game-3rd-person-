# Camera And Aim Spec

Last updated: 2026-05-09

## Goal

The player must trust the reticle. If the reticle says a shot is clear, the muzzle/cover rules should agree. If the muzzle is blocked, the reticle should show that the shot is blocked.

## Implemented V1

`ThirdPersonCameraController` now supports:

- Exploration and aim camera distances.
- Exploration, aim, and sprint FOV targets.
- Shoulder swap through `V` or middle mouse fallback.
- Camera recoil impulses.
- Aim ray from screen center.
- Aim ray filtering so the player does not target itself.
- Muzzle blocked state for the HUD.
- Collision buffer when the camera boom hits geometry.

## Shot Origin Contract

`PlayerWeaponController` follows this rule:

```text
1. Camera casts from screen center to resolve intended aim point.
2. Weapon muzzle checks line of sight to that aim point.
3. If the muzzle is blocked by cover, the shot hits the blocker.
4. If clear, hitscan shots fire from muzzle toward the aim point with weapon spread.
5. The HUD turns the reticle red when the muzzle is blocked.
```

## Required Manual Tests

- Right click transitions to closer aim view.
- Releasing right click returns to exploration view.
- V or middle mouse swaps shoulder.
- Reticle stays screen-center.
- Camera does not clip badly through waist cover or walls.
- Aiming over low cover shows blocked state if the muzzle is still behind cover.
- Shots do not pass through cover from a blocked muzzle.
- Recoil kicks the camera without permanently drifting aim.

## Next Camera Tuning

- Tune exploration and aim FOV after the rifle feels good.
- Add optional camera-side obstruction fade if the character blocks too much of the reticle.
- Add per-weapon aim FOV and recoil camera values later.
