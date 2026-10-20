# Spec 005 — Task Breakdown

Conventions: every task ends with the project verification standard (recompile clean,
console_status 0 errors, PNG captures for visual claims). One commit per task.

## 0. Preflight (blockers)

- [ ] T0.1 Review the 8 WIP files (backend auth, frontend login, DialogManager,
      InteractionManager): commit as their own WIP commit or stash. USER DECISION.
- [ ] T0.2 Delete `unity/Assets/_Recovery/0.unity` + .meta (crash artifact).
- [ ] T0.3 Confirm Editor ready + git clean before starting WS-A.

## WS-A — Visual fidelity

- [ ] T-A1 Classify all 40 textures: dimensions + pixels-per-unit -> pixel-art vs hi-res
      list. (Survey: expect the 12 point-filtered to be the pixel set.)
- [ ] T-A2 Apply per-class filter policy (pixel: Point, no mips; hi-res: Bilinear, mips ON)
      via TextureImporter in the live Editor (no .meta hand-edits). Capture player sprite
      before/after.
- [ ] T-A3 Run sprite-segment-3x3grid analysis on DialogBox + panel sprites; set 9-slice
      borders on uniform bands only; verify no edge stretching on resized panels.
- [ ] T-A4 (conditional) Only if pixel-art dominates: evaluate Pixel Perfect Camera against
      current ortho cam; adopt only if captures show real improvement. Default: skip.

## WS-B — TMP text migration (order matters: one manager per commit)

- [ ] T-B1 Import TMP Essentials; create Latin font asset (static, sampling 70-90, padding
      ratio 10%, Scale 1); add 1 dynamic fallback (atlas 512-1024, Clear Dynamic Data On
      Build ON).
- [ ] T-B2 Migrate DialogManager (nameplate + body) to TextMeshProUGUI in the existing
      panel structure. Play-mode capture + SPACE-advance check.
- [ ] T-B3 Migrate QuizManager (question text, 2-4 answer bars, score screen). Full quiz
      round via CLI clicks >=2.5s apart, capture each state.
- [ ] T-B4 Migrate WinBanner + HudAnchor. Capture.
- [ ] T-B5 Disable AutoSize on all migrated fields; final side-by-side captures.

## WS-C — Audio from zero

- [ ] T-C1 Pre-flight report (listener count -> add 1 to MainCamera, build target, output
      sample rate). REPORT FINDINGS, WAIT for user approval before importing anything
      (skill hard rule).
- [ ] T-C2 Download CC0 packs (Kenney interface + digital audio, or equivalent), curate:
      right chime, wrong buzz (gentle — K-12), UI click, dialog open/advance, task complete,
      ambient loop. WAV/OGG only.
- [ ] T-C3 Import per matrix: SFX mono; short = DecompressOnLoad; music = Streaming +
      Load In Background; Vorbis quality 0.7+. Read back importer values after reimport.
- [ ] T-C4 AudioMixer: Master -> Music, Master -> SFX (3 groups, no Master effects);
      route all sources; play/paused snapshots.
- [ ] T-C5 Wire triggers in QuizManager (right/wrong/score), DialogManager (open/advance),
      DragDropTask (complete). Play-mode verification with log markers at each trigger.
- [ ] T-C6 Final audit pass: listener count == 1, no stereo clips on 3D-blend sources,
      music Streaming confirmed in a play session.

## WS-D — Hygiene

- [ ] T-D1 Fix SceneBuilder2D.Build() to preserve live player position when a player
      already exists (known issue 3). Verify: Build() twice, position unchanged.

## Explicitly deferred (do NOT auto-fix)

- [ ] D1 Quiz/dialog keyboard input gating on Application.isFocused (stray-keypress defect
      from session 5) — schedule only on user go-ahead.
- [ ] D2 WIP file intent (see T0.1) — user's call.
