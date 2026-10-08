"""PIN hashing + HMAC session tokens (7-day expiry) + login rate limiting."""
import hashlib
import hmac
import secrets
import threading
import time
from pathlib import Path

_SECRET_FILE = Path(__file__).parent / ".secret"
TOKEN_TTL_SECONDS = 7 * 24 * 3600

# --- PIN attempt limiter (per login key) -------------------------------------
# K-12 threat model: classmate guessing a 4-digit PIN (10^4 keyspace).
# 5 wrong tries -> 15-minute cooldown for that name+school. In-memory is fine:
# a restart clears it and each process guards its own traffic.
MAX_ATTEMPTS = 5
COOLDOWN_SECONDS = 15 * 60
_attempts: dict[str, list[float]] = {}
_lock = threading.Lock()


def login_blocked(key: str) -> int:
    """Seconds remaining in cooldown, 0 if not blocked."""
    now = time.time()
    with _lock:
        tries = [t for t in _attempts.get(key, []) if now - t < COOLDOWN_SECONDS]
        _attempts[key] = tries
        if len(tries) >= MAX_ATTEMPTS:
            return int(COOLDOWN_SECONDS - (now - tries[0])) + 1
    return 0


def record_failed_login(key: str) -> None:
    now = time.time()
    with _lock:
        tries = [t for t in _attempts.get(key, []) if now - t < COOLDOWN_SECONDS]
        tries.append(now)
        _attempts[key] = tries


def clear_failures(key: str) -> None:
    with _lock:
        _attempts.pop(key, None)


def _secret() -> bytes:
    if _SECRET_FILE.exists():
        return _SECRET_FILE.read_bytes().strip()
    s = secrets.token_hex(32).encode()
    _SECRET_FILE.write_bytes(s)
    return s


def hash_pin(pin: str, salt: str) -> str:
    return hashlib.sha256((salt + pin).encode()).hexdigest()


def make_token(student_id: int) -> str:
    expires = int(time.time()) + TOKEN_TTL_SECONDS
    payload = f"{student_id}.{expires}"
    sig = hmac.new(_secret(), payload.encode(), hashlib.sha256).hexdigest()
    return f"{payload}.{sig}"


def verify_token(token: str) -> int | None:
    """Return student_id for a valid, unexpired token, else None."""
    try:
        student_id, expires, sig = token.split(".")
        payload = f"{student_id}.{expires}"
        expected = hmac.new(_secret(), payload.encode(), hashlib.sha256).hexdigest()
        if not hmac.compare_digest(sig, expected):
            return None
        if int(expires) < time.time():
            return None
        return int(student_id)
    except (ValueError, TypeError):
        return None
