import { useEffect, useState } from 'react'
import { getCheckIntervalSeconds, saveCheckIntervalSeconds } from './api.js'
import { checkSecondsOrDefault, DEFAULT_CHECK_SECONDS, MIN_CHECK_SECONDS } from './logic.js'

// The ONE rate (D-033): the server checks every URL this often, and the table auto-refreshes at the
// same rate. Saved on the server, so it is shared by every browser tab.
export default function CheckIntervalSetting({ onLoaded, onError }) {
  // Starts at the default, so the box is never empty while the server answers (or if it cannot).
  const [seconds, setSeconds] = useState(DEFAULT_CHECK_SECONDS)
  const [saved, setSaved] = useState(DEFAULT_CHECK_SECONDS)

  useEffect(() => {
    getCheckIntervalSeconds()
      .then((loaded) => {
        const value = checkSecondsOrDefault(loaded)
        setSeconds(value)
        setSaved(value)
        onLoaded(value)
      })
      .catch((err) => onError(`Could not load the check interval: ${err.message}`))
  }, [onLoaded, onError])

  function handleSubmit(event) {
    event.preventDefault()
    // The input's min/max already block a bad value in the browser; the server checks again (D-026).
    const value = checkSecondsOrDefault(seconds)
    setSeconds(value)
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
        <input
          type="number"
          required
          min={MIN_CHECK_SECONDS}
          max="86400"
          value={seconds}
          onChange={(e) => setSeconds(e.target.value)}
        />
        s
      </label>
      {Number(seconds) !== saved && <button type="submit">Save</button>}
    </form>
  )
}
