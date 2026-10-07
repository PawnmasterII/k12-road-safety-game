---
description: "Task list for 001-homepage-auth-progress"
---

# Tasks: Homepage with Student Login, Progress, and Badge Tracking

**Input**: Design documents from `/specs/001-homepage-auth-progress/`

**Prerequisites**: plan.md ✅, spec.md ✅, data-model.md ✅, contracts/api.md ✅

**Tests**: Playwright acceptance checks ARE the spec's verification requirement
(constitution VI), included per story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US4)

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Spec Kit initialized, git repo, feature branch `001-homepage-auth-progress` (done during planning)
- [x] T002 Scaffold `backend/` (FastAPI + requirements.txt) and `frontend/` (Vite + React), install deps, dev servers boot, `npm run build` passes
- [x] T003 [P] `backend/db.py`: SQLite schema (students, progress, badges) per data-model.md + seed
- [x] T004 [P] `backend/content.py`: 4 modules (proposal table verbatim) + 4 sample activities × 5 questions (Resource Guide–sourced, cited)
- [x] T005 [P] `backend/auth.py`: PIN hashing (salt+sha256), HMAC session tokens (7-day)

## Phase 2: US1 + US2 — Join and return (P1)

- [x] T006 US2 `backend/routers/auth_routes.py`: POST /api/auth/signup (validation per contract, 409 duplicate rule) — verified via API contract script (409 friendly message)
- [x] T007 US1 `backend/routers/auth_routes.py`: POST /api/auth/login (401 friendly fail), GET /api/auth/me, POST /api/auth/logout — verified via API contract script
- [x] T008 US1/US2 `frontend`: Signup.jsx + Login.jsx + api.js wrapper + router; friendly inline errors (FR-02, FR-03)
- [x] T009 US1/US2 `frontend`: Dashboard.jsx minimal — greeting by name, logout button; token persisted in localStorage
- [x] T010 US1 Playwright check: register → logout → restart backend → login → same student; wrong-PIN shows friendly error (SC-002 partial)

## Phase 3: US3 — Activity loop: score + badges (P2)

- [x] T011 US3 `backend/routers/progress_routes.py`: GET /api/progress; POST /api/activities/{id}/submit with server-side grading, best-score-wins, badge rules — verified via API contract script (retake 5/5 after 4/5 totals 5, not 9)
- [x] T012 US3 `frontend` ActivityPage.jsx: question flow, immediate per-question feedback, submit → results screen
- [x] T013 US3 `frontend` Dashboard.jsx full: total score, badge shelf with "New!" markers, per-module best scores/completion
- [x] T014 US3 Playwright check: finish activity 4/5 → homepage total +4, badge shown; retake 5/5 → best updated, badge "perfect" appears (US3-1..3)

## Phase 4: US4 — Module cards match the proposal (P3)

- [x] T015 US4 `backend/routers/modules_routes.py`: GET /api/modules (+ my_module flag), GET /api/modules/{id}/activity (answers withheld)
- [x] T016 US4 `frontend` HomePage.jsx: logged-out state with login/signup + four module cards (names, grade bands, topics verbatim); highlight "Your module!" when logged in; routes to activity player
- [x] T017 US4 Playwright check: four cards with proposal-accurate text; grade-2 and grade-10 students each see their module highlighted (US4-1..2, SC-004)

## Phase 5: Final validation & polish

- [x] T018 Full `npm run build` green; console error-free during Playwright run
- [x] T019 Injection check: name with `<script>` tags renders as text everywhere (FR-10)
- [x] T020 Spec Kit artifacts updated (tasks checked off), final commit per constitution governance

## Dependencies

T003-T005 before T006-T007 · T006-T007 before T008-T009 · T008-T009 before
T010 · T011 before T012-T013 before T014 · T015 before T016 before T017 ·
T018-T020 last.
