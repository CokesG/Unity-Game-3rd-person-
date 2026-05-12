# Tuning Log

Last updated: 2026-05-12

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

## 2026-05-09 - Gun Debug Overlay

Implemented:

- Live `F1` gun tuning overlay.
- Shot counters: fired, world hits, registered target hits, crits, misses, and blocked shots.
- Rolling accuracy, observed RPM, recent DPS, raw body DPS, sustained body DPS, and TTK estimates.
- Reload countdown and progress readout.
- Per-target current-life and session damage stacks.
- Per-target registered hit, critical hit, defeat, and last-damage readouts.

Use this pass to make tuning decisions based on measured behavior instead of feel alone.

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

## 2026-05-12 - MCP Hitbox Validation

Validated through Unity MCP:

- Unity MCP connection and `Unity_ReadConsole` tool calls are working.
- Console was clean for warnings/errors before gameplay probes.
- Default prototype target geometry allowed the body capsule to intercept rays aimed at `Head_Critical`.

Implemented:

- `TargetDummy` now shortens its prototype body capsule when a `Head_Critical` child exists, so critical hit rays reach the head collider first.
- Impact markers are runtime-only, keeping MCP/editor-side validation from leaving temporary colliders in the scene.

Follow-up:

- Repeat Play mode tests in `TPS_TestGym` with `F1` overlay visible and confirm critical hits, stacked damage, reload countdown, observed RPM, and recent DPS all move as expected.

## 2026-05-12 - Movement and Combat Debug Hooks

Implemented:

- Moved slow-walk fallback from Ctrl to Alt so crouch/slide input is no longer competing with slow-walk.
- Added `CriticalHitbox` as the explicit critical-hit marker while keeping `Head_Critical` as a fallback.
- Added rifle recoil burst tracking with pitch ramp, learnable yaw pattern, reset delay, and small yaw randomness.
- Added live F1 movement telemetry: mode, current/desired speed, acceleration, vertical speed, coyote timer, jump buffer, slide buffer, grounded-stable time, jump lock, slide speed, and stand blocker.
- Added live F1 recoil telemetry: burst index, last pitch/yaw kick, camera recoil pitch/yaw, and recoil reset delay.
- Added `NoDmg` world-hit reporting to separate geometry/collider hits from damage-registered hits.

Use this pass to tune from evidence: if the rifle feels unfair, check recoil burst and spread first; if slide/jump feels inconsistent, check movement buffers and jump lock before changing values.

## 2026-05-12 - Procedural Crouch-Walk V2 Review Pass

Implemented:

- Regenerated the full-quality forward/back/left/right procedural crouch-walk FBXs with a lower, more compact stance.
- Updated `Tools/Blender/create_nightfall_crouch_walk_procedural.py` to ground every key pose, reduce leg crossing, and bake temporary IK hand targets into a tighter two-hand shooter-ready arm pose.
- Added directional review cycling to `NightfallAnimationSandboxDriver`; in the linked sandbox, press `9` for `Crouched Walk`, then `Q` / `E` to cycle forward/back/left/right procedural candidates.
- Kept live `SampleScene` crouch-walk promotion off. The live player should still hold the reviewed crouch pose while moving crouched.

Validation:

- Blender MCP export bounds: all regenerated full-quality candidates sample at `min_z = 0.015` across key poses.
- Unity recompiled successfully after the sandbox driver change.
- Unity logged `Configured 8 procedural crouch-walk candidate importers.` after running the reimport utility.

## 2026-05-12 - Crouch-Walk Crossed Feet Fix

Implemented:

- Replaced the over-ambitious procedural crouch-walk leg cycle with a safer wide crouch shuffle.
- Reduced knee bend, widened the stance, and cut the step swing down so the feet do not scissor across each other during sandbox review.
- Regenerated the full-quality forward/back/left/right procedural FBXs.

Validation:

- Blender MCP preview shows clear left/right foot separation from the front.
- Forward candidate sampled at `min_z = 0.015` and roughly `0.394m` left/right foot gap.
- This remains sandbox-only; do not promote live crouch-walk until an authored or motion-captured crouch-walk set replaces the procedural placeholder.

## 2026-05-12 - Procedural Crouch-Walk Quarantined

Implemented:

- Rejected the current procedural crouch-walk review lane after live sandbox screenshots still showed broken crouch-walk body/leg presentation.
- Changed `NightfallAnimationSandboxDriver` so `9 Crouched Walk` defaults to the safe crouch hold instead of auto-loading procedural candidates.
- Remapped the linked sandbox controller `Crouch Walk` state to the safe crouch hold so bypassing the driver cannot show the rejected procedural clip.
- Left procedural candidate cycling behind the manual `enableProceduralCrouchWalkReview` debug toggle only.

Validation:

- Live `SampleScene` crouch-walk promotion remains off.
- Next acceptable path is an authored or motion-captured directional crouch-walk set, not more default routing through the rejected procedural placeholder.
