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

// The status word and dot color for a dashboard row: Pending until the first check, then the last result.
export function rowStatus(url) {
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

// Defaults and floors for the two settings. The page must never trust the server blindly: an old
// server, a missing field, or a bad stored value would otherwise give undefined, NaN, or 0, and
// setInterval(load, NaN) fires about every millisecond and floods the API.
export const DEFAULT_CHECK_SECONDS = 60
export const MIN_CHECK_SECONDS = 10
export const DEFAULT_RETENTION_DAYS = 30
export const MIN_RETENTION_DAYS = 1

// Anything missing or not a positive number gets the default; anything too small gets the minimum.
function settingOrDefault(value, fallback, minimum) {
  const n = Math.round(Number(value))
  if (value === null || value === undefined || value === '' || !Number.isFinite(n) || n <= 0) return fallback
  return Math.max(minimum, n)
}

export function checkSecondsOrDefault(value) {
  return settingOrDefault(value, DEFAULT_CHECK_SECONDS, MIN_CHECK_SECONDS)
}

export function retentionDaysOrDefault(value) {
  return settingOrDefault(value, DEFAULT_RETENTION_DAYS, MIN_RETENTION_DAYS)
}
