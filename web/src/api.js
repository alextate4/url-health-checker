// The three calls that change the URL list. Each one throws on a non-2xx answer,
// so the caller can show the error instead of failing quietly.

async function send(method, path, body) {
  const response = await fetch(path, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })
  if (!response.ok) {
    // A 400 carries { error: "why" }; show that, and fall back to the status code.
    const body = await response.json().catch(() => null)
    throw new Error(body?.error ?? `HTTP ${response.status}`)
  }
}

export function addUrl(url, intervalSeconds) {
  return send('POST', '/api/urls', { url, intervalSeconds })
}

// Not called "setInterval": that would hide the browser's timer function.
export function changeInterval(id, intervalSeconds) {
  return send('PATCH', `/api/urls/${id}`, { intervalSeconds })
}

export function setPaused(id, paused) {
  return send('PATCH', `/api/urls/${id}`, { paused })
}

// One dashboard row, for a playing row that refreshes itself.
export async function getUrlRow(id, timeWindow) {
  const response = await fetch(`/api/urls/${id}${query({ window: timeWindow })}`)
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return response.json()
}

// Runs one check now; resolves when the result is saved.
export function checkNow(id) {
  return send('POST', `/api/urls/${id}/check`)
}

export function removeUrl(id) {
  return send('DELETE', `/api/urls/${id}`)
}

export async function getRetentionDays() {
  const response = await fetch('/api/settings')
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return (await response.json()).retentionDays
}

export function saveRetentionDays(retentionDays) {
  return send('PUT', '/api/settings', { retentionDays })
}

// Builds "?sort=url&dir=asc&page=1&pageSize=10" from a plain object.
export function query(params) {
  return '?' + new URLSearchParams(params).toString()
}
