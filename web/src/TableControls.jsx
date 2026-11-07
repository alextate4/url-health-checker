// Shared by the dashboard and the All-checks table. The server does the sorting and paging;
// these components only change the request.

// A header cell that sorts by its column. Clicking the same column again flips the direction.
export function SortHeader({ label, sortKey, sort, onSort }) {
  const active = sort.key === sortKey
  // Every sortable column shows an arrow, so it is clear the column can be sorted.
  // ↕ = not sorted by this column yet; ▲/▼ = the current sort and its direction.
  const arrow = active ? (sort.dir === 'asc' ? '▲' : '▼') : '↕'

  function handleClick() {
    onSort({ key: sortKey, dir: active && sort.dir === 'asc' ? 'desc' : 'asc' })
  }

  return (
    <th aria-sort={active ? (sort.dir === 'asc' ? 'ascending' : 'descending') : 'none'}>
      {/* The inner div is what the user drags to resize the column (CSS "resize"). */}
      <div className="resizable">
        <button type="button" className="sort" onClick={handleClick}>
          {label}
          <span className={active ? 'sort-arrow' : 'sort-arrow idle'}>{arrow}</span>
        </button>
      </div>
    </th>
  )
}

// A header cell that does not sort, but can still be resized like the others.
export function PlainHeader({ label }) {
  return (
    <th>
      <div className="resizable">{label}</div>
    </th>
  )
}

export const PAGE_SIZES = [10, 50, 100]

export function Pager({ total, page, pageSize, onPage, onPageSize }) {
  const pages = Math.max(1, Math.ceil(total / pageSize))

  return (
    <div className="pager">
      <label>
        Show
        <select value={pageSize} onChange={(e) => onPageSize(Number(e.target.value))}>
          {PAGE_SIZES.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
        rows
      </label>
      <button type="button" disabled={page <= 1} onClick={() => onPage(page - 1)}>
        ‹ Prev
      </button>
      <span>
        Page {page} of {pages} ({total} total)
      </span>
      <button type="button" disabled={page >= pages} onClick={() => onPage(page + 1)}>
        Next ›
      </button>
    </div>
  )
}
