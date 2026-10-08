"""Progress routes: totals, best scores, badges, activity submission + grading."""
from fastapi import APIRouter, Header, HTTPException
from pydantic import BaseModel

import content
import db
from routers.auth_routes import optional_student, require_student

router = APIRouter(prefix="/api", tags=["progress"])


class SubmitBody(BaseModel):
    answers: list[int]


def _badge_rules(student_id: int, activity_id: str, score: int, num_questions: int) -> list[str]:
    """Evaluate FR-09 badge rules after a submit. Returns newly earned codes."""
    new = []
    # finishing an activity at all earns First Steps (FR-09)
    if db.award_badge(student_id, "first-steps"):
        new.append("first-steps")
    if score == num_questions:
        if db.award_badge(student_id, "perfect"):
            new.append("perfect")
    activity = content.activity_by_id(activity_id)
    module_id = activity["module_id"] if activity else None
    module_activity_ids = [m["activity_id"] for m in content.MODULES if m["id"] == module_id]
    rows = {r["activity_id"]: r["best_score"] for r in db.get_progress_rows(student_id)}
    if all(rows.get(a, 0) >= 1 for a in module_activity_ids):
        if db.award_badge(student_id, "module-champ"):
            new.append("module-champ")
    return new


@router.get("/progress")
def get_progress(authorization: str | None = Header(default=None)):
    student = require_student(authorization)
    rows = db.get_progress_rows(student["id"])
    best_by_activity = {r["activity_id"]: r["best_score"] for r in rows}
    completed = [a for a, s in best_by_activity.items() if s is not None]
    badges = [
        {"code": b["code"], "earned_at": b["earned_at"], "seen": bool(b["seen"])}
        for b in db.get_badges(student["id"])
    ]
    return {
        "total_score": sum(best_by_activity.values()),
        "best_by_activity": best_by_activity,
        "completed": completed,
        "badges": badges,
    }


@router.post("/activities/{activity_id}/submit")
def submit(activity_id: str, body: SubmitBody, authorization: str | None = Header(default=None)):
    student = optional_student(authorization)  # None = guest: grade, but save nothing
    activity = content.activity_by_id(activity_id)
    if activity is None:
        raise HTTPException(status_code=404, detail="Activity not found.")

    questions = activity["questions"]
    if len(body.answers) != len(questions):
        raise HTTPException(status_code=400, detail="Answer every question first!")

    score = sum(
        1 for q, a in zip(questions, body.answers)
        if isinstance(a, int) and 0 <= a < len(q["choices"]) and a == q["correct_index"]
    )

    if student is None:
        return {"score": score, "best_score": None, "total_score": None, "new_badges": []}

    best = db.upsert_progress(student["id"], activity_id, score)
    new_badges = _badge_rules(student["id"], activity_id, score, len(questions))
    rows = db.get_progress_rows(student["id"])
    total = sum(r["best_score"] for r in rows)

    return {"score": score, "best_score": best, "total_score": total, "new_badges": new_badges}
