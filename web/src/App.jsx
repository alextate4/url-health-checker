import { useEffect, useState } from 'react'
import UrlRow from './UrlRow.jsx'

const WINDOWS = ['1h', '24h', '7d']

// The scheduler wakes every 5s (D1-14), so polling faster would only re-read the same rows.
const POLL_MS = 5000

export default function App() {
  const [urls, setUrls] = useState(null)
  const [error, setError] = useState(null)
  // Not called "window": that would hide the browser's global window object.
  const [timeWindow, setTimeWindow] = useState('1h')

  useEffect(() => {
    // Set on cleanup, so a slow reply for the old window cannot overwrite the new window's data.
    let ignore = false

    function load() {
      fetch(`/api/urls?window=${timeWindow}`)
        .then((response) => {
          if (!response.ok) throw new Error(`HTTP ${response.status}`)
          return response.json()
        })
        .then((data) => {
          if (ignore) return
          setUrls(data)
          setError(null)
        })
        .catch((err) => {
          if (!ignore) setError(err.message)
        })
    }

    load()
    const timer = setInterval(load, POLL_MS)

    return () => {
      ignore = true
      clearInterval(timer)
    }
  }, [timeWindow])

  return (
    <main>
      <h1>URL Health Monitor</h1>
      <div className="window-picker">
        <span>Window:</span>
        {WINDOWS.map((w) => (
          <button
            key={w}
            type="button"
            className={w === timeWindow ? 'selected' : ''}
            aria-pressed={w === timeWindow}
            onClick={() => setTimeWindow(w)}
          >
            {w}
          </button>
        ))}
      </div>
      {/* Above the table, so a failed poll keeps the last good data visible. */}
      {error && <p className="error">Could not load URLs: {error}</p>}
      {urls === null ? (
        !error && <p>Loading…</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Status</th>
              <th>URL</th>
              <th>Code</th>
              <th>Response time</th>
              <th>Last checked</th>
              <th>Uptime ({timeWindow})</th>
              <th>Avg response ({timeWindow})</th>
              <th>Error</th>
            </tr>
          </thead>
          <tbody>
            {urls.map((u) => (
              <UrlRow key={u.id} url={u} />
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}
