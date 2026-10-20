import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom'
import './styles.css'
import HomePage from './pages/HomePage.jsx'
import Signup from './pages/Signup.jsx'
import Login from './pages/Login.jsx'
import GuestPage from './pages/GuestPage.jsx'
import Dashboard from './pages/Dashboard.jsx'
import ActivityPage from './pages/ActivityPage.jsx'

// Unknown URLs used to render a blank page (no matching route, no fallback).
// A presenter fat-fingering a link must get a way back, not a white screen.
function NotFound() {
  return (
    <div className="page narrow center">
      <h1>That page went for a walk 🚶</h1>
      <p className="muted">We could not find it. Let's head back home.</p>
      <div className="hero-actions">
        <Link className="btn btn-primary" to="/">Go home</Link>
      </div>
    </div>
  )
}

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      {/* Keyboard users land here first on every page. */}
      <a className="skip-link" href="#main">Skip to main content</a>
      <main id="main" tabIndex={-1}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/signup" element={<Signup />} />
          <Route path="/login" element={<Login />} />
          <Route path="/guest" element={<GuestPage />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/activity/:moduleId" element={<ActivityPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </BrowserRouter>
  </StrictMode>,
)
