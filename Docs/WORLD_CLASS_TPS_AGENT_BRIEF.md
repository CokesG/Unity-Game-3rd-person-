# World-Class TPS Agent Brief

Last updated: 2026-05-09

## Purpose

This document turns the current project docs plus Fortnite-style third-person shooter research into a practical brief for future agents.

The goal is not to clone Fortnite. The goal is to build a controller-driven third-person shooter with the same kind of readable, responsive, combat-friendly movement and weapon feel: fast traversal, strong camera control, aim-state clarity, crisp weapon feedback, and measurable tuning gates.

Use this brief with:

- `Docs/PROJECT_STATUS.md`
- `Docs/PLAYER_ARCHITECTURE.md`
- `Docs/ANIMATION_SETUP.md`
- `Docs/ASSET_PIPELINE.md`

## Research Baseline

Current Fortnite-style shooter feel is a stack, not a single feature.

Official Epic/Fortnite references reviewed:

- Fortnite Chapter 7 update notes: new weapons were described as more responsive, with layered recoil, smoother ADS transitions, saved empty-mag reload progress, clearer hitmarkers, and ADS while jumping.
  - https://www.fortnite.com/news/fortnite-battle-royale-chapter-seven-pacific-break-v39-00-update-notes
- Fortnite Ballistic v37.10: Epic removed reduced ADS accuracy while moving for most weapons and replaced that penalty with higher recoil, improved reload/tactical sprint animations, and fixed cases where players could not shoot after sliding.
  - https://www.fortnite.com/news/fortnite-ballistic-v37-10-adds-the-veiled-precision-smg-deployable-wall-gadget-and-more
- Fortnite Chapter 6 added Ledge Jump, Roll Landing, Wall Kick, and Wall Scramble as movement features.
  - https://www.fortnite.com/news/fight-as-a-ronin-in-fortnite-battle-royale-chapter-6-season-1-hunters
- Epic later disabled Wall Kick and Roll Landing in Epic-made shooter modes, while leaving them available in creator-made experiences. Treat wall-kick style parkour as optional/experimental for this project, not core shooter movement.
  - https://www.epicgames.com/help/en-US/c-202300000001636/c-202300000001721/are-the-wall-kick-and-roll-landing-movement-features-currently-disabled-a202300000010721
- Fortnite Creative's Third Person Controls docs expose movement, facing, turn-rate, aiming, shooting, and targeting-assistance controls as explicit tunables. Our Unity docs need the same level of explicit knobs.
  - https://dev.epicgames.com/documentation/fortnite-creative/using-third-person-controls-devices-in-fortnite-creative
- Fortnite Showdown, announced March 19, 2026, refreshed the weapon pool and added traversal items. This reinforces that Fortnite-style feel evolves through tuned movement items and weapon meta, not one permanent weapon setup.
  - https://www.fortnite.com/news/showdown-in-the-new-fortnite-battle-royale-season

## Current Docs Audit

### What Is Strong

- The docs correctly protect the architecture: `Player` root owns gameplay, collision, camera target, input, animation state, and combat hooks.
- The docs correctly keep the humanoid model visual-only.
- Root motion is intentionally disabled, which is the right default for tight shooter movement.
- The animation docs already define useful parameters: `Speed`, `MovementX`, `MovementY`, grounded/falling/aiming/sprinting states, and combat triggers.
- The asset docs correctly call out that the current Nightfall Vanguard model is a prototype and not weapon-hand hero quality.

### What Is Missing For World-Class Movement

- A movement feel spec with numeric targets for speed, acceleration, deceleration, rotation, jump, air control, coyote time, jump buffering, slide steering, and transition windows.
- A movement state matrix that defines entry rules, exit rules, cancels, camera behavior, weapon behavior, animation state, and acceptance tests for each state.
- A traversal plan that separates core shooter movement from later parkour experiments.
- A test arena spec with ramps, ledges, doorways, low ceilings, combat corners, and target dummies.
- Playtest language agents can use to report feel: snappy, floaty, sticky, over-rotating, input-laggy, camera-blocked, aim-drift, slide-lock, etc.

### What Is Missing For World-Class Gunplay

- A weapon data schema.
- A clear shot-origin contract for third-person aiming.
- A recoil/spread/bloom policy.
- A reticle policy.
- Hit detection rules.
- Hitmarker, damage number, impact VFX, audio, controller rumble, and camera impulse rules.
- Aim assist / bullet magnetism / target friction policy.
- Per-movement accuracy and recoil rules.
- Weapon swap, reload, interrupt, sprint, slide, and jump interaction rules.
- Acceptance tests for first rifle, shotgun, SMG, and projectile weapon.

