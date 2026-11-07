import { useEffect, useState } from 'react'
import { SortHeader, PlainHeader, Pager } from './TableControls.jsx'
import { query } from './api.js'

// Every result ever saved: live and archived, for every URL, removed ones included (D-023).
// Not polled: new rows would shift the pages under the user. The Refresh button reloads on demand.
// With urlId it shows one URL only (the history modal on the dashboard).
export default function AllChecks({ onError, urlId }) {
  const [data, setData] = useState(null)
  const [sort, setSort] = useState({ key: 'checkedAt', dir: 'desc' })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(urlId ? 10 : 50)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    const params = { sort: sort.key, dir: sort.dir, page, pageSize }
    if (urlId) params.urlId = urlId
    fetch(`/api/checks${query(params)}`)
      .then((response) => {
        if (!response.ok) throw new Error(`HTTP ${response.status}`)
        return response.json()
      })
      .then((json) => {
        if (ignore) return
        setData(json)
        onError(null)
      })
      .catch((err) => {
        if (!ignore) onError(`Could not load checks: ${err.message}`)
      })
    return () => {
      ignore = true
    }
  }, [sort, page, pageSize, version, onError, urlId])

  function changeSort(next) {
    setSort(next)
    setPage(1)
  }

  const header = (label, key) => <SortHeader label={label} sortKey={key} sort={sort} onSort={changeSort} />

  return (
    <>
      <div className="toolbar">
        <p className="hint">
          {urlId
            ? 'Every check for this URL, newest first.'
            : 'Every check ever saved, including archived results and removed URLs (marked "removed").'}
        </p>
        <button type="button" onClick={() => setVersion((v) => v + 1)}>
          Refresh
        </button>
      </div>
      {data === null ? (
        <p>Loading…</p>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                {header('Checked at', 'checkedAt')}
                {/* In the history modal the URL is already in the title, so the column is left out. */}
                {!urlId && header('URL', 'url')}
                {header('Result', 'healthy')}
                {header('Code', 'statusCode')}
                {header('Response time', 'responseTimeMs')}
                <PlainHeader label="Error" />
              </tr>
            </thead>
            <tbody>
              {data.rows.map((c, i) => (
                <tr key={`${c.urlId}-${c.checkedAt}-${i}`}>
                  <td>
                    <div className="resizable-row">{new Date(c.checkedAt).toLocaleString()}</div>
                  </td>
                  {!urlId && (
                    <td>
                      {c.url}
                      {c.urlRemoved && <span className="tag">removed</span>}
                    </td>
                  )}
                  <td>
                    <span className={`dot ${c.healthy ? 'healthy' : 'unhealthy'}`}></span>
                    {c.healthy ? 'Healthy' : 'Unhealthy'}
                  </td>
                  <td>{c.statusCode ?? '—'}</td>
                  <td>{c.responseTimeMs} ms</td>
                  <td>{c.error}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pager
            total={data.total}
            page={page}
            pageSize={pageSize}
            onPage={setPage}
            onPageSize={(size) => {
              setPageSize(size)
              setPage(1)
            }}
          />
        </>
      )}
    </>
  )
}
