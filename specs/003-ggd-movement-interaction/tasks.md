# Spec 003 — tasks

- [x] T1. Extract GGD reference architecture from dump.cs (PlayerController, Proximity, Interactable, PlayerTriggerCollider) → spec.md
- [x] T2. Write `Proximity2D.cs` (trigger zone, player-sense-bubble filter)
- [x] T3. Extend `Interactable.cs` (proximity collection + glow fade)
- [x] T4. Extend `PlayerController2D.cs` (run threshold, TeleportTo)
- [x] T5. Rework `InteractionManager.FindCandidate` to overlap model
- [x] T6. Update `SceneGeo.Build()` (player sense trigger, building zones)
- [x] T7. Recompile clean + rebuild scene (compilationFailed:false, roots=6)
- [x] T8. Play-mode verification: frames advance (13515→13606), 0 console errors
- [x] T9. In-game behavior verified by eval readouts:
      walk into HOME zone → candidate="your home" without facing + glow (color 0.585 vs base 0.450)
      walk away → candidate=NONE, glow faded back to base
      debugMove east into PARK wall for 4s → stopped at wall face x=7.45 (would be x=16 unblocked)
      entering Quiz_School zone auto-opened quiz; router correctly prioritized it (feature-002 intact)
      proof shot: Assets/Screenshots/spec003_park_zone.png
