"""Auth routes: signup, login, me, logout — contract per specs/.../contracts/api.md"""
import re
import sqlite3

from fastapi import APIRouter, Header, HTTPException
from pydantic import BaseModel, Field

import auth as auth_mod
import db

router = APIRouter(prefix="/api/auth", tags=["auth"])

VALID_GRADES = {"K"} | {str(i) for i in range(1, 13)}
DUP_MSG = "This name and PIN already have an account — try logging in!"
LOGIN_FAIL_MSG = "That name and PIN don't match. Ask your teacher if you forgot."


class SignupBody(BaseModel):
    first_name: str = Field(min_length=1, max_length=40)
    age: int = Field(ge=3, le=22)
    school: str = Field(min_length=1, max_length=80)
    grade: str
    pin: str
    # When another kid in the same school already uses this first name, the frontend
    # offers them as "Emma — 2nd grade". The kid picks THAT row instead of the PIN:
    # selecting it means "I am that Emma", so the PIN must MATCH her existing PIN.
    # Empty = new student (or the name is unique). No impersonation, no lockout.
    existing_student_id: int | None = None


class LoginBody(BaseModel):
    first_name: str = Field(min_length=1, max_length=40)
    school: str = Field(min_length=1, max_length=80)
    pin: str


def student_public(row) -> dict:
    return {
        "id": row["id"],
        "first_name": row["first_name"],
        "age": row["age"],
        "school": row["school"],
        "grade": row["grade"],
    }


def require_student(authorization: str | None) -> sqlite3.Row:
    if not authorization or not authorization.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="Please log in again.")
    token = authorization.split(" ", 1)[1].strip()
    student_id = auth_mod.verify_token(token)
    if student_id is None:
        raise HTTPException(status_code=401, detail="Please log in again.")
    row = db.get_student(student_id)
    if row is None:
        raise HTTPException(status_code=401, detail="Please log in again.")
    return row


def optional_student(authorization: str | None) -> sqlite3.Row | None:
    """Like require_student, but returns None for guests (no/bad token) instead of 401."""
    if not authorization or not authorization.lower().startswith("bearer "):
        return None
    token = authorization.split(" ", 1)[1].strip()
    student_id = auth_mod.verify_token(token)
    if student_id is None:
        return None
    return db.get_student(student_id)


@router.post("/signup")
def signup(body: SignupBody):
    if body.grade not in VALID_GRADES:
        raise HTTPException(status_code=400, detail="Pick your grade from the list.")
    if not re.fullmatch(r"\d{4}", body.pin):
        raise HTTPException(status_code=400, detail="Your PIN needs exactly 4 numbers.")

    salt = db.new_pin_salt()
    pin_hash = auth_mod.hash_pin(body.pin, salt)

    # Duplicate-name flow: if another student already uses this first name + school,
    # the signup request must say WHICH kid is signing up. Claiming an existing row
    # requires the matching PIN (same check as login). Claiming "new" while rows
    # exist is rejected so login stays unambiguous for every name.
    same_name = db.find_students_by_name(body.first_name.strip(), body.school.strip())
    if body.existing_student_id is not None:
        match = next((r for r in same_name if r["id"] == body.existing_student_id), None)
        if match is None:
            raise HTTPException(status_code=400, detail="That student doesn't exist — start fresh.")
        if match["pin_hash"] != auth_mod.hash_pin(body.pin, match["pin_salt"]):
            raise HTTPException(status_code=401, detail=LOGIN_FAIL_MSG)
        return {"token": auth_mod.make_token(match["id"]), "student": student_public(match)}
    elif same_name:
        raise HTTPException(
            status_code=409,
            detail="Someone at your school already uses this name. Choose 'That's me!' and enter your PIN.",
        )

    try:
        student_id = db.create_student(
            body.first_name.strip(), body.age, body.school.strip(), body.grade, salt, pin_hash
        )
    except Exception:
        raise HTTPException(status_code=409, detail=DUP_MSG)

    row = db.get_student(student_id)
    return {"token": auth_mod.make_token(student_id), "student": student_public(row)}


@router.get("/duplicates")
def duplicates(first_name: str, school: str):
    """Name candidates for the signup picker (id + grade only — nothing secret)."""
    rows = db.find_students_by_name(first_name.strip(), school.strip())
    return {"students": [{"id": r["id"], "grade": r["grade"]} for r in rows]}


@router.post("/login")
def login(body: LoginBody):
    key = f"{body.first_name.strip().lower()}|{body.school.strip().lower()}"
    blocked = auth_mod.login_blocked(key)
    if blocked > 0:
        mins = max(1, blocked // 60)
        raise HTTPException(
            status_code=429,
            detail=f"Too many tries. Take a break and ask your teacher for help — try again in {mins} minute(s).",
        )

    row = db.find_student_by_login(body.first_name.strip(), body.school.strip())
    if row is None or row["pin_hash"] != auth_mod.hash_pin(body.pin, row["pin_salt"]):
        auth_mod.record_failed_login(key)
        raise HTTPException(status_code=401, detail=LOGIN_FAIL_MSG)

    auth_mod.clear_failures(key)
    return {"token": auth_mod.make_token(row["id"]), "student": student_public(row)}


@router.get("/me")
def me(authorization: str | None = Header(default=None)):
    return {"student": student_public(require_student(authorization))}


@router.post("/logout")
def logout():
    return {"ok": True}
