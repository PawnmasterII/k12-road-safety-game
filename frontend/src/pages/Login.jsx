import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, setToken } from '../api.js'

export default function Login() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ first_name: '', school: '', pin: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  // Functional update: chain off the LATEST state, not the render-time closure.
  // A plain spread ({...form, field}) lets a fast second field (autofill, or a
  // kid tabbing quickly) overwrite the first field with its stale copy.
  const update = (field) => (e) => setForm((prev) => ({ ...prev, [field]: e.target.value }))

  async function submit(e) {
    e.preventDefault()
    setError('')
    // noValidate is set so the friendly messages show, which means the browser
    // no longer stops empty submits — validate here or we POST blanks and the
    // kid sees a raw 422 from FastAPI.
    if (!form.first_name.trim() || !form.school.trim()) {
      setError('Please type your first name and your school.')
      return
    }
    if (!/^\d{4}$/.test(form.pin)) {
      setError('Your secret PIN is 4 numbers.')
      return
    }
    setBusy(true)
    try {
      const r = await api('POST', '/auth/login', {
        first_name: form.first_name.trim(),
        school: form.school.trim(),
        pin: form.pin,
      })
      setToken(r.token)
      navigate('/dashboard')
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page narrow">
      <h1>Welcome back! 👋</h1>
      <form onSubmit={submit} noValidate aria-labelledby="login-heading">
        <h2 id="login-heading" className="sr-only">Log in</h2>
        <label>
          First name
          <input
            name="first_name"
            autoComplete="given-name"
            value={form.first_name}
            onChange={update('first_name')}
            placeholder="Your first name"
            maxLength={40}
            aria-invalid={error ? 'true' : undefined}
          />
        </label>
        <label>
          School
          <input
            name="school"
            autoComplete="organization"
            value={form.school}
            onChange={update('school')}
            placeholder="Your school's name"
            maxLength={80}
          />
        </label>
        <label>
          Secret PIN
          <input
            type="password"
            name="pin"
            inputMode="numeric"
            pattern="\d{4}"
            autoComplete="current-password"
            value={form.pin}
            onChange={update('pin')}
            placeholder="4 numbers"
            maxLength={4}
            aria-describedby="pin-hint"
          />
          <span id="pin-hint" className="field-note">The 4 numbers you picked when you made your account.</span>
        </label>
        {error && <p className="form-error" role="alert" aria-live="assertive">{error}</p>}
        <button className="btn btn-primary" type="submit" disabled={busy}>
          {busy ? 'Checking…' : 'Log in'}
        </button>
      </form>
      <p className="muted">New here? <Link to="/signup">Make your account</Link></p>
      <p className="muted small">Forgot your PIN? Ask your teacher or a parent to help you reset it.</p>
    </div>
  )
}
