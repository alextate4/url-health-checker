import { useEffect, useRef } from 'react'
import AllChecks from './AllChecks.jsx'

// The full history of one URL: live and archived checks, sorted and paged like the All-checks page.
// Uses the browser's own <dialog>, which gives Escape-to-close and focus trapping for free.
export default function HistoryModal({ url, onClose, onError }) {
  const dialogRef = useRef(null)

  useEffect(() => {
    dialogRef.current.showModal()
  }, [])

  return (
    <dialog ref={dialogRef} className="history-modal" onClose={onClose}>
      <div className="modal-header">
        <h2>History: {url.url}</h2>
        <button type="button" onClick={() => dialogRef.current.close()} aria-label="Close history">
          ✕
        </button>
      </div>
      <AllChecks urlId={url.id} onError={onError} />
    </dialog>
  )
}
