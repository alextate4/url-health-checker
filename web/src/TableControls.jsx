import { nextSort, sortArrow, pageCount } from './logic.js'

// Shared by the dashboard and the All-checks table. The server does the sorting and paging;
// these components only change the request.

// A header cell that sorts by its column. Clicking the same column again flips the direction.
export function SortHeader({ label, sortKey, sort, onSort }) {
  const active = sort.key === sortKey
  // Every sortable column shows an arrow (↕ until sorted), so it is clear it can be sorted.
  const arrow = sortArrow(sort, sortKey)

  function handleClick() {
    onSort(nextSort(sort, sortKey))
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
  const pages = pageCount(total, pageSize)

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
