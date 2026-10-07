# Data Model — 001-homepage-auth-progress

Prototype storage: single SQLite file `backend/cutr.db`. Tables map 1:1 to the
spec's Key Entities. All writes go through `backend/db.py` helpers.

## Tables

### students
| column | type | notes |
|---|---|---|
| id | INTEGER PK AUTOINCREMENT | |
| first_name | TEXT NOT NULL | typed as-is; rendered escaped |
| age | INTEGER NOT NULL | 3-22 accepted (covers K-12 reality) |
| school | TEXT NOT NULL | |
| grade | TEXT NOT NULL | one of `K,1,2,...,12` |
| pin_salt | TEXT NOT NULL | hex, 16 bytes |
| pin_hash | TEXT NOT NULL | sha256(salt + pin) hex |
| created_at | TEXT NOT NULL | ISO-8601 UTC |

UNIQUE index on (first_name, school, pin_hash) — enforces FR-03's duplicate
rule (same name + school + same PIN ⇒ "already have an account").

Grade → module mapping (FR-06): K,1,2 → little-explorers · 3,4,5 →
safety-heroes · 6,7,8 → smart-travelers · 9-12 → road-ready-teens.

### progress
| column | type | notes |
|---|---|---|
| id | INTEGER PK AUTOINCREMENT | |
| student_id | INTEGER NOT NULL REFERENCES students(id) | |
| activity_id | TEXT NOT NULL | e.g. `le-crossing` |
| best_score | INTEGER NOT NULL | 0-5 |
| completed_at | TEXT | NULL until first completion |

UNIQUE (student_id, activity_id). Best score wins on retry (FR-08, US3-3).
Total score = SUM(best_score) over the student's rows — computed, never stored.

### badges
| column | type | notes |
|---|---|---|
| id | INTEGER PK AUTOINCREMENT | |
| student_id | INTEGER NOT NULL REFERENCES students(id) | |
| code | TEXT NOT NULL | `first-steps` \| `perfect` \| `module-champ` |
| earned_at | TEXT NOT NULL | |
| seen | INTEGER NOT NULL DEFAULT 0 | 0 ⇒ show "New!" marker |

UNIQUE (student_id, code).

## Badge rules (FR-09, evaluated after every activity submit)

| code | rule |
|---|---|
| first-steps | first completed activity for this student |
| perfect | any submit with score 5 |
| module-champ | all activities of one module have best_score ≥ 1 |

## Static content (no table)

Modules and activities live in `backend/content.py`, seeded from the CUTR
proposal (module names, grade bands, topics) and the Resource Guide (question
text + answers + citations). Static in-code keeps the prototype single-file
auditable; a future content-management story can move it to tables.

Question schema: `{id, text, choices[4], correct_index, source}` where `source`
cites the Resource Guide line/section (e.g. "Guide §Grades 2-3: Crossing").
