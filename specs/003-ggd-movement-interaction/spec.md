# Spec 003 — GGD-style movement, interaction & collision

Source: Goose Goose Duck IL2CPP dump (`dump.cs`, scratch/il2cpp_demo) — classes
`PlayerController`, `PlayableEntity`, `PlayerTriggerCollider`, `Proximity`,
`Interactable`, `EntityControllerBase`. Mechanics only; **no GGD assets or
art** (this repo is school/publish-bound, original primitives stay).

## Goal

SafeStreets adopts the interaction/collision *architecture* GGD uses:
overlap zones instead of distance+cone math, a solid body collider plus a
separate larger trigger "sense" bubble on the player, and glow feedback on
interactables.

## GGD reference (what dump.cs shows)

- `PlayerController` has TWO colliders: `roomCollider` (solid, blocks walls)
  and `PlayerTriggerCollider` (trigger volume on the player that receives
  enter/exit from interaction zones).
- `Interactable` owns `Proximity[]` children; each `Proximity` is a trigger
  collider that flips `isInProximity` on `OnTriggerEnter2D/Exit2D`.
- `Interactable` has a glow `SpriteRenderer` fading in/out (`FADE_SPEED 0.05`).
- Movement: velocity-based, `IS_RUNNING_THRESHOLD 0.01` deadzone,
  `runCycle 0.25` cadence, `TeleportTo(Vector2)` helper.

## Changes

1. `Proximity2D.cs` (new) — GGD `Proximity` analog: child trigger collider
   that sets `IsInProximity` when the PLAYER'S SENSE TRIGGER overlaps
   (filters `isTrigger && attachedRigidbody.CompareTag("Player")`, so NPC/quiz
   triggers don't count and zone-vs-zone overlaps are ignored).
2. `Interactable.cs` — auto-collects child `Proximity2D` (GGD keeps a
   `proximities[]` array), exposes `IsPlayerInProximity`, and fades its sprite
   toward a highlight color while the player is inside the zone (GGD glow).
   Buildings without a zone stay non-candidates (no silent distance fallback).
3. `PlayerController2D.cs` — add GGD's `IS_RUNNING_THRESHOLD` deadzone +
   `IsRunning`, and `TeleportTo(Vector2)` (zeroes velocity; GGD-faithful).
   Existing WASD/velocity model kept (it already matches GGD's).
4. `InteractionManager.cs` — `FindCandidate` switches from distance+dot-cone
   scan to GGD overlap: candidate = nearest overlapped `Interactable` (edge
   distance tiebreak). Facing requirement dropped for interactables (GGD has
   none). Feature-002 router priority (quiz > crossing > dialog > building)
   unchanged; click-to-open unchanged.
5. `SceneGeo.Build()` — player gets a child sense trigger (circle r=0.75 vs
   body r=0.4); every building gets a `Proximity2D` child extending ~1.2-1.6
   world units beyond its walls.

## Out of scope / unchanged

- Quiz zones, CrossingNPC, DialogNPC keep their existing distance router.
- No NPC pathing, no vents/roles/kill logic (later specs if wanted).
- No GGD art/assets, ever (legal: mechanics OK, assets not).

## Acceptance

- Recompile clean; scene rebuilds; play mode advances frames.
- Walking into a building's zone: hint appears + sprite glows, WITHOUT facing it.
- Walking away: glow fades, hint disappears.
- Player cannot walk through buildings (solid body collider blocks).
- Console: zero errors in play mode.
