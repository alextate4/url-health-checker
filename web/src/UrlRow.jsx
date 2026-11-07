import { useState } from 'react'
import { changeInterval, removeUrl } from './api.js'

// Plain text for the browser's own tooltip; the word keeps it readable without color.
function tooltip(c) {
  return [
    c.healthy ? 'Healthy' : 'Unhealthy',
    c.statusCode ?? 'no response',
    `${c.responseTimeMs} ms`,
    new Date(c.checkedAt).toLocaleString(),
  ].join(' · ')
}

// Edits the interval in place. The scheduler picks up the new value on its next tick (URL-03).
function IntervalEditor({ url, onChanged, onError }) {
  const [value, setValue] = useState(url.intervalSeconds)

  function handleSubmit(event) {
    event.preventDefault()
    changeInterval(url.id, Number(value))
      .then(onChanged)
      .catch((err) => onError(`Could not change interval: ${err.message}`))
  }

  return (
    <form className="interval" onSubmit={handleSubmit}>
      <input
        type="number"
        required
        min="10"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        aria-label={`Check interval for ${url.url} in seconds`}
      />
      s
      {/* Shown only after an edit, so the row stays quiet otherwise. */}
      {Number(value) !== url.intervalSeconds && <button type="submit">Save</button>}
    </form>
  )
}

export default function UrlRow({ url, onChanged, onError }) {
  const check = url.lastCheck
  const summary = url.summary

  // The word next to the dot keeps the status readable without color.
  let status = { word: 'Pending', className: 'pending' }
  if (check) {
    status = check.healthy
      ? { word: 'Healthy', className: 'healthy' }
      : { word: 'Unhealthy', className: 'unhealthy' }
  }

  function handleRemove() {
    // Soft delete (D-023): the URL stops being checked, but its history stays on the All-checks page.
    if (!confirm(`Stop monitoring ${url.url}? Its history stays on the All-checks page.`)) return
    removeUrl(url.id)
      .then(onChanged)
      .catch((err) => onError(`Could not remove URL: ${err.message}`))
  }

  return (
    <tr>
      <td>
        <span className={`dot ${status.className}`}></span>
        {status.word}
      </td>
      <td>
        <div className="resizable-row">{url.url}</div>
      </td>
      <td>
        <IntervalEditor url={url} onChanged={onChanged} onError={onError} />
      </td>
      <td>{check?.statusCode ?? '—'}</td>
      <td>{check ? `${check.responseTimeMs} ms` : '—'}</td>
      <td>{check ? new Date(check.checkedAt).toLocaleTimeString() : '—'}</td>
      {/* "=== null", not "||": a real 0% must show as 0%. */}
      <td title={`${summary.checkCount} checks`}>
        {summary.uptimePercent === null ? '—' : `${summary.uptimePercent}%`}
      </td>
      <td>{summary.avgResponseTimeMs === null ? '—' : `${summary.avgResponseTimeMs} ms`}</td>
      <td>
        {summary.recent.length === 0 ? (
          '—'
        ) : (
          <div className="strip">
            {summary.recent.map((c) => (
              <span key={c.checkedAt} className={`box ${c.healthy ? 'healthy' : 'unhealthy'}`} title={tooltip(c)}></span>
            ))}
          </div>
        )}
      </td>
      <td>{check?.error}</td>
      <td>
        <button type="button" className="remove" onClick={handleRemove}>
          Remove
        </button>
      </td>
    </tr>
  )
}
