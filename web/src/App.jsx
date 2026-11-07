import { useEffect, useState } from 'react'
import Dashboard from './Dashboard.jsx'
import AllChecks from './AllChecks.jsx'

// Two views and no router package: the URL hash (#/checks) picks the view,
// so the link can be bookmarked and the browser Back button works.
function currentView() {
  return window.location.hash === '#/checks' ? 'checks' : 'dashboard'
}

export default function App() {
  const [view, setView] = useState(currentView)
  const [error, setError] = useState(null)

  useEffect(() => {
    const onHashChange = () => setView(currentView())
    window.addEventListener('hashchange', onHashChange)
    return () => window.removeEventListener('hashchange', onHashChange)
  }, [])

  return (
    <main>
      <header className="app-header">
        <h1>URL Health Monitor</h1>
        <nav>
          <a href="#/" className={view === 'dashboard' ? 'active' : ''}>
            Dashboard
          </a>
          <a href="#/checks" className={view === 'checks' ? 'active' : ''}>
            All checks (full history)
          </a>
        </nav>
      </header>
      {/* Above the table, so a failed request keeps the last good data visible. */}
      {error && <p className="error">{error}</p>}
      {view === 'checks' ? <AllChecks onError={setError} /> : <Dashboard onError={setError} />}
    </main>
  )
}
