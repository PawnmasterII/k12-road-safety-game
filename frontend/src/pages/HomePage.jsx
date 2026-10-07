import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, getToken } from '../api.js'

export default function HomePage() {
  const [modules, setModules] = useState(null)
  const [me, setMe] = useState(null)

  useEffect(() => {
    api('GET', '/modules')
      .then((r) => setModules(r.modules))
      .catch(() => setModules([]))
    if (getToken()) {
      api('GET', '/auth/me')
        .then((r) => setMe(r.student))
        .catch(() => setMe(null))
    }
  }, [])

  if (modules === null) return <div className="page"><p className="muted">Loading…</p></div>

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
            <Link className="btn btn-primary" to="/signup">Make your account</Link>
            <Link className="btn btn-ghost" to="/login">I already have one</Link>
          </div>
        )}
      </header>

      <section aria-label="Learning modules">
        <h2>Pick your adventure</h2>
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
              <Link className="btn btn-small" to={`/activity/${m.id}`}>Play a sample ▶</Link>
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
