# Plan: feature 002 (quiz popups)

- Data: `QuizData.cs` — `QuestionKind` enum, `QuizQuestion` [Serializable],
  `QuizBank` ScriptableObject (activityId, title, questions).
- UI/flow: `QuizManager.cs` — singleton like DialogManager; builds a centered
  FullRect-sprite panel at Awake (sorting 55+, above the world);
  `StartQuiz(bank, source, onDone)` freezes the player; each answer row is a
  clickable bar + TextMesh (`QuizChoiceButton` OnMouseDown) with number-key
  shortcuts; feedback coroutine uses live-tunable `feedbackSeconds`.
- Zones: `QuizZone.cs` — trigger collider, auto-start on player enter,
  re-arm on exit; SPACE entry routed by `InteractionManager` (priority above
  crossing/talk NPC and buildings; a quiz guard short-circuits the router).
- Scene: `SceneGeo.Build()` gains QuizManager, 4 banks written to
  `Assets/Quiz_*.asset` (content copied from backend/content.py), and 4 zones
  (translucent glow + label + trigger).
- Verification: recompile → Build → editor_play → set_autotick → wait_for →
  eval-driven answers (feedbackSeconds=0.4) → capture_game_view OCR'd via
  PowerShell WinRT → freeze check → console_status → editor_stop → commit.
