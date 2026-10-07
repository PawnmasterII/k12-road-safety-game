# CUTR K-12 Safety Game — Safe Travelers Club

Web-based transportation safety learning game prototype for K-12 students.
Built for Dr. Can Cui's CUTR internal grant "Game-Based Transportation Safety
Education for K-12 Students: A Web-Based Prototype" (USF, May 2026).

Spec-driven development: every feature traces to `specs/001-homepage-auth-progress/`
(spec → plan → data model → API contract → tasks), governed by
`.specify/memory/constitution.md`.

## Run it

Backend (port 8000):
```bash
cd backend
C:/Users/dongt/AppData/Local/Python/bin/python.exe -m pip install -r requirements.txt   # once
C:/Users/dongt/AppData/Local/Python/bin/python.exe -m uvicorn main:app --port 8000
```

Frontend (port 5173):
```bash
cd frontend
npm install        # once
npm run dev
```

Open http://localhost:5173 — SQLite DB (`backend/cutr.db`) is created on first
backend start.

## What's in this slice

- Homepage with the four proposal modules (Little Explorers K-2, Safety Heroes
  3-5, Smart Travelers 6-8, Road Ready Teens 9-12) and login/sign-up
- Sign-up collects first name, age, school, grade, 4-digit PIN (child-safe:
  no email; PIN salted+hashed; 7-day signed session)
- Personalized dashboard: total score, badge shelf, per-module best scores
- One sample activity per module (5 questions each, sourced from the K-12
  Traffic Safety Education Resource Guide, cited per question)
- Badges: First Steps, Perfect Score, Module Champ; best-score-wins retries
- Progress persists server-side — survives logout and backend restart

## Verify

```bash
# with both servers up and a FRESH database (delete backend/cutr.db*, restart backend):
C:/Users/dongt/AppData/Local/Python/bin/python.exe tests/e2e_phase1.py
# then restart ONLY the backend (keep the DB!) and run:
C:/Users/dongt/AppData/Local/Python/bin/python.exe tests/e2e_phase2.py
```

## Out of scope (per proposal: future expansion)

Full question bank (grant Task 3), teacher dashboards, multilingual UI,
mobile app, self-service PIN reset.
