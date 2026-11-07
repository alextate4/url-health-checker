// The UI's decisions, kept out of the components as plain functions, so they can be tested with
// Node's built-in test runner (no test package, no browser). The components only call these.

// Clicking the sorted column flips its direction; clicking another column sorts it ascending.
export function nextSort(current, clickedKey) {
  const sameColumn = current.key === clickedKey
  return { key: clickedKey, dir: sameColumn && current.dir === 'asc' ? 'desc' : 'asc' }
}

// The arrow a header shows: ↕ when it is not the sorted column.
export function sortArrow(current, key) {
  if (current.key !== key) return '↕'
  return current.dir === 'asc' ? '▲' : '▼'
}

// At least one page, so an empty table shows "page 1 of 1", not "page 1 of 0".
export function pageCount(total, pageSize) {
  return Math.max(1, Math.ceil(total / pageSize))
}

// The status word and dot color for a dashboard row. Paused wins, because the last result is old.
export function rowStatus(url) {
  if (url.paused) return { word: 'Paused', className: 'pending' }
  if (!url.lastCheck) return { word: 'Pending', className: 'pending' }
  return url.lastCheck.healthy
    ? { word: 'Healthy', className: 'healthy' }
    : { word: 'Unhealthy', className: 'unhealthy' }
}

// "=== null", not "||": a real 0% must show as 0%, and only a missing value shows as a dash.
export function formatPercent(value) {
  return value === null || value === undefined ? '—' : `${value}%`
}

export function formatMs(value) {
  return value === null || value === undefined ? '—' : `${value} ms`
}

// Text for one box in the recent-checks strip; the word keeps it readable without color.
export function checkTooltip(check) {
  return [
    check.healthy ? 'Healthy' : 'Unhealthy',
    check.statusCode ?? 'no response',
    `${check.responseTimeMs} ms`,
    new Date(check.checkedAt).toLocaleString(),
  ].join(' · ')
}

// Turns response times into SVG polyline points ("x,y x,y ..."), oldest on the left.
// The fastest value sits at the bottom and the slowest at the top. Checks with no response
// (null status) are skipped, because their time is a timeout or DNS failure, not a speed (D-019).
// Returns '' when there are fewer than two points, since one point cannot draw a line.
export function sparklinePoints(checks, width, height) {
  const values = checks.filter((c) => c.statusCode !== null).map((c) => c.responseTimeMs)
  if (values.length < 2) return ''
  const min = Math.min(...values)
  const max = Math.max(...values)
  const range = max - min || 1 // all equal → a flat line in the middle instead of dividing by 0
  const step = width / (values.length - 1)
  return values
    .map((v, i) => {
      const x = i * step
      const y = max === min ? height / 2 : height - ((v - min) / range) * height
      return `${round(x)},${round(y)}`
    })
    .join(' ')
}

function round(n) {
  return Math.round(n * 10) / 10
}
