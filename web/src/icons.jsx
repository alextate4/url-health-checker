// Small inline SVG icons, so no icon package is needed. They use currentColor, so CSS sets
// their color. Each button that shows one also has an aria-label for screen readers.

function Svg({ children }) {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
      strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {children}
    </svg>
  )
}

export function PlusIcon() {
  return (
    <Svg>
      <path d="M12 5v14M5 12h14" />
    </Svg>
  )
}

export function TrashIcon() {
  return (
    <Svg>
      <path d="M3 6h18M8 6V4h8v2M6 6l1 14h10l1-14M10 11v6M14 11v6" />
    </Svg>
  )
}

export function PlayIcon() {
  return (
    <Svg>
      <path d="M7 4l13 8-13 8z" fill="currentColor" />
    </Svg>
  )
}

export function PauseIcon() {
  return (
    <Svg>
      <path d="M7 4h3v16H7zM14 4h3v16h-3z" fill="currentColor" />
    </Svg>
  )
}

export function HistoryIcon() {
  return (
    <Svg>
      <path d="M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5M12 7v5l3 2" />
    </Svg>
  )
}
