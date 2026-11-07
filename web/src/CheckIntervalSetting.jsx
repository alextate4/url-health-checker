import { useEffect, useState } from 'react'
import { saveCheckIntervalSeconds } from './api.js'
import { checkSecondsOrDefault, DEFAULT_CHECK_SECONDS, MIN_CHECK_SECONDS } from './logic.js'

// The ONE rate (D-033): the server checks every URL this often, and the table auto-refreshes at the
// same rate. Saved on the server, so it is shared by every browser tab.
// `loaded` is undefined until the Dashboard's one settings request answers.
export default function CheckIntervalSetting({ loaded, onSaved, onError }) {
  // Starts at the default, so the box is never empty while the server answers (or if it cannot).
  const [seconds, setSeconds] = useState(DEFAULT_CHECK_SECONDS)
  const [saved, setSaved] = useState(DEFAULT_CHECK_SECONDS)

  useEffect(() => {
    if (loaded === undefined) return
    const value = checkSecondsOrDefault(loaded)
    setSeconds(value)
    setSaved(value)
  }, [loaded])

  function handleSubmit(event) {
    event.preventDefault()
    // The input's min/max already block a bad value in the browser; the server checks again (D-026).
    const value = checkSecondsOrDefault(seconds)
    setSeconds(value)
    saveCheckIntervalSeconds(value)
      .then(() => {
        setSaved(value)
        onSaved(value)
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
