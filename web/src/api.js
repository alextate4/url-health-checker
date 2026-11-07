// The calls that change the URL list or the settings. Each one throws on a non-2xx answer,
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

export function addUrl(url) {
  return send('POST', '/api/urls', { url })
}

// Runs one check now (each row's refresh button); resolves when the result is saved.
export function checkNow(id) {
  return send('POST', `/api/urls/${id}/check`)
}

export function removeUrl(id) {
  return send('DELETE', `/api/urls/${id}`)
}

async function getSettings() {
  const response = await fetch('/api/settings')
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return response.json()
}

export async function getRetentionDays() {
  return (await getSettings()).retentionDays
}

export async function getCheckIntervalSeconds() {
  return (await getSettings()).checkIntervalSeconds
}

export function saveCheckIntervalSeconds(checkIntervalSeconds) {
  return send('PUT', '/api/settings', { checkIntervalSeconds })
}

export function saveRetentionDays(retentionDays) {
  return send('PUT', '/api/settings', { retentionDays })
}

// Builds "?sort=url&dir=asc&page=1&pageSize=10" from a plain object.
export function query(params) {
  return '?' + new URLSearchParams(params).toString()
}
