# Tasks: feature 002

- [x] T1 `QuizData.cs` (question kinds, QuizQuestion, QuizBank)
- [x] T2 `QuizManager.cs` (panel, question flow, feedback, score, guards)
- [x] T3 `QuizZone.cs` + InteractionManager router priority/guards
- [x] T4 SceneGeo: banks from content.py + 4 zones + QuizManager in scene
- [x] T5 Play-mode verification (OCR captures, freeze proof, console clean)
- [x] T6 Spec/tracking files + NEXT_SESSION.md updated; committed (d93b373)

## Verification results (session 5, 2026-10-08)

- recompile: 0 errors, 0 new warnings. Play: `wait_for` met (framesObserved>0).
- Freeze proof: player position (0.000, 0.000) identical across 101 frames with
  movement input (4,0) held while the quiz card was open.
- OCR (PowerShell WinRT) on captures: MC card (4 options), TRUE/FALSE card,
  wrong-answer feedback with correct answer + Guide citation, results screen
  "you got 4 / 5. Good job!". Files: Assets/Captures/quiz1..quiz5*.png.
- Score math exact: scripted 1 wrong + 4 right = 4/5 logged.
- Walk-in auto-open, SPACE-path open, ESC/X abandon (`[Quiz] abandoned ...`
  log, no completion), and re-entry restart (zone re-arm) all exercised live.
- Console after testing: 0 errors. Known benign warnings: Input Manager
  deprecation (pre-existing), "No script asset for QuizBank/QuestData"
  (class-name≠file-name ScriptableObject quirk; assets load and run).
- DEFECT logged in NEXT_SESSION.md (unattributed quiz completions at 04:40 —
  suspected stray physical keyboard input into the playing Editor; not fixed).
