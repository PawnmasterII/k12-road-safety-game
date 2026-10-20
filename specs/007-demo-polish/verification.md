# Presentation readiness — verified results

Verified locally on 2026-10-20. These are executed checks, not subagent completion claims.

## Website

- `npm run build`: PASS, Vite production bundle generated.
- `npm run lint`: no errors; two warnings remain (Fast Refresh export guidance in main.jsx; synchronous state initialization effect in ActivityPage.jsx).
- `tests/e2e_phase1.py`: PASS against isolated SQLite database outside the repository, API 8011 + frontend 5174. Checked four module cards and grade bands, signup, safe literal rendering of an HTML-like name, dashboard, 4/5 score, badges, 5/5 retry, best-score-wins, and wrong-PIN rejection.
- API was actually stopped and restarted with that same isolated database, then `tests/e2e_phase2.py`: PASS; score, badges and module progress survived restart.
- `tests/demo_guest_smoke.py`: PASS; guest completed 5/5, desktop and 390px mobile homepage rendered, no horizontal overflow or JavaScript page errors.
- Screenshots: `docs/screenshots/website-home.png`, `website-guest-score.png`, `website-mobile.png`.

Website changes include visible keyboard focus/skip navigation, route focus management, responsive layout, homepage fetch failure/retry state, guarded quiz submit/retry state, and name/PIN form improvements.

## Unity

- Unity 6000.3.25f1 live Editor compile: PASS, zero compile errors/warnings.
- Rebuilt and saved City scene: `city built, roots=7, items=205 | zonesSeen=10 nullBank=0`. Two zones are scenery (pond/school zone); all eight quiz zones have valid banks.
- Fixed the underlying quiz serialization defect: `QuizBank` was a ScriptableObject declared in mismatched `QuizData.cs`; its assets contained `m_Script: {fileID: 0}` and three banks loaded null. Renamed the script with its meta GUID retained and regenerated all nine bank assets from their actual saved content through Unity's AssetDatabase. No questions were invented.
- Runtime inspection: eight quiz banks assigned, with question counts 5/3/3/3/3/3/5/5; six NPCs have persistent conversation assets (3–4 lines each).
- Officer Ada dialogue opened and froze movement; closing it and allowing the fade to finish restored movement.
- Plaza quiz opened and froze movement. Selecting the first correct answer advanced the question index to 1 and score to 1. Closing restored movement.
- Physics movement hook moved the player from spawn; hook reset and Play mode exited afterward. This checks movement execution, not a full manual traversal of every route.
- Frame count advanced during play. Final post-clear console check: zero errors, zero warnings. Earlier tool-command errors were identified as invalid diagnostic/capture requests, not gameplay exceptions.
- `ModalSafetyAudit.Run()`: PASS, four input-polling runtime scripts gated.
- Game-view capture: `unity/Assets/Screenshots/demo-quiz.png`; inspected for readability. Main question/options legible; compact header and background labels remain minor visual limitations.

## Boundaries / not claimed

The City NPCs currently provide lesson conversations; the earlier SafeStreets scene retains escort-crossing mechanics. The full escort regression matrix was not rerun. Neither a complete manual city playthrough nor a standalone/WebGL build was performed. Website and Unity are separate demonstrations; authenticated Unity-to-web score synchronization is not verified. No real student data should be used.

Unity-generated YAML contains trailing whitespace; this is serializer output rather than a code defect. Historical earlier specs remain as engineering records and are not retroactively marked complete.
