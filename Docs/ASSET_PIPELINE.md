# Asset Pipeline

## Current Recommendation

Add the humanoid character and locomotion animations before building combat.

Reason: combat timing, hit timing, weapon placement, aim poses, and attack feel all depend on the character's scale, skeleton, orientation, and animation style.

## Meshy / AI Model Workflow

If generating models with Meshy or another AI model tool, prefer this order:

1. Generate or acquire the visual character model.
2. Import into Blender for cleanup and inspection.
3. Check scale, forward direction, origin, texture assignments, and mesh quality.
4. Rig or validate the rig.
5. Export FBX for Unity.
6. Import into Unity as Humanoid.
7. Place under `Player/CharacterVisual`.
8. Test movement and animation parameters before combat.

Useful Meshy references:

- Meshy rigging API docs: https://docs.meshy.ai/en/api/rigging
- Meshy animation library docs: https://docs.meshy.ai/en/api/animation-library
- Meshy multi-animation Blender export help: https://help.meshy.ai/en/articles/11725598-how-do-i-export-multiple-animations-into-a-single-file-for-blender
- Meshy cleanup/export notes: https://help.meshy.ai/en/articles/9991793-how-to-use-meshy

## Rigged vs Unrigged

Best case:

- Export or download a rigged humanoid model when possible.
- The skeleton should be compatible with Unity Humanoid.
- This saves a lot of setup time.
- Use Meshy's auto-rig as a first pass, not as the final authority for hero-quality hands, weapons, facial expression, or combat polish.

Acceptable fallback:

- Use an unrigged model if the shape is good.
- Rig it in Blender or another rigging tool.
- Then export an FBX with armature and mesh.

For early prototyping, use a clean pre-rigged placeholder character before spending too long perfecting the final character.

## Meshy-Specific Notes

Meshy's rigging docs currently describe the best-supported target as a textured humanoid with clearly defined limbs and body structure. They also note that rigging is not suitable for non-humanoid assets or humanoids with unclear limb/body separation, and models above 300,000 faces need reduction before rigging through their task workflow.

Meshy can return rigged FBX/GLB outputs and basic walking/running animations. Its animation library includes many preset actions, including idle, walk/run, and attacks. Meshy also supports exporting multiple added animations in one file for Blender, where they appear as separate actions in the Action Editor.

For our game, use Meshy for:

- Base character generation.
- AI texturing.
- Fast auto-rig tests.
- Fast walk/run/idle/attack previews.
- Concepting style, costume, and silhouette.

Use Blender for:

- Checking hand geometry and finger separation.
- Adding or fixing finger bones if needed.
- Weapon grip poses and hand sockets.
- Applying transforms.
- Cleaning topology.
- Verifying weights around shoulders, hips, knees, elbows, wrists, and fingers.
- Exporting a clean Unity-ready FBX.

## Blender MCP Role

Blender is useful for:

- Checking and fixing model forward direction.
- Applying transforms.
- Scaling the model to Unity-friendly size.
- Cleaning obvious mesh issues.
- Assigning or checking textures/materials.
- Rigging or re-rigging.
- Creating simple placeholder animations.
- Exporting FBX.

Blender should not replace the Unity controller architecture. The Unity `Player` root remains the gameplay object.

Current local Blender MCP setup:

- Blender add-on: Blender MCP.
- Expected bridge port: `localhost:9876`.
- The add-on has been patched locally to auto-start the bridge when Blender opens.
- If Codex cannot connect, reopen Blender and verify the `BlenderMCP` sidebar says the server is running.

Current Nightfall Vanguard Blender findings:

- Clean Unity visual export: `Assets/Art/Characters/NightfallVanguard/Exports/NightfallVanguard_ModelOnly_FullQuality_NoAnimations.fbx`.
- Source rig has 24 bones and is Unity Humanoid compatible.
- Finger bones are not present, so hands will not support polished finger animation.
- Optimized prototype mesh is about 50k triangles, but it showed holes/artifacting and is not the active visual.
- Active full-quality prototype mesh is about 197k triangles, which is heavy but visually safer until proper retopology.
- The rig child's Animator is assigned but disabled until a known-good idle clip is added.
- The merged Meshy animation file contains many actions and should not be wired to gameplay in bulk.

## Character Blueprint For This Project

Target character style:

- Stylized-realistic third-person hero, not ultra-realistic.
- Readable silhouette from behind.
- Outfit/armor included on the character for now.
- Clear hands and fingers if weapons, magic casting, climbing, or interact prompts matter.
- Boots/feet simple enough to avoid clipping through terrain.
- Hair and loose cloth kept modest until movement/combat is stable.

Base model requirements:

- Humanoid biped.
- A-pose or T-pose.
- Symmetrical neutral stance.
- Arms separated from torso.
- Legs separated enough for auto-rigging and skinning.
- Hands visible and not fused to weapons/body.
- One character per model file.
- Textured model, preferably PBR.
- Mesh target below 100k triangles before Unity import for the playable character.

Rig requirements:

- Unity Humanoid compatible.
- Hips/spine/chest/neck/head.
- Upper/lower arms and legs.
- Hands present at minimum.
- Finger bones strongly preferred for high-quality combat, aiming, gripping, and item interactions.
- No root motion required for now.

Animation package requirements:

- Idle.
- Walk forward.
- Run or jog forward.
- Sprint forward.
- Jump start.
- Falling / airborne loop.
- Landing.
- Aim idle.
- Aim forward/back/left/right strafes.
- At least one primary attack placeholder.

If Meshy's auto-rig lacks finger quality or weapon-ready hands, keep the generated rig only as a reference/prototype and fix the production rig in Blender.

## Polygon Count Targets

For a third-person Unity prototype:

- Prototype humanoid: 5k-20k triangles.
- Main playable character: 20k-60k triangles.
- Higher-quality hero character: 60k-100k triangles if the game needs it and performance allows it.
- Mobile or many enemies on screen: stay lower, often 5k-25k triangles per character.

Texture targets:

- Prototype: 1k textures are fine.
- Main character: 2k textures are a good default.
- Hero close-up character: 4k only if needed.

Rig targets:

- Keep bone count reasonable.
- Avoid excessive helper bones unless needed.
- Prefer a standard humanoid skeleton for Unity retargeting.

## Combat Timing

Do combat after locomotion visuals are stable.

Next combat phase should add:

- Basic attack input.
- Animator trigger wiring.
- Placeholder attack animation.
- Hit window events.
- Simple hit detection.
- Cooldowns or combo timing.

Do not tune final combat feel until the character scale, weapon/socket positions, and animation timing are known.
