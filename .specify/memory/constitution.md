# CUTR K-12 Safety Game — Constitution

## Core Principles

### I. Prototype-first, research-grounded
This is the prototype described in Dr. Can Cui's CUTR internal grant
"Game-Based Transportation Safety Education for K-12 Students". It demonstrates
concept, user flow, and learning design — not production hardening. Every
feature traces back to the proposal or the K-12 Resource Guide; no invented
safety content.

### II. The four modules are fixed by the proposal
- K-2 "Little Explorers" — walking, crossing, school bus, parking lot
- Grades 3-5 "Safety Heroes" — school zone, bike safety, visibility, safe routes
- Grades 6-8 "Smart Travelers" — bike, e-scooter, e-bike, transit, distracted walking
- Grades 9-12 "Road Ready Teens" — teen driving, passenger safety, FL rules, crash prevention

### III. Child-appropriate by design
Audience is K-12 children. Collect the minimum: name, age, school, grade, PIN.
No email, no passwords-to-manage, no third-party trackers. UI reading level must
fit the youngest allowed users of each module. Friendly errors, no scary
security jargon.

### IV. Content comes from the CUTR materials only
Questions and scenarios derive from the K-12 Traffic Safety Education Resource
Guide (NHTSA lessons, Florida SRTS, FLHSMV GDL facts, FDOT Target Zero themes).
Florida specifics (under-16 helmet statute §316.2065, 3-foot passing rule, GDL
night hours) belong in age-appropriate older modules.

### V. Progress is real and persisted
Scores, badges, and level completion are stored server-side (SQLite for the
prototype). A returning student must see their own progress after logout/login
and after a server restart. No progress may live only in the browser.

### VI. Verification over claims
A story is done only when its acceptance scenario passes in a real browser
(Playwright) against the running app. `npm run build` must pass. No "should
work" reporting.

## Additional Constraints

- Stack for prototype: Vite + React frontend, FastAPI + SQLite backend (matches
  the OPS student skill set named in the proposal).
- Session auth via signed token; PIN hashed; no plaintext secrets in repo.
- Single-machine demo deployment; no cloud services required.

## Development Workflow

- Spec Kit flow per feature: `/speckit.specify` → `/speckit.plan` →
  `/speckit.tasks` → implement → verify in browser → commit per story.
- Each user story lands as its own commit; specs and tasks updated alongside.

## Governance

- This constitution supersedes ad-hoc decisions; amendments require updating
  this file in the same PR that violates it.
- Use the feature spec (`specs/<branch>/spec.md`) as the runtime source of truth
  for scope questions.

**Version**: 1.0.0 | **Ratified**: 2026-10-06 | **Last Amended**: 2026-10-06
