// The three calls that change the URL list. Each one throws on a non-2xx answer,
// so the caller can show the error instead of failing quietly.

async function send(method, path, body) {
  const response = await fetch(path, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
}

export function addUrl(url, intervalSeconds) {
  return send('POST', '/api/urls', { url, intervalSeconds })
}

// Not called "setInterval": that would hide the browser's timer function.
export function changeInterval(id, intervalSeconds) {
  return send('PATCH', `/api/urls/${id}`, { intervalSeconds })
}

export function removeUrl(id) {
  return send('DELETE', `/api/urls/${id}`)
}
