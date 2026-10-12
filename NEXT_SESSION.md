# NEXT SESSION — Safe Travelers Club (read me first)

## What exists (all committed, repo = C:/Users/dongt/Documents/CUTR-K12-Safety-Game)

- **Web app (working, E2E-verified)**: login/progress/badges site. Frontend `frontend/` (Vite:5173), backend `backend/` (FastAPI:8000, SQLite).
  - Run: `cd backend && C:/Users/dongt/AppData/Local/Python/bin/python.exe -m uvicorn main:app --port 8000`
  - Run: `cd frontend && npm run dev`
  - Tests: `tests/e2e_phase1.py` (needs fresh DB) then restart backend then `tests/e2e_phase2.py`.
- **Unity 2D project** `unity/`: top-down SafeStreets scene (grass/road/zebra/4 buildings), kid player (Rigidbody2D + PlayerController2D, WASD), ortho follow cam. Movement play-mode-verified via CLI.
- **Toolchain installed**: Unity 6000.3.25f1 + WebGL module, Unity CLI (`unity`), MCP registered, Unity skills plugin. Setup traps documented in shared skill `unity-vibecoding-setup`.

## How to drive Unity without clicking (the pattern that works)

```
cd unity
unity status                       # Editor must be open + "ready" (port 7800)
unity recompile --project-path .   # after editing .cs files; empty output = clean
unity command eval 'return SafeStreets.EditorTools.SceneGeo.Build();' --project-path .
unity command editor_play / editor_stop / editor_focus --project-path .
unity command console_status --project-path .   # groundTruth.consoleErrors is the truth
unity command get_scene_hierarchy --project-path .
```

- Frozen frameCount in play mode = Editor minimized/unfocused. Fix: click the Unity window, and/or
  `eval 'UnityEngine.Application.runInBackground = true;'`. Set PlayerSettings.runInBackground BEFORE entering play.
- Claude Code gateway (electronhub) was down → drove Unity via `unity command eval` C# instead. Works great.

## Unity CLI deep reference (session-3 findings — read before driving the Editor)

- **The plugin ships 32 Unity skills** at
  `C:\Users\dongt\.claude\plugins\cache\unity-agent-plugin\unity\0.1.8-beta\skills\`
  (2d-pixel-perfect, create-2d-physics, ui-ugui, sprite-editor, optimize-text-mesh-pro,
  manage-sprite-atlas, ...). The project-local copy of the deep skills lives in
  `unity/.claude/skills/` (unity-cli + unity-pipeline). READ the relevant one before
  building that feature — they encode Unity-version-specific API rules.
- **The frozen-play-mode mystery is SOLVED officially**: `unity command set_autotick`
  forces Editor ticks while unfocused (16ms interval). Call ONCE per Editor session
  before `editor_play` (persist=true default survives domain reloads). Our Win32
  focus hack is obsolete. `Application.runInBackground` is the second switch.
- **`unity command wait_for`** is server-side proof of life:
  `wait_for -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' --timeout_s 10`
  → `met:true, framesObserved>0` = live game; `timedOut:true` = frozen. Replaces
  double-polling frameCount by hand.
- **`capture_game_view -- --source screen --save_path <project-relative path>`** saves
  to a file instead of inlining base64. `--source screen` includes overlay UI.
- **eval rules (from the official skill)**: eval compiles a STATEMENT BLOCK — no `using`
  directives (compiler reads them as disposal statements, CS0210), types must be fully
  qualified (bare `Object` is ambiguous). `return` value arrives in `data.result.result`.
  eval's own `timeout` is MILLISECONDS; `unity command --timeout` is SECONDS — pass
  eval-specific flags after a `--` fence.
- **`unity status --until-ready`** instead of sleep-loops after editor_play (domain reload
  drops the pipeline listener briefly).
- Verification standard (standing rule, session 3): visual/rendering claims need
  `capture_game_view` → PNG → pixel-color analysis. Transform probes are NOT proof of rendering.
- Command names differ between pipeline versions — confirm with `unity command --query <name> --format json`
  which also returns each command's full parameter schema.

## KNOWN ISSUES — status after session 6 (2026-10-12)

1. ~~Crossing detection was co-location, not traversal~~ **FIXED (session 6, spec 006,
   all 6 scenarios play-mode-verified).** New logic: side-of-ROAD tracking (shoulder y>3 /
   y<-3, NOT the stripes), corridor containment from CrossingZone (stripe-union AABB, no
   0.3 phantom margin), opposite-shoulder + player-within-2.5 completion, leash (6),
   modal gating, directional exit, deferred thank-you. See specs/006-crossing-logic/.
   Same-side in/out, quiz-visit, jaywalk-drag, and solo-crossing all now score NOTHING.
2. Player spawn resets to (0, -3.7) on every `Build()` call.
   (still open — spec 005 WS-D)

