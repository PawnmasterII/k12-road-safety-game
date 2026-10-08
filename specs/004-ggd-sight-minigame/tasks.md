# Spec 004 — tasks

- [x] T1. Extract GGD reference (FogOfWar, SeeThroughMesh, TaskPanel, DraggableTaskPanel, PlayableEntity runCycle/facingRight) → spec.md
- [x] T2. `FogOfWar2D.cs` (raycast cone → vision mesh, entity LOS hiding) — self-bootstraps at play start; verts=98 @96 rays
- [x] T3. `SeeThroughWall2D.cs` (building + TextMesh label fade when player in its zone) — 1.00→~0.35 in zone, back to 1.00
- [x] T4. `TaskPanel.cs` base (CompleteAndClose/FailAndClose, onComplete, PauseForTask)
- [x] T5. `DragDropTask.cs` (drag tiles → ordered slots) + SCHOOL wiring — panel is its OWN child GameObject (never AddComponent+SetActive(false) on the scene root!)
- [x] T6. Movement finish (runCycle OnStep event, facingRight flipX) — flipX=True verified driving left
- [x] T7. InteractionManager: TaskPaused gate + TaskGame open path
- [x] T8. SceneGeo: fog component, see-through on buildings, minigame on SCHOOL
- [x] T9. Play-mode verified: fog verts=98, Ada hidden out-of-range/visible in LOS (both ways), HOME alpha fades, SCHOOL task opens→3 tiles→DebugPlaceAll→closes at 0.25s→movement resumes, 0 console errors

## Known behavior notes

- Quiz walk-in zones re-trigger on any entry (armed on exit) — teleporting INTO a
  quiz zone auto-opens its quiz; feature-002 behavior, not a spec-004 defect.
- Fog recompute is `frame % 2` (GGD recomputeMeshEveryXFrame); ray count / FOV /
  view distance are public Inspector tweakables on FogOfWar2D.
