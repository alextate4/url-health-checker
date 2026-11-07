import { useCallback, useEffect, useState } from 'react'
import UrlRow from './UrlRow.jsx'
import AddUrlForm from './AddUrlForm.jsx'
import RetentionSetting from './RetentionSetting.jsx'
import CheckIntervalSetting from './CheckIntervalSetting.jsx'
import { SortHeader, PlainHeader, Pager } from './TableControls.jsx'
import { query } from './api.js'
import { RefreshIcon } from './icons.jsx'
import { checkSecondsOrDefault, DEFAULT_CHECK_SECONDS } from './logic.js'

const WINDOWS = ['1h', '24h', '7d']


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
  // ONE rate (D-033): the server checks every URL every checkSeconds, and auto-refresh re-reads the
  // table at that same rate, so each refresh can show new results. Auto-refresh only reads.
  const [autoRefresh, setAutoRefresh] = useState(true)
  const [checkSeconds, setCheckSeconds] = useState(DEFAULT_CHECK_SECONDS)
  // Normalized again here, so the timer can never get NaN, 0, or anything under 10 s.
  const onIntervalLoaded = useCallback((seconds) => setCheckSeconds(checkSecondsOrDefault(seconds)), [])
  const refreshMs = checkSecondsOrDefault(checkSeconds) * 1000

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
    const timer = autoRefresh ? setInterval(load, refreshMs) : null

    return () => {
      ignore = true
      clearInterval(timer)
    }
  }, [timeWindow, sort, page, pageSize, version, autoRefresh, refreshMs, onError])

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
          {/* Reloads the whole table now, whatever the auto-refresh setting. */}
          <button type="button" className="secondary refresh" onClick={refresh}>
            <RefreshIcon /> Refresh
          </button>
          <label className="auto-refresh" title={`Reloads the table every ${checkSeconds} s, the same rate as the checks`}>
            <input type="checkbox" checked={autoRefresh} onChange={(e) => setAutoRefresh(e.target.checked)} />
            Auto-refresh
          </label>
        </div>
        <div className="settings">
          <CheckIntervalSetting onLoaded={onIntervalLoaded} onError={onError} />
          <RetentionSetting onError={onError} />
        </div>
      </div>
      {data === null ? (
        <p>Loading…</p>
      ) : (
        <>
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
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  {header('Status', 'status')}
                  {header('URL', 'url')}
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
        </>
      )}
    </>
  )
}