## NEW GOTCHAS (session 2)

- **Editor restart fixes a stuck play loop.** If `Time.frameCount` freezes at 1 with clean console,
  not paused, window visible: the Editor's main thread is wedged (likely stale domain reload).
  Kill Unity.exe, reopen project. `unity status` then `editor_play` again.
- `TextureImporter.spriteMeshType` doesn't exist — use `ReadTextureSettings()/SetTextureSettings()`.
- Importer settings are only applied when `sprite == null` otherwise — always check/enforce settings
  even for existing assets.
- After an Editor restart the open scene is EMPTY (no scene restores). Open SafeStreets explicitly:
  `eval 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SafeStreets.unity");'`
- Quiz answer flow has a 2.2s feedback pause; drive clicks ≥2.5s apart when testing via CLI.

## NEXT MILESTONES (in order)

0. **Game Quality Pass — spec 005 (2026-10-12)**: `specs/005-game-quality-pass/` (spec.md +
   tasks.md). Skill-driven audit findings: mixed texture filtering (28 bilinear / 12 point),
   zero audio (0 clips/sources/listener/mixer), all text = legacy TextMesh (no TMP, no Canvas),
   `_Recovery/0.unity` artifact, Build() spawn reset. Workstreams: A texture policy, B TMP
   migration (one manager per commit), C audio from zero (optimize-audio rules: findings
   reported + approval BEFORE imports), D hygiene. Two items deliberately deferred awaiting
   user call: stray-keyboard input gating (session-5 defect) and the 8 WIP files' commit intent.

1. ~~Fix visibility (sprites above) + reset player spawn~~ **DONE.**
2. ~~Interactables~~ **DONE (all 5 zones).**
3. **Content**: extend question banks for older modules (bike/e-scooter FL rules, GDL) — reuse web `content.py`.
4. **Backend hookup**: UnityWebRequest POST to FastAPI `/api/activities/{id}/submit` with the kid's
   token → score + badges appear on the web dashboard. Decide auth hand-off (web login → deep link
   with token, or in-game name+school+PIN screen). QuizUI already attempts the POST; needs a real token
   (set via `PlayerPrefs.SetString("cutr_token", ...)`) and the backend running.
5. WebGL build (module already installed) and embed/link from the web app.

## Housekeeping

- **Session 5 (feature 002 — quiz question popups, DONE + verified)**: two question kinds —
  TrueFalse (statement → TRUE/FALSE, keys 1/2 or T/F) and MultipleChoice (2-4 options, keys 1-4,
  or click any answer bar). QuizBank ScriptableObjects at `Assets/Quiz_*.asset` (content mirrored
  from `backend/content.py`, every answer cites the K-12 Guide). 4 walk-in zones (Zebra/School/
  Park/Shop → le-crossing/sh-school-bike/st-riding/rr-florida): walk in → card opens, player
  frozen; SPACE inside also starts it; ESC or X abandons (no completion logged); zone re-arms on
  exit so re-entry restarts. Right/wrong feedback + citation, then score screen +
  `[Quiz] completed activity=<id> score=<s>/<n>` log. InteractionManager router: quiz zone is
  now priority #1, and an open quiz/dialog blocks all other interaction routing.
  Verified: recompile 0 errors, play-mode `wait_for` met, freeze proof (position identical across
  101 frames while movement input held), 5 OCR'd captures in `Assets/Captures/quiz*.png`,
  score math exact (scripted 4/5), console 0 errors. Spec: `specs/002-quiz-question-popups/`.
- **DEFECT (log only, don't auto-fix)**: during session-5 play testing, two `[Quiz] completed`
  logs (2/5, 5/5) appeared at 04:40 with no agent input, and the player had drifted from spawn
  to (2.68, 3.20). Real keyboard input (numbers/T/F/SPACE) is reaching the unfocused-but-playing
  Editor window while autotick keeps it live — a stray keypress can silently answer quizzes or
  restart them. Suspect the physical keyboard was touched; if reproducible, gate quiz keys on
  `Application.isFocused` or add an input-focus guard.
- **Session 4 (guest flow)**: Home page now has 2 buttons — "Log in or make an account" (/login)
  and "Play as a guest" (/guest → GuestPage.jsx grade-band picker → module activity). Backend
  `/api/activities/{id}/submit` now grades guests anonymously via `optional_student()` (score only,
  nothing persisted, no badges); logged-in path unchanged (best score + badges). All verified
  E2E in-browser: guest K-2 → Perfect 5/5; logged-in signup still earns badges.
- 5.1 GB installers in C:/Users/dongt/unity-setup/ can be deleted once confident.
- Backend was stopped this session; Vite/Unity may still be running — kill by port if needed
  (`netstat -ano | grep :5173` / `taskkill //PID <pid> //F`).
- Spec Kit artifacts live in specs/001-homepage-auth-progress/; new features should get specs/002-...
