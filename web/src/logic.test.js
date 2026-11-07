// Run with: npm --prefix web test   (Node's built-in runner; no packages)
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { nextSort, sortArrow, pageCount, rowStatus, formatPercent, formatMs, checkTooltip, tableRefreshSeconds, rowRefreshDelayMs } from './logic.js'

test('clicking the sorted column flips its direction, both ways', () => {
  assert.deepEqual(nextSort({ key: 'url', dir: 'asc' }, 'url'), { key: 'url', dir: 'desc' })
  assert.deepEqual(nextSort({ key: 'url', dir: 'desc' }, 'url'), { key: 'url', dir: 'asc' })
})

test('clicking a different column sorts it ascending', () => {
  assert.deepEqual(nextSort({ key: 'url', dir: 'desc' }, 'uptime'), { key: 'uptime', dir: 'asc' })
})

test('every sortable header shows an arrow; only the sorted one shows its direction', () => {
  const sort = { key: 'url', dir: 'desc' }
  assert.equal(sortArrow(sort, 'url'), '▼')
  assert.equal(sortArrow({ key: 'url', dir: 'asc' }, 'url'), '▲')
  assert.equal(sortArrow(sort, 'code'), '↕')
})

test('page count rounds up and is never zero', () => {
  assert.equal(pageCount(25, 10), 3)
  assert.equal(pageCount(100, 50), 2)
  assert.equal(pageCount(0, 10), 1)
})

test('row status: paused wins, then pending, then the last result', () => {
  const healthyCheck = { healthy: true }
  assert.equal(rowStatus({ paused: true, lastCheck: healthyCheck }).word, 'Paused')
  assert.equal(rowStatus({ paused: false, lastCheck: null }).word, 'Pending')
  assert.equal(rowStatus({ paused: false, lastCheck: healthyCheck }).className, 'healthy')
  assert.equal(rowStatus({ paused: false, lastCheck: { healthy: false } }).word, 'Unhealthy')
})

test('a real 0 shows as 0, and only a missing value shows a dash', () => {
  assert.equal(formatPercent(0), '0%')
  assert.equal(formatPercent(null), '—')
  assert.equal(formatMs(0), '0 ms')
  assert.equal(formatMs(null), '—')
})

test('a check with no response says so in its tooltip', () => {
  const text = checkTooltip({ healthy: false, statusCode: null, responseTimeMs: 12, checkedAt: '2026-11-07T18:00:00Z' })
  assert.match(text, /^Unhealthy · no response · 12 ms · /)
})

test('table auto-refresh interval: the typed number, never below 5 s, 30 s if not a number', () => {
  assert.equal(tableRefreshSeconds('20'), 20)
  assert.equal(tableRefreshSeconds(2), 5)
  assert.equal(tableRefreshSeconds(''), 30)
  assert.equal(tableRefreshSeconds('abc'), 30)
})

test('a playing row reloads just after its next check should have landed', () => {
  const now = Date.parse('2026-11-07T20:00:00Z')
  // checked 4 s ago, every 10 s: due in 6 s, plus 6 s to settle = 12 s from now
  assert.equal(rowRefreshDelayMs('2026-11-07T19:59:56Z', 10, now), 12000)
  // long overdue (a slow or hanging check): retry soon, but never faster than 3 s
  assert.equal(rowRefreshDelayMs('2026-11-07T19:50:00Z', 10, now), 3000)
  // never checked yet: look again after one settle period
  assert.equal(rowRefreshDelayMs(null, 30, now), 6000)
})
