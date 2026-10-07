# Implementation Plan: Homepage with Student Login, Progress, and Badge Tracking

**Branch**: `001-homepage-auth-progress` | **Date**: 2026-10-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-homepage-auth-progress/spec.md`

## Summary

A K-12 student opens the CUTR safety-game homepage, creates an account with
(name, age, school, grade, PIN) or logs back in, and sees a personalized
homepage: their total score, badge shelf, per-module progress, and the four
CUTR modules from the proposal with their own grade band highlighted. One
playable sample activity per module lets them earn score and badges, persisted
server-side in SQLite. FastAPI backend + React (Vite) frontend.

## Technical Context

**Language/Version**: Python 3.14 (backend, user python at
`C:/Users/dongt/AppData/Local/Python/bin/python.exe`), Node 26 + Vite/React (frontend)

**Primary Dependencies**: FastAPI, uvicorn, Pydantic; React 19, Vite, react-router-dom

**Storage**: SQLite (file `backend/cutr.db`), via Python `sqlite3` stdlib (no ORM
needed for prototype scale)

**Testing**: Playwright (Python sync API) end-to-end; pytest for backend unit
checks of auth + progress logic

**Target Platform**: Single-machine demo (localhost:5173 frontend,
localhost:8000 API); Chrome/Edge desktop & tablet

**Performance Goals**: Page interactions < 200 ms local; trivial at prototype scale

**Constraints**: No cloud services; no email collection (children); PIN hashed
(sha256 + per-user salt); session token HMAC-signed, 7-day expiry

**Scale/Scope**: 1 homepage, 1 signup, 1 login, 1 activity player, 4 modules,
4 sample activities (5 questions each), 3 badge types

## Constitution Check

- I. Prototype-first: PASS — no production hardening beyond PIN hashing +
  signed sessions, which Principle III/V require.
- II. Four modules fixed: PASS — modules seeded verbatim from the proposal
  table (names, grade bands, topics).
- III. Child-appropriate: PASS — collects only name/age/school/grade/PIN;
  friendly error strings; no trackers; UI copy kept short and simple.
- IV. CUTR content only: PASS — every question cites the Resource Guide
  section it comes from.
- V. Progress persisted server-side: PASS — SQLite; Playwright restart check.
- VI. Verification: PASS — Playwright scenario run is the completion gate.

## Project Structure

### Documentation (this feature)

```text
specs/001-homepage-auth-progress/
├── spec.md              # Feature specification (done)
├── plan.md              # This file
├── data-model.md        # Entities, tables, badge rules
├── contracts/
│   └── api.md           # REST contract (endpoints, payloads, errors)
└── tasks.md             # Task list (story-organized)
```

### Source Code (repository root)

```text
backend/
├── main.py              # FastAPI app: mounts routers, serves nothing else
├── db.py                # SQLite connection, schema init, seed helpers
├── auth.py              # signup/login/session-token helpers (hashing, HMAC)
├── content.py           # Static module + activity content (from CUTR materials)
├── routers/
│   ├── auth_routes.py   # POST /api/auth/signup, /api/auth/login, GET /api/auth/me, POST /api/auth/logout
│   ├── modules_routes.py# GET /api/modules, GET /api/modules/{id}/activity
│   └── progress_routes.py # GET /api/progress, POST /api/activities/{id}/submit
├── cutr.db              # (created at first run; gitignored)
├── requirements.txt
└── tests/
    └── test_api.py      # pytest: auth + progress logic

frontend/
├── index.html
├── package.json
├── vite.config.js       # dev proxy /api -> localhost:8000
└── src/
    ├── main.jsx         # router setup
    ├── api.js           # fetch wrapper (token in localStorage)
    ├── App.jsx          # layout shell
    ├── pages/
    │   ├── HomePage.jsx     # module cards + (auth forms when logged out)
    │   ├── Dashboard.jsx    # personalized homepage (score, badges, progress)
    │   ├── Signup.jsx
    │   ├── Login.jsx
    │   └── ActivityPage.jsx # sample activity player
    └── styles.css       # single stylesheet, kid-friendly theme
```

## Implementation Approach

Story-by-story per spec priorities; each story ends with its Playwright
acceptance check green and a commit. US1+US2 share the auth slice and land
together; US3 adds the activity loop; US4 is polish on module cards.
