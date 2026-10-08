# Spec: Quiz question popups (feature 002)

## Why
The game teaches K-12 traffic safety, but the current interaction popup (001)
has no learning content. Bring back quizzing — removed in decacad — as a proper
feature with TWO question types, wired into the SafeStreets scene.

## User stories
1. As a player, when I walk into a glowing quiz zone, a quiz card opens, my
   character freezes, and I answer questions one at a time.
2. TRUE/FALSE questions show a statement with two big answers (TRUE / FALSE).
3. Multiple-choice questions show a prompt with up to four labeled options.
4. Every answer gets instant right/wrong feedback plus its source citation.
5. Finishing shows my score; SPACE closes the card and unfreezes me.
6. I can also start a quiz by pressing SPACE while standing in a zone, and
   abandon any time with ESC or the X.

## Requirements
- R1 Two question kinds: `TrueFalse` (statement, boolean answer) and
  `MultipleChoice` (2-4 options, correct index). Data = `QuizBank`
  ScriptableObjects, one per web module (`le-crossing`, `sh-school-bike`,
  `st-riding`, `rr-florida`), 5 questions each (3 MC + 2 TF), prompts and
  citations from `backend/content.py` (K-12 Traffic Safety Education Guide).
- R2 Zones: 4 walk-in trigger zones placed at the zebra crossing, school,
  park, and shop; re-armed when the player leaves.
- R3 While a quiz is open the player is frozen (Paused + zero velocity) and
  SPACE is owned by the quiz — no NPC/building interactions leak in.
- R4 Input: click any answer, or keys 1-4 (MC), 1/2 or T/F (TF); ESC/X
  abandons without logging a completion.
- R5 Feedback names the correct answer + Guide citation, holds ~2.2 s
  (live-tunable), then advances. Completion logs
  `[Quiz] completed activity=<id> score=<s>/<n>`.

## Acceptance criteria
- `unity recompile` clean; `console_status` errors 0 after play.
- OCR-verified captures show: a TRUE/FALSE card, a multiple-choice card,
  right/wrong feedback with citation, and the score screen.
- Player position identical across 2 s while a quiz is open (frozen).
- Score log line shows the expected counts for the scripted answers.
