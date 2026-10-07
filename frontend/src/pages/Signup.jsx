import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, setToken } from '../api.js'

const GRADES = ['K', '1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12']

export default function Signup() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ first_name: '', age: '', school: '', grade: 'K', pin: '' })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value })

  async function submit(e) {
    e.preventDefault()
    setError('')
    if (!form.first_name.trim()) return setError('Please tell us your first name!')
    if (!form.age) return setError('Please tell us your age!')
    if (!form.school.trim()) return setError('Please tell us your school!')
    if (!/^\d{4}$/.test(form.pin)) return setError('Your PIN needs exactly 4 numbers.')
    setBusy(true)
    try {
      const r = await api('POST', '/auth/signup', {
        first_name: form.first_name.trim(),
        age: Number(form.age),
        school: form.school.trim(),
        grade: form.grade,
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
      <h1>Make your account 🎒</h1>
      <p className="muted">Ask a grown-up to help with the PIN. Remember it — you will use it every time!</p>
      <form onSubmit={submit} noValidate>
        <label>
          First name
          <input value={form.first_name} onChange={update('first_name')} placeholder="Your first name" maxLength={40} />
        </label>
        <label>
          Age
          <input type="number" min={3} max={22} value={form.age} onChange={update('age')} placeholder="How old are you?" />
        </label>
        <label>
          School
          <input value={form.school} onChange={update('school')} placeholder="Your school's name" maxLength={80} />
        </label>
        <label>
          Grade
          <select value={form.grade} onChange={update('grade')}>
            {GRADES.map((g) => <option key={g} value={g}>{g === 'K' ? 'K (Kindergarten)' : `Grade ${g}`}</option>)}
          </select>
        </label>
        <label>
          Secret PIN
          <input type="password" inputMode="numeric" autoComplete="off" value={form.pin}
                 onChange={update('pin')} placeholder="4 numbers" maxLength={4} />
        </label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <button className="btn btn-primary" type="submit" disabled={busy}>
          {busy ? 'Making your account…' : 'Start playing!'}
        </button>
      </form>
      <p className="muted">Already have an account? <Link to="/login">Log in</Link></p>
    </div>
  )
}
