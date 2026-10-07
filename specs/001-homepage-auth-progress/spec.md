# Feature Specification: Homepage with Student Login, Progress, and Badge Tracking

**Feature Branch**: `001-homepage-auth-progress`

**Created**: 2026-10-06

**Status**: Implemented (E2E-verified)

**Input**: User description: "Homepage for the CUTR K-12 transportation safety game. Login functionality prompting K-12 children for name, age, school, grade; stores scores and badges; keeps progress."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A returning student sees their own progress (Priority: P1)

A student who has played before opens the site, enters their name and PIN, and
lands on their homepage showing their name, their saved scores, their badges,
and which module levels they have completed. Nothing they earned is lost between
visits or server restarts.

**Why this priority**: "Keep progress" is the core promise of this feature and
the part the proposal's pilot evaluation depends on (completion rate, knowledge
gain over time). Without persistence, everything else is a slideshow.

**Independent Test**: Register a student, log out, restart the server, log back
in, and verify identical scores/badges are shown. Delivers the "progress is
real" value even before any game content exists.

**Acceptance Scenarios**:

1. **Given** a registered student with recorded scores and badges, **When** they
   log out and log back in from a fresh browser session, **Then** the homepage
   shows the same total score, badge list, and per-module completion as before.
2. **Given** the backend process is stopped and restarted, **When** the student
   logs in again, **Then** their progress is unchanged (data survives restart).
3. **Given** a wrong PIN for an existing name, **When** login is attempted,
   **Then** a friendly "That name and PIN don't match" message appears and no
   session is created.

---

### User Story 2 - A new student joins through the homepage (Priority: P1)

A first-time visitor picks "New here? Make your account", fills in first name,
age, school, and grade, picks a 4-digit PIN, and lands on their new homepage
with their age-appropriate module highlighted.

**Why this priority**: Registration is the entry door; without it no student can
ever return. Ships together with US1 as the minimal viable auth slice.

**Independent Test**: Submit the sign-up form with valid data; verify the
homepage greets the new student by name and highlights the module matching
their grade.

**Acceptance Scenarios**:

1. **Given** the sign-up form, **When** a student submits name, age 7, school,
   grade 2, and PIN "1234", **Then** an account is created and the homepage
   shows "Welcome, [name]" with the Little Explorers (K-2) module marked
   "Your module!".
2. **Given** the sign-up form, **When** a student submits a grade of 6, **Then**
   the Smart Travelers (6-8) module is the one highlighted.
3. **Given** the sign-up form, **When** the form is submitted with a missing
   field or a PIN that is not 4 digits, **Then** a friendly inline message
   names the problem and nothing is saved.

---

### User Story 3 - Completing an activity updates score and badges (Priority: P2)

A student opens their module, answers a short set of safety questions (content
drawn from the K-12 Resource Guide), gets immediate right/wrong feedback, and
their score and any new badge appear on the homepage afterward.

**Why this priority**: Demonstrates the loop (learn → score → badge → progress)
that makes the prototype convincing to CUTR reviewers; depends on US1's
persistence but is demonstrable with one sample activity.

**Independent Test**: Log in, complete the sample activity scoring 4/5, verify
homepage total increases by 4 and the earned badge is displayed.

**Acceptance Scenarios**:

1. **Given** a logged-in student, **When** they finish the module's sample
   activity with 4 of 5 correct, **Then** the homepage total score increases by
   4 and the activity shows "Completed - 4/5".
2. **Given** a student who just earned their first badge, **When** the homepage
   loads, **Then** the badge appears in their badge shelf with a "New!" marker.
3. **Given** a student who repeats an activity and improves, **When** they view
   the homepage, **Then** the best score for that activity is kept and the total
   never counts the same activity twice.

---

### User Story 4 - Module tiles match the proposal and fit the student's age (Priority: P3)

The homepage shows the four CUTR modules (Little Explorers K-2, Safety Heroes
3-5, Smart Travelers 6-8, Road Ready Teens 9-12) as cards with the module names,
grade bands, and topics from the proposal. A student's own module is visually
highlighted; other modules stay visible and openable (older siblings, curious
kids), but the student's module is always the default focus.

**Why this priority**: Presentation and framing; the homepage works without it
but the demo benefits from matching Dr. Cui's framework exactly.

**Independent Test**: Load the homepage logged out; verify four module cards
with correct names/grade bands/topics appear, and that a logged-in grade-2
student sees Little Explorers highlighted.

**Acceptance Scenarios**:

1. **Given** the homepage, **When** anyone loads it, **Then** exactly four
   module cards appear with the proposal's module names, grade bands, and topic
   lists.
