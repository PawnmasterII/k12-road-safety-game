import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, getToken } from '../api.js'

export default function HomePage() {
  const [modules, setModules] = useState(null)
  const [loadError, setLoadError] = useState(false)
  const [me, setMe] = useState(null)

  useEffect(() => {
    api('GET', '/modules')
      .then((r) => setModules(r.modules))
      .catch(() => {
        setModules([])
        setLoadError(true)
      })
    if (getToken()) {
      api('GET', '/auth/me')
        .then((r) => setMe(r.student))
        .catch(() => setMe(null))
    }
  }, [])

  // Announce loading for screen readers instead of a bare ellipsis.
  if (modules === null) {
    return (
      <div className="page">
        <p className="muted" role="status">Loading adventures…</p>
      </div>
    )
  }

  return (
    <div className="page">
      <header className="hero">
        <h1>🚸 Safe Travelers Club</h1>
        <p className="tagline">Learn to walk, bike, and ride safely — and earn badges while you play!</p>
        {me ? (
          <div className="hero-actions">
            <Link className="btn btn-primary" to="/dashboard">Go to my home page, {me.first_name}!</Link>
          </div>
        ) : (
          <div className="hero-actions">
            <Link className="btn btn-primary" to="/login">Log in or make an account</Link>
            <Link className="btn btn-ghost" to="/guest">Play as a guest</Link>
          </div>
        )}
      </header>

      <section aria-labelledby="adventures-heading">
        <h2 id="adventures-heading">Pick your adventure</h2>
        {/* A dead API used to render an empty grid with no explanation — the
            worst possible state during a live demo. */}
        {loadError && (
          <p className="form-error" role="alert">
            We could not load the adventures. Check that the game server is running, then refresh.
          </p>
        )}
        <div className="module-grid">
          {modules.map((m) => (
            <article key={m.id} className={`card module-card ${m.my_module ? 'my-module' : ''}`}>
              {m.my_module && <span className="pill">Your module!</span>}
              <h3>{m.name}</h3>
              <p className="band">Grades {m.grade_band}</p>
              <p className="blurb">{m.blurb}</p>
              <ul className="topics">
                {m.topics.map((t) => <li key={t}>{t}</li>)}
              </ul>
              <Link
                className="btn btn-small"
                to={`/activity/${m.id}`}
                aria-label={`Play a sample of ${m.name}, grades ${m.grade_band}`}
              >
                Play a sample ▶
              </Link>
            </article>
          ))}
        </div>
      </section>

      <footer className="muted small">
        A CUTR / University of South Florida project · Game-Based Transportation Safety Education for K-12 Students
      </footer>
    </div>
  )
}
