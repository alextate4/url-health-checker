import { useState } from 'react'
import { addUrl } from './api.js'

// The browser's own checks (type="url", min) stop obvious typos. The server does not validate yet:
// that is a logged edge case.
export default function AddUrlForm({ onChanged, onError }) {
  const [url, setUrl] = useState('')
  const [intervalSeconds, setIntervalSeconds] = useState(60)

  function handleSubmit(event) {
    event.preventDefault()
    addUrl(url, Number(intervalSeconds))
      .then(() => {
        setUrl('')
        onChanged()
      })
      .catch((err) => onError(`Could not add URL: ${err.message}`))
  }

  return (
    <form className="add-url" onSubmit={handleSubmit}>
      <input
        type="url"
        required
        placeholder="https://example.org"
        value={url}
        onChange={(e) => setUrl(e.target.value)}
        aria-label="URL to monitor"
      />
      <label>
        every
        <input
          type="number"
          required
          min="10"
          value={intervalSeconds}
          onChange={(e) => setIntervalSeconds(e.target.value)}
        />
        s
      </label>
      <button type="submit">Add URL</button>
    </form>
  )
}
