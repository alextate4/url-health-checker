import { useState } from 'react'
import { addUrl } from './api.js'
import { PlusIcon } from './icons.jsx'

// The browser's own check (type="url") stops obvious typos; the server validates too (D-026).
// There is no per-URL interval: every URL is checked on the one global interval (D-033).
export default function AddUrlForm({ onChanged, onError }) {
  const [url, setUrl] = useState('')

  function handleSubmit(event) {
    event.preventDefault()
    addUrl(url)
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
      <button type="submit" className="primary">
        <PlusIcon /> Add URL
      </button>
    </form>
  )
}
