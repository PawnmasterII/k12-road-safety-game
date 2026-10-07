"""PIN hashing + HMAC session tokens (7-day expiry)."""
import hashlib
import hmac
import secrets
import time
from pathlib import Path

_SECRET_FILE = Path(__file__).parent / ".secret"
TOKEN_TTL_SECONDS = 7 * 24 * 3600


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
