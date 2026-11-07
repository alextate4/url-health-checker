import { useEffect, useState } from 'react'
import { getRetentionDays, saveRetentionDays } from './api.js'

// CONF-01: one global setting. Old results are archived, not deleted, so the label says so (D-023).
export default function RetentionSetting({ onError }) {
  const [days, setDays] = useState('')
  const [saved, setSaved] = useState(null)

  useEffect(() => {
    getRetentionDays()
      .then((value) => {
        setDays(value)
        setSaved(value)
      })
      .catch((err) => onError(`Could not load retention: ${err.message}`))
  }, [onError])

  function handleSubmit(event) {
    event.preventDefault()
    saveRetentionDays(Number(days))
      .then(() => setSaved(Number(days)))
      .catch((err) => onError(`Could not save retention: ${err.message}`))
  }

  return (
    <form className="retention" onSubmit={handleSubmit}>
      <label>
        Archive results older than
        <input type="number" required min="1" value={days} onChange={(e) => setDays(e.target.value)} />
        days
      </label>
      {Number(days) !== saved && <button type="submit">Save</button>}
    </form>
  )
}
