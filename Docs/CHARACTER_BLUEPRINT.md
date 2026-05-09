# Character Blueprint

## Recommendation

Use a rigged, clothed humanoid in A-pose or T-pose as the main character base.

Animations can be separate. The model itself should provide a clean mesh, textures, skeleton, and Unity Humanoid avatar.

## Why Not Start Naked?

A naked/base body is useful for a modular equipment system, but it adds work:

- Clothing has to be separate skinned meshes.
- Gear needs shared skeleton binding.
- Body parts may need hiding under armor.
- Clipping becomes a regular problem.
- The art pipeline becomes more technical earlier.

For this prototype, use a dressed/armored character that already represents the normal playable look. Add modular gear later when gameplay proves it needs it.

## Meshy Role

Use Meshy for a fast first version:

- Character concept and silhouette.
- Outfit, armor, or creature look.
- PBR-ish texture pass.
- First-pass auto-rig.
- Animation preview.

Meshy is strongest when the character has clear humanoid proportions and separated limbs. Avoid prompts that create fused arms, merged legs, hidden hands, huge capes, or complex overlapping props.

## Blender Role

Use Blender as the quality gate:

- Confirm the model faces forward correctly.
- Apply scale and transforms.
- Check topology around shoulders, hips, elbows, knees, wrists, and hands.
- Check texture/material assignments.
- Repair hands/fingers.
- Add or improve finger bones if needed.
- Add sockets/empties for future weapons or VFX anchors.
- Export a clean FBX for Unity.

## Unity Role

Unity remains the gameplay side:

- `Player` root controls movement and collision.
- `CharacterVisual` holds the visual model and Animator.
- Animator uses `PlayerHumanoid.controller`.
- Root motion stays disabled for now.
- Combat, hitboxes, and abilities attach to the controller architecture after locomotion visuals are stable.

## Style Direction

Recommended character style for this phase:

- Stylized-realistic third-person adventurer.
- Clear readable silhouette from behind.
- Medium-detail armor/clothing, not dense micro-detail.
- Strong color blocks so the character reads during movement.
- Hands visible enough for future weapon grip, casting, and interactions.
- Hair, capes, chains, and dangling cloth kept limited until animation is stable.

Good prompt language for Meshy:

```text
Stylized realistic third-person game hero character, full body, A-pose, symmetrical neutral pose, clean separated arms and legs, visible hands and fingers, medium fantasy explorer armor, leather and cloth outfit, boots, readable silhouette, PBR textures, game-ready character, no weapon in hands, no cape, no base, no background.
```

Avoid:

```text
holding sword, crossed arms, dramatic pose, cloak wrapping arms, hands in pockets, long flowing cape, seated pose, multiple characters, merged props, extreme asymmetry
```

## Quality Targets

Prototype:

- 5k-20k triangles.
- 1k-2k textures.
- Basic humanoid rig.
- No finger animation required.

Playable character target:

- 20k-60k triangles.
- 2k textures.
- Unity Humanoid compatible.
- Finger bones preferred.
- Clean in-place locomotion animations.

Higher-detail hero ceiling:

- 60k-100k triangles only if performance and camera distance justify it.
- 4k textures only for close-up hero presentation.

## Decision Rule

For the next milestone, choose a model that is:

- Rigged or easy to rig.
- Clothed/armored enough to represent the game.
- Cleanly humanoid.
- Not holding weapons.
- In A-pose or T-pose.
- Under 100k triangles.
- Good enough to test locomotion and aiming.

Do not wait for a perfect final character before testing Phase 2. A strong placeholder with the right skeleton is more valuable than a beautiful unriggable mesh.
