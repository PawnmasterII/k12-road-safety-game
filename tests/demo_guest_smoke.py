"""Presentation guest flow and responsive smoke check; no database writes.
Requires playwright + Chromium and both servers. E2E_FRONT overrides the URL.
"""
import os
from pathlib import Path
from playwright.sync_api import sync_playwright, expect

front = os.environ.get('E2E_FRONT', 'http://localhost:5173')
shots = Path(__file__).resolve().parents[1] / 'docs' / 'screenshots'
shots.mkdir(parents=True, exist_ok=True)
with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page(viewport={'width': 1440, 'height': 1000})
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.goto(front)
    expect(page.get_by_role('heading', name='Safe Travelers Club')).to_be_visible()
    expect(page.locator('.module-card')).to_have_count(4)
    page.screenshot(path=str(shots / 'website-home.png'), full_page=True)
    page.get_by_role('link', name='Play as a guest').click()
    page.get_by_role('button', name='Grades K–2').click()
    page.get_by_role('link', name='Play Little Explorers').click()
    for _ in range(5):
        page.locator('.choice').first.click()
        page.wait_for_timeout(450)
    page.get_by_role('button', name='See my score!').click()
    expect(page.locator('.big-score')).to_contain_text('5 / 5')
    page.screenshot(path=str(shots / 'website-guest-score.png'), full_page=True)
    page.goto(front)
    page.set_viewport_size({'width': 390, 'height': 844})
    expect(page.locator('.module-card')).to_have_count(4)
    assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), 'Mobile horizontal overflow'
    page.screenshot(path=str(shots / 'website-mobile.png'), full_page=True)
    assert not errors, errors
    browser.close()
print('PASS: guest 5/5 completion, desktop/mobile render, no horizontal overflow or JS page errors')
