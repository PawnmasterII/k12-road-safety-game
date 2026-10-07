import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, clearToken, getToken } from '../api.js'

const BADGES = {
  'first-steps': { name: 'First Steps', icon: '👣' },
  perfect: { name: 'Perfect Score', icon: '🌟' },
  'module-champ': { name: 'Module Champ', icon: '🏆' },
}

const GRADE_LABEL = { K: 'K' }

export default function Dashboard() {
  const navigate = useNavigate()
  const [me, setMe] = useState(null)
  const [modules, setModules] = useState(null)
  const [progress, setProgress] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!getToken()) {
      navigate('/login')
      return
    }
    Promise.all([
      api('GET', '/auth/me'),
      api('GET', '/modules'),
      api('GET', '/progress'),
    ])
      .then(([meR, modR, progR]) => {
        setMe(meR.student)
        setModules(modR.modules)
        setProgress(progR)
      })
      .catch(() => {
        clearToken()
        navigate('/login')
      })
      .finally(() => setLoading(false))
  }, [navigate])

  function logout() {
    clearToken()
    navigate('/')
  }

  if (loading) return <div className="page"><p className="muted">Loading…</p></div>

  const completed = new Set(progress.completed)
  const isNew = (b) => !b.seen

  return (
    <div className="page">
      <header className="dash-head">
        <div>
          <h1>Hi, {me.first_name}! 👋</h1>
          <p className="muted">{GRADE_LABEL[me.grade] ?? `Grade ${me.grade}`} · {me.school}</p>
        </div>
        <button className="btn btn-ghost" onClick={logout}>Log out</button>
      </header>

      <section className="stat-row" aria-label="My progress">
        <div className="stat">
          <span className="stat-num">{progress.total_score}</span>
          <span className="stat-label">my points</span>
        </div>
        <div className="stat">
          <span className="stat-num">{progress.badges.length}</span>
          <span className="stat-label">badges earned</span>
        </div>
        <div className="stat">
          <span className="stat-num">{completed.size}</span>
          <span className="stat-label">activities done</span>
        </div>
      </section>

      <section aria-label="My badges" className="badge-section">
        <h2>My badges</h2>
        {progress.badges.length === 0 ? (
          <p className="muted">No badges yet — play an activity to earn your first one!</p>
        ) : (
          <div className="badge-shelf">
            {progress.badges.map((b) => {
              const info = BADGES[b.code] ?? { name: b.code, icon: '🎖️' }
              return (
                <div key={b.code} className={`badge ${isNew(b) ? 'badge-new' : ''}`} title={info.name}>
                  <span className="badge-icon">{info.icon}</span>
                  <span className="badge-name">{info.name}</span>
                  {isNew(b) && <span className="pill pill-new">New!</span>}
                </div>
              )
            })}
          </div>
        )}
      </section>

      <section aria-label="My modules">
        <h2>My learning adventures</h2>
        <div className="module-grid">
          {modules.map((m) => {
            const best = progress.best_by_activity[m.activity_id]
            return (
              <article key={m.id} className={`card module-card ${m.my_module ? 'my-module' : ''}`}>
                {m.my_module && <span className="pill">Your module!</span>}
                <h3>{m.name}</h3>
                <p className="band">Grades {m.grade_band}</p>
                <p className="score-line">
                  {completed.has(m.activity_id)
                    ? `Best score: ${best} / 5 ✅`
                    : 'Not tried yet — be the hero!'}
                </p>
                <Link className="btn btn-small" to={`/activity/${m.id}`}>
                  {completed.has(m.activity_id) ? 'Play again ▶' : 'Play a sample ▶'}
                </Link>
              </article>
            )
          })}
        </div>
      </section>
    </div>
  )
}
