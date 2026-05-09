# Tuning Log

Last updated: 2026-05-09

## 2026-05-09 - TPS Vertical Slice V1

Implemented:

- Movement acceleration/deceleration.
- Coyote time.
- Jump buffering.
- Slide input buffering.
- Separate rotation speeds by state.
- Camera shoulder swap.
- Camera recoil.
- Center-screen aim ray.
- Muzzle obstruction contract.
- Prototype hitscan rifle.
- Target dummy damage.
- Reticle, hitmarker, ammo/state HUD.
- Runtime bootstrap for immediate testing in current scenes.
- Editor test gym builder.

Starting feel hypothesis:

- Keep sprint fast but not twitchy.
- Aim rotation should be faster than normal movement rotation so the character follows the reticle confidently.
- Rifle ADS should be controlled mostly by recoil, not heavy random bloom.
- Hip fire should visibly bloom so close-range spray is readable but not laser-accurate.

Known risks:

- Values have not yet been validated in Unity Play mode in this pass.
- Runtime bootstrap is intentionally prototype-friendly; later production scenes should use explicit scene objects/prefabs instead.
- No audio or authored muzzle flash assets yet.
- Animation integration is currently trigger-level only.

Next tuning session:

1. Generate `TPS_TestGym`.
2. Run sprint/slide/jump tests for 10 minutes.
3. Tune acceleration/deceleration first.
4. Tune camera aim distance/FOV second.
5. Tune rifle recoil/spread third.

## 2026-05-09 - Jump Stack Fix

Implemented:

- Added a jump lock that stays active until the controller has been stably grounded again.
- Kept coyote time and jump buffering, but they no longer re-arm while the post-jump lock is active.
- Added debug readouts for grounded stable time and jump lock state.

Manual test:

- Press Space repeatedly after takeoff.
- Expected: one jump only, then no second jump until the player lands.
- Press Space just before landing.
- Expected: buffered jump can still fire after the grounded reset window.