### What Is Missing For Agent Execution

- A single "definition of done" for movement/gunplay work.
- A rule that agents must update the relevant spec before introducing new mechanic behavior.
- A requirement to report serialized Unity values changed, scene objects touched, prefabs touched, and manual playtest results.
- Feature order that prevents agents from building fancy traversal before the basic camera/reticle/weapon loop is reliable.

## Product Direction

Build the game around these feel pillars:

- Responsive: input should be visible immediately, even if the full motion has acceleration.
- Readable: enemies, reticle, hit feedback, and character pose should remain clear while moving fast.
- Combat mobile: players should be able to aim, shoot, slide, jump, and reposition without the controller fighting them.
- Camera trustworthy: bullets and reticle behavior must match what the player believes they aimed at.
- Tunable: movement and weapon feel must live in inspectable data, not buried constants.
- Testable: every new mechanic must have a manual test path and at least one measurable acceptance check.

## Movement Spec Agents Need

Create or expand docs so every movement state has this contract:

```text
State:
Input:
Entry rules:
Exit rules:
Cancel rules:
Speed / acceleration / deceleration:
Rotation behavior:
Camera behavior:
Weapon behavior:
Animation parameters:
Failure cases:
Acceptance tests:
```

Required movement states:

- Idle
- Walk
- Run
- Sprint
- Aim idle
- Aim strafe forward/back/left/right
- Crouch idle
- Crouch move
- Slide
- Jump start
- Airborne rising
- Falling
- Landing
- ADS while jumping
- Optional later: mantle
- Optional later: ledge jump
- Optional later: wall scramble
- Experimental only: wall kick / roll landing

Initial tuning targets should be documented as starting points, then adjusted through playtests:

```text
Walk speed: current 2.5, test 2.6-3.0
Run speed: current 5.5, test 5.2-5.8
Sprint speed: current 7.25, test 7.0-7.8
Aim speed: current 3.0, test 2.8-3.4
Crouch speed: current 2.0, test 1.8-2.4
Slide start speed: current 8.5, test 8.0-9.5
Slide end speed: current 3.0, test 2.8-3.5
Slide duration: current 0.9s, test 0.75-1.1s
Rotation speed: current 15, test normal/aim/sprint separately
Jump height: current 1.5, keep unless jump feels too floaty
Gravity: current -9.81, test stronger gravity only after animation and camera are stable
Jump buffer: add target 0.10-0.15s
Coyote time: add target 0.08-0.12s
Slide input buffer: add target 0.10s
```

Do not add advanced parkour before these are solid:

- Sprint into slide.
- Slide into crouch.
- Jump while moving and while aiming.
- ADS while jumping.
- Shoot after slide without lockout.
- Camera remains clear in crouch/slide/aim.
- Character does not stand under low ceilings.

## Camera And Aim Spec Agents Need

The camera docs should define:

- Exploration camera distance, shoulder offset, pitch limits, smoothing, and collision radius.
- Aim camera distance, shoulder offset, pitch limits, smoothing, and reticle behavior.
- Sprint camera behavior: optional FOV kick, slight distance change, or shake, but only if it improves readability.
- Slide camera behavior: lower target, optional FOV kick, no nausea-inducing shake.
- Shoulder swap rules.
- Occlusion rules for walls and the player character.
- How the crosshair target is found.
- How the muzzle aim is reconciled with the camera aim.

Third-person shot-origin contract:

```text
1. Raycast from the center of the screen to find the intended aim point.
2. Validate line of sight from the weapon muzzle or hand socket to that aim point.
3. If the muzzle is blocked, hit the blocker instead of shooting through cover.
4. Hitscan weapons resolve immediately along the final shot ray.
5. Projectile weapons spawn at the muzzle and travel toward the resolved aim point.
6. Reticle feedback must show when the muzzle is blocked or target is out of range.
```

## Gunplay Spec Agents Need

Create weapon definitions as data. A future `WeaponDefinition` should include:

