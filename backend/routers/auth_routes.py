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


@router.post("/signup")
def signup(body: SignupBody):
    if body.grade not in VALID_GRADES:
        raise HTTPException(status_code=400, detail="Pick your grade from the list.")
    if not re.fullmatch(r"\d{4}", body.pin):
        raise HTTPException(status_code=400, detail="Your PIN needs exactly 4 numbers.")

    salt = db.new_pin_salt()
    pin_hash = auth_mod.hash_pin(body.pin, salt)

    existing = db.find_student_by_login(body.first_name, body.school)
    if existing is not None and existing["pin_hash"] == auth_mod.hash_pin(body.pin, existing["pin_salt"]):
        raise HTTPException(status_code=409, detail=DUP_MSG)

    try:
        student_id = db.create_student(
            body.first_name.strip(), body.age, body.school.strip(), body.grade, salt, pin_hash
        )
    except Exception:
        raise HTTPException(status_code=409, detail=DUP_MSG)

    row = db.get_student(student_id)
    return {"token": auth_mod.make_token(student_id), "student": student_public(row)}


@router.post("/login")
def login(body: LoginBody):
    row = db.find_student_by_login(body.first_name.strip(), body.school.strip())
    if row is None or row["pin_hash"] != auth_mod.hash_pin(body.pin, row["pin_salt"]):
        raise HTTPException(status_code=401, detail=LOGIN_FAIL_MSG)
    return {"token": auth_mod.make_token(row["id"]), "student": student_public(row)}


@router.get("/me")
def me(authorization: str | None = Header(default=None)):
    return {"student": student_public(require_student(authorization))}


@router.post("/logout")
def logout():
    return {"ok": True}
