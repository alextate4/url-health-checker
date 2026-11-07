import { useEffect, useState } from 'react'
import UrlRow from './UrlRow.jsx'

export default function App() {
  const [urls, setUrls] = useState(null)
  const [error, setError] = useState(null)

  // Fetched once on page load. Polling comes later; for now a page reload shows newer results.
  useEffect(() => {
    fetch('/api/urls')
      .then((response) => {
        if (!response.ok) throw new Error(`HTTP ${response.status}`)
        return response.json()
      })
      .then(setUrls)
      .catch((err) => setError(err.message))
  }, [])

  return (
    <main>
      <h1>URL Health Monitor</h1>
      {error ? (
        <p className="error">Could not load URLs: {error}</p>
      ) : urls === null ? (
        <p>Loading…</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Status</th>
              <th>URL</th>
              <th>Code</th>
              <th>Response time</th>
              <th>Last checked</th>
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
