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
unity command eval 'return SafeStreets.EditorTools.SceneBuilder2D.Build();' --project-path .
unity command editor_play / editor_stop / editor_focus --project-path .
unity command console_status --project-path .   # groundTruth.consoleErrors is the truth
unity command get_scene_hierarchy --project-path .
```

- Frozen frameCount in play mode = Editor minimized/unfocused. Fix: click the Unity window, and/or
  `eval 'UnityEngine.Application.runInBackground = true;'`. Set PlayerSettings.runInBackground BEFORE entering play.
- Claude Code gateway (electronhub) was down → drove Unity via `unity command eval` C# instead. Works great.

## KNOWN ISSUES (user-reported + diagnosed)

1. **Character is INVISIBLE / sprites look wrong.** Root cause hypothesis (high confidence):
   `SceneBuilder2D.NewSprite` uses `Sprite.Create(Texture2D.whiteTexture, ...)` at EDIT time.
   Those sprites reference a runtime texture that is NOT a project asset, so they break after
   scene save/reload (pink/missing/white-on-white). FIX: either (a) create real Texture2D assets
   once (AssetDatabase.CreateAsset) and assign Sprite assets, or (b) build sprites at RUNTIME in
   Awake from `Texture2D.whiteTexture` (never serialized), or (c) drop in real sprite art
   (Kenney.nl free top-down packs are perfect for K-12). Prefer (c) for the real game, (b) as stopgap.
2. **Nothing to interact with yet** — by design; that's Milestone 3 (below).
3. Player spawned at bottom sidewalk; during CLI tests it was driven to x≈93 (outside the 36m world).
   Reset its position in the scene or in Build() — it's saved wherever it was last.

## NEXT MILESTONES (in order)

1. Fix visibility (sprites above) + reset player spawn to (0, -3.7).
2. **Interactables**: TriggerZone2D (BoxCollider2D isTrigger) at zebra + buildings; walking into one
   pauses movement and pops a question card (UI Toolkit or Canvas + TextMeshPro); answer feedback.
3. **Content**: pull questions from the CUTR Resource Guide (same bank as backend `content.py` — reuse!).
4. **Backend hookup**: UnityWebRequest POST to FastAPI `/api/activities/{id}/submit` with the kid's
   token → score + badges appear on the web dashboard. Decide auth hand-off (web login → deep link
   with token, or in-game name+school+PIN screen).
5. WebGL build (module already installed) and embed/link from the web app.

## Housekeeping

- 5.1 GB installers in C:/Users/dongt/unity-setup/ can be deleted once confident.
- Backend was stopped this session; Vite/Unity may still be running — kill by port if needed
  (`netstat -ano | grep :5173` / `taskkill //PID <pid> //F`).
- Spec Kit artifacts live in specs/001-homepage-auth-progress/; new features should get specs/002-...
