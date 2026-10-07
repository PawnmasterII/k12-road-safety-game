import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, setToken } from '../api.js'

export default function Login() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ first_name: '', school: '', pin: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value })

  async function submit(e) {
    e.preventDefault()
    setError('')
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
      <form onSubmit={submit} noValidate>
        <label>
          First name
          <input value={form.first_name} onChange={update('first_name')} placeholder="Your first name" maxLength={40} />
        </label>
        <label>
          School
          <input value={form.school} onChange={update('school')} placeholder="Your school's name" maxLength={80} />
        </label>
        <label>
          Secret PIN
          <input type="password" inputMode="numeric" autoComplete="current-password" value={form.pin}
                 onChange={update('pin')} placeholder="4 numbers" maxLength={4} />
        </label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button className="btn btn-primary" type="submit" disabled={busy}>
          {busy ? 'Checking…' : 'Log in'}
        </button>
      </form>
      <p className="muted">New here? <Link to="/signup">Make your account</Link></p>
      <p className="muted small">Forgot your PIN? Ask your teacher or a parent to help you reset it.</p>
    </div>
  )
}
