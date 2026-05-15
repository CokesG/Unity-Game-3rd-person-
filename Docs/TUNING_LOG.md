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
- Left procedural candidate cycling quarantined behind manual debug-only tooling.

Validation:

- Live `SampleScene` crouch-walk promotion remains off.
- Next acceptable path is an authored or motion-captured directional crouch-walk set, not more default routing through the rejected procedural placeholder.

## 2026-05-12 - Authored Crouch-Walk Set Reconnected For Review

Implemented:

- Verified the downloaded `Crouch Walk Forward/Back/Left/Right.fbx` files are already present in `Assets/Animations/NightfallVanguard/UserCrouchWalk/` as matching `User_CrouchWalk_*` assets.
- Confirmed the live `PlayerHumanoid.controller` directional blend tree maps center to crouch hold, forward to `User_Crouch_Walk_Forward`, back to `User_Crouch_Walk_Back`, left to `User_Crouch_Walk_Left`, and right to `User_Crouch_Walk_Right`.
- Reconnected the linked sandbox `Crouch Walk` state and `NightfallAnimationSandboxDriver` review cycling to the authored user set instead of the rejected procedural set.
- Superseded by the rollback below: the authored set stayed too unreliable in play and is no longer live-promoted in `SampleScene`.

## 2026-05-12 - Authored Crouch-Walk Promoted Live

Implemented:

- Enabled `crouchWalkClipPromoted` in `SampleScene` so moving while crouched now enters `Base Layer.Crouch Walk`.
- Kept `forceCrouchWalkWhenMoving` off; live use depends on the explicit promoted flag.
- Preserved the sandbox `9` plus `Q` / `E` review path for future authored crouch-walk tuning.

## 2026-05-12 - Crouch-Walk Directional Blend Smoothing

Implemented:

- Changed the dormant crouch-walk directional parameters to use raw move input while crouched, so Back/Left/Right clips can be reached during future review even when the player root turns toward movement.
- Added crouch-specific movement damping and crossfade timing for smoother crouch idle-to-walk and walk-to-idle transitions.
- Added crouch-walk enter/exit speed thresholds to reduce idle/walk flicker near zero speed.

## 2026-05-12 - Combat Locomotion Debug And Crouch Stability

Implemented:

- Added F1 animation telemetry for movement state, `MovementX` / `MovementY`, crouch-walk visual activity, Animator state path, crouch transition timer, and visual grounding offset.
- Added crouch weapon stability tuning: crouched non-slide firing applies `crouchSpreadMultiplier` and `crouchRecoilMultiplier`.
- Added F1 weapon telemetry for stance spread and recoil multipliers so crouch accuracy/recoil tuning is visible during test-gym passes.
- Kept aim-strafe live promotion blocked for now because `PlayerHumanoid.controller` does not yet contain a live aim-strafe state or directional aim blend tree.

## 2026-05-12 - Slide Exit And Crouch Transition Stabilization

Implemented:

- Added slide ground-stick force so slide movement keeps the `CharacterController` biased down instead of popping upward during slide-to-crouch transitions.
- Capped horizontal speed carry when a slide exits into crouch, reducing launch-like carryover if the slide ends while grounding is unstable.
- Increased crouch idle/walk crossfade and separated crouch transition crossfade from jump/air timings for smoother crouch entry, crouch walk, and crouch exit.
- Added F1 `leftGround` visibility beside slide speed to help diagnose slide/crouch launch bugs.

## 2026-05-14 - Crouch-Walk Pulled From Live

Implemented:

- Disabled `crouchWalkClipPromoted` in `SampleScene` so moving while crouched returns to the stable held crouch pose.
- Disabled default linked-sandbox crouch-walk directional review; `9 Crouched Walk` shows the safe crouch hold unless review is manually enabled.
- Kept the authored crouch-walk FBXs in the project for later audit instead of deleting source assets.

## 2026-05-14 - Slide/Crouch Stability And Debug Pass

Implemented:

