# Spec 005 — Game Quality Pass (SafeStreets)

Status: PLANNED — no code changes yet
Origin: 2026-10-12 skill-driven audit (create-2d-physics, optimize-text-mesh-pro, optimize-audio, sprite-segment-3x3grid + live scene survey via Unity CLI)

## Summary

SafeStreets is mechanically complete (movement, 5 quiz zones, dialogs, fog of war, drag-drop
task, panel fades — specs 001-004) but its presentation layer is unpolished: text renders via
legacy TextMesh, there is no audio at all, texture filtering is inconsistent, and a few
dev-hygiene defects slow every session. This spec is a QUALITY pass — no new features.

## Evidence (measured 2026-10-12, not assumed)

| # | Finding | Source |
|---|---------|--------|
| E1 | Texture filter modes mixed: 28 assets bilinear, 12 point, 40 total | grep of .meta files |
| E2 | Zero audio: 0 clips, 0 AudioSources, 0 AudioListeners, no AudioMixer | scene survey via `unity command eval` |
| E3 | All game text is legacy `TextMesh` (DialogManager, QuizManager, WinBanner, HudAnchor) | script census |
| E4 | 0 Canvas / 0 TMP objects in scene; UI is sprite-panel + TextMesh world meshes | scene survey |
| E5 | `unity/Assets/_Recovery/0.unity` crash artifact committed in Assets | filesystem |
| E6 | Player spawn resets to (0,-3.7) on every `Build()` call | NEXT_SESSION.md known issue 3 |
| E7 | 8 files of uncommitted WIP (backend auth, frontend login, DialogManager, InteractionManager) | git status |
| E8 | Physics: legacy system, 1 Rigidbody2D (velocity mover), 18 Collider2D, console 0 errors | scene survey + create-2d-physics signals table |

## Goals

- A. Visual fidelity: consistent sprite filtering policy matched to art style; crisp camera.
- B. Text/UI readability: migrate dialog + quiz text to TextMeshPro (uGUI panels), configured
    per the optimize-text-mesh-pro rules (static Latin main font, dynamic fallbacks bounded,
    padding ratio consistent, AutoSize off on locked layout).
- C. Audio feedback: right/wrong chimes, UI clicks, dialog open/close, ambient loop — imported
    and routed per optimize-audio rules from day 1 (mono SFX, correct load types, 1 listener,
    shallow mixer Master/Music/SFX).
- D. Hygiene: clean git, remove recovery artifact, fix Build() spawn reset.

## Non-goals

- New gameplay features (milestones 3-5 in NEXT_SESSION.md: content expansion, backend hookup,
  WebGL build — separate specs).
- Switching physics systems (legacy 2D is correct here; create-2d-physics discipline: no fix
  without a fault — E8 shows none).
- Localization/CJK (deferred; the TMP fallback stack built in B.2 keeps the door open).

## Deferred defects (logged, need explicit go-ahead — per standing preference)

- D1. Stray physical keyboard input reaches the unfocused play-mode Editor and can silently
  answer quizzes (NEXT_SESSION.md session-5 defect). Proposed fix: gate quiz/dialog keys on
  `Application.isFocused`. NOT scheduled — decide when scheduling tasks.
- D2. The modified WIP files (E7) are from a prior session — commit message intent is the
  user's call before any of this spec's work starts.

## Workstreams

### WS-A — Visual fidelity (skills: 2d-pixel-perfect domain, sprite-segment-3x3grid)

- A1. Classify all 40 sprites: true pixel art vs hi-res painted (by source dimensions + PPU).
- A2. Apply policy: pixel art -> Point filter + mipmaps OFF; hi-res painted -> keep Bilinear,
  mips ON. No blind uniform switch. Record per-asset decisions in the task log.
- A3. If (and only if) pixel-art route dominates: evaluate Unity Pixel Perfect Camera
  (com.unity.2d.pixel-perfect) vs current ortho setup; keep camera as-is otherwise.
- A4. 9-slice validation: run sprite-segment-3x3grid analysis on the DialogBox/panel sprites
  before setting SpriteEditor borders, so slice edges land on uniform bands (no stretched art).

### WS-B — Text migration to TMP (skill: optimize-text-mesh-pro)

- B1. Import TMP Essentials; create the Latin font asset per core rules: static, sampling
  point size 70-90, padding ratio 10%, Scale = 1, SDF16 if glyphs >= 72pt look fuzzy.
- B2. Add ONE dynamic fallback font (atlas 512-1024, `Clear Dynamic Data On Build` ON) —
  future-proofs CJK/symbols without shipping editor-baked glyphs.
- B3. Migrate DialogManager nameplate + body to TextMeshProUGUI inside the existing panel
  structure (uGUI, consistent with GGD-style panels; panels remain their own GameObjects).
- B4. Migrate QuizManager question/answer bars + WinBanner + HudAnchor the same way.
- B5. Disable AutoSize on all migrated fields once layout is locked (live counters rule).
- B6. Verify: recompile 0 errors, play-mode captures of a dialog and a quiz (PNG evidence,
  established capture_game_view standard), compare legibility before/after.

### WS-C — Audio from zero (skill: optimize-audio; routing patterns: audio-setup-mixers)

- C1. Pre-flight (per skill §1): confirm 1 AudioListener (add to MainCamera — none exists),
  active build target, output sample rate. Skill rule: report findings before any change.
- C2. Source CC0 SFX (Kenney interface/digital-audio packs or equivalent CC0) — kid-friendly
  right/wrong chime, UI click, dialog open/close, footstep loop, calm ambient music loop.
  Prefer lossless sources (WAV/OGG), never MP3 re-encodes (skill §4C lossy-source warning).
- C3. Import per skill matrix: SFX mono (forceToMono), short clips DecompressOnLoad, music
  Streaming + Load In Background, Vorbis quality 0.7+ for any compressed voice-like audio.
- C4. AudioMixer with 3 groups only (Master -> Music, Master -> SFX); no effects on Master.
  Route sources via audio-setup-mixers patterns; one snapshot set (play / paused).
- C5. Wire: QuizManager right/wrong + score jingle, DialogManager open/advance, task complete.
- C6. Verify per skill §5: re-read loadType/channels/importer values after reimport; play-mode
  audible-event check via log markers; iterate max 3 adjust-verify cycles.

### WS-D — Hygiene

- D-a. Commit or stash WIP (needs user's call, see D2).
- D-b. Delete `Assets/_Recovery/0.unity` (+ .meta) — crash artifact.
- D-c. Fix `SceneBuilder2D.Build()` to preserve the live player position when a player already
  exists (kills known issue 3: spawn reset on rebuild).

## Verification standard (project-established)

Every task closes with: `unity recompile` empty output, `console_status`
groundTruth.consoleErrors = 0, and for visual claims a capture_game_view PNG diff. Physics
untouched unless a fault is demonstrated. Audio changes report before/after importer values.

## Risks

- TMP migration touches 4 UI scripts at once -> land B3/B4 as separate commits, one manager
  per commit, console clean between each.
- Adding an AudioListener changes nothing audible until sources exist -> do C1 with C5, not alone.
- Filter-mode changes can alter perceived scale (point sampling is sharper, not bigger) ->
  A2 includes before/after captures for the player sprite specifically.