2. **Given** a logged-in student in grade 10, **When** the homepage loads,
   **Then** Road Ready Teens is highlighted as "Your module!".

---

### Edge Cases

- Duplicate first name + school: registration must still succeed — accounts are
  distinguished by name + PIN within a school (prototype scope). If a student
  re-registers with an existing name + school + PIN combination, the system
  says "This name and PIN already have an account — try logging in!" instead of
  creating a duplicate.
- Age/grade mismatch: grade is authoritative for module mapping; age is
  collected per the proposal but not used for gating.
- Student forgets PIN: prototype scope shows a friendly message telling them to
  ask their teacher/parent to reset it (no self-service reset, no email on file).
- Very long names or emoji in names: accepted as typed, stored as-is, rendered
  safely (no HTML injection anywhere).
- Session expiry after 7 days: student is returned to login with a friendly
  "Please log in again" note, no data loss.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-01**: The homepage MUST present, for logged-out visitors, a login form
  (name + PIN) and a link to the sign-up form, plus the four module cards.
- **FR-02**: Sign-up MUST collect first name, age, school name, grade (K-12),
  and a 4-digit PIN; all fields required; grade offered as a picker (K, 1-12).
- **FR-03**: Sign-up MUST create an account keyed by (name, school, PIN) and
  MUST reject a duplicate (name, school, PIN) with a friendly message.
- **FR-04**: Login MUST accept (name, school, PIN) and return a signed session
  token valid 7 days; PIN MUST be stored hashed, never plaintext.
- **FR-05**: The homepage MUST display, for the logged-in student: their name,
  their total score, their badge shelf, and per-module best scores/completion.
- **FR-06**: The system MUST expose the four CUTR modules with the proposal's
  names, grade bands, and topic lists, and highlight the module matching the
  student's grade.
- **FR-07**: The system MUST provide one playable sample activity per module,
  with questions derived from the K-12 Resource Guide and immediate per-question
  feedback.
- **FR-08**: Completing an activity MUST persist: per-activity best score,
  total score (sum of best scores), badges earned, and completion status -
  server-side.
- **FR-09**: Badge rules (prototype): First Steps (finish any activity), Perfect
  (score 5/5 on any activity), Module Champ (finish all activities in one
  module). Badges MUST appear with a "New!" marker on the first homepage visit
  after earning.
- **FR-10**: All pages MUST render safely with arbitrary user-typed text (no
  HTML/script injection).
- **FR-11**: Age MUST be collected at sign-up but MUST NOT gate module access.
- **FR-12**: Logout MUST clear the session immediately and return to the public
  homepage.

### Key Entities *(include if feature involves data)*

- **Student**: first_name, age, school, grade (K-12), pin_hash, created_at.
  Identified for login by (name, school, PIN).
- **Module**: id (little-explorers / safety-heroes / smart-travelers /
  road-ready-teens), name, grade_band, topics[], sample_activity ref.
  Static content from the proposal.
- **Activity**: module_id, title, 5 questions; each question has text, choices,
  correct index, and a source citation into the Resource Guide.
- **ProgressEntry**: student_id, activity_id, best_score, completed_at.
  One row per student per activity; best score wins on retry.
- **Badge**: student_id, code (first-steps / perfect / module-champ),
  earned_at, seen (boolean for the "New!" marker).
- **Session**: signed token containing student_id and expiry; no server-side
  session table needed for the prototype.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new student can go from landing on the homepage to seeing their
  personalized homepage in under 60 seconds (form is one screen, 5 fields).
- **SC-002**: A returning student's exact scores and badges reappear after
  logout, browser restart, AND backend restart - verified by an automated
  Playwright check.
- **SC-003**: Completing the sample activity updates total score and badges on
  the homepage without a manual page refresh needing to be more than one reload.
- **SC-004**: All four module cards render with proposal-accurate names, grade
  bands, and topics (diff-checked against the proposal table).
- **SC-005**: Automated Playwright run covering US1-US3 passes green
  (register → logout → login → activity → score/badge visible → restart →
  progress intact).

## Assumptions

- Single-school pilots first; (name, school, PIN) is sufficient identity for a
  prototype and avoids collecting emails or phone numbers from children.
- A 4-digit PIN (not a password) is age-appropriate; teachers/parents handle
  resets in person.
- One sample activity per module is enough for this prototype slice; the full
  question bank is Task 3 of the grant and out of scope here.
- Server-side storage is SQLite via FastAPI; deployed on one machine for
  demos; no cloud services.
- Mobile support: nice-to-have; desktop/tablet is the demo target.
- English-only UI for this slice (multilingual is listed as future expansion in
  the proposal).
