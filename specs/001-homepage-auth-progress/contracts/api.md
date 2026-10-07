# API Contract — 001-homepage-auth-progress

Base URL: `http://localhost:8000/api`. All bodies JSON. Auth endpoints return a
`token` (HMAC-signed `student_id.expiry.sig`, 7 days); clients send it as
`Authorization: Bearer <token>`. Errors: 4xx with `{"detail": "<kid-friendly
message>"}`.

## Auth

### POST /api/auth/signup
Request: `{first_name, age, school, grade, pin}`
- Validation: first_name 1-40 chars; age int 3-22; school 1-80 chars; grade in
  K,1..12; pin matches `^\d{4}$`.
- 200 → `{token, student: {id, first_name, grade, ...}}`
- 400 → field problem (e.g. "Your PIN needs exactly 4 numbers")
- 409 → duplicate (name, school, PIN): "This name and PIN already have an
  account — try logging in!"

### POST /api/auth/login
Request: `{first_name, school, pin}`
- 200 → `{token, student}`
- 401 → "That name and PIN don't match. Ask your teacher if you forgot."

### GET /api/auth/me
Header required. 200 → `{student}` · 401 → expired/invalid token ("Please log
in again" on the client).

### POST /api/auth/logout
Client-side token discard; endpoint provided for symmetry → `{ok: true}`.

## Content

### GET /api/modules
200 → `{modules: [{id, name, grade_band, topics[], activity_id, my_module:
bool|null}]}` — `my_module` true for the one matching the logged-in student's
grade, null when logged out. Serves FR-01/FR-06/US4-1.

### GET /api/modules/{id}/activity
200 → `{activity: {id, module_id, title, questions: [{id, text, choices[]}...]}}`
— correct answers and sources are NOT sent to the client.

## Progress

### GET /api/progress
200 → `{total_score, best_by_activity: {activity_id: best_score},
badges: [{code, earned_at, seen}], completed: [activity_id...]}`

### POST /api/activities/{activity_id}/submit
Request: `{answers: [int, int, int, int, int]}` (one per question, in order)
- 200 → `{score, total_score, new_badges: [{code}], best_score}`
- 404 → unknown activity id.
Server grades (client never sees answers), updates progress + badges per the
badge rules, returns the updated totals. "New!" markers clear on next
GET /api/progress (seen=1) after being reported once.
