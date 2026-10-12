# Spec 006 — Crossing Detection Rework (traversal event, not co-location)

Status: IMPLEMENTED + R2 fixes verified 2026-10-12. See Verification log + R2 log below.
Scope: CrossingNPC.cs (mostly), small SceneGeo.cs addition. No changes to QuestManager/QuizManager.

## Judgment of current logic (CrossingNPC.InZebraZone + co-presence check)

Current rule: fire when `InZebraZone(npc) && InZebraZone(player) && dist < 2f`, where
InZebraZone = point inside any `Zebra*` sprite bounds + 0.3 margin, swept via
FindObjectsByType<SpriteRenderer> every frame.

| # | Flaw | Evidence |
|---|------|----------|
| J1 | Counts CO-PRESENCE, not CROSSING. Both entering the stripes from the SAME side and leaving the same way still scores. The lesson taught ("arrive on the far side together") is not the lesson measured. | CrossingNPC.cs:45-46 |
| J2 | The zebra QUIZ zone (Quiz_Zebra, 3.6x5.2 at origin) sits INSIDE the stripe region. Escorting any NPC and walking to the zebra quiz = instant free crossing credit + NPC yanks +8y north + thank-you dialog tries to stack on the open quiz card. | SceneGeo.cs:49,155; CrossingNPC.cs:45,110 |
| J3 | Margin 0.3 makes the safe zone BIGGER than the painted stripes: y in [2.6, 2.9] is bare road lane (traffic lane!), yet counts as "in the zebra zone". | CrossingNPC.cs:70-72 |
| J4 | Direction never validated: NPC origin side never recorded; CrossAndLeave hardcodes +8 y (north) regardless of where the pair actually is. | CrossingNPC.cs:96 |
| J5 | No corridor containment: logic can't tell stripes from anywhere else because it only ever checks inside-rect; conversely the follow AI is unconditional glue (MoveTowards, no leash, through walls), so the NPC can be dragged across the map and into the rect from anywhere. | CrossingNPC.cs:53-56 |
| J6 | Per-frame FindObjectsByType<SpriteRenderer> sweep per NPC + fragile `name.StartsWith("Zebra")` contract (any future sprite named Zebra* silently extends the safe zone). | CrossingNPC.cs:66-68 |
| J7 | NPC follow does not pause while quiz/dialog/task owns input — it glue-follows the frozen player into zones during modals (feeds J2). | CrossingNPC.cs:39-57 |

What to KEEP: zone read from scene objects (right instinct), quest decoupling via
RecordCrossing, one-shot _crossed latch, follow deadzone.

## New logic — CrossingProgress state machine (per escorting NPC)

Geometry sources (no magic numbers): Road sprite bounds give roadHalfY; stripe union AABB
(cached by new CrossingZone component) gives the corridor. Builder adds one
`CrossingZone` GameObject over the stripes (BoxCollider2D trigger, gizmo-drawn).

States: `None -> Escorting -> Crossed` (abandon returns to None).

While escorting, each Update (skipped entirely when any modal owns input — fixes J7):

