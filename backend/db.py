"""SQLite access layer — schema per specs/001-homepage-auth-progress/data-model.md"""
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

DB_PATH = Path(__file__).parent / "cutr.db"


def get_conn() -> sqlite3.Connection:
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA journal_mode=WAL")
    conn.execute("PRAGMA foreign_keys=ON")
    return conn


def init_db() -> None:
    conn = get_conn()
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS students (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            first_name TEXT NOT NULL,
            age INTEGER NOT NULL,
            school TEXT NOT NULL,
            grade TEXT NOT NULL,
            pin_salt TEXT NOT NULL,
            pin_hash TEXT NOT NULL,
            created_at TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS idx_students_login
            ON students (first_name COLLATE NOCASE, school COLLATE NOCASE, pin_hash);
        CREATE TABLE IF NOT EXISTS progress (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            student_id INTEGER NOT NULL REFERENCES students(id),
            activity_id TEXT NOT NULL,
            best_score INTEGER NOT NULL,
            completed_at TEXT,
            UNIQUE (student_id, activity_id)
        );
        CREATE TABLE IF NOT EXISTS badges (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            student_id INTEGER NOT NULL REFERENCES students(id),
            code TEXT NOT NULL,
            earned_at TEXT NOT NULL,
            seen INTEGER NOT NULL DEFAULT 0,
            UNIQUE (student_id, code)
        );
        """
    )
    conn.commit()
    conn.close()


def _now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def create_student(first_name: str, age: int, school: str, grade: str,
                   pin_salt: str, pin_hash: str) -> int:
    conn = get_conn()
    try:
        cur = conn.execute(
            "INSERT INTO students (first_name, age, school, grade, pin_salt, pin_hash, created_at)"
            " VALUES (?, ?, ?, ?, ?, ?, ?)",
            (first_name, age, school, grade, pin_salt, pin_hash, _now()),
        )
        conn.commit()
        return cur.lastrowid
    finally:
        conn.close()


def find_student_by_login(first_name: str, school: str):
    conn = get_conn()
    try:
        return conn.execute(
            "SELECT * FROM students WHERE first_name = ? COLLATE NOCASE AND school = ? COLLATE NOCASE",
            (first_name, school),
        ).fetchone()
    finally:
        conn.close()


def get_student(student_id: int):
    conn = get_conn()
    try:
        return conn.execute("SELECT * FROM students WHERE id = ?", (student_id,)).fetchone()
    finally:
        conn.close()


def upsert_progress(student_id: int, activity_id: str, score: int) -> int:
    """Best score wins. Returns the stored best score for this activity."""
    conn = get_conn()
    try:
        row = conn.execute(
            "SELECT best_score FROM progress WHERE student_id = ? AND activity_id = ?",
            (student_id, activity_id),
        ).fetchone()
        if row is None:
            conn.execute(
                "INSERT INTO progress (student_id, activity_id, best_score, completed_at)"
                " VALUES (?, ?, ?, ?)",
                (student_id, activity_id, score, _now()),
            )
            best = score
        else:
            best = max(row["best_score"], score)
            conn.execute(
                "UPDATE progress SET best_score = ?, completed_at = COALESCE(completed_at, ?)"
                " WHERE student_id = ? AND activity_id = ?",
                (best, _now(), student_id, activity_id),
            )
        conn.commit()
        return best
    finally:
        conn.close()


def get_progress_rows(student_id: int):
    conn = get_conn()
    try:
        return conn.execute(
            "SELECT activity_id, best_score FROM progress WHERE student_id = ?",
            (student_id,),
        ).fetchall()
    finally:
        conn.close()


def get_badges(student_id: int, include_seen: bool = True):
    conn = get_conn()
    try:
        q = "SELECT code, earned_at, seen FROM badges WHERE student_id = ?"
        if not include_seen:
            q += " AND seen = 0"
        return conn.execute(q, (student_id,)).fetchall()
    finally:
        conn.close()


def award_badge(student_id: int, code: str) -> bool:
    """Insert badge if new. Returns True when it is newly earned."""
    conn = get_conn()
    try:
        cur = conn.execute(
            "INSERT OR IGNORE INTO badges (student_id, code, earned_at, seen) VALUES (?, ?, ?, 0)",
            (student_id, code, _now()),
        )
        conn.commit()
        return cur.rowcount > 0
    finally:
        conn.close()


def mark_badges_seen(student_id: int) -> None:
    conn = get_conn()
    try:
        conn.execute("UPDATE badges SET seen = 1 WHERE student_id = ?", (student_id,))
        conn.commit()
    finally:
        conn.close()


def new_pin_salt() -> str:
    return uuid.uuid4().bytes.hex()
