"""E2E phase 2 (run AFTER backend restart): login again, verify progress
survived the restart — SC-002. Must run against the SAME db as phase 1.

Override the frontend with E2E_FRONT — defaults to the documented dev port."""
import os
import sys
from playwright.sync_api import sync_playwright, expect

FRONT = os.environ.get("E2E_FRONT", "http://localhost:5173")
NAME = "Ella<script>alert(1)</script>"
SCHOOL = "Sunshine Elementary"
PIN = "1234"

failures = []


def check(label, fn):
    try:
        fn()
        print(f"  PASS  {label}")
    except Exception as e:  # noqa: BLE001
        failures.append((label, str(e).splitlines()[0] if str(e) else repr(e)))
        print(f"  FAIL  {label}: {e}")


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()

    page.goto(f"{FRONT}/login")
    page.get_by_placeholder("Your first name").fill(NAME)
    page.get_by_placeholder("Your school's name").fill(SCHOOL)
    page.get_by_placeholder("4 numbers").fill(PIN)
    page.get_by_role("button", name="Log in").click()

    check("dashboard after backend restart", lambda: expect(page.get_by_role("heading", name=f"Hi, {NAME}! 👋")).to_be_visible())
    check("total score survives restart", lambda: expect(page.locator(".stat").first).to_contain_text("5"))
    check("badges survive restart", lambda: expect(page.locator(".badge-name", has_text="Perfect Score")).to_be_visible())
    check("Module Champ badge survives", lambda: expect(page.locator(".badge-name", has_text="Module Champ")).to_be_visible())
    check("best score survives", lambda: expect(page.locator(".module-card.my-module .score-line")).to_contain_text("Best score: 5 / 5"))

    browser.close()

if failures:
    print(f"\n{len(failures)} FAILURE(S):")
    for label, msg in failures:
        print(f"  - {label}: {msg}")
    sys.exit(1)
print("PHASE 2 PASSED — PROGRESS SURVIVED RESTART")
