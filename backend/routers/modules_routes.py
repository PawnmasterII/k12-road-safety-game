"""Module + activity content routes."""
from fastapi import APIRouter, Header, HTTPException

import content
from routers.auth_routes import require_student

router = APIRouter(prefix="/api", tags=["modules"])


@router.get("/modules")
def list_modules(authorization: str | None = Header(default=None)):
    my_module_id = None
    if authorization:
        try:
            student = require_student(authorization)
            m = content.module_for_grade(student["grade"])
            my_module_id = m["id"] if m else None
        except HTTPException:
            pass  # logged out or stale token -> public view

    modules = []
    for m in content.MODULES:
        modules.append({
            "id": m["id"],
            "name": m["name"],
            "grade_band": m["grade_band"],
            "topics": m["topics"],
            "blurb": m["blurb"],
            "activity_id": m["activity_id"],
            "my_module": (m["id"] == my_module_id) if my_module_id else None,
        })
    return {"modules": modules}


@router.get("/modules/{module_id}/activity")
def get_activity(module_id: str):
    module = content.module_by_id(module_id)
    if module is None:
        raise HTTPException(status_code=404, detail="Module not found.")
    activity = content.activity_by_id(module["activity_id"])
    if activity is None:
        raise HTTPException(status_code=404, detail="Activity not found.")
    return {
        "activity": {
            "id": activity["id"],
            "module_id": activity["module_id"],
            "title": activity["title"],
            "questions": [
                {"id": q["id"], "text": q["text"], "choices": q["choices"]}
                for q in activity["questions"]
            ],
        }
    }