1. **Leash**: player farther than leashDistance (6) -> Abandon("I got scared! Stay close!").
   NPC stops following (no more map-glue; fixes J5's leash half).
2. **Corridor containment**: NPC x outside stripe-union x-range -> Abandon("Stay on the
   stripes!") when the NPC is ON the road (|y| <= roadHalfY). Off-road wandering is fine.
3. **Side tracking**: side(pos) = +1 if pos.y > roadHalfY, -1 if < -roadHalfY, else 0 (on road).
   Escort start records startSide = side(npc).
4. **Completion** (all must hold, checked when side(npc) == -startSide):
   - NPC reached the OPPOSITE shoulder (fixes J1/J4 direction),
   - player is on the same opposite shoulder (side(player) == -startSide) — "arrive together",
   - player within escortRadius (2.5) at that moment.
   Then -> RecordCrossing (unchanged), CrossAndLeave walks NPC AWAY FROM the road along its
   current side (sign of y * 8 instead of hardcoded +8).

Jaywalking (dragging NPC across the road anywhere outside the corridor) never scores:
containment rule 2 abandons it. Walking into the stripes and back out the same side never
scores: side never flips (kills J1/J2's free credit). The 0.3 forgiveness margin is deleted;
the corridor is exactly the painted stripes (kills J3).

Tunables (public fields, live-tunable like feedbackSeconds): leashDistance=6, escortRadius=2.5.

Thank-you timing fix: CrossAndLeave waits until QuizManager AND DialogManager are both closed
before showing _thanks (defers instead of stacking; closes J2's modal-stack half).

## Tasks

- [x] T1 SceneGeo: emit CrossingZone (stripe-union sized) + keep stripe sprites as-is.
- [x] T2 CrossingZone component: cached bounds, gizmos, static accessor (reset statics on
      SubsystemRegistration — project convention).
- [x] T3 CrossingNPC: state machine per design; delete InZebraZone sweep; gate follow on
      modals; directional CrossAndLeave; deferred thank-you.
- [x] T4 Play-mode verification (project standard: captures + console 0):
      a. same-side in/out -> NO credit; b. quiz visit mid-escort -> NO credit;
      c. drag off-stripe while on road -> abandon line fires, no credit;
      d. real south->north traversal together -> credit; e. north->south likewise;
      f. player loiters south while NPC would cross alone -> NPC cannot cross (glue follows
      player), no credit.

## Verification log (2026-10-12, live play-mode via `unity command eval`)

| Scenario | Result |
|---|---|
| Geometry: corridor=[-3.95,-2.60..3.95,2.60], roadHalfY=3.0, sides -1/0/+1, old phantom-margin point (0,2.75) now outside | PASS |
| a. same-side corridor dwell + exit | helped=0, escort intact | PASS |
| b. quiz visit mid-escort | quiz opened, NPC froze (no glue-follow), helped=0, abandon+dialog clean, no stacking | PASS |
| c. NPC dragged onto road at x=-8 (off-corridor) | abandon line shown, helped=0 | PASS |
| d. real south->north traversal, pair on far shoulder together | helped=1, NPC exited north (y=7.5) | PASS |
| e. reverse north->south with second NPC | helped=2, NPC exited south (y=-7.5) | PASS |
| f. solo-crossing attempt (player 8.5 away) | NPC frozen, leash abandon fired, helped unchanged | PASS |
| Final: consoleErrors=0, compilationFailed=False | PASS |

## Deferred (user's call)
- Pedagogy beat: one-line "Stop! Look left, look right." when the pair first steps onto the
  stripes (hook for WS-C chime later).
- NPC follow through walls (MoveTowards glue) -> NavMeshAgent/RB steering is a bigger change;
  out of scope unless asked.

## R2 fixes (2026-10-12, user-reported)

| # | Report | Root cause | Fix | Verified |
|---|--------|-----------|-----|----------|
| R2.1 | Infinite dialog loop when not stepping on stripes | Abandon() opened a MODAL dialog (freezes player, SPACE to clear); each failed attempt re-armed Talk() -> intro -> abandon -> intro ... | Abandon is now a non-blocking floating toast (2.2s, auto-expires, player never frozen) + 3s re-talk cooldown; instant re-talk provably refused (duringCooldown=False, afterCooldown=True) | PASS |
| R2.2 | Hitbox/visual inconsistency | Corridor was the stripe-union AABB (solid 7.9x5.2 box incl. bare-asphalt gaps between vertical bars) | Stripes reoriented so painted band == corridor: 6 horizontal rungs 7.9x0.7 at y=-2.65+i*1.06 cover y[-3,3]; corridor=[-3.95,-3..3.95,3] exactly | PASS |
| R2.3 | Stripes wrong direction for the road | Bars ran parallel to the pedestrian path (vertical ladder); real zebra rungs run parallel to TRAFFIC, stacked across the road | Ladder orientation: rungs long in x (7.9, parallel to the 40x6 road), stacked in y across the road | PASS (6/6 rungs horizontal asserted live) |

R2 regression battery (play mode, console 0 errors): geometry + horizontal stripes asserted;
jaywalk-drag -> toast, no modal, helped unchanged; cooldown guard proven both directions;
S->N and N->S traversals score (helped 1 then 2); solo-cross + quiz-visit both hold
(NPC frozen at 8.9u while quiz owned input, no credit).

## R3 fix (2026-10-12, user-reported: loop persists on re-talk after failed crossing)

Root cause found: after an on-road abandon the NPC stayed stranded ON the road (side 0);
Talk() gated the escort on `_startSide != 0`, so the escort SILENTLY NEVER STARTED and
every SPACE re-ran the full intro dialog — a different loop than R2's, same symptom.

Fix (three parts, all verified live):
1. Escort ALWAYS starts; `_startSide == 0` means "undetermined" and is captured the first
   time the NPC reaches a shoulder mid-escort (mid-road re-talks work).
2. An abandon while on-road makes the NPC flee to the nearest shoulder (never stranded in
   traffic; next re-talk always starts sane).
3. Retry talks use a one-line "Let's try again" instead of the full intro.

Verified: fail-on-road -> toast -> flee to shoulder (y 0.5->3.0) -> re-talk opens one-line
dialog -> Escorting startSide=-1 -> full traversal completes (helped=1); mid-road re-talk
arms Escorting with startSide=0 -> auto-arms -1 when following player off the road;
quiz-visit regression holds (helped unchanged, NPC held 8.9u); console 0 errors.
