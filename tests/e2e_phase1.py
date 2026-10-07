"""E2E phase 1 (fresh DB required): US4 cards, US2 signup, US3 activity loop,
logout, wrong-PIN. Run e2e_phase2.py AFTER restarting the backend."""
import sys
from playwright.sync_api import sync_playwright, expect

FRONT = "http://localhost:5173"
NAME = "Ella<script>alert(1)</script>"
SCHOOL = "Sunshine Elementary"
PIN = "1234"

failures = []
console_errors = []


def check(label, fn):
    try:
        fn()
        print(f"  PASS  {label}")
    except Exception as e:  # noqa: BLE001
        failures.append((label, str(e).splitlines()[0] if str(e) else repr(e)))
        print(f"  FAIL  {label}: {e}")


def answer_question(page, pick_index):
    page.locator(".choice").nth(pick_index).click()
    page.wait_for_timeout(400)  # auto-advance delay


with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page()
    page.on("console", lambda m: console_errors.append(m.text) if m.type == "error" else None)
    page.on("dialog", lambda d: (failures.append(("dialog", f"JS dialog fired: {d.message}")), d.dismiss()))

    print("US4: module cards match the proposal")
    page.goto(FRONT)
    check("hero renders", lambda: expect(page.get_by_role("heading", name="Safe Travelers Club")).to_be_visible())
    for module_name in ["Little Explorers", "Safety Heroes", "Smart Travelers", "Road Ready Teens"]:
        check(f"card: {module_name}", lambda n=module_name: expect(page.locator(".module-card h3", has_text=n)).to_be_visible())
    for band in ["K-2", "3-5", "6-8", "9-12"]:
        check(f"grade band {band}", lambda b=band: expect(page.locator(".module-card .band", has_text=f"Grades {b}")).to_be_visible())

    print("US2: signup -> personalized dashboard")
    page.get_by_role("link", name="Make your account").click()
    page.get_by_placeholder("Your first name").fill(NAME)
    page.get_by_placeholder("How old are you?").fill("7")
    page.get_by_placeholder("Your school's name").fill(SCHOOL)
    page.locator("select").select_option("2")
    page.get_by_placeholder("4 numbers").fill(PIN)
    page.get_by_role("button", name="Start playing!").click()
    check("dashboard greets by name (as text, not HTML)", lambda: expect(page.get_by_role("heading", name=f"Hi, {NAME}! 👋")).to_be_visible())
    check("grade shown", lambda: expect(page.locator(".dash-head")).to_contain_text("Grade 2"))
    check("Little Explorers highlighted", lambda: expect(page.locator(".module-card.my-module h3")).to_have_text("Little Explorers"))
    check("'Your module!' pill", lambda: expect(page.locator(".module-card.my-module .pill")).to_have_text("Your module!"))
    check("zero state: points", lambda: expect(page.locator(".stat").first).to_contain_text("0"))

    print("FR-10: injection renders as text — no JS dialog fired (asserted by dialog hook)")

    print("US3: activity 4/5 -> score + badge, retake 5/5")
    page.locator(".module-card.my-module").get_by_role("link", name="Play a sample").click()
    expect(page.get_by_role("heading", name="Crossing with a Grown-Up")).to_be_visible()
    for pick in [0, 0, 0, 0, 1]:  # Q1-4 correct, Q5 wrong
        answer_question(page, pick)
    page.get_by_role("button", name="See my score!").click()
    check("score screen 4/5", lambda: expect(page.locator(".big-score")).to_have_text("4 / 5 correct"))
    check("new badge celebrated", lambda: expect(page.locator(".new-badges")).to_be_visible())
    page.get_by_role("link", name="Go home").click()
    check("total score = 4", lambda: expect(page.locator(".stat").first).to_contain_text("4"))
    check("badge shelf shows First Steps", lambda: expect(page.locator(".badge-name", has_text="First Steps")).to_be_visible())
    check("best score line", lambda: expect(page.locator(".module-card.my-module .score-line")).to_contain_text("Best score: 4 / 5"))

    page.locator(".module-card.my-module").get_by_role("link", name="Play again").click()
    for _ in range(5):
        answer_question(page, 0)
    page.get_by_role("button", name="See my score!").click()
    check("retake 5/5", lambda: expect(page.locator(".big-score")).to_have_text("5 / 5 correct"))
    page.get_by_role("link", name="Go home").click()
    check("total = 5 (best-score-wins, not 9)", lambda: expect(page.locator(".stat").first).to_contain_text("5"))
    check("Perfect Score badge", lambda: expect(page.locator(".badge-name", has_text="Perfect Score")).to_be_visible())
    check("activities done = 1 (not 2)", lambda: expect(page.locator(".stat").nth(2)).to_contain_text("1"))

    print("US1: logout -> wrong PIN rejected")
    page.get_by_role("button", name="Log out").click()
    expect(page.get_by_role("heading", name="Safe Travelers Club")).to_be_visible()
    page.get_by_role("link", name="I already have one").click()
    page.get_by_placeholder("Your first name").fill(NAME)
    page.get_by_placeholder("Your school's name").fill(SCHOOL)
    page.get_by_placeholder("4 numbers").fill("9999")
    page.get_by_role("button", name="Log in").click()
    check("wrong PIN friendly error", lambda: expect(page.locator(".form-error")).to_contain_text("don't match"))

    browser.close()

real_errors = [
    e for e in console_errors
    # intentional 401s: the wrong-PIN login check MUST produce one; browsers
    # always log failed fetches, so "Failed to load resource ... 401" is expected
    if not ("Failed to load resource" in e and "401" in e)
]
if real_errors:
    failures.append(("console", f"{len(real_errors)} console error(s): {real_errors[:2]}"))

if failures:
    print(f"\n{len(failures)} FAILURE(S):")
    for label, msg in failures:
        print(f"  - {label}: {msg}")
    sys.exit(1)
print("PHASE 1 PASSED")
