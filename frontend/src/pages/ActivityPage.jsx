import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, getToken } from '../api.js'

export default function ActivityPage() {
  const { moduleId } = useParams()
  const [activity, setActivity] = useState(null)
  const [error, setError] = useState('')
  const [answers, setAnswers] = useState({})
  const [current, setCurrent] = useState(0)
  const [result, setResult] = useState(null)
  const [needsLogin, setNeedsLogin] = useState(false)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    setNeedsLogin(!getToken())
    api('GET', `/modules/${moduleId}/activity`)
      .then((r) => setActivity(r.activity))
      .catch((e) => setError(e.message))
  }, [moduleId])

  if (error) {
    return (
      <div className="page narrow">
        <p className="form-error" role="alert">{error}</p>
        <p className="small"><Link to="/">← Back home</Link></p>
      </div>
    )
  }
  if (!activity) return <div className="page"><p className="muted" role="status">Loading your activity…</p></div>

  const questions = activity.questions
  const q = questions[current]
  const answeredAll = questions.every((qq) => answers[qq.id] !== undefined)

  async function submit() {
    if (busy) return
    setBusy(true)
    try {
      const r = await api('POST', `/activities/${activity.id}/submit`, {
        answers: questions.map((qq) => answers[qq.id]),
      })
      setResult(r)
    } catch (e) {
      if (e.status === 401) setNeedsLogin(true)
      else setError(e.message)
    } finally {
      setBusy(false)
    }
  }

  function pick(choiceIdx) {
    // Functional update so a double-tap can't write a stale answers object
    // over the previous question's pick.
    setAnswers((prev) => ({ ...prev, [q.id]: choiceIdx }))
    // brief pause so kids see their pick highlighted, then advance
    setTimeout(() => {
      if (current < questions.length - 1) setCurrent(current + 1)
    }, 250)
  }

  if (result) {
    return (
      <div className="page narrow center" role="status" aria-live="polite">
        <h1>{result.score === questions.length ? 'Perfect! 🌟' : result.score > 0 ? 'Nice work! 🎉' : 'Good try! 💪'}</h1>
        <p className="big-score">{result.score} / {questions.length} correct</p>
        <p className="muted">
          {result.score === questions.length
            ? 'You got every question right!'
            : 'You can play again to make your best score even better.'}
        </p>
        {result.new_badges.length > 0 && (
          <div className="new-badges">
            <h2>New badge{result.new_badges.length > 1 ? 's' : ''} earned!</h2>
            <p className="badge-burst">{'🎖️ '.repeat(result.new_badges.length)}</p>
            <p className="muted">Check your home page to see them shine.</p>
          </div>
        )}
        <div className="hero-actions">
          <button
            className="btn"
            onClick={() => {
              setResult(null)
              setAnswers({})
              setCurrent(0)
              window.scrollTo({ top: 0 })
            }}
          >
            Play again
          </button>
          <Link className="btn btn-primary" to={getToken() ? '/dashboard' : '/'}>Go home</Link>
        </div>
        {result.total_score !== null && (
          <p className="muted small">Score added: {result.score} point{result.score === 1 ? '' : 's'} · Total: {result.total_score}</p>
        )}
      </div>
    )
  }

  return (
    <div className="page narrow">
      <p className="muted small"><Link to="/">← Back home</Link></p>
      <h1>{activity.title}</h1>
      {needsLogin && (
        <p className="notice">
          You are playing as a guest — <Link to="/signup">make an account</Link> to save your score and badges!
        </p>
      )}
      <div className="progress-dots">
        {/* Dots are decoration; the count below is the accessible label. */}
        <span aria-hidden="true" style={{ display: 'contents' }}>
          {questions.map((qq, i) => (
            <span key={qq.id} className={`dot ${i < current ? 'dot-done' : ''} ${i === current ? 'dot-now' : ''}`}>●</span>
          ))}
        </span>
        <span className="muted small" role="status" aria-live="polite">
          Question {current + 1} of {questions.length}
        </span>
      </div>
      <div className="card question-card" aria-labelledby="question-text">
        <h2 id="question-text">{q.text}</h2>
        <div className="choices" role="group" aria-label="Answer choices">
          {q.choices.map((c, i) => (
            <button
              key={i}
              type="button"
              className={`choice ${answers[q.id] === i ? 'choice-picked' : ''}`}
              aria-pressed={answers[q.id] === i}
              onClick={() => pick(i)}
            >
              {c}
            </button>
          ))}
        </div>
      </div>
      <div className="hero-actions">
        {current > 0 && (
          <button className="btn btn-ghost" type="button" onClick={() => setCurrent(current - 1)}>← Back</button>
        )}
        <button className="btn btn-primary" type="button" disabled={!answeredAll || busy} onClick={submit}>
          {busy ? 'Checking…' : 'See my score!'}
        </button>
      </div>
      {!answeredAll && (
        <p className="muted small">Answer every question to see your score.</p>
      )}
    </div>
  )
}
