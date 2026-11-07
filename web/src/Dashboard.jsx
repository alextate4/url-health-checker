import { useEffect, useState } from 'react'
import UrlRow from './UrlRow.jsx'
import AddUrlForm from './AddUrlForm.jsx'
import RetentionSetting from './RetentionSetting.jsx'
import { SortHeader, PlainHeader, Pager } from './TableControls.jsx'
import { query } from './api.js'

const WINDOWS = ['1h', '24h', '7d']

// The scheduler wakes every 5s (D1-14), so polling faster would only re-read the same rows.
const POLL_MS = 5000

export default function Dashboard({ onError }) {
  const [data, setData] = useState(null)
  // Not called "window": that would hide the browser's global window object.
  const [timeWindow, setTimeWindow] = useState('1h')
  const [sort, setSort] = useState({ key: 'id', dir: 'asc' })
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  // Bumped after an add, re-time, or remove, so the list reloads at once instead of on the next poll.
  const [version, setVersion] = useState(0)
  const refresh = () => setVersion((v) => v + 1)

  useEffect(() => {
    // Set on cleanup, so a slow reply for old settings cannot overwrite the new data.
    let ignore = false
    const params = query({ window: timeWindow, sort: sort.key, dir: sort.dir, page, pageSize })

    function load() {
      fetch(`/api/urls${params}`)
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
          if (!ignore) onError(`Could not load URLs: ${err.message}`)
        })
    }

    load()
    const timer = setInterval(load, POLL_MS)

    return () => {
      ignore = true
      clearInterval(timer)
    }
  }, [timeWindow, sort, page, pageSize, version, onError])

  // A new sort or page size starts again at page 1, so the user is never left on an empty page.
  function changeSort(next) {
    setSort(next)
    setPage(1)
  }

  const header = (label, key) => <SortHeader label={label} sortKey={key} sort={sort} onSort={changeSort} />

  return (
    <>
      <AddUrlForm onChanged={refresh} onError={onError} />
      <div className="toolbar">
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
        <RetentionSetting onError={onError} />
      </div>
      {data === null ? (
        <p>Loading…</p>
      ) : (
        <>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  {header('Status', 'status')}
                  {header('URL', 'url')}
                  {header('Every', 'interval')}
                  {header('Code', 'statusCode')}
                  {header('Response time', 'responseTimeMs')}
                  {header('Last checked', 'lastChecked')}
                  {header(`Uptime (${timeWindow})`, 'uptime')}
                  {header(`Avg response (${timeWindow})`, 'avg')}
                  <PlainHeader label="Recent checks" />
                  <PlainHeader label="Error" />
                  <PlainHeader label="Actions" />
                </tr>
              </thead>
              <tbody>
                {data.rows.map((u) => (
                  <UrlRow key={u.id} url={u} onChanged={refresh} onError={onError} />
                ))}
              </tbody>
            </table>
          </div>
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
