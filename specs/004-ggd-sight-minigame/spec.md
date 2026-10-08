# Spec 004 — GGD sight (fog of war), wall see-through, task minigame, movement finish

Source: GGD dump.cs classes `FogOfWar`, `SeeThroughMesh`/`SeeThroughMeshHandler`,
`TaskPanel`/`DraggableTaskPanel`, `PlayableEntity` (runCycle/facingRight).
Mechanics only — no GGD assets.

## 1. FogOfWar2D.cs — what you can see

GGD faithful: N=96 rays (GGD `accuracy`), cone FOV 140° around the player's
facing (GGD `fieldOfView`), view distance 7 (GGD `baseViewDistance`), rays
blocked by wall colliders via ContactFilter2D (useTriggers=false — walls only;
quiz/NPC/sense triggers never block sight). Rebuild the vision mesh every 2
frames (GGD `recomputeMeshEveryXFrame`). Rendering, shaderless: dark overlay
quad over the map (alpha 0.8) + warm translucent vision-polygon mesh above it
(dim outside, lit inside). Entities (NPCs) without LOS (raycast blocked or
out of range/cone) are hidden — GGD hides non-visible entities. Player never
hidden. Paused state does not freeze the fog.

## 2. SeeThroughWall2D.cs — walls go transparent when you're behind/near them

GGD `SeeThroughMesh`: occluders near the player fade. Each building fades to
`SeeThroughAlpha` 0.35 while the player is inside its Proximity2D zone (spec
003 zones), building + label together, lerp at fade speed. Guarantees the
character is always visible against a wall it is standing against.

## 3. TaskPanel.cs + DragDropTask.cs — the minigame

GGD `TaskPanel` port: base class with `Action<bool> onComplete`,
`CompleteAndClose(0.25f)` / `FailAndClose(0.25f)`, closes → resumes movement.
`DragDropTask` = GGD `DraggableTaskPanel`: drag item tiles into ordered slots;
all placed correctly → complete; wrong slot → tile bounces back (kid-friendly,
no fail state; FailAndClose kept in base for faithfulness). First instance:
SCHOOL building — "crossing steps" minigame (Look Left → Look Right → Hold a
Grown-Up's Hand). InteractionManager: Interactable with a TaskGame opens the
minigame on SPACE instead of the info popup; quiz/dialog router priority
unchanged.

## 4. Movement port completion (PlayableEntity leftovers)

`runCycle` 0.25 cadence → OnStep event while IsRunning (footstep/anim hook);
`facingRight` → sprite flipX from horizontal input.

## Acceptance

- Compile clean, rebuild, play mode: frames advance, 0 console errors.
- Fog mesh exists (verts>0); NPC behind a wall from the player hidden; NPC in
  open LOS visible; moving the player flips which one is visible.
- Standing in HOME zone → building alpha ≈0.35; leaving → 1.0.
- SPACE at SCHOOL opens minigame, movement pauses; completing it (programmatic
  place-all) closes panel after 0.25s and resumes movement.
- debugMove left → player flipX=true.