- Added a short stable-ground requirement before slide start so the player cannot enter slide from a noisy grounded frame.
- Added a post-slide crouch ground-stick timer to keep slide-to-crouch exits biased downward instead of popping upward.
- Added slide exit reason and post-exit stick time to the F1 movement telemetry.
- Added real-time reload countdown to the always-visible weapon readout, not only the expanded F1 overlay.
- Added short post-slide jump and stand locks so slide-to-crouch cannot immediately fire a jump or stand transition while the capsule is still settling.
- Hardened slide start by snapping the `CharacterController` immediately into the crouch capsule, forcing slide step offset to `0`, and applying downward stick before horizontal slide movement.
- Routed live slide visuals to `Base Layer.Slide` with the existing `Nightfall_Slide` animation while keeping root motion off.
- Corrected live slide to use the original baked `NightfallVanguard_Armature|slide_light` clip from `NightfallVanguard_Prototype_Animated.fbx` after the standalone slide anim failed to produce the intended low slide pose in play.
- Quarantined slide visuals after the original baked slide clip also produced a broken airborne/arms-up pose on the current full-quality rig. Keep slide gameplay active, but leave `slideClipPromoted` off until a fresh rig-compatible slide animation is imported and audited.
- Imported the fresh user-provided `Running Slide.fbx` as `Assets/Animations/NightfallVanguard/UserSlide/User_Running_Slide.fbx`, pointed `Base Layer.Slide` at `User_Running_Slide`, and re-enabled `slideClipPromoted` in `SampleScene` for live review.
- Smoothed slide animation transitions by adding dedicated slide enter and slide-to-crouch crossfade timings. `Slide -> Crouch Idle` now crossfades into the held crouch pose instead of using the hard crouch-idle `Play()` path.
- Smoothed post-slide crouch-to-stand by extending the stand unlock to `0.30s` and adding a `0.35s` post-slide crouch settle window that uses a `0.30s` stand-up crossfade. F1 now shows the animation `slideSettle` timer.
- Restored slide visual priority so an active slide selects `Base Layer.Slide` before airborne/jump fallback states. This prevents a noisy grounded frame during slide start from hiding the slide animation.
- Audited `User_Running_Slide` through Unity MCP frame renders and found it reads upright/run-like on the live rig, so the live `Slide` state now uses the stable held crouch pose as a safe visible low slide placeholder. Reduced `Stand Up` state speed from `4.6` to `3.4` and extended its live timer to `1.05s` to soften the crouch-to-stand rise.
- Re-enabled `User_Running_Slide` on `Base Layer.Slide` for live visual review after confirming the placeholder made slide gameplay visible but hid the authored slide animation.
- Removed the `slideClipPromoted` gate from live motor slide state selection. When `ThirdPersonMotor.IsSliding()` is true, `PlayerAnimationController` now enters `Base Layer.Slide` so stale Play Mode component flags cannot produce gameplay slide without the selected slide animation.

Validation target:

- In `SampleScene`, sprint-slide on flat ground and ramps, then let the slide decay into crouch. Expected: no upward launch, F1 shows `slideExit` as `timer`, `speed`, `aim cancel`, or `left ground`, and `settle stick/jump/stand` briefly counts down after crouch entry.

## 2026-05-15 - Player Settings And Reticle Pass

Implemented:

- Added an Esc settings panel through `TPSReticleHUD` that unlocks the cursor while open and relocks it when closed.
- Replaced the old hot camera sensitivity defaults with normal shooter-style sliders: Sensitivity, ADS Sens, and Vertical. The hidden look math uses a Source/Apex-like `0.022` degrees-per-pixel scalar so players do not need to edit DPI or cm/360 values.
- Added persistent reticle customization for crosshair, dot, circle, and circle+dot styles with color presets, size, gap, thickness, and outline controls. Dot and circle reticles use generated circle/ring textures for cleaner shapes.
- Updated the live scene camera sensitivity multipliers to `1` so the calibrated settings own the actual look rate.
