# Roadmap

## Done

- Project moved to `C:\Game Dev\Unity Game 3rd person`.
- Git repo initialized and pushed.
- Third-person movement works.
- Camera follow/aim works.
- Camera sensitivity lowered.
- Aim is hold right click.
- Phase 2 animation scaffolding exists.
- Unity MCP and Codex are connected.
- World-class third-person shooter agent brief added in `Docs/WORLD_CLASS_TPS_AGENT_BRIEF.md`.
- Nightfall animation sandbox scene and sandbox Animator Controller added.
- Live Player animation bridge made parameter-driven by default to prevent unverified clips from hijacking movement.
- Movement feel V1 added: acceleration/deceleration, coyote time, jump buffer, slide buffer, and state-specific turn speeds.
- Camera/aim V1 added: shoulder swap, aim ray, FOV states, recoil, and muzzle-blocked feedback hook.
- Gunplay V1 added: data-driven prototype rifle, ammo/reload, hitscan, spread/recoil, target dummies, and reticle HUD.
- Test gym builder added under `Tools > TPS > Create Test Gym Scene`.

## Next

1. Use `Docs/HERO_CHARACTER_PIPELINE.md` as the source of truth for the playable hero pipeline.
2. Test one idle clip in `Assets/Scenes/AnimationSandbox_Nightfall.unity`.
3. Test one walk clip, then one run/jog clip, then one jump clip.
4. Promote only verified clips into `Assets/Animations/PlayerHumanoid.controller`.
5. Enable the live Player Animator only after idle/walk/run/jump are stable in the sandbox.
6. Test movement, sprint, jump, aim, crouch, slide, and animation parameter updates.
7. Generate `Assets/Scenes/TPS_TestGym.unity` from the Unity menu and run `Docs/PLAYTEST_CHECKLIST.md`.

## After Character Locomotion

1. Tune movement/camera/rifle values in the test gym.
2. Replace runtime bootstrap with explicit prefab wiring once scene setup stabilizes.
3. Add authored muzzle flash and weapon audio.
4. Add combat placeholder animation.
5. Add weapon or hand socket on the real humanoid.
6. Add upper-body aim/fire/reload animation layer.
7. Add combat state/cooldowns.

## Later

- Better camera collision tuning.
- Controller aim assist / target friction if needed.
- Shoulder swap.
- Mantle and ledge traversal after core shooter movement is stable.
- Weapon roster: rifle, shotgun, SMG, DMR/sniper.
- Ability system.
- Health/damage system.
- Enemy AI.
- UI.
- Save/load.
- Real environment art pass.
