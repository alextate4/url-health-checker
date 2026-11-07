import { useEffect, useState } from 'react'
import { saveRetentionDays } from './api.js'
import { retentionDaysOrDefault, DEFAULT_RETENTION_DAYS, MIN_RETENTION_DAYS } from './logic.js'

// CONF-01: one global setting. Old results are archived, not deleted, so the label says so (D-023).
// `loaded` is undefined until the Dashboard's one settings request answers.
export default function RetentionSetting({ loaded, onError }) {
  // Starts at the default, so the box is never empty while the server answers (or if it cannot).
  const [days, setDays] = useState(DEFAULT_RETENTION_DAYS)
  const [saved, setSaved] = useState(null)

  useEffect(() => {
    if (loaded === undefined) return
    // A stored 0 or a missing value shows the default; Save then fixes the server too.
    setDays(retentionDaysOrDefault(loaded))
    setSaved(loaded)
  }, [loaded])

  function handleSubmit(event) {
    event.preventDefault()
    const value = retentionDaysOrDefault(days)
    setDays(value)
    saveRetentionDays(value)
      .then(() => setSaved(value))
      .catch((err) => onError(`Could not save retention: ${err.message}`))
  }

  return (
    <form className="retention" onSubmit={handleSubmit}>
      <label>
        Archive results older than
        <input type="number" required min={MIN_RETENTION_DAYS} value={days} onChange={(e) => setDays(e.target.value)} />
        days
      </label>
      {Number(days) !== saved && <button type="submit">Save</button>}
    </form>
  )
}
