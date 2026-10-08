import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, setToken, clearToken } from '../api.js'

// Grade bands mirror the backend modules (content.py). Picking a band
// highlights the matching module and deep-links into its sample activity.
const BANDS = [
  { key: 'K-2', label: 'Grades K–2', emoji: '🧸', blurb: 'Little Explorers' },
  { key: '3-5', label: 'Grades 3–5', emoji: '🦸', blurb: 'Safety Heroes' },
  { key: '6-8', label: 'Grades 6–8', emoji: '🚲', blurb: 'Smart Travelers' },
  { key: '9-12', label: 'Grades 9–12', emoji: '🚗', blurb: 'Road Ready Teens' },
]

export default function GuestPage() {
  const [modules, setModules] = useState(null)
  const [band, setBand] = useState(null)

  useEffect(() => {
    api('GET', '/modules')
      .then((r) => setModules(r.modules))
      .catch(() => setModules([]))
  }, [])

  const chosen = modules && band ? modules.find((m) => m.grade_band === band) : null

  return (
    <div className="page narrow">
      <p className="muted small"><Link to="/">← Back home</Link></p>
      <h1>Play as a guest 🎈</h1>
      <p className="muted">Pick your grade to get the right adventures. Nothing is saved —
        make an account later to keep badges and scores!</p>

      {modules === null ? (
        <p className="muted">Loading…</p>
      ) : (
        <>
          <div className="guest-grid" role="group" aria-label="Pick your grade">
            {BANDS.map((b) => (
              <button
                key={b.key}
                className={`card guest-card ${band === b.key ? 'guest-picked' : ''}`}
                onClick={() => setBand(b.key)}
              >
                <span className="guest-emoji" aria-hidden="true">{b.emoji}</span>
                <span className="guest-label">{b.label}</span>
                <span className="guest-blurb">{b.blurb}</span>
              </button>
            ))}
          </div>

          {chosen && (
            <div className="hero-actions">
              <Link
                className="btn btn-primary"
                to={`/activity/${chosen.id}`}
                onClick={clearToken}
              >
                Play {chosen.name} ▶
              </Link>
            </div>
          )}
        </>
      )}
    </div>
  )
}
