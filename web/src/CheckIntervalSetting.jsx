import { useEffect, useState } from 'react'
import { getCheckIntervalSeconds, saveCheckIntervalSeconds } from './api.js'

// The ONE rate (D-033): the server checks every URL this often, and the table auto-refreshes at the
// same rate. Saved on the server, so it is shared by every browser tab.
export default function CheckIntervalSetting({ onLoaded, onError }) {
  const [seconds, setSeconds] = useState('')
  const [saved, setSaved] = useState(null)

  useEffect(() => {
    getCheckIntervalSeconds()
      .then((value) => {
        setSeconds(value)
        setSaved(value)
        onLoaded(value)
      })
      .catch((err) => onError(`Could not load the check interval: ${err.message}`))
  }, [onLoaded, onError])

  function handleSubmit(event) {
    event.preventDefault()
    const value = Number(seconds)
    saveCheckIntervalSeconds(value)
      .then(() => {
        setSaved(value)
        onLoaded(value)
      })
      .catch((err) => onError(`Could not save the check interval: ${err.message}`))
  }

  return (
    <form className="retention" onSubmit={handleSubmit}>
      <label>
        Check every
        <input type="number" required min="10" max="86400" value={seconds} onChange={(e) => setSeconds(e.target.value)} />
        s
      </label>
      {Number(seconds) !== saved && <button type="submit">Save</button>}
    </form>
  )
}
