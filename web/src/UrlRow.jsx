import { useState } from 'react'
import { changeInterval, removeUrl, setPaused } from './api.js'
import { TrashIcon, PlayIcon, PauseIcon, HistoryIcon } from './icons.jsx'
import HistoryModal from './HistoryModal.jsx'
import { rowStatus, formatPercent, formatMs, checkTooltip } from './logic.js'

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
  const [showHistory, setShowHistory] = useState(false)

  // The word next to the dot keeps the status readable without color.
  const status = rowStatus(url)

  function handlePlayPause() {
    setPaused(url.id, !url.paused)
      .then(onChanged)
      .catch((err) => onError(`Could not ${url.paused ? 'resume' : 'pause'}: ${err.message}`))
  }

  function handleRemove() {
    // Soft delete (D-023): the URL stops being checked, but its history stays on the All-checks page.
    if (!confirm(`Stop monitoring ${url.url}? Its history stays in the Archive.`)) return
    removeUrl(url.id)
      .then(onChanged)
      .catch((err) => onError(`Could not remove URL: ${err.message}`))
  }

  return (
    <tr>
      <td>
        {/* Drag this cell's corner to change the row height. */}
        <div className="resizable-row nowrap">
          <span className={`dot ${status.className}`}></span>
          {status.word}
        </div>
      </td>
      <td className="url-cell">{url.url}</td>
      <td>
        <IntervalEditor url={url} onChanged={onChanged} onError={onError} />
      </td>
      <td>{check?.statusCode ?? '—'}</td>
      <td className="nowrap">{check ? `${check.responseTimeMs} ms` : '—'}</td>
      <td className="nowrap">{check ? new Date(check.checkedAt).toLocaleTimeString() : '—'}</td>
      {/* "=== null", not "||": a real 0% must show as 0%. */}
      <td title={`${summary.checkCount} checks`}>
        {formatPercent(summary.uptimePercent)}
      </td>
      <td className="nowrap">{formatMs(summary.avgResponseTimeMs)}</td>
      <td>
        <div className="recent">
          {summary.recent.length === 0 ? (
            '—'
          ) : (
            <div className="strip">
              {summary.recent.map((c) => (
                <span key={c.checkedAt} className={`box ${c.healthy ? 'healthy' : 'unhealthy'}`} title={checkTooltip(c)}></span>
              ))}
            </div>
          )}
          <button
            type="button"
            className="icon"
            onClick={() => setShowHistory(true)}
            aria-label={`Show full history of ${url.url}`}
            title="Full history"
          >
            <HistoryIcon />
          </button>
        </div>
        {showHistory && <HistoryModal url={url} onClose={() => setShowHistory(false)} onError={onError} />}
      </td>
      <td className="error-cell" title={check?.error ?? ''}>
        {check?.error}
      </td>
      <td>
        <div className="actions">
          <button
            type="button"
            className="icon"
            onClick={handlePlayPause}
            aria-label={url.paused ? `Resume checking ${url.url}` : `Pause checking ${url.url}`}
            title={url.paused ? 'Resume' : 'Pause'}
          >
            {url.paused ? <PlayIcon /> : <PauseIcon />}
          </button>
          <button
            type="button"
            className="icon"
            onClick={handleRemove}
            aria-label={`Remove ${url.url}`}
            title="Remove (history is kept)"
          >
            <TrashIcon />
          </button>
        </div>
      </td>
    </tr>
  )
}