```text
Weapon id / display name
Weapon class: rifle, SMG, shotgun, pistol, sniper, explosive, ability
Fire mode: semi, burst, auto, charge
Shot model: hitscan or projectile
Damage body/head
Damage falloff range
Fire rate
Magazine size
Reload time
Reload interrupt rules
Equip time
ADS time
Hip spread
ADS spread
Spread recovery
Recoil pattern or procedural recoil values
Camera recoil
Reticle bloom behavior
Projectile speed / gravity, if projectile
Aim movement multiplier
Shooting movement multiplier
Jump/slide/crouch accuracy modifiers
Impact VFX
Muzzle VFX
Audio events
Hitmarker type
Rarity/scaling hooks, if needed later
```

Prototype weapon order:

1. Hitscan assault rifle: deterministic recoil, modest hip spread, clean ADS.
2. Shotgun: pellet spread, strong hit feedback, clear range falloff.
3. SMG: close-range tracking, higher fire rate, faster spread growth.
4. DMR/sniper: projectile or precise hitscan, long ADS time, strong readable shot cadence.

Gunplay policy for the first polished pass:

- Prefer recoil over heavy random ADS bloom for aimed automatic weapons.
- Keep hip fire spread visible and understandable through reticle bloom.
- ADS should tighten aim, slow movement, and make recoil the main skill check.
- Sliding and jumping can allow shooting, but should apply clear recoil/spread penalties.
- Do not create a state where the player cannot shoot after sliding unless the design explicitly requires it.
- Damage feedback must be immediate: hitmarker, impact sound, target reaction, and optional damage number.
- Every weapon must work against a simple target dummy before enemy AI work begins.

## Aim Assist Policy

Aim assist should be designed deliberately, not accidentally.

Separate these systems:

- Reticle slowdown / target friction for controller.
- Bullet magnetism for near-misses.
- Soft lock-on for accessibility or specific modes.
- Target prioritization when multiple targets overlap.

For mouse and keyboard, default to no assist. For controller, start with light target friction and no hard lock. Any assist must be tunable by distance, angle, target visibility, input device, and weapon class.

## Animation Spec Agents Need

The existing animation docs should eventually expand beyond simple locomotion clips into shooter layers:

- Lower-body locomotion blend tree.
- Aim strafe blend tree.
- Upper-body additive aim offsets.
- Weapon-hand IK.
- Left-hand support IK.
- Recoil additive.
- Reload animations by weapon class.
- Fire animations by weapon class.
- Sprint with weapon carried or lowered.
- Slide with weapon retained.
- Jump/fall/land with aim overlay.
- Hit reactions.

Do not require final animation quality before weapon prototypes exist. Use placeholders, but keep the state and layer names production-shaped.

## Agent Definition Of Done

Every movement or gunplay agent task should end with:

- Files changed.
- Scene or prefab objects changed.
- Serialized values changed.
- New input actions, if any.
- New Animator parameters, if any.
- Manual test path.
- Known problems.
- What to tune next.

Minimum manual test report:

```text
Movement tested:
Camera tested:
Weapon tested:
Target dummy tested:
Animation state tested:
Regression check:
```

Agents must not:

- Move gameplay control to the humanoid mesh.
- Parent the camera to an animated bone.
- Enable root motion without rewriting the movement architecture docs first.
- Bulk-wire unverified animation clips.
- Add advanced traversal before the rifle/camera/reticle loop is reliable.
- Hide tuning values as private magic numbers without serialized fields or data assets.

## Recommended Doc Backlog

Add these docs as the project grows:

- `Docs/MOVEMENT_FEEL_SPEC.md`
- `Docs/CAMERA_AIM_SPEC.md`
- `Docs/GUNPLAY_SPEC.md`
- `Docs/WEAPON_DATA_SCHEMA.md`
- `Docs/PLAYTEST_CHECKLIST.md`
- `Docs/TUNING_LOG.md`

For now, this brief is the source of truth for those topics.

## Recommended Build Order

1. Movement feel pass: acceleration, deceleration, jump buffer, coyote time, slide polish, crouch/stand reliability.
2. Camera/reticle pass: shoulder aim, crosshair target ray, muzzle obstruction, shoulder swap, occlusion polish.
3. Weapon core: data-driven hitscan rifle, target dummy, damage events, reload, fire cadence.
4. Feedback pass: hitmarkers, damage numbers, muzzle/impact VFX, audio, camera recoil.
5. Animation integration: aim offsets, fire/reload placeholders, weapon socket, IK placeholders.
6. Weapon roster: shotgun, SMG, DMR/sniper.
7. Traversal expansion: mantle, ledge jump, traversal item prototype.
8. Advanced systems: controller aim assist, weapon rarity/mods, enemy AI, network-safe authority.

