// Plain text for the browser's own tooltip; the word keeps it readable without color.
function tooltip(c) {
  return [
    c.healthy ? 'Healthy' : 'Unhealthy',
    c.statusCode ?? 'no response',
    `${c.responseTimeMs} ms`,
    new Date(c.checkedAt).toLocaleString(),
  ].join(' · ')
}

export default function UrlRow({ url }) {
  const check = url.lastCheck
  const summary = url.summary

  // The word next to the dot keeps the status readable without color.
  let status = { word: 'Pending', className: 'pending' }
  if (check) {
    status = check.healthy
      ? { word: 'Healthy', className: 'healthy' }
      : { word: 'Unhealthy', className: 'unhealthy' }
  }

  return (
    <tr>
      <td>
        <span className={`dot ${status.className}`}></span>
        {status.word}
      </td>
      <td>{url.url}</td>
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
    </tr>
  )
}
