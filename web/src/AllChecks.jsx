import { useEffect, useState } from 'react'
import { SortHeader, Pager } from './TableControls.jsx'
import { query } from './api.js'

// Every result ever saved: live and archived, for every URL, removed ones included (D-023).
// Not polled: new rows would shift the pages under the user. The Refresh button reloads on demand.
export default function AllChecks({ onError }) {
  const [data, setData] = useState(null)
  const [sort, setSort] = useState({ key: 'checkedAt', dir: 'desc' })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(50)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    fetch(`/api/checks${query({ sort: sort.key, dir: sort.dir, page, pageSize })}`)
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
  }, [sort, page, pageSize, version, onError])

  function changeSort(next) {
    setSort(next)
    setPage(1)
  }

  const header = (label, key) => <SortHeader label={label} sortKey={key} sort={sort} onSort={changeSort} />

  return (
    <>
      <div className="toolbar">
        <p className="hint">Every check ever saved. Archived results and removed URLs are kept and marked.</p>
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
                {header('URL', 'url')}
                {header('Result', 'healthy')}
                {header('Code', 'statusCode')}
                {header('Response time', 'responseTimeMs')}
                <th>Error</th>
                {header('Stored in', 'archived')}
              </tr>
            </thead>
            <tbody>
              {data.rows.map((c, i) => (
                <tr key={`${c.urlId}-${c.checkedAt}-${i}`}>
                  <td>{new Date(c.checkedAt).toLocaleString()}</td>
                  <td>
                    <div className="resizable-row">
                      {c.url}
                      {c.urlRemoved && <span className="tag">removed</span>}
                    </div>
                  </td>
                  <td>
                    <span className={`dot ${c.healthy ? 'healthy' : 'unhealthy'}`}></span>
                    {c.healthy ? 'Healthy' : 'Unhealthy'}
                  </td>
                  <td>{c.statusCode ?? '—'}</td>
                  <td>{c.responseTimeMs} ms</td>
                  <td>{c.error}</td>
                  <td>{c.archived ? 'Archive' : 'Live'}</td>
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
