export default function UrlRow({ url }) {
  const check = url.lastCheck

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
      <td>{check?.error}</td>
    </tr>
  )
}
