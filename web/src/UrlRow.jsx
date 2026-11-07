import { useState } from 'react'
import { removeUrl, checkNow } from './api.js'
import { TrashIcon, HistoryIcon, RefreshIcon } from './icons.jsx'
import HistoryModal from './HistoryModal.jsx'
import { rowStatus, formatPercent, formatMs, checkTooltip } from './logic.js'

// One dashboard row. Every URL is checked on the one global interval (D-033); the refresh button
// checks this URL right now.
export default function UrlRow({ url, onChanged, onError }) {
  const check = url.lastCheck
  const summary = url.summary
  const [showHistory, setShowHistory] = useState(false)
  // A check can take up to the 10 s timeout; disable the button meanwhile so it is not clicked twice.
  const [checking, setChecking] = useState(false)

  // The word next to the dot keeps the status readable without color.
  const status = rowStatus(url)

  function handleCheckNow() {
    setChecking(true)
    checkNow(url.id)
      .then(onChanged)
      .catch((err) => onError(`Could not check ${url.url}: ${err.message}`))
      .finally(() => setChecking(false))
  }

  function handleRemove() {
    // Soft delete (D-023): the URL stops being checked, but its history stays in the Archive.
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
            onClick={handleCheckNow}
            disabled={checking}
            aria-label={`Check ${url.url} now`}
            title={checking ? 'Checking…' : 'Check now'}
          >
            <RefreshIcon />
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
