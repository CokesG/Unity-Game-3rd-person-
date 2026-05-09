# Playtest Checklist

Last updated: 2026-05-09

## Setup

Fast path:

1. Open `Assets/Scenes/SampleScene.unity`.
2. Press Play.
3. Runtime bootstrap adds the prototype rifle, reticle HUD, and simple target dummies if they are missing.

Full test gym:

1. In Unity, choose `Tools > TPS > Create Test Gym Scene`.
2. Open `Assets/Scenes/TPS_TestGym.unity`.
3. Press Play.

## Movement

- Idle to walk feels immediate.
- Walk/run blend does not snap.
- Stop does not feel icy.
- Sprint starts only while moving forward.
- Aim cancels sprint.
- Crouch toggles on C/Ctrl when grounded.
- Sprint plus crouch starts slide.
- Slide can steer but not hard-turn.
- Slide exits crouched.
- Jump buffer catches near-landing input.
- Coyote time catches just-after-ledge input.
- Space spam cannot stack jumps or keep lifting the player upward.
- Low ceiling prevents standing.

## Camera

- Exploration view frames the character from behind/shoulder.
- Aim view tightens without hiding the target.
- Shoulder swap works.
- Camera collision does not pop violently.
- Reticle remains readable during sprint, slide, and aim.
- Recoil returns to center.

## Rifle

- Left click fires.
- Holding left click fires automatic cadence.
- Right click tightens spread.
- R reloads.
- Empty firing starts reload.
- Dummies take damage and reset.
- Head hits do higher damage.
- Cover blocks shots when the muzzle is behind it.
- Reticle turns red when blocked.

## Gun Debug Overlay

- Press `F1` to show/hide the overlay.
- Reload countdown decreases in real time while reloading.
- Shots fired increments on every consumed shot.
- Registered hits increments only when a target dummy receives damage.
- World hits increments on geometry hits, including blocked muzzle shots into cover.
- Misses increment only when the hitscan ray hits nothing.
- Critical hits increment when shooting a collider named `Head_Critical`.
- Total damage matches the sum of registered target damage.
- Focus target shows current HP, current-life damage, session damage, hit count, crit count, defeats, and last damage.
- Observed RPM is close to tuned RPM during sustained automatic fire.
- Recent DPS changes while actively landing hits and decays after stopping.

## Report Format

```text
Scene:
Movement feel:
Camera feel:
Rifle feel:
Blocked muzzle test:
Best thing:
Worst thing:
Tune next:
```
